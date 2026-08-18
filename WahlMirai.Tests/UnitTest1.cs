using System.Text;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

using Microsoft.EntityFrameworkCore.Diagnostics;

namespace WahlMirai.Tests;

public class UnitTest1
{
    private WahlMiraiDbContext GetInMemoryDbContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new WahlMiraiDbContext(options);

        // Seed roles & grades
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Role { Id = 1, Name = "ADMIN", Description = "Administrador del sistema" },
                new Role { Id = 2, Name = "ELECTOR", Description = "Estudiante votante" }
            );
        }

        if (!context.Grades.Any())
        {
            context.Grades.AddRange(
                new Grade { Id = 1, Name = "6°", SequenceOrder = 1, IsLastGrade = false },
                new Grade { Id = 2, Name = "7°", SequenceOrder = 2, IsLastGrade = false },
                new Grade { Id = 6, Name = "11°", SequenceOrder = 6, IsLastGrade = true }
            );
        }

        if (!context.AcademicYears.Any())
        {
            context.AcademicYears.Add(new AcademicYear
            {
                Id = 1,
                Year = 2026,
                IsCurrent = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        context.SaveChanges();
        return context;
    }

    private class MockAuthService : IAuthService
    {
        public Task<Voter?> ValidateLoginAsync(string document, string password) => Task.FromResult<Voter?>(null);
        public Task<string> GenerateInitialPasswordAsync(string document) => Task.FromResult("Pass123");
        public Task<bool> ChangePasswordAsync(int voterId, string newPassword, string ipAddress) => Task.FromResult(true);
        public string HashPassword(string password) => "hashed_pass";
        public string HashDocument(string document) => "hash_" + document;
    }

    private class MockAuditService : IAuditService
    {
        public Task LogAsync(string action, int? voterId, string targetEntity, int? targetId, string? fieldName, string? oldValue, string? newValue, string? details, string? ipAddress)
        {
            return Task.CompletedTask;
        }
    }

    private class MockEncryptionService : IDocumentEncryptionService
    {
        public string Encrypt(string plainText) => "enc_" + plainText;
        public string Decrypt(string cipherText) => cipherText.Replace("enc_", "");
    }

    private class MockCredentialService : ICredentialService
    {
        public Task IssueNewPasswordAsync(int voterId, EmailType emailType, int? actorVoterId, CancellationToken ct = default)
        {
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Test_IndividualVoterRegistration_ValidAndDuplicate()
    {
        // Arrange
        using var context = GetInMemoryDbContext("Db_IndividualRegistrationTest");
        var censusService = new CensusService(
            context,
            new MockAuthService(),
            new MockAuditService(),
            new MockEncryptionService(),
            new MockCredentialService()
        );

        // Act - Registration of valid voter
        var voter = await censusService.AddVoterAsync("1098765432", "Estudiante Prueba", "prueba@colegio.edu.co", 1, 2, false, "127.0.0.1");

        // Assert
        Assert.NotNull(voter);
        Assert.Equal("Estudiante Prueba", voter.FullName);
        Assert.Equal("ACTIVO", voter.Status);

        // Act & Assert - Registration of duplicate document should throw
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await censusService.AddVoterAsync("1098765432", "Otro Nombre", "otro@colegio.edu.co", 1, 2, false, "127.0.0.1");
        });
    }

    [Fact]
    public async Task Test_CsvImport_ValidAndInvalidHandling()
    {
        // Arrange
        using var context = GetInMemoryDbContext("Db_CsvImportTest");
        var censusService = new CensusService(
            context,
            new MockAuthService(),
            new MockAuditService(),
            new MockEncryptionService(),
            new MockCredentialService()
        );

        var csvContent = "documento,nombre,correo_contacto,grado_id,excluir_promocion\n" +
                         "1001,Juan Perez,juan@colegio.edu.co,1,0\n" +
                         "1002,Maria Lopez,maria@colegio.edu.co,2,1\n" +
                         "ABC_INVALIDO,Invalido,inv@colegio.edu.co,1,0\n" +
                         "1001,Juan Duplicado,duplicado@colegio.edu.co,1,0\n";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await censusService.ImportCsvAsync(stream, "127.0.0.1");

        // Assert
        Assert.Equal(4, result.ProcessedCount);
        Assert.Equal(2, result.InsertedCount);
        Assert.Equal(1, result.DuplicateCount);
        Assert.Equal(1, result.ErrorCount);
        Assert.Single(result.Errors.Where(e => e.Reason.Contains("sólo números")));
    }

    [Fact]
    public async Task Test_SoftDeleteAndRestoreVoter()
    {
        // Arrange
        using var context = GetInMemoryDbContext("Db_SoftDeleteTest");
        var censusService = new CensusService(
            context,
            new MockAuthService(),
            new MockAuditService(),
            new MockEncryptionService(),
            new MockCredentialService()
        );

        var voter = await censusService.AddVoterAsync("2001", "Pedro Gomez", "pedro@colegio.edu.co", 1, 2, false, "127.0.0.1");
        int voterId = (int)voter.Id;

        // Act - Soft Delete
        bool deleteSuccess = await censusService.SoftDeleteVoterAsync(voterId, "127.0.0.1");
        var deletedVoter = await context.Voters.FindAsync((uint)voterId);

        // Assert
        Assert.True(deleteSuccess);
        Assert.Equal("ELIMINADO", deletedVoter!.Status);
        Assert.NotNull(deletedVoter.DeletedAt);

        // Act - Restore
        bool restoreSuccess = await censusService.RestoreVoterAsync(voterId, "127.0.0.1");
        var restoredVoter = await context.Voters.FindAsync((uint)voterId);

        // Assert
        Assert.True(restoreSuccess);
        Assert.Equal("ACTIVO", restoredVoter!.Status);
        Assert.Null(restoredVoter.DeletedAt);
    }

    [Fact]
    public async Task Test_AutomaticPromotionService()
    {
        // Arrange
        using var context = GetInMemoryDbContext("Db_PromotionTest");
        var promotionService = new PromotionService(context, new MockAuditService());

        // Add 3 voters: Grade 1 (6°), Grade 6 (11°), and Grade 1 (Repitente)
        context.Voters.AddRange(
            new Voter { DocumentHash = "h1", EncryptedDocument = "e1", FullName = "Est 6to", ContactEmail = "e6@test.com", GradeId = 1, RoleId = 2, Status = "ACTIVO", ExcluirDePromocion = false, PasswordHash = "p" },
            new Voter { DocumentHash = "h2", EncryptedDocument = "e2", FullName = "Est 11to", ContactEmail = "e11@test.com", GradeId = 6, RoleId = 2, Status = "ACTIVO", ExcluirDePromocion = false, PasswordHash = "p" },
            new Voter { DocumentHash = "h3", EncryptedDocument = "e3", FullName = "Est Repitente", ContactEmail = "rep@test.com", GradeId = 1, RoleId = 2, Status = "ACTIVO", ExcluirDePromocion = true, PasswordHash = "p" }
        );
        await context.SaveChangesAsync();

        // Act - Preview
        var preview = await promotionService.GetPromotionPreviewAsync();
        Assert.Equal(2, preview.EligibleCount);
        Assert.Equal(1, preview.ExcludedCount);
        Assert.Equal(1, preview.ToGraduateCount);

        // Act - Run Promotion
        var result = await promotionService.RunPromotionAsync(false, "127.0.0.1");

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.PromotedCount);
        Assert.Equal(1, result.GraduatedCount);
        Assert.Equal(1, result.RetainedCount);

        var est6 = await context.Voters.FirstAsync(v => v.FullName == "Est 6to");
        Assert.Equal((byte)2, est6.GradeId); // Promovido a 7° (ID 2)

        var est11 = await context.Voters.FirstAsync(v => v.FullName == "Est 11to");
        Assert.Equal("EGRESADO", est11.Status); // Egresado

        var estRep = await context.Voters.FirstAsync(v => v.FullName == "Est Repitente");
        Assert.Equal((byte)1, estRep.GradeId); // Mantiene 6°
        Assert.False(estRep.ExcluirDePromocion); // Bandera reiniciada para el nuevo ciclo
    }
}
