using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Catálogo reutilizable de cargos electorales (RF-M03-00, RN-11)
/// </summary>
public partial class ElectionPosition
{
    public uint Id { get; set; }

    /// <summary>
    /// Ej: Personero, Contralor, Representante de Curso
    /// </summary>
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Status { get; set; } = "ACTIVO";

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<PositionRequirement> PositionRequirements { get; set; } = new List<PositionRequirement>();
}
