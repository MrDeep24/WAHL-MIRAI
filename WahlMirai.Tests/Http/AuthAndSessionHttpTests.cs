// ============================================================
// Archivo:       AuthAndSessionHttpTests.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión (Pruebas HTTP)
// Desarrollador: Kevin
// Descripción:   Pruebas HTTP integrales sobre el pipeline real ASP.NET Core para
//                sesión única concurrente (RN-14), revocación por cambio de estado,
//                cierre de sesión, escalera de bloqueo 1/5/15 min y limitación de
//                tasa HTTP / bucket IP (RN-15), y validación antiforgery en AuthController.
// Requerimientos: RF-M01-01, RF-M07-01, RN-14, RN-15.
// ============================================================
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests.Http;

/// <summary>
/// Colección xUnit secuencial para pruebas HTTP basadas en <see cref="WahlMiraiWebFactory"/>,
/// evitando carreras sobre el <c>ReloadableLogger</c> global de Serilog durante el arranque de <c>Program.cs</c>.
/// </summary>
[CollectionDefinition(CollectionName, DisableParallelization = true)]
public sealed class HttpPipelineCollection
{
    public const string CollectionName = "HttpPipeline";
}

/// <summary>
/// Pruebas HTTP end-to-end para autenticación, revocación de sesión única (RN-14),
/// bloqueo escalonado y rate limiting (RN-15) y protección CSRF en <see cref="WahlMirai.Web.Controllers.AuthController"/>.
/// </summary>
[Collection(HttpPipelineCollection.CollectionName)]
public sealed class AuthAndSessionHttpTests
{
    private const string DefaultPassword = "UserPass!2026";
    private const string DefaultElectorDocument = "1002003001";

    /// <summary>
    /// RN-14: Un segundo inicio de sesión invalida la sesión previa del mismo usuario;
    /// la siguiente solicitud del primer cliente redirige a <c>/Auth/Login?reason=device</c>.
    /// </summary>
    [Fact]
    public async Task Login_SecondSession_InvalidatesFirstSession_RedirectsToDeviceReason()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedElectorAsync(factory, id: 101, document: DefaultElectorDocument, password: DefaultPassword);

        using var client1 = factory.CreateNoRedirectClient(useHttps: true);
        using var client2 = factory.CreateNoRedirectClient(useHttps: true);

        await LoginAsync(client1, DefaultElectorDocument, DefaultPassword);
        var initialDashboard = await client1.GetAsync("/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.OK, initialDashboard.StatusCode);

        // Segundo inicio de sesión con las mismas credenciales reemplaza el SessionStamp en IActiveSessionStore.
        await LoginAsync(client2, DefaultElectorDocument, DefaultPassword);
        var client2Dashboard = await client2.GetAsync("/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.OK, client2Dashboard.StatusCode);

        // El primer cliente queda revocado por sesión reemplazada en otro dispositivo (RN-14).
        var revokedResponse = await client1.GetAsync("/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, revokedResponse.StatusCode);
        Assert.Equal("/Auth/Login?reason=device", revokedResponse.Headers.Location?.OriginalString);
    }

    /// <summary>
    /// RN-14: Si el estado de la cuenta cambia a no activo (<c>INACTIVO</c>, <c>SUSPENDIDO</c>,
    /// <c>ELIMINADO</c>) o se marca <c>DeletedAt</c> durante una sesión activa, la siguiente
    /// solicitud redirige a <c>/Auth/Login?reason=disabled</c>.
    /// </summary>
    [Theory]
    [InlineData("INACTIVO", false)]
    [InlineData("SUSPENDIDO", false)]
    [InlineData("ELIMINADO", false)]
    [InlineData("ACTIVO", true)]
    public async Task Session_StatusChangeOrSoftDeleteMidSession_RedirectsToDisabledReason(
        string newStatus,
        bool setDeletedAt)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedElectorAsync(factory, id: 102, document: DefaultElectorDocument, password: DefaultPassword);

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        await LoginAsync(client, DefaultElectorDocument, DefaultPassword);

        var okResponse = await client.GetAsync("/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.OK, okResponse.StatusCode);

        await factory.SeedAsync(async db =>
        {
            var voter = await db.Voters.SingleAsync(v => v.Id == 102);
            voter.Status = newStatus;
            voter.DeletedAt = setDeletedAt ? clock.GetUtcNow().UtcDateTime : null;
        });

        var revokedResponse = await client.GetAsync("/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, revokedResponse.StatusCode);
        Assert.Equal("/Auth/Login?reason=disabled", revokedResponse.Headers.Location?.OriginalString);
    }

    /// <summary>
    /// RN-14: El cierre explícito en <c>/Auth/Logout</c> invalida el sello en
    /// <see cref="IActiveSessionStore"/> con motivo <see cref="SessionInvalidationReason.LoggedOut"/>
    /// y redirige a <c>/Auth/Login</c> sin parámetros de reemplazo de dispositivo ni deshabilitación.
    /// </summary>
    [Fact]
    public async Task Logout_InvalidatesSession_AndRedirectsWithoutDeviceOrDisabledReason()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedElectorAsync(factory, id: 103, document: DefaultElectorDocument, password: DefaultPassword);

        using var client = factory.CreateNoRedirectClient(useHttps: true, handleCookies: false);

        var getLogin = await client.GetAsync("/Auth/Login");
        var loginCookies = ExtractCookiePairs(getLogin);
        var csrfToken = ExtractAntiforgeryToken(await getLogin.Content.ReadAsStringAsync());

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/Auth/Login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrfToken,
                ["Document"] = DefaultElectorDocument,
                ["Password"] = DefaultPassword
            })
        };
        AddCookieHeader(loginRequest, loginCookies);

