using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

public class ProfileServiceTests
{
    [Fact]
    public async Task UpdateContactEmailAsync_WhenNotificationSucceeds_SavesEmailAndReportsBothFlags()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var emailSender = new FakeEmailSender();
        var service = CreateService(context, audit, emailSender);
        context.Voters.Add(CreateVoter(1, "old@example.com", "OldPassword!123"));
        await context.SaveChangesAsync();

        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "127.0.0.1");

        Assert.True(result.Success);
        Assert.True(result.EmailSaved);
        Assert.True(result.NotificationSent);
        Assert.Equal(string.Empty, result.ErrorMessage);
        Assert.Equal("new@example.com", (await context.Voters.FindAsync((uint)1))!.ContactEmail);
        Assert.Single(emailSender.Messages);
    }

    [Fact]
    public async Task UpdateContactEmailAsync_WhenNotificationFails_KeepsSavedFlagAndReportsNotificationFailure()
    {
        await using var context = CreateContext();
        var emailSender = new FakeEmailSender { ShouldFail = true };
        var service = CreateService(context, new FakeAuditService(), emailSender);
        context.Voters.Add(CreateVoter(1, "old@example.com", "OldPassword!123"));
        await context.SaveChangesAsync();

        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "127.0.0.1");

        Assert.True(result.Success);
        Assert.True(result.EmailSaved);
        Assert.False(result.NotificationSent);
        Assert.Equal("new@example.com", (await context.Voters.FindAsync((uint)1))!.ContactEmail);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithWrongCurrentPassword_BlocksUpdate()
    {
        await using var context = CreateContext();
        var voter = CreateVoter(1, "old@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();
        var service = CreateService(context, new FakeAuditService(), new FakeEmailSender());
        var originalHash = voter.PasswordHash;

        var result = await service.UpdateProfileAsync(1, "new@example.com", "WrongPassword!123", "NewPassword!123", "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal("La contraseña actual no coincide.", result.ErrorMessage);
        Assert.Equal("old@example.com", voter.ContactEmail);
        Assert.Equal(originalHash, voter.PasswordHash);
    }

    [Fact]
    public async Task UpdateProfileAsync_WithCorrectCurrentPassword_AllowsNewPassword()
    {
        await using var context = CreateContext();
        var voter = CreateVoter(1, "old@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();
        var service = CreateService(context, new FakeAuditService(), new FakeEmailSender());

        var result = await service.UpdateProfileAsync(1, null, "CorrectPassword!123", "NewPassword!123", "127.0.0.1");

        Assert.True(result.Success);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword!123", voter.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("CorrectPassword!123", voter.PasswordHash));
    }

    private static ProfileService CreateService(WahlMiraiDbContext context, IAuditService audit, IEmailSender emailSender) =>
        new(context, audit, emailSender, new FakeCredentialService());

    private static WahlMiraiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new WahlMiraiDbContext(options);
    }

    private static Voter CreateVoter(uint id, string email, string password) => new()
    {
        Id = id,
        RoleId = 3,
        DocumentHash = $"document-{id}",
        EncryptedDocument = $"encrypted-{id}",
        FullName = "Test Voter",
        ContactEmail = email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
        Status = "ACTIVO",
        RegisteredAt = DateTime.UtcNow
    };

    private sealed class FakeEmailSender : IEmailSender
    {
        public bool ShouldFail { get; init; }
        public List<string> Messages { get; } = new();

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (ShouldFail)
            {
                throw new InvalidOperationException("SMTP unavailable");
            }

            Messages.Add(toEmail);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCredentialService : ICredentialService
    {
        public Task IssueNewPasswordAsync(int voterId, EmailType emailType, int? actorVoterId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
