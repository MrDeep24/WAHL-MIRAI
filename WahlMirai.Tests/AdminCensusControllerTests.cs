// ============================================================
// Archivo:       AdminCensusControllerTests.cs
// Módulo:        M02 / M06 — Censo, Lista Blanca, Carga CSV y Promoción (AdminCensusController)
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias para AdminCensusController y CensusService:
//                CargaCsv RFC 4180 (BOM UTF-8, delimitador ';' y ',', comillas
//                escapadas y saltos de línea embebidos, alias de columnas, duplicados
//                en CSV/lista blanca/usuarios, truncamiento >10 errores), CRUD e
//                inmutabilidad de lista blanca reclamada, ciclo de vida de electores
//                (EditVoter, DeleteVoter, RestoreVoter) e invalidación de ActiveSessionStore
//                (RN-14), ResetPassword con EmailQueue REASIGNACION_ADMIN, promoción,
//                migración documental y documentación CSRF (R3).
// Requerimientos: RF-M02-00, RF-M02-01, RF-M02-02, RF-M06-00, RF-M06-01, RN-14.
// ============================================================
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using WahlMirai.Web.ViewModels;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias de <see cref="AdminCensusController"/> y <see cref="CensusService"/>
/// sobre EF Core InMemory (RF-M02-00, RF-M02-01, RF-M02-02, RF-M06-00, RF-M06-01, RN-14).
/// </summary>
public sealed class AdminCensusControllerTests
{
    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var kv in values)
            {
                if (kv.Value != null)
                {
                    _data[kv.Key] = kv.Value;
                }
            }
        }
    }

    private sealed class LocalValidatingUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => new();
        public string? Action(UrlActionContext actionContext) =>
            "/" + (actionContext.Controller ?? "AdminCensus") + "/" + actionContext.Action;
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            if (url[0] == '/')
            {
                return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
            }
            return url.StartsWith("~/", StringComparison.Ordinal);
        }
        public string? Link(string? routeName, object? values) => "/" + routeName;
        public string? RouteUrl(UrlRouteContext routeContext) => "/" + routeContext.RouteName;
    }

    private sealed class ThrowingOnIdEncryptionService : IDocumentEncryptionService
    {
        private readonly uint _failingVoterIdSuffix;
        private readonly string _failingCipher;

        public ThrowingOnIdEncryptionService(string failingCipher)
        {
            _failingCipher = failingCipher;
            _failingVoterIdSuffix = 0;
        }

        public string Encrypt(string plainDocument) => $"enc-{plainDocument}";

        public string Decrypt(string cipherText)
        {
            _ = _failingVoterIdSuffix;
            if (cipherText == _failingCipher)
            {
                throw new CryptographicException("Llave corrupta simulada.");
            }
            return cipherText.Replace("enc-", string.Empty);
        }
    }

    private static WahlMiraiDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new WahlMiraiDbContext(options);

        context.Roles.AddRange(
            new Role { Id = Roles.Elector, Name = Roles.ElectorName, Description = "Elector" },
            new Role { Id = Roles.Admin, Name = Roles.AdminName, Description = "Admin" },
            new Role { Id = Roles.SuperAdmin, Name = Roles.SuperAdminName, Description = "Super Admin" });

        context.Grades.AddRange(
            new Grade { Id = 1, Name = "6°", SequenceOrder = 1, IsLastGrade = false },
            new Grade { Id = 2, Name = "7°", SequenceOrder = 2, IsLastGrade = false },
            new Grade { Id = 3, Name = "8°", SequenceOrder = 3, IsLastGrade = false },
            new Grade { Id = 4, Name = "9°", SequenceOrder = 4, IsLastGrade = false },
            new Grade { Id = 5, Name = "10°", SequenceOrder = 5, IsLastGrade = false },
            new Grade { Id = 6, Name = "11°", SequenceOrder = 6, IsLastGrade = true });

        context.SaveChanges();
        return context;
    }

    private static (
        AdminCensusController Controller,
        CensusService CensusService,
        ActiveSessionStore SessionStore,
        PendingPasswordStore PasswordStore,
        FakeAuditService Audit) CreateHarness(
        WahlMiraiDbContext context,
        IDocumentEncryptionService? encryptionService = null,
        uint adminUserId = 1)
    {
        var auth = new FakeAuthService();
        var audit = new FakeAuditService();
        var enc = encryptionService ?? new FakeEncryptionService();
        var sessionStore = new ActiveSessionStore();
        var passwordStore = new PendingPasswordStore();
        var tokenStore = new PasswordResetTokenStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var credentialService = new CredentialService(
            context,
            passwordStore,
            audit,
            tokenStore,
            new FakeLoginAttemptStore(),
            sessionStore);

        var censusService = new CensusService(context, auth, audit, enc, credentialService, sessionStore);
        var promotionService = new PromotionService(context, audit);

        var httpContext = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("127.0.0.30") },
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()),
                new Claim(ClaimTypes.Role, Roles.AdminName)
            }, "TestAuth"))
        };

        var controller = new AdminCensusController(censusService, promotionService, enc, context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider()),
            Url = new LocalValidatingUrlHelper()
        };

        return (controller, censusService, sessionStore, passwordStore, audit);
    }

    private static IFormFile BuildFormFile(byte[] content, string fileName)
    {
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, content.Length, "csvFile", fileName);
    }

    private static IFormFile BuildCsvFile(string csvText, string fileName = "censo.csv", bool includeUtf8Bom = false)
    {
        var bodyBytes = Encoding.UTF8.GetBytes(csvText);
        byte[] payload = includeUtf8Bom
            ? new byte[] { 0xEF, 0xBB, 0xBF }.Concat(bodyBytes).ToArray()
            : bodyBytes;
        return BuildFormFile(payload, fileName);
    }

    /// <summary>Verifica que CargaCsv rechaza archivo nulo, vacío o extensión distinta de .csv ignorando returnUrl.</summary>
    [Theory]
    [InlineData("NULL")]
    [InlineData("EMPTY")]
    [InlineData("TXT")]
    [InlineData("XLSX")]
    public async Task CargaCsv_NullEmptyOrNonCsv_SetsTempDataErrorAndIgnoresReturnUrl(string kind)
    {
        using var context = CreateInMemoryContext();
        var (controller, _, _, _, _) = CreateHarness(context);

        IFormFile file = kind switch
        {
            "NULL" => null!,
            "EMPTY" => BuildFormFile(Array.Empty<byte>(), "vacio.csv"),
            "TXT" => BuildCsvFile("documento,nombre\n123,Ana", "datos.txt"),
            _ => BuildCsvFile("documento,nombre\n123,Ana", "datos.xlsx")
        };

        var result = await controller.CargaCsv(file, "/AdminCensus/Index");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminCensusController.PendingWhitelist), redirect.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Empty(context.CensusWhitelists);
        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica CargaCsv RFC 4180 con BOM UTF-8, delimitador ';', saltos de línea/comillas en citas y alias de cabecera.</summary>
    [Fact]
    public async Task CargaCsv_Rfc4180WithUtf8BomSemicolonQuotedNewlinesEscapedQuotesAndHeaderAliases_ImportsCleanlyWithoutEmailQueue()
    {
        using var context = CreateInMemoryContext();
        var (controller, _, _, _, _) = CreateHarness(context);

        var csv = string.Join("\r\n", new[]
        {
            "cedula;estudiante;correo;grado;repitente;observacion",
            "1010101;\"Ana María\";ANA@TEST.LOCAL;6°;si;\"Nota \"\"importante\"\"\r\ncon;delimitador\"",
            "2020202;\"Luis Pérez\";luis@test.local;7°;true;\"Línea 1\nLínea 2\"",
            "3030303;\"Sofía Díaz\";sofia@test.local;8°;1;ok"
        });

        // Verificar también el parser RFC 4180 directamente con saltos embebidos y comillas escapadas en campo
        var parsed = CensusService.ParseCsvContent("\uFEFF" + csv);
        Assert.Equal(4, parsed.Count);
        Assert.Equal(2, parsed[1].RowNumber);
        Assert.Contains("Nota \"importante\"\ncon;delimitador", parsed[1].Fields[5]);
        // La fila 1 abarcó las líneas físicas 2 y 3 por el salto embebido, por lo que la siguiente inicia en la línea 4
        Assert.Equal(4, parsed[2].RowNumber);

        var file = BuildCsvFile(csv, "censo_bom.csv", includeUtf8Bom: true);
        var result = await controller.CargaCsv(file);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminCensusController.PendingWhitelist), redirect.ActionName);
        var success = Assert.IsType<string>(controller.TempData["Success"]);
        Assert.Contains("Insertados en Lista Blanca: 3", success);
        Assert.Null(controller.TempData["Warning"]);

        var rows = await context.CensusWhitelists.OrderBy(w => w.DocumentHash).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.Equal("ana@test.local", rows[0].ContactEmail);
        Assert.True(rows[0].ExcluirDePromocion);
        Assert.Equal((byte)1, rows[0].GradeId);
        Assert.True(rows[1].ExcluirDePromocion);
        Assert.True(rows[2].ExcluirDePromocion);
        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica CargaCsv con delimitador ',' y enrutamiento seguro de returnUrl local vs externo.</summary>
    [Theory]
    [InlineData("/AdminCensus/Index", true)]
    [InlineData("https://evil.com", false)]
    [InlineData("//evil.com", false)]
    public async Task CargaCsv_CommaDelimiterAndReturnUrlValidation_RedirectsSafely(string returnUrl, bool expectLocal)
    {
        using var context = CreateInMemoryContext();
        var (controller, _, _, _, _) = CreateHarness(context);

        var csv = "documento,nombre,correo_contacto,grado_id,excluir_promocion\n445566,Carlos Ruiz,carlos@test.local,2,0";
        var file = BuildCsvFile(csv);

        var result = await controller.CargaCsv(file, returnUrl);

        if (expectLocal)
        {
            var local = Assert.IsType<RedirectResult>(result);
            Assert.Equal(returnUrl, local.Url);
        }
        else
        {
            var fallback = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(AdminCensusController.PendingWhitelist), fallback.ActionName);
        }

        Assert.Single(context.CensusWhitelists);
    }

    /// <summary>Verifica que CargaCsv rechaza duplicados dentro del mismo CSV, contra CensusWhitelists y contra Voters.</summary>
    [Fact]
    public async Task CargaCsv_DuplicatesAcrossCsvWhitelistAndVoters_RejectedWithWarning()
    {
        using var context = CreateInMemoryContext();
        context.CensusWhitelists.Add(new CensusWhitelist
        {
            Id = 1,
            DocumentHash = "hash-222222",
            EncryptedDocument = "enc-222222",
            FullName = "Existente Whitelist",
            ContactEmail = "wl@test.local",
            GradeId = 1,
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        context.Voters.Add(new Voter
        {
            Id = 2,
            RoleId = Roles.Elector,
            GradeId = 1,
            DocumentHash = "hash-333333",
            EncryptedDocument = "enc-333333",
            FullName = "Existente Voter",
            ContactEmail = "voter@test.local",
            PasswordHash = "hash",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var (controller, _, _, _, _) = CreateHarness(context);
        var csv = string.Join("\n", new[]
        {
            "documento,nombre,correo,grado,excluir",
            "111111,Nuevo Único,n1@test.local,1,0",
            "111111,Duplicado En Csv,n2@test.local,1,0",
            "222222,Duplicado Whitelist,n3@test.local,1,0",
            "333333,Duplicado Voter,n4@test.local,1,0"
        });

        var result = await controller.CargaCsv(BuildCsvFile(csv));

        Assert.IsType<RedirectToActionResult>(result);
        var warning = Assert.IsType<string>(controller.TempData["Warning"]);
        Assert.Contains("Insertados en Lista Blanca: 1", warning);
        Assert.Contains("Duplicados: 3", warning);
        Assert.Equal(2, await context.CensusWhitelists.CountAsync());
        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica que CargaCsv reporta cabecera faltante, errores de campos y trunca >10 errores en TempData["Warning"].</summary>
    [Fact]
    public async Task CargaCsv_MissingHeaderAndMoreThanTenRowErrors_TruncatesWarningAfterTenErrors()
    {
        using var context = CreateInMemoryContext();
        var (controller, _, _, _, _) = CreateHarness(context);

        var badHeaderCsv = BuildCsvFile("columna_a,columna_b\n1,2");
        await controller.CargaCsv(badHeaderCsv);
        var headerWarning = Assert.IsType<string>(controller.TempData["Warning"]);
        Assert.Contains("Cabecera", headerWarning);

        var rows = new List<string> { "documento,nombre,correo,grado,excluir" };
        for (int i = 1; i <= 12; i++)
        {
            rows.Add($"ABC{i},Nombre {i},correo_invalido,1,0");
        }

        await controller.CargaCsv(BuildCsvFile(string.Join("\n", rows)));

        var warning = Assert.IsType<string>(controller.TempData["Warning"]);
        Assert.Contains("Errores: 12", warning);
        Assert.Contains("... y 2 errores adicionales.", warning);
        Assert.Empty(context.CensusWhitelists);
    }

    /// <summary>Verifica CRUD de lista blanca (AddToWhitelist, AddVoter, GetWhitelistDetails) y su rechazo sin EmailQueue.</summary>
    [Fact]
    public async Task WhitelistCrud_AddGetAndAddVoterCompatibility_BehavesCorrectlyWithoutEmailQueue()
    {
        using var context = CreateInMemoryContext();
        var (controller, _, _, _, _) = CreateHarness(context);

        // AddToWhitelist válido con returnUrl externo ignorado
        var addRes = await controller.AddToWhitelist("555666", "Elena Torres", 2, true, "ELENA@TEST.LOCAL", "https://evil.com");
        var redirect = Assert.IsType<RedirectToActionResult>(addRes);
        Assert.Equal(nameof(AdminCensusController.PendingWhitelist), redirect.ActionName);
        Assert.NotNull(controller.TempData["Success"]);

        // AddToWhitelist duplicado -> Error
        await controller.AddToWhitelist("555666", "Elena Duplicada", 2, false, "elena2@test.local");
        Assert.NotNull(controller.TempData["Error"]);

        // AddVoter compatibilidad -> redirige a /AdminCensus/Index
        var compatRes = await controller.AddVoter("777888", "Mateo Silva", 3, false, "mateo@test.local");
        var compatRedirect = Assert.IsType<RedirectResult>(compatRes);
        Assert.Equal("/AdminCensus/Index", compatRedirect.Url);

        // GetWhitelistDetails
        Assert.IsType<NotFoundResult>(await controller.GetWhitelistDetails(9999));
        var first = await context.CensusWhitelists.FirstAsync(w => w.DocumentHash == "hash-555666");
        var detailsRes = Assert.IsType<JsonResult>(await controller.GetWhitelistDetails(first.Id));
        var dto = Assert.IsType<WhitelistDetailDto>(detailsRes.Value);
        Assert.Equal("555666", dto.Document);
        Assert.Equal("elena@test.local", dto.ContactEmail);

        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica edición/eliminación de entrada pendiente vs inmutabilidad de entrada ya reclamada (ClaimedAt != null).</summary>
    [Fact]
    public async Task WhitelistEditAndDelete_PendingSucceeds_ClaimedEntryIsImmutable()
    {
        using var context = CreateInMemoryContext();
        context.CensusWhitelists.AddRange(
            new CensusWhitelist
            {
                Id = 1,
                DocumentHash = "hash-1001",
                EncryptedDocument = "enc-1001",
                FullName = "Pendiente Original",
                ContactEmail = "pend@test.local",
                GradeId = 1,
                UploadedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                ClaimedAt = null
            },
            new CensusWhitelist
            {
                Id = 2,
                DocumentHash = "hash-2002",
                EncryptedDocument = "enc-2002",
                FullName = "Reclamada Inmutable",
                ContactEmail = "claim@test.local",
                GradeId = 2,
                UploadedByUserId = 1,
                CreatedAt = DateTime.UtcNow,
                ClaimedAt = DateTime.UtcNow,
                ClaimedByUserId = 99
            });
        await context.SaveChangesAsync();

        var (controller, _, _, _, _) = CreateHarness(context);

        // Editar entrada reclamada -> error y BD intacta
        await controller.EditWhitelistEntry(2, "Cambio Prohibido", 3, true, "nuevo@test.local");
        Assert.NotNull(controller.TempData["Error"]);
        var claimed = await context.CensusWhitelists.SingleAsync(w => w.Id == 2);
        Assert.Equal("Reclamada Inmutable", claimed.FullName);
        Assert.Equal((byte)2, claimed.GradeId);

        // Eliminar entrada reclamada -> error y BD intacta
        await controller.DeleteWhitelistEntry(2);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.NotNull(await context.CensusWhitelists.FindAsync((uint)2));

        // Editar y eliminar entrada pendiente -> éxito
        await controller.EditWhitelistEntry(1, "Pendiente Editada", 4, true, "edit@test.local");
        Assert.NotNull(controller.TempData["Success"]);
        var edited = await context.CensusWhitelists.SingleAsync(w => w.Id == 1);
        Assert.Equal("Pendiente Editada", edited.FullName);
        Assert.Equal((byte)4, edited.GradeId);

        await controller.DeleteWhitelistEntry(1);
        Assert.NotNull(controller.TempData["Success"]);
        Assert.Null(await context.CensusWhitelists.FindAsync((uint)1));
    }

    /// <summary>Verifica Index, PendingWhitelist y GetVoterDetails sobre EF InMemory con roles reales.</summary>
    [Fact]
    public async Task IndexPendingWhitelistAndGetVoterDetails_ReturnExpectedViewsAndJson()
    {
        using var context = CreateInMemoryContext();
        context.Voters.Add(new Voter
        {
            Id = 10,
            RoleId = Roles.Elector,
            GradeId = 1,
            DocumentHash = "hash-9090",
            EncryptedDocument = "enc-9090",
            FullName = "Valentina Rojas",
            ContactEmail = "val@test.local",
            PasswordHash = "hash",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        });
        context.CensusWhitelists.Add(new CensusWhitelist
        {
            Id = 10,
            DocumentHash = "hash-8080",
            EncryptedDocument = "enc-8080",
            FullName = "Pendiente Uno",
            ContactEmail = "p1@test.local",
            GradeId = 1,
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var (controller, _, _, _, _) = CreateHarness(context);

        var idxView = Assert.IsType<ViewResult>(await controller.Index());
        var idxModel = Assert.IsType<PagedResult<VoterDetailDto>>(idxView.Model);
        Assert.Single(idxModel.Items);
        Assert.Equal("9090", idxModel.Items[0].Document);

        var wlView = Assert.IsType<ViewResult>(await controller.PendingWhitelist());
        var wlModel = Assert.IsType<PagedResult<WhitelistDetailDto>>(wlView.Model);
        Assert.Single(wlModel.Items);
        Assert.NotNull(controller.ViewBag.Stats);

        Assert.IsType<NotFoundResult>(await controller.GetVoterDetails(9999));
        var voterJson = Assert.IsType<JsonResult>(await controller.GetVoterDetails(10));
        var voterDto = Assert.IsType<VoterDetailDto>(voterJson.Value);
        Assert.Equal("Valentina Rojas", voterDto.FullName);
        Assert.Equal("9090", voterDto.Document);
    }

    /// <summary>Verifica EditVoter: actualización, rechazo de documento duplicado, limpieza de GradeId para Admin e invalidación de sesión (RN-14).</summary>
    [Fact]
    public async Task EditVoter_UpdatesClearsAdminGradeRejectsDuplicateDocAndInvalidatesSessionOnInactive()
    {
        using var context = CreateInMemoryContext();
        context.Voters.AddRange(
            new Voter
            {
                Id = 10,
                RoleId = Roles.Elector,
                GradeId = 1,
                DocumentHash = "hash-1010",
                EncryptedDocument = "enc-1010",
                FullName = "Elector Diez",
                ContactEmail = "diez@test.local",
                PasswordHash = "hash",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            },
            new Voter
            {
                Id = 11,
                RoleId = Roles.Elector,
                GradeId = 2,
                DocumentHash = "hash-1111",
                EncryptedDocument = "enc-1111",
                FullName = "Elector Once",
                ContactEmail = "once@test.local",
                PasswordHash = "hash",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var (controller, _, sessionStore, _, _) = CreateHarness(context);
        sessionStore.RegisterSession(10, "stamp-10");
        Assert.True(sessionStore.IsSessionValid(10, "stamp-10").IsValid);

        // Documento duplicado rechazado
        await controller.EditVoter(10, "1111", "Elector Diez", "diez@test.local", 1, Roles.Elector, "ACTIVO", false);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Equal("hash-1010", (await context.Voters.SingleAsync(v => v.Id == 10)).DocumentHash);

        // Cambio a Roles.Admin y status INACTIVO -> limpia GradeId e invalida sesión en ActiveSessionStore
        await controller.EditVoter(10, "101099", "Admin Diez", "admin10@test.local", 3, Roles.Admin, "INACTIVO", false);
        Assert.NotNull(controller.TempData["Success"]);

        var updated = await context.Voters.SingleAsync(v => v.Id == 10);
        Assert.Equal(Roles.Admin, updated.RoleId);
        Assert.Null(updated.GradeId);
        Assert.Equal("INACTIVO", updated.Status);
        Assert.Equal("hash-101099", updated.DocumentHash);

        var sessionCheck = sessionStore.IsSessionValid(10, "stamp-10");
        Assert.False(sessionCheck.IsValid);
        Assert.Equal(SessionInvalidationReason.AccountDisabledOrDeleted, sessionCheck.Reason);
    }

    /// <summary>Verifica DeleteVoter (borrado lógico + invalidación RN-14 + rechazo repetido) y RestoreVoter (solo desde ELIMINADO).</summary>
    [Fact]
    public async Task DeleteVoterAndRestoreVoter_LifecycleAndSessionInvalidation()
    {
        using var context = CreateInMemoryContext();
        context.Voters.AddRange(
            new Voter
            {
                Id = 20,
                RoleId = Roles.Elector,
                GradeId = 5,
                DocumentHash = "hash-2020",
                EncryptedDocument = "enc-2020",
                FullName = "Usuario Veinte",
                ContactEmail = "u20@test.local",
                PasswordHash = "hash",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            },
            new Voter
            {
                Id = 21,
                RoleId = Roles.Elector,
                GradeId = 5,
                DocumentHash = "hash-2121",
                EncryptedDocument = "enc-2121",
                FullName = "Usuario Inactivo",
                ContactEmail = "u21@test.local",
                PasswordHash = "hash",
                Status = "INACTIVO",
                RegisteredAt = DateTime.UtcNow
            },
            new Voter
            {
                Id = 22,
                RoleId = Roles.Elector,
                GradeId = null,
                DocumentHash = "hash-2222",
                EncryptedDocument = "enc-2222",
                FullName = "Usuario Egresado",
                ContactEmail = "u22@test.local",
                PasswordHash = "hash",
                Status = "EGRESADO",
                RegisteredAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var (controller, _, sessionStore, _, audit) = CreateHarness(context);
        sessionStore.RegisterSession(20, "stamp-20");

        // Restore rechazado cuando no es ELIMINADO (ACTIVO / INACTIVO / EGRESADO / inexistente)
        foreach (var badId in new[] { 20, 21, 22, 999 })
        {
            await controller.RestoreVoter(badId);
            Assert.NotNull(controller.TempData["Error"]);
        }

        // DeleteVoter exitoso
        await controller.DeleteVoter(20);
        Assert.NotNull(controller.TempData["Success"]);
        var deleted = await context.Voters.SingleAsync(v => v.Id == 20);
        Assert.Equal("ELIMINADO", deleted.Status);
        Assert.NotNull(deleted.DeletedAt);
        Assert.Contains("USER_DELETED", audit.Actions);

        var sessionCheck = sessionStore.IsSessionValid(20, "stamp-20");
        Assert.False(sessionCheck.IsValid);
        Assert.Equal(SessionInvalidationReason.AccountDisabledOrDeleted, sessionCheck.Reason);

        // DeleteVoter repetido o inexistente -> Error
        await controller.DeleteVoter(20);
        Assert.NotNull(controller.TempData["Error"]);
        await controller.DeleteVoter(999);
        Assert.NotNull(controller.TempData["Error"]);

        // RestoreVoter válido desde ELIMINADO
        await controller.RestoreVoter(20);
        Assert.NotNull(controller.TempData["Success"]);
        var restored = await context.Voters.SingleAsync(v => v.Id == 20);
        Assert.Equal("ACTIVO", restored.Status);
        Assert.Null(restored.DeletedAt);
        Assert.Contains("USER_RESTORED", audit.Actions);
    }

    /// <summary>Verifica ResetPassword (EmailQueue REASIGNACION_ADMIN, PendingPasswordStore, ActiveSessionStore RN-14) y que CargaCsv/AddToWhitelist no encolan EmailQueue.</summary>
    [Fact]
    public async Task ResetPassword_EnqueuesReasignacionAdminUpdatesPasswordStoreAndInvalidatesSession()
    {
        // NOTA: Las operaciones de alta en lista blanca (CargaCsv / AddToWhitelist) no encolan filas en email_queue.
        // Los encolados de CANDIDATURA_APROBADA y CANDIDATURA_RECHAZADA en email_queue ocurren exclusivamente en
        // CandidateReviewService y están cubiertos en AdminCandidatesControllerTests.cs.
        using var context = CreateInMemoryContext();
        context.Voters.AddRange(
            new Voter
            {
                Id = 30,
                RoleId = Roles.Elector,
                GradeId = 4,
                DocumentHash = "hash-3030",
                EncryptedDocument = "enc-3030",
                FullName = "Usuario Treinta",
                ContactEmail = "u30@test.local",
                PasswordHash = "old-hash",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            },
            new Voter
            {
                Id = 31,
                RoleId = Roles.Elector,
                GradeId = 4,
                DocumentHash = "hash-3131",
                EncryptedDocument = "enc-3131",
                FullName = "Sin Correo",
                ContactEmail = "   ",
                PasswordHash = "old-hash",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var (controller, _, sessionStore, passwordStore, audit) = CreateHarness(context);
        sessionStore.RegisterSession(30, "stamp-30");

        // CargaCsv y AddToWhitelist NO encolan EmailQueue
        await controller.AddToWhitelist("404040", "Sin Correo Encolado", 1, false, "nuevo@test.local");
        Assert.Empty(context.EmailQueues);

        // ResetPassword fallido cuando falta usuario o correo
        await controller.ResetPassword(999);
        Assert.NotNull(controller.TempData["Error"]);
        await controller.ResetPassword(31);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Empty(context.EmailQueues);

        // ResetPassword válido
        var result = await controller.ResetPassword(30);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminCensusController.Index), redirect.ActionName);
        Assert.NotNull(controller.TempData["Success"]);

        var voter = await context.Voters.SingleAsync(v => v.Id == 30);
        Assert.NotEqual("old-hash", voter.PasswordHash);

        var email = await context.EmailQueues.SingleAsync();
        Assert.Equal(30u, email.VoterId);
        Assert.Equal("REASIGNACION_ADMIN", email.EmailType);
        Assert.Equal("PENDIENTE", email.Status);
        Assert.Equal((byte)0, email.Attempts);

        Assert.True(passwordStore.TryGetPassword(email.Id, out var tempPwd));
        Assert.False(string.IsNullOrWhiteSpace(tempPwd));

        var sessionCheck = sessionStore.IsSessionValid(30, "stamp-30");
        Assert.False(sessionCheck.IsValid);
        Assert.Equal(SessionInvalidationReason.AccountDisabledOrDeleted, sessionCheck.Reason);

        Assert.Contains("PASSWORD_REASSIGNED", audit.Actions);
    }

    /// <summary>Verifica DescargarPlantillaCsv, PromotionPreview y RunPromotion con force=false y force=true.</summary>
    [Fact]
    public async Task TemplateAndPromotionActions_GenerateCsvPreviewAndRespectForceFlag()
    {
        using var context = CreateInMemoryContext();
        context.AcademicYears.Add(new AcademicYear { Id = 1, Year = 2026, IsCurrent = true });
        context.Voters.Add(new Voter
        {
            Id = 40,
            RoleId = Roles.Elector,
            GradeId = 1,
            DocumentHash = "hash-4040",
            EncryptedDocument = "enc-4040",
            FullName = "Promovible Sexto",
            ContactEmail = "p6@test.local",
            PasswordHash = "hash",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var (controller, _, _, _, _) = CreateHarness(context);

        var template = Assert.IsType<FileContentResult>(controller.DescargarPlantillaCsv());
        Assert.Equal("text/csv", template.ContentType);
        Assert.Equal("plantilla_censo_whitelist.csv", template.FileDownloadName);
        Assert.StartsWith("documento,nombre,correo_contacto,grado_id,excluir_promocion", Encoding.UTF8.GetString(template.FileContents));

        var previewRes = Assert.IsType<PartialViewResult>(await controller.PromotionPreview());
        Assert.Equal("_PromotionModal", previewRes.ViewName);
        var preview = Assert.IsType<PromotionPreview>(previewRes.Model);
        Assert.Equal(1, preview.EligibleCount);

        // Primera ejecución sin force -> éxito (6° -> 7°)
        await controller.RunPromotion(force: false);
        Assert.NotNull(controller.TempData["Success"]);
        Assert.Equal((byte)2, (await context.Voters.SingleAsync(v => v.Id == 40)).GradeId);

        // Segunda ejecución sin force -> Error
        await controller.RunPromotion(force: false);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Equal((byte)2, (await context.Voters.SingleAsync(v => v.Id == 40)).GradeId);

        // Con force=true -> éxito (7° -> 8°)
        await controller.RunPromotion(force: true);
        Assert.NotNull(controller.TempData["Success"]);
        Assert.Equal((byte)3, (await context.Voters.SingleAsync(v => v.Id == 40)).GradeId);
    }

    /// <summary>Verifica MigrateDocuments: cifra documentos planos, omite ya cifrados y reporta fallos criptográficos.</summary>
    [Fact]
    public async Task MigrateDocuments_EncryptsPlaintextSkipsEncryptedAndReportsCryptoFailures()
    {
        using var context = CreateInMemoryContext();
        context.Voters.AddRange(
            new Voter
            {
                Id = 50,
                RoleId = Roles.Elector,
                GradeId = 1,
                DocumentHash = "h50",
                EncryptedDocument = "12345678", // texto plano
                FullName = "Plano",
                ContactEmail = "p@test.local",
                PasswordHash = "h",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            },
            new Voter
            {
                Id = 51,
                RoleId = Roles.Elector,
                GradeId = 1,
                DocumentHash = "h51",
                EncryptedDocument = "enc-87654321", // ya cifrado
                FullName = "Cifrado",
                ContactEmail = "c@test.local",
                PasswordHash = "h",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            },
            new Voter
            {
                Id = 52,
                RoleId = Roles.Elector,
                GradeId = 1,
                DocumentHash = "h52",
                EncryptedDocument = "CORRUPT_CIPHER",
                FullName = "Fallido",
                ContactEmail = "f@test.local",
                PasswordHash = "h",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var (controller, _, _, _, _) = CreateHarness(context, new ThrowingOnIdEncryptionService("CORRUPT_CIPHER"));

        var result = await controller.MigrateDocuments();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminCensusController.Index), redirect.ActionName);
        var error = Assert.IsType<string>(controller.TempData["Error"]);
        Assert.Contains("Migrados: 1", error);
        Assert.Contains("Ya cifrados (omitidos): 1", error);
        Assert.Contains("Fallidos: 1", error);
        Assert.Contains("52", error);

        Assert.Equal("enc-12345678", (await context.Voters.SingleAsync(v => v.Id == 50)).EncryptedDocument);
        Assert.Equal("enc-87654321", (await context.Voters.SingleAsync(v => v.Id == 51)).EncryptedDocument);
    }

    /// <summary>Documenta el estado actual de los 11 métodos [HttpPost] de AdminCensusController sin [ValidateAntiForgeryToken] (R3).</summary>
    [Fact]
    public void AdminCensusController_PostActions_CurrentBehaviorLacksValidateAntiForgeryTokenAttribute()
    {
        // NOTA: R3 — Ninguna de las 11 acciones [HttpPost] de AdminCensusController
        // (EditVoter, DeleteVoter, RestoreVoter, ResetPassword, AddToWhitelist, AddVoter,
        // EditWhitelistEntry, DeleteWhitelistEntry, CargaCsv, RunPromotion, MigrateDocuments)
        // declara [ValidateAntiForgeryToken]. Fuera de alcance de P1–P4: no se modifica producción,
        // se documenta aquí el comportamiento actual.
        var postMethods = typeof(AdminCensusController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        Assert.Equal(11, postMethods.Count);
        foreach (var method in postMethods)
        {
            Assert.Null(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }
    }
}