        var loginResponse = await client.SendAsync(loginRequest);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);
        var authCookies = MergeCookies(loginCookies, ExtractCookiePairs(loginResponse));

        // Verificar que la cookie antes del logout sí autentica en una ruta protegida.
        using (var preLogoutDash = new HttpRequestMessage(HttpMethod.Get, "/Elector/Dashboard"))
        {
            AddCookieHeader(preLogoutDash, authCookies);
            var dashRes = await client.SendAsync(preLogoutDash);
            Assert.Equal(HttpStatusCode.OK, dashRes.StatusCode);
        }

        // Ejecutar /Auth/Logout enviando la cookie de sesión.
        using (var logoutRequest = new HttpRequestMessage(HttpMethod.Get, "/Auth/Logout"))
        {
            AddCookieHeader(logoutRequest, authCookies);
            var logoutResponse = await client.SendAsync(logoutRequest);
            Assert.Equal(HttpStatusCode.Redirect, logoutResponse.StatusCode);
            Assert.Equal("/Auth/Login", logoutResponse.Headers.Location?.OriginalString);
        }

        // Reintentar con la cookie previa al logout: OnValidatePrincipal debe rechazarla sin ?reason=device ni ?reason=disabled.
        using (var replayDash = new HttpRequestMessage(HttpMethod.Get, "/Elector/Dashboard"))
        {
            AddCookieHeader(replayDash, authCookies);
            var replayResponse = await client.SendAsync(replayDash);
            Assert.Equal(HttpStatusCode.Redirect, replayResponse.StatusCode);
            var location = replayResponse.Headers.Location?.OriginalString ?? string.Empty;
            var redirectUri = location.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? new Uri(location)
                : new Uri(new Uri("https://localhost"), location);
            Assert.Equal("/Auth/Login", redirectUri.AbsolutePath);
            Assert.DoesNotContain("reason=device", location, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("reason=disabled", location, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// RN-15: Verifica determinísticamente con <see cref="FakeTimeProvider"/> la escalera
    /// de bloqueo por intentos fallidos en login: 1 min (5.º fallo), 5 min (6.º fallo) y 15 min (7.º fallo).
    /// </summary>
    [Fact]
    public async Task Login_LockoutLadder_1Min_5Min_15Min_ControlledDeterministicallyByFakeTimeProvider()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedElectorAsync(factory, id: 104, document: DefaultElectorDocument, password: DefaultPassword);

        using var client = factory.CreateNoRedirectClient(useHttps: true);

        // 5 intentos fallidos consecutivos activan el primer escalón de bloqueo (1 minuto).
        for (var i = 0; i < 5; i++)
        {
            var failResponse = await PostLoginRawAsync(client, DefaultElectorDocument, "WrongPass!99");
            Assert.Equal(HttpStatusCode.OK, failResponse.StatusCode);
        }

        // Justo antes de expirar el minuto (59 s): incluso la clave correcta sigue bloqueada.
        clock.Advance(TimeSpan.FromSeconds(59));
        var stillLocked1Min = await PostLoginRawAsync(client, DefaultElectorDocument, DefaultPassword);
        Assert.Equal(HttpStatusCode.OK, stillLocked1Min.StatusCode);

        // Al superar el minuto (61 s): desbloqueado; un 6.º fallo escala a 5 minutos.
        clock.Advance(TimeSpan.FromSeconds(2));
        var fail6 = await PostLoginRawAsync(client, DefaultElectorDocument, "WrongPass!99");
        Assert.Equal(HttpStatusCode.OK, fail6.StatusCode);

        // Justo antes de los 5 minutos (4 m 59 s): sigue bloqueado.
        clock.Advance(TimeSpan.FromMinutes(4) + TimeSpan.FromSeconds(59));
        var stillLocked5Min = await PostLoginRawAsync(client, DefaultElectorDocument, DefaultPassword);
        Assert.Equal(HttpStatusCode.OK, stillLocked5Min.StatusCode);

        // Al superar los 5 minutos: desbloqueado; un 7.º fallo escala a 15 minutos.
        clock.Advance(TimeSpan.FromSeconds(2));
        var fail7 = await PostLoginRawAsync(client, DefaultElectorDocument, "WrongPass!99");
        Assert.Equal(HttpStatusCode.OK, fail7.StatusCode);

        // Justo antes de los 15 minutos (14 m 59 s): sigue bloqueado.
        clock.Advance(TimeSpan.FromMinutes(14) + TimeSpan.FromSeconds(59));
        var stillLocked15Min = await PostLoginRawAsync(client, DefaultElectorDocument, DefaultPassword);
        Assert.Equal(HttpStatusCode.OK, stillLocked15Min.StatusCode);

        // Al superar los 15 minutos: desbloqueado y la contraseña válida permite iniciar sesión.
        clock.Advance(TimeSpan.FromSeconds(2));
        var unlockedSuccess = await PostLoginRawAsync(client, DefaultElectorDocument, DefaultPassword);
        Assert.Equal(HttpStatusCode.Redirect, unlockedSuccess.StatusCode);
        Assert.Equal("/Elector/Dashboard", unlockedSuccess.Headers.Location?.OriginalString);
    }

    /// <summary>
    /// RN-15: La política <c>AuthLogin</c> aplica limitación de tasa por IP (<c>X-Test-Remote-Ip</c>),
    /// respondiendo <c>429 TooManyRequests</c> y cabecera <c>Retry-After: 60</c> al superar <c>LoginPermitPerMinute</c>.
    /// </summary>
    [Fact]
    public async Task RateLimiting_AuthLogin_ExceedingPermitPerMinute_Returns429WithRetryAfter60()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:LoginPermitPerMinute"] = "2"
        };
        await using var factory = new WahlMiraiWebFactory(configOverrides: overrides);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        const string ipA = "198.51.100.10";
        const string ipB = "198.51.100.11";

        var getLogin = await client.GetAsync("/Auth/Login");
        var csrfToken = ExtractAntiforgeryToken(await getLogin.Content.ReadAsStringAsync());

        for (var i = 0; i < 2; i++)
        {
            using var req = CreateLoginPostRequest(csrfToken, DefaultElectorDocument, "WrongPass!1", ipA);
            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        // 3.ª petición desde ipA excede el cupo de 2 por minuto -> 429 + Retry-After: 60.
        using (var throttledReq = CreateLoginPostRequest(csrfToken, DefaultElectorDocument, "WrongPass!1", ipA))
        {
            var throttledRes = await client.SendAsync(throttledReq);
            Assert.Equal(HttpStatusCode.TooManyRequests, throttledRes.StatusCode);
            Assert.Equal("60", throttledRes.Headers.RetryAfter?.Delta?.TotalSeconds.ToString() ?? throttledRes.Headers.GetValues("Retry-After").FirstOrDefault());
            Assert.Contains("60", throttledRes.Headers.GetValues("Retry-After"));
        }

        // Otra IP distinta mantiene su partición independiente y no queda limitada.
        using (var otherIpReq = CreateLoginPostRequest(csrfToken, DefaultElectorDocument, "WrongPass!1", ipB))
        {
            var otherIpRes = await client.SendAsync(otherIpReq);
            Assert.Equal(HttpStatusCode.OK, otherIpRes.StatusCode);
        }
    }

    /// <summary>
    /// RN-15: La política <c>AuthRegister</c> responde <c>429 TooManyRequests</c> con
    /// <c>Retry-After: 60</c> al exceder <c>RegisterPermitPerMinute</c> por IP, sin afectar otras IPs.
    /// </summary>
    [Fact]
    public async Task RateLimiting_AuthRegister_ExceedingPermitPerMinute_Returns429WithRetryAfter60()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:RegisterPermitPerMinute"] = "2"
        };
        await using var factory = new WahlMiraiWebFactory(configOverrides: overrides);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        const string ipA = "198.51.100.20";
        const string ipB = "198.51.100.21";

        var getReg = await client.GetAsync("/Auth/Registro");
        var csrfToken = ExtractAntiforgeryToken(await getReg.Content.ReadAsStringAsync());

        for (var i = 0; i < 2; i++)
        {
            using var req = CreateRegistroPostRequest(csrfToken, $"990000{i}", ipA);
            var res = await client.SendAsync(req);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        using (var throttledReq = CreateRegistroPostRequest(csrfToken, "9900009", ipA))
        {
            var throttledRes = await client.SendAsync(throttledReq);
            Assert.Equal(HttpStatusCode.TooManyRequests, throttledRes.StatusCode);
            Assert.Contains("60", throttledRes.Headers.GetValues("Retry-After"));
        }

        using (var otherIpReq = CreateRegistroPostRequest(csrfToken, "9900009", ipB))
        {
            var otherIpRes = await client.SendAsync(otherIpReq);
            Assert.Equal(HttpStatusCode.OK, otherIpRes.StatusCode);
        }
    }

    /// <summary>
    /// RN-15: El bucket IP <c>register:{clientIpKey}</c> bloquea nuevos intentos de auto-registro
    /// tras alcanzar <c>RateLimiting:RegisterFailureThreshold</c>, sin afectar a otra IP.
    /// </summary>
    [Fact]
    public async Task Registration_IpBucket_RegisterFailureThreshold_LocksOnlyFailingIp()
    {
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:RegisterFailureThreshold"] = "2",
            ["RateLimiting:RegisterPermitPerMinute"] = "50"
        };
        await using var factory = new WahlMiraiWebFactory(configOverrides: overrides);

        await factory.SeedAsync(async db =>
        {
            await EnsureBaseCatalogsAsync(db);
            db.CensusWhitelists.Add(new CensusWhitelist
            {
                Id = 501,
                DocumentHash = Sha256Hex("777888999"),
                EncryptedDocument = "enc-777888999",
                FullName = "Student Valid",
                ContactEmail = "valid.student@example.com",
                GradeId = 11,
                UploadedByUserId = 1,
                CreatedAt = DateTime.UtcNow
            });
        });

        using var clientA = factory.CreateNoRedirectClient(useHttps: true);
        using var clientB = factory.CreateNoRedirectClient(useHttps: true);

        const string ipA = "198.51.100.30";
        const string ipB = "198.51.100.31";

        var getRegA = await clientA.GetAsync("/Auth/Registro");
        var csrfA = ExtractAntiforgeryToken(await getRegA.Content.ReadAsStringAsync());

        // 2 fallos con documentos inexistentes bloquean el bucket register:{ipA}.
        for (var i = 0; i < 2; i++)
        {
            using var failReq = CreateRegistroPostRequest(csrfA, $"000000{i}", ipA);
            var failRes = await clientA.SendAsync(failReq);
            Assert.Equal(HttpStatusCode.OK, failRes.StatusCode);
        }

        // Incluso enviando un documento válido en whitelist, ipA recibe vista genérica de bloqueo y NO envía correo.
        using (var blockedValidReq = CreateRegistroPostRequest(csrfA, "777888999", ipA))
        {
            var blockedRes = await clientA.SendAsync(blockedValidReq);
            Assert.Equal(HttpStatusCode.OK, blockedRes.StatusCode);
            var html = await blockedRes.Content.ReadAsStringAsync();
            Assert.Contains("Demasiados intentos fallidos desde esta conexión", html);
            Assert.Empty(factory.EmailSender.SentEmails);
        }

        // ipB puede iniciar el registro válido sin bloqueo.
        var getRegB = await clientB.GetAsync("/Auth/Registro");
        var csrfB = ExtractAntiforgeryToken(await getRegB.Content.ReadAsStringAsync());
        using (var allowedReq = CreateRegistroPostRequest(csrfB, "777888999", ipB))
        {
            var allowedRes = await clientB.SendAsync(allowedReq);
            Assert.Equal(HttpStatusCode.Redirect, allowedRes.StatusCode);
            Assert.Equal("/Auth/VerificarCodigo", allowedRes.Headers.Location?.OriginalString);
            Assert.Single(factory.EmailSender.SentEmails);
        }
    }

    /// <summary>
    /// RF-M01-01: <c>POST /Auth/Login</c> exige token antiforgery válido y rechaza con
    /// <c>400 BadRequest</c> cuando el token falta o es inválido.
    /// </summary>
    [Fact]
    public async Task Login_Post_MissingOrInvalidAntiforgeryToken_Returns400BadRequest()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedElectorAsync(factory, id: 105, document: DefaultElectorDocument, password: DefaultPassword);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        // Sin token antiforgery
        var missingRes = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Document"] = DefaultElectorDocument,
            ["Password"] = DefaultPassword
        }));
        Assert.Equal(HttpStatusCode.BadRequest, missingRes.StatusCode);

        // Con cookie antiforgery válida pero token alterado en el formulario
        await client.GetAsync("/Auth/Login");
        var invalidRes = await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = "invalid-antiforgery-token",
            ["Document"] = DefaultElectorDocument,
            ["Password"] = DefaultPassword
        }));
        Assert.Equal(HttpStatusCode.BadRequest, invalidRes.StatusCode);
    }

    /// <summary>
    /// RF-M07-01 (Brecha documentada R3): <c>POST /Auth/CambiarClave</c> actualmente procesa la solicitud
    /// sin exigir <c>[ValidateAntiForgeryToken]</c> y actualiza la contraseña cuando se omite el token.
    /// </summary>
    [Fact]
    public async Task CambiarClave_Post_WithoutAntiforgeryToken_CurrentlySucceeds_DocumentedGap()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedElectorAsync(factory, id: 106, document: DefaultElectorDocument, password: DefaultPassword);

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        await LoginAsync(client, DefaultElectorDocument, DefaultPassword);

        const string newPassword = "UpdatedPass!2026";

        // NOTA: Brecha antiforgery fuera de alcance (R3): AuthController.CambiarClave ([HttpPost])
        // carece de [ValidateAntiForgeryToken] y Views/Auth/CambiarClave.cshtml no incluye @Html.AntiForgeryToken().
        // Se afirma el comportamiento actual sin modificar producción; requiere seguimiento futuro.
        var changeRes = await client.PostAsync("/Auth/CambiarClave", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["NewPassword"] = newPassword,
            ["ConfirmPassword"] = newPassword
        }));

        Assert.Equal(HttpStatusCode.Redirect, changeRes.StatusCode);
        Assert.Equal("/Elector/Dashboard", changeRes.Headers.Location?.OriginalString);

        // Verificar que la nueva contraseña quedó persistida y permite iniciar sesión.
        using var verifyClient = factory.CreateNoRedirectClient(useHttps: true);
        var reloginRes = await PostLoginRawAsync(verifyClient, DefaultElectorDocument, newPassword);
        Assert.Equal(HttpStatusCode.Redirect, reloginRes.StatusCode);
        Assert.Equal("/Elector/Dashboard", reloginRes.Headers.Location?.OriginalString);
    }

    private static async Task SeedElectorAsync(WahlMiraiWebFactory factory, uint id, string document, string password)
    {
        await factory.SeedAsync(async db =>
        {
            await EnsureBaseCatalogsAsync(db);
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

    private static async Task EnsureBaseCatalogsAsync(WahlMiraiDbContext db)
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
    }

    private static async Task LoginAsync(HttpClient client, string document, string password)
    {
        var response = await PostLoginRawAsync(client, document, password);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
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

    private static HttpRequestMessage CreateLoginPostRequest(string csrfToken, string document, string password, string remoteIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/Auth/Login")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrfToken,
                ["Document"] = document,
                ["Password"] = password
            })
        };
        request.Headers.Add("X-Test-Remote-Ip", remoteIp);
        return request;
    }

    private static HttpRequestMessage CreateRegistroPostRequest(string csrfToken, string document, string remoteIp)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/Auth/Registro")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["__RequestVerificationToken"] = csrfToken,
                ["Document"] = document
            })
        };
        request.Headers.Add("X-Test-Remote-Ip", remoteIp);
        return request;
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

    private static Dictionary<string, string> ExtractCookiePairs(HttpResponseMessage response)
    {
        var cookies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return cookies;
        }

        foreach (var header in values)
        {
            var firstSegment = header.Split(';', 2)[0];
            var eqIdx = firstSegment.IndexOf('=');
            if (eqIdx > 0)
            {
                var name = firstSegment[..eqIdx].Trim();
                var value = firstSegment[(eqIdx + 1)..].Trim();
                if (!string.IsNullOrEmpty(value))
                {
                    cookies[name] = value;
                }
            }
        }

        return cookies;
    }

    private static Dictionary<string, string> MergeCookies(
        Dictionary<string, string> existing,
        Dictionary<string, string> incoming)
    {
        var merged = new Dictionary<string, string>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in incoming)
        {
            merged[kvp.Key] = kvp.Value;
        }
        return merged;
    }

    private static void AddCookieHeader(HttpRequestMessage request, Dictionary<string, string> cookies)
    {
        if (cookies.Count > 0)
        {
            request.Headers.Add("Cookie", string.Join("; ", cookies.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }
    }

    private static string Sha256Hex(string input)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
