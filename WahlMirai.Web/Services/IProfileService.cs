namespace WahlMirai.Web.Services;

public interface IProfileService
{
    Task<(bool Success, string ErrorMessage)> UpdateProfileAsync(int voterId, string? newContactEmail, string? currentPassword, string? newPassword, string ipAddress);
    Task<(bool Success, string Message)> RequestPasswordResetAsync(int voterId);

    /// <summary>
    /// Actualiza solo el correo de contacto y devuelve estado diferenciado para
    /// permitir que la capa de presentación distinga entre fallo de persistencia
    /// y fallo de notificación SMTP (el cambio en BD puede haber ocurrido aunque
    /// el envío de correo falle).
    /// </summary>
    Task<(bool Success, bool EmailSaved, bool NotificationSent, string ErrorMessage)>
        UpdateContactEmailAsync(int voterId, string newEmail, string ipAddress);
}
