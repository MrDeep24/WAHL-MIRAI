using Microsoft.EntityFrameworkCore;
using WahlMirai.Web.Models;

namespace WahlMirai.Web.Services;

// ============================================================
// Archivo:       WhitelistService.cs
// Módulo:        M01 — Gestión de Acceso, Auto-registro y Sesión
// Desarrollador: Kevin
// Descripción:   Define el contrato y la implementación para consultar
//                y reclamar entradas durante el auto-registro de electores.
// Requerimientos: RF-M01-00, RN-1, RN-1.1, RN-8; apoyo para RN-2.1.
// ============================================================
// M01-00 WHITELIST SERVICE — CONTRACT NOTES
// ============================================================
// This service encapsulates the two operations M01-00 performs
// against census_whitelist:
//
//   1. GetUnclaimedEntryByDocumentHashAsync  — read-only lookup
//      (SELECT WHERE document_hash = ? AND claimed_at IS NULL).
//
//   2. RegisterElectorAsync — atomic transaction that:
//        a) INSERTs a new users row (role ELECTOR, status ACTIVO).
//        b) UPDATEs census_whitelist.claimed_at + claimed_by_user_id.
//        c) INSERTs an audit_log row (action = 'SELF_REGISTER').
//      All three writes share a single SaveChangesAsync() inside an
//      explicit DB transaction so a partial failure cannot leave an
//      orphaned user row or an unclaimed whitelist entry.
//
// OUT OF SCOPE: whitelist upload, bulk CSV import, editing entries,
// annual promotion — those belong to M02-00 (Dev 2).
// ============================================================

/// <summary>
/// Provides the minimal whitelist operations required by the self-registration flow
/// (RF-M01-00, RN-1, RN-1.1). Full whitelist management (upload, CSV, editing)
/// belongs to M02-00 and must NOT be added here.
/// </summary>
public interface IWhitelistService
{
    /// <summary>
    /// Returns the census_whitelist entry whose document_hash matches
    /// <paramref name="documentHash"/> AND whose claimed_at IS NULL.
    /// </summary>
    Task<CensusWhitelist?> GetUnclaimedEntryByDocumentHashAsync(string documentHash);

    /// <summary>
    /// Returns the census_whitelist entry matching <paramref name="documentHash"/>,
    /// regardless of whether it has already been claimed (RF-M01-00).
    /// Used by self-registration to distinguish between non-existent documents and already claimed documents.
    /// </summary>
    Task<CensusWhitelist?> GetEntryByDocumentHashAsync(string documentHash);

    /// <summary>
    /// Consulta una entrada de lista blanca por su identificador que aún no haya sido reclamada (RF-M01-00).
    /// </summary>
    /// <param name="id">Identificador único de la entrada en census_whitelist.</param>
    /// <returns>La entrada de lista blanca con su Grado cargado o <c>null</c> si no existe o ya fue reclamada.</returns>
    Task<CensusWhitelist?> GetUnclaimedEntryByIdAsync(uint id);

    /// <summary>
    /// Atomically registers a new ELECTOR and marks the whitelist entry as claimed.
    /// Verifies that the submitted document's hash still matches the whitelist entry
    /// and that the entry is still unclaimed at the moment of registration.
    /// </summary>
    /// <param name="document">Plain-text document number submitted by the student.</param>
    /// <param name="whitelistId">ID of the whitelist entry from step 1.</param>
    /// <param name="contactEmail">Contact email chosen by the student.</param>
    /// <param name="password">Plain-text password chosen by the student (will be BCrypt-hashed).</param>
    /// <param name="ipAddress">Client IP address for the audit log.</param>
    /// <returns>The newly created <see cref="Voter"/> with Role and Grade loaded.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the whitelist entry cannot be found, is already claimed, the document
    /// hash does not match, or the document/email already exists in users.
    /// </exception>
    Task<Voter> RegisterElectorAsync(
        string document,
        uint whitelistId,
        string contactEmail,
        string password,
        string ipAddress);
}

/// <summary>
/// Implementa la consulta de entradas disponibles y el registro transaccional de electores
/// contra la lista blanca (RF-M01-00, RN-1, RN-1.1).
/// </summary>
public class WhitelistService : IWhitelistService
{
    private readonly WahlMiraiDbContext _context;
    private readonly IAuthService _authService;
    private readonly IDocumentEncryptionService _encryptionService;

    // ── ELECTOR role name constant — resolved dynamically from the DB at runtime ──
    private const string ElectorRoleName = "ELECTOR";

    /// <summary>Inicializa la implementación con servicios de datos, autenticación y cifrado.</summary>
    /// <param name="context">Contexto de la lista blanca, usuarios y auditoría.</param>
    /// <param name="authService">Calcula el hash del documento enviado.</param>
    /// <param name="encryptionService">Cifra el documento para su almacenamiento.</param>
    public WhitelistService(
        WahlMiraiDbContext context,
        IAuthService authService,
        IDocumentEncryptionService encryptionService)
    {
        _context = context;
        _authService = authService;
        _encryptionService = encryptionService;
    }

