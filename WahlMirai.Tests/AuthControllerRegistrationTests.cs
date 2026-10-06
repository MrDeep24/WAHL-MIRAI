// ============================================================
// Archivo:       AuthControllerRegistrationTests.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Verifica límites por IP y fallos de whitelist.
// Requerimientos: RF-M01-00, RN-1, RN-1.1, RN-15.
// ============================================================
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MySqlConnector;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Tests;

/// <summary>Pruebas del contador por IP y las respuestas genéricas de auto-registro (RN-1, RN-1.1, RN-15).</summary>
public sealed class AuthControllerRegistrationTests
{
    private const string ClientIp = "192.0.2.30";
    private const string GenericMessage = "No fue posible procesar el auto-registro con el documento ingresado. Si consideras que esto es un error o requieres asistencia, por favor contacta al Administrador institucional.";

    /// <summary>Los 29 primeros lookups fallidos no bloquean el IP ni generan auditoría de fallo.</summary>
    [Fact]
    public async Task Registro_FailuresBelowDefaultThreshold_DoNotLockOrAuditEachFailure()
    {
        var whitelist = new TestWhitelistService();
        var audit = new FakeAuditService();
        var store = new LoginAttemptStore(TimeProvider.System);
        var controller = CreateController(whitelist, store, audit);

        for (var attempt = 0; attempt < 29; attempt++)
        {
            controller.ModelState.Clear();
            var result = await controller.Registro(new RegistroStep1ViewModel { Document = $"12345{attempt}" });
            AssertGenericMessage(controller, result);
        }

        Assert.Equal(29, whitelist.LookupCount);
        Assert.False(store.IsLockedOut($"register:{ClientIp}", out _));
        Assert.DoesNotContain("REGISTRO_LOCKED_OUT", audit.Actions);
    }

    /// <summary>El fallo 30 activa un único evento; los POST posteriores no consultan la whitelist (RN-1, RN-15).</summary>
    [Fact]
    public async Task Registro_ThresholdLocksIpOnce_AndBothEndpointsSkipWhitelistWhileLocked()
    {
        var whitelist = new TestWhitelistService();
        var audit = new FakeAuditService();
        var store = new LoginAttemptStore(TimeProvider.System);
        var controller = CreateController(whitelist, store, audit);

        for (var attempt = 0; attempt < 30; attempt++)
        {
            controller.ModelState.Clear();
            var result = await controller.Registro(new RegistroStep1ViewModel { Document = $"12345{attempt}" });
            AssertGenericMessage(controller, result);
        }

        Assert.Equal(30, whitelist.LookupCount);
        Assert.Equal(new[] { "REGISTRO_LOCKED_OUT" }, audit.Actions);
        Assert.Equal(ClientIp, audit.IpAddresses.Single());

        controller.ModelState.Clear();
        var lockedLookup = await controller.Registro(new RegistroStep1ViewModel { Document = "123456" });
        AssertGenericMessage(controller, lockedLookup);
        controller.ModelState.Clear();
        var lockedComplete = await controller.Completar(CreateStep2Model());
        AssertGenericMessage(controller, lockedComplete);

        Assert.Equal(30, whitelist.LookupCount);
        Assert.Equal(0, whitelist.RegistrationCount);
        Assert.Single(audit.Actions);
    }

    /// <summary>Una validación fallida al completar cuenta en el mismo bucket IP (RF-M01-00, RN-1.1, RN-15).</summary>
    [Fact]
    public async Task Completar_WhitelistValidationFailure_UsesRegistrationBucket()
    {
        var whitelist = new TestWhitelistService
        {
            RegistrationException = new InvalidOperationException("Whitelist entry already claimed.")
        };
        var store = new LoginAttemptStore(TimeProvider.System);
        var controller = CreateController(whitelist, store, new FakeAuditService(), failureThreshold: 2);

        controller.ModelState.Clear();
        AssertGenericMessage(controller, await controller.Completar(CreateStep2Model()));
        Assert.False(store.IsLockedOut($"register:{ClientIp}", out _));
        controller.ModelState.Clear();
        AssertGenericMessage(controller, await controller.Completar(CreateStep2Model()));

        Assert.True(store.IsLockedOut($"register:{ClientIp}", out _));
        Assert.Equal(2, whitelist.RegistrationCount);
    }

