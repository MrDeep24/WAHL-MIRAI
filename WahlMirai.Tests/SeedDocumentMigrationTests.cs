using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

public class SeedDocumentMigrationTests
{
    private const string SuperAdminDoc = "1020304050";
    private const string SuperAdminHash = "9e1f341dff9161b69d6afc54140ea30e44676ed3600dc98f507f0678fe64e320";

    private const string WhitelistDoc = "1015998877";
    private const string WhitelistHash = "16ad24eeedca3411b3ab6e848d6789bdb0f6c864edb0b2b60e361a7277f69fbd";

    private const string ElectorDoc = "1001234567";
    private const string ElectorHash = "c7dc7b2f9562560342d1b7ba78febecc04199365383ea3452c44b9266a09f758";

    private static WahlMiraiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new WahlMiraiDbContext(options);
    }

    private static IDocumentEncryptionService CreateRealEncryptionService()
    {
        var provider = new EphemeralDataProtectionProvider();
        return new DocumentEncryptionService(provider, NullLogger<DocumentEncryptionService>.Instance);
    }

    [Fact]
    public void MigrateSeedDocuments_PlainTextWithPrefix_UsersAndWhitelist_EncryptsAndDecryptsCleanDoc()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "Super Admin",
            ContactEmail = "coordinacion.electoral@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}{SuperAdminDoc}",
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };
        var whitelist = new CensusWhitelist
        {
            Id = 10,
            FullName = "Sofía Ramírez",
            ContactEmail = "sofia.ramirez@colegio.edu.co",
            DocumentHash = WhitelistHash,
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}{WhitelistDoc}",
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Voters.Add(voter);
        context.CensusWhitelists.Add(whitelist);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(1, result.UsersMigrated);
        Assert.Equal(1, result.WhitelistMigrated);
        Assert.Equal(2, result.TotalMigrated);

        // Los valores ya no deben tener el prefijo
        Assert.False(voter.EncryptedDocument.StartsWith(SeedDocumentMigration.PendingEncryptionPrefix));
        Assert.False(whitelist.EncryptedDocument.StartsWith(SeedDocumentMigration.PendingEncryptionPrefix));

        // Descifrado debe entregar los documentos limpios
        Assert.Equal(SuperAdminDoc, enc.Decrypt(voter.EncryptedDocument));
        Assert.Equal(WhitelistDoc, enc.Decrypt(whitelist.EncryptedDocument));
    }

    [Fact]
    public void MigrateSeedDocuments_AlreadyCorruptedRowsWithPrefix_RepairsAndDecryptsCleanDoc()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        // Simula el bug previo donde el cifrado se aplicó incluyendo el prefijo
        var corruptedVoterCipher = enc.Encrypt($"{SeedDocumentMigration.PendingEncryptionPrefix}{SuperAdminDoc}");
        var corruptedWhitelistCipher = enc.Encrypt($"{SeedDocumentMigration.PendingEncryptionPrefix}{WhitelistDoc}");

        var voter = new Voter
        {
            Id = 1,
            FullName = "Super Admin Corrupted",
            ContactEmail = "coordinacion.electoral@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = corruptedVoterCipher,
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };
        var whitelist = new CensusWhitelist
        {
            Id = 10,
            FullName = "Sofía Corrupted",
            ContactEmail = "sofia.ramirez@colegio.edu.co",
            DocumentHash = WhitelistHash,
            EncryptedDocument = corruptedWhitelistCipher,
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Voters.Add(voter);
        context.CensusWhitelists.Add(whitelist);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(1, result.UsersMigrated);
        Assert.Equal(1, result.WhitelistMigrated);

        // Tras la reparación, Decrypt devuelve el documento limpio sin prefijo
        Assert.Equal(SuperAdminDoc, enc.Decrypt(voter.EncryptedDocument));
        Assert.Equal(WhitelistDoc, enc.Decrypt(whitelist.EncryptedDocument));
    }

    [Fact]
    public void MigrateSeedDocuments_HashMismatch_DoesNotModifyAndDoesNotSave()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        const string wrongHash = "0000000000000000000000000000000000000000000000000000000000000000";
        var rawPrefixedVoter = $"{SeedDocumentMigration.PendingEncryptionPrefix}{SuperAdminDoc}";
        var rawPrefixedWhitelist = $"{SeedDocumentMigration.PendingEncryptionPrefix}{WhitelistDoc}";

        var voter = new Voter
        {
            Id = 1,
            FullName = "Mismatch User",
            ContactEmail = "mismatch@colegio.edu.co",
            DocumentHash = wrongHash,
            EncryptedDocument = rawPrefixedVoter,
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };
        var whitelist = new CensusWhitelist
        {
            Id = 10,
            FullName = "Mismatch Whitelist",
            ContactEmail = "mismatch.white@colegio.edu.co",
            DocumentHash = wrongHash,
            EncryptedDocument = rawPrefixedWhitelist,
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Voters.Add(voter);
        context.CensusWhitelists.Add(whitelist);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(0, result.UsersMigrated);
        Assert.Equal(0, result.WhitelistMigrated);
        Assert.Equal(0, result.TotalMigrated);

        Assert.Equal(rawPrefixedVoter, voter.EncryptedDocument);
        Assert.Equal(rawPrefixedWhitelist, whitelist.EncryptedDocument);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public void MigrateSeedDocuments_CorruptedRow_HashMismatch_DoesNotModify()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        const string wrongHash = "bad_hash_value";
        var corruptedCipher = enc.Encrypt($"{SeedDocumentMigration.PendingEncryptionPrefix}{SuperAdminDoc}");

        var voter = new Voter
        {
            Id = 1,
            FullName = "Corrupted Mismatch User",
            ContactEmail = "corrupted.mismatch@colegio.edu.co",
            DocumentHash = wrongHash,
            EncryptedDocument = corruptedCipher,
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };

        context.Voters.Add(voter);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(0, result.UsersMigrated);
        Assert.Equal(corruptedCipher, voter.EncryptedDocument);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public void MigrateSeedDocuments_AlreadyCorrect_DoesNotModifyAndZeroWrites()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "Correct User",
            ContactEmail = "correct@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = enc.Encrypt(SuperAdminDoc),
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };
        var whitelist = new CensusWhitelist
        {
            Id = 10,
            FullName = "Correct Whitelist",
            ContactEmail = "correct.white@colegio.edu.co",
            DocumentHash = WhitelistHash,
            EncryptedDocument = enc.Encrypt(WhitelistDoc),
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Voters.Add(voter);
        context.CensusWhitelists.Add(whitelist);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(0, result.UsersMigrated);
        Assert.Equal(0, result.WhitelistMigrated);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public void MigrateSeedDocuments_SecondExecution_IsIdempotent()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "User Run",
            ContactEmail = "run@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}{SuperAdminDoc}",
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };
        var whitelist = new CensusWhitelist
        {
            Id = 10,
            FullName = "Whitelist Run",
            ContactEmail = "whitelist.run@colegio.edu.co",
            DocumentHash = WhitelistHash,
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}{WhitelistDoc}",
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        context.Voters.Add(voter);
        context.CensusWhitelists.Add(whitelist);
        context.SaveChanges();

        // Primera corrida: debe migrar ambos
        var firstRun = SeedDocumentMigration.MigrateSeedDocuments(context, enc);
        Assert.Equal(1, firstRun.UsersMigrated);
        Assert.Equal(1, firstRun.WhitelistMigrated);

        // Segunda corrida: cero modificaciones y cero escrituras
        var secondRun = SeedDocumentMigration.MigrateSeedDocuments(context, enc);
        Assert.Equal(0, secondRun.UsersMigrated);
        Assert.Equal(0, secondRun.WhitelistMigrated);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public void MigrateSeedDocuments_PlainTextWithoutPrefix_PreservesExistingBehavior()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "Plain Text Without Prefix",
            ContactEmail = "plain@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = SuperAdminDoc, // Texto plano sin prefijo
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };

        context.Voters.Add(voter);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(1, result.UsersMigrated);
        Assert.NotEqual(SuperAdminDoc, voter.EncryptedDocument);
        Assert.Equal(SuperAdminDoc, enc.Decrypt(voter.EncryptedDocument));
    }

    [Fact]
    public void MigrateSeedDocuments_AllThreeDdlSeeds_MigratedAccurately()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        // 1. Súper Admin (users)
        var superAdmin = new Voter
        {
            Id = 1,
            FullName = "Coordinación Electoral",
            ContactEmail = "coordinacion.electoral@colegio.edu.co",
            RoleId = 3,
            DocumentHash = SuperAdminHash,
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}{SuperAdminDoc}",
            PasswordHash = "dummy",
            Status = "ACTIVO"
        };

        // 2. Entrada de lista blanca (census_whitelist)
        var whitelistEntry = new CensusWhitelist
        {
            Id = 1,
            FullName = "Sofía Ramírez Torres",
            ContactEmail = "sofia.ramirez@colegio.edu.co",
            GradeId = 6,
            DocumentHash = WhitelistHash,
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}{WhitelistDoc}",
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow
        };

        // 3. Elector de prueba ya auto-registrado (users)
        var elector = new Voter
        {
            Id = 2,
            FullName = "Ana María López Pérez",
            ContactEmail = "acudiente.ana.lopez@example.com",
            RoleId = 1,
            GradeId = 6,
            DocumentHash = ElectorHash,
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}{ElectorDoc}",
            PasswordHash = "dummy",
            Status = "ACTIVO"
        };

        context.Voters.AddRange(superAdmin, elector);
        context.CensusWhitelists.Add(whitelistEntry);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(2, result.UsersMigrated);
        Assert.Equal(1, result.WhitelistMigrated);
        Assert.Equal(3, result.TotalMigrated);

        Assert.Equal(SuperAdminDoc, enc.Decrypt(superAdmin.EncryptedDocument));
        Assert.Equal(ElectorDoc, enc.Decrypt(elector.EncryptedDocument));
        Assert.Equal(WhitelistDoc, enc.Decrypt(whitelistEntry.EncryptedDocument));
    }

    [Fact]
    public void MigrateSeedDocuments_ExplicitAuthService_DelegatesToAuthServiceHashDocument()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        var trackingAuth = new TrackingAuthService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "Super Admin With AuthService",
            ContactEmail = "coordinacion@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}{SuperAdminDoc}",
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };

        context.Voters.Add(voter);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc, trackingAuth);

        Assert.Equal(1, result.UsersMigrated);
        Assert.Equal(1, trackingAuth.HashDocumentCalls);
        Assert.Equal(SuperAdminDoc, enc.Decrypt(voter.EncryptedDocument));
    }

    [Fact]
    public void MigrateSeedDocuments_AlreadyCorrect_VerifiesZeroCallsToSaveChanges()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        using var countingContext = new CountingDbContext(options);
        var enc = CreateRealEncryptionService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "Already Correct",
            ContactEmail = "correct@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = enc.Encrypt(SuperAdminDoc),
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };

        countingContext.Voters.Add(voter);
        countingContext.SaveChanges();
        int initialSaves = countingContext.SaveChangesCount;

        var result = SeedDocumentMigration.MigrateSeedDocuments(countingContext, enc);

        Assert.Equal(0, result.UsersMigrated);
        Assert.Equal(0, result.WhitelistMigrated);
        Assert.Equal(initialSaves, countingContext.SaveChangesCount);
    }

    [Fact]
    public void MigrateSeedDocuments_CaseInsensitivePrefix_MigratesCleanly()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "Lowercase Prefix User",
            ContactEmail = "lower@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = $"pendiente_cifrar:{SuperAdminDoc}",
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };

        context.Voters.Add(voter);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(1, result.UsersMigrated);
        Assert.Equal(SuperAdminDoc, enc.Decrypt(voter.EncryptedDocument));
    }

    [Fact]
    public void MigrateSeedDocuments_UpperCaseStoredHash_MatchesCaseInsensitively()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "Upper Hash User",
            ContactEmail = "upper@colegio.edu.co",
            DocumentHash = SuperAdminHash.ToUpperInvariant(),
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}{SuperAdminDoc}",
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };

        context.Voters.Add(voter);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(1, result.UsersMigrated);
        Assert.Equal(SuperAdminDoc, enc.Decrypt(voter.EncryptedDocument));
    }

    [Fact]
    public void MigrateSeedDocuments_EmptyDocAfterPrefix_SkipsGracefully()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "Empty Doc User",
            ContactEmail = "empty@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = $"{SeedDocumentMigration.PendingEncryptionPrefix}",
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };

        context.Voters.Add(voter);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(0, result.UsersMigrated);
        Assert.Equal($"{SeedDocumentMigration.PendingEncryptionPrefix}", voter.EncryptedDocument);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    [Fact]
    public void MigrateSeedDocuments_NullOrWhitespaceStored_SkipsGracefully()
    {
        using var context = CreateContext();
        var enc = CreateRealEncryptionService();

        var voter = new Voter
        {
            Id = 1,
            FullName = "Whitespace Doc User",
            ContactEmail = "ws@colegio.edu.co",
            DocumentHash = SuperAdminHash,
            EncryptedDocument = "   ",
            PasswordHash = "dummy",
            RoleId = 3,
            Status = "ACTIVO"
        };

        context.Voters.Add(voter);
        context.SaveChanges();

        var result = SeedDocumentMigration.MigrateSeedDocuments(context, enc);

        Assert.Equal(0, result.UsersMigrated);
        Assert.False(context.ChangeTracker.HasChanges());
    }

    private sealed class TrackingAuthService : IAuthService
    {
        public int HashDocumentCalls { get; private set; }

        public string HashDocument(string document)
        {
            HashDocumentCalls++;
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(document));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public Task<Voter?> ValidateLoginAsync(string document, string password, string? ipAddress) => Task.FromResult<Voter?>(null);
        public Task<bool> ChangePasswordAsync(int voterId, string newPassword, string ipAddress) => Task.FromResult(true);
        public string HashPassword(string password) => "dummy";
    }

    private sealed class CountingDbContext : WahlMiraiDbContext
    {
        public int SaveChangesCount { get; private set; }

        public CountingDbContext(DbContextOptions<WahlMiraiDbContext> options) : base(options) { }

        public override int SaveChanges()
        {
            SaveChangesCount++;
            return base.SaveChanges();
        }
    }
}
