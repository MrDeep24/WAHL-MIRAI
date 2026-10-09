// ============================================================
// Archivo:       RecuperacionAccesoControllerTests.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias del controlador de recuperación de acceso
//                para endpoints GET/POST Restablecer y POST Recuperar.
// Requerimientos: RF-M01-02, RN-2, RN-9, RN-14, RN-15.
// ============================================================
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias para RecuperacionAccesoController en los flujos de solicitud
/// y restablecimiento de contraseña mediante token efímero de un solo uso (RF-M01-02, RN-15).
/// </summary>
public class RecuperacionAccesoControllerTests
{
    private const uint TestVoterId = 202;
    private const string InitialHash = "$2a$11$InitialPasswordHashMock1234567890123456789012";

    [Fact]
    public void Restablecer_Get_ValidToken_ReturnsViewWithTokenAndSecurityHeaders()
    {
        var (controller, tokenStore, _, _) = CreateController();
        var token = tokenStore.Issue(TestVoterId);

        var result = controller.Restablecer(token);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RestablecerViewModel>(viewResult.Model);
        Assert.False(model.IsInvalidLink);
        Assert.Equal(token, model.Token);

        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        Assert.Equal("no-store", controller.Response.Headers["Cache-Control"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("non-existent-token")]
    public void Restablecer_Get_InvalidOrMissingToken_ReturnsInvalidLinkStateWithSecurityHeaders(string? token)
    {
        var (controller, _, _, _) = CreateController();

        var result = controller.Restablecer(token);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<RestablecerViewModel>(viewResult.Model);
        Assert.True(model.IsInvalidLink);

        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        Assert.Equal("no-store", controller.Response.Headers["Cache-Control"]);
    }

    [Fact]
    public async Task Restablecer_Post_InvalidModelState_DoesNotConsumeToken_AndRerendersView()
    {
        var (controller, tokenStore, _, _) = CreateController();
        var token = tokenStore.Issue(TestVoterId);

        var model = new RestablecerViewModel
        {
            Token = token,
            Password = "weak",
            ConfirmPassword = "weak"
        };
        controller.ModelState.AddModelError("Password", "La contraseña debe tener al menos 8 caracteres.");

        var result = await controller.Restablecer(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        var returnModel = Assert.IsType<RestablecerViewModel>(viewResult.Model);
        Assert.False(returnModel.IsInvalidLink);
        Assert.Equal(token, returnModel.Token);

        // El token NO debe haber sido consumido (RN-15)
        Assert.True(tokenStore.TryPeek(token, out _));

        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        Assert.Equal("no-store", controller.Response.Headers["Cache-Control"]);
    }

    [Fact]
    public async Task Restablecer_Post_ValidModel_Success_ReturnsExitoView_AndConsumesToken()
    {
        var (controller, tokenStore, context, _) = CreateController();
        SeedVoter(context, TestVoterId, "ACTIVO");

        var token = tokenStore.Issue(TestVoterId);
        var model = new RestablecerViewModel
        {
            Token = token,
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!"
        };

        var result = await controller.Restablecer(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Equal("Exito", viewResult.ViewName);
        Assert.Equal("Tu contraseña ha sido actualizada. Ya puedes iniciar sesión con tu nueva contraseña.", controller.ViewBag.SuccessMessage);

        // Token consumido
        Assert.False(tokenStore.TryPeek(token, out _));

        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        Assert.Equal("no-store", controller.Response.Headers["Cache-Control"]);
    }

    [Fact]
    public async Task Restablecer_Post_ReplayToken_ReturnsGenericInvalidLinkState()
    {
        var (controller, tokenStore, context, _) = CreateController();
        SeedVoter(context, TestVoterId, "ACTIVO");

        var token = tokenStore.Issue(TestVoterId);
        var model = new RestablecerViewModel
        {
            Token = token,
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!"
        };

        // Primer intento exitoso
        var firstResult = await controller.Restablecer(model);
        Assert.Equal("Exito", Assert.IsType<ViewResult>(firstResult).ViewName);

        // Segundo intento (replay)
        var replayResult = await controller.Restablecer(model);
        var viewResult = Assert.IsType<ViewResult>(replayResult);
        var returnModel = Assert.IsType<RestablecerViewModel>(viewResult.Model);
        Assert.True(returnModel.IsInvalidLink);

        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        Assert.Equal("no-store", controller.Response.Headers["Cache-Control"]);
    }

    [Fact]
    public async Task Restablecer_Post_InvalidToken_ReturnsGenericInvalidLinkState()
    {
        var (controller, _, _, _) = CreateController();

        var model = new RestablecerViewModel
        {
            Token = "unknown-token-12345",
            Password = "ValidPassword123!",
            ConfirmPassword = "ValidPassword123!"
        };

        var result = await controller.Restablecer(model);

        var viewResult = Assert.IsType<ViewResult>(result);
        var returnModel = Assert.IsType<RestablecerViewModel>(viewResult.Model);
        Assert.True(returnModel.IsInvalidLink);

        Assert.Equal("no-referrer", controller.Response.Headers["Referrer-Policy"]);
        Assert.Equal("no-store", controller.Response.Headers["Cache-Control"]);
    }

    [Fact]
    public async Task Recuperar_Post_BothExistingAndNonExistingUser_ReturnsGenericSuccessMessage()
    {
        var (controller, _, context, _) = CreateController();
        SeedVoter(context, TestVoterId, "ACTIVO", "12345678");

        // 1. Documento existente
        var existingResult = await controller.Recuperar(new RecuperarAccesoViewModel { Documento = "12345678" });
        var existingView = Assert.IsType<ViewResult>(existingResult);
        Assert.Equal("Exito", existingView.ViewName);
        Assert.Equal("Si el documento está registrado, recibirás un enlace de restablecimiento en el correo de contacto asociado en los próximos minutos.", controller.ViewBag.SuccessMessage);

        // 2. Documento no existente (anti-enumeración)
        var nonExistingResult = await controller.Recuperar(new RecuperarAccesoViewModel { Documento = "99999999" });
        var nonExistingView = Assert.IsType<ViewResult>(nonExistingResult);
        Assert.Equal("Exito", nonExistingView.ViewName);
        Assert.Equal("Si el documento está registrado, recibirás un enlace de restablecimiento en el correo de contacto asociado en los próximos minutos.", controller.ViewBag.SuccessMessage);
    }

    private static (RecuperacionAccesoController controller, IPasswordResetTokenStore tokenStore, WahlMiraiDbContext context, ICredentialService credService) CreateController()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new WahlMiraiDbContext(options);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var tokenStore = new PasswordResetTokenStore(memoryCache, TimeProvider.System);
        var passwordStore = new PendingPasswordStore();
        var audit = new FakeAuditService();
        var credService = new CredentialService(context, passwordStore, audit, tokenStore, new FakeLoginAttemptStore(), new ActiveSessionStore());

        var controller = new RecuperacionAccesoController(context, credService, memoryCache, tokenStore)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };

        return (controller, tokenStore, context, credService);
    }

    private static void SeedVoter(WahlMiraiDbContext context, uint id, string status, string plainDocument = "10203040")
    {
        var documentHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plainDocument))).ToLowerInvariant();
        context.Voters.Add(new Voter
        {
            Id = id,
            RoleId = 3,
            DocumentHash = documentHash,
            EncryptedDocument = $"enc-{plainDocument}",
            FullName = $"Voter {id}",
            ContactEmail = $"voter{id}@example.com",
            PasswordHash = InitialHash,
            Status = status,
            RegisteredAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }
}
