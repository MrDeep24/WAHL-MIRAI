using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public interface IAuthService
{
    Task<Voter?> ValidateLoginAsync(string document, string password);
    Task<string> GenerateInitialPasswordAsync(string document);
    Task<bool> ChangePasswordAsync(int voterId, string newPassword, string ipAddress);
    string HashPassword(string password);
    string HashDocument(string document);
}

public class AuthService : IAuthService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuditService _auditService;

    public AuthService(WahlMiraiDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<Voter?> ValidateLoginAsync(string document, string password)
    {
        var docHash = HashDocument(document);
        var voter = await _context.Voters
            .Include(v => v.Role)
            .FirstOrDefaultAsync(v => v.DocumentHash == docHash && v.Status == "ACTIVO");

        if (voter == null) return null;

        if (BCrypt.Net.BCrypt.Verify(password, voter.PasswordHash))
        {
            await _auditService.LogAsync("LOGIN_SUCCESS", (int)voter.Id, "voters", (int)voter.Id, null, null, null, null, null);
            return voter;
        }

        await _auditService.LogAsync("LOGIN_FAILED", (int)voter.Id, "voters", (int)voter.Id, null, null, null, "Invalid password", null);
        return null;
    }

    public async Task<string> GenerateInitialPasswordAsync(string document)
    {
        var currentYear = await _context.AcademicYears.FirstOrDefaultAsync(a => a.IsCurrent);
        int year = currentYear?.Year ?? DateTime.UtcNow.Year;
        return $"{document}.{year}";
    }

    public async Task<bool> ChangePasswordAsync(int voterId, string newPassword, string ipAddress)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null) return false;

        var oldHash = voter.PasswordHash;
        voter.PasswordHash = HashPassword(newPassword);
        voter.RequiereCambioClave = false;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PASSWORD_CHANGED", (int)voter.Id, "voters", (int)voter.Id, "password_hash", oldHash, voter.PasswordHash, "User changed password", ipAddress);

        return true;
    }

    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }

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
