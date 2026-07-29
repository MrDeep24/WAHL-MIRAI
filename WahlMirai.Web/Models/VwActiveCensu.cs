using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

public partial class VwActiveCensu
{
    public uint Id { get; set; }

    public string FullName { get; set; } = null!;

    /// <summary>
    /// Correo de contacto (elector o acudiente). Solo credenciales/recuperación (RN-2.1), nunca login
    /// </summary>
    public string ContactEmail { get; set; } = null!;

    /// <summary>
    /// Ej: 6°, 7°, ..., 11°
    /// </summary>
    public string? Grade { get; set; }

    public string Status { get; set; } = null!;

    /// <summary>
    /// 1 = repitente, se omite en la promoción masiva
    /// </summary>
    public bool ExcluirDePromocion { get; set; }

    public DateTime RegisteredAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
