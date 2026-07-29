using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public interface ICensusService
{
    Task<List<VwActiveCensu>> GetActiveCensusAsync();
    Task<Voter> AddVoterAsync(string document, string fullName, string contactEmail, byte? gradeId, byte roleId, bool excluirDePromocion, string adminIp);
    Task<bool> SoftDeleteVoterAsync(int voterId, string adminIp);
    Task<bool> RestoreVoterAsync(int voterId, string adminIp);
    Task<bool> ResetPasswordAsync(int voterId, string adminIp);
}

public class CensusService : ICensusService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuthService _authService;
    private readonly IAuditService _auditService;
    private readonly IDocumentEncryptionService _encryptionService;
    private readonly ICredentialService _credentialService;

    public CensusService(
        WahlMiraiDbContext context,
        IAuthService authService,
        IAuditService auditService,
        IDocumentEncryptionService encryptionService,
        ICredentialService credentialService)
    {
        _context = context;
        _authService = authService;
        _auditService = auditService;
        _encryptionService = encryptionService;
        _credentialService = credentialService;
    }

    public async Task<List<VwActiveCensu>> GetActiveCensusAsync()
    {
        return await _context.VwActiveCensus.ToListAsync();
    }

    public async Task<Voter> AddVoterAsync(string document, string fullName, string contactEmail, byte? gradeId, byte roleId, bool excluirDePromocion, string adminIp)
    {
        // Use a temporary hash; CredentialService will overwrite it with the real secure one
        var voter = new Voter
        {
            DocumentHash      = _authService.HashDocument(document),
            EncryptedDocument = _encryptionService.Encrypt(document),   // Cifrado con Data Protection API (RF-M02-01)
            FullName          = fullName,
            ContactEmail      = contactEmail,
            GradeId           = gradeId,
            RoleId            = roleId,
            PasswordHash      = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // temporal placeholder
            ExcluirDePromocion = excluirDePromocion,
            Status            = "ACTIVO",
            RegisteredAt      = DateTime.UtcNow
        };

        _context.Voters.Add(voter);
        await _context.SaveChangesAsync();

        // Issue secure random password and queue welcome email (CREDENCIAL_INICIAL)
        await _credentialService.IssueNewPasswordAsync((int)voter.Id, EmailType.CREDENCIAL_INICIAL, null);

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
        if (voter == null || string.IsNullOrWhiteSpace(voter.ContactEmail)) return false;

        await _credentialService.IssueNewPasswordAsync(voterId, EmailType.REASIGNACION_ADMIN, null);
        
        return true;
    }
}
