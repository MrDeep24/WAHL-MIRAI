// ============================================================
// Archivo:       EmailQueueBackgroundServiceTests.cs
// Módulo:        Servicio compartido de notificaciones
// Desarrollador: Kevin
// Descripción:   Verifica reintentos y control de tasa del worker.
// Requerimientos: RN-9
// ============================================================
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

/// <summary>Pruebas del ciclo de envío y reintentos de la cola de correo (RN-9).</summary>
public sealed class EmailQueueBackgroundServiceTests
{
    private const ulong QueueId = 12;
    private const uint VoterId = 3;
    private const string Password = "Temporary!123";
    private const string MissingPasswordMessage = "La contraseña en memoria se perdió (reinicio del servicio). El usuario deberá solicitarla nuevamente.";

    /// <summary>Conserva el secreto tras un fallo SMTP y lo elimina después del envío exitoso (RN-9).</summary>
    [Fact]
    public async Task ProcessQueueAsync_TransientFailureThenSuccess_RetainsThenRemovesSecretAndClearsError()
    {
        await using var testContext = await CreateTestContextAsync(attempt => attempt == 1);

        await ProcessOneAsync(testContext.Worker);

        var pending = await testContext.ReadQueueAsync();
        Assert.Equal("PENDIENTE", pending.Status);
        Assert.Equal("SMTP unavailable", pending.ErrorMessage);
        Assert.True(testContext.PasswordStore.TryGetPassword(QueueId, out _));

        await ProcessOneAsync(testContext.Worker);

        var sent = await testContext.ReadQueueAsync();
        Assert.Equal("ENVIADO", sent.Status);
        Assert.Null(sent.ErrorMessage);
        Assert.NotNull(sent.SentAt);
        Assert.Equal(new[] { true, true }, testContext.EmailSender.SecretAvailableAtSend);
        Assert.False(testContext.PasswordStore.TryGetPassword(QueueId, out _));
    }

    /// <summary>Agota tres fallos SMTP, marca FALLIDO y elimina entonces el secreto (RN-9).</summary>
    [Fact]
    public async Task ProcessQueueAsync_AllAttemptsFail_MarksFailedAndRemovesSecretAtLimit()
    {
        await using var testContext = await CreateTestContextAsync(_ => true);

        await ProcessOneAsync(testContext.Worker);
        Assert.True(testContext.PasswordStore.TryGetPassword(QueueId, out _));
        await ProcessOneAsync(testContext.Worker);
        Assert.True(testContext.PasswordStore.TryGetPassword(QueueId, out _));
        await ProcessOneAsync(testContext.Worker);

        var failed = await testContext.ReadQueueAsync();
        Assert.Equal("FALLIDO", failed.Status);
        Assert.Equal((byte)3, failed.Attempts);
        Assert.Equal("SMTP unavailable", failed.ErrorMessage);
        Assert.Equal(new[] { true, true, true }, testContext.EmailSender.SecretAvailableAtSend);
        Assert.False(testContext.PasswordStore.TryGetPassword(QueueId, out _));
    }

    /// <summary>Conserva el fallo terminal y el mensaje existente si el secreto ya no está disponible (RN-9).</summary>
    [Fact]
    public async Task ProcessQueueAsync_MissingSecret_MarksFailedWithExistingMessage()
    {
        await using var testContext = await CreateTestContextAsync(_ => false, seedPassword: false);

        await ProcessOneAsync(testContext.Worker);

        var failed = await testContext.ReadQueueAsync();
        Assert.Equal("FALLIDO", failed.Status);
        Assert.Equal((byte)1, failed.Attempts);
        Assert.Equal(MissingPasswordMessage, failed.ErrorMessage);
        Assert.Empty(testContext.EmailSender.SecretAvailableAtSend);
    }

