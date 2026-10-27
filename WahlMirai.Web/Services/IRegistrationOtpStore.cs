// ============================================================
// Archivo:       IRegistrationOtpStore.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Contrato e implementación en memoria del almacén de tickets
//                y códigos OTP para auto-registro seguro (RF-M01-00, RN-1, RN-15).
// Requerimientos: RF-M01-00, RN-1, RN-1.1, RN-2, RN-2.1, RN-15.
// ============================================================
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace WahlMirai.Web.Services;

/// <summary>
/// Resultado de la emisión de un ticket de registro y despacho de código OTP (RF-M01-00, RN-15).
/// </summary>
public sealed record RegistrationTicketResult(
    string Ticket,
    string? RawOtp,
    string? ContactEmail,
    bool IsCooldownActive,
    int CooldownRemainingSeconds = 0,
    bool HasValidSessionForEntry = false);

/// <summary>
/// Resultado del reenvío de un código OTP asociado a un ticket de registro (RF-M01-00, RN-15).
/// </summary>
public sealed record ResendOtpResult(
    bool Success,
    string? RawOtp,
    string? ContactEmail,
    bool IsCooldownActive,
    int CooldownRemainingSeconds,
    string? Message);

/// <summary>
/// Contrato para emitir, validar y consumir tickets y códigos OTP de un solo uso durante el auto-registro
/// con verificación estricta al correo preautorizado de la lista blanca (RF-M01-00, RN-1, RN-15).
/// </summary>
public interface IRegistrationOtpStore
{
    /// <summary>
    /// Emite un ticket de registro y genera un código OTP para la entrada de lista blanca indicada.
    /// Si está en cooldown de 60 segundos por entrada: no reemite OTP ni revoca el ticket previo;
    /// si la petición presenta la cookie válida de esa entrada, indica sesión válida;
    /// de lo contrario, reporta cooldown activo con los segundos restantes.
    /// </summary>
    RegistrationTicketResult IssueRegistrationTicket(
        string document,
        uint whitelistId,
        string contactEmail,
        string? documentHash,
        string? currentTicket = null,
        string? fullName = null);

    /// <summary>
    /// Valida el código OTP provisto contra el ticket de registro entregado en cookie.
    /// En caso de éxito, marca el ticket como verificado y consume el código OTP (un solo uso).
    /// En caso de fallo, incrementa el contador de fallos del código y retorna false.
    /// </summary>
    bool TryValidateOtp(string ticket, string code, out string? errorMessage, out bool isLockoutEligible);

    /// <summary>
    /// Reenvía el código OTP para el ticket indicado respetando el cooldown de 60 segundos.
    /// Si está en cooldown, no emite nuevo código ni reinicia intentos.
    /// </summary>
    ResendOtpResult ResendOtp(string ticket);

    /// <summary>
    /// Inspecciona si el ticket es válido y ya se encuentra verificado por OTP (RF-M01-00, D1).
    /// </summary>
    bool TryGetVerifiedTicket(string ticket, out uint whitelistId, out string document);

    /// <summary>
    /// Consume atómicamente el ticket verificado para completar el auto-registro, impidiendo su reutilización.
    /// </summary>
    bool TryConsumeVerifiedTicket(string ticket, out uint whitelistId, out string document);

    /// <summary>
    /// Obtiene el nombre y correo parcialmente enmascarados desde el ticket en memoria (RF-M01-00).
    /// Retorna false si el ticket es nulo, inválido o ha expirado.
    /// </summary>
    bool TryGetTicketDisplayInfo(string ticket, out string maskedFullName, out string maskedEmail);

    /// <summary>
    /// Emite un código OTP numérico simple (adaptador de compatibilidad).
    /// </summary>
    string IssueOtp(uint whitelistId, string email);

    /// <summary>
    /// Valida el código OTP simple (adaptador de compatibilidad).
    /// </summary>
    bool TryValidateOtp(uint whitelistId, string code, out string? errorMessage);

    /// <summary>
    /// Revoca cualquier estado activo asociado al ticket y libera el cooldown de la entrada censal.
    /// </summary>
    void Revoke(string ticket);

    /// <summary>
    /// Enmascara una dirección de correo para presentarla de forma segura en la interfaz.
    /// </summary>
    string MaskEmail(string email);

    /// <summary>
    /// Enmascara un nombre completo por palabra (máximo 2 caracteres visibles, 1 si longitud menor o igual a 3).
    /// </summary>
    string MaskFullName(string? fullName);
}

