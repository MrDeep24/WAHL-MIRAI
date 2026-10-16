// ============================================================
// Archivo:       ProfileService.cs
// Módulo:        M07 — Perfil de Usuario y Autogestión de Credenciales
// Desarrollador: Kevin
// Descripción:   Implementa la lectura y actualización de datos editables del perfil,
//                registra los cambios y delega la emisión de credenciales de reinicio.
//                UpdateContactEmailAsync exige la contraseña actual (RF-M07-01, RN-15),
//                usa el bucket "profile:{id}" para throttling independiente del login,
//                y notifica tanto a la dirección anterior como a la nueva (RN-8).
// Requerimientos: RF-M07-01; soporte al flujo de recuperación RF-M01-02; RN-15; RN-8.
// ============================================================

using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

/// <summary>
/// Implementa las operaciones de actualización de perfil y autogestión de credenciales.
/// </summary>
public class ProfileService : IProfileService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IEmailSender _emailSender;
    private readonly ICredentialService _credentialService;
    private readonly ILoginAttemptStore _loginAttemptStore;

    /// <summary>
    /// Inicializa el servicio con dependencias de persistencia, auditoría, correo,
    /// credenciales y control de intentos (RN-15).
    /// </summary>
    /// <param name="context">Contexto de datos de usuarios.</param>
    /// <param name="auditService">Servicio para registrar los cambios de perfil.</param>
    /// <param name="emailSender">Servicio para enviar notificaciones de perfil.</param>
    /// <param name="credentialService">Servicio para emitir credenciales de reinicio.</param>
    /// <param name="loginAttemptStore">Almacén de intentos fallidos para throttling de perfil (RN-15).</param>
    public ProfileService(
        WahlMiraiDbContext context,
        IAuditService auditService,
        IEmailSender emailSender,
        ICredentialService credentialService,
        ILoginAttemptStore loginAttemptStore)
    {
        _context = context;
        _auditService = auditService;
        _emailSender = emailSender;
        _credentialService = credentialService;
        _loginAttemptStore = loginAttemptStore;
    }

    /// <summary>
    /// Clave del bucket de intentos para operaciones de perfil, segregada del bucket de login
    /// para que el throttling de perfil no afecte el contador de inicio de sesión (RN-15).
    /// </summary>
    private static string ProfileBucketKey(int voterId) => $"profile:{voterId}";

    /// <summary>
    /// Actualiza la contraseña del usuario, previa verificación obligatoria de la contraseña actual (RN-15).
    /// NOTA: El correo de contacto NO se modifica aquí; las actualizaciones de correo se realizan
    /// exclusivamente mediante <see cref="UpdateContactEmailAsync"/> (RF-M07-01).
    /// </summary>
    /// <param name="voterId">Identificador del usuario.</param>
    /// <param name="newContactEmail">Ignorado; los cambios de correo solo se permiten vía <see cref="UpdateContactEmailAsync"/>.</param>
    /// <param name="currentPassword">Contraseña actual obligatoria cuando se suministra nueva contraseña.</param>
    /// <param name="newPassword">Nueva contraseña, o <see langword="null"/> para no modificarla.</param>
    /// <param name="ipAddress">Dirección IP registrada junto con el cambio.</param>
    /// <returns>El resultado de la operación y un mensaje de error si falla.</returns>
    public async Task<(bool Success, string ErrorMessage)> UpdateProfileAsync(int voterId, string? newContactEmail, string? currentPassword, string? newPassword, string ipAddress)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null) return (false, "Usuario no encontrado.");

        bool hasChanges = false;

        // Check and update password
        if (!string.IsNullOrEmpty(newPassword))
        {
            var bucket = ProfileBucketKey(voterId);
            if (_loginAttemptStore.IsLockedOut(bucket, out var remaining))
            {
                var mins = (int)Math.Ceiling(remaining.TotalMinutes);
                return (false, $"Demasiados intentos fallidos. Por seguridad, la operación está bloqueada temporalmente. Intenta de nuevo en {mins} minuto(s).");
            }

            if (string.IsNullOrEmpty(currentPassword))
            {
                return (false, "Debes ingresar tu contraseña actual.");
            }

            if (!BCrypt.Net.BCrypt.Verify(currentPassword, voter.PasswordHash))
            {
                var attemptResult = _loginAttemptStore.RegisterFailure(bucket);
                await _auditService.LogAsync(
                    "PROFILE_AUTH_FAILED", voterId, "users", voterId,
                    "PasswordHash", null, null,
                    $"Contraseña incorrecta al intentar cambiar contraseña. Intentos: {attemptResult.FailedCount}",
                    ipAddress);

                if (attemptResult.JustLockedOut)
                {
                    var mins = (int)Math.Ceiling(attemptResult.LockedUntilUtc!.Value.Subtract(DateTimeOffset.UtcNow).TotalMinutes);
                    await _auditService.LogAsync(
                        "PROFILE_LOCKED_OUT", voterId, "users", voterId,
                        "PasswordHash", null, null,
                        $"Cuenta bloqueada temporalmente para cambio de contraseña. Duración: {mins} minuto(s).",
                        ipAddress);
                    return (false,
                        $"Demasiados intentos fallidos. Por seguridad, la operación está bloqueada temporalmente. Intenta de nuevo en {mins} minuto(s).");
                }

                return (false, "La contraseña actual no coincide.");
            }

            _loginAttemptStore.RegisterSuccess(bucket);

            var oldHash = voter.PasswordHash;
            voter.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            await _auditService.LogAsync("PROFILE_UPDATED", (int)voter.Id, "users", (int)voter.Id, "PasswordHash", oldHash, voter.PasswordHash, "User updated password", ipAddress);
            hasChanges = true;
        }

        if (hasChanges)
        {
            voter.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // Real email notification
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2e7d32;'>Hola {voter.FullName},</h2>
                    <p>Te informamos que tu perfil en Wahl Mirai ha sido actualizado con éxito (cambio de contraseña).</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                    <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático, por favor no respondas a este correo.</p>
                </div>
            ";
            await _emailSender.SendAsync(voter.ContactEmail, "Actualización de Perfil - Wahl Mirai", htmlBody);
        }

        return (true, string.Empty);
    }

    /// <inheritdoc />
    public async Task<(bool Success, string ErrorMessage)> VerifyCurrentPasswordAsync(int voterId, string? currentPassword, string ipAddress)
    {
        var bucket = ProfileBucketKey(voterId);
        if (_loginAttemptStore.IsLockedOut(bucket, out var remaining))
        {
            var mins = (int)Math.Ceiling(remaining.TotalMinutes);
            return (false, $"Demasiados intentos fallidos. Por seguridad, la operación está bloqueada temporalmente. Intenta de nuevo en {mins} minuto(s).");
        }

        if (string.IsNullOrEmpty(currentPassword))
        {
            return (false, "Debes ingresar tu contraseña actual.");
        }

        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null)
            return (false, "Usuario no encontrado.");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, voter.PasswordHash))
        {
            var attemptResult = _loginAttemptStore.RegisterFailure(bucket);
            await _auditService.LogAsync(
                "PROFILE_AUTH_FAILED", voterId, "users", voterId,
                "PasswordHash", null, null,
                $"Contraseña incorrecta al intentar cambiar contraseña. Intentos: {attemptResult.FailedCount}",
                ipAddress);

            if (attemptResult.JustLockedOut)
            {
                var mins = (int)Math.Ceiling(attemptResult.LockedUntilUtc!.Value.Subtract(DateTimeOffset.UtcNow).TotalMinutes);
                await _auditService.LogAsync(
                    "PROFILE_LOCKED_OUT", voterId, "users", voterId,
                    "PasswordHash", null, null,
                    $"Cuenta bloqueada temporalmente para cambio de contraseña. Duración: {mins} minuto(s).",
                    ipAddress);
                return (false,
                    $"Demasiados intentos fallidos. Por seguridad, la operación está bloqueada temporalmente. Intenta de nuevo en {mins} minuto(s).");
            }

            return (false, "La contraseña actual no es correcta.");
        }

        _loginAttemptStore.RegisterSuccess(bucket);
        return (true, string.Empty);
    }

    /// <summary>
    /// Solicita al servicio de credenciales emitir y enviar una nueva contraseña al usuario activo.
    /// </summary>
    /// <param name="voterId">Identificador del usuario cuya contraseña se reinicia.</param>
    /// <returns>El resultado de la solicitud y el mensaje para el usuario.</returns>
    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(int voterId)
    {
        // NOTA: la firma no deja claro si se exige la contraseña actual
        // ni cómo se notifica el reinicio; el envío no demuestra
        // procesamiento progresivo (RN-9) por sí solo — verificar contra
        // el consumidor de la cola.
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.Status != "ACTIVO") 
            return (false, "Usuario no encontrado o inactivo.");

        await _credentialService.IssuePasswordResetTokenAsync((int)voter.Id, (int)voter.Id);
        return (true, "Se ha enviado un enlace para restablecer tu contraseña al correo de contacto registrado. Tiene vigencia de 30 minutos.");
    }

    /// <inheritdoc />
    public async Task<(bool Success, bool EmailSaved, bool NotificationSent, string ErrorMessage)>
        UpdateContactEmailAsync(int voterId, string newEmail, string currentPassword, string ipAddress)
    {
        // ── Fase 0: Throttling de perfil (RN-15) ────────────────────────────────────
        // Bucket independiente del login para no interferir con el contador de autenticación.
        var bucket = ProfileBucketKey(voterId);
        if (_loginAttemptStore.IsLockedOut(bucket, out var remaining))
        {
            var mins = (int)Math.Ceiling(remaining.TotalMinutes);
            return (false, false, false,
                $"Demasiados intentos fallidos. Por seguridad, la operación está bloqueada temporalmente. Intenta de nuevo en {mins} minuto(s).");
        }

        // ── Fase 1: Validación de contraseña ────────────────────────────────────────
        if (string.IsNullOrEmpty(currentPassword))
            return (false, false, false, "Debes ingresar tu contraseña actual para modificar el correo de contacto.");

        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null)
            return (false, false, false, "Usuario no encontrado.");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, voter.PasswordHash))
        {
            var attemptResult = _loginAttemptStore.RegisterFailure(bucket);
            await _auditService.LogAsync(
                "PROFILE_AUTH_FAILED", voterId, "users", voterId,
                "ContactEmail", null, null,
                $"Contraseña incorrecta al intentar cambiar correo de contacto. Intentos: {attemptResult.FailedCount}",
                ipAddress);

            if (attemptResult.JustLockedOut)
            {
                var mins = (int)Math.Ceiling(attemptResult.LockedUntilUtc!.Value.Subtract(DateTimeOffset.UtcNow).TotalMinutes);
                await _auditService.LogAsync(
                    "PROFILE_LOCKED_OUT", voterId, "users", voterId,
                    "ContactEmail", null, null,
                    $"Cuenta bloqueada temporalmente para cambio de correo de contacto. Duración: {mins} minuto(s).",
                    ipAddress);
                return (false, false, false,
                    $"Demasiados intentos fallidos. Por seguridad, la operación está bloqueada temporalmente. Intenta de nuevo en {mins} minuto(s).");
            }

            return (false, false, false, "La contraseña actual no es correcta.");
        }

        // Contraseña correcta — limpia el bucket de perfil
        _loginAttemptStore.RegisterSuccess(bucket);

        // ── Fase 2: Validación de negocio ───────────────────────────────────────────
        if (voter.ContactEmail == newEmail)
            return (false, false, false, "El correo indicado es el mismo que el actual.");

        // ── Fase 3: Persistencia ────────────────────────────────────────────────────
        var oldEmail = voter.ContactEmail;
        voter.ContactEmail = newEmail;
        voter.UpdatedAt = DateTime.UtcNow;

        // Audit log antes de guardar (AuditService tiene su propio SaveChangesAsync interno)
        await _auditService.LogAsync(
            "PROFILE_UPDATED", (int)voter.Id, "users", (int)voter.Id,
            "ContactEmail", oldEmail, newEmail,
            "User updated contact email via AJAX modal", ipAddress);

        await _context.SaveChangesAsync();
        // A partir de aquí: EmailSaved = true (BD confirmada)

        // ── Fase 4: Aviso de seguridad a la dirección ANTERIOR ──────────────────────
        // NOTA: se usa IEmailSender directamente (sin encolar) porque email_queue no tiene
        // columna recipient y vw_pending_email_queue resuelve users.contact_email en tiempo
        // de entrega — un ítem encolado llegaría a la NUEVA dirección. Este bypass es
        // coherente con el patrón ya empleado en UpdateProfileAsync (RN-9 no aplica aquí).
        var maskedNew = MaskEmail(newEmail);
        bool notificationSent;
        try
        {
            var noticeBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #c62828;'>Alerta de seguridad — Wahl Mirai</h2>
                    <p>Hola {voter.FullName},</p>
                    <p>El <strong>{DateTime.Now:dd/MM/yyyy}</strong> a las <strong>{DateTime.Now:HH:mm} (hora local)</strong>
                       se registró un cambio en el correo de contacto asociado a tu cuenta.</p>
                    <p>El nuevo correo registrado es: <strong>{maskedNew}</strong></p>
                    <p style='color: #c62828;'><strong>Si no fuiste tú quien realizó este cambio</strong>,
                       comunícate de inmediato con el administrador del sistema para recuperar el acceso a tu cuenta.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                    <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático del sistema Wahl Mirai,
                       por favor no respondas a este correo.</p>
                </div>
            ";
            await _emailSender.SendAsync(oldEmail, "Alerta: tu correo de contacto fue modificado — Wahl Mirai", noticeBody);
            notificationSent = true;
        }
        catch
        {
            // El fallo SMTP al notificar la dirección anterior no revierte la BD
            // ni bloquea la confirmación al nuevo correo.
            notificationSent = false;
        }

        // ── Fase 5: Confirmación a la dirección NUEVA ───────────────────────────────
        // NOTA: fallo independiente; no afecta el valor de notificationSent (aviso a dirección anterior).
        try
        {
            var confirmBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2e7d32;'>Hola {voter.FullName},</h2>
                    <p>Tu correo de contacto en Wahl Mirai ha sido actualizado con éxito.</p>
                    <p>Tu nuevo correo de contacto registrado es: <strong>{newEmail}</strong></p>
                    <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                    <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático del sistema Wahl Mirai,
                       por favor no respondas a este correo.</p>
                </div>
            ";
            await _emailSender.SendAsync(newEmail, "Correo de Contacto Actualizado - Wahl Mirai", confirmBody);
        }
        catch
        {
            // El fallo de SMTP en la confirmación al nuevo correo no revierte el cambio en BD.
            // El campo notificationSent refleja el aviso a la dirección anterior, no esta confirmación.
        }

        return (true, true, notificationSent, string.Empty);
    }

    /// <summary>
    /// Enmascara una dirección de correo para mostrarla en avisos de seguridad
    /// sin revelar la dirección completa (ej. <c>j***@d***.com</c>).
    /// </summary>
    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return "***";

        var local = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var dotIndex = domain.LastIndexOf('.');

        var maskedLocal  = local.Length  <= 1 ? local  : local[0]  + "***";
        var maskedDomain = dotIndex <= 0 ? "***"
            : domain[..1] + "***" + domain[dotIndex..];

        return $"{maskedLocal}@{maskedDomain}";
    }
}
