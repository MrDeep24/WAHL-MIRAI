namespace WahlMirai.Web.Services;

public interface IProfileService
{
    Task<(bool Success, string ErrorMessage)> UpdateProfileAsync(int voterId, string? newContactEmail, string? currentPassword, string? newPassword, string ipAddress);
    Task<(bool Success, string Message)> RequestPasswordResetAsync(int voterId);
}
