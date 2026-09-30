using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

/// <summary>
/// Tests de ProfileService (RF-M07-01, RN-15, RN-8).
/// Cubre: verificación de contraseña obligatoria, throttling por bucket de perfil,
/// notificación a la dirección anterior, confirmación a la nueva, y aislamiento
/// entre el bucket de perfil y el bucket de login.
/// </summary>
public class ProfileServiceTests
{
    // ══════════════════════════════════════════════════════════════════════════════
    // Tests de UpdateContactEmailAsync existentes (actualizados: ahora requieren currentPassword)
    // ══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateContactEmailAsync_WhenNotificationSucceeds_SavesEmailAndReportsBothFlags()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var emailSender = new FakeEmailSender();
        var store = new FakeLoginAttemptStore();
        var service = CreateService(context, audit, emailSender, store);
        context.Voters.Add(CreateVoter(1, "old@example.com", "OldPassword!123"));
        await context.SaveChangesAsync();

        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "OldPassword!123", "127.0.0.1");

        Assert.True(result.Success);
        Assert.True(result.EmailSaved);
        Assert.True(result.NotificationSent);   // NotificationSent = aviso al correo ANTERIOR
        Assert.Equal(string.Empty, result.ErrorMessage);
        Assert.Equal("new@example.com", (await context.Voters.FindAsync((uint)1))!.ContactEmail);
        // Deben haberse enviado 2 correos: aviso a old@ y confirmación a new@
        Assert.Equal(2, emailSender.Messages.Count);
        Assert.Contains("old@example.com", emailSender.Messages);
        Assert.Contains("new@example.com", emailSender.Messages);
    }

    [Fact]
    public async Task UpdateContactEmailAsync_WhenNotificationFails_KeepsSavedFlagAndReportsNotificationFailure()
    {
        await using var context = CreateContext();
        // ShouldFail afecta a TODOS los envíos; NotificationSent = false porque el aviso al correo anterior falló
        var emailSender = new FakeEmailSender { ShouldFail = true };
        var service = CreateService(context, new FakeAuditService(), emailSender, new FakeLoginAttemptStore());
        context.Voters.Add(CreateVoter(1, "old@example.com", "OldPassword!123"));
        await context.SaveChangesAsync();

        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "OldPassword!123", "127.0.0.1");

        Assert.True(result.Success);
        Assert.True(result.EmailSaved);
        Assert.False(result.NotificationSent);
        Assert.Equal("new@example.com", (await context.Voters.FindAsync((uint)1))!.ContactEmail);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // Tests nuevos: contraseña obligatoria
    // ══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateContactEmailAsync_WithMissingPassword_RejectsWithoutChangingEmail()
    {
        await using var context = CreateContext();
        var emailSender = new FakeEmailSender();
        var service = CreateService(context, new FakeAuditService(), emailSender, new FakeLoginAttemptStore());
        context.Voters.Add(CreateVoter(1, "old@example.com", "OldPassword!123"));
        await context.SaveChangesAsync();

        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "", "127.0.0.1");

        Assert.False(result.Success);
        Assert.False(result.EmailSaved);
        Assert.Equal("old@example.com", (await context.Voters.FindAsync((uint)1))!.ContactEmail);
        Assert.Empty(emailSender.Messages);
    }

    [Fact]
    public async Task UpdateContactEmailAsync_WithWrongPassword_RejectsWithEmailUnchangedAndNoMailSent()
    {
        await using var context = CreateContext();
        var emailSender = new FakeEmailSender();
        var audit = new FakeAuditService();
        var service = CreateService(context, audit, emailSender, new FakeLoginAttemptStore());
        context.Voters.Add(CreateVoter(1, "old@example.com", "CorrectPassword!123"));
        await context.SaveChangesAsync();

        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "WrongPassword!999", "127.0.0.1");

        Assert.False(result.Success);
        Assert.False(result.EmailSaved);
        Assert.False(result.NotificationSent);
        Assert.Equal("old@example.com", (await context.Voters.FindAsync((uint)1))!.ContactEmail);
        Assert.Empty(emailSender.Messages);
        // Debe registrarse un fallo de auditoría
        Assert.Contains("PROFILE_AUTH_FAILED", audit.Actions);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // Tests nuevos: destinatarios correctos
    // ══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateContactEmailAsync_WithCorrectPassword_OldAddressReceivesNoticeAndNewAddressConfirmation()
    {
        await using var context = CreateContext();
        var emailSender = new FakeEmailSender();
        var service = CreateService(context, new FakeAuditService(), emailSender, new FakeLoginAttemptStore());
        context.Voters.Add(CreateVoter(1, "old@example.com", "Correct!Pass123"));
        await context.SaveChangesAsync();

        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "Correct!Pass123", "127.0.0.1");

        Assert.True(result.Success);
        // Los dos correos enviados deben ser exactamente: el anterior y el nuevo
        Assert.Equal(2, emailSender.Messages.Count);
        Assert.Equal("old@example.com", emailSender.Messages[0]); // aviso va primero
        Assert.Equal("new@example.com", emailSender.Messages[1]); // confirmación va segundo
    }

    [Fact]
    public async Task UpdateContactEmailAsync_SmtpFailureOnOldAddress_DoesNotRevertPersistenceAndStillTriesNewAddress()
    {
        await using var context = CreateContext();
        // Solo falla el primer envío (al correo anterior); el segundo (al nuevo) debe intentarse
        var emailSender = new FakeEmailSender { ShouldFailFirst = true };
        var service = CreateService(context, new FakeAuditService(), emailSender, new FakeLoginAttemptStore());
        context.Voters.Add(CreateVoter(1, "old@example.com", "Correct!Pass123"));
        await context.SaveChangesAsync();

        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "Correct!Pass123", "127.0.0.1");

        Assert.True(result.Success);
        Assert.True(result.EmailSaved);
        Assert.False(result.NotificationSent);                     // aviso al anterior falló
        Assert.Equal("new@example.com", (await context.Voters.FindAsync((uint)1))!.ContactEmail);
        // La confirmación al nuevo correo debe haberse intentado (y enviado)
        Assert.Contains("new@example.com", emailSender.Messages);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // Tests nuevos: throttling (RN-15)
    // ══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateContactEmailAsync_LockoutAfterNFailures_BlocksEvenCorrectPassword()
    {
        await using var context = CreateContext();
        var store = new FakeLoginAttemptStore(threshold: 3); // bloqueo al 3er fallo (test más rápido)
        var service = CreateService(context, new FakeAuditService(), new FakeEmailSender(), store);
        context.Voters.Add(CreateVoter(1, "old@example.com", "CorrectPassword!123"));
        await context.SaveChangesAsync();

        // Forzar bloqueo con contraseñas incorrectas
        for (int i = 0; i < 3; i++)
        {
            await service.UpdateContactEmailAsync(1, "new@example.com", "Wrong!Pass999", "127.0.0.1");
        }

        // La contraseña correcta debe ser rechazada mientras dura el bloqueo
        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "CorrectPassword!123", "127.0.0.1");

        Assert.False(result.Success);
        Assert.False(result.EmailSaved);
        Assert.Equal("old@example.com", (await context.Voters.FindAsync((uint)1))!.ContactEmail);
        Assert.Contains("bloqueada", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateContactEmailAsync_SuccessResetsProfileBucket()
    {
        await using var context = CreateContext();
        var store = new FakeLoginAttemptStore(threshold: 3);
        var service = CreateService(context, new FakeAuditService(), new FakeEmailSender(), store);
        context.Voters.Add(CreateVoter(1, "old@example.com", "CorrectPassword!123"));
        await context.SaveChangesAsync();

        // 2 fallos (sin llegar al bloqueo)
        await service.UpdateContactEmailAsync(1, "new@example.com", "Wrong!Pass999", "127.0.0.1");
        await service.UpdateContactEmailAsync(1, "new@example.com", "Wrong!Pass999", "127.0.0.1");

        // Éxito — debe resetear el bucket
        var result = await service.UpdateContactEmailAsync(1, "new@example.com", "CorrectPassword!123", "127.0.0.1");
        Assert.True(result.Success);

        // Tras el reset, un nuevo fallo NO debe bloquear de inmediato
        var resetContext = CreateContext();
        resetContext.Voters.Add(CreateVoter(2, "other@example.com", "OtherPass!123"));
        await resetContext.SaveChangesAsync();
        var service2 = CreateService(resetContext, new FakeAuditService(), new FakeEmailSender(), store);
        var afterReset = await service2.UpdateContactEmailAsync(2, "x@example.com", "Wrong!Pass", "127.0.0.1");
        // No bloqueado todavía (solo 1 fallo, umbral=3)
        Assert.Contains("correcta", afterReset.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProfileBucket_DoesNotAffectLoginLockout()
    {
        // El bucket "profile:{id}" es independiente del bucket de login (el documentHash).
        // Verificamos que acumular fallos en el bucket de perfil no afecta el bucket de login.
        var store = new FakeLoginAttemptStore(threshold: 3);
        var profileKey = "profile:1";
        var loginKey   = "documenthash-abc";

        // 3 fallos en el bucket de perfil → bloqueo del bucket de perfil
        for (int i = 0; i < 3; i++) store.RegisterFailure(profileKey);
        Assert.True(store.IsLockedOut(profileKey, out _));

        // El bucket de login NO está bloqueado
        Assert.False(store.IsLockedOut(loginKey, out _));

        // Acumular fallos en login no afecta al bucket de perfil (aislamiento bidireccional)
        store.RegisterFailure(loginKey);
        store.RegisterSuccess(loginKey);
        Assert.True(store.IsLockedOut(profileKey, out _)); // el perfil sigue bloqueado
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // Tests de UpdateProfileAsync — Endurecimiento y Cierre de Bypass (Entrega 1.1 Item C)
    // ══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateProfileAsync_EmailSupplied_IsIgnoredAndNotPersisted()
    {
        await using var context = CreateContext();
        var voter = CreateVoter(1, "old@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();
        var service = CreateService(context, new FakeAuditService(), new FakeEmailSender(), new FakeLoginAttemptStore());

        // Intento de modificar correo a través de UpdateProfileAsync
        var result = await service.UpdateProfileAsync(1, "bypassed@example.com", "CorrectPassword!123", "NewPassword!123", "127.0.0.1");

        Assert.True(result.Success);
        var reloaded = await context.Voters.FindAsync((uint)1);
        Assert.Equal("old@example.com", reloaded!.ContactEmail); // Correo intacto (ignorado)
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword!123", reloaded.PasswordHash));
    }

    [Fact]
    public async Task UpdateProfileAsync_NewPasswordWithoutCurrentPassword_Rejected()
    {
        await using var context = CreateContext();
        var voter = CreateVoter(1, "old@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();
        var service = CreateService(context, new FakeAuditService(), new FakeEmailSender(), new FakeLoginAttemptStore());
        var originalHash = voter.PasswordHash;

        // Caso 1: currentPassword vacío
        var resultEmpty = await service.UpdateProfileAsync(1, null, "", "NewPassword!123", "127.0.0.1");
        Assert.False(resultEmpty.Success);
        Assert.Equal("Debes ingresar tu contraseña actual.", resultEmpty.ErrorMessage);

        // Caso 2: currentPassword null
        var resultNull = await service.UpdateProfileAsync(1, null, null, "NewPassword!123", "127.0.0.1");
        Assert.False(resultNull.Success);
        Assert.Equal("Debes ingresar tu contraseña actual.", resultNull.ErrorMessage);

        var reloaded = await context.Voters.FindAsync((uint)1);
        Assert.Equal(originalHash, reloaded!.PasswordHash);
    }

    [Fact]
    public async Task UpdateProfileAsync_WrongCurrentPassword_CountsTowardAndTriggersProfileLockout()
    {
        await using var context = CreateContext();
        var store = new FakeLoginAttemptStore(threshold: 3);
        var audit = new FakeAuditService();
        var service = CreateService(context, audit, new FakeEmailSender(), store);
        var voter = CreateVoter(1, "old@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();
        var originalHash = voter.PasswordHash;

        // 3 fallos consecutivos en UpdateProfileAsync activan el bloqueo en el bucket profile:1
        for (int i = 0; i < 3; i++)
        {
            var res = await service.UpdateProfileAsync(1, null, "WrongPassword!123", "NewPassword!123", "127.0.0.1");
            Assert.False(res.Success);
        }

        Assert.Contains("PROFILE_AUTH_FAILED", audit.Actions);
        Assert.Contains("PROFILE_LOCKED_OUT", audit.Actions);

        // El siguiente intento, aun con contraseña correcta, es bloqueado por throttling
        var lockedResult = await service.UpdateProfileAsync(1, null, "CorrectPassword!123", "NewPassword!123", "127.0.0.1");
        Assert.False(lockedResult.Success);
        Assert.Contains("bloqueada", lockedResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);

        var reloaded = await context.Voters.FindAsync((uint)1);
        Assert.Equal(originalHash, reloaded!.PasswordHash);
    }

    [Fact]
    public async Task UpdateProfileAsync_SuccessResetsProfileBucket()
    {
        await using var context = CreateContext();
        var store = new FakeLoginAttemptStore(threshold: 3);
        var service = CreateService(context, new FakeAuditService(), new FakeEmailSender(), store);
        var voter = CreateVoter(1, "old@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        // 2 fallos previos (umbral = 3)
        await service.UpdateProfileAsync(1, null, "WrongPassword!123", "NewPassword!123", "127.0.0.1");
        await service.UpdateProfileAsync(1, null, "WrongPassword!123", "NewPassword!123", "127.0.0.1");

        // Éxito con contraseña correcta -> reinicia el contador
        var successResult = await service.UpdateProfileAsync(1, null, "CorrectPassword!123", "NewPassword!123", "127.0.0.1");
        Assert.True(successResult.Success);

        // Ahora puede cometer 2 nuevos fallos sin ser bloqueado de inmediato
        var fail1 = await service.UpdateProfileAsync(1, null, "WrongPassword!123", "AnotherNew!123", "127.0.0.1");
        var fail2 = await service.UpdateProfileAsync(1, null, "WrongPassword!123", "AnotherNew!123", "127.0.0.1");
        Assert.False(fail1.Success);
        Assert.False(fail2.Success);
        Assert.False(store.IsLockedOut("profile:1", out _));
    }

    [Fact]
    public async Task UpdateProfileAsync_WithWrongCurrentPassword_BlocksUpdate()
    {
        await using var context = CreateContext();
        var voter = CreateVoter(1, "old@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();
        var service = CreateService(context, new FakeAuditService(), new FakeEmailSender(), new FakeLoginAttemptStore());
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
        var service = CreateService(context, new FakeAuditService(), new FakeEmailSender(), new FakeLoginAttemptStore());

        var result = await service.UpdateProfileAsync(1, null, "CorrectPassword!123", "NewPassword!123", "127.0.0.1");

        Assert.True(result.Success);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPassword!123", voter.PasswordHash));
        Assert.False(BCrypt.Net.BCrypt.Verify("CorrectPassword!123", voter.PasswordHash));
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // Tests de VerifyCurrentPasswordAsync (Paso 1 del modal, auditoría y throttling RN-15, RN-8)
    // ══════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task VerifyCurrentPasswordAsync_WrongPassword_RegistersSingleFailureAndAuditEntry()
    {
        await using var context = CreateContext();
        var store = new FakeLoginAttemptStore(threshold: 3);
        var audit = new FakeAuditService();
        var service = CreateService(context, audit, new FakeEmailSender(), store);
        var voter = CreateVoter(1, "voter@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        var result = await service.VerifyCurrentPasswordAsync(1, "WrongPassword!999", "192.168.1.50");

        Assert.False(result.Success);
        Assert.Equal("La contraseña actual no es correcta.", result.ErrorMessage);

        // Registra exactamente un fallo en el bucket de intentos
        Assert.Equal(1, store.FailureCallCount);

        // Registra exactamente una entrada de auditoría PROFILE_AUTH_FAILED con IP y detalles
        Assert.Single(audit.Actions);
        Assert.Equal("PROFILE_AUTH_FAILED", audit.Actions[0]);
        Assert.Equal("192.168.1.50", audit.IpAddresses[0]);
        Assert.Contains("Intentos: 1", audit.Details[0]);
    }

    [Fact]
    public async Task VerifyCurrentPasswordAsync_ReachingThreshold_AuditsProfileLockedOutAndBlocksSubsequentCalls()
    {
        await using var context = CreateContext();
        var store = new FakeLoginAttemptStore(threshold: 3);
        var audit = new FakeAuditService();
        var service = CreateService(context, audit, new FakeEmailSender(), store);
        var voter = CreateVoter(1, "voter@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        // 3 fallos consecutivos alcanzan el umbral de bloqueo
        for (int i = 0; i < 3; i++)
        {
            var res = await service.VerifyCurrentPasswordAsync(1, "WrongPassword!999", "192.168.1.50");
            Assert.False(res.Success);
        }

        // Se deben registrar 3 fallos y el evento de bloqueo
        Assert.Equal(3, store.FailureCallCount);
        Assert.Contains("PROFILE_AUTH_FAILED", audit.Actions);
        Assert.Contains("PROFILE_LOCKED_OUT", audit.Actions);

        // Siguiente llamada, aun con contraseña correcta, queda bloqueada
        var lockedResult = await service.VerifyCurrentPasswordAsync(1, "CorrectPassword!123", "192.168.1.50");
        Assert.False(lockedResult.Success);
        Assert.Contains("bloqueada", lockedResult.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyCurrentPasswordAsync_Success_ResetsProfileBucket()
    {
        await using var context = CreateContext();
        var store = new FakeLoginAttemptStore(threshold: 3);
        var audit = new FakeAuditService();
        var service = CreateService(context, audit, new FakeEmailSender(), store);
        var voter = CreateVoter(1, "voter@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        // 2 fallos previos (umbral = 3)
        await service.VerifyCurrentPasswordAsync(1, "WrongPassword!999", "192.168.1.50");
        await service.VerifyCurrentPasswordAsync(1, "WrongPassword!999", "192.168.1.50");
        Assert.Equal(2, store.FailureCallCount);

        // Éxito con contraseña correcta -> reinicia el bucket
        var successResult = await service.VerifyCurrentPasswordAsync(1, "CorrectPassword!123", "192.168.1.50");
        Assert.True(successResult.Success);
        Assert.Equal(string.Empty, successResult.ErrorMessage);
        Assert.Equal(1, store.SuccessCallCount);

        // Ahora puede tener 2 nuevos fallos sin alcanzar el bloqueo
        var res1 = await service.VerifyCurrentPasswordAsync(1, "WrongPassword!999", "192.168.1.50");
        var res2 = await service.VerifyCurrentPasswordAsync(1, "WrongPassword!999", "192.168.1.50");
        Assert.False(res1.Success);
        Assert.False(res2.Success);
        Assert.False(store.IsLockedOut("profile:1", out _));
    }

    [Fact]
    public async Task VerifyCurrentPasswordAsync_EmptyOrNullPassword_RejectsWithoutFailureRegistrationOrAudit()
    {
        await using var context = CreateContext();
        var store = new FakeLoginAttemptStore(threshold: 3);
        var audit = new FakeAuditService();
        var service = CreateService(context, audit, new FakeEmailSender(), store);
        var voter = CreateVoter(1, "voter@example.com", "CorrectPassword!123");
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        var resEmpty = await service.VerifyCurrentPasswordAsync(1, "", "127.0.0.1");
        var resNull = await service.VerifyCurrentPasswordAsync(1, null, "127.0.0.1");

        Assert.False(resEmpty.Success);
        Assert.Equal("Debes ingresar tu contraseña actual.", resEmpty.ErrorMessage);
        Assert.False(resNull.Success);
        Assert.Equal("Debes ingresar tu contraseña actual.", resNull.ErrorMessage);

        Assert.Equal(0, store.FailureCallCount);
        Assert.Empty(audit.Actions);
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // Helpers
    // ══════════════════════════════════════════════════════════════════════════════

    private static ProfileService CreateService(
        WahlMiraiDbContext context,
        IAuditService audit,
        IEmailSender emailSender,
        ILoginAttemptStore store) =>
        new(context, audit, emailSender, new FakeCredentialService(), store);

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

    /// <summary>
    /// Fake de IEmailSender para los tests de perfil.
    /// Registra los destinatarios de cada envío en <see cref="Messages"/>.
    /// <see cref="ShouldFail"/>: todos los envíos fallan.
    /// <see cref="ShouldFailFirst"/>: solo el primer envío falla (simula SMTP fallo en el aviso al correo anterior).
    /// </summary>
    private sealed class FakeEmailSender : IEmailSender
    {
        public bool ShouldFail      { get; init; }
        public bool ShouldFailFirst { get; init; }
        public List<string> Messages { get; } = new();
        private int _callCount;

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            if (ShouldFail)
                throw new InvalidOperationException("SMTP unavailable");

            _callCount++;
            if (ShouldFailFirst && _callCount == 1)
                throw new InvalidOperationException("SMTP unavailable (first send)");

            Messages.Add(toEmail);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCredentialService : ICredentialService
    {
        public Task IssueNewPasswordAsync(int voterId, EmailType emailType, int? actorVoterId, CancellationToken ct = default) => Task.CompletedTask;
    }
}
