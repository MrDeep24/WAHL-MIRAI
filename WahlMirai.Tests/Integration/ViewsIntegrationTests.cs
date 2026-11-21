using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using WahlMirai.Web.Models;
using Xunit;

namespace WahlMirai.Tests.Integration;

[Collection(MySqlDatabaseCollection.Name)]
public class ViewsIntegrationTests : IAsyncLifetime
{
    private readonly MySqlDatabaseFixture _fixture;

    public ViewsIntegrationTests(MySqlDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseStateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Helpers

    private async Task<uint> InsertUserAsync(
        string fullName,
        string email,
        string roleName = "ELECTOR",
        string status = "ACTIVO",
        byte? gradeId = 1,
        bool excluirDePromocion = false)
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        byte roleId = roleName switch
        {
            "ELECTOR" => 1,
            "ADMIN" => 2,
            "SUPER_ADMIN" => 3,
            _ => 1
        };

        var doc = Guid.NewGuid().ToString("N")[..10];
        cmd.CommandText = @"
            INSERT INTO `users`
                (`role_id`, `grade_id`, `document_hash`, `encrypted_document`,
                 `full_name`, `contact_email`, `password_hash`, `excluir_de_promocion`, `status`)
            VALUES
                (@roleId, @gradeId, @docHash, @encDoc, @fullName, @email, '$2b$11$dummy', @excluir, @status);
            SELECT LAST_INSERT_ID();";

        cmd.Parameters.AddWithValue("@roleId", roleId);
        cmd.Parameters.AddWithValue("@gradeId", (object?)gradeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@docHash", Guid.NewGuid().ToString("N"));
        cmd.Parameters.AddWithValue("@encDoc", "ENC:" + doc);
        cmd.Parameters.AddWithValue("@fullName", fullName);
        cmd.Parameters.AddWithValue("@email", email);
        cmd.Parameters.AddWithValue("@excluir", excluirDePromocion ? 1 : 0);
        cmd.Parameters.AddWithValue("@status", status);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToUInt32(result);
    }

    private async Task<uint> InsertVotingEventAsync(
        string title,
        string status = "ACTIVA",
        uint positionId = 1)
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO `voting_events`
                (`created_by_user_id`, `position_id`, `title`, `description`, `election_type`,
                 `registration_start_date`, `registration_start_time`,
                 `registration_end_date`, `registration_end_time`,
                 `proposals_start_date`, `proposals_start_time`,
                 `proposals_end_date`, `proposals_end_time`,
                 `voting_start_date`, `voting_start_time`,
                 `voting_end_date`, `voting_end_time`,
                 `status`)
            VALUES
                (1, @positionId, @title, 'Descripción de prueba', 'PERSONAS',
                 CURRENT_DATE - INTERVAL 5 DAY, '08:00:00',
                 CURRENT_DATE - INTERVAL 4 DAY, '18:00:00',
                 CURRENT_DATE - INTERVAL 3 DAY, '08:00:00',
                 CURRENT_DATE - INTERVAL 2 DAY, '18:00:00',
                 CURRENT_DATE - INTERVAL 1 DAY, '08:00:00',
                 CURRENT_DATE + INTERVAL 1 DAY, '18:00:00',
                 @status);
            SELECT LAST_INSERT_ID();";

        cmd.Parameters.AddWithValue("@positionId", positionId);
        cmd.Parameters.AddWithValue("@title", title);
        cmd.Parameters.AddWithValue("@status", status);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToUInt32(result);
    }

    private async Task<uint> InsertCandidateAsync(
        uint eventId,
        uint? userId,
        string name,
        string status = "APROBADO",
        bool isBlankVote = false)
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO `candidates`
                (`voting_event_id`, `user_id`, `name`, `status`, `is_blank_vote`, `enrolled_at`)
            VALUES
                (@eventId, @userId, @name, @status, @isBlank, NOW());
            SELECT LAST_INSERT_ID();";

        cmd.Parameters.AddWithValue("@eventId", eventId);
        cmd.Parameters.AddWithValue("@userId", (object?)userId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@isBlank", isBlankVote ? 1 : 0);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToUInt32(result);
    }

    private async Task InsertVoteAsync(uint eventId, uint candidateId, string? voteHash = null)
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO `votes` (`voting_event_id`, `candidate_id`, `vote_hash`, `voted_at`)
            VALUES (@eventId, @candidateId, @voteHash, NOW());";

