using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Propuestas de campaña de cada candidato, mostradas antes de confirmar el voto
/// </summary>
public partial class CandidateProposal
{
    public uint Id { get; set; }

    public uint CandidateId { get; set; }

    /// <summary>
    /// Un punto de la propuesta
    /// </summary>
    public string Content { get; set; } = null!;

    /// <summary>
    /// Orden de aparición en la ventana emergente
    /// </summary>
    public byte DisplayOrder { get; set; }

    public virtual Candidate Candidate { get; set; } = null!;
}
