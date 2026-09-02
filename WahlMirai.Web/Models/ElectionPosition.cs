using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Catálogo reutilizable de cargos electorales (RF-M03-00)
/// </summary>
public partial class ElectionPosition
{
    public uint Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<VotingEvent> VotingEvents { get; set; } = new List<VotingEvent>();

    public virtual ICollection<PositionRequirement> PositionRequirements { get; set; } = new List<PositionRequirement>();
}
