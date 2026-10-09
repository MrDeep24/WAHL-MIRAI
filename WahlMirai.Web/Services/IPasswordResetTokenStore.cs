// ============================================================
// Archivo:       IPasswordResetTokenStore.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define e implementa el almacén en memoria para tokens
//                de un solo uso de recuperación de acceso con expiración y revocación.
// Requerimientos: RF-M01-02, RN-2, RN-14, RN-15.
// ============================================================
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;

namespace WahlMirai.Web.Services;

/// <summary>
/// Contrato para emitir, inspeccionar y consumir tokens efímeros de un solo uso
/// para recuperación de acceso (RF-M01-02, RN-2, RN-14).
/// </summary>
public interface IPasswordResetTokenStore
{
    /// <summary>
    /// Emite un nuevo token de un solo uso para el usuario, invalidando cualquier token previo del mismo (RN-14).
    /// </summary>
    /// <param name="userId">Identificador de la cuenta.</param>
    /// <returns>Token crudo seguro para transmisión en URL.</returns>
    string Issue(uint userId);

    /// <summary>
    /// Valida la vigencia del token sin consumirlo ni retirarlo del almacén (RF-M01-02).
    /// </summary>
    /// <param name="token">Token presentado en la solicitud.</param>
    /// <param name="userId">Identificador del usuario asociado si es válido.</param>
    /// <returns><see langword="true"/> si el token existe y no ha expirado.</returns>
    bool TryPeek(string token, out uint userId);

    /// <summary>
    /// Valida y consume atómicamente el token, impidiendo su reutilización posterior (RF-M01-02, RN-14).
    /// </summary>
    /// <param name="token">Token presentado en la solicitud.</param>
    /// <param name="userId">Identificador del usuario asociado si fue consumido exitosamente.</param>
    /// <returns><see langword="true"/> si el token era válido y fue consumido.</returns>
    bool TryConsume(string token, out uint userId);

    /// <summary>
    /// Revoca cualquier token activo emitido para el usuario indicado.
    /// </summary>
    /// <param name="userId">Identificador de la cuenta.</param>
    void Revoke(uint userId);
}

/// <summary>
/// Implementa el almacén de tokens de recuperación en memoria local (RAM) respaldado por IMemoryCache
/// y sincronizado para atomicidad y unicidad por usuario (RF-M01-02, RN-15).
/// </summary>
public class PasswordResetTokenStore : IPasswordResetTokenStore
{
    /// <summary>Tiempo de vigencia de cada token de recuperación en minutos.</summary>
    public const int TokenTtlMinutes = 30;

    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new();

    private record TokenEntry(uint UserId, DateTimeOffset ExpiresAtUtc);

    /// <summary>Inicializa el almacén con la caché en memoria y el proveedor de tiempo.</summary>
    /// <param name="cache">Instancia de caché en memoria de la aplicación.</param>
    /// <param name="timeProvider">Proveedor de tiempo para validación determinista de expiración.</param>
    public PasswordResetTokenStore(IMemoryCache cache, TimeProvider? timeProvider = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public string Issue(uint userId)
    {
        var rawBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(rawBytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var tokenHash = HashToken(rawToken);

        var ttl = TimeSpan.FromMinutes(TokenTtlMinutes);
        var now = _timeProvider.GetUtcNow();
        var expiresAtUtc = now.Add(ttl);

        var tokenKey = GetTokenKey(tokenHash);
        var userKey = GetUserKey(userId);

        lock (_lock)
        {
            // Un solo token activo por usuario: revocar token previo si existe
            if (_cache.TryGetValue(userKey, out string? previousHash) && !string.IsNullOrEmpty(previousHash))
            {
                _cache.Remove(GetTokenKey(previousHash));
            }

            var entry = new TokenEntry(userId, expiresAtUtc);
            _cache.Set(tokenKey, entry, ttl);
            _cache.Set(userKey, tokenHash, ttl);
        }

        return rawToken;
    }

    /// <inheritdoc />
    public bool TryPeek(string token, out uint userId)
    {
        userId = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var tokenHash = HashToken(token);
        var tokenKey = GetTokenKey(tokenHash);

        lock (_lock)
        {
            if (_cache.TryGetValue(tokenKey, out TokenEntry? entry) && entry != null)
            {
                if (entry.ExpiresAtUtc > _timeProvider.GetUtcNow())
                {
                    userId = entry.UserId;
                    return true;
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    public bool TryConsume(string token, out uint userId)
    {
        userId = 0;
        if (string.IsNullOrWhiteSpace(token)) return false;

        var tokenHash = HashToken(token);
        var tokenKey = GetTokenKey(tokenHash);

        lock (_lock)
        {
            if (_cache.TryGetValue(tokenKey, out TokenEntry? entry) && entry != null)
            {
                if (entry.ExpiresAtUtc > _timeProvider.GetUtcNow())
                {
                    userId = entry.UserId;
                    _cache.Remove(tokenKey);

                    var userKey = GetUserKey(userId);
                    if (_cache.TryGetValue(userKey, out string? currentHash) && currentHash == tokenHash)
                    {
                        _cache.Remove(userKey);
                    }

                    return true;
                }

                // Expirado: limpiar entradas
                _cache.Remove(tokenKey);
                var expiredUserKey = GetUserKey(entry.UserId);
                if (_cache.TryGetValue(expiredUserKey, out string? expHash) && expHash == tokenHash)
                {
                    _cache.Remove(expiredUserKey);
                }
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void Revoke(uint userId)
    {
        var userKey = GetUserKey(userId);

        lock (_lock)
        {
            if (_cache.TryGetValue(userKey, out string? activeHash) && !string.IsNullOrEmpty(activeHash))
            {
                _cache.Remove(GetTokenKey(activeHash));
                _cache.Remove(userKey);
            }
        }
    }

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string GetTokenKey(string tokenHash) => "pwd_reset_token:" + tokenHash;
    private static string GetUserKey(uint userId) => "pwd_reset_user:" + userId;
}
