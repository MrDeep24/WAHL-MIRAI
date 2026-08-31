using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public interface IWhitelistService
{
    Task<CensusWhitelist?> GetUnclaimedEntryByDocumentHashAsync(string documentHash);
    Task<Voter> RegisterElectorAsync(
        string document,
        uint whitelistId,
        string contactEmail,
        string password,
        string ipAddress);
}

public class WhitelistService : IWhitelistService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuthService _authService;
    private readonly IDocumentEncryptionService _encryptionService;

    public WhitelistService(
        WahlMiraiDbContext context,
        IAuthService authService,
        IDocumentEncryptionService encryptionService)
    {
        _context = context;
        _authService = authService;
        _encryptionService = encryptionService;
    }

    public async Task<CensusWhitelist?> GetUnclaimedEntryByDocumentHashAsync(string documentHash)
    {
        return await _context.CensusWhitelists
            .Include(w => w.Grade)
            .FirstOrDefaultAsync(w => w.DocumentHash == documentHash && w.ClaimedAt == null);
    }

    public async Task<Voter> RegisterElectorAsync(
        string document,
        uint whitelistId,
        string contactEmail,
        string password,
        string ipAddress)
    {
        var entry = await _context.CensusWhitelists
            .Include(w => w.Grade)
            .FirstOrDefaultAsync(w => w.Id == whitelistId);

        if (entry == null || entry.ClaimedAt != null)
            throw new InvalidOperationException("Whitelist entry not found or already claimed.");

        var computedHash = _authService.HashDocument(document);
        if (!string.Equals(computedHash, entry.DocumentHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Submitted document does not match the whitelist entry.");

        var existingByDoc = await _context.Voters.AnyAsync(v => v.DocumentHash == computedHash);
        if (existingByDoc)
            throw new InvalidOperationException("A user with this document already exists.");

        var normalizedEmail = contactEmail.Trim();
        var existingByEmail = await _context.Voters.AnyAsync(v => v.ContactEmail.ToLower() == normalizedEmail.ToLower());
        if (existingByEmail)
            throw new InvalidOperationException($"The email '{contactEmail}' is already registered.");

        var electorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "ELECTOR")
            ?? throw new InvalidOperationException("ELECTOR role not found in the roles catalogue.");

        if (_context.Database.IsRelational())
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var voter = await CreateElectorAsync(entry, electorRole, computedHash, document, contactEmail, password, ipAddress);
                await tx.CommitAsync();
                return voter;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        return await CreateElectorAsync(entry, electorRole, computedHash, document, contactEmail, password, ipAddress);
    }

    private async Task<Voter> CreateElectorAsync(
        CensusWhitelist entry,
        Role electorRole,
        string computedHash,
        string document,
        string contactEmail,
        string password,
        string ipAddress)
    {
        var voter = new Voter
        {
            RoleId = electorRole.Id,
            GradeId = entry.GradeId,
            DocumentHash = computedHash,
            EncryptedDocument = _encryptionService.Encrypt(document),
            FullName = entry.FullName,
            ContactEmail = contactEmail.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            ExcluirDePromocion = entry.ExcluirDePromocion,
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow,
        };

        _context.Voters.Add(voter);
        await _context.SaveChangesAsync();

        entry.ClaimedAt = DateTime.UtcNow;
        entry.ClaimedByUserId = voter.Id;

        _context.AuditLogs.Add(new AuditLog
        {
            Action = "SELF_REGISTER",
            VoterId = voter.Id,
            TargetEntity = "users",
            TargetId = (int)voter.Id,
            IpAddress = ipAddress,
            Details = $"Self-registration completed. Whitelist entry id={entry.Id} claimed.",
            OccurredAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        voter.Role = electorRole;
        voter.Grade = entry.Grade;
        return voter;
    }
}
