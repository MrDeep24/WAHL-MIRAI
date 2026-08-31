using System;

namespace WahlMirai.Web.Models;

// ============================================================
// M01-00 / M02-00 CROSS-TEAM CONTRACT — READ BEFORE MODIFYING
// ============================================================
//
// This entity is the minimal EF Core mapping of the `census_whitelist` table
// required by the self-registration flow (RF-M01-00, RN-1, RN-1.1).
//
// FIELDS USED BY M01-00 (self-registration — this file):
//   - DocumentHash       : queried with WHERE document_hash = ? AND claimed_at IS NULL
//                          to verify the student is authorized to register.
//   - FullName           : pre-filled (read-only) on the registration form;
//                          inherited verbatim into the new `users` row.
//   - GradeId / Grade    : same as above; the new user inherits this grade.
//   - ClaimedAt          : set to UTC NOW() when the student completes registration (RN-1.1).
//   - ClaimedByUserId    : set to the newly created users.id (RN-1.1).
//
// FIELDS WRITTEN BY M02-00 (whitelist management — Dev 2's scope):
//   - EncryptedDocument  : stored when an Administrator loads the whitelist entry.
//   - UploadedByUserId   : FK to the Admin who created the entry.
//   - ExcluirDePromocion : managed by M02-00 / M02-02 (year promotion logic).
//   - CreatedAt          : DB default, set at INSERT time.
//
// OUT OF SCOPE FOR M01-00:
// Whitelist upload (bulk CSV / individual entry), editing existing entries, and the
// annual promotion flow all belong to M02-00 (Dev 2).  Do NOT add any of that logic
// here.  The ONLY writes M01-00 performs on this table are:
//   UPDATE census_whitelist
//      SET claimed_at = UTC_NOW(), claimed_by_user_id = <new_user_id>
//    WHERE id = <entry_id>
// executed as part of the atomic SELF_REGISTER transaction.
// ============================================================

/// <summary>
/// Maps the <c>census_whitelist</c> table.
/// Represents an Administrator-uploaded census entry that authorizes a student
/// to complete their own self-registration (RF-M01-00, RN-1, RN-1.1).
/// By itself this is NOT a system account — it has no email or password.
/// </summary>
public partial class CensusWhitelist
{
    public uint Id { get; set; }

    /// <summary>
    /// SHA-256 deterministic hash of the document number.
    /// Used by M01-00 to look up unclaimed whitelist entries during self-registration.
    /// </summary>
    public string DocumentHash { get; set; } = null!;

    /// <summary>
    /// AES-256 encrypted document (Data Protection API, purpose "WahlMirai.DocumentEncryption.v1").
    /// Written by M02-00 when the Administrator loads the whitelist entry.
    /// </summary>
    public string EncryptedDocument { get; set; } = null!;

    /// <summary>
    /// Student's full name. Pre-filled (read-only) in the self-registration form
    /// and inherited verbatim into the new <c>users</c> row.
    /// </summary>
    public string FullName { get; set; } = null!;

    /// <summary>FK to <c>grades.id</c>. Inherited by the new <c>users</c> row.</summary>
    public byte GradeId { get; set; }

    /// <summary>
    /// 1 = retained student; excluded from automatic year promotion (M02-02).
    /// Managed exclusively by M02-00 / M02-02.
    /// </summary>
    public bool ExcluirDePromocion { get; set; }

    /// <summary>
    /// NULL = not yet claimed.
    /// Set to UTC NOW() when the student completes self-registration (RN-1.1).
    /// Written by M01-00 as part of the atomic SELF_REGISTER transaction.
    /// </summary>
    public DateTime? ClaimedAt { get; set; }

    /// <summary>
    /// FK to <c>users.id</c> of the student who claimed this entry (RN-1.1).
    /// Written by M01-00 alongside <see cref="ClaimedAt"/>.
    /// </summary>
    public uint? ClaimedByUserId { get; set; }

    /// <summary>
    /// FK to <c>users.id</c> of the Administrator who uploaded this entry.
    /// Managed exclusively by M02-00. Never written by M01-00.
    /// </summary>
    public uint UploadedByUserId { get; set; }

    /// <summary>DB default timestamp; set at INSERT by M02-00.</summary>
    public DateTime CreatedAt { get; set; }

    // ── Navigation properties ────────────────────────────────────────────────────

    /// <summary>Grade catalogue entry. Loaded by M01-00 to pre-fill the registration form.</summary>
    public virtual Grade Grade { get; set; } = null!;

    /// <summary>The student who claimed this entry. NULL until registration is completed.</summary>
    public virtual Voter? ClaimedByVoter { get; set; }

    /// <summary>The Administrator who uploaded this entry. Managed by M02-00.</summary>
    public virtual Voter UploadedByVoter { get; set; } = null!;
}
