// ============================================================
// Archivo:       ProfileService.cs
// Módulo:        M07 — Perfil de Usuario y Autogestión de Credenciales
// Desarrollador: Kevin
// Descripción:   Implementa la lectura y actualización de datos editables del perfil,
//                registra los cambios y delega la emisión de credenciales de reinicio.
// Requerimientos: RF-M07-01; soporte al flujo de recuperación RF-M01-02 y RN-8.
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

    /// <summary>
    /// Inicializa el servicio con dependencias de persistencia, auditoría y correo.
    /// </summary>
    /// <param name="context">Contexto de datos de usuarios.</param>
    /// <param name="auditService">Servicio para registrar los cambios de perfil.</param>
    /// <param name="emailSender">Servicio para enviar notificaciones de perfil.</param>
    /// <param name="credentialService">Servicio para emitir credenciales de reinicio.</param>
    public ProfileService(WahlMiraiDbContext context, IAuditService auditService, IEmailSender emailSender, ICredentialService credentialService)
    {
        _context = context;
        _auditService = auditService;
        _emailSender = emailSender;
        _credentialService = credentialService;
    }

    /// <summary>
    /// Actualiza el correo de contacto y/o la contraseña, registra los cambios y notifica al usuario.
    /// </summary>
    /// <param name="voterId">Identificador del usuario.</param>
    /// <param name="newContactEmail">Nuevo correo, o <see langword="null"/> para no modificarlo.</param>
    /// <param name="currentPassword">Contraseña actual opcional; solo se valida si se proporciona.</param>
    /// <param name="newPassword">Nueva contraseña, o <see langword="null"/> para no modificarla.</param>
    /// <param name="ipAddress">Dirección IP registrada junto con el cambio.</param>
    /// <returns>El resultado de la operación y un mensaje de error si falla.</returns>
    public async Task<(bool Success, string ErrorMessage)> UpdateProfileAsync(int voterId, string? newContactEmail, string? currentPassword, string? newPassword, string ipAddress)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null) return (false, "Usuario no encontrado.");

        // NOTA: la contraseña actual solo se valida si se proporciona; no se exige de forma incondicional.
        if (!string.IsNullOrEmpty(currentPassword))
        {
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, voter.PasswordHash))
            {
                return (false, "La contraseña actual no coincide.");
            }
        }

        bool hasChanges = false;

        // Check and update email (solo si se proporcionó)
        if (newContactEmail != null && voter.ContactEmail != newContactEmail)
        {
            var oldEmail = voter.ContactEmail;
            voter.ContactEmail = newContactEmail;
            
            await _auditService.LogAsync("PROFILE_UPDATED", (int)voter.Id, "users", (int)voter.Id, "ContactEmail", oldEmail, newContactEmail, "User updated contact email", ipAddress);
            hasChanges = true;
        }

        // Check and update password
        if (!string.IsNullOrEmpty(newPassword))
        {
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
                <div style='font-family: Arial, sans-serif; max-w-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2e7d32;'>Hola {voter.FullName},</h2>
                    <p>Te informamos que tu perfil en Wahl Mirai ha sido actualizado con éxito (cambio de correo de contacto o contraseña).</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                    <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático, por favor no respondas a este correo.</p>
                </div>
            ";
            await _emailSender.SendAsync(voter.ContactEmail, "Actualización de Perfil - Wahl Mirai", htmlBody);
        }

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

        await _credentialService.IssueNewPasswordAsync((int)voter.Id, EmailType.RECUPERACION_ACCESO, (int)voter.Id);
        return (true, "Se generará una nueva contraseña aleatoria y se enviará al correo de contacto registrado. Tu sesión actual permanecerá activa.");
    }

    /// <inheritdoc />
    public async Task<(bool Success, bool EmailSaved, bool NotificationSent, string ErrorMessage)>
        UpdateContactEmailAsync(int voterId, string newEmail, string ipAddress)
    {
        // ── Fase 1: Persistencia ────────────────────────────────────────────────
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null)
            return (false, false, false, "Usuario no encontrado.");

        if (voter.ContactEmail == newEmail)
            return (false, false, false, "El correo indicado es el mismo que el actual.");

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

        // ── Fase 2: Notificación por correo (independiente de la persistencia) ──
        bool notificationSent;
        try
        {
            var htmlBody = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #2e7d32;'>Hola {voter.FullName},</h2>
                    <p>Te informamos que tu correo de contacto en Wahl Mirai ha sido actualizado con éxito.</p>
                    <p>Tu nuevo correo de contacto registrado es: <strong>{newEmail}</strong></p>
                    <hr style='border: none; border-top: 1px solid #eee; margin-top: 30px;' />
                    <p style='color: #999; font-size: 0.8em;'>Este es un mensaje automático, por favor no respondas a este correo.</p>
                </div>
            ";
            await _emailSender.SendAsync(newEmail, "Correo de Contacto Actualizado - Wahl Mirai", htmlBody);
            notificationSent = true;
        }
        catch
        {
            // El fallo de SMTP no revierte el cambio en BD. Se informa al caller
            // para que lo comunique al usuario sin ocultar el estado parcial.
            notificationSent = false;
        }

        return (true, true, notificationSent, string.Empty);
    }
}
