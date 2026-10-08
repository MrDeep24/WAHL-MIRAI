// ============================================================
// Archivo:       IAuthService.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define e implementa la validación de acceso,
//                el cambio de contraseña y el hashing de credenciales.
// Requerimientos: RF-M01-01, RF-M07-01; apoyo de hashing para RF-M01-00.
// ============================================================
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

/// <summary>
/// Contrato de autenticación, cambio de contraseña y hashing utilizado por los flujos
/// de acceso y auto-registro (RF-M01-01; apoyo de hashing para RF-M01-00).
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Busca una cuenta activa por el hash del documento y valida su contraseña considerando bloqueo por intentos fallidos (RF-M01-01, RN-15).
    /// </summary>
    /// <param name="document">Documento ingresado como identificador de acceso.</param>
    /// <param name="password">Contraseña en texto plano que se verificará contra el hash.</param>
    /// <param name="ipAddress">Dirección IP del cliente asociada al intento de acceso.</param>
    /// <returns>La cuenta autenticada o <see langword="null"/> si no existe, está inactiva, está bloqueada o la clave no coincide.</returns>
    Task<Voter?> ValidateLoginAsync(string document, string password, string? ipAddress);

    /// <summary>
    /// Cambia la contraseña de una cuenta y registra la operación (apoyo para RF-M07-01).
    /// </summary>
    /// <param name="voterId">Identificador de la cuenta.</param>
    /// <param name="newPassword">Nueva contraseña en texto plano.</param>
    /// <param name="ipAddress">Dirección IP asociada a la solicitud.</param>
    /// <returns><see langword="true"/> si se encontró y actualizó la cuenta.</returns>
    Task<bool> ChangePasswordAsync(int voterId, string newPassword, string ipAddress);

    /// <summary>Genera el hash BCrypt de una contraseña para almacenarla.</summary>
    /// <param name="password">Contraseña en texto plano.</param>
    /// <returns>Hash BCrypt de la contraseña.</returns>
    string HashPassword(string password);

    /// <summary>Calcula la representación hexadecimal SHA-256 de un documento.</summary>
    /// <param name="document">Documento en texto plano.</param>
    /// <returns>Hash SHA-256 en minúsculas.</returns>
    string HashDocument(string document);
}

/// <summary>
/// Implementa validación de credenciales, protección contra fuerza bruta, persistencia de cambios de contraseña y funciones de hash.
/// </summary>
public class AuthService : IAuthService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;
    private readonly ILoginAttemptStore _loginAttemptStore;

    // Hash dummy para mitigar ataques de temporización (timing attacks) cuando la cuenta no existe o está bloqueada.
    private static readonly string DummyHash = BCrypt.Net.BCrypt.HashPassword("dummy-password-for-timing-mitigation");

    /// <summary>Inicializa el servicio con el contexto de datos, servicio de auditoría y almacén de intentos.</summary>
    /// <param name="context">Contexto de usuarios y demás datos de la aplicación.</param>
    /// <param name="auditService">Servicio que registra accesos y cambios de contraseña.</param>
    /// <param name="loginAttemptStore">Almacén de seguimiento y bloqueo de intentos fallidos (RN-15).</param>
    public AuthService(WahlMiraiDbContext context, IAuditService auditService, ILoginAttemptStore loginAttemptStore)
    {
        _context = context;
        _auditService = auditService;
        _loginAttemptStore = loginAttemptStore;
    }

    /// <inheritdoc />
    public async Task<Voter?> ValidateLoginAsync(string document, string password, string? ipAddress)
    {
        var docHash = HashDocument(document);

        // NOTA: Riesgo de denegación dirigida: un atacante que conozca el número de documento de un estudiante
        // puede disparar deliberadamente el bloqueo durante la ventana de votación (RN-15).
        if (_loginAttemptStore.IsLockedOut(docHash, out _))
        {
            // Mitigación de timing attack: ejecutar verificación contra hash dummy y retornar sin auditar
            BCrypt.Net.BCrypt.Verify(password, DummyHash);
            return null;
        }

        var voter = await _context.Voters
            .Include(v => v.Role)
            .Include(v => v.Grade)
            .FirstOrDefaultAsync(v => v.DocumentHash == docHash && v.Status == "ACTIVO");

        if (voter == null)
        {
            // Mitigación de timing attack para documentos no encontrados
            BCrypt.Net.BCrypt.Verify(password, DummyHash);
            return null;
        }

        if (BCrypt.Net.BCrypt.Verify(password, voter.PasswordHash))
        {
            _loginAttemptStore.RegisterSuccess(docHash);
            await _auditService.LogAsync("LOGIN_SUCCESS", (int)voter.Id, "users", (int)voter.Id, null, null, null, null, ipAddress);
            return voter;
        }

        var attemptResult = _loginAttemptStore.RegisterFailure(docHash);
        await _auditService.LogAsync("LOGIN_FAILED", (int)voter.Id, "users", (int)voter.Id, null, null, null, "Invalid password", ipAddress);

        if (attemptResult.JustLockedOut)
        {
            var lockedUntilStr = attemptResult.LockedUntilUtc?.UtcDateTime.ToString("u") ?? "unknown";
            await _auditService.LogAsync("LOGIN_LOCKED_OUT", (int)voter.Id, "users", (int)voter.Id, null, null, null, $"Threshold reached; locked until {lockedUntilStr}", ipAddress);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<bool> ChangePasswordAsync(int voterId, string newPassword, string ipAddress)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null) return false;

        var oldHash = voter.PasswordHash;
        voter.PasswordHash = HashPassword(newPassword);

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PASSWORD_CHANGED", (int)voter.Id, "users", (int)voter.Id, "password_hash", oldHash, voter.PasswordHash, "User changed password", ipAddress);

        return true;
    }

    /// <inheritdoc />
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

    /// <inheritdoc />
    public string HashDocument(string document)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(document));
        var builder = new StringBuilder();
        foreach (var b in bytes)
        {
            builder.Append(b.ToString("x2"));
        }
        return builder.ToString();
    }
}
