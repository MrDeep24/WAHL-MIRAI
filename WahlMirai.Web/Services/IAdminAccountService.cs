using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

public sealed class AdminAccountListItem
{
    public uint Id { get; set; }
    public string Document { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public string? PositionTitle { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public interface IAdminAccountService
{
    Task<List<AdminAccountListItem>> GetAccountsAsync(bool includeDeleted = true, CancellationToken ct = default);
    Task CreateAsync(string document, string fullName, string contactEmail, string roleName,
        string? positionTitle, int actorVoterId, string ipAddress, CancellationToken ct = default);
    Task<bool> UpdateAsync(int accountId, string document, string fullName, string contactEmail,
        string roleName, string? positionTitle, int actorVoterId, string ipAddress, CancellationToken ct = default);
    Task<bool> SoftDeleteAsync(int accountId, int actorVoterId, string ipAddress, CancellationToken ct = default);
    Task<bool> RestoreAsync(int accountId, int actorVoterId, string ipAddress, CancellationToken ct = default);
}

public sealed class AdminAccountService : IAdminAccountService
{
    private static readonly string[] AllowedRoles = ["ADMIN", "SUPER_ADMIN"];
    private readonly WahlMiraiDbContext _context;
    private readonly IAuthService _authService;
    private readonly IDocumentEncryptionService _encryptionService;
    private readonly ICredentialService _credentialService;
    private readonly IAuditService _auditService;

    public AdminAccountService(WahlMiraiDbContext context, IAuthService authService,
        IDocumentEncryptionService encryptionService, ICredentialService credentialService,
        IAuditService auditService)
    {
        _context = context;
        _authService = authService;
        _encryptionService = encryptionService;
        _credentialService = credentialService;
        _auditService = auditService;
    }

    public async Task<List<AdminAccountListItem>> GetAccountsAsync(bool includeDeleted = true, CancellationToken ct = default)
    {
        var query = _context.Voters.Include(v => v.Role)
            .Where(v => v.Role.Name == "ADMIN" || v.Role.Name == "SUPER_ADMIN");
        if (!includeDeleted)
            query = query.Where(v => v.Status != "ELIMINADO");

        var accounts = await query.OrderBy(v => v.FullName).ToListAsync(ct);
        return accounts.Select(v => new AdminAccountListItem
        {
            Id = v.Id,
            Document = DecryptDocument(v.EncryptedDocument),
            FullName = v.FullName,
            ContactEmail = v.ContactEmail,
            RoleName = v.Role.Name,
            PositionTitle = v.PositionTitle,
            Status = v.Status,
            RegisteredAt = v.RegisteredAt,
            DeletedAt = v.DeletedAt
        }).ToList();
    }

    public async Task CreateAsync(string document, string fullName, string contactEmail, string roleName,
        string? positionTitle, int actorVoterId, string ipAddress, CancellationToken ct = default)
    {
        var normalizedDocument = NormalizeRequired(document, "documento");
        var normalizedName = NormalizeRequired(fullName, "nombre");
        var normalizedEmail = NormalizeEmail(contactEmail);
        var normalizedRole = NormalizeRole(roleName);
        var documentHash = _authService.HashDocument(normalizedDocument);

        if (await _context.Voters.AnyAsync(v => v.DocumentHash == documentHash, ct))
            throw new InvalidOperationException("Ya existe una cuenta con ese documento.");

        var account = new Voter
        {
            RoleId = await GetRoleIdAsync(normalizedRole, ct),
            GradeId = null,
            DocumentHash = documentHash,
            EncryptedDocument = _encryptionService.Encrypt(normalizedDocument),
            FullName = normalizedName,
            ContactEmail = normalizedEmail,
            PasswordHash = string.Empty,
            PositionTitle = NormalizeOptional(positionTitle),
            ExcluirDePromocion = false,
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        };

        _context.Voters.Add(account);
        await _context.SaveChangesAsync(ct);
        await _credentialService.IssueNewPasswordAsync((int)account.Id, EmailType.REASIGNACION_ADMIN, actorVoterId, ct);
        await _auditService.LogAsync("ADMIN_ACCOUNT_CREATED", actorVoterId, "users", (int)account.Id,
            null, null, normalizedRole, "{\"credential_email_type\":\"REASIGNACION_ADMIN\"}", ipAddress);
    }

    public async Task<bool> UpdateAsync(int accountId, string document, string fullName, string contactEmail,
        string roleName, string? positionTitle, int actorVoterId, string ipAddress, CancellationToken ct = default)
    {
        var account = await GetAdministrativeAccountAsync(accountId, ct);
        if (account == null) return false;

        var normalizedDocument = NormalizeRequired(document, "documento");
        var normalizedRole = NormalizeRole(roleName);
        var documentHash = _authService.HashDocument(normalizedDocument);
        if (await _context.Voters.AnyAsync(v => v.Id != account.Id && v.DocumentHash == documentHash, ct))
            throw new InvalidOperationException("Ya existe una cuenta con ese documento.");

        if (account.Role.Name == "SUPER_ADMIN" && normalizedRole != "SUPER_ADMIN" &&
            await CountActiveSuperAdminsAsync(ct) <= 1)
            throw new InvalidOperationException("No se puede retirar el último SUPER_ADMIN activo.");

        var oldRole = account.Role.Name;
        account.DocumentHash = documentHash;
        account.EncryptedDocument = _encryptionService.Encrypt(normalizedDocument);
        account.FullName = NormalizeRequired(fullName, "nombre");
        account.ContactEmail = NormalizeEmail(contactEmail);
        account.RoleId = await GetRoleIdAsync(normalizedRole, ct);
        account.PositionTitle = NormalizeOptional(positionTitle);
        await _context.SaveChangesAsync(ct);
        await _auditService.LogAsync("ADMIN_ACCOUNT_UPDATED", actorVoterId, "users", accountId,
            "role_id", oldRole, normalizedRole, null, ipAddress);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(int accountId, int actorVoterId, string ipAddress, CancellationToken ct = default)
    {
        var account = await GetAdministrativeAccountAsync(accountId, ct);
        if (account == null || account.Status == "ELIMINADO") return false;
        if (account.Id == (uint)actorVoterId)
            throw new InvalidOperationException("No puede eliminar su propia cuenta.");
        if (account.Role.Name == "SUPER_ADMIN" && await CountActiveSuperAdminsAsync(ct) <= 1)
            throw new InvalidOperationException("No se puede eliminar el último SUPER_ADMIN activo.");

        account.Status = "ELIMINADO";
        account.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        await _auditService.LogAsync("ADMIN_ACCOUNT_DELETED", actorVoterId, "users", accountId,
            "status", "ACTIVO", "ELIMINADO", null, ipAddress);
        return true;
    }

    public async Task<bool> RestoreAsync(int accountId, int actorVoterId, string ipAddress, CancellationToken ct = default)
    {
        var account = await GetAdministrativeAccountAsync(accountId, ct);
        if (account == null || account.Status != "ELIMINADO") return false;

        account.Status = "ACTIVO";
        account.DeletedAt = null;
        await _context.SaveChangesAsync(ct);
        await _auditService.LogAsync("ADMIN_ACCOUNT_RESTORED", actorVoterId, "users", accountId,
            "status", "ELIMINADO", "ACTIVO", null, ipAddress);
        return true;
    }

    private async Task<Voter?> GetAdministrativeAccountAsync(int accountId, CancellationToken ct) =>
        await _context.Voters.Include(v => v.Role).SingleOrDefaultAsync(v => v.Id == (uint)accountId &&
            (v.Role.Name == "ADMIN" || v.Role.Name == "SUPER_ADMIN"), ct);

    private async Task<byte> GetRoleIdAsync(string roleName, CancellationToken ct)
    {
        var role = await _context.Roles.SingleOrDefaultAsync(r => r.Name == roleName, ct)
            ?? throw new InvalidOperationException("El rol seleccionado no existe.");
        return role.Id;
    }

    private Task<int> CountActiveSuperAdminsAsync(CancellationToken ct) =>
        _context.Voters.Include(v => v.Role).CountAsync(v => v.Role.Name == "SUPER_ADMIN" && v.Status == "ACTIVO", ct);

    private string DecryptDocument(string value)
    {
        try { return _encryptionService.Decrypt(value); }
        catch { return "N/A"; }
    }

    private static string NormalizeRequired(string? value, string field)
    {
        var result = value?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(result)) throw new ArgumentException($"El {field} es obligatorio.");
        if (field == "nombre" && !System.Text.RegularExpressions.Regex.IsMatch(result, @"^[\p{L}]+(?:[ '\-][\p{L}]+)*$"))
            throw new ArgumentException("El nombre solo puede contener letras, espacios, guiones o apóstrofes.");
        if (field == "documento" && !result.All(char.IsDigit))
            throw new ArgumentException("El documento solo debe contener números.");
        return result;
    }

    private static string NormalizeEmail(string? value)
    {
        var result = NormalizeRequired(value, "correo");
        if (!new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(result))
            throw new ArgumentException("El correo no tiene un formato válido.");
        return result;
    }

    private static string NormalizeRole(string? value)
    {
        var result = NormalizeRequired(value, "rol").ToUpperInvariant();
        if (!AllowedRoles.Contains(result)) throw new ArgumentException("El rol debe ser ADMIN o SUPER_ADMIN.");
        return result;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}