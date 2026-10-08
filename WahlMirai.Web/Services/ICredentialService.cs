// ============================================================
// Archivo:       ICredentialService.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Declara la operación de emisión de nuevas contraseñas
//                y los tipos de notificación asociados.
// Requerimientos: Contrato de apoyo para RF-M01-02, RF-M07-02 y RF-M09-01.
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
}
