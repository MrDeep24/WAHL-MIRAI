// ============================================================
// Archivo:       VotingConcurrencyIntegrationTests.cs
// Módulo:        M05 — Concurrencia de Voto Único, Anonimato y Tarjetón (MySQL)
// Desarrollador: Kevin
// Descripción:   Pruebas de integración contra MySQL/MariaDB real que verifican
//                el envío concurrente duplicado (uq_ep_user_event), anonimato RN-3
//                en esquema/filas de votes, filtrado de tarjetón y rechazos P3.
// Requerimientos: RF-M05-00, RF-M05-01, RN-3, RN-7.1, RN-12.
// ============================================================
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Hubs;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests.Integration;

/// <summary>
/// Pruebas de integración en MySQL/MariaDB real sobre concurrencia de voto único,
/// esquema anónimo RN-3, filtrado de tarjetón y validaciones P3 (RF-M05-00, RN-3).
/// </summary>
[Collection(MySqlDatabaseCollection.Name)]
public sealed class VotingConcurrencyIntegrationTests : IAsyncLifetime
{
    private readonly MySqlDatabaseFixture _fixture;

    public VotingConcurrencyIntegrationTests(MySqlDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseStateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private sealed class NoOpClientProxy : IClientProxy
    {
        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpHubClients : IHubClients
    {
        private readonly IClientProxy _proxy = new NoOpClientProxy();
        public IClientProxy All => _proxy;
        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Client(string connectionId) => _proxy;
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => _proxy;
        public IClientProxy Group(string groupName) => _proxy;
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => _proxy;
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => _proxy;
        public IClientProxy User(string userId) => _proxy;
        public IClientProxy Users(IReadOnlyList<string> userIds) => _proxy;
    }

    private sealed class NoOpGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class NoOpHubContext : IHubContext<ResultsHub>
    {
        public IHubClients Clients { get; } = new NoOpHubClients();
        public IGroupManager Groups { get; } = new NoOpGroupManager();
    }

    private static FakeTimeProvider CreateLocalFakeTimeProvider(DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        var offset = TimeZoneInfo.Local.GetUtcOffset(unspecified);
        var localDto = new DateTimeOffset(unspecified, offset);
        return new FakeTimeProvider(localDto.ToUniversalTime());
    }

    private async Task<uint> InsertUserAsync(string fullName, byte? gradeId = 1, string status = "ACTIVO")
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO `users`
                (`role_id`, `grade_id`, `document_hash`, `encrypted_document`,
                 `full_name`, `contact_email`, `password_hash`, `excluir_de_promocion`, `status`)
            VALUES
                (1, @gradeId, @docHash, @encDoc, @fullName, @email, '$2b$11$dummy', 0, @status);
            SELECT LAST_INSERT_ID();";

        cmd.Parameters.AddWithValue("@gradeId", (object?)gradeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@docHash", Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        cmd.Parameters.AddWithValue("@encDoc", "ENC:" + Guid.NewGuid().ToString("N")[..8]);
        cmd.Parameters.AddWithValue("@fullName", fullName);
        cmd.Parameters.AddWithValue("@email", $"{Guid.NewGuid():N}@test.local");
        cmd.Parameters.AddWithValue("@status", status);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToUInt32(result);
    }

    private async Task<uint> InsertEventAsync(string title, string status = "ACTIVA", byte allowedGradeId = 1)
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();

        uint eventId;
        await using (var cmd = conn.CreateCommand())
        {
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
                    (1, 1, @title, 'Prueba concurrencia MySQL', 'PERSONAS',
                     '2026-06-15', '08:00:00',
                     '2026-06-15', '10:00:00',
                     '2026-06-15', '10:00:00',
                     '2026-06-15', '12:00:00',
                     '2026-06-15', '12:00:00',
                     '2026-06-15', '18:00:00',
                     @status);
                SELECT LAST_INSERT_ID();";
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@status", status);
            eventId = Convert.ToUInt32(await cmd.ExecuteScalarAsync());
        }

        await using (var gradeCmd = conn.CreateCommand())
        {
            gradeCmd.CommandText = @"
                INSERT INTO `event_grades` (`voting_event_id`, `grade_id`)
                VALUES (@eventId, @gradeId);";
            gradeCmd.Parameters.AddWithValue("@eventId", eventId);
            gradeCmd.Parameters.AddWithValue("@gradeId", allowedGradeId);
            await gradeCmd.ExecuteNonQueryAsync();
        }

        return eventId;
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

        return Convert.ToUInt32(await cmd.ExecuteScalarAsync());
    }

    /// <summary>Dos envíos concurrentes para el mismo elector/evento producen exactamente 1 voto y 1 participación (uq_ep_user_event, RN-3).</summary>
    [MySqlIntegrationFact]
    public async Task CastVoteAsync_ConcurrentDoubleSubmit_PersistsSingleVoteAndEnforcesUniqueParticipation()
    {
        var voterId = await InsertUserAsync("Elector Concurrente", gradeId: 1);
        var candUserId = await InsertUserAsync("Candidato Concurrente", gradeId: 1);
        var eventId = await InsertEventAsync("Elección Concurrencia", status: "ACTIVA", allowedGradeId: 1);
        var candidateId = await InsertCandidateAsync(eventId, candUserId, "Candidato A", "APROBADO");
        var blankVoteId = await InsertCandidateAsync(eventId, null, "Voto en Blanco", "APROBADO", isBlankVote: true);

        var clock = CreateLocalFakeTimeProvider(new DateTime(2026, 6, 15, 14, 30, 0));

        await using var context1 = _fixture.CreateDbContext();
        await using var context2 = _fixture.CreateDbContext();
        var service1 = new VotingService(context1, new AuditService(context1), new NoOpHubContext(), clock);
        var service2 = new VotingService(context2, new AuditService(context2), new NoOpHubContext(), clock);

        using var ready = new CountdownEvent(2);
        var startGate = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        var task1 = Task.Run(async () =>
        {
            ready.Signal();
            await startGate.Task;
            return await service1.CastVoteAsync((int)voterId, (int)eventId, (int)candidateId, "198.51.100.10");
        });

        var task2 = Task.Run(async () =>
        {
            ready.Signal();
            await startGate.Task;
            return await service2.CastVoteAsync((int)voterId, (int)eventId, (int)blankVoteId, "198.51.100.11");
        });

        Assert.True(ready.Wait(TimeSpan.FromSeconds(10)));
        startGate.SetResult(true);

        var results = await Task.WhenAll(task1, task2);

        Assert.Equal(1, results.Count(r => r));
        Assert.Equal(1, results.Count(r => !r));

        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();

        await using (var votesCmd = conn.CreateCommand())
        {
            votesCmd.CommandText = "SELECT COUNT(*) FROM `votes` WHERE `voting_event_id` = @eventId;";
            votesCmd.Parameters.AddWithValue("@eventId", eventId);
            Assert.Equal(1L, Convert.ToInt64(await votesCmd.ExecuteScalarAsync()));
        }

        await using (var partCmd = conn.CreateCommand())
        {
            partCmd.CommandText = "SELECT COUNT(*) FROM `event_participations` WHERE `user_id` = @voterId AND `voting_event_id` = @eventId;";
            partCmd.Parameters.AddWithValue("@voterId", voterId);
            partCmd.Parameters.AddWithValue("@eventId", eventId);
            Assert.Equal(1L, Convert.ToInt64(await partCmd.ExecuteScalarAsync()));
        }
    }

    /// <summary>La tabla votes en MySQL carece de columnas de usuario/elector y el voto registrado no revela identidad (RN-3).</summary>
    [MySqlIntegrationFact]
    public async Task VotesTableSchemaAndRow_HasNoVoterOrUserReference_EnforcesRn3()
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();

        var columns = new List<string>();
        await using (var schemaCmd = conn.CreateCommand())
        {
            schemaCmd.CommandText = @"
                SELECT `COLUMN_NAME`
                FROM `INFORMATION_SCHEMA`.`COLUMNS`
                WHERE `TABLE_SCHEMA` = @dbName AND `TABLE_NAME` = 'votes'
                ORDER BY `ORDINAL_POSITION`;";
            schemaCmd.Parameters.AddWithValue("@dbName", _fixture.DatabaseName);
            await using var reader = await schemaCmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                columns.Add(reader.GetString(0));
            }
        }

