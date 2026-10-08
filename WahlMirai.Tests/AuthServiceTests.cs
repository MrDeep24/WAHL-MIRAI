using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class AuthServiceTests
{
    [Fact]
    public async Task ValidateLoginAsync_WithCorrectCredentials_ReturnsActiveVoterAndAuditsSuccess()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var loginStore = new LoginAttemptStore(timeProvider);
        var service = new AuthService(context, audit, loginStore);
        var passwordHash = service.HashPassword("Clave!123");

        context.Voters.Add(CreateVoter(service.HashDocument("123456"), passwordHash, "ACTIVO"));
        await context.SaveChangesAsync();

        var result = await service.ValidateLoginAsync("123456", "Clave!123", "192.168.1.50");

        Assert.NotNull(result);
        Assert.Equal("ACTIVO", result.Status);
        Assert.Contains("LOGIN_SUCCESS", audit.Actions);
        Assert.Contains("192.168.1.50", audit.IpAddresses);
    }

    [Fact]
    public async Task ValidateLoginAsync_WithWrongPassword_ReturnsNullAndAuditsFailure()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var loginStore = new LoginAttemptStore(timeProvider);
        var service = new AuthService(context, audit, loginStore);

        context.Voters.Add(CreateVoter(service.HashDocument("123456"), service.HashPassword("Clave!123"), "ACTIVO"));
        await context.SaveChangesAsync();

        var result = await service.ValidateLoginAsync("123456", "ClaveIncorrecta", "192.168.1.51");

        Assert.Null(result);
        Assert.Contains("LOGIN_FAILED", audit.Actions);
        Assert.Contains("192.168.1.51", audit.IpAddresses);
    }

    [Fact]
    public async Task ValidateLoginAsync_WithInactiveVoter_ReturnsNull()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var loginStore = new LoginAttemptStore(timeProvider);
        var service = new AuthService(context, audit, loginStore);

        context.Voters.Add(CreateVoter(service.HashDocument("123456"), service.HashPassword("Clave!123"), "INACTIVO"));
        await context.SaveChangesAsync();

        var result = await service.ValidateLoginAsync("123456", "Clave!123", "192.168.1.52");

        Assert.Null(result);
        Assert.Empty(audit.Actions);
    }

    [Fact]
    public async Task ValidateLoginAsync_WhenLockedOut_ReturnsNullAndDoesNotAudit()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var loginStore = new LoginAttemptStore(timeProvider);
        var service = new AuthService(context, audit, loginStore);
        var doc = "123456";
        var docHash = service.HashDocument(doc);

        context.Voters.Add(CreateVoter(docHash, service.HashPassword("Clave!123"), "ACTIVO"));
        await context.SaveChangesAsync();

        // Cause 5 failed attempts to lock out the account
        for (int i = 0; i < 5; i++)
        {
            await service.ValidateLoginAsync(doc, "ClaveErronea", "10.0.0.1");
        }

        audit.Actions.Clear();

        // 6th attempt with CORRECT credentials while locked out
        var result = await service.ValidateLoginAsync(doc, "Clave!123", "10.0.0.1");

        // Must return null and perform NO audit writes
        Assert.Null(result);
        Assert.Empty(audit.Actions);
    }

    [Fact]
    public async Task ValidateLoginAsync_IpReachesAuditCall_ForSuccessAndFailure()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var loginStore = new LoginAttemptStore(timeProvider);
        var service = new AuthService(context, audit, loginStore);
        const string testIp = "200.50.100.25";

        context.Voters.Add(CreateVoter(service.HashDocument("888888"), service.HashPassword("Pass!123"), "ACTIVO"));
        await context.SaveChangesAsync();

        // Failed login
        await service.ValidateLoginAsync("888888", "WrongPass", testIp);
        Assert.Equal(testIp, audit.IpAddresses[^1]);

        // Successful login
        await service.ValidateLoginAsync("888888", "Pass!123", testIp);
        Assert.Equal(testIp, audit.IpAddresses[^1]);
    }

    [Fact]
    public async Task ValidateLoginAsync_WhenLockoutThresholdReached_LogsLoginLockedOutOnce()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var timeProvider = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var loginStore = new LoginAttemptStore(timeProvider);
        var service = new AuthService(context, audit, loginStore);
        var doc = "777777";

        context.Voters.Add(CreateVoter(service.HashDocument(doc), service.HashPassword("Pass!123"), "ACTIVO"));
        await context.SaveChangesAsync();

        // 4 failures: only LOGIN_FAILED
        for (int i = 0; i < 4; i++)
        {
            await service.ValidateLoginAsync(doc, "BadPass", "127.0.0.1");
        }
        Assert.DoesNotContain("LOGIN_LOCKED_OUT", audit.Actions);

        // 5th failure: threshold reached -> triggers LOGIN_LOCKED_OUT exactly once
        await service.ValidateLoginAsync(doc, "BadPass", "127.0.0.1");
        Assert.Single(audit.Actions, a => a == "LOGIN_LOCKED_OUT");
    }

    [Fact]
    public void HashDocument_WithSameInput_IsDeterministic()
    {
        var service = new AuthService(CreateContext(), new FakeAuditService(), new LoginAttemptStore(new FakeTimeProvider(DateTimeOffset.UtcNow)));

        var firstHash = service.HashDocument("123456");
        var secondHash = service.HashDocument("123456");

        Assert.Equal(firstHash, secondHash);
        Assert.NotEqual(firstHash, service.HashDocument("654321"));
    }

    [Fact]
    public void HashPassword_CanBeVerifiedWithBCrypt()
    {
        var service = new AuthService(CreateContext(), new FakeAuditService(), new LoginAttemptStore(new FakeTimeProvider(DateTimeOffset.UtcNow)));

        var hash = service.HashPassword("Clave!123");

        Assert.NotEqual("Clave!123", hash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Clave!123", hash));
    }

    [Fact]
    public async Task ChangePasswordAsync_UpdatesHashAndWritesAuditEntry()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var service = new AuthService(context, audit, new LoginAttemptStore(new FakeTimeProvider(DateTimeOffset.UtcNow)));
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
