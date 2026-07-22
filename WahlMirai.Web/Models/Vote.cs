using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Votos emitidos — inmutables y sin referencia directa al elector
/// </summary>
public partial class Vote
{
    public ulong Id { get; set; }

    public uint VotingEventId { get; set; }

    public uint CandidateId { get; set; }

    public DateTime VotedAt { get; set; }

    /// <summary>
    /// SHA-256 para integridad criptográfica
    /// </summary>
    public string VoteHash { get; set; } = null!;

    public virtual Candidate Candidate { get; set; } = null!;

    public virtual VotingEvent VotingEvent { get; set; } = null!;
}
