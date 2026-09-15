using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class CensusWhitelistTests
{
    // ── 1. Alta individual en census_whitelist ─────────────────────────────────────

    [Fact]
    public async Task AddToWhitelistAsync_CreatesPendingEntryAndAuditLog()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var auth = new FakeAuthService();
        var enc = new FakeEncryptionService();
        var service = CreateCensusService(context, auth, audit, enc);

        var entry = await service.AddToWhitelistAsync("1020304050", "Carlos Santana", 1, false, 1, "127.0.0.1");

        Assert.NotNull(entry);
        Assert.Equal("Carlos Santana", entry.FullName);
        Assert.Equal(1, entry.GradeId);
        Assert.False(entry.ExcluirDePromocion);
        Assert.Equal("hash-1020304050", entry.DocumentHash);
        Assert.Equal("enc-1020304050", entry.EncryptedDocument);
        Assert.Null(entry.ClaimedAt);
        Assert.Null(entry.ClaimedByUserId);
        Assert.Equal((uint)1, entry.UploadedByUserId);

        // Verificar que NO se haya creado una cuenta en la tabla users
        Assert.Empty(context.Voters);

        // Verificar registro de auditoría
        Assert.Contains(audit.Actions, a => a == "WHITELIST_ENTRY_CREATED");
    }

    [Fact]
    public async Task AddToWhitelistAsync_ThrowsOnDuplicateDocument()
    {
        await using var context = CreateContext();
        var service = CreateCensusService(context);

        await service.AddToWhitelistAsync("1020304050", "Carlos Santana", 1, false, 1, "127.0.0.1");

        // Intentar registrar el mismo documento debe fallar
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddToWhitelistAsync("1020304050", "Carlos Santana Duplicado", 1, false, 1, "127.0.0.1"));
    }

    [Fact]
    public async Task AddToWhitelistAsync_ThrowsIfDocumentAlreadyInUsers()
    {
        await using var context = CreateContext();
        context.Voters.Add(new Voter
        {
            Id = 10,
            DocumentHash = "hash-1020304050",
            EncryptedDocument = "enc-1020304050",
            FullName = "Usuario Existente",
            ContactEmail = "user@example.com",
            PasswordHash = "hash",
            RoleId = 3,
            Status = "ACTIVO"
        });
        await context.SaveChangesAsync();

        var service = CreateCensusService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddToWhitelistAsync("1020304050", "Estudiante Nuevo", 1, false, 1, "127.0.0.1"));
    }

    // ── 2. Carga masiva CSV de 2000+ registros hacia census_whitelist ─────────────

    [Fact]
    public async Task ImportCsvAsync_Processes2000RecordsEfficiently_AndTracksDuplicates()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var service = CreateCensusService(context, audit: audit);

        // Pre-insertar 1 registro existente en la base de datos para probar duplicado externo
        context.CensusWhitelists.Add(new CensusWhitelist
        {
            Id = 999,
            DocumentHash = "hash-999999",
            EncryptedDocument = "enc-999999",
            FullName = "Estudiante Existente",
            GradeId = 1,
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        // Construir CSV con 2000 registros únicos + 1 duplicado en el mismo CSV + 1 duplicado en BD + 1 fila con error
        var csvBuilder = new StringBuilder();
        csvBuilder.AppendLine("documento,nombre,grado_id,excluir_promocion");

        // Registro duplicado contra BD
        csvBuilder.AppendLine("999999,Estudiante Existente,1,0");

        // 2000 registros válidos
        for (int i = 1; i <= 2000; i++)
        {
            csvBuilder.AppendLine($"1000{i:D5},Estudiante de Prueba,1,0");
        }

        // Fila duplicada internamente dentro del CSV (repetir el primer documento)
        csvBuilder.AppendLine("100000001,Estudiante Duplicado Interno,1,0");

        // Fila con error de formato (documento no numérico)
        csvBuilder.AppendLine("ABC12345,Estudiante Error,1,0");

        var csvBytes = Encoding.UTF8.GetBytes(csvBuilder.ToString());
        using var stream = new MemoryStream(csvBytes);

        var stopwatch = Stopwatch.StartNew();
        var result = await service.ImportCsvAsync(stream, 1, "127.0.0.1");
        stopwatch.Stop();

        // Verificaciones de resultados
        Assert.Equal(2003, result.ProcessedCount);
        Assert.Equal(2000, result.InsertedCount);
        Assert.Equal(2, result.DuplicateCount); // 1 duplicado en BD + 1 duplicado interno
        Assert.Equal(1, result.ErrorCount);     // 1 error de formato no numérico

        // Verificación de rendimiento: 2000 registros deben procesarse en menos de 5 segundos
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, $"El procesamiento tomó {stopwatch.ElapsedMilliseconds}ms, esperado < 5000ms.");

        // Verificar que las 2000 entradas están en census_whitelist sin reclamar
        var totalWhitelist = await context.CensusWhitelists.CountAsync(w => w.ClaimedAt == null);
        Assert.Equal(2001, totalWhitelist); // 2000 insertados + 1 que ya existía

        // Verificar que NO se crearon cuentas activas ni contraseñas
        Assert.Empty(context.Voters);

        // Verificar auditoría
        Assert.Contains(audit.Actions, a => a == "CSV_IMPORT_WHITELIST");
    }

    // ── 3. Modificación y eliminación con bloqueo de reclamados ───────────────────

    [Fact]
    public async Task UpdateWhitelistEntryAsync_AllowsEditingPendingEntry_BlocksClaimedEntry()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var service = CreateCensusService(context, audit: audit);

        var entry = await service.AddToWhitelistAsync("11223344", "Nombre Original", 1, false, 1, "127.0.0.1");

        // Editar entrada pendiente -> debe permitir
        var updated = await service.UpdateWhitelistEntryAsync(entry.Id, "Nombre Modificado", 2, true, 1, "127.0.0.1");
        Assert.True(updated);

        var reloaded = await context.CensusWhitelists.FindAsync(entry.Id);
        Assert.NotNull(reloaded);
        Assert.Equal("Nombre Modificado", reloaded.FullName);
        Assert.Equal(2, reloaded.GradeId);
        Assert.True(reloaded.ExcluirDePromocion);
        Assert.Contains(audit.Actions, a => a == "WHITELIST_ENTRY_UPDATED");

        // Marcar la entrada como reclamada
        reloaded.ClaimedAt = DateTime.UtcNow;
        reloaded.ClaimedByUserId = 50;
        await context.SaveChangesAsync();

        // Intentar editar entrada ya reclamada -> debe lanzar InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateWhitelistEntryAsync(entry.Id, "Nombre Intento Reclamado", 3, false, 1, "127.0.0.1"));
    }

    [Fact]
    public async Task DeleteWhitelistEntryAsync_AllowsDeletingPendingEntry_BlocksClaimedEntry()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var service = CreateCensusService(context, audit: audit);

        var entry = await service.AddToWhitelistAsync("55667788", "Estudiante a Eliminar", 1, false, 1, "127.0.0.1");

        // Entrada pendiente -> eliminar debe funcionar
        var deleted = await service.DeleteWhitelistEntryAsync(entry.Id, 1, "127.0.0.1");
        Assert.True(deleted);
        Assert.Null(await context.CensusWhitelists.FindAsync(entry.Id));
        Assert.Contains(audit.Actions, a => a == "WHITELIST_ENTRY_DELETED");

        // Crear otra entrada y marcarla como reclamada
        var claimedEntry = await service.AddToWhitelistAsync("99887766", "Estudiante Reclamado", 1, false, 1, "127.0.0.1");
        claimedEntry.ClaimedAt = DateTime.UtcNow;
        claimedEntry.ClaimedByUserId = 60;
        await context.SaveChangesAsync();

        // Intentar eliminar entrada reclamada -> debe fallar
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.DeleteWhitelistEntryAsync(claimedEntry.Id, 1, "127.0.0.1"));
    }

    // ── 4. Paginación server-side ─────────────────────────────────────────────────

    [Fact]
    public async Task GetPendingWhitelistPagedAsync_AppliesPaginationCorrectly()
    {
        await using var context = CreateContext();
        var service = CreateCensusService(context);

        // Crear 45 entradas en lista blanca
        for (int i = 1; i <= 45; i++)
        {
            context.CensusWhitelists.Add(new CensusWhitelist
            {
                Id = (uint)i,
                DocumentHash = $"hash-{i}",
                EncryptedDocument = $"enc-{i}",
                FullName = $"Estudiante Paginado {i:D2}",
                GradeId = (byte)((i % 3) + 1), // Grados 1, 2, 3
                UploadedByUserId = 1,
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await context.SaveChangesAsync();

        // Página 1 con tamaño 10
        var page1 = await service.GetPendingWhitelistPagedAsync(pageNumber: 1, pageSize: 10);
        Assert.Equal(45, page1.TotalCount);
        Assert.Equal(5, page1.TotalPages);
        Assert.Equal(10, page1.Items.Count);
        Assert.False(page1.HasPreviousPage);
        Assert.True(page1.HasNextPage);
        Assert.Equal(1, page1.StartItemIndex);
        Assert.Equal(10, page1.EndItemIndex);

        // Página 5 (última página, tamaño 10) -> debe contener 5 elementos
        var page5 = await service.GetPendingWhitelistPagedAsync(pageNumber: 5, pageSize: 10);
        Assert.Equal(5, page5.Items.Count);
        Assert.True(page5.HasPreviousPage);
        Assert.False(page5.HasNextPage);
        Assert.Equal(41, page5.StartItemIndex);
        Assert.Equal(45, page5.EndItemIndex);

        // Filtro por grado 1
        var filteredByGrade = await service.GetPendingWhitelistPagedAsync(gradeId: 1, pageNumber: 1, pageSize: 50);
        Assert.Equal(15, filteredByGrade.TotalCount);
        Assert.All(filteredByGrade.Items, item => Assert.Equal(1, item.GradeId));
    }

    [Fact]
    public async Task GetVotersPagedAsync_AppliesPaginationCorrectly()
    {
        await using var context = CreateContext();
        var service = CreateCensusService(context);

        for (int i = 1; i <= 25; i++)
        {
            context.Voters.Add(new Voter
            {
                Id = (uint)i,
                DocumentHash = $"hash-voter-{i}",
                EncryptedDocument = $"enc-voter-{i}",
                FullName = $"Elector Activo {i:D2}",
                ContactEmail = $"elector{i}@colegio.edu.co",
                PasswordHash = "hash",
                RoleId = 3, // ELECTOR
                GradeId = 1,
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            });
        }
        await context.SaveChangesAsync();

        var page1 = await service.GetVotersPagedAsync(pageNumber: 1, pageSize: 10);
        Assert.Equal(25, page1.TotalCount);
        Assert.Equal(3, page1.TotalPages);
        Assert.Equal(10, page1.Items.Count);
        Assert.True(page1.HasNextPage);
    }

    // ── 5. Promoción anual incluyendo no reclamados ───────────────────────────────

    [Fact]
    public async Task RunPromotionAsync_PromotesBothActiveVotersAndPendingWhitelist()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var promotionService = new PromotionService(context, audit);

        // Configurar año lectivo activo
        context.AcademicYears.Add(new AcademicYear
        {
            Id = 1,
            Year = 2026,
            IsCurrent = true,
            PromotionExecutedAt = null
        });

        // 1. Activo en 6° (promover a 7°)
        context.Voters.Add(new Voter { Id = 1, FullName = "Activo 6to", GradeId = 1, RoleId = 3, Status = "ACTIVO", DocumentHash = "h1", EncryptedDocument = "e1", ContactEmail = "a1@test.com", PasswordHash = "p" });
        // 2. Activo en 11° (egresar)
        context.Voters.Add(new Voter { Id = 2, FullName = "Activo 11mo", GradeId = 6, RoleId = 3, Status = "ACTIVO", DocumentHash = "h2", EncryptedDocument = "e2", ContactEmail = "a2@test.com", PasswordHash = "p" });
        // 3. Activo repitente en 6° (mantener en 6°)
        context.Voters.Add(new Voter { Id = 3, FullName = "Activo Repitente", GradeId = 1, ExcluirDePromocion = true, RoleId = 3, Status = "ACTIVO", DocumentHash = "h3", EncryptedDocument = "e3", ContactEmail = "a3@test.com", PasswordHash = "p" });

        // 4. Whitelist en 6° (promover a 7°)
        context.CensusWhitelists.Add(new CensusWhitelist { Id = 101, FullName = "Whitelist 6to", GradeId = 1, DocumentHash = "w1", EncryptedDocument = "we1", UploadedByUserId = 1, CreatedAt = DateTime.UtcNow });
        // 5. Whitelist en 11° (último grado -> egresado / excluido)
        context.CensusWhitelists.Add(new CensusWhitelist { Id = 102, FullName = "Whitelist 11mo", GradeId = 6, DocumentHash = "w2", EncryptedDocument = "we2", UploadedByUserId = 1, CreatedAt = DateTime.UtcNow });
        // 6. Whitelist repitente en 6° (mantener en 6°)
        context.CensusWhitelists.Add(new CensusWhitelist { Id = 103, FullName = "Whitelist Repitente", GradeId = 1, ExcluirDePromocion = true, DocumentHash = "w3", EncryptedDocument = "we3", UploadedByUserId = 1, CreatedAt = DateTime.UtcNow });

        await context.SaveChangesAsync();

        // Ejecutar promoción anual
        var result = await promotionService.RunPromotionAsync(force: false, adminIp: "127.0.0.1");

        Assert.True(result.Success);
        Assert.Equal(2, result.PromotedCount);  // 1 activo (6->7) + 1 whitelist (6->7)
        Assert.Equal(2, result.GraduatedCount); // 1 activo (11->EGRESADO) + 1 whitelist (11->Egresado)
        Assert.Equal(2, result.RetainedCount);  // 1 activo repitente + 1 whitelist repitente

        // Verificar estados individuales en base de datos
        var v1 = await context.Voters.FindAsync((uint)1);
        Assert.Equal((byte)2, v1!.GradeId); // 6° promovido a 7°

        var v2 = await context.Voters.FindAsync((uint)2);
        Assert.Equal("EGRESADO", v2!.Status);
        Assert.Null(v2.GradeId);

        var v3 = await context.Voters.FindAsync((uint)3);
        Assert.Equal((byte)1, v3!.GradeId); // Se mantuvo en 6°
        Assert.False(v3.ExcluirDePromocion); // Bandera reiniciada

        var w1 = await context.CensusWhitelists.FindAsync((uint)101);
        Assert.Equal((byte)2, w1!.GradeId); // 6° promovido a 7°

        var w2 = await context.CensusWhitelists.FindAsync((uint)102);
        Assert.True(w2!.ExcluirDePromocion); // Excluido de futuras promociones

        var w3 = await context.CensusWhitelists.FindAsync((uint)103);
        Assert.Equal((byte)1, w3!.GradeId); // Se mantuvo en 6°
        Assert.False(w3.ExcluirDePromocion); // Bandera reiniciada

        // Verificar bloqueo de doble ejecución en el mismo año lectivo
        var doubleRun = await promotionService.RunPromotionAsync(force: false, adminIp: "127.0.0.1");
        Assert.False(doubleRun.Success);
        Assert.Contains("ya fue ejecutada", doubleRun.Message);

        // Verificar auditoría
        Assert.Contains(audit.Actions, a => a == "PROMOTION_RUN");
    }

    [Fact]
    public async Task GetPromotionPreviewAsync_ReflectsConsolidatedTotals()
    {
        await using var context = CreateContext();
        var promotionService = new PromotionService(context, new FakeAuditService());

        context.AcademicYears.Add(new AcademicYear { Id = 1, Year = 2026, IsCurrent = true });

        context.Voters.Add(new Voter { Id = 1, FullName = "Activo 6to", GradeId = 1, RoleId = 3, Status = "ACTIVO", DocumentHash = "h1", EncryptedDocument = "e1", ContactEmail = "a1@test.com", PasswordHash = "p" });
        context.CensusWhitelists.Add(new CensusWhitelist { Id = 101, FullName = "Whitelist 6to", GradeId = 1, DocumentHash = "w1", EncryptedDocument = "we1", UploadedByUserId = 1, CreatedAt = DateTime.UtcNow });

        await context.SaveChangesAsync();

        var preview = await promotionService.GetPromotionPreviewAsync();

        Assert.Equal(2026, preview.CurrentYear);
        Assert.Equal(1, preview.ActiveVotersCount);
        Assert.Equal(1, preview.PendingWhitelistCount);
        Assert.Equal(2, preview.EligibleCount);
        Assert.Equal(2, preview.PreviewList.Count);
        Assert.Contains(preview.PreviewList, item => item.SourceType == "ACTIVO");
        Assert.Contains(preview.PreviewList, item => item.SourceType == "LISTA_BLANCA");
    }

    // ── Utilidades de Test ────────────────────────────────────────────────────────

    private static WahlMiraiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new WahlMiraiDbContext(options);

        context.Roles.AddRange(
            new Role { Id = 1, Name = "SUPER_ADMIN" },
            new Role { Id = 2, Name = "ADMIN" },
            new Role { Id = 3, Name = "ELECTOR" });

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

    private static ICensusService CreateCensusService(
        WahlMiraiDbContext context,
        IAuthService? auth = null,
        IAuditService? audit = null,
        IDocumentEncryptionService? enc = null,
        ICredentialService? cred = null)
    {
        return new CensusService(
            context,
            auth ?? new FakeAuthService(),
            audit ?? new FakeAuditService(),
            enc ?? new FakeEncryptionService(),
            cred ?? new CredentialService(context, new PendingPasswordStore(), audit ?? new FakeAuditService()));
    }

    private sealed class FakeAuthService : IAuthService
    {
        public Task<Voter?> ValidateLoginAsync(string document, string password) => Task.FromResult<Voter?>(null);
        public Task<string> GenerateInitialPasswordAsync(string document) => Task.FromResult("Pass123!");
        public Task<bool> ChangePasswordAsync(int voterId, string newPassword, string ipAddress) => Task.FromResult(true);
        public string HashPassword(string password) => "password-hash";
        public string HashDocument(string document) => $"hash-{document}";
    }

    private sealed class FakeEncryptionService : IDocumentEncryptionService
    {
        public string Encrypt(string plainText) => $"enc-{plainText}";
        public string Decrypt(string cipherText) => cipherText.Replace("enc-", string.Empty);
    }

    private sealed class FakeAuditService : IAuditService
    {
        public List<string> Actions { get; } = new();

        public Task LogAsync(string action, int? voterId, string targetEntity, int? targetId, string? fieldName, string? oldValue, string? newValue, string? details, string? ipAddress)
        {
            Actions.Add(action);
            return Task.CompletedTask;
        }
    }
}
