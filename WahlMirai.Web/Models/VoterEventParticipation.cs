using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Control anti-duplicado: elector ya ejerció su voto en la elección
/// </summary>
public partial class VoterEventParticipation
{
    public uint Id { get; set; }

    public uint VoterId { get; set; }

    public uint VotingEventId { get; set; }

    public DateTime ParticipatedAt { get; set; }

    public virtual Voter Voter { get; set; } = null!;

    public virtual VotingEvent VotingEvent { get; set; } = null!;
}
