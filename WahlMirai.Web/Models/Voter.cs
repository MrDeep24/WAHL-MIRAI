using System;
using System.Collections.Generic;

namespace WahlMirai.Web.Models;

/// <summary>
/// Censo persistente de usuarios: electores y administradores
/// </summary>
public partial class Voter
{
    public uint Id { get; set; }

    public byte RoleId { get; set; }

    /// <summary>
    /// NULL para administradores
    /// </summary>
    public byte? GradeId { get; set; }

    /// <summary>
    /// SHA-256 determinístico del documento, usado para login/búsqueda
    /// </summary>
    public string DocumentHash { get; set; } = null!;

    /// <summary>
    /// Documento cifrado AES-256 para mostrar/editar en UI
    /// </summary>
    public string EncryptedDocument { get; set; } = null!;

    public string FullName { get; set; } = null!;

    /// <summary>
    /// Hash BCrypt
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// 1 = debe cambiar clave autogenerada en próximo login (RF-M01-02)
    /// </summary>
    public bool? RequiereCambioClave { get; set; }

    /// <summary>
    /// 1 = repitente, se omite en la promoción masiva
    /// </summary>
    public bool ExcluirDePromocion { get; set; }

    public string Status { get; set; } = null!;

    /// <summary>
    /// Fecha de eliminación lógica; NULL si no aplica
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public DateTime RegisteredAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();

    public virtual Grade? Grade { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<VoterEventParticipation> VoterEventParticipations { get; set; } = new List<VoterEventParticipation>();

    public virtual ICollection<VotingEvent> VotingEvents { get; set; } = new List<VotingEvent>();
}
