using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class AdminAccountServiceTests
{
    [Fact]
    public async Task CreateAdmin_StoresAccountAndInitialCredentialQueue()
    {
        await using var context = CreateContext();
        var superAdmin = AddAccount(context, 1, "SUPER_ADMIN", "super@example.com");
        await context.SaveChangesAsync();
        var audit = new FakeAuditService();
        var service = CreateService(context, new PendingPasswordStore(), audit);

        await service.CreateAsync("123456", "Nueva Admin", "admin@example.com", "ADMIN", "Orientadora", 1, "127.0.0.1");

        var account = await context.Voters.Include(v => v.Role).SingleAsync(v => v.FullName == "Nueva Admin");
        Assert.Equal("ADMIN", account.Role.Name);
        Assert.Null(account.GradeId);
        Assert.Single(context.EmailQueues);
        Assert.Equal("REASIGNACION_ADMIN", context.EmailQueues.Single().EmailType);
        Assert.Contains(audit.Actions, action => action == "ADMIN_ACCOUNT_CREATED");
    }

    [Fact]
    public async Task SoftDelete_BlocksSelfDeletionAndLastSuperAdminDeletion()
    {
        await using var context = CreateContext();
        AddAccount(context, 1, "SUPER_ADMIN", "super@example.com");
        AddAccount(context, 3, "ADMIN", "admin@example.com");
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SoftDeleteAsync(1, 1, "127.0.0.1"));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SoftDeleteAsync(1, 3, "127.0.0.1"));
    }

    [Fact]
    public async Task CreateAccount_RejectsElectorRole()
    {
        await using var context = CreateContext();
        AddAccount(context, 1, "SUPER_ADMIN", "super@example.com");
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync("123456", "Nombre", "user@example.com", "ELECTOR", null, 1, "127.0.0.1"));
    }

    private static WahlMiraiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var context = new WahlMiraiDbContext(options);
        context.Roles.AddRange(
            new Role { Id = 1, Name = "SUPER_ADMIN" },
            new Role { Id = 2, Name = "ADMIN" },
            new Role { Id = 3, Name = "ELECTOR" });
        return context;
    }

    private static Voter AddAccount(WahlMiraiDbContext context, uint id, string roleName, string email)
    {
        var account = new Voter
        {
            Id = id,
            RoleId = roleName == "SUPER_ADMIN" ? (byte)1 : (byte)2,
            DocumentHash = $"hash-{id}",
            EncryptedDocument = $"enc-{id}",
            FullName = roleName,
            ContactEmail = email,
            PasswordHash = "hash",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        };
        account.Role = context.Roles.Local.Single(role => role.Name == roleName);
        context.Voters.Add(account);
        return account;
    }

    private static IAdminAccountService CreateService(WahlMiraiDbContext context, IPendingPasswordStore? passwordStore = null, IAuditService? audit = null) =>
        new AdminAccountService(context, new FakeAuthService(), new FakeEncryptionService(),
            new CredentialService(context, passwordStore ?? new PendingPasswordStore(), audit ?? new FakeAuditService()), audit ?? new FakeAuditService());

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