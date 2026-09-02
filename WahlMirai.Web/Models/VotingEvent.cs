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
    /// Cargo electoral asociado (RF-M03-00)
    /// </summary>
    public uint PositionId { get; set; }

    /// <summary>
    /// Nombre de la elección
    /// </summary>
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>
    /// RF-M03-01
    /// </summary>
    public string ElectionType { get; set; } = null!;

    // Etapa 1: Inscripción de Candidatos
    public DateOnly RegistrationStartDate { get; set; }
    public TimeOnly RegistrationStartTime { get; set; }
    public DateOnly RegistrationEndDate { get; set; }
    public TimeOnly RegistrationEndTime { get; set; }

    // Etapa 2: Consulta de Propuestas
    public DateOnly ProposalsStartDate { get; set; }
    public TimeOnly ProposalsStartTime { get; set; }
    public DateOnly ProposalsEndDate { get; set; }
    public TimeOnly ProposalsEndTime { get; set; }

    // Etapa 3: Votación
    public DateOnly VotingStartDate { get; set; }
    public TimeOnly VotingStartTime { get; set; }
    public DateOnly VotingEndDate { get; set; }
    public TimeOnly VotingEndTime { get; set; }

    /// <summary>
    /// ELIMINADO = soft-delete (RN-7.1); el proceso deja de ser visible/operable pero sus votos son inmutables
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// Fecha de eliminación lógica; NULL si no aplica (mismo patrón que voters.deleted_at)
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();

    public virtual Voter CreatedByVoter { get; set; } = null!;

    public virtual ElectionPosition Position { get; set; } = null!;

    public virtual ICollection<EventGrade> EventGrades { get; set; } = new List<EventGrade>();

    public virtual ICollection<VoterEventParticipation> VoterEventParticipations { get; set; } = new List<VoterEventParticipation>();

    public virtual ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
