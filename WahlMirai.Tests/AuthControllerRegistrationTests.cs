// ============================================================
// Archivo:       AuthControllerRegistrationTests.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Verifica retroalimentación explícita de auto-registro,
//                límites por IP, enmascaramiento parcial de identidad en verificación y finalización de registro.
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

/// <summary>
/// Pruebas de auto-registro con retroalimentación explícita,
/// contador por IP, cooldown y enmascaramiento parcial (RF-M01-00, RN-1, RN-1.1, RN-15).
/// </summary>
public sealed class AuthControllerRegistrationTests
{
    private const string ClientIp = "192.0.2.30";
    private const string GenericMessage = "Demasiados intentos fallidos desde esta conexión. Espera unos minutos e inténtalo de nuevo, o contacta al Administrador institucional.";

    /// <summary>Los 29 primeros lookups fallidos devuelven NotFound sin bloquear el IP ni auditar (RN-15).</summary>
    [Fact]
    public async Task Registro_FailuresBelowDefaultThreshold_DoNotLockOrAuditEachFailure()
    {
        var whitelist = new TestWhitelistService { LookupResult = null };
        var audit = new FakeAuditService();
        var store = new LoginAttemptStore(TimeProvider.System);
        var controller = CreateController(whitelist, store, audit);

        for (var attempt = 0; attempt < 29; attempt++)
        {
            controller.ModelState.Clear();
            var result = await controller.Registro(new RegistroStep1ViewModel { Document = $"12345{attempt}" });
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RegistroStep1ViewModel>(viewResult.Model);
            Assert.Equal(RegistroFailureReason.NotFound, model.FailureReason);
        }

        Assert.Equal(29, whitelist.LookupCount);
        Assert.False(store.IsLockedOut($"register:{ClientIp}", out _));
        Assert.DoesNotContain("REGISTRO_LOCKED_OUT", audit.Actions);
    }

    /// <summary>El fallo 30 activa un único evento; los POST posteriores no consultan la whitelist y muestran vista de error (RN-1, RN-15).</summary>
    [Fact]
    public async Task Registro_ThresholdLocksIpOnce_AndBothEndpointsSkipWhitelistWhileLocked()
    {
        var whitelist = new TestWhitelistService { LookupResult = null };
        var audit = new FakeAuditService();
        var store = new LoginAttemptStore(TimeProvider.System);
        var controller = CreateController(whitelist, store, audit);

        for (var attempt = 0; attempt < 30; attempt++)
        {
            controller.ModelState.Clear();
            var result = await controller.Registro(new RegistroStep1ViewModel { Document = $"12345{attempt}" });
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<RegistroStep1ViewModel>(viewResult.Model);
            Assert.Equal(RegistroFailureReason.NotFound, model.FailureReason);
        }

        Assert.Equal(30, whitelist.LookupCount);
        Assert.Equal(new[] { "REGISTRO_LOCKED_OUT" }, audit.Actions);
        Assert.Equal(ClientIp, audit.IpAddresses.Single());

        controller.ModelState.Clear();
        var lockedLookup = await controller.Registro(new RegistroStep1ViewModel { Document = "123456" });
        AssertGenericMessage(controller, lockedLookup);
        var lockedModel = Assert.IsType<RegistroStep1ViewModel>(Assert.IsType<ViewResult>(lockedLookup).Model);
        Assert.Equal(RegistroFailureReason.Locked, lockedModel.FailureReason);

        controller.ModelState.Clear();
        var lockedComplete = await controller.Completar(CreateStep2Model());
        AssertGenericMessage(controller, lockedComplete);

        Assert.Equal(30, whitelist.LookupCount);
        Assert.Equal(0, whitelist.RegistrationCount);
        Assert.Single(audit.Actions);
    }

