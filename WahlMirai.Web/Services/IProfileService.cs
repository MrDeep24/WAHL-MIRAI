// ============================================================
// Archivo:       IProfileService.cs
// Módulo:        M07 — Perfil de Usuario y Autogestión de Credenciales
// Desarrollador: Kevin
// Descripción:   Define las operaciones de actualización del perfil propio,
//                reinicio de contraseña y cambio de correo con notificación.
// Requerimientos: RF-M07-01; soporte al flujo de recuperación RF-M01-02.
// ============================================================

namespace WahlMirai.Web.Services;

/// <summary>
/// Contrato de las operaciones de perfil y autogestión de credenciales.
/// </summary>
public interface IProfileService
{
    /// <summary>
    /// Actualiza el correo de contacto y/o la contraseña del usuario indicado.
    /// </summary>
    /// <param name="voterId">Identificador del usuario.</param>
    /// <param name="newContactEmail">Nuevo correo, o <see langword="null"/> para no modificarlo.</param>
    /// <param name="currentPassword">Contraseña actual opcional; se valida cuando se proporciona.</param>
    /// <param name="newPassword">Nueva contraseña, o <see langword="null"/> para no modificarla.</param>
    /// <param name="ipAddress">Dirección IP asociada a la operación de auditoría.</param>
    /// <returns>El resultado de la operación y un mensaje de error si falla.</returns>
    Task<(bool Success, string ErrorMessage)> UpdateProfileAsync(int voterId, string? newContactEmail, string? currentPassword, string? newPassword, string ipAddress);

    /// <summary>
    /// Solicita la emisión y el envío de una nueva contraseña para el usuario indicado.
    /// </summary>
    /// <param name="voterId">Identificador del usuario cuya contraseña se reinicia.</param>
    /// <returns>El resultado de la solicitud y el mensaje que se presentará al usuario.</returns>
    Task<(bool Success, string Message)> RequestPasswordResetAsync(int voterId);
    // NOTA: la firma no deja claro si se exige la contraseña actual
    // ni cómo se notifica el reinicio; el envío no demuestra
    // procesamiento progresivo (RN-9) por sí solo — verificar contra
    // el consumidor de la cola.

    /// <summary>
    /// Actualiza solo el correo de contacto y devuelve estado diferenciado para
    /// permitir que la capa de presentación distinga entre fallo de persistencia
    /// y fallo de notificación SMTP (el cambio en BD puede haber ocurrido aunque
    /// el envío de correo falle).
    /// </summary>
    Task<(bool Success, bool EmailSaved, bool NotificationSent, string ErrorMessage)>
        UpdateContactEmailAsync(int voterId, string newEmail, string ipAddress);
}
