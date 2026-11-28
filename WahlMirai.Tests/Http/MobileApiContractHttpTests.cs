// ============================================================
// Archivo:       MobileApiContractHttpTests.cs
// Módulo:        API Móvil — Contrato HTTP (M01 / M03 / M05 / M06)
// Desarrollador: Kevin
// Descripción:   Pruebas HTTP integrales sobre el contrato de MobileApiController
//                (/api/mobile/*): health, login/logout, rate limiting, perfil y
//                expiración/revocación de tokens Bearer, visibilidad RN-4/RN-4.1/RN-5
//                en eventos, conversión de fechas ToEpochMillis, emisión de voto
//                y descarga cifrada de archivos multimedia (.enc).
// Requerimientos: RF-M01-01, RF-M05-00, RN-4, RN-4.1, RN-5, RN-14, RN-15.
// ============================================================
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests.Http;

/// <summary>
/// Pruebas HTTP end-to-end sobre el contrato completo de <see cref="WahlMirai.Web.Controllers.MobileApiController"/>
/// (<c>/api/mobile/*</c>) con aislamiento por fábrica y reloj determinista <see cref="FakeTimeProvider"/>.
/// </summary>
[Collection(HttpPipelineCollection.CollectionName)]
public sealed class MobileApiContractHttpTests
{
    private const string SharedPassword = "MobilePass!2026";
    private const string SuperAdminDoc = "4000000001";
    private const string AdminDoc = "4000000002";
    private const string ElectorDoc = "4000000003";
    private const string SecondElectorDoc = "4000000004";

    /// <summary>
    /// Verifica que <c>GET /api/mobile/health</c> retorna <c>200 OK</c> con el estado del servicio
    /// y la marca temporal en milisegundos alineada con <see cref="FakeTimeProvider"/>.
    /// </summary>
    [Fact]
    public async Task Health_Returns200WithServiceStatusAndDeterministicTimestamp()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 15, 30, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        var res1 = await client.GetAsync("/api/mobile/health");
        Assert.Equal(HttpStatusCode.OK, res1.StatusCode);

        using (var json = JsonDocument.Parse(await res1.Content.ReadAsStringAsync()))
        {
            var root = json.RootElement;
            Assert.Equal("ok", root.GetProperty("status").GetString());
            Assert.Equal("WAHL-MIRAI Mobile API", root.GetProperty("service").GetString());
            Assert.Equal(clock.GetUtcNow().ToUnixTimeMilliseconds(), root.GetProperty("timestamp").GetInt64());
        }

        clock.Advance(TimeSpan.FromSeconds(42));
        var res2 = await client.GetAsync("/api/mobile/health");
        Assert.Equal(HttpStatusCode.OK, res2.StatusCode);