    /// <summary>Documento no encontrado retorna View con FailureReason.NotFound, sin cookie y computa en el bucket IP (RF-M01-00, RN-15).</summary>
    [Fact]
    public async Task Registro_MissingDocument_ReturnsViewWithNotFoundReason_AndNoCookie()
    {
        var whitelist = new TestWhitelistService { LookupResult = null };
        var store = new LoginAttemptStore(TimeProvider.System);
        var controller = CreateController(whitelist, store, new FakeAuditService(), failureThreshold: 1);

        var result = await controller.Registro(new RegistroStep1ViewModel { Document = "999999" });

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RegistroStep1ViewModel>(viewResult.Model);
        Assert.Equal(RegistroFailureReason.NotFound, model.FailureReason);
        Assert.False(controller.Response.Headers.ContainsKey("Set-Cookie"));
        // Con threshold = 1, el fallo contabilizado bloquea el bucket IP
        Assert.True(store.IsLockedOut($"register:{ClientIp}", out _));
    }

    /// <summary>Documento con correo nulo/vacío retorna View con FailureReason.NoContactEmail, sin cookie e incrementa el bucket IP.</summary>
    [Fact]
    public async Task Registro_NullOrEmptyContactEmail_ReturnsViewWithNoContactEmailReason_AndNoCookie()
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
        var controller = CreateController(whitelist, store, new FakeAuditService(), failureThreshold: 1);

