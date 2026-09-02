using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Documentos de soporte cargados por el candidato para cumplir los requisitos (RF-M04-01, RN-11)
/// </summary>
public partial class CandidacyDocument
{
    public uint Id { get; set; }

    public uint CandidateId { get; set; }

    public uint RequirementId { get; set; }

    public string FileUrl { get; set; } = null!;

    public DateTime UploadedAt { get; set; }

    public virtual Candidate Candidate { get; set; } = null!;

    public virtual PositionRequirement Requirement { get; set; } = null!;
}