        using (var json = JsonDocument.Parse(await res2.Content.ReadAsStringAsync()))
        {
            Assert.Equal(clock.GetUtcNow().ToUnixTimeMilliseconds(), json.RootElement.GetProperty("timestamp").GetInt64());
        }
    }

    /// <summary>
    /// RF-M01-01, RN-14: Verifica <c>POST /api/mobile/login</c> con credenciales válidas para
    /// <c>ELECTOR</c>, <c>ADMIN</c> y <c>SUPER_ADMIN</c>: retorna <c>200 OK</c>, token protegido,
    /// <c>expiresAt</c> en formato ISO <c>"O"</c> (+2h), DTO de usuario con documento limpio
    /// y sello de sesión registrado en <see cref="IActiveSessionStore"/>.
    /// </summary>
    [Theory]
    [InlineData("  " + ElectorDoc + "  ", ElectorDoc, "11°", "Elector")]
    [InlineData(AdminDoc, AdminDoc, "N/A", "Administrador")]
    [InlineData(SuperAdminDoc, SuperAdminDoc, "N/A", "Súper Administrador")]
    public async Task Login_ValidCredentials_Returns200WithTokenIsoExpiresAtUserDtoAndRegistersSession(
        string inputDocument,
        string expectedDocument,
        string expectedGrade,
        string expectedRoleDisplay)
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        var (res, token, body) = await LoginMobileAsync(client, inputDocument, SharedPassword);
        using (body)
        {
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(token));

            var root = body!.RootElement;
            Assert.Equal(clock.GetUtcNow().AddHours(2).ToString("O"), root.GetProperty("expiresAt").GetString());

            var user = root.GetProperty("user");
            Assert.True(user.GetProperty("id").GetUInt32() > 0);
            Assert.Equal(expectedDocument, user.GetProperty("documento").GetString());
            Assert.False(string.IsNullOrWhiteSpace(user.GetProperty("nombre").GetString()));
            Assert.Equal(expectedGrade, user.GetProperty("grado").GetString());
            Assert.Equal(expectedRoleDisplay, user.GetProperty("rol").GetString());
            Assert.Equal("ACTIVO", user.GetProperty("status").GetString());
        }

        // Confirmar que la sesión quedó registrada en IActiveSessionStore consultando /profile.
        using var profileRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", token);
        Assert.Equal(HttpStatusCode.OK, profileRes.StatusCode);
    }

    /// <summary>
    /// RF-M01-01: <c>POST /api/mobile/login</c> retorna <c>400 BadRequest</c> con código
    /// <c>INVALID_REQUEST</c> cuando el documento o la contraseña están vacíos/en blanco o el payload es nulo.
    /// </summary>
    [Fact]
    public async Task Login_InvalidRequest_Returns400InvalidRequest()
    {
        await using var factory = new WahlMiraiWebFactory();
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        var payloads = new[]
        {
            JsonContent.Create(new { document = "", password = SharedPassword }),
            JsonContent.Create(new { document = ElectorDoc, password = "   " }),
            JsonContent.Create<object?>(null)
        };

        foreach (var content in payloads)
        {
            using var res = await client.PostAsync("/api/mobile/login", content);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);

            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("INVALID_REQUEST", json.RootElement.GetProperty("code").GetString());
            Assert.Equal("Por favor ingrese documento y contraseña.", json.RootElement.GetProperty("message").GetString());
        }
    }

    /// <summary>
    /// RF-M01-01, RN-14: <c>POST /api/mobile/login</c> retorna <c>401 Unauthorized</c> con código
    /// <c>INVALID_CREDENTIALS</c> ante contraseña errónea, documento desconocido o cuenta <c>INACTIVO</c>/<c>ELIMINADO</c>.
    /// </summary>
    [Fact]
    public async Task Login_WrongPasswordUnknownOrInactiveDeleted_Returns401InvalidCredentials()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);

        await factory.ExecuteWithServicesAsync(async sp =>
        {
            var encryptor = sp.GetRequiredService<IDocumentEncryptionService>();
            var db = sp.GetRequiredService<WahlMiraiDbContext>();
            var hash = BCrypt.Net.BCrypt.HashPassword(SharedPassword, workFactor: 4);

            db.Voters.AddRange(
                new Voter
                {
                    Id = 10,
                    RoleId = Roles.Elector,
                    GradeId = 11,
                    DocumentHash = Sha256Hex("4000000010"),
                    EncryptedDocument = encryptor.Encrypt("4000000010"),
                    FullName = "Elector Inactivo",
                    ContactEmail = "inactivo@example.com",
                    PasswordHash = hash,
                    Status = "INACTIVO",
                    RegisteredAt = clock.GetUtcNow().UtcDateTime
                },
                new Voter
                {
                    Id = 11,
                    RoleId = Roles.Elector,
                    GradeId = 11,
                    DocumentHash = Sha256Hex("4000000011"),
                    EncryptedDocument = encryptor.Encrypt("4000000011"),
                    FullName = "Elector Eliminado",
                    ContactEmail = "eliminado@example.com",
                    PasswordHash = hash,
                    Status = "ELIMINADO",
                    DeletedAt = clock.GetUtcNow().UtcDateTime,
                    RegisteredAt = clock.GetUtcNow().UtcDateTime
                });
            await db.SaveChangesAsync();
        });

        using var client = factory.CreateNoRedirectClient(useHttps: true);

        var attempts = new[]
        {
            (ElectorDoc, "WrongPass!99"),
            ("9999999999", SharedPassword),
            ("4000000010", SharedPassword),
            ("4000000011", SharedPassword)
        };

        foreach (var (doc, pwd) in attempts)
        {
            var (res, _, body) = await LoginMobileAsync(client, doc, pwd);
            using (body)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
                Assert.Equal("INVALID_CREDENTIALS", body!.RootElement.GetProperty("code").GetString());
            }
        }
    }

    /// <summary>
    /// RN-15: Tras 5 intentos fallidos en <c>POST /api/mobile/login</c>, la cuenta queda bloqueada
    /// durante 1 minuto (incluso con la contraseña correcta retorna <c>401 INVALID_CREDENTIALS</c>)
    /// y se desbloquea al superar el minuto en <see cref="FakeTimeProvider"/>.
    /// </summary>
    [Fact]
    public async Task Login_FiveFailedAttempts_LocksOutAccountUntilLockoutWindowExpires()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 10, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        for (var i = 0; i < 5; i++)
        {
            var (failRes, _, failBody) = await LoginMobileAsync(client, ElectorDoc, "BadPassword!1");
            using (failBody)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, failRes.StatusCode);
                Assert.Equal("INVALID_CREDENTIALS", failBody!.RootElement.GetProperty("code").GetString());
            }
        }

        // Antes de cumplir 1 minuto (59 s): incluso con contraseña válida sigue bloqueado.
        clock.Advance(TimeSpan.FromSeconds(59));
        var (lockedRes, _, lockedBody) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        using (lockedBody)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, lockedRes.StatusCode);
            Assert.Equal("INVALID_CREDENTIALS", lockedBody!.RootElement.GetProperty("code").GetString());
        }

        // Tras superar 1 minuto: desbloqueado y la contraseña válida emite token.
        clock.Advance(TimeSpan.FromSeconds(2));
        var (okRes, token, okBody) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        using (okBody)
        {
            Assert.Equal(HttpStatusCode.OK, okRes.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(token));
        }
    }

    /// <summary>
    /// RN-15: <c>POST /api/mobile/login</c> aplica la política <c>AuthLogin</c> por IP
    /// (<c>X-Test-Remote-Ip</c>), respondiendo <c>429 TooManyRequests</c> con <c>Retry-After: 60</c>
    /// al superar <c>RateLimiting:LoginPermitPerMinute</c>, sin afectar a otra IP.
    /// </summary>
    [Fact]
    public async Task Login_ExceedingRateLimit_Returns429WithRetryAfter60_WithoutAffectingOtherIp()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        var overrides = new Dictionary<string, string?>
        {
            ["RateLimiting:LoginPermitPerMinute"] = "2"
        };
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock, configOverrides: overrides);
        await SeedStandardActorsAsync(factory, clock);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        const string ipA = "198.51.100.50";
        const string ipB = "198.51.100.51";

        for (var i = 0; i < 2; i++)
        {
            var (res, _, body) = await LoginMobileAsync(client, ElectorDoc, "WrongPass!1", ipA);
            using (body)
            {
                Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
            }
        }

        var (throttledRes, _, _) = await LoginMobileAsync(client, ElectorDoc, SharedPassword, ipA);
        Assert.Equal(HttpStatusCode.TooManyRequests, throttledRes.StatusCode);
        Assert.Contains("60", throttledRes.Headers.GetValues("Retry-After"));
        Assert.Contains("Demasiadas solicitudes", await throttledRes.Content.ReadAsStringAsync());

        var (otherIpRes, token, otherIpBody) = await LoginMobileAsync(client, ElectorDoc, SharedPassword, ipB);
        using (otherIpBody)
        {
            Assert.Equal(HttpStatusCode.OK, otherIpRes.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(token));
        }
    }

    /// <summary>
    /// RN-14: <c>POST /api/mobile/logout</c> con token Bearer válido retorna <c>200 OK</c>
    /// (<c>{ success: true }</c>) e invalida la sesión de forma que el siguiente <c>GET /api/mobile/profile</c>
    /// retorna <c>401 Unauthorized</c> con código <c>SESSION_EXPIRED</c>.
    /// </summary>
    [Fact]
    public async Task Logout_WithValidToken_InvalidatesSession_SubsequentProfileReturns401SessionExpired()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        var (loginRes, token, loginBody) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        loginBody?.Dispose();
        Assert.Equal(HttpStatusCode.OK, loginRes.StatusCode);

        using var logoutRes = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/logout", token);
        Assert.Equal(HttpStatusCode.OK, logoutRes.StatusCode);
        using (var json = JsonDocument.Parse(await logoutRes.Content.ReadAsStringAsync()))
        {
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        }

        using var profileAfterLogout = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", token);
        Assert.Equal(HttpStatusCode.Unauthorized, profileAfterLogout.StatusCode);
        using (var json = JsonDocument.Parse(await profileAfterLogout.Content.ReadAsStringAsync()))
        {
            Assert.Equal("SESSION_EXPIRED", json.RootElement.GetProperty("code").GetString());
        }
    }

    /// <summary>
    /// Contrato actual de <c>POST /api/mobile/logout</c> sin token Bearer: retorna <c>200 OK</c>
    /// (<c>{ success: true }</c>) en lugar de <c>401 Unauthorized</c> (observación documentada).
    /// </summary>
    [Fact]
    public async Task Logout_WithoutToken_Returns200Success_DocumentedBehavior()
    {
        await using var factory = new WahlMiraiWebFactory();
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        // NOTA: MobileApiController.Logout() invoca AuthenticateBearerAsync() pero ignora authResult.ErrorResult
        // y solo invalida la sesión si authResult.Voter != null, devolviendo siempre 200 { success: true }
        // aunque no se envíe token (o se envíe un token inválido) en lugar de 401 Unauthorized.
        using var res = await client.PostAsync("/api/mobile/logout", content: null);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
    }

    /// <summary>
    /// RF-M01-01: <c>GET /api/mobile/profile</c> con token válido retorna <c>200 OK</c>
    /// con el documento descifrado mediante <see cref="IDocumentEncryptionService"/> y el DTO completo.
    /// </summary>
    [Fact]
    public async Task Profile_ValidToken_Returns200WithDecryptedDocumentAndFullUserDto()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        var (_, token, loginBody) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        loginBody?.Dispose();

        using var res = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", token);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var root = json.RootElement;
        Assert.Equal(3u, root.GetProperty("id").GetUInt32());
        Assert.Equal(ElectorDoc, root.GetProperty("documento").GetString());
        Assert.Equal("Elector Activo", root.GetProperty("nombre").GetString());
        Assert.Equal("11°", root.GetProperty("grado").GetString());
        Assert.Equal("Elector", root.GetProperty("rol").GetString());
        Assert.Equal("ACTIVO", root.GetProperty("status").GetString());
    }

    /// <summary>
    /// RN-14: Verifica todas las variantes <c>401 Unauthorized</c> de <c>GET /api/mobile/profile</c>:
    /// <c>TOKEN_MISSING</c>, <c>TOKEN_INVALID</c>, <c>SESSION_EXPIRED</c> por reemplazo de sesión (RN-14),
    /// <c>SESSION_EXPIRED</c> por vencimiento de <c>TokenLifetime</c> (2h + 1s en <see cref="FakeTimeProvider"/>,
    /// verificando que a las 2h exactas aún responde <c>200 OK</c>) y <c>ACCOUNT_DISABLED</c>
    /// por cambio de estado o borrado lógico (<c>DeletedAt != null</c>).
    /// </summary>
    [Fact]
    public async Task Profile_All401Variants_Missing_Invalid_Rn14SessionReplaced_ClockExpired_AndAccountDisabled()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);
        using var client = factory.CreateNoRedirectClient(useHttps: true);

        // 1. TOKEN_MISSING (sin cabecera o esquema distinto de Bearer).
        using (var noHeader = await client.GetAsync("/api/mobile/profile"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, noHeader.StatusCode);
            using var json = JsonDocument.Parse(await noHeader.Content.ReadAsStringAsync());
            Assert.Equal("TOKEN_MISSING", json.RootElement.GetProperty("code").GetString());
        }

        using (var basicReq = new HttpRequestMessage(HttpMethod.Get, "/api/mobile/profile"))
        {
            basicReq.Headers.Authorization = new AuthenticationHeaderValue("Basic", "dGVzdDp0ZXN0");
            using var basicRes = await client.SendAsync(basicReq);
            Assert.Equal(HttpStatusCode.Unauthorized, basicRes.StatusCode);
            using var json = JsonDocument.Parse(await basicRes.Content.ReadAsStringAsync());
            Assert.Equal("TOKEN_MISSING", json.RootElement.GetProperty("code").GetString());
        }

        using (var bareBearerReq = new HttpRequestMessage(HttpMethod.Get, "/api/mobile/profile"))
        {
            // HTTP normaliza "Bearer " recortando espacios finales a "Bearer" -> no coincide con "Bearer ".
            bareBearerReq.Headers.Add("Authorization", "Bearer ");
            using var bareBearerRes = await client.SendAsync(bareBearerReq);
            Assert.Equal(HttpStatusCode.Unauthorized, bareBearerRes.StatusCode);
            using var json = JsonDocument.Parse(await bareBearerRes.Content.ReadAsStringAsync());
            Assert.Equal("TOKEN_MISSING", json.RootElement.GetProperty("code").GetString());
        }

        // 2. TOKEN_INVALID (token malformado o manipulado).
        using (var invalidRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", "tampered.invalid.token"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, invalidRes.StatusCode);
            using var json = JsonDocument.Parse(await invalidRes.Content.ReadAsStringAsync());
            Assert.Equal("TOKEN_INVALID", json.RootElement.GetProperty("code").GetString());
        }

        // 3. SESSION_EXPIRED por RN-14 (segundo login invalida el primer token) + manipulación de token real -> TOKEN_INVALID.
        var (_, token1, body1) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        body1?.Dispose();
        var tamperedRealToken = token1![..^1] + (token1[^1] == 'A' ? 'B' : 'A');
        using (var tamperedRealRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", tamperedRealToken))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, tamperedRealRes.StatusCode);
            using var json = JsonDocument.Parse(await tamperedRealRes.Content.ReadAsStringAsync());
            Assert.Equal("TOKEN_INVALID", json.RootElement.GetProperty("code").GetString());
        }

        var (_, token2, body2) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        body2?.Dispose();

        using (var replacedRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", token1))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, replacedRes.StatusCode);
            using var json = JsonDocument.Parse(await replacedRes.Content.ReadAsStringAsync());
            Assert.Equal("SESSION_EXPIRED", json.RootElement.GetProperty("code").GetString());
            Assert.Equal("Tu sesión fue iniciada en otro dispositivo.", json.RootElement.GetProperty("message").GetString());
        }

        // 4. SESSION_EXPIRED por expiración de reloj (2h exactas -> 200 OK; 2h + 1s -> 401 SESSION_EXPIRED).
        clock.Advance(TimeSpan.FromHours(2));
        using (var exactBoundaryRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", token2))
        {
            Assert.Equal(HttpStatusCode.OK, exactBoundaryRes.StatusCode);
        }

        clock.Advance(TimeSpan.FromSeconds(1));
        using (var expiredRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", token2))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, expiredRes.StatusCode);
            using var json = JsonDocument.Parse(await expiredRes.Content.ReadAsStringAsync());
            Assert.Equal("SESSION_EXPIRED", json.RootElement.GetProperty("code").GetString());
        }

        // 5. ACCOUNT_DISABLED cuando Status != "ACTIVO" o DeletedAt != null durante la sesión.
        var (_, elector1Token, b1) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        b1?.Dispose();
        var (_, elector2Token, b2) = await LoginMobileAsync(client, SecondElectorDoc, SharedPassword);
        b2?.Dispose();

        await factory.SeedAsync(async db =>
        {
            var elector1 = await db.Voters.SingleAsync(v => v.Id == 3);
            elector1.Status = "INACTIVO";

            var elector2 = await db.Voters.SingleAsync(v => v.Id == 4);
            elector2.DeletedAt = clock.GetUtcNow().UtcDateTime;
        });

        using (var inactiveRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", elector1Token))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, inactiveRes.StatusCode);
            using var json = JsonDocument.Parse(await inactiveRes.Content.ReadAsStringAsync());
            Assert.Equal("ACCOUNT_DISABLED", json.RootElement.GetProperty("code").GetString());
        }

        using (var softDeletedRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/profile", elector2Token))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, softDeletedRes.StatusCode);
            using var json = JsonDocument.Parse(await softDeletedRes.Content.ReadAsStringAsync());
            Assert.Equal("ACCOUNT_DISABLED", json.RootElement.GetProperty("code").GetString());
        }
    }

    /// <summary>
    /// RN-4, RN-4.1, RN-5: Verifica <c>GET /api/mobile/events</c> y <c>GET /api/mobile/events/{id}</c>:
    /// filtrado por rol/grado/etapa, <c>404 EVENT_NOT_FOUND</c> (inexistente, <c>ELIMINADO</c>,
    /// <c>PROGRAMADA</c> para elector o grado no habilitado), visibilidad de <c>votes</c>/<c>percentage</c>,
    /// ordenación de candidatos aprobados (<c>"01"</c>, <c>"02"</c>..., <c>"00"</c> voto en blanco),
    /// exclusión de no aprobados, reescritura de URLs multimedia y forma JSON completa.
    /// </summary>
    [Fact]
    public async Task Events_And_EventById_EnforceVisibilityRn4Rn41Rn5_Filtering404_AndFullJsonShape()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);

        var localNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.Local).DateTime;

        await factory.SeedAsync(db =>
        {
            db.ElectionPositions.Add(new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = DateTime.UtcNow });

            db.VotingEvents.AddRange(
                CreateEventForLocalNow(10, "Elección Programada", "PROGRAMADA", localNow),
                CreateEventForLocalNow(11, "Elección Inscripción", "INSCRIPCION", localNow),
                CreateEventForLocalNow(12, "Elección Propuestas", "PROPUESTAS", localNow),
                CreateEventForLocalNow(13, "Elección Activa Sin Voto", "ACTIVA", localNow),
                CreateEventForLocalNow(14, "Elección Activa Con Voto", "ACTIVA", localNow),
                CreateEventForLocalNow(15, "Elección Finalizada Grado 11", "FINALIZADA", localNow),
                CreateEventForLocalNow(16, "Elección Finalizada Otro Grado", "FINALIZADA", localNow),
                CreateEventForLocalNow(17, "Elección Eliminada", "ELIMINADO", localNow, deletedAt: clock.GetUtcNow().UtcDateTime));

            // Elector Id=3 pertenece al grado 11; evento 16 es solo para grado 10.
            db.EventGrades.AddRange(
                new EventGrade { Id = 1, VotingEventId = 10, GradeId = 11 },
                new EventGrade { Id = 2, VotingEventId = 11, GradeId = 11 },
                new EventGrade { Id = 3, VotingEventId = 12, GradeId = 11 },
                new EventGrade { Id = 4, VotingEventId = 13, GradeId = 11 },
                new EventGrade { Id = 5, VotingEventId = 14, GradeId = 11 },
                new EventGrade { Id = 6, VotingEventId = 15, GradeId = 11 },
                new EventGrade { Id = 7, VotingEventId = 16, GradeId = 10 },
                new EventGrade { Id = 8, VotingEventId = 17, GradeId = 11 });

            // Candidatos para evento 14 (mixto: aprobados, no aprobados, voto en blanco y rutas media).
            db.Candidates.AddRange(
                new Candidate
                {
                    Id = 201,
                    VotingEventId = 14,
                    VoterId = 3,
                    Name = "Candidata Uno",
                    Slogan = "Lema Uno",
                    PhotoUrl = "/uploads/candidacies/photo_201.png.enc",
                    GovernmentPlanUrl = "https://cdn.example.com/plan201.pdf",
                    IsBlankVote = false,
                    Status = "APROBADO",
                    EnrolledAt = clock.GetUtcNow().UtcDateTime
                },
                new Candidate
                {
                    Id = 202,
                    VotingEventId = 14,
                    VoterId = 4,
                    Name = "Candidato Dos",
                    Slogan = "Lema Dos",
                    PhotoUrl = "photo_202.jpg.enc",
                    GovernmentPlanUrl = "/uploads/candidacies/plan_202.pdf.enc",
                    IsBlankVote = false,
                    Status = "APROBADO",
                    EnrolledAt = clock.GetUtcNow().UtcDateTime
                },
                new Candidate
                {
                    Id = 203,
                    VotingEventId = 14,
                    VoterId = null,
                    Name = "Candidato Pendiente",
                    IsBlankVote = false,
                    Status = "PENDIENTE",
                    EnrolledAt = clock.GetUtcNow().UtcDateTime
                },
                new Candidate
                {
                    Id = 204,
                    VotingEventId = 14,
                    VoterId = null,
                    Name = "Candidato Rechazado",
                    IsBlankVote = false,
                    Status = "RECHAZADO",
                    EnrolledAt = clock.GetUtcNow().UtcDateTime
                },
                new Candidate
                {
                    Id = 205,
                    VotingEventId = 14,
                    VoterId = null,
                    Name = "Candidato Retirado",
                    IsBlankVote = false,
                    Status = "RETIRADO",
                    EnrolledAt = clock.GetUtcNow().UtcDateTime
                },
                new Candidate
                {
                    Id = 206,
                    VotingEventId = 14,
                    VoterId = null,
                    Name = "Voto en Blanco",
                    Slogan = null,
                    IsBlankVote = true,
                    Status = "APROBADO",
                    EnrolledAt = clock.GetUtcNow().UtcDateTime
                });

            db.CandidateProposals.AddRange(
                new CandidateProposal { Id = 301, CandidateId = 201, DisplayOrder = 2, Content = "Segunda propuesta" },
                new CandidateProposal { Id = 302, CandidateId = 201, DisplayOrder = 1, Content = "Primera propuesta" });

            // Candidatos para evento 13 (ACTIVA sin voto) y 15 (FINALIZADA).
            db.Candidates.AddRange(
                new Candidate { Id = 210, VotingEventId = 13, VoterId = 4, Name = "Candidato 13", IsBlankVote = false, Status = "APROBADO", EnrolledAt = clock.GetUtcNow().UtcDateTime },
                new Candidate { Id = 211, VotingEventId = 15, VoterId = 4, Name = "Candidato 15", IsBlankVote = false, Status = "APROBADO", EnrolledAt = clock.GetUtcNow().UtcDateTime });

            // El elector (Id=3) ya votó en el evento 14.
            db.VoterEventParticipations.Add(new VoterEventParticipation
            {
                Id = 1,
                VoterId = 3,
                VotingEventId = 14,
                ParticipatedAt = clock.GetUtcNow().UtcDateTime
            });
            db.Votes.Add(new Vote
            {
                Id = 1,
                VotingEventId = 14,
                CandidateId = 201,
                VotedAt = clock.GetUtcNow().UtcDateTime,
                VoteHash = Sha256Hex("vote-14-201")
            });

            return Task.CompletedTask;
        });

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        var (_, electorToken, eb) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        eb?.Dispose();
        var (_, adminToken, ab) = await LoginMobileAsync(client, AdminDoc, SharedPassword);
        ab?.Dispose();

        // Sin token -> 401 TOKEN_MISSING.
        using (var unauthList = await client.GetAsync("/api/mobile/events"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, unauthList.StatusCode);
        }

        // 1. Elector: GET /api/mobile/events excluye PROGRAMADA (10), otro grado (16) y ELIMINADO (17).
        using (var electorListRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events", electorToken))
        {
            Assert.Equal(HttpStatusCode.OK, electorListRes.StatusCode);
            using var json = JsonDocument.Parse(await electorListRes.Content.ReadAsStringAsync());
            var ids = json.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToHashSet();
            Assert.Equal(new HashSet<string?> { "11", "12", "13", "14", "15" }, ids);

            foreach (var ev in json.RootElement.EnumerateArray())
            {
                var status = ev.GetProperty("status").GetString();
                var hasVoted = ev.GetProperty("hasVoted").GetBoolean();
                if (status is "INSCRIPCION" or "PROPUESTAS" || (status == "ACTIVA" && !hasVoted))
                {
                    foreach (var cand in ev.GetProperty("candidates").EnumerateArray())
                    {
                        Assert.Equal(0, cand.GetProperty("votes").GetInt32());
                        Assert.Equal(0f, cand.GetProperty("percentage").GetSingle());
                    }
                }
            }
        }

        // 2. Elector: GET /api/mobile/events/14 (ACTIVA + hasVoted == true) -> forma JSON completa.
        using (var detail14Res = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events/14", electorToken))
        {
            Assert.Equal(HttpStatusCode.OK, detail14Res.StatusCode);
            using var json = JsonDocument.Parse(await detail14Res.Content.ReadAsStringAsync());
            var ev = json.RootElement;

            Assert.Equal("14", ev.GetProperty("id").GetString());
            Assert.Equal("Elección Activa Con Voto", ev.GetProperty("title").GetString());
            Assert.Equal("Descripción Elección Activa Con Voto", ev.GetProperty("description").GetString());
            Assert.Equal("PERSONAS", ev.GetProperty("electionType").GetString());
            Assert.Equal("ACTIVA", ev.GetProperty("status").GetString());
            Assert.True(ev.GetProperty("hasVoted").GetBoolean());
            Assert.True(ev.GetProperty("registrationStartDate").GetInt64() > 0);
            Assert.True(ev.GetProperty("registrationEndDate").GetInt64() > 0);
            Assert.True(ev.GetProperty("proposalsStartDate").GetInt64() > 0);
            Assert.True(ev.GetProperty("proposalsEndDate").GetInt64() > 0);
            Assert.True(ev.GetProperty("votingStartDate").GetInt64() > 0);
            Assert.True(ev.GetProperty("votingEndDate").GetInt64() > 0);

            var candidates = ev.GetProperty("candidates").EnumerateArray().ToList();
            Assert.Equal(3, candidates.Count);

            // Orden: 201 ("01"), 202 ("02"), 206 voto en blanco ("00").
            var c1 = candidates[0];
            Assert.Equal("201", c1.GetProperty("id").GetString());
            Assert.Equal("01", c1.GetProperty("number").GetString());
            Assert.Equal("11°", c1.GetProperty("grade").GetString());
            Assert.Equal("Lema Uno", c1.GetProperty("slogan").GetString());
            Assert.Equal("Primera propuesta", c1.GetProperty("summary").GetString());
            Assert.False(c1.GetProperty("isWhiteVote").GetBoolean());
            Assert.Equal("APROBADO", c1.GetProperty("status").GetString());
            Assert.Equal(
                $"/api/mobile/media?path={Uri.EscapeDataString("/uploads/candidacies/photo_201.png.enc")}",
                c1.GetProperty("photoUrl").GetString());
            Assert.Equal("https://cdn.example.com/plan201.pdf", c1.GetProperty("pdfUrl").GetString());

            var proposals = c1.GetProperty("proposals").EnumerateArray().ToList();
            Assert.Equal(2, proposals.Count);
            Assert.Equal("302", proposals[0].GetProperty("id").GetString());
            Assert.Equal("Primera propuesta", proposals[0].GetProperty("text").GetString());
            Assert.Equal("301", proposals[1].GetProperty("id").GetString());
            Assert.Equal("Segunda propuesta", proposals[1].GetProperty("text").GetString());

            var c2 = candidates[1];
            Assert.Equal("202", c2.GetProperty("id").GetString());
            Assert.Equal("02", c2.GetProperty("number").GetString());
            Assert.Equal("Lema Dos", c2.GetProperty("summary").GetString());
            Assert.Equal(
                $"/api/mobile/media?path={Uri.EscapeDataString("photo_202.jpg.enc")}",
                c2.GetProperty("photoUrl").GetString());
            Assert.Equal(
                $"/api/mobile/media?path={Uri.EscapeDataString("/uploads/candidacies/plan_202.pdf.enc")}",
                c2.GetProperty("pdfUrl").GetString());

            var blank = candidates[2];
            Assert.Equal("206", blank.GetProperty("id").GetString());
            Assert.Equal("00", blank.GetProperty("number").GetString());
            Assert.Equal("N/A", blank.GetProperty("grade").GetString());
            Assert.True(blank.GetProperty("isWhiteVote").GetBoolean());
            Assert.Equal("Voto institucional de neutralidad.", blank.GetProperty("summary").GetString());
            Assert.Equal("Opción de desacuerdo o abstención consciente", blank.GetProperty("slogan").GetString());
            Assert.Equal(JsonValueKind.Null, blank.GetProperty("photoUrl").ValueKind);
            Assert.Equal(JsonValueKind.Null, blank.GetProperty("pdfUrl").ValueKind);

            // NOTA: En EF Core InMemory, la vista sin clave VwVoteCounts (.HasNoKey().ToView("vw_vote_counts"))
            // devuelve 0 filas sin lanzar excepción; por ello votes=0 y percentage=0 aun cuando canSeeCounts es true
            // (ACTIVA con hasVoted, FINALIZADA o ADMIN). El conteo real de la vista MySQL se verifica en ViewsIntegrationTests.
            Assert.Equal(0, c1.GetProperty("votes").GetInt32());
            Assert.Equal(0f, c1.GetProperty("percentage").GetSingle());
        }

        // 3. Elector: acceso permitido a FINALIZADA del mismo grado (15) y ACTIVA sin voto (13).
        Assert.Equal(HttpStatusCode.OK, (await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events/13", electorToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events/15", electorToken)).StatusCode);

        // 4. Elector: 404 EVENT_NOT_FOUND para inexistente, ELIMINADO (17), PROGRAMADA (10) y otro grado (16).
        foreach (var hiddenId in new[] { 9999, 17, 10, 16 })
        {
            using var notFoundRes = await SendWithBearerAsync(client, HttpMethod.Get, $"/api/mobile/events/{hiddenId}", electorToken);
            Assert.Equal(HttpStatusCode.NotFound, notFoundRes.StatusCode);
            using var json = JsonDocument.Parse(await notFoundRes.Content.ReadAsStringAsync());
            Assert.Equal("EVENT_NOT_FOUND", json.RootElement.GetProperty("code").GetString());
        }

        // 5. ADMIN (RN-5): ve todas las etapas no eliminadas sin filtro de grado (incluyendo PROGRAMADA 10 y grado 10 evento 16),
        // pero ELIMINADO (17) sigue devolviendo 404 EVENT_NOT_FOUND.
        using (var adminListRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events", adminToken))
        {
            Assert.Equal(HttpStatusCode.OK, adminListRes.StatusCode);
            using var json = JsonDocument.Parse(await adminListRes.Content.ReadAsStringAsync());
            var adminIds = json.RootElement.EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToHashSet();
            Assert.Equal(new HashSet<string?> { "10", "11", "12", "13", "14", "15", "16" }, adminIds);
        }

        Assert.Equal(HttpStatusCode.OK, (await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events/10", adminToken)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events/16", adminToken)).StatusCode);
        using (var adminDeletedRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events/17", adminToken))
        {
            Assert.Equal(HttpStatusCode.NotFound, adminDeletedRes.StatusCode);
            using var json = JsonDocument.Parse(await adminDeletedRes.Content.ReadAsStringAsync());
            Assert.Equal("EVENT_NOT_FOUND", json.RootElement.GetProperty("code").GetString());
        }
    }

    /// <summary>
    /// Verifica la serialización de los 6 campos de fecha/hora en milisegundos Unix producidos por
    /// <c>MobileApiController.ToEpochMillis</c> en <c>GET /api/mobile/events/{id}</c>.
    /// </summary>
    [Fact]
    public async Task Events_ToEpochMillis_SerializesBoundariesUsingLocalDateTimeKind_DocumentedGap()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);

        var regStart = (new DateOnly(2026, 5, 10), new TimeOnly(8, 15));
        var regEnd = (new DateOnly(2026, 5, 11), new TimeOnly(17, 45));
        var propStart = (new DateOnly(2026, 5, 12), new TimeOnly(9, 0));
        var propEnd = (new DateOnly(2026, 5, 13), new TimeOnly(18, 30));
        var votStart = (new DateOnly(2026, 5, 14), new TimeOnly(7, 0));
        var votEnd = (new DateOnly(2026, 5, 15), new TimeOnly(16, 20));

        await factory.SeedAsync(db =>
        {
            db.ElectionPositions.Add(new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = DateTime.UtcNow });
            db.VotingEvents.Add(new VotingEvent
            {
                Id = 77,
                Title = "Elección Epoch Test",
                ElectionType = "PERSONAS",
                PositionId = 1,
                Status = "FINALIZADA",
                CreatedByVoterId = 2,
                RegistrationStartDate = regStart.Item1,
                RegistrationStartTime = regStart.Item2,
                RegistrationEndDate = regEnd.Item1,
                RegistrationEndTime = regEnd.Item2,
                ProposalsStartDate = propStart.Item1,
                ProposalsStartTime = propStart.Item2,
                ProposalsEndDate = propEnd.Item1,
                ProposalsEndTime = propEnd.Item2,
                VotingStartDate = votStart.Item1,
                VotingStartTime = votStart.Item2,
                VotingEndDate = votEnd.Item1,
                VotingEndTime = votEnd.Item2,
                CreatedAt = clock.GetUtcNow().UtcDateTime
            });
            return Task.CompletedTask;
        });

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        var (_, adminToken, ab) = await LoginMobileAsync(client, AdminDoc, SharedPassword);
        ab?.Dispose();

        using var res = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events/77", adminToken);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        // NOTA: Hallazgo fuera de alcance (R3): MobileApiController.ToEpochMillis convierte DateOnly+TimeOnly
        // usando DateTimeKind.Local (zona horaria local del host) en lugar de UTC o una zona horaria institucional explícita.
        // La aserción valida el contrato vigente contra DateTimeKind.Local.
        static long ExpectedEpochMillis(DateOnly d, TimeOnly t) =>
            new DateTimeOffset(d.ToDateTime(t, DateTimeKind.Local)).ToUnixTimeMilliseconds();

        using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var ev = json.RootElement;
        Assert.Equal(ExpectedEpochMillis(regStart.Item1, regStart.Item2), ev.GetProperty("registrationStartDate").GetInt64());
        Assert.Equal(ExpectedEpochMillis(regEnd.Item1, regEnd.Item2), ev.GetProperty("registrationEndDate").GetInt64());
        Assert.Equal(ExpectedEpochMillis(propStart.Item1, propStart.Item2), ev.GetProperty("proposalsStartDate").GetInt64());
        Assert.Equal(ExpectedEpochMillis(propEnd.Item1, propEnd.Item2), ev.GetProperty("proposalsEndDate").GetInt64());
        Assert.Equal(ExpectedEpochMillis(votStart.Item1, votStart.Item2), ev.GetProperty("votingStartDate").GetInt64());
        Assert.Equal(ExpectedEpochMillis(votEnd.Item1, votEnd.Item2), ev.GetProperty("votingEndDate").GetInt64());
    }

    /// <summary>
    /// RF-M05-00, RN-4: <c>POST /api/mobile/vote</c> registra exitosamente un voto hacia un candidato
    /// <c>APROBADO</c> o hacia el voto en blanco (<c>IsBlankVote = true</c>), persiste el voto anónimo
    /// (sin columna <c>VoterId</c>) y la participación en <see cref="VoterEventParticipation"/>,
    /// actualizando <c>hasVoted = true</c> en <c>GET /api/mobile/events/{id}</c>.
    /// </summary>
    [Fact]
    public async Task Vote_ApprovedCandidateAndBlankVote_Return200_PersistAnonymousVoteAndParticipation()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);

        var localNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.Local).DateTime;
        await factory.SeedAsync(db =>
        {
            db.ElectionPositions.Add(new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = DateTime.UtcNow });
            db.VotingEvents.Add(CreateEventForLocalNow(20, "Elección Activa Votación", "ACTIVA", localNow));
            db.EventGrades.Add(new EventGrade { Id = 20, VotingEventId = 20, GradeId = 11 });
            db.Candidates.AddRange(
                new Candidate { Id = 401, VotingEventId = 20, VoterId = 3, Name = "Candidato Nominal", IsBlankVote = false, Status = "APROBADO", EnrolledAt = clock.GetUtcNow().UtcDateTime },
                new Candidate { Id = 402, VotingEventId = 20, VoterId = null, Name = "Voto en Blanco", IsBlankVote = true, Status = "APROBADO", EnrolledAt = clock.GetUtcNow().UtcDateTime });
            return Task.CompletedTask;
        });

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        var (_, elector1Token, b1) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        b1?.Dispose();
        var (_, elector2Token, b2) = await LoginMobileAsync(client, SecondElectorDoc, SharedPassword);
        b2?.Dispose();

        // 1. Elector 1 vota por candidato nominal APROBADO (401).
        using (var vote1Res = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/vote", elector1Token, new { eventId = 20, candidateId = 401 }))
        {
            Assert.Equal(HttpStatusCode.OK, vote1Res.StatusCode);
            using var json = JsonDocument.Parse(await vote1Res.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
            Assert.Equal("¡Tu voto ha sido registrado exitosamente!", json.RootElement.GetProperty("message").GetString());
        }

        // 2. Elector 2 vota por Voto en Blanco (402).
        using (var vote2Res = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/vote", elector2Token, new { eventId = 20, candidateId = 402 }))
        {
            Assert.Equal(HttpStatusCode.OK, vote2Res.StatusCode);
            using var json = JsonDocument.Parse(await vote2Res.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("success").GetBoolean());
        }

        // Verificar anonimato del modelo Vote (sin propiedad VoterId) y persistencia en BD.
        Assert.Null(typeof(Vote).GetProperty("VoterId"));
        var votesCount = await factory.QueryDbAsync(db => db.Votes.CountAsync(v => v.VotingEventId == 20));
        var participations = await factory.QueryDbAsync(db =>
            db.VoterEventParticipations.Where(p => p.VotingEventId == 20).Select(p => p.VoterId).ToListAsync());
        Assert.Equal(2, votesCount);
        Assert.Equal(new HashSet<uint> { 3u, 4u }, participations.ToHashSet());

        // Verificar que hasVoted pasó a true en /events/20.
        using (var afterEventRes = await SendWithBearerAsync(client, HttpMethod.Get, "/api/mobile/events/20", elector1Token))
        {
            Assert.Equal(HttpStatusCode.OK, afterEventRes.StatusCode);
            using var json = JsonDocument.Parse(await afterEventRes.Content.ReadAsStringAsync());
            Assert.True(json.RootElement.GetProperty("hasVoted").GetBoolean());
        }
    }

    /// <summary>
    /// RF-M05-00: Verifica todos los códigos de rechazo de <c>POST /api/mobile/vote</c>:
    /// <c>400 INVALID_REQUEST</c>, <c>403 ROLE_NOT_ALLOWED</c> (<c>ADMIN</c>/<c>SUPER_ADMIN</c>),
    /// <c>404 EVENT_NOT_FOUND</c> (inexistente, <c>ELIMINADO</c>, <c>PROGRAMADA</c>, otro grado),
    /// <c>400 EVENT_NOT_ACTIVE</c> (<c>INSCRIPCION</c>/<c>PROPUESTAS</c>/<c>FINALIZADA</c>),
    /// <c>400 INVALID_CANDIDATE</c> (<c>PENDIENTE</c>/<c>RECHAZADO</c>/<c>RETIRADO</c>/otro evento/inexistente)
    /// y <c>409 ALREADY_VOTED</c>.
    /// </summary>
    [Fact]
    public async Task Vote_RejectsInvalidRequest_AdminRole_MissingOrInactiveEvent_InvalidCandidate_AndDoubleVote409()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);

        var localNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.Local).DateTime;
        await factory.SeedAsync(db =>
        {
            db.ElectionPositions.Add(new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = DateTime.UtcNow });

            db.VotingEvents.AddRange(
                CreateEventForLocalNow(30, "Programada", "PROGRAMADA", localNow),
                CreateEventForLocalNow(31, "Inscripción", "INSCRIPCION", localNow),
                CreateEventForLocalNow(32, "Propuestas", "PROPUESTAS", localNow),
                CreateEventForLocalNow(33, "Finalizada", "FINALIZADA", localNow),
                CreateEventForLocalNow(34, "Eliminada", "ELIMINADO", localNow, deletedAt: clock.GetUtcNow().UtcDateTime),
                CreateEventForLocalNow(35, "Otro Grado", "ACTIVA", localNow),
                CreateEventForLocalNow(36, "Activa Principal", "ACTIVA", localNow));

            db.EventGrades.AddRange(
                new EventGrade { Id = 30, VotingEventId = 30, GradeId = 11 },
                new EventGrade { Id = 31, VotingEventId = 31, GradeId = 11 },
                new EventGrade { Id = 32, VotingEventId = 32, GradeId = 11 },
                new EventGrade { Id = 33, VotingEventId = 33, GradeId = 11 },
                new EventGrade { Id = 34, VotingEventId = 34, GradeId = 11 },
                new EventGrade { Id = 35, VotingEventId = 35, GradeId = 10 },
                new EventGrade { Id = 36, VotingEventId = 36, GradeId = 11 });

            db.Candidates.AddRange(
                new Candidate { Id = 501, VotingEventId = 36, VoterId = 3, Name = "Aprobado 36", IsBlankVote = false, Status = "APROBADO", EnrolledAt = clock.GetUtcNow().UtcDateTime },
                new Candidate { Id = 502, VotingEventId = 36, VoterId = null, Name = "Pendiente 36", IsBlankVote = false, Status = "PENDIENTE", EnrolledAt = clock.GetUtcNow().UtcDateTime },
                new Candidate { Id = 503, VotingEventId = 36, VoterId = null, Name = "Rechazado 36", IsBlankVote = false, Status = "RECHAZADO", EnrolledAt = clock.GetUtcNow().UtcDateTime },
                new Candidate { Id = 504, VotingEventId = 36, VoterId = null, Name = "Retirado 36", IsBlankVote = false, Status = "RETIRADO", EnrolledAt = clock.GetUtcNow().UtcDateTime },
                new Candidate { Id = 505, VotingEventId = 35, VoterId = null, Name = "Aprobado Otro Evento", IsBlankVote = false, Status = "APROBADO", EnrolledAt = clock.GetUtcNow().UtcDateTime });

            return Task.CompletedTask;
        });

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        var (_, electorToken, eb) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        eb?.Dispose();
        var (_, adminToken, ab) = await LoginMobileAsync(client, AdminDoc, SharedPassword);
        ab?.Dispose();
        var (_, superAdminToken, sb) = await LoginMobileAsync(client, SuperAdminDoc, SharedPassword);
        sb?.Dispose();

        // 1. 400 INVALID_REQUEST
        foreach (var badPayload in new object?[] { new { eventId = 0, candidateId = 501 }, new { eventId = 36, candidateId = 0 }, null })
        {
            using var res = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/vote", electorToken, badPayload);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("INVALID_REQUEST", json.RootElement.GetProperty("code").GetString());
        }

        // 2. 403 ROLE_NOT_ALLOWED para ADMIN y SUPER_ADMIN
        foreach (var privilegedToken in new[] { adminToken, superAdminToken })
        {
            using var res = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/vote", privilegedToken, new { eventId = 36, candidateId = 501 });
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("ROLE_NOT_ALLOWED", json.RootElement.GetProperty("code").GetString());
        }

        // 3. 404 EVENT_NOT_FOUND (inexistente, PROGRAMADA, ELIMINADO, otro grado)
        foreach (var missingEventId in new[] { 9999, 30, 34, 35 })
        {
            using var res = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/vote", electorToken, new { eventId = missingEventId, candidateId = 501 });
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("EVENT_NOT_FOUND", json.RootElement.GetProperty("code").GetString());
        }

        // 4. 400 EVENT_NOT_ACTIVE (INSCRIPCION, PROPUESTAS, FINALIZADA)
        foreach (var nonActiveEventId in new[] { 31, 32, 33 })
        {
            using var res = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/vote", electorToken, new { eventId = nonActiveEventId, candidateId = 501 });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("EVENT_NOT_ACTIVE", json.RootElement.GetProperty("code").GetString());
        }

        // 5. 400 INVALID_CANDIDATE (PENDIENTE, RECHAZADO, RETIRADO, otro evento, inexistente)
        foreach (var invalidCandidateId in new[] { 502, 503, 504, 505, 9999 })
        {
            using var res = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/vote", electorToken, new { eventId = 36, candidateId = invalidCandidateId });
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("INVALID_CANDIDATE", json.RootElement.GetProperty("code").GetString());
        }

        // 6. Voto válido (200 OK) seguido de reintento (409 ALREADY_VOTED)
        using (var firstVote = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/vote", electorToken, new { eventId = 36, candidateId = 501 }))
        {
            Assert.Equal(HttpStatusCode.OK, firstVote.StatusCode);
        }

        using (var duplicateVote = await SendWithBearerAsync(client, HttpMethod.Post, "/api/mobile/vote", electorToken, new { eventId = 36, candidateId = 501 }))
        {
            Assert.Equal(HttpStatusCode.Conflict, duplicateVote.StatusCode);
            using var json = JsonDocument.Parse(await duplicateVote.Content.ReadAsStringAsync());
            Assert.Equal("ALREADY_VOTED", json.RootElement.GetProperty("code").GetString());
        }
    }

    /// <summary>
    /// Verifica <c>GET /api/mobile/media</c> para archivos <c>.enc</c> cifrados:
    /// descifra y entrega exactamente los bytes originales con el <c>Content-Type</c> correspondiente
    /// (<c>image/png</c>, <c>image/jpeg</c>, <c>image/webp</c>, <c>application/pdf</c>)
    /// para <c>photo_</c>/<c>plan_</c> como <c>ELECTOR</c> y <c>doc_</c> como <c>ADMIN</c>/<c>SUPER_ADMIN</c>.
    /// </summary>
    [Fact]
    public async Task Media_ValidEncryptedFiles_Return200WithDecryptedBytesAndExactContentTypes()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3 };
        var jpgBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 4, 5, 6 };
        var jpegBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE1, 7, 8, 9 };
        var webpBytes = Encoding.ASCII.GetBytes("RIFF\0\0\0\0WEBP");
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%test-plan\n%%EOF");
        var docPdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n%admin-only-doc\n%%EOF");

        await WriteEncryptedMediaAsync(factory, "photo_cand.png.enc", pngBytes);
        await WriteEncryptedMediaAsync(factory, "photo_cand.jpg.enc", jpgBytes);
        await WriteEncryptedMediaAsync(factory, "photo_cand.jpeg.enc", jpegBytes);
        await WriteEncryptedMediaAsync(factory, "photo_cand.webp.enc", webpBytes);
        await WriteEncryptedMediaAsync(factory, "plan_cand.pdf.enc", pdfBytes);
        await WriteEncryptedMediaAsync(factory, "doc_identity.pdf.enc", docPdfBytes);

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        var (_, electorToken, eb) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        eb?.Dispose();
        var (_, adminToken, ab) = await LoginMobileAsync(client, AdminDoc, SharedPassword);
        ab?.Dispose();
        var (_, superAdminToken, sb) = await LoginMobileAsync(client, SuperAdminDoc, SharedPassword);
        sb?.Dispose();

        var electorCases = new[]
        {
            ("/uploads/candidacies/photo_cand.png.enc", "image/png", pngBytes),
            ("photo_cand.jpg.enc", "image/jpeg", jpgBytes),
            ("photo_cand.jpeg.enc", "image/jpeg", jpegBytes),
            ("photo_cand.webp.enc", "image/webp", webpBytes),
            ("/uploads/candidacies/plan_cand.pdf.enc", "application/pdf", pdfBytes)
        };

        foreach (var (path, expectedContentType, expectedBytes) in electorCases)
        {
            using var res = await SendWithBearerAsync(
                client,
                HttpMethod.Get,
                $"/api/mobile/media?path={Uri.EscapeDataString(path)}",
                electorToken);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            Assert.Equal(expectedContentType, res.Content.Headers.ContentType?.MediaType);
            Assert.Equal(expectedBytes, await res.Content.ReadAsByteArrayAsync());
        }

        foreach (var privilegedToken in new[] { adminToken, superAdminToken })
        {
            using var docRes = await SendWithBearerAsync(
                client,
                HttpMethod.Get,
                $"/api/mobile/media?path={Uri.EscapeDataString("doc_identity.pdf.enc")}",
                privilegedToken);
            Assert.Equal(HttpStatusCode.OK, docRes.StatusCode);
            Assert.Equal("application/pdf", docRes.Content.Headers.ContentType?.MediaType);
            Assert.Equal(docPdfBytes, await docRes.Content.ReadAsByteArrayAsync());
        }
    }

    /// <summary>
    /// Verifica los rechazos de <c>GET /api/mobile/media</c>: <c>401 TOKEN_MISSING</c> sin autenticación,
    /// <c>400 INVALID_MEDIA_PATH</c>, <c>400 INVALID_MEDIA_FILE</c> (no <c>.enc</c>),
    /// <c>403 FORBIDDEN</c> cuando <c>ELECTOR</c> solicita <c>doc_*</c>, y <c>404 MEDIA_NOT_FOUND</c>
    /// incluyendo intentos de path traversal neutralizados mediante <see cref="Path.GetFileName(string?)"/>.
    /// </summary>
    [Fact]
    public async Task Media_RejectsUnauthenticated_InvalidPathOrExtension_ElectorDocPrefix403_AndPathTraversal404()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 12, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory, clock);

        await WriteEncryptedMediaAsync(factory, "doc_secret.pdf.enc", Encoding.ASCII.GetBytes("%PDF-secret"));

        // Archivo .enc colocado fuera de candidacies/ (en CandidacyUploadsBasePath) para probar que
        // ../../photo_outside.png.enc no puede escapar de la carpeta candidacies/.
        await factory.ExecuteWithServicesAsync(async sp =>
        {
            var encryptor = sp.GetRequiredService<IDocumentEncryptionService>();
            var outsideCipher = encryptor.Encrypt(Convert.ToBase64String(new byte[] { 1, 2, 3, 4 }));
            await File.WriteAllTextAsync(Path.Combine(factory.CandidacyUploadsBasePath, "photo_outside.png.enc"), outsideCipher);
        });

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        var (_, electorToken, eb) = await LoginMobileAsync(client, ElectorDoc, SharedPassword);
        eb?.Dispose();

        // 1. 401 sin autenticación
        using (var unauth = await client.GetAsync("/api/mobile/media?path=photo_1.png.enc"))
        {
            Assert.Equal(HttpStatusCode.Unauthorized, unauth.StatusCode);
            using var json = JsonDocument.Parse(await unauth.Content.ReadAsStringAsync());
            Assert.Equal("TOKEN_MISSING", json.RootElement.GetProperty("code").GetString());
        }

        // 2. 400 INVALID_MEDIA_PATH
        foreach (var emptyUrl in new[] { "/api/mobile/media", "/api/mobile/media?path=%20%20" })
        {
            using var res = await SendWithBearerAsync(client, HttpMethod.Get, emptyUrl, electorToken);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("INVALID_MEDIA_PATH", json.RootElement.GetProperty("code").GetString());
        }

        // 3. 400 INVALID_MEDIA_FILE (sin extensión .enc, incluso con path traversal)
        foreach (var badFile in new[] { "photo_1.png", "../../appsettings.json", "/uploads/candidacies/" })
        {
            using var res = await SendWithBearerAsync(
                client,
                HttpMethod.Get,
                $"/api/mobile/media?path={Uri.EscapeDataString(badFile)}",
                electorToken);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("INVALID_MEDIA_FILE", json.RootElement.GetProperty("code").GetString());
        }

        // 4. 403 FORBIDDEN cuando ELECTOR solicita doc_* (o cualquier prefijo distinto de photo_/plan_)
        foreach (var forbiddenPath in new[] { "doc_secret.pdf.enc", "/uploads/candidacies/doc_secret.pdf.enc", "other_file.png.enc" })
        {
            using var res = await SendWithBearerAsync(
                client,
                HttpMethod.Get,
                $"/api/mobile/media?path={Uri.EscapeDataString(forbiddenPath)}",
                electorToken);
            Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("FORBIDDEN", json.RootElement.GetProperty("code").GetString());
        }

        // 5. 404 MEDIA_NOT_FOUND para inexistentes y path traversal confinado a candidacies/
        foreach (var missingPath in new[] { "photo_missing.png.enc", "../../photo_outside.png.enc", @"..\..\photo_outside.png.enc" })
        {
            using var res = await SendWithBearerAsync(
                client,
                HttpMethod.Get,
                $"/api/mobile/media?path={Uri.EscapeDataString(missingPath)}",
                electorToken);
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
            using var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            Assert.Equal("MEDIA_NOT_FOUND", json.RootElement.GetProperty("code").GetString());
        }
    }

    private static async Task SeedStandardActorsAsync(WahlMiraiWebFactory factory, FakeTimeProvider clock)
    {
        await factory.ExecuteWithServicesAsync(async sp =>
        {
            var db = sp.GetRequiredService<WahlMiraiDbContext>();
            var encryptor = sp.GetRequiredService<IDocumentEncryptionService>();

            db.Roles.AddRange(
                new Role { Id = Roles.Elector, Name = Roles.ElectorName },
                new Role { Id = Roles.Admin, Name = Roles.AdminName },
                new Role { Id = Roles.SuperAdmin, Name = Roles.SuperAdminName });

            db.Grades.AddRange(
                new Grade { Id = 10, Name = "10°", SequenceOrder = 5, IsLastGrade = false },
                new Grade { Id = 11, Name = "11°", SequenceOrder = 6, IsLastGrade = true });

            var hash = BCrypt.Net.BCrypt.HashPassword(SharedPassword, workFactor: 4);
            var now = clock.GetUtcNow().UtcDateTime;

            db.Voters.AddRange(
                new Voter
                {
                    Id = 1,
                    RoleId = Roles.SuperAdmin,
                    GradeId = null,
                    DocumentHash = Sha256Hex(SuperAdminDoc),
                    EncryptedDocument = encryptor.Encrypt(SuperAdminDoc),
                    FullName = "Super Admin",
                    ContactEmail = "superadmin@example.com",
                    PasswordHash = hash,
                    Status = "ACTIVO",
                    RegisteredAt = now
                },
                new Voter
                {
                    Id = 2,
                    RoleId = Roles.Admin,
                    GradeId = null,
                    DocumentHash = Sha256Hex(AdminDoc),
                    EncryptedDocument = encryptor.Encrypt(AdminDoc),
                    FullName = "Admin Electoral",
                    ContactEmail = "admin@example.com",
                    PasswordHash = hash,
                    Status = "ACTIVO",
                    RegisteredAt = now
                },
                new Voter
                {
                    Id = 3,
                    RoleId = Roles.Elector,
                    GradeId = 11,
                    DocumentHash = Sha256Hex(ElectorDoc),
                    EncryptedDocument = encryptor.Encrypt(ElectorDoc),
                    FullName = "Elector Activo",
                    ContactEmail = "elector@example.com",
                    PasswordHash = hash,
                    Status = "ACTIVO",
                    RegisteredAt = now
                },
                new Voter
                {
                    Id = 4,
                    RoleId = Roles.Elector,
                    GradeId = 11,
                    DocumentHash = Sha256Hex(SecondElectorDoc),
                    EncryptedDocument = encryptor.Encrypt(SecondElectorDoc),
                    FullName = "Segundo Elector",
                    ContactEmail = "elector2@example.com",
                    PasswordHash = hash,
                    Status = "ACTIVO",
                    RegisteredAt = now
                });

            await db.SaveChangesAsync();
        });
    }

    private static async Task WriteEncryptedMediaAsync(WahlMiraiWebFactory factory, string fileName, byte[] rawBytes)
    {
        await factory.ExecuteWithServicesAsync(async sp =>
        {
            var encryptor = sp.GetRequiredService<IDocumentEncryptionService>();
            var cipher = encryptor.Encrypt(Convert.ToBase64String(rawBytes));
            var fullPath = Path.Combine(factory.CandidacyUploadsBasePath, "candidacies", fileName);
            await File.WriteAllTextAsync(fullPath, cipher);
        });
    }

    private static async Task<(HttpResponseMessage Response, string? Token, JsonDocument? Body)> LoginMobileAsync(
        HttpClient client,
        string document,
        string password,
        string? remoteIp = null)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/mobile/login")
        {
            Content = JsonContent.Create(new { document, password })
        };
        if (!string.IsNullOrEmpty(remoteIp))
        {
            req.Headers.Add("X-Test-Remote-Ip", remoteIp);
        }

        var response = await client.SendAsync(req);
        var raw = await response.Content.ReadAsStringAsync();
        JsonDocument? json = null;
        string? token = null;

        if (!string.IsNullOrWhiteSpace(raw) && raw.TrimStart().StartsWith('{'))
        {
            json = JsonDocument.Parse(raw);
            if (json.RootElement.TryGetProperty("token", out var tokenProp))
            {
                token = tokenProp.GetString();
            }
        }

        return (response, token, json);
    }

    private static async Task<HttpResponseMessage> SendWithBearerAsync(
        HttpClient client,
        HttpMethod method,
        string url,
        string? token,
        object? jsonBody = null)
    {
        using var req = new HttpRequestMessage(method, url);
        if (token != null)
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (jsonBody != null)
        {
            req.Content = JsonContent.Create(jsonBody);
        }
        else if (method == HttpMethod.Post && url.EndsWith("/vote", StringComparison.OrdinalIgnoreCase))
        {
            req.Content = JsonContent.Create<object?>(null);
        }

        return await client.SendAsync(req);
    }

    private static VotingEvent CreateEventForLocalNow(
        uint id,
        string title,
        string stage,
        DateTime localNow,
        DateTime? deletedAt = null)
    {
        DateTime regStart, regEnd, propStart, propEnd, votStart, votEnd;
        switch (stage)
        {
            case "PROGRAMADA":
                regStart = localNow.AddHours(1);
                regEnd = localNow.AddHours(2);
                propStart = localNow.AddHours(2);
                propEnd = localNow.AddHours(3);
                votStart = localNow.AddHours(3);
                votEnd = localNow.AddHours(4);
                break;
            case "INSCRIPCION":
                regStart = localNow.AddHours(-1);
                regEnd = localNow.AddHours(1);
                propStart = localNow.AddHours(1);
                propEnd = localNow.AddHours(2);
                votStart = localNow.AddHours(2);
                votEnd = localNow.AddHours(3);
                break;
            case "PROPUESTAS":
                regStart = localNow.AddHours(-2);
                regEnd = localNow.AddHours(-1);
                propStart = localNow.AddHours(-1);
                propEnd = localNow.AddHours(1);
                votStart = localNow.AddHours(1);
                votEnd = localNow.AddHours(2);
                break;
            case "FINALIZADA":
                regStart = localNow.AddHours(-4);
                regEnd = localNow.AddHours(-3);
                propStart = localNow.AddHours(-3);
                propEnd = localNow.AddHours(-2);
                votStart = localNow.AddHours(-2);
                votEnd = localNow.AddHours(-1);
                break;
            default: // ACTIVA / ELIMINADO
                regStart = localNow.AddHours(-3);
                regEnd = localNow.AddHours(-2);
                propStart = localNow.AddHours(-2);
                propEnd = localNow.AddHours(-1);
                votStart = localNow.AddHours(-1);
                votEnd = localNow.AddHours(1);
                break;
        }

        return new VotingEvent
        {
            Id = id,
            Title = title,
            Description = $"Descripción {title}",
            ElectionType = "PERSONAS",
            PositionId = 1,
            Status = stage,
            DeletedAt = deletedAt,
            CreatedByVoterId = 2,
            CreatedAt = DateTime.UtcNow,
            RegistrationStartDate = DateOnly.FromDateTime(regStart),
            RegistrationStartTime = TimeOnly.FromDateTime(regStart),
            RegistrationEndDate = DateOnly.FromDateTime(regEnd),
            RegistrationEndTime = TimeOnly.FromDateTime(regEnd),
            ProposalsStartDate = DateOnly.FromDateTime(propStart),
            ProposalsStartTime = TimeOnly.FromDateTime(propStart),
            ProposalsEndDate = DateOnly.FromDateTime(propEnd),
            ProposalsEndTime = TimeOnly.FromDateTime(propEnd),
            VotingStartDate = DateOnly.FromDateTime(votStart),
            VotingStartTime = TimeOnly.FromDateTime(votStart),
            VotingEndDate = DateOnly.FromDateTime(votEnd),
            VotingEndTime = TimeOnly.FromDateTime(votEnd)
        };
    }

    private static string Sha256Hex(string input)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
