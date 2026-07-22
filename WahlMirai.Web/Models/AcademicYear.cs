using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Año lectivo vigente; controla generación de clave inicial y bloqueo de doble promoción
/// </summary>
public partial class AcademicYear
{
    public ushort Id { get; set; }

    /// <summary>
    /// Ej: 2026
    /// </summary>
    public ushort Year { get; set; }

    /// <summary>
    /// 1 = año lectivo activo, solo uno a la vez
    /// </summary>
    public bool IsCurrent { get; set; }

    /// <summary>
    /// NULL = aún no se corre la promoción este año
    /// </summary>
    public DateTime? PromotionExecutedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
