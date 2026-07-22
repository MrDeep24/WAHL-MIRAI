using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

public partial class VwVoteCount
{
    public uint EventId { get; set; }

    /// <summary>
    /// Nombre de la elección
    /// </summary>
    public string EventTitle { get; set; } = null!;

    public string EventStatus { get; set; } = null!;

    public uint CandidateId { get; set; }

    /// <summary>
    /// Nombre visible en el tarjetón
    /// </summary>
    public string CandidateName { get; set; } = null!;

    /// <summary>
    /// 1 = Voto en Blanco
    /// </summary>
    public bool IsBlankVote { get; set; }

    public long TotalVotes { get; set; }

    public decimal? VotePercentage { get; set; }
}