    /// <summary>Acota los valores extremos a una demora positiva y preserva 20 correos/minuto (RN-9).</summary>
    [Theory]
    [InlineData(0, 60000)]
    [InlineData(-5, 60000)]
    [InlineData(60001, 1000)]
    [InlineData(20, 3000)]
    public void GetDelayMs_ExtremeRates_AlwaysReturnsPositiveDelay(int configuredRate, int expectedDelay)
    {
        var method = typeof(EmailQueueBackgroundService).GetMethod("GetDelayMs", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var delay = (int)method.Invoke(null, new object[] { configuredRate })!;

        Assert.Equal(expectedDelay, delay);
        Assert.True(delay > 0);
    }

    private static async Task ProcessOneAsync(EmailQueueBackgroundService worker)
    {
        var method = typeof(EmailQueueBackgroundService).GetMethod("ProcessQueueAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task)method.Invoke(worker, new object[] { CancellationToken.None })!;
        await task;
    }

    private static async Task<TestContext> CreateTestContextAsync(Func<int, bool> shouldFail, bool seedPassword = true)
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var passwordStore = new PendingPasswordStore();
        if (seedPassword)
        {
            passwordStore.StorePassword(QueueId, Password);
        }

        var emailSender = new TestEmailSender(passwordStore, shouldFail);
        await using (var context = new TestWahlMiraiDbContext(options))
        {
            var voter = new Voter
            {
                Id = VoterId,
                RoleId = 3,
                DocumentHash = "test-document-hash",
                EncryptedDocument = "encrypted-document",
                FullName = "Test Voter",
                ContactEmail = "voter@example.com",
                PasswordHash = "test-password-hash",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            };
            var emailType = EmailType.RECUPERACION_ACCESO.ToString();
            var createdAt = DateTime.UtcNow;
            context.Voters.Add(voter);
            context.EmailQueues.Add(new EmailQueue
            {
                Id = QueueId,
                VoterId = VoterId,
                EmailType = emailType,
                Status = "PENDIENTE",
                Attempts = 0,
                CreatedAt = createdAt,
                Voter = voter
            });
            context.VwPendingEmailQueues.Add(new VwPendingEmailQueue
            {
                Id = QueueId,
                VoterId = VoterId,
                ContactEmail = voter.ContactEmail,
                FullName = voter.FullName,
                EmailType = emailType,
                Attempts = 0,
                CreatedAt = createdAt
            });
            await context.SaveChangesAsync();
        }

        var services = new ServiceCollection();
        services.AddSingleton<IPendingPasswordStore>(passwordStore);
        services.AddSingleton<IEmailSender>(emailSender);
        services.AddScoped<WahlMiraiDbContext>(_ => new TestWahlMiraiDbContext(options));
        var serviceProvider = services.BuildServiceProvider();
        var worker = new EmailQueueBackgroundService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Options.Create(new EmailSettings { RateLimitPerMinute = 20 }),
            NullLogger<EmailQueueBackgroundService>.Instance);

        return new TestContext(serviceProvider, options, worker, passwordStore, emailSender);
    }

    private sealed class TestContext(
        ServiceProvider serviceProvider,
        DbContextOptions<WahlMiraiDbContext> options,
        EmailQueueBackgroundService worker,
        PendingPasswordStore passwordStore,
        TestEmailSender emailSender) : IAsyncDisposable
    {
        public EmailQueueBackgroundService Worker { get; } = worker;
        public PendingPasswordStore PasswordStore { get; } = passwordStore;
        public TestEmailSender EmailSender { get; } = emailSender;

        public async Task<EmailQueue> ReadQueueAsync()
        {
            await using var context = new TestWahlMiraiDbContext(options);
            return await context.EmailQueues.SingleAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Worker.Dispose();
            await serviceProvider.DisposeAsync();
        }
    }

    private sealed class TestWahlMiraiDbContext(DbContextOptions<WahlMiraiDbContext> options)
        : WahlMiraiDbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<VwPendingEmailQueue>().HasKey(queue => queue.Id);
            modelBuilder.Entity<VwPendingEmailQueue>().ToTable("TestVwPendingEmailQueue");
        }
    }

    private sealed class TestEmailSender(PendingPasswordStore passwordStore, Func<int, bool> shouldFail) : IEmailSender
    {
        public List<bool> SecretAvailableAtSend { get; } = new();
        private int _attempt;

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            _attempt++;
            SecretAvailableAtSend.Add(passwordStore.TryGetPassword(QueueId, out _));
            if (shouldFail(_attempt))
            {
                throw new InvalidOperationException("SMTP unavailable");
            }

            return Task.CompletedTask;
        }
    }
}