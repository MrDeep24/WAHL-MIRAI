// ============================================================
// Archivo:       CredentialServiceTests.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias de emisión de tokens de recuperación,
//                bloqueo de contraseñas para RECUPERACION_ACCESO y vigencia de REASIGNACION_ADMIN.
// Requerimientos: RF-M01-02, RF-M07-02, RN-2, RN-9.
// ============================================================
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias de las operaciones de CredentialService para tokens y contraseñas (RF-M01-02, RF-M07-02).
/// </summary>
public class CredentialServiceTests
{
    private const uint TestVoterId = 101;
    private const string InitialHash = "$2a$11$InitialPasswordHashMock1234567890123456789012";

    [Fact]
    public async Task IssuePasswordResetTokenAsync_LeavesPasswordHashUntouched_EnqueuesEmailAndStoresToken()
    {
        await using var context = CreateContext();
        SeedVoter(context, TestVoterId, "ACTIVO");

        var passwordStore = new PendingPasswordStore();
        var tokenStore = new PasswordResetTokenStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var audit = new FakeAuditService();
        var service = new CredentialService(context, passwordStore, audit, tokenStore);

        await service.IssuePasswordResetTokenAsync((int)TestVoterId, actorUserId: null);

        // 1. PasswordHash permanece estrictamente intacto (RN-2)
        var voter = await context.Voters.FindAsync(TestVoterId);
        Assert.NotNull(voter);
        Assert.Equal(InitialHash, voter.PasswordHash);

        // 2. Se encola la notificación RECUPERACION_ACCESO
        var queueItem = await context.EmailQueues.SingleAsync();
        Assert.Equal(TestVoterId, queueItem.VoterId);
        Assert.Equal(EmailType.RECUPERACION_ACCESO.ToString(), queueItem.EmailType);
        Assert.Equal("PENDIENTE", queueItem.Status);
        Assert.Equal((byte)0, queueItem.Attempts);

        // 3. El token crudo se guarda en PendingPasswordStore bajo el Id de la cola
        Assert.True(passwordStore.TryGetPassword(queueItem.Id, out var rawToken));
        Assert.False(string.IsNullOrWhiteSpace(rawToken));

        // 4. El token es válido en el token store para este usuario
        Assert.True(tokenStore.TryPeek(rawToken!, out var peekedUserId));
        Assert.Equal(TestVoterId, peekedUserId);

        // 5. Auditoría registrada
        Assert.Contains(audit.Actions, a => a == "PASSWORD_RECOVERY_REQUESTED");
    }

    [Fact]
    public async Task IssuePasswordResetTokenAsync_NonActiveOrMissingUser_DoesNothing()
    {
        await using var context = CreateContext();
        SeedVoter(context, TestVoterId, "ELIMINADO");

        var passwordStore = new PendingPasswordStore();
        var tokenStore = new PasswordResetTokenStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var audit = new FakeAuditService();
        var service = new CredentialService(context, passwordStore, audit, tokenStore);

        // Usuario inactivo: no debe encolar ni emitir token
        await service.IssuePasswordResetTokenAsync((int)TestVoterId, null);
        Assert.Empty(context.EmailQueues);

        // Usuario inexistente: no debe encolar ni emitir token
        await service.IssuePasswordResetTokenAsync(9999, null);
        Assert.Empty(context.EmailQueues);
    }

    [Fact]
    public async Task IssuePasswordResetTokenAsync_WhenSaveChangesFails_RevokesIssuedTokenAndRethrows()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        await using var context = new FailingWahlMiraiDbContext(options);
        SeedVoter(context, TestVoterId, "ACTIVO");

        var passwordStore = new PendingPasswordStore();
        var baseTokenStore = new PasswordResetTokenStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var spyTokenStore = new SpyingTokenStore(baseTokenStore);
        var audit = new FakeAuditService();
        var service = new CredentialService(context, passwordStore, audit, spyTokenStore);

