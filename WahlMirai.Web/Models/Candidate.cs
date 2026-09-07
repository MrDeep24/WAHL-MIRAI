using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Candidatos postulados en cada elección, incluyendo el voto en blanco
/// </summary>
public partial class Candidate
{
    public uint Id { get; set; }

    public uint VotingEventId { get; set; }

    /// <summary>
    /// NULL si es voto en blanco
    /// </summary>
    public uint? VoterId { get; set; }

    /// <summary>
    /// Nombre visible en el tarjetón
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Lema de campaña
    /// </summary>
    public string? Slogan { get; set; }

    /// <summary>
    /// URL foto o avatar
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Plan de gobierno cargado por el candidato
    /// </summary>
    public string? GovernmentPlanUrl { get; set; }

    /// <summary>
    /// 1 = Voto en Blanco
    /// </summary>
    public bool IsBlankVote { get; set; }

    public string Status { get; set; } = null!;

    /// <summary>
    /// 1 = aprobado pese a requisitos documentales faltantes (RN-10.1)
    /// </summary>
    public bool ApprovedWithExceptions { get; set; }

    /// <summary>
    /// Detalle de qué requisitos quedaron pendientes al aprobar con excepción
    /// </summary>
    public string? ExceptionsDetail { get; set; }

    /// <summary>
    /// Motivo obligatorio si status = RECHAZADO (RN-10)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Si es true, el elector puede volver a inscribirse subsanando los requisitos
    /// </summary>
    public bool AllowCorrection { get; set; }

    public uint? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime EnrolledAt { get; set; }

    public virtual ICollection<CandidateProposal> CandidateProposals { get; set; } = new List<CandidateProposal>();

    public virtual ICollection<CandidacyDocument> CandidacyDocuments { get; set; } = new List<CandidacyDocument>();

    public virtual Voter? Voter { get; set; }

    public virtual Voter? ReviewedByVoter { get; set; }

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();

    public virtual VotingEvent VotingEvent { get; set; } = null!;
}
