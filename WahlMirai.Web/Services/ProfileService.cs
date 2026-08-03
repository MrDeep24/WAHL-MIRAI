using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class ProfileService : IProfileService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IEmailSender _emailSender;
    private readonly ICredentialService _credentialService;

    public ProfileService(WahlMiraiDbContext context, IAuditService auditService, IEmailSender emailSender, ICredentialService credentialService)
    {
        _context = context;
        _auditService = auditService;
        _emailSender = emailSender;
        _credentialService = credentialService;
    }

    public async Task<(bool Success, string ErrorMessage)> UpdateProfileAsync(int voterId, string? newContactEmail, string? currentPassword, string? newPassword, string ipAddress)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null) return (false, "Usuario no encontrado.");

        // Verify current password (solo si fue proporcionada, p. ej. desde el modal)
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
            // Validate uniqueness
            var emailExists = await _context.Voters.AnyAsync(v => v.ContactEmail == newContactEmail && v.Id != voter.Id);
            if (emailExists)
            {
                return (false, "El correo electrónico ya está en uso por otro usuario.");
            }

            var oldEmail = voter.ContactEmail;
            voter.ContactEmail = newContactEmail;
            
            await _auditService.LogAsync("PROFILE_UPDATED", (int)voter.Id, "voters", (int)voter.Id, "ContactEmail", oldEmail, newContactEmail, "User updated contact email", ipAddress);
            hasChanges = true;
        }

        // Check and update password
        if (!string.IsNullOrEmpty(newPassword))
        {
            var oldHash = voter.PasswordHash;
            voter.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            
            await _auditService.LogAsync("PROFILE_UPDATED", (int)voter.Id, "voters", (int)voter.Id, "PasswordHash", oldHash, voter.PasswordHash, "User updated password", ipAddress);
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

    public async Task<(bool Success, string Message)> RequestPasswordResetAsync(int voterId)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.Status != "ACTIVO") 
            return (false, "Usuario no encontrado o inactivo.");

        await _credentialService.IssueNewPasswordAsync((int)voter.Id, EmailType.RECUPERACION_ACCESO, (int)voter.Id);
        return (true, "Se generará una nueva contraseña aleatoria y se enviará al correo de contacto registrado. Tu sesión actual permanecerá activa.");
    }
}