        Assert.Equal(new[] { "id", "voting_event_id", "candidate_id", "voted_at", "vote_hash" }, columns);
        Assert.DoesNotContain(columns, c => c.Contains("user", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(columns, c => c.Contains("voter", StringComparison.OrdinalIgnoreCase));

        var voterId = await InsertUserAsync("Elector Anónimo", gradeId: 1);
        var eventId = await InsertEventAsync("Elección RN-3", status: "ACTIVA", allowedGradeId: 1);
        var blankVoteId = await InsertCandidateAsync(eventId, null, "Voto en Blanco", "APROBADO", isBlankVote: true);

        await using var context = _fixture.CreateDbContext();
        var clock = CreateLocalFakeTimeProvider(new DateTime(2026, 6, 15, 15, 0, 0));
        var service = new VotingService(context, new AuditService(context), new NoOpHubContext(), clock);

        var ok = await service.CastVoteAsync((int)voterId, (int)eventId, (int)blankVoteId, "198.51.100.55");
        Assert.True(ok);

        var voteRow = await context.Votes.AsNoTracking().SingleAsync(v => v.VotingEventId == eventId);
        Assert.Equal(blankVoteId, voteRow.CandidateId);
        Assert.Equal(64, voteRow.VoteHash.Length);

        var audit = await context.AuditLogs.AsNoTracking()
            .SingleAsync(a => a.Action == "VOTE_CAST" && a.VoterId == voterId);
        Assert.Null(audit.TargetId);
        Assert.Equal($"Event: {eventId}", audit.Details);
    }

    /// <summary>GetCandidatesForEventAsync en MySQL real retorna exclusivamente candidatos APROBADO y voto en blanco del evento (RF-M05-00).</summary>
    [MySqlIntegrationFact]
    public async Task GetCandidatesForEventAsync_RealMySql_ReturnsOnlyApprovedAndBlankVote()
    {
        var u1 = await InsertUserAsync("Cand 1", gradeId: 1);
        var u2 = await InsertUserAsync("Cand 2", gradeId: 1);
        var u3 = await InsertUserAsync("Cand 3", gradeId: 1);
        var u4 = await InsertUserAsync("Cand 4", gradeId: 1);

        var eventId = await InsertEventAsync("Elección Tarjetón MySQL", status: "ACTIVA", allowedGradeId: 1);
        var otherEventId = await InsertEventAsync("Otro Evento", status: "ACTIVA", allowedGradeId: 1);

        var approvedId = await InsertCandidateAsync(eventId, u1, "Aprobado", "APROBADO");
        var blankId = await InsertCandidateAsync(eventId, null, "Voto en Blanco", "APROBADO", isBlankVote: true);
        await InsertCandidateAsync(eventId, u2, "Pendiente", "PENDIENTE");
        await InsertCandidateAsync(eventId, u3, "Rechazado", "RECHAZADO");
        await InsertCandidateAsync(eventId, u4, "Retirado", "RETIRADO");
        await InsertCandidateAsync(otherEventId, null, "Voto en Blanco Otro", "APROBADO", isBlankVote: true);

        await using var context = _fixture.CreateDbContext();
        var clock = CreateLocalFakeTimeProvider(new DateTime(2026, 6, 15, 14, 0, 0));
        var service = new VotingService(context, new AuditService(context), new NoOpHubContext(), clock);

        var ballot = await service.GetCandidatesForEventAsync((int)eventId);

        Assert.Equal(2, ballot.Count);
        Assert.Contains(ballot, c => c.Id == approvedId);
        Assert.Contains(ballot, c => c.Id == blankId && c.IsBlankVote);
    }

    /// <summary>CastVoteDetailedAsync en MySQL real aplica rechazos P3 sin persistir votos ni participaciones.</summary>
    [MySqlIntegrationFact]
    public async Task CastVoteDetailedAsync_RealMySql_RejectsP3ViolationsAndPersistsNothing()
    {
        var voterGrade1 = await InsertUserAsync("Elector Grado 1", gradeId: 1);
        var voterGrade2 = await InsertUserAsync("Elector Grado 2", gradeId: 2);
        var candUser = await InsertUserAsync("Cand Pendiente", gradeId: 1);

        var activeEventId = await InsertEventAsync("Elección Activa", status: "ACTIVA", allowedGradeId: 1);
        var proposalsEventId = await InsertEventAsync("Elección Propuestas", status: "PROPUESTAS", allowedGradeId: 1);
        var deletedEventId = await InsertEventAsync("Elección Eliminada", status: "ELIMINADO", allowedGradeId: 1);

        var pendingCandId = await InsertCandidateAsync(activeEventId, candUser, "Pendiente", "PENDIENTE");
        var approvedCandId = await InsertCandidateAsync(activeEventId, null, "Voto en Blanco", "APROBADO", isBlankVote: true);
        var proposalsCandId = await InsertCandidateAsync(proposalsEventId, null, "Voto en Blanco Prop", "APROBADO", isBlankVote: true);
        var deletedEventCandId = await InsertCandidateAsync(deletedEventId, null, "Voto en Blanco Del", "APROBADO", isBlankVote: true);

        await using var context = _fixture.CreateDbContext();

        // 1. Grado no habilitado
        var serviceActiveTime = new VotingService(
            context,
            new AuditService(context),
            new NoOpHubContext(),
            CreateLocalFakeTimeProvider(new DateTime(2026, 6, 15, 14, 0, 0)));
        var wrongGrade = await serviceActiveTime.CastVoteDetailedAsync((int)voterGrade2, (int)activeEventId, (int)approvedCandId, "198.51.100.1");
        Assert.False(wrongGrade.Success);
        Assert.Equal("GRADE_NOT_ELIGIBLE", wrongGrade.ErrorCode);

        // 2. Evento ELIMINADO
        var deletedEv = await serviceActiveTime.CastVoteDetailedAsync((int)voterGrade1, (int)deletedEventId, (int)deletedEventCandId, "198.51.100.1");
        Assert.False(deletedEv.Success);
        Assert.Equal("EVENT_NOT_FOUND", deletedEv.ErrorCode);

        // 3. Candidato PENDIENTE
        var pendingCand = await serviceActiveTime.CastVoteDetailedAsync((int)voterGrade1, (int)activeEventId, (int)pendingCandId, "198.51.100.1");
        Assert.False(pendingCand.Success);
        Assert.Equal("INVALID_CANDIDATE", pendingCand.ErrorCode);

        // 4. Etapa fuera de ACTIVA (PROPUESTAS y FINALIZADA tras VotingEnd)
        var serviceProposalsTime = new VotingService(
            context,
            new AuditService(context),
            new NoOpHubContext(),
            CreateLocalFakeTimeProvider(new DateTime(2026, 6, 15, 11, 0, 0)));
        var wrongStageProposals = await serviceProposalsTime.CastVoteDetailedAsync((int)voterGrade1, (int)proposalsEventId, (int)proposalsCandId, "198.51.100.1");
        Assert.False(wrongStageProposals.Success);
        Assert.Equal("EVENT_NOT_ACTIVE", wrongStageProposals.ErrorCode);

        var serviceAfterVotingEnd = new VotingService(
            context,
            new AuditService(context),
            new NoOpHubContext(),
            CreateLocalFakeTimeProvider(new DateTime(2026, 6, 15, 18, 0, 0)));
        var wrongStageEnded = await serviceAfterVotingEnd.CastVoteDetailedAsync((int)voterGrade1, (int)activeEventId, (int)approvedCandId, "198.51.100.1");
        Assert.False(wrongStageEnded.Success);
        Assert.Equal("EVENT_NOT_ACTIVE", wrongStageEnded.ErrorCode);

        Assert.Equal(0, await context.Votes.CountAsync());
        Assert.Equal(0, await context.VoterEventParticipations.CountAsync());
    }
}
