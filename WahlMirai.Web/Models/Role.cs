using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Catálogo de roles del sistema
/// </summary>
public partial class Role
{
    public byte Id { get; set; }

    /// <summary>
    /// Ej: ADMIN, ELECTOR
    /// </summary>
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Voter> Voters { get; set; } = new List<Voter>();
}
