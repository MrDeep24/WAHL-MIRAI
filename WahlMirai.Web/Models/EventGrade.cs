using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Grados escolares habilitados para participar en cada elección
/// </summary>
public partial class EventGrade
{
    public uint Id { get; set; }

    public uint VotingEventId { get; set; }

    public byte GradeId { get; set; }

    public virtual Grade Grade { get; set; } = null!;

    public virtual VotingEvent VotingEvent { get; set; } = null!;
}
