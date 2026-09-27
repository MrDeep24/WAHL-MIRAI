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
    /// Busca una cuenta activa por el hash del documento y valida su contraseña (RF-M01-01).
    /// </summary>
    /// <param name="document">Documento ingresado como identificador de acceso.</param>
    /// <param name="password">Contraseña en texto plano que se verificará contra el hash.</param>
    /// <returns>La cuenta autenticada o <see langword="null"/> si no existe, está inactiva o la clave no coincide.</returns>
    Task<Voter?> ValidateLoginAsync(string document, string password);

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
/// Implementa validación de credenciales, persistencia de cambios de contraseña y funciones de hash.
/// </summary>
public class AuthService : IAuthService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;

    /// <summary>Inicializa el servicio con el contexto de datos y el servicio de auditoría.</summary>
    /// <param name="context">Contexto de usuarios y demás datos de la aplicación.</param>
    /// <param name="auditService">Servicio que registra accesos y cambios de contraseña.</param>
    public AuthService(WahlMiraiDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <inheritdoc />
    public async Task<Voter?> ValidateLoginAsync(string document, string password)
    {
        var docHash = HashDocument(document);
        var voter = await _context.Voters
            .Include(v => v.Role)
            .Include(v => v.Grade)
            .FirstOrDefaultAsync(v => v.DocumentHash == docHash && v.Status == "ACTIVO");

        if (voter == null) return null;

        if (BCrypt.Net.BCrypt.Verify(password, voter.PasswordHash))
        {
            await _auditService.LogAsync("LOGIN_SUCCESS", (int)voter.Id, "users", (int)voter.Id, null, null, null, null, null);
            return voter;
        }

        await _auditService.LogAsync("LOGIN_FAILED", (int)voter.Id, "users", (int)voter.Id, null, null, null, "Invalid password", null);
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
