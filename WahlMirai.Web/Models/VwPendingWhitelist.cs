using System;

namespace WahlMirai.Web.Models;

/// <summary>
/// Mapea la vista <c>vw_pending_whitelist</c>.
/// Representa las entradas de la lista blanca cargadas por la administración
/// que aún no han sido reclamadas por ningún estudiante mediante auto-registro (claimed_at IS NULL).
/// </summary>
public partial class VwPendingWhitelist
{
    public uint Id { get; set; }

    public string DocumentHash { get; set; } = null!;

    public string EncryptedDocument { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public byte GradeId { get; set; }

    public string? Grade { get; set; }

    public bool ExcluirDePromocion { get; set; }

    public DateTime CreatedAt { get; set; }
}
