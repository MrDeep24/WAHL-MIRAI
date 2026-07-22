using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Auditoría criptográfica de operaciones sensibles del sistema
/// </summary>
public partial class AuditLog
{
    public ulong Id { get; set; }

    /// <summary>
    /// NULL si fue el sistema (ej. promoción automática)
    /// </summary>
    public uint? VoterId { get; set; }

    /// <summary>
    /// LOGIN, VOTE_CAST, VOTER_CREATED, VOTER_UPDATED, VOTER_DELETED, VOTER_RESTORED, PROMOTION_RUN...
    /// </summary>
    public string Action { get; set; } = null!;

    /// <summary>
    /// Tabla/entidad afectada
    /// </summary>
    public string? TargetEntity { get; set; }

    /// <summary>
    /// ID del registro afectado
    /// </summary>
    public int? TargetId { get; set; }

    /// <summary>
    /// Campo modificado; NULL si no aplica
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Valor anterior del campo
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Valor nuevo del campo
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Contexto adicional en JSON (ej. resumen de promoción masiva)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// IP IPv4/IPv6 del cliente
    /// </summary>
    public string? IpAddress { get; set; }

    public DateTime OccurredAt { get; set; }

    public virtual Voter? Voter { get; set; }
}