    /// <summary>Una violación MySQL 1062 de la claim concurrente devuelve el mismo mensaje genérico (RN-1.1).</summary>
    [Fact]
    public async Task Completar_DuplicateKeyDbUpdateException_ReturnsGenericMessage()
    {
        var whitelist = new TestWhitelistService
        {
            RegistrationException = CreateDuplicateKeyDbUpdateException()
        };
        var controller = CreateController(whitelist, new LoginAttemptStore(TimeProvider.System), new FakeAuditService());

        var result = await controller.Completar(CreateStep2Model());

        AssertGenericMessage(controller, result);
        Assert.Equal(1, whitelist.RegistrationCount);
    }

    /// <summary>Los errores de persistencia distintos de MySQL 1062 siguen propagándose sin ocultarse.</summary>
    [Fact]
    public async Task Completar_OtherDbUpdateException_IsRethrown()
    {
        var databaseException = new DbUpdateException("database unavailable", new InvalidOperationException("other database error"));
        var whitelist = new TestWhitelistService { RegistrationException = databaseException };
        var controller = CreateController(whitelist, new LoginAttemptStore(TimeProvider.System), new FakeAuditService());

        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => controller.Completar(CreateStep2Model()));

        Assert.Same(databaseException, thrown);
    }

    private static AuthController CreateController(
        TestWhitelistService whitelist,
        ILoginAttemptStore store,
        FakeAuditService audit,
        int? failureThreshold = null)
    {
        var settings = new Dictionary<string, string?>();
        if (failureThreshold.HasValue)
        {
            settings["RateLimiting:RegisterFailureThreshold"] = failureThreshold.Value.ToString();
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ClientIp);

        var controller = new AuthController(
            new FakeAuthService(),
            whitelist,
            new ActiveSessionStore(),
            store,
            audit,
            NullLogger<AuthController>.Instance,
            configuration)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };

        return controller;
    }

    private static RegistroStep2ViewModel CreateStep2Model() => new()
    {
        Document = "123456",
        WhitelistId = 3,
        FullName = "Test Student",
        GradeName = "Grade 6",
        ContactEmail = "student@example.com",
        Password = "TestPass!123",
        ConfirmPassword = "TestPass!123"
    };

    private static void AssertGenericMessage(AuthController controller, IActionResult actionResult)
    {
        Assert.IsType<ViewResult>(actionResult);
        Assert.Contains(controller.ModelState.Values.SelectMany(entry => entry.Errors), error => error.ErrorMessage == GenericMessage);
    }

    private static DbUpdateException CreateDuplicateKeyDbUpdateException()
    {
        var constructor = typeof(MySqlException)
            .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.GetParameters() is var parameters
                && parameters.Length == 4
                && parameters[0].ParameterType == typeof(MySqlErrorCode));
        var innerException = (MySqlException)constructor.Invoke(new object?[]
        {
            MySqlErrorCode.DuplicateKeyEntry,
            "23000",
            "Duplicate entry",
            null
        });

        return new DbUpdateException("Unique key conflict", innerException);
    }

    private sealed class TestWhitelistService : IWhitelistService
    {
        public int LookupCount { get; private set; }
        public int RegistrationCount { get; private set; }
        public Exception? RegistrationException { get; init; }
        public CensusWhitelist? LookupResult { get; init; }

        public Task<CensusWhitelist?> GetUnclaimedEntryByDocumentHashAsync(string documentHash)
        {
            LookupCount++;
            return Task.FromResult(LookupResult);
        }

        public Task<Voter> RegisterElectorAsync(string document, uint whitelistId, string contactEmail, string password, string ipAddress)
        {
            RegistrationCount++;
            return RegistrationException is null
                ? Task.FromResult(new Voter())
                : Task.FromException<Voter>(RegistrationException);
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}