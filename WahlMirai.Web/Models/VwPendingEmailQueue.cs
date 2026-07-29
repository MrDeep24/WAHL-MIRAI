using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

public partial class VwPendingEmailQueue
{
    public ulong Id { get; set; }

    public uint VoterId { get; set; }

    /// <summary>
    /// Correo de contacto (elector o acudiente). Solo credenciales/recuperación (RN-2.1), nunca login
    /// </summary>
    public string ContactEmail { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string EmailType { get; set; } = null!;

    /// <summary>
    /// Número de intentos de envío realizados
    /// </summary>
    public byte Attempts { get; set; }

    public DateTime CreatedAt { get; set; }
}
