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
    /// 1 = Voto en Blanco
    /// </summary>
    public bool IsBlankVote { get; set; }

    public string Status { get; set; } = null!;

    public DateTime EnrolledAt { get; set; }

    public virtual ICollection<CandidateProposal> CandidateProposals { get; set; } = new List<CandidateProposal>();

    public virtual Voter? Voter { get; set; }

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();

    public virtual VotingEvent VotingEvent { get; set; } = null!;
}
