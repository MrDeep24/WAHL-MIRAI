using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Cola de envío progresivo de correos de credenciales, con control de tasa (RN-9)
/// </summary>
public partial class EmailQueue
{
    public ulong Id { get; set; }

    public uint VoterId { get; set; }

    public string EmailType { get; set; } = null!;

    public string Status { get; set; } = null!;

    /// <summary>
    /// Número de intentos de envío realizados
    /// </summary>
    public byte Attempts { get; set; }

    /// <summary>
    /// Detalle del fallo; NULL si fue exitoso o aún no se procesa
    /// </summary>
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// NULL hasta que la cola lo procese exitosamente
    /// </summary>
    public DateTime? SentAt { get; set; }

    public virtual Voter Voter { get; set; } = null!;
}
