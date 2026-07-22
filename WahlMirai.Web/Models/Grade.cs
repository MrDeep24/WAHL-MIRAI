using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Catálogo de grados escolares en orden, base de la promoción automática
/// </summary>
public partial class Grade
{
    public byte Id { get; set; }

    /// <summary>
    /// Ej: 6°, 7°, ..., 11°
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Orden para calcular el siguiente grado al promover
    /// </summary>
    public byte SequenceOrder { get; set; }

    /// <summary>
    /// 1 = al promover, el elector pasa a EGRESADO
    /// </summary>
    public bool IsLastGrade { get; set; }

    public virtual ICollection<EventGrade> EventGrades { get; set; } = new List<EventGrade>();

    public virtual ICollection<Voter> Voters { get; set; } = new List<Voter>();
}
