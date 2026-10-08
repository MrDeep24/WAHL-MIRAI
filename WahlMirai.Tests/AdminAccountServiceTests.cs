using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class AdminAccountServiceTests
{
    /// <summary>Verifica que la búsqueda paginada de M09 coincida parcialmente sin distinguir mayúsculas (RF-M09-01).</summary>
    [Fact]
    public async Task GetPagedAccountsAsync_SearchesNameEmailAndPositionTitle()
    {
        await using var context = CreateContext();
        var nameMatch = AddAccount(context, 1, "ADMIN", "name@example.com");
        nameMatch.FullName = "Alice Admin";
        var emailMatch = AddAccount(context, 2, "ADMIN", "findme@example.com");
        emailMatch.FullName = "Bob Admin";
        var titleMatch = AddAccount(context, 3, "SUPER_ADMIN", "title@example.com");
        titleMatch.FullName = "Carol Admin";
        titleMatch.PositionTitle = "Data Steward";
        AddAccount(context, 4, "ADMIN", "other@example.com").FullName = "David Admin";
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var byName = await service.GetPagedAccountsAsync(1, 20, "ALICE", null, true);
        var byEmail = await service.GetPagedAccountsAsync(1, 20, "FINDME", null, true);
        var byTitle = await service.GetPagedAccountsAsync(1, 20, "steward", null, true);

        Assert.Equal(new uint[] { 1 }, byName.Items.Select(item => item.Id));
        Assert.Equal(new uint[] { 2 }, byEmail.Items.Select(item => item.Id));
        Assert.Equal(new uint[] { 3 }, byTitle.Items.Select(item => item.Id));
    }

    /// <summary>Verifica que una búsqueda M09 sin coincidencias devuelva página vacía y total cero (RF-M09-01).</summary>
    [Fact]
    public async Task GetPagedAccountsAsync_NoMatchesReturnsEmptyPageAndZeroTotal()
    {
        await using var context = CreateContext();
        AddAccount(context, 1, "ADMIN", "admin@example.com");
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.GetPagedAccountsAsync(1, 20, "missing", null, true);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    /// <summary>Verifica orden ascendente y descendente por nombre en la lista M09 (RF-M09-01).</summary>
    [Fact]
    public async Task GetPagedAccountsAsync_SortsFullNameAscendingAndDescending()
    {
        await using var context = CreateContext();
        AddAccount(context, 1, "ADMIN", "z@example.com").FullName = "Zara Admin";
        AddAccount(context, 2, "ADMIN", "a@example.com").FullName = "Ana Admin";
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var ascending = await service.GetPagedAccountsAsync(1, 20, null, "FullName", true);
        var descending = await service.GetPagedAccountsAsync(1, 20, null, "FullName", false);

        Assert.Equal(new[] { "Ana Admin", "Zara Admin" }, ascending.Items.Select(item => item.FullName));
        Assert.Equal(new[] { "Zara Admin", "Ana Admin" }, descending.Items.Select(item => item.FullName));
    }

    /// <summary>Verifica que una clave de orden M09 desconocida use FullName ascendente (RF-M09-01).</summary>
    [Fact]
    public async Task GetPagedAccountsAsync_UnknownSortFallsBackToFullNameAscending()
    {
        await using var context = CreateContext();
        AddAccount(context, 1, "ADMIN", "z@example.com").FullName = "Zara Admin";
        AddAccount(context, 2, "ADMIN", "a@example.com").FullName = "Ana Admin";
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.GetPagedAccountsAsync(1, 20, null, "NotAColumn", false);

        Assert.Equal(new[] { "Ana Admin", "Zara Admin" }, result.Items.Select(item => item.FullName));
    }

    /// <summary>Verifica que una página M09 fuera del conjunto no arroje error y conserve el total (RF-M09-01).</summary>
    [Fact]
    public async Task GetPagedAccountsAsync_PageBeyondDataReturnsEmptyItemsAndCorrectTotal()
    {
        await using var context = CreateContext();
        AddAccount(context, 1, "ADMIN", "one@example.com");
        AddAccount(context, 2, "ADMIN", "two@example.com");
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.GetPagedAccountsAsync(3, 1, null, null, true);

        Assert.Empty(result.Items);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(3, result.PageNumber);
    }

    /// <summary>Verifica el límite de resultados y la inclusión predeterminada de eliminadas en M09 (RF-M09-01).</summary>
    [Fact]
    public async Task GetPagedAccountsAsync_RespectsPageSizeAndIncludesDeletedByDefault()
    {
        await using var context = CreateContext();
        AddAccount(context, 1, "ADMIN", "one@example.com");
        AddAccount(context, 2, "ADMIN", "two@example.com").Status = "ELIMINADO";
        AddAccount(context, 3, "ADMIN", "three@example.com");
        await context.SaveChangesAsync();
        var service = CreateService(context);

        var result = await service.GetPagedAccountsAsync(1, 2, null, null, true);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(3, result.TotalCount);
    }

    [Fact]
    public async Task CreateAdmin_StoresAccountAndInitialCredentialQueue()
    {
        await using var context = CreateContext();
        var superAdmin = AddAccount(context, 1, "SUPER_ADMIN", "super@example.com");
        await context.SaveChangesAsync();
        var audit = new FakeAuditService();
        var service = CreateService(context, new PendingPasswordStore(), audit);

        await service.CreateAsync("123456", "Nueva Admin", "admin@example.com", "ADMIN", "Orientador(a) Escolar", 1, "127.0.0.1");

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

    [Fact]
    public async Task CreateAccount_RejectsInvalidName()
    {
        await using var context = CreateContext();
        AddAccount(context, 1, "SUPER_ADMIN", "super@example.com");
        await context.SaveChangesAsync();
        var service = CreateService(context);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateAsync("123456", "Nombre123", "user@example.com", "ADMIN", null, 1, "127.0.0.1"));
    }

    [Fact]
    public async Task RegisterElectorAsync_ClaimsWhitelistEntryAndCreatesActiveUser()
    {
        await using var context = CreateContext();
        context.Roles.Add(new Role { Id = 4, Name = "ELECTOR" });
        context.Grades.Add(new Grade { Id = 1, Name = "8°", SequenceOrder = 1, IsLastGrade = false });
        context.CensusWhitelists.Add(new CensusWhitelist
        {
            Id = 1,
            DocumentHash = "hash-123456",
            EncryptedDocument = "enc-123456",
            FullName = "Ana Gómez",
            GradeId = 1,
            UploadedByUserId = 1,
            CreatedAt = DateTime.UtcNow,
            Grade = context.Grades.Local.Single(g => g.Id == 1)
        });
        await context.SaveChangesAsync();

        var whitelistService = new WhitelistService(context, new FakeAuthService(), new FakeEncryptionService());

        var created = await whitelistService.RegisterElectorAsync("123456", 1, "ana@example.com", "Clave!123", "127.0.0.1");

        Assert.Equal("ACTIVO", created.Status);
        Assert.Equal("Ana Gómez", created.FullName);
        Assert.Equal("ana@example.com", created.ContactEmail);
        Assert.NotNull(created.Role);
        Assert.Equal("ELECTOR", created.Role.Name);

        var whitelist = await context.CensusWhitelists.SingleAsync(w => w.Id == 1);
        Assert.NotNull(whitelist.ClaimedAt);
        Assert.Equal(created.Id, whitelist.ClaimedByUserId);

        Assert.Contains(context.AuditLogs, a => a.Action == "SELF_REGISTER" && a.TargetId == (int)created.Id);
    }

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

}