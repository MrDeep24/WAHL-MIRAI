// ============================================================
// Archivo:       ILoginAttemptStore.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define el contrato y las políticas para registrar
//                intentos fallidos, éxitos y bloqueos temporales por cuenta.
// Requerimientos: RN-1, RN-1.1, RN-15 (auto-registro y mitigación de fuerza bruta).
// ============================================================
using System;

namespace WahlMirai.Web.Services;

/// <summary>
/// Representa el resultado de registrar un intento fallido en los buckets de RN-15 y auto-registro RN-1.
/// </summary>
/// <param name="FailedCount">Número de intentos fallidos acumulados en la ventana actual.</param>
/// <param name="LockedUntilUtc">Fecha/hora UTC hasta la que la cuenta queda bloqueada, o <see langword="null"/> si no hay bloqueo.</param>
/// <param name="JustLockedOut">Indica si este intento específico activó una nueva condición de bloqueo.</param>
public record LoginAttemptResult(int FailedCount, DateTimeOffset? LockedUntilUtc, bool JustLockedOut);

/// <summary>
/// Define la política de ventana temporal y la progresión escalonada de bloqueos (RN-15).
/// </summary>
public class LoginAttemptPolicy
{
    /// <summary>Duración de la ventana de acumulación de fallos (15 minutos).</summary>
    public TimeSpan WindowDuration { get; init; } = TimeSpan.FromMinutes(15);

    /// <summary>Umbral de intentos fallidos para iniciar el bloqueo (5 intentos).</summary>
    public int LockoutThreshold { get; init; } = 5;

    /// <summary>
    /// Calcula la duración del bloqueo con el umbral configurado (escalera: 1 min, 5 min, 15 min).
    /// </summary>
    /// <param name="failedCount">Conteo acumulado de fallos en la ventana.</param>
    /// <returns>Duración del bloqueo temporal.</returns>
    public TimeSpan GetLockoutDuration(int failedCount)
    {
        if (failedCount < LockoutThreshold) return TimeSpan.Zero;
        return failedCount switch
        {
            5 => TimeSpan.FromMinutes(1),
            6 => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromMinutes(15)
        };
    }

    /// <summary>Calcula la escalera usando un umbral específico para un bucket independiente (RN-1, RN-15).</summary>
    /// <param name="failedCount">Conteo acumulado en la ventana.</param>
    /// <param name="lockoutThreshold">Conteo que activa el primer bloqueo.</param>
    /// <returns>1 minuto en el umbral, 5 minutos en el siguiente fallo y 15 minutos desde el posterior.</returns>
    public TimeSpan GetLockoutDuration(int failedCount, int lockoutThreshold)
    {
        if (lockoutThreshold < 1) throw new ArgumentOutOfRangeException(nameof(lockoutThreshold));
        if (failedCount < lockoutThreshold) return TimeSpan.Zero;
        return (failedCount - lockoutThreshold) switch
        {
            0 => TimeSpan.FromMinutes(1),
            1 => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromMinutes(15)
        };
    }
}

/// <summary>
/// Contrato para seguir intentos y bloqueos por cuenta, perfil y auto-registro (RN-1, RN-1.1, RN-15).
/// </summary>
public interface ILoginAttemptStore
{
    /// <summary>
    /// Registra un intento fallido para el hash de documento indicado (RN-15).
    /// Incrementa el contador, renueva o evalúa la ventana temporal y calcula el bloqueo si alcanza el umbral.
    /// </summary>
    /// <param name="documentHash">Hash SHA-256 del documento de identidad.</param>
    /// <returns>Resultado con el conteo acumulado y el estado de bloqueo resultante.</returns>
    LoginAttemptResult RegisterFailure(string documentHash);

    /// <summary>
    /// Registra un fallo en un bucket con umbral independiente, como el bucket IP de auto-registro (RN-1, RN-15).
    /// </summary>
    /// <param name="bucketKey">Clave aislada del contador, por ejemplo <c>register:{ip}</c>.</param>
    /// <param name="lockoutThreshold">Fallos dentro de la ventana antes de activar el bloqueo.</param>
    /// <returns>Conteo, expiración del bloqueo y si este intento lo activó.</returns>
    LoginAttemptResult RegisterFailure(string bucketKey, int lockoutThreshold);

    /// <summary>
    /// Registra un inicio de sesión exitoso, removiendo cualquier contador o bloqueo previo (RN-15).
    /// </summary>
    /// <param name="documentHash">Hash SHA-256 del documento de identidad.</param>
    void RegisterSuccess(string documentHash);

    /// <summary>
    /// Comprueba si la cuenta asociada al hash de documento se encuentra bloqueada en el momento actual (RN-15).
    /// </summary>
    /// <param name="documentHash">Hash SHA-256 del documento de identidad.</param>
    /// <param name="remaining">Tiempo restante de bloqueo en caso de encontrarse bloqueada.</param>
    /// <returns><see langword="true"/> si la cuenta está bloqueada; de lo contrario, <see langword="false"/>.</returns>
    bool IsLockedOut(string documentHash, out TimeSpan remaining);
}