/// <summary>
/// Implementación concurrente en memoria respaldada por IMemoryCache con expiración determinista (15 min),
/// cooldown de 60 segundos y límite estricto de intentos para mitigar adivinación por fuerza bruta (RF-M01-00, RN-15).
/// </summary>
public class RegistrationOtpStore : IRegistrationOtpStore
{
    /// <summary>Vigencia en minutos del ticket y código OTP.</summary>
    public const int OtpTtlMinutes = 15;

    /// <summary>Máximo de intentos fallidos antes de invalidar el código.</summary>
    public const int MaxFailedAttempts = 5;

    /// <summary>Tiempo de espera mínimo entre emisiones consecutivas de código (cooldown).</summary>
    public const int CooldownSeconds = 60;

    private readonly IMemoryCache _cache;
    private readonly TimeProvider _timeProvider;
    private readonly object _lock = new();

    private sealed record TicketEntry(
        string Ticket,
        uint WhitelistId,
        string Document,
        string DocumentHash,
        string? ContactEmail,
        bool IsVerified,
        string? CodeHash,
        DateTimeOffset ExpiresAtUtc,
        DateTimeOffset LastIssuedAtUtc,
        int FailedAttempts,
        string MaskedFullName,
        string MaskedEmail);

    private sealed record WhitelistCooldownInfo(string Ticket, DateTimeOffset LastIssuedAtUtc);

    public RegistrationOtpStore(IMemoryCache cache, TimeProvider? timeProvider = null)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public RegistrationTicketResult IssueRegistrationTicket(
        string document,
        uint whitelistId,
        string contactEmail,
        string? documentHash,
        string? currentTicket = null,
        string? fullName = null)
    {
        if (whitelistId == 0 || string.IsNullOrWhiteSpace(contactEmail))
        {
            throw new ArgumentException("Se requiere un ID de lista blanca válido y un correo de contacto.");
        }

        var now = _timeProvider.GetUtcNow();
        var ttl = TimeSpan.FromMinutes(OtpTtlMinutes);
        var expiresAtUtc = now.Add(ttl);

        lock (_lock)
        {
            // Valid entry: check issuance cooldown per whitelistId
            var cooldownKey = GetWhitelistCooldownKey(whitelistId);
            if (_cache.TryGetValue(cooldownKey, out WhitelistCooldownInfo? cooldownInfo) && cooldownInfo != null)
            {
                var elapsed = now - cooldownInfo.LastIssuedAtUtc;
                if (elapsed < TimeSpan.FromSeconds(CooldownSeconds))
                {
                    var remaining = (int)Math.Ceiling((TimeSpan.FromSeconds(CooldownSeconds) - elapsed).TotalSeconds);

                    // Si la petición presenta la cookie válida que coincide con el ticket activo de la entrada censal:
                    if (!string.IsNullOrWhiteSpace(currentTicket) &&
                        string.Equals(currentTicket, cooldownInfo.Ticket, StringComparison.Ordinal) &&
                        _cache.TryGetValue(GetTicketKey(currentTicket), out TicketEntry? existingEntry) &&
                        existingEntry != null &&
                        existingEntry.WhitelistId == whitelistId)
                    {
                        return new RegistrationTicketResult(
                            cooldownInfo.Ticket,
                            null,
                            contactEmail,
                            IsCooldownActive: true,
                            CooldownRemainingSeconds: remaining,
                            HasValidSessionForEntry: true);
                    }

                    // Dentro del cooldown sin cookie válida: NO emite ticket, NO reemite OTP, NO revoca ticket existente.
                    return new RegistrationTicketResult(
                        string.Empty,
                        null,
                        null,
                        IsCooldownActive: true,
                        CooldownRemainingSeconds: remaining,
                        HasValidSessionForEntry: false);
                }

                // Cooldown expirado: revocar el ticket anterior de esa entrada antes de reemitir
                if (!string.IsNullOrWhiteSpace(cooldownInfo.Ticket))
                {
                    _cache.Remove(GetTicketKey(cooldownInfo.Ticket));
                }
            }

            // Issue new 6-digit numeric OTP with CSPRNG
            var randomInt = RandomNumberGenerator.GetInt32(100000, 1000000);
            var rawOtp = randomInt.ToString("D6");
            var codeHash = HashCode(rawOtp);
            var ticket = GenerateTicketToken();

            var maskedFullName = MaskFullName(fullName);
            var maskedEmail = MaskEmail(contactEmail);

            var ticketEntry = new TicketEntry(
                ticket,
                whitelistId,
                document,
                documentHash ?? string.Empty,
                contactEmail.Trim(),
                IsVerified: false,
                codeHash,
                expiresAtUtc,
                now,
                0,
                maskedFullName,
                maskedEmail);

            _cache.Set(GetTicketKey(ticket), ticketEntry, ttl);
            _cache.Set(cooldownKey, new WhitelistCooldownInfo(ticket, now), ttl);

            return new RegistrationTicketResult(ticket, rawOtp, contactEmail.Trim(), IsCooldownActive: false, CooldownRemainingSeconds: 0, HasValidSessionForEntry: false);
        }
    }

