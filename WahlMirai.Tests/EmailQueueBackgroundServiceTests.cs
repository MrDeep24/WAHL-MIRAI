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

    /// <summary>El correo de token contiene el enlace con el token escapado y la URL base (RF-M01-02, RN-9).</summary>
    [Fact]
    public async Task ProcessQueueAsync_RecoveryEmail_ContainsEscapedTokenAndBaseUrl()
    {
        await using var testContext = await CreateTestContextAsync(_ => false);

        await ProcessOneAsync(testContext.Worker);

        var sent = await testContext.ReadQueueAsync();
        Assert.Equal("ENVIADO", sent.Status);
        Assert.Single(testContext.EmailSender.SentSubjects);
        Assert.Equal("Restablecer tu contraseña - Wahl Mirai", testContext.EmailSender.SentSubjects[0]);
        var body = testContext.EmailSender.SentBodies[0];
        Assert.Contains("https://localhost/RecuperacionAcceso/Restablecer?token=", body);
        Assert.Contains(Uri.EscapeDataString(Password), body);
        Assert.DoesNotContain("Aquí tienes tu nueva contraseña", body);
    }

    /// <summary>BaseUrl no configurada o inválida marca FALLIDO de inmediato y retira el secreto sin reintentos (RN-9).</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("http://external-site.com")]
    [InlineData("not-a-valid-url")]
    public async Task ProcessQueueAsync_InvalidOrEmptyBaseUrl_MarksFailedTerminallyAndRemovesSecret(string invalidUrl)
    {
        await using var testContext = await CreateTestContextAsync(_ => false, seedPassword: true, baseUrl: invalidUrl);

        await ProcessOneAsync(testContext.Worker);

        var failed = await testContext.ReadQueueAsync();
        Assert.Equal("FALLIDO", failed.Status);
        Assert.Contains("EmailSettings:BaseUrl no está configurada o es inválida", failed.ErrorMessage);
        Assert.False(testContext.PasswordStore.TryGetPassword(QueueId, out _));
        Assert.Empty(testContext.EmailSender.SecretAvailableAtSend);
    }

    /// <summary>Valida que el esquema HTTP solo sea admitido en loopback local (RN-9).</summary>
    [Theory]
    [InlineData("https://localhost:7062", true)]
    [InlineData("https://midominio.edu.co", true)]
    [InlineData("http://localhost:5166", true)]
    [InlineData("http://127.0.0.1:8080", true)]
    [InlineData("http://[::1]:8080", true)]
    [InlineData("http://dominio-inseguro.com", false)]
    [InlineData("ftp://localhost", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsValidBaseUrl_ValidationRules_EnforcesHttpsOrLocalLoopback(string? url, bool expectedValid)
    {
        var isValid = EmailQueueBackgroundService.IsValidBaseUrl(url, out var uri);
        Assert.Equal(expectedValid, isValid);
        if (expectedValid)
        {
            Assert.NotNull(uri);
        }
    }

    /// <summary>REASIGNACION_ADMIN conserva el asunto y cuerpo de entrega de contraseña en claro (RN-9).</summary>
    [Fact]
    public async Task ProcessQueueAsync_ReasignacionAdmin_SendsPasswordEmailUnchanged()
    {
        await using var testContext = await CreateTestContextAsync(_ => false, seedPassword: true, baseUrl: "https://localhost", type: EmailType.REASIGNACION_ADMIN);

        await ProcessOneAsync(testContext.Worker);

        var sent = await testContext.ReadQueueAsync();
        Assert.Equal("ENVIADO", sent.Status);
        Assert.Single(testContext.EmailSender.SentSubjects);
        Assert.Equal("Credenciales de Acceso - Wahl Mirai", testContext.EmailSender.SentSubjects[0]);
        var body = testContext.EmailSender.SentBodies[0];
        Assert.Contains("Aquí tienes tu nueva contraseña para ingresar a Wahl Mirai", body);
        Assert.Contains(Password, body);
    }

    private static async Task ProcessOneAsync(EmailQueueBackgroundService worker)
    {
        var method = typeof(EmailQueueBackgroundService).GetMethod("ProcessQueueAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task)method.Invoke(worker, new object[] { CancellationToken.None })!;
        await task;
    }

    private static async Task<TestContext> CreateTestContextAsync(
        Func<int, bool> shouldFail,
        bool seedPassword = true,
        string baseUrl = "https://localhost",
        EmailType type = EmailType.RECUPERACION_ACCESO)
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
            var emailType = type.ToString();
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
            Options.Create(new EmailSettings { RateLimitPerMinute = 20, BaseUrl = baseUrl }),
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
        public List<string> SentSubjects { get; } = new();
        public List<string> SentBodies { get; } = new();
        private int _attempt;

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            _attempt++;
            SecretAvailableAtSend.Add(passwordStore.TryGetPassword(QueueId, out _));
            SentSubjects.Add(subject);
            SentBodies.Add(htmlBody);
            if (shouldFail(_attempt))
            {
                throw new InvalidOperationException("SMTP unavailable");
            }

            return Task.CompletedTask;
        }
    }
}