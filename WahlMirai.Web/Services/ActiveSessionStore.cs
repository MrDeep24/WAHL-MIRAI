// ============================================================
// Archivo:       ActiveSessionStore.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Mantiene en memoria el sello vigente por usuario
//                y permite validar o invalidar sesiones concurrentemente.
// Requerimientos: RN-14 (implementación del almacén de sesión activa).
// ============================================================
using System.Collections.Concurrent;

namespace WahlMirai.Web.Services;

/// <summary>Representa el sello registrado y el motivo pendiente de invalidación.</summary>
/// <param name="Stamp">Sello único asignado a la sesión.</param>
/// <param name="PendingReason">Motivo que se devuelve si el sello no es válido.</param>
public record SessionEntry(string Stamp, SessionInvalidationReason? PendingReason);

/// <summary>
/// Implementa en memoria el registro, validación e invalidación de sesiones únicas por usuario (RN-14).
/// </summary>
public class ActiveSessionStore : IActiveSessionStore
{
    private readonly ConcurrentDictionary<uint, SessionEntry> _sessions = new();

    /// <inheritdoc />
    public void RegisterSession(uint userId, string sessionStamp)
    {
        _sessions[userId] = new SessionEntry(sessionStamp, SessionInvalidationReason.ReplacedByNewDevice);
    }

    /// <inheritdoc />
    public (bool IsValid, SessionInvalidationReason? Reason) IsSessionValid(uint userId, string sessionStamp)
    {
        if (string.IsNullOrWhiteSpace(sessionStamp))
        {
            return (false, SessionInvalidationReason.ReplacedByNewDevice);
        }

        if (!_sessions.TryGetValue(userId, out var entry))
        {
            return (false, SessionInvalidationReason.ReplacedByNewDevice);
        }

        if (entry.Stamp == sessionStamp)
        {
            return (true, null);
        }

        return (false, entry.PendingReason ?? SessionInvalidationReason.ReplacedByNewDevice);
    }

    /// <inheritdoc />
    public void InvalidateSession(uint userId, SessionInvalidationReason reason)
    {
        _sessions[userId] = new SessionEntry(string.Empty, reason);
    }
}
