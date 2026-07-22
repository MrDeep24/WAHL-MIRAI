using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace WahlMirai.Web.Models;

public partial class WahlMiraiDbContext : DbContext
{
    public WahlMiraiDbContext()
    {
    }

    public WahlMiraiDbContext(DbContextOptions<WahlMiraiDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AcademicYear> AcademicYears { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<Candidate> Candidates { get; set; }

    public virtual DbSet<CandidateProposal> CandidateProposals { get; set; }

    public virtual DbSet<EventGrade> EventGrades { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Vote> Votes { get; set; }

    public virtual DbSet<Voter> Voters { get; set; }

    public virtual DbSet<VoterEventParticipation> VoterEventParticipations { get; set; }

    public virtual DbSet<VotingEvent> VotingEvents { get; set; }

    public virtual DbSet<VwActiveCensu> VwActiveCensus { get; set; }

    public virtual DbSet<VwVoteCount> VwVoteCounts { get; set; }



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_unicode_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<AcademicYear>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("academic_years", tb => tb.HasComment("Año lectivo vigente; controla generación de clave inicial y bloqueo de doble promoción"));

            entity.HasIndex(e => e.Year, "uq_academic_years_year").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("smallint(5) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsCurrent)
                .HasComment("1 = año lectivo activo, solo uno a la vez")
                .HasColumnName("is_current");
            entity.Property(e => e.PromotionExecutedAt)
                .HasComment("NULL = aún no se corre la promoción este año")
                .HasColumnType("datetime")
                .HasColumnName("promotion_executed_at");
            entity.Property(e => e.Year)
                .HasComment("Ej: 2026")
                .HasColumnType("smallint(5) unsigned")
                .HasColumnName("year");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("audit_log", tb => tb.HasComment("Auditoría criptográfica de operaciones sensibles del sistema"));

            entity.HasIndex(e => e.Action, "idx_al_action");

            entity.HasIndex(e => e.OccurredAt, "idx_al_occurred_at");

            entity.HasIndex(e => e.VoterId, "idx_al_voter_id");

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .HasComment("LOGIN, VOTE_CAST, VOTER_CREATED, VOTER_UPDATED, VOTER_DELETED, VOTER_RESTORED, PROMOTION_RUN...")
                .HasColumnName("action");
            entity.Property(e => e.Details)
                .HasComment("Contexto adicional en JSON (ej. resumen de promoción masiva)")
                .HasColumnType("text")
                .HasColumnName("details");
            entity.Property(e => e.FieldName)
                .HasMaxLength(100)
                .HasComment("Campo modificado; NULL si no aplica")
                .HasColumnName("field_name");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasComment("IP IPv4/IPv6 del cliente")
                .HasColumnName("ip_address");
            entity.Property(e => e.NewValue)
                .HasComment("Valor nuevo del campo")
                .HasColumnType("text")
                .HasColumnName("new_value");
            entity.Property(e => e.OccurredAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("occurred_at");
            entity.Property(e => e.OldValue)
                .HasComment("Valor anterior del campo")
                .HasColumnType("text")
                .HasColumnName("old_value");
            entity.Property(e => e.TargetEntity)
                .HasMaxLength(200)
                .HasComment("Tabla/entidad afectada")
                .HasColumnName("target_entity");
            entity.Property(e => e.TargetId)
                .HasComment("ID del registro afectado")
                .HasColumnType("int(11)")
                .HasColumnName("target_id");
            entity.Property(e => e.VoterId)
                .HasComment("NULL si fue el sistema (ej. promoción automática)")
                .HasColumnType("int(10) unsigned")
                .HasColumnName("voter_id");

            entity.HasOne(d => d.Voter).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.VoterId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_al_voter");
        });

        modelBuilder.Entity<Candidate>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("candidates", tb => tb.HasComment("Candidatos postulados en cada elección, incluyendo el voto en blanco"));

            entity.HasIndex(e => e.IsBlankVote, "idx_cand_is_blank");

            entity.HasIndex(e => e.Status, "idx_cand_status");

            entity.HasIndex(e => e.VotingEventId, "idx_cand_voting_event_id");

            entity.HasIndex(e => new { e.VoterId, e.VotingEventId }, "uq_cand_voter_event").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.EnrolledAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("enrolled_at");
            entity.Property(e => e.IsBlankVote)
                .HasComment("1 = Voto en Blanco")
                .HasColumnName("is_blank_vote");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasComment("Nombre visible en el tarjetón")
                .HasColumnName("name");
            entity.Property(e => e.PhotoUrl)
                .HasMaxLength(500)
                .HasComment("URL foto o avatar")
                .HasColumnName("photo_url");
            entity.Property(e => e.Slogan)
                .HasComment("Lema de campaña")
                .HasColumnType("text")
                .HasColumnName("slogan");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'PENDIENTE'")
                .HasColumnType("enum('PENDIENTE','APROBADO','RECHAZADO')")
                .HasColumnName("status");
            entity.Property(e => e.VoterId)
                .HasComment("NULL si es voto en blanco")
                .HasColumnType("int(10) unsigned")
                .HasColumnName("voter_id");
            entity.Property(e => e.VotingEventId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("voting_event_id");

            entity.HasOne(d => d.Voter).WithMany(p => p.Candidates)
                .HasForeignKey(d => d.VoterId)
                .HasConstraintName("fk_cand_voter");

            entity.HasOne(d => d.VotingEvent).WithMany(p => p.Candidates)
                .HasForeignKey(d => d.VotingEventId)
                .HasConstraintName("fk_cand_voting_event");
        });

        modelBuilder.Entity<CandidateProposal>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("candidate_proposals", tb => tb.HasComment("Propuestas de campaña de cada candidato, mostradas antes de confirmar el voto"));

            entity.HasIndex(e => new { e.CandidateId, e.DisplayOrder }, "uq_cp_candidate_order").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CandidateId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("candidate_id");
            entity.Property(e => e.Content)
                .HasComment("Un punto de la propuesta")
                .HasColumnType("text")
                .HasColumnName("content");
            entity.Property(e => e.DisplayOrder)
                .HasDefaultValueSql("'1'")
                .HasComment("Orden de aparición en la ventana emergente")
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("display_order");

            entity.HasOne(d => d.Candidate).WithMany(p => p.CandidateProposals)
                .HasForeignKey(d => d.CandidateId)
                .HasConstraintName("fk_cp_candidate");
        });

