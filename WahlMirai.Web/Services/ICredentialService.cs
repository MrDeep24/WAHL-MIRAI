// ============================================================
// Archivo:       ICredentialService.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Declara la operación de emisión de nuevas contraseñas,
//                tokens de restablecimiento y la confirmación del cambio de contraseña.
// Requerimientos: RF-M01-02, RF-M07-02, RF-M09-01; RN-2, RN-9, RN-14, RN-15.
// ============================================================
namespace WahlMirai.Web.Services;

/// <summary>
/// Identifica el motivo de notificación asociado a una emisión de credenciales o a otros avisos del sistema.
/// </summary>
public enum EmailType
{
    /// <summary>Recuperación de acceso solicitada por el usuario (RF-M01-02).</summary>
    RECUPERACION_ACCESO,
    /// <summary>Reasignación de contraseña iniciada por administración (RF-M07-02).</summary>
    REASIGNACION_ADMIN,
    /// <summary>Cambio de correo o datos de perfil que requiere aviso.</summary>
    CAMBIO_PERFIL,
    /// <summary>Respuesta a una solicitud PQR (RF-M08-02).</summary>
    RESPUESTA_PQR,
    /// <summary>Notificación de aprobación de candidatura.</summary>
    CANDIDATURA_APROBADA,
    /// <summary>Notificación de rechazo de candidatura.</summary>
    CANDIDATURA_RECHAZADA
}

/// <summary>
/// Contrato de apoyo para solicitar una nueva contraseña en flujos de recuperación,
/// reasignación y creación de cuentas (RF-M01-02, RF-M07-02, RF-M09-01).
/// </summary>
public interface ICredentialService
{
    /// <summary>
    /// Solicita la emisión de una nueva contraseña y asocia la notificación con su motivo.
    /// </summary>
    /// <param name="voterId">Identificador de la cuenta que recibirá la nueva credencial.</param>
    /// <param name="emailType">Motivo que determina el tipo de notificación.</param>
    /// <param name="actorVoterId">Identificador del actor que inicia la operación, si aplica.</param>
    /// <param name="ct">Token para cancelar la operación asincrónica.</param>
    /// <returns>Tarea que representa la operación de emisión.</returns>
    Task IssueNewPasswordAsync(int voterId, EmailType emailType, int? actorVoterId, CancellationToken ct = default);

    /// <summary>
    /// Solicita la emisión de un token de restablecimiento de contraseña de un solo uso y encola el correo con el enlace (RF-M01-02, RN-2, RN-9).
    /// </summary>
    /// <param name="userId">Identificador de la cuenta que solicita la recuperación.</param>
    /// <param name="actorUserId">Identificador del usuario actor que solicita la operación, si aplica.</param>
    /// <param name="ct">Token para cancelar la operación asincrónica.</param>
    /// <returns>Tarea que representa la operación de emisión y encolado.</returns>
    Task IssuePasswordResetTokenAsync(int userId, int? actorUserId, CancellationToken ct = default);

    /// <summary>
    /// Confirma el restablecimiento de contraseña: consume el token de un solo uso, actualiza el hash,
    /// limpia el bloqueo de login (RN-15), invalida la sesión activa (RN-14) y registra auditoría.
    /// Devuelve <see langword="false"/> con mensaje genérico si el token es inválido, expirado, ya
    /// consumido o si el usuario no está ACTIVO, sin revelar el motivo exacto (anti-enumeración).
    /// </summary>
    /// <param name="token">Token crudo presentado en la URL de restablecimiento.</param>
    /// <param name="newPassword">Nueva contraseña ya validada por el ViewModel (complejidad RN-2).</param>
    /// <param name="ipAddress">Dirección IP del solicitante para auditoría.</param>
    /// <param name="ct">Token de cancelación.</param>
    /// <returns>(<see langword="true"/>, string.Empty) si se completó; (<see langword="false"/>, mensajeGenérico) si el token no es válido.</returns>
    Task<(bool Success, string ErrorMessage)> CompletePasswordResetAsync(
        string token, string newPassword, string? ipAddress, CancellationToken ct = default);
}
