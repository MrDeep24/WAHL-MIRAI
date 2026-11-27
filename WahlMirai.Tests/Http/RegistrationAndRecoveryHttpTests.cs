// ============================================================
// Archivo:       RegistrationAndRecoveryHttpTests.cs
// Módulo:        M01 — Auto-registro OTP y Recuperación de Acceso (Pruebas HTTP)
// Desarrollador: Kevin
// Descripción:   Pruebas HTTP integrales del flujo de auto-registro contra lista blanca
//                (cookie wm_reg_ticket y banderas, cooldown 60s con/sin cookie, fallo SMTP,
//                enmascaramiento de identidad, consentimiento RN-16) y recuperación de acceso
//                RF-M01-02 (TryPeek/TryConsume, cabeceras de seguridad, revocación RN-14,
//                limpieza de lockout RN-15 y antiforgery).
// Requerimientos: RF-M01-00, RF-M01-02, RN-1, RN-1.1, RN-2, RN-9, RN-14, RN-15, RN-16.
// ============================================================
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests.Http;

/// <summary>
/// Pruebas HTTP end-to-end para el auto-registro protegido por OTP y lista blanca (RF-M01-00, RN-16)
/// y para la recuperación de acceso mediante token efímero (RF-M01-02, RN-14, RN-15).
/// </summary>
[Collection(HttpPipelineCollection.CollectionName)]
public sealed class RegistrationAndRecoveryHttpTests
{
    private const string WhitelistDoc = "1050607080";
    private const string RecoveryDoc = "2050607080";
    private const string InitialPassword = "InitPass!2026";

    /// <summary>
    /// RF-M01-00: La cookie <c>wm_reg_ticket</c> emitida por <c>POST /Auth/Registro</c>
    /// incluye <c>HttpOnly</c>, <c>SameSite=Strict</c>, <c>MaxAge=900s</c> (15 min) y establece
    /// <c>Secure</c> únicamente cuando la petición entra por HTTPS.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Registro_TicketCookieFlags_HttpOnlySameSiteStrictMaxAge900_SecureOnlyOverHttps(bool useHttps)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedWhitelistEntryAsync(factory, id: 10, document: WhitelistDoc, fullName: "Valentina Rodriguez", email: "valentina@colegio.edu.co");

        using var client = factory.CreateNoRedirectClient(useHttps: useHttps);
        var response = await PostRegistroAsync(client, WhitelistDoc);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Auth/VerificarCodigo", response.Headers.Location?.OriginalString);

        var setCookies = SetCookieHeaderValue.ParseList(response.Headers.GetValues("Set-Cookie").ToList());
        var ticketCookie = Assert.Single(setCookies, c =>
            string.Equals(c.Name.Value, AuthController.RegistrationTicketCookieName, StringComparison.Ordinal));