    /// <inheritdoc />
    public bool TryValidateOtp(string ticket, string code, out string? errorMessage, out bool isLockoutEligible)
    {
        isLockoutEligible = false;
        if (string.IsNullOrWhiteSpace(ticket))
        {
            errorMessage = "La sesión de verificación ha expirado o no es válida.";
            isLockoutEligible = true;
            return false;
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            errorMessage = "Debes ingresar el código de verificación de 6 dígitos.";
            isLockoutEligible = true;
            return false;
        }

        var normalizedCode = code.Trim();
        var now = _timeProvider.GetUtcNow();

        lock (_lock)
        {
            if (!_cache.TryGetValue(GetTicketKey(ticket), out TicketEntry? entry) || entry == null || entry.ExpiresAtUtc <= now)
            {
                errorMessage = "El código de verificación o la sesión ha expirado. Inicia el proceso nuevamente.";
                isLockoutEligible = true;
                return false;
            }

            if (entry.IsVerified)
            {
                errorMessage = null;
                return true;
            }

            if (entry.FailedAttempts >= MaxFailedAttempts || string.IsNullOrEmpty(entry.CodeHash))
            {
                errorMessage = "Demasiados intentos fallidos. Solicita un nuevo código de verificación.";
                isLockoutEligible = true;
                return false;
            }

            var inputHash = HashCode(normalizedCode);
            var isMatch = CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(inputHash),
                Encoding.UTF8.GetBytes(entry.CodeHash));

            if (isMatch)
            {
                // Single-use: code hash is wiped and ticket is marked as verified
                var updated = entry with { IsVerified = true, CodeHash = null };
                var remainingTtl = entry.ExpiresAtUtc - now;
                _cache.Set(GetTicketKey(ticket), updated, remainingTtl > TimeSpan.Zero ? remainingTtl : TimeSpan.FromSeconds(1));
                errorMessage = null;
                return true;
            }

            var newFailed = entry.FailedAttempts + 1;
            isLockoutEligible = true;

            if (newFailed >= MaxFailedAttempts)
            {
                var updated = entry with { FailedAttempts = newFailed, CodeHash = null };
                var remainingTtl = entry.ExpiresAtUtc - now;
                _cache.Set(GetTicketKey(ticket), updated, remainingTtl > TimeSpan.Zero ? remainingTtl : TimeSpan.FromSeconds(1));
                errorMessage = "Demasiados intentos fallidos. Solicita un nuevo código de verificación.";
                return false;
            }
            else
            {
                var updated = entry with { FailedAttempts = newFailed };
                var remainingTtl = entry.ExpiresAtUtc - now;
                _cache.Set(GetTicketKey(ticket), updated, remainingTtl > TimeSpan.Zero ? remainingTtl : TimeSpan.FromSeconds(1));
                errorMessage = $"El código de verificación es incorrecto. Intentos restantes: {MaxFailedAttempts - newFailed}.";
                return false;
            }
        }
    }

    /// <inheritdoc />
    public ResendOtpResult ResendOtp(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket))
        {
            return new ResendOtpResult(false, null, null, false, 0, "Sesión no válida.");
        }

        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            if (!_cache.TryGetValue(GetTicketKey(ticket), out TicketEntry? entry) || entry == null || entry.ExpiresAtUtc <= now)
            {
                return new ResendOtpResult(false, null, null, false, 0, "La sesión ha expirado.");
            }

            DateTimeOffset lastIssued = entry.LastIssuedAtUtc;
            var cooldownKey = GetWhitelistCooldownKey(entry.WhitelistId);
            if (_cache.TryGetValue(cooldownKey, out WhitelistCooldownInfo? cdInfo) && cdInfo != null)
            {
                if (cdInfo.LastIssuedAtUtc > lastIssued)
                {
                    lastIssued = cdInfo.LastIssuedAtUtc;
                }
            }

            var elapsed = now - lastIssued;
            if (elapsed < TimeSpan.FromSeconds(CooldownSeconds))
            {
                var remaining = (int)Math.Ceiling((TimeSpan.FromSeconds(CooldownSeconds) - elapsed).TotalSeconds);
                return new ResendOtpResult(false, null, null, true, remaining, $"Debes esperar {remaining} segundos antes de solicitar un nuevo código.");
            }

            if (entry.IsVerified)
            {
                return new ResendOtpResult(false, null, null, false, 0, "El código ya ha sido verificado.");
            }

            // Generate new OTP and reset failed attempts counter
            var randomInt = RandomNumberGenerator.GetInt32(100000, 1000000);
            var rawOtp = randomInt.ToString("D6");
            var codeHash = HashCode(rawOtp);
            var remainingTtlTicket = entry.ExpiresAtUtc - now;

            var updated = entry with
            {
                CodeHash = codeHash,
                FailedAttempts = 0,
                LastIssuedAtUtc = now
            };

            _cache.Set(GetTicketKey(ticket), updated, remainingTtlTicket);
            _cache.Set(GetWhitelistCooldownKey(entry.WhitelistId), new WhitelistCooldownInfo(ticket, now), remainingTtlTicket);

            return new ResendOtpResult(true, rawOtp, entry.ContactEmail, false, 0, null);
        }
    }

    /// <inheritdoc />
    public bool TryGetVerifiedTicket(string ticket, out uint whitelistId, out string document)
    {
        whitelistId = 0;
        document = string.Empty;

        if (string.IsNullOrWhiteSpace(ticket)) return false;

        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            if (!_cache.TryGetValue(GetTicketKey(ticket), out TicketEntry? entry) || entry == null || entry.ExpiresAtUtc <= now)
            {
                return false;
            }

            if (!entry.IsVerified)
            {
                return false;
            }

            whitelistId = entry.WhitelistId;
            document = entry.Document;
            return true;
        }
    }

    /// <inheritdoc />
    public bool TryConsumeVerifiedTicket(string ticket, out uint whitelistId, out string document)
    {
        whitelistId = 0;
        document = string.Empty;

        if (string.IsNullOrWhiteSpace(ticket)) return false;

        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            if (!_cache.TryGetValue(GetTicketKey(ticket), out TicketEntry? entry) || entry == null || entry.ExpiresAtUtc <= now)
            {
                return false;
            }

            if (!entry.IsVerified)
            {
                return false;
            }

            whitelistId = entry.WhitelistId;
            document = entry.Document;

            _cache.Remove(GetTicketKey(ticket));
            _cache.Remove(GetWhitelistCooldownKey(entry.WhitelistId));
            return true;
        }
    }

    /// <inheritdoc />
    public bool TryGetTicketDisplayInfo(string ticket, out string maskedFullName, out string maskedEmail)
    {
        maskedFullName = string.Empty;
        maskedEmail = string.Empty;

        if (string.IsNullOrWhiteSpace(ticket)) return false;

        var now = _timeProvider.GetUtcNow();
        lock (_lock)
        {
            if (!_cache.TryGetValue(GetTicketKey(ticket), out TicketEntry? entry) || entry == null || entry.ExpiresAtUtc <= now)
            {
                return false;
            }

            maskedFullName = entry.MaskedFullName;
            maskedEmail = entry.MaskedEmail;
            return true;
        }
    }

    /// <inheritdoc />
    public string IssueOtp(uint whitelistId, string email)
    {
        var result = IssueRegistrationTicket(string.Empty, whitelistId, email, null);
        return result.RawOtp ?? string.Empty;
    }

    /// <inheritdoc />
    public bool TryValidateOtp(uint whitelistId, string code, out string? errorMessage)
    {
        var cooldownKey = GetWhitelistCooldownKey(whitelistId);
        if (_cache.TryGetValue(cooldownKey, out WhitelistCooldownInfo? info) && info != null)
        {
            return TryValidateOtp(info.Ticket, code, out errorMessage, out _);
        }

        errorMessage = "El código de verificación ha expirado o no es válido.";
        return false;
    }

    /// <inheritdoc />
    public void Revoke(string ticket)
    {
        if (string.IsNullOrWhiteSpace(ticket)) return;

        lock (_lock)
        {
            if (_cache.TryGetValue(GetTicketKey(ticket), out TicketEntry? entry) && entry != null)
            {
                _cache.Remove(GetWhitelistCooldownKey(entry.WhitelistId));
            }
            _cache.Remove(GetTicketKey(ticket));
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

    /// <inheritdoc />
    public string MaskFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return "***";

        var words = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) return "***";

        var maskedWords = new List<string>(words.Length);
        foreach (var word in words)
        {
            if (word.Length <= 3)
            {
                maskedWords.Add(word[..1] + "***");
            }
            else
            {
                maskedWords.Add(word[..2] + "***");
            }
        }

        return string.Join(" ", maskedWords);
    }

    private static string GetTicketKey(string ticket) => $"reg_ticket:{ticket}";
    private static string GetWhitelistCooldownKey(uint whitelistId) => $"reg_wl_cooldown:{whitelistId}";

    private static string GenerateTicketToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string HashCode(string rawCode)
    {
        var bytes = Encoding.UTF8.GetBytes(rawCode);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
