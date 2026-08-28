using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Requisitos de elegibilidad exigidos por cada cargo electoral (RN-11)
/// </summary>
public partial class PositionRequirement
{
    public uint Id { get; set; }

    public uint PositionId { get; set; }

    /// <summary>
    /// Ej: Certificado de haber cursado y aprobado 10°
    /// </summary>
    public string Description { get; set; } = null!;

    public bool IsMandatory { get; set; } = true;

    /// <summary>
    /// Orden de aparición/evaluación del requisito
    /// </summary>
    public byte DisplayOrder { get; set; } = 1;

    public virtual ElectionPosition Position { get; set; } = null!;

    public virtual ICollection<CandidacyDocument> CandidacyDocuments { get; set; } = new List<CandidacyDocument>();
}