        Assert.False(string.IsNullOrWhiteSpace(ticketCookie.Value.Value));
        Assert.True(ticketCookie.HttpOnly);
        Assert.Equal(SameSiteMode.Strict, ticketCookie.SameSite);
        Assert.Equal(TimeSpan.FromMinutes(15), ticketCookie.MaxAge);
        Assert.Equal(useHttps, ticketCookie.Secure);
    }

    /// <summary>
    /// RF-M01-00, RN-15: El cooldown de 60 s coordina las reemisiones por entrada censal:
    /// con la cookie válida del ticket redirige a <c>VerificarCodigo</c> sin reenviar correo;
    /// sin cookie bloquea durante 59 s y permite reemitir al superar los 60 s vía <see cref="FakeTimeProvider"/>.
    /// </summary>
    [Fact]
    public async Task Registro_Cooldown60Seconds_WithAndWithoutCookie_ControlledByFakeTimeProvider()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedWhitelistEntryAsync(factory, id: 11, document: WhitelistDoc, fullName: "Carlos Mendoza", email: "carlos@colegio.edu.co");

        using var clientWithCookie = factory.CreateNoRedirectClient(useHttps: true);
        using var clientWithoutCookie = factory.CreateNoRedirectClient(useHttps: true);

        // 1. Emisión inicial con clientWithCookie.
        var initialRes = await PostRegistroAsync(clientWithCookie, WhitelistDoc);
        Assert.Equal(HttpStatusCode.Redirect, initialRes.StatusCode);
        Assert.Equal("/Auth/VerificarCodigo", initialRes.Headers.Location?.OriginalString);
        Assert.Single(factory.EmailSender.SentEmails);

        // 2. Reintento inmediato CON cookie válida -> redirige a VerificarCodigo sin enviar otro correo.
        var retryWithCookie = await PostRegistroAsync(clientWithCookie, WhitelistDoc);
        Assert.Equal(HttpStatusCode.Redirect, retryWithCookie.StatusCode);
        Assert.Equal("/Auth/VerificarCodigo", retryWithCookie.Headers.Location?.OriginalString);
        Assert.Single(factory.EmailSender.SentEmails);

        // 3. Reintento inmediato SIN cookie -> retorna vista con mensaje de cooldown y sin cookie wm_reg_ticket.
        var retryNoCookie = await PostRegistroAsync(clientWithoutCookie, WhitelistDoc);
        Assert.Equal(HttpStatusCode.OK, retryNoCookie.StatusCode);
        var cooldownHtml = WebUtility.HtmlDecode(await retryNoCookie.Content.ReadAsStringAsync());
        Assert.Contains("Debes esperar", cooldownHtml);
        if (retryNoCookie.Headers.TryGetValues("Set-Cookie", out var noCookieSetHeaders))
        {
            var parsedNoCookie = SetCookieHeaderValue.ParseList(noCookieSetHeaders.ToList());
            Assert.DoesNotContain(parsedNoCookie, c =>
                string.Equals(c.Name.Value, AuthController.RegistrationTicketCookieName, StringComparison.Ordinal));
        }
        Assert.Single(factory.EmailSender.SentEmails);

        // 4. Avanzar 59 segundos (< 60 s): aún permanece en cooldown.
        clock.Advance(TimeSpan.FromSeconds(59));
        var stillCooldown = await PostRegistroAsync(clientWithoutCookie, WhitelistDoc);
        Assert.Equal(HttpStatusCode.OK, stillCooldown.StatusCode);
        Assert.Contains("Debes esperar", await stillCooldown.Content.ReadAsStringAsync());
        Assert.Single(factory.EmailSender.SentEmails);

        // 5. Avanzar 2 segundos más (total 61 s >= 60 s): permite emitir nuevo ticket y enviar correo.
        clock.Advance(TimeSpan.FromSeconds(2));
        var afterCooldown = await PostRegistroAsync(clientWithoutCookie, WhitelistDoc);
        Assert.Equal(HttpStatusCode.Redirect, afterCooldown.StatusCode);
        Assert.Equal("/Auth/VerificarCodigo", afterCooldown.Headers.Location?.OriginalString);
        Assert.Equal(2, factory.EmailSender.SentEmails.Count);
    }

    /// <summary>
    /// RF-M01-00, RN-15: Si falla el envío SMTP al emitir OTP, no se entrega cookie <c>wm_reg_ticket</c>,
    /// se libera el cooldown de la entrada censal y NO se incrementa el bucket IP <c>register:{clientIpKey}</c>.
    /// </summary>
    [Fact]
    public async Task Registro_SmtpFailure_ReturnsNoCookie_ReleasesCooldown_AndDoesNotIncrementIpBucket()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 13, 0, 0, TimeSpan.Zero));
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:RegisterFailureThreshold"] = "1"
        };
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock, configOverrides: overrides);
        await SeedWhitelistEntryAsync(factory, id: 12, document: WhitelistDoc, fullName: "Lucia Torres", email: "lucia@colegio.edu.co");

        factory.EmailSender.ShouldFail = true;
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        const string ip = "198.51.100.44";
        var failedSmtpRes = await PostRegistroAsync(client, WhitelistDoc, ip);

        Assert.Equal(HttpStatusCode.OK, failedSmtpRes.StatusCode);
        var html = await failedSmtpRes.Content.ReadAsStringAsync();
        Assert.Contains("No fue posible enviar el código de verificación", html);

        if (failedSmtpRes.Headers.TryGetValues("Set-Cookie", out var rawSetCookies))
        {
            var parsedCookies = SetCookieHeaderValue.ParseList(rawSetCookies.ToList());
            Assert.DoesNotContain(parsedCookies, c =>
                string.Equals(c.Name.Value, AuthController.RegistrationTicketCookieName, StringComparison.Ordinal));
        }

        // Recuperado el SMTP, el mismo cliente puede reintentar de inmediato (0 s avanzados):
        // prueba que el cooldown fue liberado y que el bucket IP (threshold=1) NO fue incrementado.
        factory.EmailSender.ShouldFail = false;
        var immediateRetry = await PostRegistroAsync(client, WhitelistDoc, ip);

        Assert.Equal(HttpStatusCode.Redirect, immediateRetry.StatusCode);
        Assert.Equal("/Auth/VerificarCodigo", immediateRetry.Headers.Location?.OriginalString);
        Assert.Single(factory.EmailSender.SentEmails);
    }

    /// <summary>
    /// RF-M01-00: En <c>VerificarCodigo</c> solo se expone el nombre y correo parcialmente enmascarados
    /// (sin nombre completo, correo real ni grado); el nombre completo y el grado se muestran únicamente en <c>Completar</c>.
    /// </summary>
    [Fact]
    public async Task Registro_IdentityMasking_MaskedAtVerificarCodigo_FullNameAndGradeOnlyAtCompletar()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedWhitelistEntryAsync(
            factory,
            id: 13,
            document: WhitelistDoc,
            fullName: "Valentina Rodriguez",
            email: "valentina.rodriguez@colegio.edu.co",
            gradeName: "10-A");

        using var client = factory.CreateNoRedirectClient(useHttps: true);

        var step1Res = await PostRegistroAsync(client, WhitelistDoc);
        Assert.Equal(HttpStatusCode.Redirect, step1Res.StatusCode);
        Assert.Equal("/Auth/VerificarCodigo", step1Res.Headers.Location?.OriginalString);

        // Verificar enmascaramiento en GET /Auth/VerificarCodigo
        var verifyGet = await client.GetAsync("/Auth/VerificarCodigo");
        Assert.Equal(HttpStatusCode.OK, verifyGet.StatusCode);
        var verifyHtml = await verifyGet.Content.ReadAsStringAsync();

        Assert.Contains("Va*** Ro***", verifyHtml);
        Assert.Contains("v***@c***.co", verifyHtml);
        Assert.DoesNotContain("Valentina Rodriguez", verifyHtml);
        Assert.DoesNotContain("valentina.rodriguez@colegio.edu.co", verifyHtml);
        Assert.DoesNotContain("10-A", verifyHtml);

        // Validar OTP extraído del correo simulado
        var otp = ExtractOtpFromEmail(Assert.Single(factory.EmailSender.SentEmails).HtmlBody);
        var verifyCsrf = ExtractAntiforgeryToken(verifyHtml);

        var verifyPost = await client.PostAsync("/Auth/VerificarCodigo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = verifyCsrf,
            ["OtpCode"] = otp
        }));
        Assert.Equal(HttpStatusCode.Redirect, verifyPost.StatusCode);
        Assert.Equal("/Auth/Completar", verifyPost.Headers.Location?.OriginalString);

        // Verificar nombre completo y grado en GET /Auth/Completar
        var completeGet = await client.GetAsync("/Auth/Completar");
        Assert.Equal(HttpStatusCode.OK, completeGet.StatusCode);
        var completeHtml = await completeGet.Content.ReadAsStringAsync();

        Assert.Contains("Valentina Rodriguez", completeHtml);
        Assert.Contains("10-A", completeHtml);
    }

    /// <summary>
    /// RF-M01-00, RN-16, RN-1.1: <c>Completar</c> rechaza el registro si no se acepta el consentimiento;
    /// al aceptarlo, activa al elector, reclama la entrada en <c>census_whitelist</c>, registra
    /// <c>CONSENT_ACCEPTED</c> en auditoría con <see cref="LegalDocuments.Version"/> e inicia sesión.
    /// </summary>
    [Fact]
    public async Task Completar_ConsentRequired_LogsConsentAcceptedAndActivatesUser()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedWhitelistEntryAsync(
            factory,
            id: 14,
            document: WhitelistDoc,
            fullName: "Mateo Silva",
            email: "mateo.silva@colegio.edu.co",
            gradeName: "11-B");

        using var client = factory.CreateNoRedirectClient(useHttps: true);

        await PostRegistroAsync(client, WhitelistDoc);
        var otp = ExtractOtpFromEmail(Assert.Single(factory.EmailSender.SentEmails).HtmlBody);

        var verifyGet = await client.GetAsync("/Auth/VerificarCodigo");
        var verifyCsrf = ExtractAntiforgeryToken(await verifyGet.Content.ReadAsStringAsync());
        var verifyPost = await client.PostAsync("/Auth/VerificarCodigo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = verifyCsrf,
            ["OtpCode"] = otp
        }));
        Assert.Equal(HttpStatusCode.Redirect, verifyPost.StatusCode);

        // 1. Intento sin aceptar consentimiento (AcceptTermsAndPrivacy = false)
        var completeGet = await client.GetAsync("/Auth/Completar");
        var completeCsrf = ExtractAntiforgeryToken(await completeGet.Content.ReadAsStringAsync());

        var noConsentPost = await client.PostAsync("/Auth/Completar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = completeCsrf,
            ["Password"] = "StrongPass!2026",
            ["ConfirmPassword"] = "StrongPass!2026",
            ["AcceptTermsAndPrivacy"] = "false"
        }));

        Assert.Equal(HttpStatusCode.OK, noConsentPost.StatusCode);
        var noConsentRawHtml = await noConsentPost.Content.ReadAsStringAsync();
        var noConsentHtml = WebUtility.HtmlDecode(noConsentRawHtml);
        Assert.Contains("Debes aceptar los Términos de Uso y la Política de Privacidad", noConsentHtml);

        var docHash = Sha256Hex(WhitelistDoc);
        var createdBeforeConsent = await factory.QueryDbAsync(db => db.Voters.AnyAsync(v => v.DocumentHash == docHash));
        Assert.False(createdBeforeConsent);

        // 2. Intento con consentimiento aceptado (AcceptTermsAndPrivacy = true)
        var retryCsrf = ExtractAntiforgeryToken(noConsentRawHtml);
        var consentPost = await client.PostAsync("/Auth/Completar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = retryCsrf,
            ["Password"] = "StrongPass!2026",
            ["ConfirmPassword"] = "StrongPass!2026",
            ["AcceptTermsAndPrivacy"] = "true"
        }));

        Assert.Equal(HttpStatusCode.Redirect, consentPost.StatusCode);
        Assert.Equal("/Elector/Dashboard", consentPost.Headers.Location?.OriginalString);

        // Verificar sesión auto-iniciada en /Elector/Dashboard
        var dashboardRes = await client.GetAsync("/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.OK, dashboardRes.StatusCode);

        // Verificar persistencia de Voter, Whitelist reclamada y AuditLog CONSENT_ACCEPTED
        var (voter, whitelist, consentAudit) = await factory.QueryDbAsync(async db =>
        {
            var v = await db.Voters.SingleOrDefaultAsync(x => x.DocumentHash == docHash);
            var w = await db.CensusWhitelists.SingleAsync(x => x.Id == 14);
            var a = await db.AuditLogs.FirstOrDefaultAsync(x => x.Action == "CONSENT_ACCEPTED");
            return (v, w, a);
        });

        Assert.NotNull(voter);
        Assert.Equal("ACTIVO", voter!.Status);
        Assert.Equal("Mateo Silva", voter.FullName);
        Assert.NotNull(whitelist.ClaimedAt);
        Assert.Equal(voter.Id, whitelist.ClaimedByUserId);

        Assert.NotNull(consentAudit);
        Assert.Equal("ACCEPTED", consentAudit!.NewValue);
        Assert.Contains(LegalDocuments.Version, consentAudit.Details);
        Assert.Contains("Ley 1581 de 2012", consentAudit.Details);
    }

    /// <summary>
    /// RF-M01-02, RN-15: <c>GET /RecuperacionAcceso/Restablecer</c> usa <c>TryPeek</c> (múltiples GET
    /// conservan el token válido) y emite las cabeceras <c>Referrer-Policy: no-referrer</c> y
    /// <c>Cache-Control: no-cache, no-store</c>.
    /// </summary>
    [Fact]
    public async Task Restablecer_GetTryPeek_DoesNotConsumeToken_AndSetsSecurityHeaders()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedActiveVoterAsync(factory, id: 201, document: RecoveryDoc, password: InitialPassword);

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        var rawToken = await RequestRecoveryAndReadTokenAsync(factory, client, RecoveryDoc);

        for (var i = 0; i < 2; i++)
        {
            var getRes = await client.GetAsync($"/RecuperacionAcceso/Restablecer?token={Uri.EscapeDataString(rawToken)}");
            Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);

            var html = await getRes.Content.ReadAsStringAsync();
            Assert.Contains("Ingresa tu nueva contraseña", html);
            Assert.DoesNotContain("Este enlace de restablecimiento no es válido", html);

            AssertSecurityHeaders(getRes);
        }
    }

    /// <summary>
    /// RF-M01-02, RN-2, RN-15: Un <c>POST /RecuperacionAcceso/Restablecer</c> con modelo inválido
    /// no consume el token; el siguiente POST válido lo consume vía <c>TryConsume</c>, y cualquier
    /// reintento posterior con el mismo token (replay) es rechazado.
    /// </summary>
    [Fact]
    public async Task Restablecer_Post_InvalidModelDoesNotConsumeToken_ValidPostConsumes_AndReplayRejected()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedActiveVoterAsync(factory, id: 202, document: RecoveryDoc, password: InitialPassword);

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        var rawToken = await RequestRecoveryAndReadTokenAsync(factory, client, RecoveryDoc);

        var getRes = await client.GetAsync($"/RecuperacionAcceso/Restablecer?token={Uri.EscapeDataString(rawToken)}");
        var csrfToken = ExtractAntiforgeryToken(await getRes.Content.ReadAsStringAsync());

        // 1. POST con contraseña débil / no coincidente (ModelState inválido) -> NO consume el token.
        var invalidPost = await client.PostAsync("/RecuperacionAcceso/Restablecer", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = csrfToken,
            ["Token"] = rawToken,
            ["Password"] = "weak",
            ["ConfirmPassword"] = "different"
        }));
        Assert.Equal(HttpStatusCode.OK, invalidPost.StatusCode);
        var invalidRawHtml = await invalidPost.Content.ReadAsStringAsync();
        var invalidHtml = WebUtility.HtmlDecode(invalidRawHtml);
        Assert.DoesNotContain("Tu contraseña ha sido actualizada", invalidHtml);
        AssertSecurityHeaders(invalidPost);

        // 2. POST válido con el MISMO token -> éxito y consumo atómico del token.
        var validCsrf = ExtractAntiforgeryToken(invalidRawHtml);
        var validPost = await client.PostAsync("/RecuperacionAcceso/Restablecer", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = validCsrf,
            ["Token"] = rawToken,
            ["Password"] = "ResetStrong!2026",
            ["ConfirmPassword"] = "ResetStrong!2026"
        }));
        Assert.Equal(HttpStatusCode.OK, validPost.StatusCode);
        var successHtml = WebUtility.HtmlDecode(await validPost.Content.ReadAsStringAsync());
        Assert.Contains("Tu contraseña ha sido actualizada", successHtml);
        AssertSecurityHeaders(validPost);

        // 3. Replay del mismo token -> rechazado con mensaje genérico de enlace inválido.
        var replayPost = await client.PostAsync("/RecuperacionAcceso/Restablecer", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = validCsrf,
            ["Token"] = rawToken,
            ["Password"] = "AnotherStrong!2026",
            ["ConfirmPassword"] = "AnotherStrong!2026"
        }));
        Assert.Equal(HttpStatusCode.OK, replayPost.StatusCode);
        var replayHtml = WebUtility.HtmlDecode(await replayPost.Content.ReadAsStringAsync());
        Assert.Contains("Este enlace de restablecimiento no es válido o ha expirado", replayHtml);
        AssertSecurityHeaders(replayPost);
    }

    /// <summary>
    /// RF-M01-02, RN-14, RN-15: Completar el restablecimiento revoca la sesión activa previa
    /// (sin motivo <c>device</c> ni <c>disabled</c>) y limpia el bloqueo por intentos fallidos
    /// para permitir el acceso inmediato con la nueva contraseña.
    /// </summary>
    [Fact]
    public async Task Restablecer_Post_Success_RevokesActiveSessionAndClearsLockout()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 15, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedActiveVoterAsync(factory, id: 203, document: RecoveryDoc, password: InitialPassword);

        // 1. Sesión activa en client1.
        using var client1 = factory.CreateNoRedirectClient(useHttps: true);
        await LoginAsync(client1, RecoveryDoc, InitialPassword);
        Assert.Equal(HttpStatusCode.OK, (await client1.GetAsync("/Elector/Dashboard")).StatusCode);

        // 2. Activar bloqueo RN-15 (5 fallos) en client2.
        using var client2 = factory.CreateNoRedirectClient(useHttps: true);
        for (var i = 0; i < 5; i++)
        {
            var failRes = await PostLoginRawAsync(client2, RecoveryDoc, "WrongPass!999");
            Assert.Equal(HttpStatusCode.OK, failRes.StatusCode);
        }

        // Confirmar que el login está bloqueado incluso con la contraseña original.
        var lockedLogin = await PostLoginRawAsync(client2, RecoveryDoc, InitialPassword);
        Assert.Equal(HttpStatusCode.OK, lockedLogin.StatusCode);

        // 3. Ejecutar flujo completo de recuperación con client3.
        using var client3 = factory.CreateNoRedirectClient(useHttps: true);
        var rawToken = await RequestRecoveryAndReadTokenAsync(factory, client3, RecoveryDoc);
        var getReset = await client3.GetAsync($"/RecuperacionAcceso/Restablecer?token={Uri.EscapeDataString(rawToken)}");
        var resetCsrf = ExtractAntiforgeryToken(await getReset.Content.ReadAsStringAsync());

        const string newPassword = "RecoveredPass!2026";
        var postReset = await client3.PostAsync("/RecuperacionAcceso/Restablecer", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = resetCsrf,
            ["Token"] = rawToken,
            ["Password"] = newPassword,
            ["ConfirmPassword"] = newPassword
        }));
        Assert.Equal(HttpStatusCode.OK, postReset.StatusCode);
        Assert.Contains("Tu contraseña ha sido actualizada", WebUtility.HtmlDecode(await postReset.Content.ReadAsStringAsync()));

        // 4. La sesión previa de client1 quedó revocada con LoggedOut (sin ?reason=device ni ?reason=disabled).
        var revokedRes = await client1.GetAsync("/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, revokedRes.StatusCode);
        var revokedLocation = revokedRes.Headers.Location?.OriginalString ?? string.Empty;
        var revokedUri = revokedLocation.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? new Uri(revokedLocation)
            : new Uri(new Uri("https://localhost"), revokedLocation);
        Assert.Equal("/Auth/Login", revokedUri.AbsolutePath);
        Assert.DoesNotContain("reason=device", revokedLocation, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reason=disabled", revokedLocation, StringComparison.OrdinalIgnoreCase);

        // 5. Sin avanzar el reloj, el usuario puede iniciar sesión de inmediato con la nueva clave (lockout limpiado).
        using var client4 = factory.CreateNoRedirectClient(useHttps: true);
        var immediateLogin = await PostLoginRawAsync(client4, RecoveryDoc, newPassword);
        Assert.Equal(HttpStatusCode.Redirect, immediateLogin.StatusCode);
        Assert.Equal("/Elector/Dashboard", immediateLogin.Headers.Location?.OriginalString);
    }

    /// <summary>
    /// RF-M01-00, RF-M01-02: Todos los endpoints POST de auto-registro y recuperación de acceso
    /// rechazan con <c>400 BadRequest</c> cuando se omite el token antiforgery.
    /// </summary>
    [Theory]
    [InlineData("/Auth/Registro")]
    [InlineData("/Auth/VerificarCodigo")]
    [InlineData("/Auth/ResendOtp")]
    [InlineData("/Auth/Completar")]
    [InlineData("/RecuperacionAcceso/Recuperar")]
    [InlineData("/RecuperacionAcceso/Restablecer")]
    public async Task RegistrationAndRecovery_PostsWithoutAntiforgeryToken_Return400BadRequest(string postPath)
    {
        await using var factory = new WahlMiraiWebFactory();
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        var response = await client.PostAsync(postPath, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Dummy"] = "value"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static async Task SeedWhitelistEntryAsync(
        WahlMiraiWebFactory factory,
        uint id,
        string document,
        string fullName,
        string email,
        string gradeName = "11°")
    {
        await factory.SeedAsync(async db =>
        {
            if (!await db.Roles.AnyAsync())
            {
                db.Roles.AddRange(
                    new Role { Id = Roles.Elector, Name = Roles.ElectorName },
                    new Role { Id = Roles.Admin, Name = Roles.AdminName },
                    new Role { Id = Roles.SuperAdmin, Name = Roles.SuperAdminName });
            }

            var grade = await db.Grades.FirstOrDefaultAsync(g => g.Name == gradeName);
            if (grade == null)
            {
                grade = new Grade { Id = 11, Name = gradeName, SequenceOrder = 6, IsLastGrade = true };
                db.Grades.Add(grade);
            }

            db.CensusWhitelists.Add(new CensusWhitelist
            {
                Id = id,
                DocumentHash = Sha256Hex(document),
                EncryptedDocument = $"enc-{document}",
                FullName = fullName,
                ContactEmail = email,
                GradeId = grade.Id,
                UploadedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            });
        });
    }

    private static async Task SeedActiveVoterAsync(WahlMiraiWebFactory factory, uint id, string document, string password)
    {
        await factory.SeedAsync(async db =>
        {
            if (!await db.Roles.AnyAsync())
            {
                db.Roles.AddRange(
                    new Role { Id = Roles.Elector, Name = Roles.ElectorName },
                    new Role { Id = Roles.Admin, Name = Roles.AdminName },
                    new Role { Id = Roles.SuperAdmin, Name = Roles.SuperAdminName });
            }

            if (!await db.Grades.AnyAsync())
            {
                db.Grades.Add(new Grade { Id = 11, Name = "11°", SequenceOrder = 6, IsLastGrade = true });
            }

            db.Voters.Add(new Voter
            {
                Id = id,
                RoleId = Roles.Elector,
                GradeId = 11,
                DocumentHash = Sha256Hex(document),
                EncryptedDocument = $"enc-{document}",
                FullName = $"Elector {id}",
                ContactEmail = $"elector{id}@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 4),
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            });
        });
    }

    private static async Task<HttpResponseMessage> PostRegistroAsync(HttpClient client, string document, string? remoteIp = null)
    {
        var getReg = await client.GetAsync("/Auth/Registro");
        getReg.EnsureSuccessStatusCode();
        var csrfToken = ExtractAntiforgeryToken(await getReg.Content.ReadAsStringAsync());

        using var req = new HttpRequestMessage(HttpMethod.Post, "/Auth/Registro")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrfToken,
                ["Document"] = document
            })
        };

        if (!string.IsNullOrEmpty(remoteIp))
        {
            req.Headers.Add("X-Test-Remote-Ip", remoteIp);
        }

        return await client.SendAsync(req);
    }

    private static async Task<string> RequestRecoveryAndReadTokenAsync(
        WahlMiraiWebFactory factory,
        HttpClient client,
        string document)
    {
        var getRec = await client.GetAsync("/RecuperacionAcceso/Recuperar");
        getRec.EnsureSuccessStatusCode();
        var csrfToken = ExtractAntiforgeryToken(await getRec.Content.ReadAsStringAsync());

        var postRec = await client.PostAsync("/RecuperacionAcceso/Recuperar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = csrfToken,
            ["Documento"] = document
        }));
        Assert.Equal(HttpStatusCode.OK, postRec.StatusCode);

        var latestQueueId = await factory.QueryDbAsync(db =>
            db.EmailQueues
                .OrderByDescending(q => q.Id)
                .Select(q => q.Id)
                .FirstAsync());

        string? rawToken = null;
        await factory.ExecuteWithServicesAsync(sp =>
        {
            var pendingStore = sp.GetRequiredService<IPendingPasswordStore>();
            Assert.True(pendingStore.TryGetPassword(latestQueueId, out rawToken));
            return Task.CompletedTask;
        });

        Assert.False(string.IsNullOrWhiteSpace(rawToken));
        return rawToken!;
    }

    private static async Task LoginAsync(HttpClient client, string document, string password)
    {
        var res = await PostLoginRawAsync(client, document, password);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostLoginRawAsync(HttpClient client, string document, string password)
    {
        var getLogin = await client.GetAsync("/Auth/Login");
        getLogin.EnsureSuccessStatusCode();
        var csrfToken = ExtractAntiforgeryToken(await getLogin.Content.ReadAsStringAsync());

        return await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = csrfToken,
            ["Document"] = document,
            ["Password"] = password
        }));
    }

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Assert.True(response.Headers.TryGetValues("Referrer-Policy", out var referrerValues));
        Assert.Contains("no-referrer", referrerValues);

        var cacheControl = response.Headers.CacheControl?.ToString() ?? string.Empty;
        Assert.Contains("no-cache", cacheControl, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no-store", cacheControl, StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractOtpFromEmail(string htmlBody)
    {
        var match = Regex.Match(htmlBody, @">\s*(\d{6})\s*<");
        if (!match.Success)
        {
            throw new InvalidOperationException("No se encontró el código OTP de 6 dígitos en el cuerpo HTML del correo.");
        }

        return match.Groups[1].Value;
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""|<input[^>]*value=""([^""]+)""[^>]*name=""__RequestVerificationToken""",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            throw new InvalidOperationException("No se encontró __RequestVerificationToken en la respuesta HTML.");
        }

        return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
    }

    private static string Sha256Hex(string input)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
