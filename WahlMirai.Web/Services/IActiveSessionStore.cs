// ============================================================
// Archivo:       IActiveSessionStore.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define el contrato para registrar, validar e invalidar
//                el sello de sesión activa de cada usuario.
// Requerimientos: Contrato de apoyo para RN-14.
// ============================================================
namespace WahlMirai.Web.Services;

/// <summary>
/// Motivo por el que un sello de sesión deja de ser válido (RN-14).
/// </summary>
public enum SessionInvalidationReason
{
    /// <summary>La sesión fue reemplazada por un acceso desde otro dispositivo.</summary>
    ReplacedByNewDevice,
    /// <summary>La cuenta fue deshabilitada o eliminada lógicamente.</summary>
    AccountDisabledOrDeleted,
    /// <summary>El usuario cerró su sesión.</summary>
    LoggedOut
}

/// <summary>Contrato de apoyo para gestionar la sesión única e invalidación inmediata (RN-14).</summary>
public interface IActiveSessionStore
{
    /// <summary>Registra el sello de sesión vigente para el usuario.</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="sessionStamp">Sello único asociado a la sesión.</param>
    void RegisterSession(uint userId, string sessionStamp);

    /// <summary>Comprueba si el sello presentado coincide con la sesión vigente.</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="sessionStamp">Sello presentado por la solicitud.</param>
    /// <returns>Validez del sello y, si no es válido, el motivo de invalidación disponible.</returns>
    (bool IsValid, SessionInvalidationReason? Reason) IsSessionValid(uint userId, string sessionStamp);

    /// <summary>Invalida la sesión vigente y conserva el motivo asociado (RN-14).</summary>
    /// <param name="userId">Identificador del usuario.</param>
    /// <param name="reason">Motivo de la invalidación.</param>
    void InvalidateSession(uint userId, SessionInvalidationReason reason);
}
