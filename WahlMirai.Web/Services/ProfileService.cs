using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public class ProfileService : IProfileService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;

    public ProfileService(WahlMiraiDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<(bool Success, string ErrorMessage)> UpdateProfileAsync(int voterId, string newContactEmail, string currentPassword, string? newPassword, string ipAddress)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null) return (false, "Usuario no encontrado.");

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, voter.PasswordHash))
        {
            return (false, "La contraseña actual no coincide.");
        }

        bool hasChanges = false;

        // Check and update email
        if (voter.ContactEmail != newContactEmail)
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
            
            // Simulate email notification
            Console.WriteLine($"[SIMULATED EMAIL] Notificación enviada a {voter.ContactEmail} sobre la actualización de su perfil.");
        }

        return (true, string.Empty);
    }
}
