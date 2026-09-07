using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Requisitos de elegibilidad exigidos por cada cargo o proceso electoral (RN-11)
/// </summary>
public partial class PositionRequirement
{
    public uint Id { get; set; }

    public uint PositionId { get; set; }

    public string Description { get; set; } = null!;

    public bool IsMandatory { get; set; }

    public byte DisplayOrder { get; set; }

    public virtual ElectionPosition Position { get; set; } = null!;

    public virtual ICollection<CandidacyDocument> CandidacyDocuments { get; set; } = new List<CandidacyDocument>();
}
