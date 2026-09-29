// ============================================================
// Archivo:       LoginAttemptStore.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Implementación en memoria del almacén de intentos fallidos
//                con ventana de 15 minutos, escalera de bloqueo y depuración perezosa.
// Requerimientos: RN-15.
// ============================================================
using System;
using System.Collections.Concurrent;

namespace WahlMirai.Web.Services;

/// <summary>
/// Estructura interna que almacena los intentos acumulados y el bloqueo vigente para una cuenta.
/// </summary>
internal sealed class LoginAttemptEntry
{
    public int FailedCount { get; set; }
    public DateTimeOffset WindowStartUtc { get; set; }
    public DateTimeOffset? LockedUntilUtc { get; set; }
}

/// <summary>
/// Almacén en memoria concurrente para rastrear intentos fallidos y bloqueos progresivos por documento (RN-15).
/// </summary>
public class LoginAttemptStore : ILoginAttemptStore
{
    private readonly TimeProvider _timeProvider;
    private readonly LoginAttemptPolicy _policy;
    private readonly ConcurrentDictionary<string, LoginAttemptEntry> _entries = new();

    /// <summary>
    /// Inicializa una nueva instancia de <see cref="LoginAttemptStore"/> con el proveedor de tiempo y política dados.
    /// </summary>
    /// <param name="timeProvider">Proveedor de fecha/hora para cómputo de ventanas y bloqueos.</param>
    /// <param name="policy">Política de ventana y umbrales (opcional).</param>
    public LoginAttemptStore(TimeProvider timeProvider, LoginAttemptPolicy? policy = null)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _policy = policy ?? new LoginAttemptPolicy();
    }

    /// <inheritdoc />
    public LoginAttemptResult RegisterFailure(string documentHash)
    {
        // NOTA: En memoria: el reinicio del proceso reinicia todos los contadores y bloqueos activos.
        // Mismo trade-off que IActiveSessionStore; no apto para escalado horizontal sin persistencia distribuida.
        // NOTA: Riesgo de denegación dirigida: un atacante que conozca el documento de un elector puede forzar su bloqueo intencionalmente.
        var now = _timeProvider.GetUtcNow();
        var entry = _entries.GetOrAdd(documentHash, _ => new LoginAttemptEntry());

        LoginAttemptResult result;
        lock (entry)
        {
            var windowRef = entry.LockedUntilUtc.HasValue && entry.LockedUntilUtc.Value > entry.WindowStartUtc
                ? entry.LockedUntilUtc.Value
                : entry.WindowStartUtc;

            if (entry.WindowStartUtc == default || (now - windowRef) > _policy.WindowDuration)
            {
                entry.FailedCount = 0;
                entry.WindowStartUtc = now;
                entry.LockedUntilUtc = null;
            }

            entry.FailedCount++;
            bool justLockedOut = false;

            if (entry.FailedCount >= _policy.LockoutThreshold)
            {
                var duration = _policy.GetLockoutDuration(entry.FailedCount);
                entry.LockedUntilUtc = now + duration;
                justLockedOut = true;
            }
            else
            {
                entry.LockedUntilUtc = null;
            }

            result = new LoginAttemptResult(entry.FailedCount, entry.LockedUntilUtc, justLockedOut);
        }

        if (_entries.Count > 5000)
        {
            LazyPurge(now);
        }

        return result;
    }

    /// <inheritdoc />
    public void RegisterSuccess(string documentHash)
    {
        _entries.TryRemove(documentHash, out _);
    }

    /// <inheritdoc />
    public bool IsLockedOut(string documentHash, out TimeSpan remaining)
    {
        var now = _timeProvider.GetUtcNow();
        if (!_entries.TryGetValue(documentHash, out var entry))
        {
            remaining = TimeSpan.Zero;
            return false;
        }

        lock (entry)
        {
            if (entry.LockedUntilUtc.HasValue && entry.LockedUntilUtc.Value > now)
            {
                remaining = entry.LockedUntilUtc.Value - now;
                return true;
            }

            remaining = TimeSpan.Zero;
            return false;
        }
    }

    private void LazyPurge(DateTimeOffset now)
    {
        foreach (var kvp in _entries)
        {
            var entry = kvp.Value;
            bool canRemove = false;
            lock (entry)
            {
                var windowRef = entry.LockedUntilUtc.HasValue && entry.LockedUntilUtc.Value > entry.WindowStartUtc
                    ? entry.LockedUntilUtc.Value
                    : entry.WindowStartUtc;

                bool windowExpired = entry.WindowStartUtc != default && (now - windowRef) > _policy.WindowDuration;
                bool lockExpired = !entry.LockedUntilUtc.HasValue || entry.LockedUntilUtc.Value <= now;
                if (windowExpired && lockExpired)
                {
                    canRemove = true;
                }
            }

            if (canRemove)
            {
                _entries.TryRemove(kvp.Key, out _);
            }
        }
    }
}
