// ============================================================
// Archivo:       IRegistrationOtpStore.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Contrato e implementación en memoria del almacén de códigos OTP
//                de 6 dígitos para validación de auto-registro contra lista blanca (v3.0).
// Requerimientos: RF-M01-00, RN-1, RN-1.1, RN-15.
// ============================================================
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace WahlMirai.Web.Services;

/// <summary>
/// Contrato para emitir y validar códigos de un solo uso (OTP) durante el auto-registro
/// con verificación al correo de la lista blanca (RF-M01-00, RN-1, RN-15).
/// </summary>
public interface IRegistrationOtpStore
{
    /// <summary>
    /// Emite un código OTP de 6 dígitos asociado a la entrada de lista blanca.
    /// Invalida cualquier código previo emitido para la misma entrada.
    /// </summary>
    /// <param name="whitelistId">Identificador de la entrada en census_whitelist.</param>
    /// <param name="email">Correo electrónico preautorizado donde se enviará el código.</param>
    /// <returns>Código numérico de 6 dígitos en texto plano para despacho por correo.</returns>
    string IssueOtp(uint whitelistId, string email);

    /// <summary>
    /// Valida el código OTP provisto por el usuario. Si es correcto, lo consume atómicamente.
    /// Si falla 5 veces consecutivas, el código se invalida por seguridad (RN-15).
    /// </summary>
    /// <param name="whitelistId">Identificador de la entrada en census_whitelist.</param>
    /// <param name="code">Código de 6 dígitos ingresado por el usuario.</param>
    /// <param name="errorMessage">Mensaje de error descriptivo en caso de fallo.</param>
    /// <returns><see langword="true"/> si el código es válido y fue consumido exitosamente.</returns>
    bool TryValidateOtp(uint whitelistId, string code, out string? errorMessage);

    /// <summary>
    /// Revoca cualquier código activo para la entrada indicada.
    /// </summary>
    /// <param name="whitelistId">Identificador de la entrada en census_whitelist.</param>
    void Revoke(uint whitelistId);

    /// <summary>
    /// Enmascara una dirección de correo para presentarla de forma segura en la interfaz.
    /// </summary>
    /// <param name="email">Dirección de correo a enmascarar.</param>
    /// <returns>Dirección enmascarada (ej. <c>j***@d***.com</c>).</returns>
    string MaskEmail(string email);
}

/// <summary>
/// Implementación concurrente en memoria respaldada por IMemoryCache con expiración determinista (15 min)
/// y límite estricto de intentos para mitigar adivinación por fuerza bruta (RF-M01-00, RN-15).
/// </summary>
public class RegistrationOtpStore : IRegistrationOtpStore
{
    /// <summary>Vigencia en minutos del código OTP.</summary>
    public const int OtpTtlMinutes = 15;

    /// <summary>Máximo de intentos fallidos antes de invalidar el código.</summary>
    public const int MaxFailedAttempts = 5;

    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new();

    private record OtpEntry(uint WhitelistId, string Email, string CodeHash, DateTimeOffset ExpiresAtUtc, int FailedAttempts);

    public RegistrationOtpStore(IMemoryCache cache, TimeProvider? timeProvider = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string IssueOtp(uint whitelistId, string email)
    {
        // Código criptográfico de 6 dígitos: 100000 - 999999
        var randomInt = RandomNumberGenerator.GetInt32(100000, 1000000);
        var rawCode = randomInt.ToString("D6");
        var codeHash = HashCode(rawCode);

        var ttl = TimeSpan.FromMinutes(OtpTtlMinutes);
        var now = _timeProvider.GetUtcNow();
        var expiresAtUtc = now.Add(ttl);
        var cacheKey = GetCacheKey(whitelistId);

        lock (_lock)
        {
            var entry = new OtpEntry(whitelistId, email, codeHash, expiresAtUtc, 0);
            _cache.Set(cacheKey, entry, ttl);
        }

        return rawCode;
    }

    /// <inheritdoc />
    public bool TryValidateOtp(uint whitelistId, string code, out string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            errorMessage = "Debes ingresar el código de verificación de 6 dígitos.";
            return false;
        }

        var normalizedCode = code.Trim();
        var cacheKey = GetCacheKey(whitelistId);
        var now = _timeProvider.GetUtcNow();

        lock (_lock)
        {
            if (!_cache.TryGetValue(cacheKey, out OtpEntry? entry) || entry == null || entry.ExpiresAtUtc <= now)
            {
                _cache.Remove(cacheKey);
                errorMessage = "El código de verificación ha expirado o no es válido.";
                return false;
            }

            var inputHash = HashCode(normalizedCode);
            var isMatch = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(inputHash),
                Encoding.UTF8.GetBytes(entry.CodeHash));

            if (isMatch)
            {
                // Código consumido exitosamente (un solo uso)
                _cache.Remove(cacheKey);
                errorMessage = null;
                return true;
            }

            var newAttempts = entry.FailedAttempts + 1;
            if (newAttempts >= MaxFailedAttempts)
            {
                // Agotado: revocar de inmediato
                _cache.Remove(cacheKey);
                errorMessage = "Demasiados intentos fallidos. Solicita un nuevo código de verificación.";
                return false;
            }

            // Actualizar contador de fallos preservando la expiración original
            var remainingTtl = entry.ExpiresAtUtc - now;
            var updatedEntry = entry with { FailedAttempts = newAttempts };
            _cache.Set(cacheKey, updatedEntry, remainingTtl > TimeSpan.Zero ? remainingTtl : TimeSpan.FromSeconds(1));

            errorMessage = $"El código de verificación es incorrecto. Intentos restantes: {MaxFailedAttempts - newAttempts}.";
            return false;
        }
    }

    /// <inheritdoc />
    public void Revoke(uint whitelistId)
    {
        lock (_lock)
        {
            _cache.Remove(GetCacheKey(whitelistId));
        }
    }

    /// <inheritdoc />
    public string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "***";
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0) return "***";

        var local = email[..atIndex];
        var domain = email[(atIndex + 1)..];
        var dotIndex = domain.LastIndexOf('.');

        var maskedLocal = local.Length <= 1 ? local : local[0] + "***";
        var maskedDomain = dotIndex <= 0 ? "***" : domain[..1] + "***" + domain[dotIndex..];

        return $"{maskedLocal}@{maskedDomain}";
    }

    private static string GetCacheKey(uint whitelistId) => $"reg_otp:{whitelistId}";

    private static string HashCode(string rawCode)
    {
        var bytes = Encoding.UTF8.GetBytes(rawCode);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