        cmd.Parameters.AddWithValue("@eventId", eventId);
        cmd.Parameters.AddWithValue("@candidateId", candidateId);
        cmd.Parameters.AddWithValue("@voteHash", voteHash ?? Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));

        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<uint> InsertWhitelistEntryAsync(
        string fullName,
        string? email,
        byte gradeId = 1,
        DateTime? claimedAt = null,
        DateTime? createdAt = null)
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO `census_whitelist`
                (`document_hash`, `encrypted_document`, `full_name`, `contact_email`,
                 `grade_id`, `excluir_de_promocion`, `uploaded_by_user_id`, `claimed_at`, `created_at`)
            VALUES
                (@docHash, @encDoc, @fullName, @email,
                 @gradeId, 0, 1, @claimedAt, @createdAt);
            SELECT LAST_INSERT_ID();";

        cmd.Parameters.AddWithValue("@docHash", Guid.NewGuid().ToString("N"));
        cmd.Parameters.AddWithValue("@encDoc", "ENC:" + Guid.NewGuid().ToString("N")[..10]);
        cmd.Parameters.AddWithValue("@fullName", fullName);
        cmd.Parameters.AddWithValue("@email", (object?)email ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@gradeId", gradeId);
        cmd.Parameters.AddWithValue("@claimedAt", (object?)claimedAt ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@createdAt", (object?)createdAt ?? DateTime.UtcNow);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToUInt32(result);
    }

    private async Task<ulong> InsertEmailQueueAsync(
        uint userId,
        string emailType = "RECUPERACION_ACCESO",
        string status = "PENDIENTE",
        byte attempts = 0,
        DateTime? createdAt = null)
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO `email_queue`
                (`user_id`, `email_type`, `status`, `attempts`, `created_at`)
            VALUES
                (@userId, @emailType, @status, @attempts, @createdAt);
            SELECT LAST_INSERT_ID();";

        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@emailType", emailType);
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@attempts", attempts);
        cmd.Parameters.AddWithValue("@createdAt", (object?)createdAt ?? DateTime.UtcNow);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToUInt64(result);
    }

    #endregion

    #region vw_vote_counts Tests

    [MySqlIntegrationFact]
    public async Task VwVoteCounts_CalculatesTotalsAndPercentagesCorrectly_VerifiesWindowFunction()
    {
        // Prueba la función de ventana SUM(COUNT(v.id)) OVER (PARTITION BY ve.id) en MariaDB real
        var eventId = await InsertVotingEventAsync("Elección de Prueba - Totales");
        var userA = await InsertUserAsync("Candidato A", "candA@test.com");
        var userB = await InsertUserAsync("Candidato B", "candB@test.com");

        var candA = await InsertCandidateAsync(eventId, userA, "Candidato A", "APROBADO");
        var candB = await InsertCandidateAsync(eventId, userB, "Candidato B", "APROBADO");

        // 3 votos para A, 1 voto para B => Total 4 votos
        await InsertVoteAsync(eventId, candA);
        await InsertVoteAsync(eventId, candA);
        await InsertVoteAsync(eventId, candA);
        await InsertVoteAsync(eventId, candB);

        using var context = _fixture.CreateDbContext();
        var results = await context.VwVoteCounts
            .Where(v => v.EventId == eventId)
            .OrderByDescending(v => v.TotalVotes)
            .ToListAsync();

        Assert.Equal(2, results.Count);

        var resultA = results.Single(r => r.CandidateId == candA);
        Assert.Equal(3, resultA.TotalVotes);
        Assert.Equal(75.00m, resultA.VotePercentage);

        var resultB = results.Single(r => r.CandidateId == candB);
        Assert.Equal(1, resultB.TotalVotes);
        Assert.Equal(25.00m, resultB.VotePercentage);
    }

    [MySqlIntegrationFact]
    public async Task VwVoteCounts_HandlesTiesCorrectly()
    {
        var eventId = await InsertVotingEventAsync("Elección Empate");
        var userA = await InsertUserAsync("Candidato Empate 1", "empate1@test.com");
        var userB = await InsertUserAsync("Candidato Empate 2", "empate2@test.com");

        var candA = await InsertCandidateAsync(eventId, userA, "Candidato Empate 1", "APROBADO");
        var candB = await InsertCandidateAsync(eventId, userB, "Candidato Empate 2", "APROBADO");

        // 2 votos cada uno => 50% cada uno
        await InsertVoteAsync(eventId, candA);
        await InsertVoteAsync(eventId, candA);
        await InsertVoteAsync(eventId, candB);
        await InsertVoteAsync(eventId, candB);

        using var context = _fixture.CreateDbContext();
        var results = await context.VwVoteCounts
            .Where(v => v.EventId == eventId)
            .ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal(2, r.TotalVotes);
            Assert.Equal(50.00m, r.VotePercentage);
        });
    }

    [MySqlIntegrationFact]
    public async Task VwVoteCounts_ElectionWithoutVotes_ReturnsNullPercentageWithoutDivisionError()
    {
        // Sin votos emitidos, NULLIF debe prevenir división entre cero y arrojar VotePercentage = NULL
        var eventId = await InsertVotingEventAsync("Elección Sin Votos");
        var userA = await InsertUserAsync("Candidato Sin Votos", "sinvotos@test.com");
        var candA = await InsertCandidateAsync(eventId, userA, "Candidato Sin Votos", "APROBADO");

        using var context = _fixture.CreateDbContext();
        var results = await context.VwVoteCounts
            .Where(v => v.EventId == eventId)
            .ToListAsync();

        Assert.Single(results);
        var r = results[0];
        Assert.Equal(0, r.TotalVotes);
        Assert.Null(r.VotePercentage);
    }

    [MySqlIntegrationFact]
    public async Task VwVoteCounts_OnlyApprovedCandidatesAreIncluded()
    {
        var eventId = await InsertVotingEventAsync("Elección Filtro Estados");
        var u1 = await InsertUserAsync("Aprobado", "u1@test.com");
        var u2 = await InsertUserAsync("Pendiente", "u2@test.com");
        var u3 = await InsertUserAsync("Rechazado", "u3@test.com");
        var u4 = await InsertUserAsync("Retirado", "u4@test.com");

        var candAprobado = await InsertCandidateAsync(eventId, u1, "Aprobado", "APROBADO");
        await InsertCandidateAsync(eventId, u2, "Pendiente", "PENDIENTE");
        await InsertCandidateAsync(eventId, u3, "Rechazado", "RECHAZADO");
        await InsertCandidateAsync(eventId, u4, "Retirado", "RETIRADO");

        await InsertVoteAsync(eventId, candAprobado);

        using var context = _fixture.CreateDbContext();
        var results = await context.VwVoteCounts
            .Where(v => v.EventId == eventId)
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal(candAprobado, results[0].CandidateId);
    }

    [MySqlIntegrationFact]
    public async Task VwVoteCounts_IncludesBlankVote()
    {
        var eventId = await InsertVotingEventAsync("Elección Voto Blanco");
        var u1 = await InsertUserAsync("Candidato Real", "real@test.com");
        var candReal = await InsertCandidateAsync(eventId, u1, "Candidato Real", "APROBADO");
        var candBlanco = await InsertCandidateAsync(eventId, null, "Voto en Blanco", "APROBADO", isBlankVote: true);

        await InsertVoteAsync(eventId, candReal);
        await InsertVoteAsync(eventId, candBlanco);

        using var context = _fixture.CreateDbContext();
        var results = await context.VwVoteCounts
            .Where(v => v.EventId == eventId)
            .OrderByDescending(v => v.IsBlankVote)
            .ToListAsync();

        Assert.Equal(2, results.Count);

        var blanco = results.Single(r => r.IsBlankVote);
        Assert.Equal("Voto en Blanco", blanco.CandidateName);
        Assert.Equal(1, blanco.TotalVotes);
        Assert.Equal(50.00m, blanco.VotePercentage);
    }

    [MySqlIntegrationFact]
    public async Task VwVoteCounts_ExcludesEliminatedEvents()
    {
        // RN-7.1: Procesos con soft-delete status = 'ELIMINADO' no deben aparecer en vw_vote_counts
        var eventId = await InsertVotingEventAsync("Elección Eliminada", status: "ELIMINADO");
        var u1 = await InsertUserAsync("Candidato Eliminado", "elim@test.com");
        var cand = await InsertCandidateAsync(eventId, u1, "Candidato Eliminado", "APROBADO");
        await InsertVoteAsync(eventId, cand);

        using var context = _fixture.CreateDbContext();
        var results = await context.VwVoteCounts
            .Where(v => v.EventId == eventId)
            .ToListAsync();

        Assert.Empty(results);
    }

    #endregion

    #region vw_active_census Tests

    [MySqlIntegrationFact]
    public async Task VwActiveCensus_FiltersByRoleElectorAndActiveInactiveStatus()
    {
        // Solo rol ELECTOR y status ACTIVO o INACTIVO; excluye ELIMINADO y EGRESADO
        var uActivo = await InsertUserAsync("Elector Activo", "activo@test.com", "ELECTOR", "ACTIVO");
        var uInactivo = await InsertUserAsync("Elector Inactivo", "inactivo@test.com", "ELECTOR", "INACTIVO");
        var uEliminado = await InsertUserAsync("Elector Eliminado", "eliminado@test.com", "ELECTOR", "ELIMINADO");
        var uEgresado = await InsertUserAsync("Elector Egresado", "egresado@test.com", "ELECTOR", "EGRESADO");

        using var context = _fixture.CreateDbContext();
        var results = await context.VwActiveCensus
            .Where(c => c.Id == uActivo || c.Id == uInactivo || c.Id == uEliminado || c.Id == uEgresado)
            .ToListAsync();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, c => c.Id == uActivo && c.Status == "ACTIVO");
        Assert.Contains(results, c => c.Id == uInactivo && c.Status == "INACTIVO");
        Assert.DoesNotContain(results, c => c.Id == uEliminado);
        Assert.DoesNotContain(results, c => c.Id == uEgresado);
    }

    [MySqlIntegrationFact]
    public async Task VwActiveCensus_ResolvesReadableGradeName()
    {
        // El DDL siembra grados: id 1 = 6°, id 2 = 7°
        var userWithGrade = await InsertUserAsync("Con Grado", "congrado@test.com", gradeId: 1);
        var userWithoutGrade = await InsertUserAsync("Sin Grado", "singrado@test.com", gradeId: null);

        using var context = _fixture.CreateDbContext();
        var rWithGrade = await context.VwActiveCensus.FirstOrDefaultAsync(c => c.Id == userWithGrade);
        var rWithoutGrade = await context.VwActiveCensus.FirstOrDefaultAsync(c => c.Id == userWithoutGrade);

        Assert.NotNull(rWithGrade);
        Assert.Equal("6°", rWithGrade.Grade);

        Assert.NotNull(rWithoutGrade);
        Assert.Null(rWithoutGrade.Grade);
    }

    [MySqlIntegrationFact]
    public async Task VwActiveCensus_ExcludesAdminUsers()
    {
        // Los administradores (id 1 o nuevos) no deben figurar en el censo electoral
        var adminUser = await InsertUserAsync("Administrador Nuevo", "admin2@colegio.edu.co", roleName: "ADMIN");

        using var context = _fixture.CreateDbContext();
        var adminResult = await context.VwActiveCensus.FirstOrDefaultAsync(c => c.Id == adminUser);
        var seedSuperAdmin = await context.VwActiveCensus.FirstOrDefaultAsync(c => c.Id == 1);

        Assert.Null(adminResult);
        Assert.Null(seedSuperAdmin);
    }

    #endregion

    #region vw_pending_whitelist Tests

    [MySqlIntegrationFact]
    public async Task VwPendingWhitelist_IncludesOnlyUnclaimedEntries()
    {
        var unclaimedId = await InsertWhitelistEntryAsync("No Reclamado", "norecl@test.com", gradeId: 1, claimedAt: null);
        var claimedId = await InsertWhitelistEntryAsync("Ya Reclamado", "recl@test.com", gradeId: 1, claimedAt: DateTime.UtcNow);

        using var context = _fixture.CreateDbContext();
        var results = await context.VwPendingWhitelists
            .Where(w => w.Id == unclaimedId || w.Id == claimedId)
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal(unclaimedId, results[0].Id);
    }

    [MySqlIntegrationFact]
    public async Task VwPendingWhitelist_ProjectsContactEmailAndGrade()
    {
        // Permite verificar que contact_email acepta tanto email como NULL
        var idWithEmail = await InsertWhitelistEntryAsync("Con Correo", "correo@test.com", gradeId: 2);
        var idWithoutEmail = await InsertWhitelistEntryAsync("Sin Correo", null, gradeId: 1);

        using var context = _fixture.CreateDbContext();
        var itemEmail = await context.VwPendingWhitelists.FirstOrDefaultAsync(w => w.Id == idWithEmail);
        var itemNoEmail = await context.VwPendingWhitelists.FirstOrDefaultAsync(w => w.Id == idWithoutEmail);

        Assert.NotNull(itemEmail);
        Assert.Equal("correo@test.com", itemEmail.ContactEmail);
        Assert.Equal("7°", itemEmail.Grade);

        Assert.NotNull(itemNoEmail);
        Assert.Null(itemNoEmail.ContactEmail);
        Assert.Equal("6°", itemNoEmail.Grade);
    }

    [MySqlIntegrationFact]
    public async Task VwPendingWhitelist_OrdersByCreatedAtAscending()
    {
        var baseTime = DateTime.UtcNow.AddMinutes(-30);
        var id1 = await InsertWhitelistEntryAsync("Tercero en insert", "e3@test.com", createdAt: baseTime.AddMinutes(20));
        var id2 = await InsertWhitelistEntryAsync("Primero en tiempo", "e1@test.com", createdAt: baseTime);
        var id3 = await InsertWhitelistEntryAsync("Segundo en tiempo", "e2@test.com", createdAt: baseTime.AddMinutes(10));

        using var context = _fixture.CreateDbContext();
        var results = await context.VwPendingWhitelists
            .Where(w => w.Id == id1 || w.Id == id2 || w.Id == id3)
            .ToListAsync();

        Assert.Equal(3, results.Count);
        Assert.Equal(id2, results[0].Id);
        Assert.Equal(id3, results[1].Id);
        Assert.Equal(id1, results[2].Id);
    }

    #endregion

    #region vw_pending_email_queue Tests

    [MySqlIntegrationFact]
    public async Task VwPendingEmailQueue_IncludesOnlyPendingStatus()
    {
        var u = await InsertUserAsync("Usuario Correo", "correo@test.com");

        var qPendiente = await InsertEmailQueueAsync(u, status: "PENDIENTE");
        var qEnviado = await InsertEmailQueueAsync(u, status: "ENVIADO");
        var qFallido = await InsertEmailQueueAsync(u, status: "FALLIDO");

        using var context = _fixture.CreateDbContext();
        var results = await context.VwPendingEmailQueues
            .Where(q => q.Id == qPendiente || q.Id == qEnviado || q.Id == qFallido)
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal(qPendiente, results[0].Id);
    }

    [MySqlIntegrationFact]
    public async Task VwPendingEmailQueue_ResolvesContactEmailAndFullNameFromUsers()
    {
        var u = await InsertUserAsync("Estudiante Notificado", "notif@test.com");
        var qId = await InsertEmailQueueAsync(u, emailType: "RECUPERACION_ACCESO", status: "PENDIENTE", attempts: 1);

        using var context = _fixture.CreateDbContext();
        var item = await context.VwPendingEmailQueues.FirstOrDefaultAsync(q => q.Id == qId);

        Assert.NotNull(item);
        Assert.Equal("Estudiante Notificado", item.FullName);
        Assert.Equal("notif@test.com", item.ContactEmail);
        Assert.Equal("RECUPERACION_ACCESO", item.EmailType);
        Assert.Equal((byte)1, item.Attempts);
        Assert.Equal(u, item.VoterId);
    }

    [MySqlIntegrationFact]
    public async Task VwPendingEmailQueue_OrdersByCreatedAtAscending()
    {
        var u = await InsertUserAsync("Cola Orden", "colaorden@test.com");
        var baseTime = DateTime.UtcNow.AddMinutes(-30);

        var q3 = await InsertEmailQueueAsync(u, createdAt: baseTime.AddMinutes(20));
        var q1 = await InsertEmailQueueAsync(u, createdAt: baseTime);
        var q2 = await InsertEmailQueueAsync(u, createdAt: baseTime.AddMinutes(10));

        using var context = _fixture.CreateDbContext();
        var results = await context.VwPendingEmailQueues
            .Where(q => q.Id == q1 || q.Id == q2 || q.Id == q3)
            .ToListAsync();

        Assert.Equal(3, results.Count);
        Assert.Equal(q1, results[0].Id);
        Assert.Equal(q2, results[1].Id);
        Assert.Equal(q3, results[2].Id);
    }

    #endregion

    #region vw_pending_candidacies Tests

    [MySqlIntegrationFact]
    public async Task VwPendingCandidacies_IncludesOnlyPendingStatus()
    {
        var eventId = await InsertVotingEventAsync("Elección Candidaturas");
        var u1 = await InsertUserAsync("Cand Pendiente", "cpend@test.com");
        var u2 = await InsertUserAsync("Cand Aprobado", "capr@test.com");

        var cPend = await InsertCandidateAsync(eventId, u1, "Cand Pendiente", "PENDIENTE");
        var cApr = await InsertCandidateAsync(eventId, u2, "Cand Aprobado", "APROBADO");

        using var context = _fixture.CreateDbContext();
        var results = await context.VwPendingCandidacies
            .Where(c => c.CandidateId == cPend || c.CandidateId == cApr)
            .ToListAsync();

        Assert.Single(results);
        Assert.Equal(cPend, results[0].CandidateId);
    }

    [MySqlIntegrationFact]
    public async Task VwPendingCandidacies_CalculatesMandatoryRequirementsAndUploadedDocuments()
    {
        // En el DDL semilla:
        // Position 1 ("Personero"): 2 requisitos sembrados (ambos is_mandatory = 1)
        // Position 3 ("Representante de Curso"): 1 requisito sembrado (is_mandatory = 1)
        var eventId = await InsertVotingEventAsync("Elección Requisitos", positionId: 1);
        var u = await InsertUserAsync("Candidato Con Documentos", "cdoc@test.com");
        var candId = await InsertCandidateAsync(eventId, u, "Candidato Con Documentos", "PENDIENTE");

        // Subir 1 documento obligatorio para el requisito 1 del cargo 1
        await using (var conn = _fixture.CreateRawConnection())
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO `candidacy_documents`
                    (`candidate_id`, `requirement_id`, `file_url`, `uploaded_at`)
                VALUES
                    (@candId, 1, 'https://storage.test/doc1.pdf', NOW());";
            cmd.Parameters.AddWithValue("@candId", candId);
            await cmd.ExecuteNonQueryAsync();
        }

        using var context = _fixture.CreateDbContext();
        var item = await context.VwPendingCandidacies.FirstOrDefaultAsync(c => c.CandidateId == candId);

        Assert.NotNull(item);
        Assert.Equal(2, item.MandatoryRequirements);
        Assert.Equal(1, item.MandatoryDocumentsUploaded);
        Assert.Equal("Personero", item.PositionName);
        Assert.Equal("Elección Requisitos", item.EventTitle);
        Assert.Equal("Candidato Con Documentos", item.CandidateName);
    }

    [MySqlIntegrationFact]
    public async Task VwPendingCandidacies_MandatoryAndOptionalRequirementsMixed_CalculatesCountsCorrectly()
    {
        // Posición 3 ("Representante de Curso") tiene 1 requisito obligatorio sembrado (id 4, is_mandatory = 1)
        // Agregamos un requisito OPCIONAL (is_mandatory = 0) para comprobar el comportamiento con mezcla
        uint optionalReqId;
        await using (var conn = _fixture.CreateRawConnection())
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO `position_requirements`
                    (`position_id`, `description`, `is_mandatory`, `display_order`)
                VALUES
                    (3, 'Certificado de voluntariado (Opcional)', 0, 2);
                SELECT LAST_INSERT_ID();";
            optionalReqId = Convert.ToUInt32(await cmd.ExecuteScalarAsync());
        }

        var eventId = await InsertVotingEventAsync("Elección Representante con Opcional", positionId: 3);
        var u = await InsertUserAsync("Candidato Representante", "rep@test.com");
        var candId = await InsertCandidateAsync(eventId, u, "Candidato Representante", "PENDIENTE");

        // Subir únicamente el documento para el requisito OPCIONAL
        await using (var conn = _fixture.CreateRawConnection())
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO `candidacy_documents`
                    (`candidate_id`, `requirement_id`, `file_url`, `uploaded_at`)
                VALUES
                    (@candId, @optReqId, 'https://storage.test/opcional.pdf', NOW());";
            cmd.Parameters.AddWithValue("@candId", candId);
            cmd.Parameters.AddWithValue("@optReqId", optionalReqId);
            await cmd.ExecuteNonQueryAsync();
        }

        using var context = _fixture.CreateDbContext();
        var item = await context.VwPendingCandidacies.FirstOrDefaultAsync(c => c.CandidateId == candId);

        Assert.NotNull(item);
        // Debe haber 1 obligatorio (el sembrado) ignorando el opcional recién agregado
        Assert.Equal(1, item.MandatoryRequirements);
        // Debe ser 0 subidos obligatorios porque el único subido es opcional
        Assert.Equal(0, item.MandatoryDocumentsUploaded);
        Assert.Equal("Representante de Curso", item.PositionName);
        Assert.Equal("Elección Representante con Opcional", item.EventTitle);

        // Ahora subir el documento obligatorio (id 4)
        await using (var conn = _fixture.CreateRawConnection())
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO `candidacy_documents`
                    (`candidate_id`, `requirement_id`, `file_url`, `uploaded_at`)
                VALUES
                    (@candId, 4, 'https://storage.test/obligatorio.pdf', NOW());";
            cmd.Parameters.AddWithValue("@candId", candId);
            await cmd.ExecuteNonQueryAsync();
        }

        var updatedItem = await context.VwPendingCandidacies.AsNoTracking().FirstOrDefaultAsync(c => c.CandidateId == candId);
        Assert.NotNull(updatedItem);
        Assert.Equal(1, updatedItem.MandatoryRequirements);
        Assert.Equal(1, updatedItem.MandatoryDocumentsUploaded);
    }

    #endregion

    #region EF Core Type Mapping Materialization

    [MySqlIntegrationFact]
    public async Task AllViews_MaterializeViaEfCoreKeylessDbSetsWithoutTypeExceptions()
    {
        // Crea al menos una fila visible en cada vista para garantizar que EF Core
        // materialice entidades reales sin excepciones de conversión de tipo
        // (DECIMAL de ROUND, COUNT como long, TINYINT(1) como bool, DATETIME, etc.)
        var eventId = await InsertVotingEventAsync("Elección Para Mapeo");
        var uElector1 = await InsertUserAsync("Elector Mapeo 1", "mapeo1@test.com", gradeId: 1);
        var uElector2 = await InsertUserAsync("Elector Mapeo 2", "mapeo2@test.com", gradeId: 1);
        var candId = await InsertCandidateAsync(eventId, uElector1, "Candidato Mapeo", "APROBADO");
        await InsertVoteAsync(eventId, candId);

        var candPendId = await InsertCandidateAsync(eventId, uElector2, "Candidato Pend Mapeo", "PENDIENTE");
        await InsertWhitelistEntryAsync("Whitelist Mapeo", "wmap@test.com", gradeId: 1);
        await InsertEmailQueueAsync(uElector1, status: "PENDIENTE");

        using var context = _fixture.CreateDbContext();

        var voteCounts = await context.VwVoteCounts.ToListAsync();
        Assert.NotEmpty(voteCounts);
        Assert.All(voteCounts, vc =>
        {
            Assert.IsType<uint>(vc.EventId);
            Assert.IsType<long>(vc.TotalVotes);
            if (vc.VotePercentage.HasValue)
            {
                Assert.IsType<decimal>(vc.VotePercentage.Value);
            }
        });

        var activeCensus = await context.VwActiveCensus.ToListAsync();
        Assert.NotEmpty(activeCensus);
        Assert.All(activeCensus, ac =>
        {
            Assert.IsType<uint>(ac.Id);
            Assert.IsType<bool>(ac.ExcluirDePromocion);
            Assert.IsType<DateTime>(ac.RegisteredAt);
        });

        var pendingWhitelists = await context.VwPendingWhitelists.ToListAsync();
        Assert.NotEmpty(pendingWhitelists);
        Assert.All(pendingWhitelists, pw =>
        {
            Assert.IsType<uint>(pw.Id);
            Assert.IsType<byte>(pw.GradeId);
            Assert.IsType<bool>(pw.ExcluirDePromocion);
        });

        var pendingEmailQueues = await context.VwPendingEmailQueues.ToListAsync();
        Assert.NotEmpty(pendingEmailQueues);
        Assert.All(pendingEmailQueues, eq =>
        {
            Assert.IsType<ulong>(eq.Id);
            Assert.IsType<uint>(eq.VoterId);
            Assert.IsType<byte>(eq.Attempts);
        });

        var pendingCandidacies = await context.VwPendingCandidacies.ToListAsync();
        Assert.NotEmpty(pendingCandidacies);
        Assert.All(pendingCandidacies, pc =>
        {
            Assert.IsType<uint>(pc.CandidateId);
            Assert.IsType<long>(pc.MandatoryRequirements);
            Assert.IsType<long>(pc.MandatoryDocumentsUploaded);
        });
    }

    #endregion
}
