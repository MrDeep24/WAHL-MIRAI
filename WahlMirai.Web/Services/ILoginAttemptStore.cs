// ============================================================
// Archivo:       ILoginAttemptStore.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define el contrato y las políticas para registrar
//                intentos fallidos, éxitos y bloqueos temporales por cuenta.
// Requerimientos: RN-15 (mitigación de fuerza bruta, ventana y escalera de bloqueo).
// ============================================================
using System;

namespace WahlMirai.Web.Services;

/// <summary>
/// Representa el resultado de registrar un intento fallido de inicio de sesión (RN-15).
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
    /// Calcula la duración del bloqueo según la cantidad de fallos acumulados (escalera: 1 min, 5 min, 15 min).
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
}

/// <summary>
/// Contrato para gestionar el seguimiento en memoria de intentos fallidos y bloqueo progresivo por cuenta (RN-15).
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
