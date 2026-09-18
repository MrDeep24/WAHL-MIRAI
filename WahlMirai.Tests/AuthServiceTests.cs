using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task ValidateLoginAsync_WithCorrectCredentials_ReturnsActiveVoterAndAuditsSuccess()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var service = new AuthService(context, audit);
        var passwordHash = service.HashPassword("Clave!123");

        context.Voters.Add(CreateVoter(service.HashDocument("123456"), passwordHash, "ACTIVO"));
        await context.SaveChangesAsync();

        var result = await service.ValidateLoginAsync("123456", "Clave!123");

        Assert.NotNull(result);
        Assert.Equal("ACTIVO", result.Status);
        Assert.Contains("LOGIN_SUCCESS", audit.Actions);
    }

    [Fact]
    public async Task ValidateLoginAsync_WithWrongPassword_ReturnsNullAndAuditsFailure()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var service = new AuthService(context, audit);

        context.Voters.Add(CreateVoter(service.HashDocument("123456"), service.HashPassword("Clave!123"), "ACTIVO"));
        await context.SaveChangesAsync();

        var result = await service.ValidateLoginAsync("123456", "ClaveIncorrecta");

        Assert.Null(result);
        Assert.Contains("LOGIN_FAILED", audit.Actions);
    }

    [Fact]
    public async Task ValidateLoginAsync_WithInactiveVoter_ReturnsNull()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var service = new AuthService(context, audit);

        context.Voters.Add(CreateVoter(service.HashDocument("123456"), service.HashPassword("Clave!123"), "INACTIVO"));
        await context.SaveChangesAsync();

        var result = await service.ValidateLoginAsync("123456", "Clave!123");

        Assert.Null(result);
        Assert.Empty(audit.Actions);
    }

    [Fact]
    public void HashDocument_WithSameInput_IsDeterministic()
    {
        var service = new AuthService(CreateContext(), new FakeAuditService());

        var firstHash = service.HashDocument("123456");
        var secondHash = service.HashDocument("123456");

        Assert.Equal(firstHash, secondHash);
        Assert.NotEqual(firstHash, service.HashDocument("654321"));
    }

    [Fact]
    public void HashPassword_CanBeVerifiedWithBCrypt()
    {
        var service = new AuthService(CreateContext(), new FakeAuditService());

        var hash = service.HashPassword("Clave!123");

        Assert.NotEqual("Clave!123", hash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Clave!123", hash));
    }

    [Fact]
    public async Task ChangePasswordAsync_UpdatesHashAndWritesAuditEntry()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var service = new AuthService(context, audit);
        var oldHash = service.HashPassword("ClaveAnterior!123");
        var voter = CreateVoter("document-hash", oldHash, "ACTIVO");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        var changed = await service.ChangePasswordAsync((int)voter.Id, "ClaveNueva!123", "127.0.0.1");

        Assert.True(changed);
        Assert.NotEqual(oldHash, voter.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify("ClaveNueva!123", voter.PasswordHash));
        Assert.Contains("PASSWORD_CHANGED", audit.Actions);
    }

    private static WahlMiraiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new WahlMiraiDbContext(options);
        context.Roles.Add(new Role { Id = 3, Name = "ELECTOR" });
        return context;
    }

    private static Voter CreateVoter(string documentHash, string passwordHash, string status) => new()
    {
        Id = 1,
        RoleId = 3,
        DocumentHash = documentHash,
        EncryptedDocument = "encrypted-document",
        FullName = "Test Voter",
        ContactEmail = "voter@example.com",
        PasswordHash = passwordHash,
        Status = status,
        RegisteredAt = DateTime.UtcNow
    };
}