        modelBuilder.Entity<EventGrade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("event_grades", tb => tb.HasComment("Grados escolares habilitados para participar en cada elección"));

            entity.HasIndex(e => e.GradeId, "idx_eg_grade_id");

            entity.HasIndex(e => new { e.VotingEventId, e.GradeId }, "uq_eg_event_grade").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.GradeId)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("grade_id");
            entity.Property(e => e.VotingEventId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("voting_event_id");

            entity.HasOne(d => d.Grade).WithMany(p => p.EventGrades)
                .HasForeignKey(d => d.GradeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_eg_grade");

            entity.HasOne(d => d.VotingEvent).WithMany(p => p.EventGrades)
                .HasForeignKey(d => d.VotingEventId)
                .HasConstraintName("fk_eg_voting_event");
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("grades", tb => tb.HasComment("Catálogo de grados escolares en orden, base de la promoción automática"));

            entity.HasIndex(e => e.Name, "uq_grades_name").IsUnique();

            entity.HasIndex(e => e.SequenceOrder, "uq_grades_sequence_order").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.IsLastGrade)
                .HasComment("1 = al promover, el elector pasa a EGRESADO")
                .HasColumnName("is_last_grade");
            entity.Property(e => e.Name)
                .HasMaxLength(10)
                .HasComment("Ej: 6°, 7°, ..., 11°")
                .HasColumnName("name");
            entity.Property(e => e.SequenceOrder)
                .HasComment("Orden para calcular el siguiente grado al promover")
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("sequence_order");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("roles", tb => tb.HasComment("Catálogo de roles del sistema"));

            entity.HasIndex(e => e.Name, "uq_roles_name").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(100)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasComment("Ej: ADMIN, ELECTOR")
                .HasColumnName("name");
        });

        modelBuilder.Entity<Vote>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("votes", tb => tb.HasComment("Votos emitidos — inmutables y sin referencia directa al elector"));

            entity.HasIndex(e => e.CandidateId, "idx_votes_candidate_id");

            entity.HasIndex(e => e.VotingEventId, "idx_votes_event_id");

            entity.HasIndex(e => e.VotedAt, "idx_votes_voted_at");

            entity.HasIndex(e => e.VoteHash, "uq_votes_hash").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("bigint(20) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CandidateId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("candidate_id");
            entity.Property(e => e.VoteHash)
                .HasMaxLength(64)
                .HasComment("SHA-256 para integridad criptográfica")
                .HasColumnName("vote_hash");
            entity.Property(e => e.VotedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("voted_at");
            entity.Property(e => e.VotingEventId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("voting_event_id");

            entity.HasOne(d => d.Candidate).WithMany(p => p.Votes)
                .HasForeignKey(d => d.CandidateId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_votes_candidate");

            entity.HasOne(d => d.VotingEvent).WithMany(p => p.Votes)
                .HasForeignKey(d => d.VotingEventId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_votes_voting_event");
        });

        modelBuilder.Entity<Voter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("voters", tb => tb.HasComment("Censo persistente de usuarios: electores y administradores"));

            entity.HasIndex(e => e.GradeId, "idx_voters_grade_id");

            entity.HasIndex(e => e.RoleId, "idx_voters_role_id");

            entity.HasIndex(e => e.Status, "idx_voters_status");

            entity.HasIndex(e => e.DocumentHash, "uq_voters_document_hash").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.DeletedAt)
                .HasComment("Fecha de eliminación lógica; NULL si no aplica")
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.DocumentHash)
                .HasMaxLength(64)
                .IsFixedLength()
                .HasComment("SHA-256 determinístico del documento, usado para login/búsqueda")
                .HasColumnName("document_hash");
            entity.Property(e => e.EncryptedDocument)
                .HasMaxLength(500)
                .HasComment("Documento cifrado AES-256 para mostrar/editar en UI")
                .HasColumnName("encrypted_document");
            entity.Property(e => e.ExcluirDePromocion)
                .HasComment("1 = repitente, se omite en la promoción masiva")
                .HasColumnName("excluir_de_promocion");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.GradeId)
                .HasComment("NULL para administradores")
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("grade_id");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasComment("Hash BCrypt")
                .HasColumnName("password_hash");
            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("registered_at");
            entity.Property(e => e.RequiereCambioClave)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("1 = debe cambiar clave autogenerada en próximo login (RF-M01-02)")
                .HasColumnName("requiere_cambio_clave");
            entity.Property(e => e.RoleId)
                .HasColumnType("tinyint(3) unsigned")
                .HasColumnName("role_id");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'ACTIVO'")
                .HasColumnType("enum('ACTIVO','INACTIVO','ELIMINADO','EGRESADO')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.Grade).WithMany(p => p.Voters)
                .HasForeignKey(d => d.GradeId)
                .HasConstraintName("fk_voters_grade");

            entity.HasOne(d => d.Role).WithMany(p => p.Voters)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_voters_role");
        });

        modelBuilder.Entity<VoterEventParticipation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("voter_event_participations", tb => tb.HasComment("Control anti-duplicado: elector ya ejerció su voto en la elección"));

            entity.HasIndex(e => e.VotingEventId, "idx_vep_voting_event_id");

            entity.HasIndex(e => new { e.VoterId, e.VotingEventId }, "uq_vep_voter_event").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.ParticipatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("participated_at");
            entity.Property(e => e.VoterId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("voter_id");
            entity.Property(e => e.VotingEventId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("voting_event_id");

            entity.HasOne(d => d.Voter).WithMany(p => p.VoterEventParticipations)
                .HasForeignKey(d => d.VoterId)
                .HasConstraintName("fk_vep_voter");

            entity.HasOne(d => d.VotingEvent).WithMany(p => p.VoterEventParticipations)
                .HasForeignKey(d => d.VotingEventId)
                .HasConstraintName("fk_vep_voting_event");
        });

        modelBuilder.Entity<VotingEvent>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("voting_events", tb => tb.HasComment("Procesos electorales configurados por el administrador"));

            entity.HasIndex(e => e.CreatedByVoterId, "idx_ve_created_by");

            entity.HasIndex(e => new { e.EndDate, e.EndTime }, "idx_ve_end");

            entity.HasIndex(e => new { e.StartDate, e.StartTime }, "idx_ve_start");

            entity.HasIndex(e => e.Status, "idx_ve_status");

            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByVoterId)
                .HasComment("Administrador creador")
                .HasColumnType("int(10) unsigned")
                .HasColumnName("created_by_voter_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.ElectionType)
                .HasDefaultValueSql("'PERSONAS'")
                .HasComment("RF-M03-01")
                .HasColumnType("enum('PERSONAS','TEMAS')")
                .HasColumnName("election_type");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.EndTime)
                .HasColumnType("time")
                .HasColumnName("end_time");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.StartTime)
                .HasColumnType("time")
                .HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'PROGRAMADA'")
                .HasColumnType("enum('PROGRAMADA','ACTIVA','FINALIZADA')")
                .HasColumnName("status");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasComment("Nombre de la elección")
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("datetime")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByVoter).WithMany(p => p.VotingEvents)
                .HasForeignKey(d => d.CreatedByVoterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ve_created_by");
        });

        modelBuilder.Entity<VwActiveCensu>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_active_census");

            entity.Property(e => e.ExcluirDePromocion)
                .HasComment("1 = repitente, se omite en la promoción masiva")
                .HasColumnName("excluir_de_promocion");
            entity.Property(e => e.FullName)
                .HasMaxLength(150)
                .HasColumnName("full_name");
            entity.Property(e => e.Grade)
                .HasMaxLength(10)
                .HasComment("Ej: 6°, 7°, ..., 11°")
                .HasColumnName("grade");
            entity.Property(e => e.Id)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("id");
            entity.Property(e => e.RegisteredAt)
                .HasDefaultValueSql("current_timestamp()")
                .HasColumnType("datetime")
                .HasColumnName("registered_at");
            entity.Property(e => e.RequiereCambioClave)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasComment("1 = debe cambiar clave autogenerada en próximo login (RF-M01-02)")
                .HasColumnName("requiere_cambio_clave");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'ACTIVO'")
                .HasColumnType("enum('ACTIVO','INACTIVO','ELIMINADO','EGRESADO')")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<VwVoteCount>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_vote_counts");

            entity.Property(e => e.CandidateId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("candidate_id");
            entity.Property(e => e.CandidateName)
                .HasMaxLength(150)
                .HasComment("Nombre visible en el tarjetón")
                .HasColumnName("candidate_name");
            entity.Property(e => e.EventId)
                .HasColumnType("int(10) unsigned")
                .HasColumnName("event_id");
            entity.Property(e => e.EventStatus)
                .HasDefaultValueSql("'PROGRAMADA'")
                .HasColumnType("enum('PROGRAMADA','ACTIVA','FINALIZADA')")
                .HasColumnName("event_status");
            entity.Property(e => e.EventTitle)
                .HasMaxLength(200)
                .HasComment("Nombre de la elección")
                .HasColumnName("event_title");
            entity.Property(e => e.IsBlankVote)
                .HasComment("1 = Voto en Blanco")
                .HasColumnName("is_blank_vote");
            entity.Property(e => e.TotalVotes)
                .HasColumnType("bigint(21)")
                .HasColumnName("total_votes");
            entity.Property(e => e.VotePercentage)
                .HasPrecision(26, 2)
                .HasColumnName("vote_percentage");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
