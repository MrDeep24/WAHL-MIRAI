using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Procesos electorales configurados por el administrador
/// </summary>
public partial class VotingEvent
{
    public uint Id { get; set; }

    /// <summary>
    /// Administrador creador
    /// </summary>
    public uint CreatedByVoterId { get; set; }

    /// <summary>
    /// Nombre de la elección
    /// </summary>
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// RF-M03-01
    /// </summary>
    public string ElectionType { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public DateOnly EndDate { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();

    public virtual Voter CreatedByVoter { get; set; } = null!;

    public virtual ICollection<EventGrade> EventGrades { get; set; } = new List<EventGrade>();

    public virtual ICollection<VoterEventParticipation> VoterEventParticipations { get; set; } = new List<VoterEventParticipation>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
