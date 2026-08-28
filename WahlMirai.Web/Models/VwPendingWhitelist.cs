using System;

namespace WahlMirai.Web.Models;

/// <summary>
/// Entradas de la lista blanca aún no reclamadas (vw_pending_whitelist)
/// </summary>
public partial class VwPendingWhitelist
{
    public uint Id { get; set; }

    public string FullName { get; set; } = null!;

    public string? Grade { get; set; }

    public DateTime CreatedAt { get; set; }
}
