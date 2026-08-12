using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

public partial class PqrTicket
{
    public ulong Id { get; set; }

    public uint VoterId { get; set; }

    public string Subject { get; set; } = null!;

    public string Message { get; set; } = null!;

    public string Status { get; set; } = null!; // 'ABIERTO' | 'RESUELTO'

    public string? AdminResponse { get; set; }

    public uint? RespondedByVoterId { get; set; }

    public DateTime? RespondedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Voter Voter { get; set; } = null!;
    
    public virtual Voter? RespondedBy { get; set; }
}
