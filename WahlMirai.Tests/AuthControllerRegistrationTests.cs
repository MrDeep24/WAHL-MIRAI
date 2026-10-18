// ============================================================
// Archivo:       AuthControllerRegistrationTests.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Verifica límites por IP, emisión de tickets decoy,
//                respuestas indistinguibles (A2), verificación previa (D1),
//                y conteo de fallos OTP en el bucket IP (D4).
// Requerimientos: RF-M01-00, RN-1, RN-1.1, RN-15.
// ============================================================
using System.Net;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Tests;

/// <summary>Pruebas del contador por IP, tickets señuelo y respuestas indistinguibles (RN-1, RN-1.1, RN-15, A1-A3, D1-D4).</summary>
public sealed class AuthControllerRegistrationTests
{
    private const string ClientIp = "192.0.2.30";
    private const string GenericMessage = "No fue posible procesar el auto-registro con el documento ingresado. Si consideras que esto es un error o requieres asistencia, por favor contacta al Administrador institucional.";

    /// <summary>Los 29 primeros lookups fallidos redirigen a verificación (A2) sin bloquear el IP ni auditar (RN-15).</summary>
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
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AuthController.VerificarCodigo), redirect.ActionName);
        }

        Assert.Equal(29, whitelist.LookupCount);
        Assert.False(store.IsLockedOut($"register:{ClientIp}", out _));
        Assert.DoesNotContain("REGISTRO_LOCKED_OUT", audit.Actions);
    }

    /// <summary>El fallo 30 activa un único evento; los POST posteriores no consultan la whitelist y muestran vista de error (RN-1, RN-15).</summary>
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
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AuthController.VerificarCodigo), redirect.ActionName);
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

    /// <summary>Documento no encontrado emite ticket decoy, redirige 302 a VerificarCodigo e incrementa bucket IP (A2).</summary>
    [Fact]
    public async Task Registro_MissingDocument_IssuesDecoyTicketAndRedirects302()
    {
        var whitelist = new TestWhitelistService { LookupResult = null };
        var store = new LoginAttemptStore(TimeProvider.System);
        var controller = CreateController(whitelist, store, new FakeAuditService());

        var result = await controller.Registro(new RegistroStep1ViewModel { Document = "999999" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.VerificarCodigo), redirect.ActionName);
        Assert.Contains(AuthController.RegistrationTicketCookieName, controller.Response.Headers.SetCookie.ToString());
    }

    /// <summary>Documento con correo nulo/vacío emite ticket decoy, redirige 302 y no expone datos (A2, D3).</summary>
    [Fact]
    public async Task Registro_NullOrEmptyContactEmail_IssuesDecoyTicketAndRedirects302()
    {
        var whitelist = new TestWhitelistService
        {
            LookupResult = new CensusWhitelist
            {
                Id = 10,
                FullName = "Secret Student",
                ContactEmail = null,
                Grade = new Grade { Name = "10-B" }
            }
        };
        var store = new LoginAttemptStore(TimeProvider.System);
        var controller = CreateController(whitelist, store, new FakeAuditService());

        var result = await controller.Registro(new RegistroStep1ViewModel { Document = "101010" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.VerificarCodigo), redirect.ActionName);
        Assert.Contains(AuthController.RegistrationTicketCookieName, controller.Response.Headers.SetCookie.ToString());
    }

    /// <summary>Fallo al ingresar código OTP computa en el bucket IP register:{clientIpKey} (D4).</summary>
    [Fact]
    public async Task VerificarCodigo_OtpFailure_IncrementsRegistrationBucket()
    {
        var store = new LoginAttemptStore(TimeProvider.System);
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var controller = CreateController(new TestWhitelistService(), store, new FakeAuditService(), failureThreshold: 3, otpStore: otpStore);

        // Emitir ticket válido pero ingresar código incorrecto
        var ticketResult = otpStore.IssueRegistrationTicket("123456", 1, "test@example.com", "hash");
        controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"{AuthController.RegistrationTicketCookieName}={ticketResult.Ticket}";

        for (var i = 0; i < 2; i++)
        {
            controller.ModelState.Clear();
            var res = await controller.VerificarCodigo(new VerificarCodigoViewModel { OtpCode = "000000" });
            Assert.IsType<ViewResult>(res);
        }

        Assert.False(store.IsLockedOut($"register:{ClientIp}", out _));

        // 3er intento activa bloqueo en bucket IP
        controller.ModelState.Clear();
        await controller.VerificarCodigo(new VerificarCodigoViewModel { OtpCode = "000000" });
        Assert.True(store.IsLockedOut($"register:{ClientIp}", out _));
    }

    /// <summary>Completar GET sin ticket verificado redirige a Registro (D1, A2).</summary>
    [Fact]
    public async Task Completar_Get_WithoutVerifiedTicket_RedirectsToRegistro()
    {
        var controller = CreateController(new TestWhitelistService(), new LoginAttemptStore(TimeProvider.System), new FakeAuditService());

        var result = await controller.Completar();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.Registro), redirect.ActionName);
    }

    /// <summary>Completar GET con ticket verificado carga FullName y GradeName desde whitelist (D1, A3).</summary>
    [Fact]
    public async Task Completar_Get_WithVerifiedTicket_PopulatesIdentityFromWhitelist()
    {
        var whitelist = new TestWhitelistService
        {
            LookupResult = new CensusWhitelist
            {
                Id = 42,
                FullName = "Estudiante Seguro",
                Grade = new Grade { Name = "11-A" }
            }
        };
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var controller = CreateController(whitelist, new LoginAttemptStore(TimeProvider.System), new FakeAuditService(), otpStore: otpStore);

        SetVerifiedTicket(controller, otpStore, whitelistId: 42);

        var result = await controller.Completar();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RegistroStep2ViewModel>(viewResult.Model);
        Assert.Equal("Estudiante Seguro", model.FullName);
        Assert.Equal("11-A", model.GradeName);
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
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var controller = CreateController(whitelist, store, new FakeAuditService(), failureThreshold: 2, otpStore: otpStore);

        SetVerifiedTicket(controller, otpStore);
        controller.ModelState.Clear();
        AssertGenericMessage(controller, await controller.Completar(CreateStep2Model()));
        Assert.False(store.IsLockedOut($"register:{ClientIp}", out _));

        SetVerifiedTicket(controller, otpStore);
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
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var controller = CreateController(whitelist, new LoginAttemptStore(TimeProvider.System), new FakeAuditService(), otpStore: otpStore);

        SetVerifiedTicket(controller, otpStore);
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
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var controller = CreateController(whitelist, new LoginAttemptStore(TimeProvider.System), new FakeAuditService(), otpStore: otpStore);

        SetVerifiedTicket(controller, otpStore);
        var thrown = await Assert.ThrowsAsync<DbUpdateException>(() => controller.Completar(CreateStep2Model()));

        Assert.Same(databaseException, thrown);
    }

    /// <summary>Completar sin aceptar términos y privacidad rechaza el registro con error de validación (RN-16, Ley 1581).</summary>
    [Fact]
    public async Task Completar_MissingConsentCheckbox_ReturnsViewWithValidationError()
    {
        var whitelist = new TestWhitelistService
        {
            LookupResult = new CensusWhitelist
            {
                Id = 3,
                FullName = "Test Student",
                ContactEmail = "student@example.com",
                Grade = new Grade { Name = "Grade 6" }
            }
        };
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var audit = new FakeAuditService();
        var controller = CreateController(whitelist, new LoginAttemptStore(TimeProvider.System), audit, otpStore: otpStore);

        SetVerifiedTicket(controller, otpStore);

        var model = CreateStep2Model();
        model.AcceptTermsAndPrivacy = false;

        var result = await controller.Completar(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        var returnModel = Assert.IsType<RegistroStep2ViewModel>(viewResult.Model);
        Assert.False(returnModel.AcceptTermsAndPrivacy);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState.Values.SelectMany(v => v.Errors),
            e => e.ErrorMessage.Contains("Términos de Uso y la Política de Privacidad"));
        Assert.Equal(0, whitelist.RegistrationCount);
        Assert.DoesNotContain("CONSENT_ACCEPTED", audit.Actions);
    }

    /// <summary>Completar con consentimiento registra CONSENT_ACCEPTED con versión y base legal en auditoría (RN-16).</summary>
    [Fact]
    public async Task Completar_WithConsentCheckbox_LogsConsentAcceptedAuditEntry()
    {
        var whitelist = new TestWhitelistService
        {
            LookupResult = new CensusWhitelist
            {
                Id = 3,
                FullName = "Test Student",
                ContactEmail = "student@example.com",
                Grade = new Grade { Name = "Grade 6" }
            }
        };
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var audit = new FakeAuditService();
        var controller = CreateController(whitelist, new LoginAttemptStore(TimeProvider.System), audit, otpStore: otpStore);

        SetVerifiedTicket(controller, otpStore);

        var model = CreateStep2Model();
        model.AcceptTermsAndPrivacy = true;

        var result = await controller.Completar(model);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Dashboard", redirect.ActionName);
        Assert.Equal("Elector", redirect.ControllerName);
        Assert.Equal(1, whitelist.RegistrationCount);

        Assert.Contains("CONSENT_ACCEPTED", audit.Actions);
        var consentIndex = audit.Actions.IndexOf("CONSENT_ACCEPTED");
        var details = audit.Details[consentIndex];
        Assert.NotNull(details);
        Assert.Contains(LegalDocuments.Version, details);
        Assert.Contains("Ley 1581 de 2012", details);
    }

    private static AuthController CreateController(
        TestWhitelistService whitelist,
        ILoginAttemptStore store,
        FakeAuditService audit,
        int? failureThreshold = null,
        IRegistrationOtpStore? otpStore = null)
    {
        var settings = new Dictionary<string, string?>();
        if (failureThreshold.HasValue)
        {
            settings["RateLimiting:RegisterFailureThreshold"] = failureThreshold.Value.ToString();
        }

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ClientIp);

        var services = new ServiceCollection();
        services.AddSingleton<IAuthenticationService, FakeAuthenticationService>();
        services.AddSingleton<IUrlHelperFactory, FakeUrlHelperFactory>();
        httpContext.RequestServices = services.BuildServiceProvider();

        var actualOtpStore = otpStore ?? new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);

        var controller = new AuthController(
            new FakeAuthService(),
            whitelist,
            new ActiveSessionStore(),
            store,
            audit,
            NullLogger<AuthController>.Instance,
            configuration,
            actualOtpStore)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };

        return controller;
    }

    private static void SetVerifiedTicket(AuthController controller, IRegistrationOtpStore store, uint whitelistId = 3, string document = "123456", string email = "student@example.com")
    {
        var ticket = store.IssueRegistrationTicket(document, whitelistId, email, "hash");
        store.TryValidateOtp(ticket.Ticket, ticket.RawOtp!, out _, out _);
        controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"{AuthController.RegistrationTicketCookieName}={ticket.Ticket}";
    }

    private static RegistroStep2ViewModel CreateStep2Model() => new()
    {
        Document = "123456",
        WhitelistId = 3,
        FullName = "Test Student",
        GradeName = "Grade 6",
        ContactEmail = "student@example.com",
        Password = "TestPass!123",
        ConfirmPassword = "TestPass!123",
        AcceptTermsAndPrivacy = true
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

        public Task<CensusWhitelist?> GetUnclaimedEntryByIdAsync(uint whitelistId)
        {
            return Task.FromResult(LookupResult);
        }

        public Task<Voter> RegisterElectorAsync(string document, uint whitelistId, string contactEmail, string password, string ipAddress)
        {
            RegistrationCount++;
            return RegistrationException is null
                ? Task.FromResult(new Voter { Id = 1, FullName = "Test Student", Role = new Role { Id = 3, Name = "ELECTOR" } })
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

    private sealed class FakeAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) =>
            Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties) =>
            Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) =>
            Task.CompletedTask;
    }

    private sealed class FakeUrlHelperFactory : IUrlHelperFactory
    {
        public IUrlHelper GetUrlHelper(ActionContext context) => new FakeUrlHelper();
    }

    private sealed class FakeUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => new();
        public string? Action(UrlActionContext actionContext) => actionContext.Action;
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url) => true;
        public string? Link(string? routeName, object? values) => "/" + routeName;
        public string? RouteUrl(UrlRouteContext routeContext) => "/" + routeContext.RouteName;
    }
}