        context.FailOnSave = true;

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.IssuePasswordResetTokenAsync((int)TestVoterId, null));

        // Debe haberse revocado el token al fallar la persistencia
        Assert.Equal(TestVoterId, spyTokenStore.LastRevokedUserId);
        Assert.NotNull(spyTokenStore.LastIssuedToken);
        Assert.False(baseTokenStore.TryPeek(spyTokenStore.LastIssuedToken, out _));
    }

    [Fact]
    public async Task IssueNewPasswordAsync_WithRecuperacionAcceso_ThrowsArgumentException()
    {
        await using var context = CreateContext();
        SeedVoter(context, TestVoterId, "ACTIVO");

        var passwordStore = new PendingPasswordStore();
        var tokenStore = new PasswordResetTokenStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var audit = new FakeAuditService();
        var service = new CredentialService(context, passwordStore, audit, tokenStore);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.IssueNewPasswordAsync((int)TestVoterId, EmailType.RECUPERACION_ACCESO, null));

        Assert.Contains("RECUPERACION_ACCESO ya no admite contraseñas generadas", ex.Message);
    }

    [Fact]
    public async Task IssueNewPasswordAsync_WithReasignacionAdmin_GeneratesPasswordAndEnqueues()
    {
        await using var context = CreateContext();
        SeedVoter(context, TestVoterId, "ACTIVO");

        var passwordStore = new PendingPasswordStore();
        var tokenStore = new PasswordResetTokenStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System);
        var audit = new FakeAuditService();
        var service = new CredentialService(context, passwordStore, audit, tokenStore);

        await service.IssueNewPasswordAsync((int)TestVoterId, EmailType.REASIGNACION_ADMIN, actorVoterId: 1);

        // 1. PasswordHash sí se actualiza en REASIGNACION_ADMIN (RF-M07-02)
        var voter = await context.Voters.FindAsync(TestVoterId);
        Assert.NotNull(voter);
        Assert.NotEqual(InitialHash, voter.PasswordHash);

        // 2. Se encola la fila REASIGNACION_ADMIN
        var queueItem = await context.EmailQueues.SingleAsync();
        Assert.Equal(EmailType.REASIGNACION_ADMIN.ToString(), queueItem.EmailType);

        // 3. La contraseña generada se almacena en el passwordStore
        Assert.True(passwordStore.TryGetPassword(queueItem.Id, out var generatedPassword));
        Assert.False(string.IsNullOrWhiteSpace(generatedPassword));

        // 4. Auditoría asentada
        Assert.Contains(audit.Actions, a => a == "PASSWORD_REASSIGNED");
    }

    private static WahlMiraiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new WahlMiraiDbContext(options);
    }

    private static void SeedVoter(WahlMiraiDbContext context, uint id, string status)
    {
        context.Voters.Add(new Voter
        {
            Id = id,
            RoleId = 3,
            DocumentHash = $"hash-{id}",
            EncryptedDocument = $"enc-{id}",
            FullName = $"Voter {id}",
            ContactEmail = $"voter{id}@example.com",
            PasswordHash = InitialHash,
            Status = status,
            RegisteredAt = DateTime.UtcNow
        });
        context.SaveChanges();
    }

    private sealed class FailingWahlMiraiDbContext(DbContextOptions<WahlMiraiDbContext> options)
        : WahlMiraiDbContext(options)
    {
        public bool FailOnSave { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailOnSave)
            {
                throw new DbUpdateException("Simulated database failure", new Exception());
            }
            return base.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed class SpyingTokenStore(IPasswordResetTokenStore inner) : IPasswordResetTokenStore
    {
        public string? LastIssuedToken { get; private set; }
        public uint? LastRevokedUserId { get; private set; }

        public string Issue(uint userId)
        {
            LastIssuedToken = inner.Issue(userId);
            return LastIssuedToken;
        }

        public bool TryPeek(string token, out uint userId) => inner.TryPeek(token, out userId);
        public bool TryConsume(string token, out uint userId) => inner.TryConsume(token, out userId);

        public void Revoke(uint userId)
        {
            LastRevokedUserId = userId;
            inner.Revoke(userId);
        }
    }
}