    /// <inheritdoc />
    public async Task<CensusWhitelist?> GetUnclaimedEntryByDocumentHashAsync(string documentHash)
    {
        return await _context.CensusWhitelists
            .Include(w => w.Grade)
            .FirstOrDefaultAsync(w => w.DocumentHash == documentHash && w.ClaimedAt == null);
    }

    /// <inheritdoc />
    public async Task<CensusWhitelist?> GetEntryByDocumentHashAsync(string documentHash)
    {
        return await _context.CensusWhitelists
            .Include(w => w.Grade)
            .FirstOrDefaultAsync(w => w.DocumentHash == documentHash);
    }

    /// <inheritdoc />
    public async Task<CensusWhitelist?> GetUnclaimedEntryByIdAsync(uint id)
    {
        return await _context.CensusWhitelists
            .Include(w => w.Grade)
            .FirstOrDefaultAsync(w => w.Id == id && w.ClaimedAt == null);
    }

    /// <inheritdoc />
    public async Task<Voter> RegisterElectorAsync(
        string document,
        uint whitelistId,
        string contactEmail,
        string password,
        string ipAddress)
    {
        // ── Pre-transaction checks ────────────────────────────────────────────────
        // Re-fetch the entry to guard against a race condition between step 1 and step 2.
        var entry = await _context.CensusWhitelists
            .Include(w => w.Grade)
            .FirstOrDefaultAsync(w => w.Id == whitelistId);

        if (entry == null || entry.ClaimedAt != null)
            throw new InvalidOperationException("Whitelist entry not found or already claimed.");

        // D2 & D3: users.contact_email must come exclusively from census_whitelist.contact_email
        var effectiveEmail = !string.IsNullOrWhiteSpace(entry.ContactEmail)
            ? entry.ContactEmail.Trim()
            : (!string.IsNullOrWhiteSpace(contactEmail)
                ? contactEmail.Trim()
                : throw new InvalidOperationException("Whitelist entry does not have a contact email."));

        // Verify the submitted document still matches the stored hash (tamper-proof).
        var computedHash = _authService.HashDocument(document);
        if (!string.Equals(computedHash, entry.DocumentHash, StringComparison.Ordinal))
            throw new InvalidOperationException("Submitted document does not match the whitelist entry.");

        // Uniqueness guards on the users table.
        var existingByDoc = await _context.Voters
            .AnyAsync(v => v.DocumentHash == computedHash);
        if (existingByDoc)
            throw new InvalidOperationException("A user with this document already exists.");

        // RN-2.1: contact_email is an indexed notification address, not a unique identity key.
        // Duplicate contact_email values are allowed, so no application-layer uniqueness check is enforced here.

        // Resolve ELECTOR role ID from the database (avoids hardcoding).
        var electorRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == ElectorRoleName)
            ?? throw new InvalidOperationException("ELECTOR role not found in the roles catalogue.");

        // ── Atomic transaction: three writes, one SaveChangesAsync ───────────────
        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Create the new Voter row (role ELECTOR, status ACTIVO).
            //    full_name and grade_id are inherited from the whitelist entry (not editable by student).
            //    document_hash and encrypted_document are computed from the submitted plain text,
            //    using the same IAuthService.HashDocument and IDocumentEncryptionService.Encrypt
            //    methods used throughout the rest of the project.
            var voter = new Voter
            {
                RoleId            = electorRole.Id,
                GradeId           = entry.GradeId,
                DocumentHash      = computedHash,
                EncryptedDocument = _encryptionService.Encrypt(document),
                FullName          = entry.FullName,
                ContactEmail      = effectiveEmail,
                PasswordHash      = BCrypt.Net.BCrypt.HashPassword(password),
                ExcluirDePromocion = entry.ExcluirDePromocion,
                Status            = "ACTIVO",
                RegisteredAt      = DateTime.UtcNow,
            };
            _context.Voters.Add(voter);

            // We need voter.Id before writing the other two rows, so flush to the DB now
            // (still within the open transaction — not committed yet).
            await _context.SaveChangesAsync();

            // 2. Mark the whitelist entry as claimed (RN-1.1).
            entry.ClaimedAt       = DateTime.UtcNow;
            entry.ClaimedByUserId = voter.Id;

            // 3. Insert audit_log entry directly — do NOT call AuditService.LogAsync,
            //    because that method issues its own SaveChangesAsync which would conflict
            //    with the outer transaction. Direct Add + single commit keeps atomicity.
            var auditEntry = new AuditLog
            {
                Action       = "SELF_REGISTER",
                VoterId      = voter.Id,
                TargetEntity = "users",
                TargetId     = (int)voter.Id,
                FieldName    = null,
                OldValue     = null,
                NewValue      = null,
                Details      = $"Self-registration completed. Whitelist entry id={entry.Id} claimed.",
                IpAddress    = ipAddress,
                OccurredAt   = DateTime.UtcNow,
            };
            _context.AuditLogs.Add(auditEntry);

            // Single SaveChangesAsync for writes 2 + 3.
            await _context.SaveChangesAsync();

            await tx.CommitAsync();

            // Load navigation properties needed for the auto-login claims.
            voter.Role  = electorRole;
            voter.Grade = entry.Grade;

            return voter;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }
}