        var result = await controller.Registro(new RegistroStep1ViewModel { Document = "101010" });

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RegistroStep1ViewModel>(viewResult.Model);
        Assert.Equal(RegistroFailureReason.NoContactEmail, model.FailureReason);
        Assert.False(controller.Response.Headers.ContainsKey("Set-Cookie"));
        Assert.True(store.IsLockedOut($"register:{ClientIp}", out _));
    }

    /// <summary>Documento ya reclamado retorna View con FailureReason.AlreadyClaimed, sin cookie e incrementa el bucket IP (RF-M01-00, RN-15).</summary>
    [Fact]
    public async Task Registro_AlreadyClaimedDocument_ReturnsViewWithAlreadyClaimedReason_AndNoCookie()
    {
        var whitelist = new TestWhitelistService
        {
            LookupResult = new CensusWhitelist
            {
                Id = 15,
                FullName = "Estudiante Ya Reclamado",
                ContactEmail = "claimed@example.com",
                ClaimedAt = DateTime.UtcNow.AddDays(-1),
                Grade = new Grade { Name = "11-A" }
            }
        };
        var store = new LoginAttemptStore(TimeProvider.System);
        var controller = CreateController(whitelist, store, new FakeAuditService(), failureThreshold: 1);

        var result = await controller.Registro(new RegistroStep1ViewModel { Document = "151515" });

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RegistroStep1ViewModel>(viewResult.Model);
        Assert.Equal(RegistroFailureReason.AlreadyClaimed, model.FailureReason);
        Assert.False(controller.Response.Headers.ContainsKey("Set-Cookie"));
        Assert.True(store.IsLockedOut($"register:{ClientIp}", out _));
    }

    /// <summary>Documento válido sin reclamar emite ticket, despacha email y redirige 302 a VerificarCodigo con cookie.</summary>
    [Fact]
    public async Task Registro_ValidDocument_IssuesTicket_SendsEmail_AndRedirects302ToVerificarCodigo()
    {
        var whitelist = new TestWhitelistService
        {
            LookupResult = new CensusWhitelist
            {
                Id = 20,
                FullName = "Carlos Pérez",
                ContactEmail = "carlos@example.com",
                ClaimedAt = null,
                Grade = new Grade { Name = "9-A" }
            }
        };
        var store = new LoginAttemptStore(TimeProvider.System);
        var fakeEmailSender = new FakeEmailSender();
        var controller = CreateController(whitelist, store, new FakeAuditService(), emailSender: fakeEmailSender);

        var result = await controller.Registro(new RegistroStep1ViewModel { Document = "202020" });

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.VerificarCodigo), redirect.ActionName);
        Assert.Contains(AuthController.RegistrationTicketCookieName, controller.Response.Headers.SetCookie.ToString());
        Assert.Single(fakeEmailSender.SentMessages);
        Assert.Equal("carlos@example.com", fakeEmailSender.SentMessages[0].Recipient);
        Assert.False(store.IsLockedOut($"register:{ClientIp}", out _));
    }

    /// <summary>Cooldown activo para entrada censal sin cookie válida previa retorna View con FailureReason.Cooldown y no computa en bucket IP.</summary>
    [Fact]
    public async Task Registro_CooldownActive_WithoutValidSession_ReturnsViewWithCooldownReason_DoesNotCountInBucket()
    {
        var whitelist = new TestWhitelistService
        {
            LookupResult = new CensusWhitelist
            {
                Id = 30,
                FullName = "Ana Gómez",
                ContactEmail = "ana@example.com",
                ClaimedAt = null,
                Grade = new Grade { Name = "10-A" }
            }
        };
        var store = new LoginAttemptStore(TimeProvider.System);
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var controller = CreateController(whitelist, store, new FakeAuditService(), failureThreshold: 1, otpStore: otpStore);

        // Emitir ticket inicial directamente en otpStore
        var firstTicket = otpStore.IssueRegistrationTicket("303030", 30, "ana@example.com", "hash30");
        Assert.False(firstTicket.IsCooldownActive);

        // Segundo intento de registro inmediato sin presentar la cookie del ticket
        var result = await controller.Registro(new RegistroStep1ViewModel { Document = "303030" });

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RegistroStep1ViewModel>(viewResult.Model);
        Assert.Equal(RegistroFailureReason.Cooldown, model.FailureReason);
        Assert.True(model.CooldownRemainingSeconds > 0 && model.CooldownRemainingSeconds <= 60);
        // No debe contar en bucket IP (threshold es 1, no debe estar bloqueado)
        Assert.False(store.IsLockedOut($"register:{ClientIp}", out _));
        // El ticket original no debe haber sido revocado
        Assert.True(otpStore.TryValidateOtp(firstTicket.Ticket, firstTicket.RawOtp!, out _, out _));
    }

    /// <summary>Cooldown activo para entrada censal con cookie válida asociada a la misma entrada redirige 302 a VerificarCodigo.</summary>
    [Fact]
    public async Task Registro_CooldownActive_WithValidSession_Redirects302ToVerificarCodigo()
    {
        var whitelist = new TestWhitelistService
        {
            LookupResult = new CensusWhitelist
            {
                Id = 35,
                FullName = "Laura Díaz",
                ContactEmail = "laura@example.com",
                ClaimedAt = null,
                Grade = new Grade { Name = "10-A" }
            }
        };
        var store = new LoginAttemptStore(TimeProvider.System);
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var controller = CreateController(whitelist, store, new FakeAuditService(), otpStore: otpStore);

        // Primer intento normal
        var firstResult = await controller.Registro(new RegistroStep1ViewModel { Document = "353535" });
        Assert.IsType<RedirectToActionResult>(firstResult);

        // Obtener cookie generada y asignarla al request
        var setCookie = controller.Response.Headers.SetCookie.ToString();
        var cookieValue = setCookie.Split(';')[0].Split('=')[1];
        controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"{AuthController.RegistrationTicketCookieName}={cookieValue}";

        // Segundo intento dentro del cooldown con la cookie asignada
        controller.ModelState.Clear();
        var secondResult = await controller.Registro(new RegistroStep1ViewModel { Document = "353535" });

        var redirect = Assert.IsType<RedirectToActionResult>(secondResult);
        Assert.Equal(nameof(AuthController.VerificarCodigo), redirect.ActionName);
    }

    /// <summary>Falla SMTP al emitir OTP descarta ticket, no deja cookie, libera cooldown y no computa en bucket IP.</summary>
    [Fact]
    public async Task Registro_SmtpFailure_RevokesTicket_LeavesNoCookie_FreesCooldown_AndDoesNotCountInBucket()
    {
        var whitelist = new TestWhitelistService
        {
            LookupResult = new CensusWhitelist
            {
                Id = 40,
                FullName = "Mario Bros",
                ContactEmail = "mario@example.com",
                ClaimedAt = null,
                Grade = new Grade { Name = "11-B" }
            }
        };
        var store = new LoginAttemptStore(TimeProvider.System);
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var failingEmailSender = new FakeEmailSender { ShouldFail = true };
        var controller = CreateController(whitelist, store, new FakeAuditService(), failureThreshold: 1, otpStore: otpStore, emailSender: failingEmailSender);

        var result = await controller.Registro(new RegistroStep1ViewModel { Document = "404040" });

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RegistroStep1ViewModel>(viewResult.Model);
        Assert.Equal(RegistroFailureReason.SendFailed, model.FailureReason);
        Assert.False(controller.Response.Headers.ContainsKey("Set-Cookie"));
        // No cuenta en bucket IP (threshold 1 no debe estar bloqueado)
        Assert.False(store.IsLockedOut($"register:{ClientIp}", out _));

        // Debe haber liberado el cooldown de la entrada censal para que el usuario pueda reintentar de inmediato
        var retryTicket = otpStore.IssueRegistrationTicket("404040", 40, "mario@example.com", "hash40");
        Assert.False(retryTicket.IsCooldownActive);
    }

    /// <summary>VerificarCodigo GET con ticket válido expone nombre y correo parcialmente enmascarados en el ViewModel.</summary>
    [Fact]
    public void VerificarCodigo_Get_WithValidTicket_ExposesMaskedNameAndEmailInViewModel()
    {
        var otpStore = new RegistrationOtpStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var ticket = otpStore.IssueRegistrationTicket("123456", 50, "kevin.lopez@example.com", "hash", fullName: "Kevin López");

        var controller = CreateController(new TestWhitelistService(), new LoginAttemptStore(TimeProvider.System), new FakeAuditService(), otpStore: otpStore);
        controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"{AuthController.RegistrationTicketCookieName}={ticket.Ticket}";

        var result = controller.VerificarCodigo();

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<VerificarCodigoViewModel>(viewResult.Model);
        Assert.Equal("Ke*** Ló***", model.MaskedFullName);
        Assert.Equal("k***@e***.com", model.MaskedEmail);
    }

    /// <summary>VerificarCodigo GET sin cookie o con ticket expirado redirige 302 a Registro.</summary>
    [Fact]
    public void VerificarCodigo_Get_WithoutCookieOrExpiredTicket_RedirectsToRegistro()
    {
        var controller = CreateController(new TestWhitelistService(), new LoginAttemptStore(TimeProvider.System), new FakeAuditService());

        // Sin cookie
        var result1 = controller.VerificarCodigo();
        var redirect1 = Assert.IsType<RedirectToActionResult>(result1);
        Assert.Equal(nameof(AuthController.Registro), redirect1.ActionName);

        // Con ticket inexistente
        controller.ControllerContext.HttpContext.Request.Headers.Cookie = $"{AuthController.RegistrationTicketCookieName}=invalid_ticket";
        var result2 = controller.VerificarCodigo();
        var redirect2 = Assert.IsType<RedirectToActionResult>(result2);
        Assert.Equal(nameof(AuthController.Registro), redirect2.ActionName);
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

    /// <summary>Completar GET sin ticket verificado redirige a Registro (D1).</summary>
    [Fact]
    public async Task Completar_Get_WithoutVerifiedTicket_RedirectsToRegistro()
    {
        var controller = CreateController(new TestWhitelistService(), new LoginAttemptStore(TimeProvider.System), new FakeAuditService());

        var result = await controller.Completar();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AuthController.Registro), redirect.ActionName);
    }

    /// <summary>Completar GET con ticket verificado carga FullName y GradeName desde whitelist (D1).</summary>
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
        IRegistrationOtpStore? otpStore = null,
        IEmailSender? emailSender = null)
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
            actualOtpStore,
            emailSender)
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

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool ShouldFail { get; init; }
        public List<(string Recipient, string Subject, string Body)> SentMessages { get; } = new();

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (ShouldFail)
            {
                throw new InvalidOperationException("SMTP connection failed");
            }

            SentMessages.Add((toEmail, subject, htmlBody));
            return Task.CompletedTask;
        }
    }

    private sealed class TestWhitelistService : IWhitelistService
    {
        public int LookupCount { get; private set; }
        public int RegistrationCount { get; private set; }
        public Exception? RegistrationException { get; init; }
        public CensusWhitelist? LookupResult { get; set; }

        public Task<CensusWhitelist?> GetEntryByDocumentHashAsync(string documentHash)
        {
            LookupCount++;
            return Task.FromResult(LookupResult);
        }

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