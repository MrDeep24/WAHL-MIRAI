using System;

namespace WahlMirai.Web.Models;

/// <summary>
/// Mapea la vista <c>vw_pending_candidacies</c>.
/// Expone las candidaturas en estado PENDIENTE con el conteo pre-agregado
/// de requisitos y documentos obligatorios cargados.
/// </summary>
public partial class VwPendingCandidacy
{
    public uint CandidateId { get; set; }

    public uint VotingEventId { get; set; }

    public string EventTitle { get; set; } = null!;

    public uint PositionId { get; set; }

    public string PositionName { get; set; } = null!;

    public string CandidateName { get; set; } = null!;

    public DateTime EnrolledAt { get; set; }

    public long MandatoryRequirements { get; set; }

    public long MandatoryDocumentsUploaded { get; set; }
}
