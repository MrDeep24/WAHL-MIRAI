using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public interface ICensusService
{
    Task<List<VwActiveCensu>> GetActiveCensusAsync();
    Task<Voter> AddVoterAsync(string document, string fullName, byte? gradeId, byte roleId, bool excluirDePromocion, string adminIp);
    Task<bool> SoftDeleteVoterAsync(int voterId, string adminIp);
    Task<bool> RestoreVoterAsync(int voterId, string adminIp);
    Task<bool> ResetPasswordAsync(int voterId, string adminIp);
}

public class CensusService : ICensusService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;

    public CensusService(WahlMiraiDbContext context, IAuthService authService, IAuditService auditService)
    {
        _context = context;
        _authService = authService;
        _auditService = auditService;
    }

    public async Task<List<VwActiveCensu>> GetActiveCensusAsync()
    {
        return await _context.VwActiveCensus.ToListAsync();
    }

    public async Task<Voter> AddVoterAsync(string document, string fullName, byte? gradeId, byte roleId, bool excluirDePromocion, string adminIp)
    {
        var initialPassword = await _authService.GenerateInitialPasswordAsync(document);

        var voter = new Voter
        {
            DocumentHash     = _authService.HashDocument(document),
            EncryptedDocument = document,   // MVP: plain; production: AES-256
            FullName         = fullName,
            GradeId          = gradeId,
            RoleId           = roleId,
            PasswordHash     = _authService.HashPassword(initialPassword),
            RequiereCambioClave = true,
            ExcluirDePromocion  = excluirDePromocion,
            Status           = "ACTIVO",
            RegisteredAt     = DateTime.UtcNow
        };

        _context.Voters.Add(voter);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync("VOTER_CREATED", null, "voters", (int)voter.Id, null, null, null,
            $"Created voter: {fullName}", adminIp);

        return voter;
    }

    public async Task<bool> SoftDeleteVoterAsync(int voterId, string adminIp)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.Status == "ELIMINADO") return false;

        voter.Status    = "ELIMINADO";
        voter.DeletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("VOTER_DELETED", null, "voters", (int)voter.Id, "status", "ACTIVO", "ELIMINADO",
            "Soft delete", adminIp);
        return true;
    }

    public async Task<bool> RestoreVoterAsync(int voterId, string adminIp)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null || voter.Status != "ELIMINADO") return false;

        voter.Status    = "ACTIVO";
        voter.DeletedAt = null;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("VOTER_RESTORED", null, "voters", (int)voter.Id, "status", "ELIMINADO", "ACTIVO",
            "Restore", adminIp);
        return true;
    }

    public async Task<bool> ResetPasswordAsync(int voterId, string adminIp)
    {
        var voter = await _context.Voters.FindAsync((uint)voterId);
        if (voter == null) return false;

        var newPassword = await _authService.GenerateInitialPasswordAsync(voter.EncryptedDocument);
        voter.PasswordHash        = _authService.HashPassword(newPassword);
        voter.RequiereCambioClave = true;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync("PASSWORD_RESET", null, "voters", (int)voter.Id, "password_hash", null, null,
            "Admin reset password", adminIp);
        return true;
    }
}
