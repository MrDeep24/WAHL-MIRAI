using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MySqlConnector;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests.Integration;

[Collection(MySqlDatabaseCollection.Name)]
public class ServicesRelationalBranchTests : IAsyncLifetime
{
    private readonly MySqlDatabaseFixture _fixture;

    public ServicesRelationalBranchTests(MySqlDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseStateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    #region Helpers & Fakes

    private sealed class TestEmailSender : IEmailSender
    {
        public List<string> SentEmails { get; } = new();

        public Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
        {
            SentEmails.Add(toEmail);
            return Task.CompletedTask;
        }
    }

    private sealed class EmptyTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object?> LoadTempData(HttpContext context) => new Dictionary<string, object?>();
        public void SaveTempData(HttpContext context, IDictionary<string, object?> values) { }
    }

    private static WahlMiraiDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new WahlMiraiDbContext(options);

        // Sembrar catálogos básicos en el contexto en memoria
        context.Roles.AddRange(
            new Role { Id = 1, Name = "ELECTOR", Description = "Elector" },
            new Role { Id = 2, Name = "ADMIN", Description = "Admin" },
            new Role { Id = 3, Name = "SUPER_ADMIN", Description = "Super Admin" });

        context.Grades.AddRange(
            new Grade { Id = 1, Name = "6°", SequenceOrder = 1 },
            new Grade { Id = 2, Name = "7°", SequenceOrder = 2 },
            new Grade { Id = 3, Name = "8°", SequenceOrder = 3 },
            new Grade { Id = 6, Name = "11°", SequenceOrder = 6 });

        context.ElectionPositions.AddRange(
            new ElectionPosition { Id = 1, Name = "Personero", Description = "Personero", Status = "ACTIVO" });

        context.PositionRequirements.AddRange(
            new PositionRequirement { Id = 1, PositionId = 1, Description = "Certificado 10°", IsMandatory = true, DisplayOrder = 1 },
            new PositionRequirement { Id = 2, PositionId = 1, Description = "Carta compromiso", IsMandatory = true, DisplayOrder = 2 });

        context.SaveChanges();
        return context;
    }

    private async Task<uint> InsertUserAsync(
        string fullName,
        string email,
        string roleName = "ELECTOR",
        string status = "ACTIVO",
        byte? gradeId = 1)
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
                (@roleId, @gradeId, @docHash, @encDoc, @fullName, @email, '$2b$11$dummy', 0, @status);
            SELECT LAST_INSERT_ID();";

        cmd.Parameters.AddWithValue("@roleId", roleId);
        cmd.Parameters.AddWithValue("@gradeId", (object?)gradeId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@docHash", Guid.NewGuid().ToString("N"));
        cmd.Parameters.AddWithValue("@encDoc", "ENC:" + doc);
        cmd.Parameters.AddWithValue("@fullName", fullName);
        cmd.Parameters.AddWithValue("@email", email);
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
        string status = "PENDIENTE",
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

    private async Task InsertCandidacyDocumentAsync(uint candidateId, uint requirementId, string fileUrl)
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO `candidacy_documents`
                (`candidate_id`, `requirement_id`, `file_url`, `uploaded_at`)
            VALUES
                (@candidateId, @requirementId, @fileUrl, NOW());";

        cmd.Parameters.AddWithValue("@candidateId", candidateId);
        cmd.Parameters.AddWithValue("@requirementId", requirementId);
        cmd.Parameters.AddWithValue("@fileUrl", fileUrl);

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

    private static CensusService CreateCensusService(WahlMiraiDbContext context)
    {
        var audit = new FakeAuditService();
        return new CensusService(
            context,
            new FakeAuthService(),
            audit,
            new FakeEncryptionService(),
            new CredentialService(
                context,
                new PendingPasswordStore(),
                audit,
                new PasswordResetTokenStore(new MemoryCache(new MemoryCacheOptions()), TimeProvider.System),
                new FakeLoginAttemptStore(),
                new ActiveSessionStore()),
            new ActiveSessionStore());
    }

    #endregion

    #region CandidateReviewService Tests

    [MySqlIntegrationFact]
    public async Task CandidateReviewService_RelationalBranch_MatchesInMemoryFallback()
    {
        // Rama relacional (línea ~23 con vw_pending_candidacies) vs fallback en memoria (línea ~133)
        var eventId = await InsertVotingEventAsync("Elección Review", positionId: 1);
        var u1 = await InsertUserAsync("Carlos Candidato", "carlos@test.com", gradeId: 1);
        var u2 = await InsertUserAsync("Beatriz Candidata", "beatriz@test.com", gradeId: 2);

        var cand1 = await InsertCandidateAsync(eventId, u1, "Carlos Candidato", "PENDIENTE");
        var cand2 = await InsertCandidateAsync(eventId, u2, "Beatriz Candidata", "PENDIENTE");

        // Subir 1 documento para el candidato 1 (requisito obligatorio 1)
        await InsertCandidacyDocumentAsync(cand1, 1, "https://storage/doc1.pdf");

        // Ejecutar en la base relacional real (IsRelational() == true)
        using var relationalContext = _fixture.CreateDbContext();
        Assert.True(relationalContext.Database.IsRelational());
        var relationalService = new CandidateReviewService(relationalContext, new FakeAuditService());
        var relationalResults = await relationalService.GetCandidatesForReviewAsync(eventId, "PENDIENTE");

        // Sembrar los mismos datos en un contexto InMemory (IsRelational() == false)
        using var inMemoryContext = CreateInMemoryDbContext();
        Assert.False(inMemoryContext.Database.IsRelational());

        var imEvent = new VotingEvent
        {
            Id = eventId,
            Title = "Elección Review",
            PositionId = 1,
            CreatedByVoterId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)),
            RegistrationEndTime = new TimeOnly(18, 0),
            ProposalsStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            ProposalsEndTime = new TimeOnly(18, 0),
            VotingStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            VotingEndTime = new TimeOnly(18, 0),
            ElectionType = "PERSONAS",
            Status = "ACTIVA"
        };
        inMemoryContext.VotingEvents.Add(imEvent);

        var imUser1 = new Voter { Id = u1, FullName = "Carlos Candidato", ContactEmail = "carlos@test.com", GradeId = 1, RoleId = 1, Status = "ACTIVO", DocumentHash = "h1", EncryptedDocument = "e1", PasswordHash = "$2b$11$dummy" };
        var imUser2 = new Voter { Id = u2, FullName = "Beatriz Candidata", ContactEmail = "beatriz@test.com", GradeId = 2, RoleId = 1, Status = "ACTIVO", DocumentHash = "h2", EncryptedDocument = "e2", PasswordHash = "$2b$11$dummy" };
        inMemoryContext.Voters.AddRange(imUser1, imUser2);

        var imCand1 = new Candidate { Id = cand1, VotingEventId = eventId, VoterId = u1, Name = "Carlos Candidato", Status = "PENDIENTE", EnrolledAt = DateTime.UtcNow.AddMinutes(-5) };
        var imCand2 = new Candidate { Id = cand2, VotingEventId = eventId, VoterId = u2, Name = "Beatriz Candidata", Status = "PENDIENTE", EnrolledAt = DateTime.UtcNow.AddMinutes(-10) };
        inMemoryContext.Candidates.AddRange(imCand1, imCand2);

        inMemoryContext.CandidacyDocuments.Add(new CandidacyDocument { CandidateId = cand1, RequirementId = 1, FileUrl = "https://storage/doc1.pdf", UploadedAt = DateTime.UtcNow });
        await inMemoryContext.SaveChangesAsync();

        var inMemoryService = new CandidateReviewService(inMemoryContext, new FakeAuditService());
        var inMemoryResults = await inMemoryService.GetCandidatesForReviewAsync(eventId, "PENDIENTE");

        // Comparar equivalencia entre ambas ramas
        Assert.Equal(inMemoryResults.Count, relationalResults.Count);

        for (int i = 0; i < relationalResults.Count; i++)
        {
            var r = relationalResults[i];
            var im = inMemoryResults.Single(x => x.CandidateId == r.CandidateId);

            Assert.Equal(im.CandidateName, r.CandidateName);
            Assert.Equal(im.Status, r.Status);
            Assert.Equal(im.MandatoryCount, r.MandatoryCount);
            Assert.Equal(im.MandatoryUploadedCount, r.MandatoryUploadedCount);
            Assert.Equal(im.IsEligible, r.IsEligible);
            Assert.Equal(im.Requirements.Count, r.Requirements.Count);
        }

        // Verificaciones puntuales del candidato con 1 de 2 documentos obligatorios
        var rCand1 = relationalResults.Single(c => c.CandidateId == cand1);
        Assert.Equal(2, rCand1.MandatoryCount);
        Assert.Equal(1, rCand1.MandatoryUploadedCount);
        Assert.False(rCand1.IsEligible); // Requiere 2, tiene 1

        var rCand2 = relationalResults.Single(c => c.CandidateId == cand2);
        Assert.Equal(2, rCand2.MandatoryCount);
        Assert.Equal(0, rCand2.MandatoryUploadedCount);
        Assert.False(rCand2.IsEligible);
    }

    [MySqlIntegrationFact]
    public async Task CandidateReviewService_RelationalBranch_HandlesEligibilityWhenAllMandatoryUploaded()
    {
        var eventId = await InsertVotingEventAsync("Elección Elegible", positionId: 1);
        var u1 = await InsertUserAsync("Candidato Completo", "completo@test.com");
        var candId = await InsertCandidateAsync(eventId, u1, "Candidato Completo", "PENDIENTE");

        // Subir ambos requisitos obligatorios (1 y 2)
        await InsertCandidacyDocumentAsync(candId, 1, "https://storage/doc1.pdf");
        await InsertCandidacyDocumentAsync(candId, 2, "https://storage/doc2.pdf");

        using var context = _fixture.CreateDbContext();
        var service = new CandidateReviewService(context, new FakeAuditService());
        var results = await service.GetCandidatesForReviewAsync(eventId, "PENDIENTE");

        var c = Assert.Single(results);
        Assert.Equal(2, c.MandatoryCount);
        Assert.Equal(2, c.MandatoryUploadedCount);
        Assert.True(c.IsEligible);
    }

    [MySqlIntegrationFact]
    public async Task CandidateReviewService_GetCandidateReviewDetailAsync_RelationalBranch_ReturnsCorrectDetail()
    {
        var eventId = await InsertVotingEventAsync("Elección Detalle Review", positionId: 1);
        var u1 = await InsertUserAsync("Diana Candidata", "diana.c@test.com", gradeId: 1);
        var candId = await InsertCandidateAsync(eventId, u1, "Diana Candidata", "PENDIENTE");

        await InsertCandidacyDocumentAsync(candId, 1, "https://storage/diana_doc1.pdf");

        using var relationalContext = _fixture.CreateDbContext();
        var service = new CandidateReviewService(relationalContext, new FakeAuditService());
        var detail = await service.GetCandidateReviewDetailAsync(candId);

        Assert.NotNull(detail);
        Assert.Equal(candId, detail.CandidateId);
        Assert.Equal("Diana Candidata", detail.CandidateName);
        Assert.Equal("Elección Detalle Review", detail.EventTitle);
        Assert.Equal("PENDIENTE", detail.Status);
        Assert.Equal(2, detail.MandatoryCount);
        Assert.Equal(1, detail.MandatoryUploadedCount);
        Assert.False(detail.IsEligible);
        Assert.Equal(2, detail.Requirements.Count);

        var docReq1 = detail.Requirements.Single(r => r.RequirementId == 1);
        Assert.True(docReq1.IsUploaded);
        Assert.Equal("https://storage/diana_doc1.pdf", docReq1.FileUrl);

        var docReq2 = detail.Requirements.Single(r => r.RequirementId == 2);
        Assert.False(docReq2.IsUploaded);
    }

    [MySqlIntegrationFact]
    public async Task CandidateReviewService_RelationalBranch_HandlesMixedCandidateStatuses()
    {
        // Prueba la coexistencia de candidatos PENDIENTE (que leen vw_pending_candidacies)
        // y candidatos APROBADO (que usan el fallback BuildCandidateDto en la misma consulta)
        var eventId = await InsertVotingEventAsync("Elección Estados Mixtos", positionId: 1);
        var u1 = await InsertUserAsync("Candidato Pendiente Mixto", "cp.mixto@test.com", gradeId: 1);
        var u2 = await InsertUserAsync("Candidato Aprobado Mixto", "ca.mixto@test.com", gradeId: 2);

        var candPend = await InsertCandidateAsync(eventId, u1, "Candidato Pendiente Mixto", "PENDIENTE");
        var candApr = await InsertCandidateAsync(eventId, u2, "Candidato Aprobado Mixto", "APROBADO");

        await InsertCandidacyDocumentAsync(candPend, 1, "https://storage/doc1.pdf");
        await InsertCandidacyDocumentAsync(candApr, 1, "https://storage/doc1.pdf");
        await InsertCandidacyDocumentAsync(candApr, 2, "https://storage/doc2.pdf");

        using var relationalContext = _fixture.CreateDbContext();
        var relationalService = new CandidateReviewService(relationalContext, new FakeAuditService());
        var relationalResults = await relationalService.GetCandidatesForReviewAsync(eventId, status: null);

        // En InMemory
        using var inMemoryContext = CreateInMemoryDbContext();
        inMemoryContext.VotingEvents.Add(new VotingEvent
        {
            Id = eventId,
            Title = "Elección Estados Mixtos",
            PositionId = 1,
            CreatedByVoterId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-4)),
            RegistrationEndTime = new TimeOnly(18, 0),
            ProposalsStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-3)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            ProposalsEndTime = new TimeOnly(18, 0),
            VotingStartDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            VotingEndTime = new TimeOnly(18, 0),
            ElectionType = "PERSONAS",
            Status = "ACTIVA"
        });
        inMemoryContext.Voters.AddRange(
            new Voter { Id = u1, FullName = "Candidato Pendiente Mixto", ContactEmail = "cp.mixto@test.com", GradeId = 1, RoleId = 1, Status = "ACTIVO", DocumentHash = "hm1", EncryptedDocument = "em1", PasswordHash = "$2b$11$dummy" },
            new Voter { Id = u2, FullName = "Candidato Aprobado Mixto", ContactEmail = "ca.mixto@test.com", GradeId = 2, RoleId = 1, Status = "ACTIVO", DocumentHash = "hm2", EncryptedDocument = "em2", PasswordHash = "$2b$11$dummy" });
        inMemoryContext.Candidates.AddRange(
            new Candidate { Id = candPend, VotingEventId = eventId, VoterId = u1, Name = "Candidato Pendiente Mixto", Status = "PENDIENTE", EnrolledAt = DateTime.UtcNow.AddMinutes(-5) },
            new Candidate { Id = candApr, VotingEventId = eventId, VoterId = u2, Name = "Candidato Aprobado Mixto", Status = "APROBADO", EnrolledAt = DateTime.UtcNow.AddMinutes(-10) });
        inMemoryContext.CandidacyDocuments.AddRange(
            new CandidacyDocument { CandidateId = candPend, RequirementId = 1, FileUrl = "https://storage/doc1.pdf", UploadedAt = DateTime.UtcNow },
            new CandidacyDocument { CandidateId = candApr, RequirementId = 1, FileUrl = "https://storage/doc1.pdf", UploadedAt = DateTime.UtcNow },
            new CandidacyDocument { CandidateId = candApr, RequirementId = 2, FileUrl = "https://storage/doc2.pdf", UploadedAt = DateTime.UtcNow });
        await inMemoryContext.SaveChangesAsync();

        var inMemoryService = new CandidateReviewService(inMemoryContext, new FakeAuditService());
        var inMemoryResults = await inMemoryService.GetCandidatesForReviewAsync(eventId, status: null);

        Assert.Equal(2, relationalResults.Count);
        Assert.Equal(inMemoryResults.Count, relationalResults.Count);

        var rPend = relationalResults.Single(c => c.CandidateId == candPend);
        var imPend = inMemoryResults.Single(c => c.CandidateId == candPend);
        Assert.Equal(imPend.IsEligible, rPend.IsEligible);
        Assert.False(rPend.IsEligible);

        var rApr = relationalResults.Single(c => c.CandidateId == candApr);
        var imApr = inMemoryResults.Single(c => c.CandidateId == candApr);
        Assert.Equal(imApr.IsEligible, rApr.IsEligible);
        Assert.True(rApr.IsEligible);
    }

    #endregion

    #region CensusService Active Census Tests

    [MySqlIntegrationFact]
    public async Task CensusService_ActiveCensusRelationalBranch_MatchesInMemoryFallback()
    {
        // Rama relacional (línea ~134 con vw_active_census) vs fallback en memoria (línea ~216)
        var u1 = await InsertUserAsync("Ana Elector", "ana@test.com", "ELECTOR", "ACTIVO", gradeId: 1);
        var u2 = await InsertUserAsync("Bernardo Elector", "bernardo@test.com", "ELECTOR", "INACTIVO", gradeId: 2);
        var u3 = await InsertUserAsync("Carlos Eliminado", "carlos@test.com", "ELECTOR", "ELIMINADO", gradeId: 1);

        using var relationalContext = _fixture.CreateDbContext();
        Assert.True(relationalContext.Database.IsRelational());
        var relationalCensusService = CreateCensusService(relationalContext);
        var relationalPaged = await relationalCensusService.GetVotersPagedAsync(pageNumber: 1, pageSize: 20);

        // En InMemory
        using var inMemoryContext = CreateInMemoryDbContext();
        inMemoryContext.Voters.AddRange(
            new Voter { Id = 2, FullName = "Ana María López Pérez", ContactEmail = "acudiente.ana.lopez@example.com", GradeId = 6, RoleId = 1, Status = "ACTIVO", DocumentHash = "hseed2", EncryptedDocument = "eseed2", PasswordHash = "$2b$11$dummy" },
            new Voter { Id = u1, FullName = "Ana Elector", ContactEmail = "ana@test.com", GradeId = 1, RoleId = 1, Status = "ACTIVO", DocumentHash = "h1", EncryptedDocument = "e1", PasswordHash = "$2b$11$dummy" },
            new Voter { Id = u2, FullName = "Bernardo Elector", ContactEmail = "bernardo@test.com", GradeId = 2, RoleId = 1, Status = "INACTIVO", DocumentHash = "h2", EncryptedDocument = "e2", PasswordHash = "$2b$11$dummy" },
            new Voter { Id = u3, FullName = "Carlos Eliminado", ContactEmail = "carlos@test.com", GradeId = 1, RoleId = 1, Status = "ELIMINADO", DocumentHash = "h3", EncryptedDocument = "e3", PasswordHash = "$2b$11$dummy" });
        await inMemoryContext.SaveChangesAsync();

        var inMemoryCensusService = CreateCensusService(inMemoryContext);
        var inMemoryPaged = await inMemoryCensusService.GetVotersPagedAsync(status: "ACTIVO", pageNumber: 1, pageSize: 20);

        // Filtrando por ACTIVO en ambas ramas
        var relationalActivoPaged = await relationalCensusService.GetVotersPagedAsync(status: "ACTIVO", pageNumber: 1, pageSize: 20);

        Assert.Equal(inMemoryPaged.TotalCount, relationalActivoPaged.TotalCount);
        var imAna = inMemoryPaged.Items.Single(x => x.FullName == "Ana Elector");
        var rAna = relationalActivoPaged.Items.Single(x => x.FullName == "Ana Elector");

        Assert.Equal(imAna.FullName, rAna.FullName);
        Assert.Equal(imAna.GradeName, rAna.GradeName);
        Assert.Equal(imAna.Status, rAna.Status);
    }

    [MySqlIntegrationFact]
    public async Task CensusService_ActiveCensusRelationalBranch_SupportsFilteringAndPaging()
    {
        var u1 = await InsertUserAsync("Zoe Gomez", "zoe@test.com", "ELECTOR", "ACTIVO", gradeId: 1);
        var u2 = await InsertUserAsync("Andres Perez", "andres@test.com", "ELECTOR", "ACTIVO", gradeId: 2);
        var u3 = await InsertUserAsync("Carlos Gomez", "carlos.g@test.com", "ELECTOR", "ACTIVO", gradeId: 1);

        using var context = _fixture.CreateDbContext();
        var service = CreateCensusService(context);

        // Filtrar por término de búsqueda "Gomez"
        var searchResult = await service.GetVotersPagedAsync(search: "Gomez", pageNumber: 1, pageSize: 10);
        Assert.Equal(2, searchResult.TotalCount);
        Assert.All(searchResult.Items, i => Assert.Contains("Gomez", i.FullName));

        // Filtrar por grado "7°" (id 2)
        var gradeResult = await service.GetVotersPagedAsync(grade: "7°", pageNumber: 1, pageSize: 10);
        Assert.Contains(gradeResult.Items, i => i.FullName == "Andres Perez");
        Assert.All(gradeResult.Items, i => Assert.Equal("7°", i.GradeName));
    }

    #endregion

    #region CensusService Pending Whitelist Tests

    [MySqlIntegrationFact]
    public async Task CensusService_PendingWhitelistRelationalBranch_MatchesInMemoryFallback()
    {
        // Rama relacional (línea ~929 con vw_pending_whitelist) vs fallback en memoria (línea ~991)
        var baseTime = DateTime.UtcNow.AddMinutes(-30);
        var w1 = await InsertWhitelistEntryAsync("Entrada Uno", "uno@test.com", gradeId: 1, claimedAt: null, createdAt: baseTime);
        var w2 = await InsertWhitelistEntryAsync("Entrada Dos", "dos@test.com", gradeId: 2, claimedAt: null, createdAt: baseTime.AddMinutes(5));
        var wClaimed = await InsertWhitelistEntryAsync("Entrada Reclamada", "rec@test.com", gradeId: 1, claimedAt: DateTime.UtcNow);

        using var relationalContext = _fixture.CreateDbContext();
        Assert.True(relationalContext.Database.IsRelational());
        var relationalService = CreateCensusService(relationalContext);
        var relationalPaged = await relationalService.GetPendingWhitelistPagedAsync(pageNumber: 1, pageSize: 20);

        // En InMemory
        using var inMemoryContext = CreateInMemoryDbContext();
        inMemoryContext.CensusWhitelists.AddRange(
            new CensusWhitelist { Id = 1, FullName = "Sofía Ramírez Torres", ContactEmail = "sofia.ramirez@colegio.edu.co", GradeId = 6, ClaimedAt = null, CreatedAt = baseTime.AddHours(-1), DocumentHash = "hseed", EncryptedDocument = "eseed" },
            new CensusWhitelist { Id = w1, FullName = "Entrada Uno", ContactEmail = "uno@test.com", GradeId = 1, ClaimedAt = null, CreatedAt = baseTime, DocumentHash = "h1", EncryptedDocument = "e1" },
            new CensusWhitelist { Id = w2, FullName = "Entrada Dos", ContactEmail = "dos@test.com", GradeId = 2, ClaimedAt = null, CreatedAt = baseTime.AddMinutes(5), DocumentHash = "h2", EncryptedDocument = "e2" },
            new CensusWhitelist { Id = wClaimed, FullName = "Entrada Reclamada", ContactEmail = "rec@test.com", GradeId = 1, ClaimedAt = DateTime.UtcNow, CreatedAt = baseTime, DocumentHash = "h3", EncryptedDocument = "e3" });
        await inMemoryContext.SaveChangesAsync();

        var inMemoryService = CreateCensusService(inMemoryContext);
        var inMemoryPaged = await inMemoryService.GetPendingWhitelistPagedAsync(pageNumber: 1, pageSize: 20);

        // Ambos excluyen la entrada reclamada
        Assert.Equal(inMemoryPaged.TotalCount, relationalPaged.TotalCount);

        var rItem1 = relationalPaged.Items.Single(x => x.Id == w1);
        var imItem1 = inMemoryPaged.Items.Single(x => x.Id == w1);

        Assert.Equal(imItem1.FullName, rItem1.FullName);
        Assert.Equal(imItem1.ContactEmail, rItem1.ContactEmail);
        Assert.Equal(imItem1.GradeName, rItem1.GradeName);
    }

    [MySqlIntegrationFact]
    public async Task CensusService_PendingWhitelistRelationalBranch_SupportsFilteringAndPaging()
    {
        var w1 = await InsertWhitelistEntryAsync("Diana Navarro", "diana@test.com", gradeId: 1);
        var w2 = await InsertWhitelistEntryAsync("Esteban Navarro", "esteban@test.com", gradeId: 2);
        var w3 = await InsertWhitelistEntryAsync("Felipe Gomez", "felipe@test.com", gradeId: 1);

        using var context = _fixture.CreateDbContext();
        var service = CreateCensusService(context);

        // Filtrar por search "Navarro"
        var searchResult = await service.GetPendingWhitelistPagedAsync(search: "Navarro", pageNumber: 1, pageSize: 10);
        Assert.Equal(2, searchResult.TotalCount);
        Assert.All(searchResult.Items, i => Assert.Contains("Navarro", i.FullName));

        // Filtrar por gradeId 1
        var gradeResult = await service.GetPendingWhitelistPagedAsync(gradeId: 1, pageNumber: 1, pageSize: 10);
        Assert.Contains(gradeResult.Items, i => i.FullName == "Diana Navarro");
        Assert.Contains(gradeResult.Items, i => i.FullName == "Felipe Gomez");
        Assert.DoesNotContain(gradeResult.Items, i => i.FullName == "Esteban Navarro");
    }

    #endregion

    #region ResultsController Tests

    [MySqlIntegrationFact]
    public async Task ResultsController_IndexAndGetLiveData_ReadsFromVwVoteCounts()
    {
        // Línea ~71 de ResultsController: lee VwVoteCounts
        var eventId = await InsertVotingEventAsync("Elección de Resultados Controlador", status: "ACTIVA");
        var u1 = await InsertUserAsync("Candidato Uno", "cand1@test.com");
        var u2 = await InsertUserAsync("Candidato Dos", "cand2@test.com");

        var c1 = await InsertCandidateAsync(eventId, u1, "Candidato Uno", "APROBADO");
        var c2 = await InsertCandidateAsync(eventId, u2, "Candidato Dos", "APROBADO");

        // 2 votos a C1, 1 voto a C2 => Total 3
        await InsertVoteAsync(eventId, c1);
        await InsertVoteAsync(eventId, c1);
        await InsertVoteAsync(eventId, c2);

        using var context = _fixture.CreateDbContext();
        var votingService = new FakeVotingService();
        var controller = new ResultsController(context, votingService);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "1"),
            new(ClaimTypes.Role, "ADMIN")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);
        var httpContext = new DefaultHttpContext { User = user };

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new EmptyTempDataProvider());

        // 1. Probar Index(id) -> ViewResult con List<VwVoteCount>
        var actionResult = await controller.Index((int)eventId);
        var viewResult = Assert.IsType<ViewResult>(actionResult);
        var model = Assert.IsAssignableFrom<List<VwVoteCount>>(viewResult.Model);

        Assert.Equal(2, model.Count);
        var top = model[0];
        Assert.Equal(c1, top.CandidateId);
        Assert.Equal(2, top.TotalVotes);
        Assert.Equal(66.67m, top.VotePercentage);

        var runnerUp = model[1];
        Assert.Equal(c2, runnerUp.CandidateId);
        Assert.Equal(1, runnerUp.TotalVotes);
        Assert.Equal(33.33m, runnerUp.VotePercentage);

        // 2. Probar GetLiveData(id) -> JsonResult
        var liveActionResult = await controller.GetLiveData((int)eventId);
        var jsonResult = Assert.IsType<JsonResult>(liveActionResult);
        Assert.NotNull(jsonResult.Value);

        var jsonString = JsonSerializer.Serialize(jsonResult.Value);
        using var jsonDoc = JsonDocument.Parse(jsonString);
        var root = jsonDoc.RootElement;

        Assert.True(root.GetProperty("ok").GetBoolean());
        Assert.Equal((int)eventId, root.GetProperty("eventId").GetInt32());
        Assert.Equal(3, root.GetProperty("totalVotes").GetInt64());
    }

    #endregion

    #region EmailQueueBackgroundService Tests

    [MySqlIntegrationFact]
    public async Task EmailQueueBackgroundService_ProcessesPendingEmailUsingVwPendingEmailQueue()
    {
        // Línea ~71 de EmailQueueBackgroundService: lee dbContext.VwPendingEmailQueues
        var u = await InsertUserAsync("Destinatario Cola", "destinatario@test.com");
        var baseTime = DateTime.UtcNow.AddMinutes(-20);

        // q1: PENDIENTE, 0 intentos (creado hace 20 min) -> Debe ser elegido primero
        var q1 = await InsertEmailQueueAsync(u, emailType: "RECUPERACION_ACCESO", status: "PENDIENTE", attempts: 0, createdAt: baseTime);
        // q2: PENDIENTE, 1 intento (creado hace 10 min)
        var q2 = await InsertEmailQueueAsync(u, emailType: "RECUPERACION_ACCESO", status: "PENDIENTE", attempts: 1, createdAt: baseTime.AddMinutes(10));
        // qMax: PENDIENTE, 3 intentos (alcanzó límite MaxAttempts = 3, no debe ser procesado)
        var qMax = await InsertEmailQueueAsync(u, emailType: "RECUPERACION_ACCESO", status: "PENDIENTE", attempts: 3, createdAt: baseTime.AddMinutes(-5));
        // qEnviado: ENVIADO
        var qEnviado = await InsertEmailQueueAsync(u, emailType: "RECUPERACION_ACCESO", status: "ENVIADO", attempts: 1, createdAt: baseTime.AddMinutes(-10));

        var passwordStore = new PendingPasswordStore();
        passwordStore.StorePassword(q1, "Secret#2026!");

        var emailSender = new TestEmailSender();

        var services = new ServiceCollection();
        services.AddSingleton<IPendingPasswordStore>(passwordStore);
        services.AddSingleton<IEmailSender>(emailSender);
        services.AddScoped<WahlMiraiDbContext>(_ => _fixture.CreateDbContext());

        await using var serviceProvider = services.BuildServiceProvider();
        var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();

        var worker = new EmailQueueBackgroundService(
            scopeFactory,
            Options.Create(new EmailSettings { RateLimitPerMinute = 20, BaseUrl = "https://localhost:5001" }),
            NullLogger<EmailQueueBackgroundService>.Instance);

        // Invocar ProcessQueueAsync a través de reflexión
        var method = typeof(EmailQueueBackgroundService).GetMethod("ProcessQueueAsync", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        var task = (Task)method.Invoke(worker, new object[] { CancellationToken.None })!;
        await task;

        // Verificar que el correo fue despachado
        Assert.Single(emailSender.SentEmails);
        Assert.Equal("destinatario@test.com", emailSender.SentEmails[0]);

        // Verificar en la base de datos real que q1 fue actualizado a ENVIADO
        await using (var conn = _fixture.CreateRawConnection())
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT `status`, `attempts` FROM `email_queue` WHERE `id` = @id;";
            cmd.Parameters.AddWithValue("@id", q1);
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("ENVIADO", reader.GetString("status"));
            Assert.Equal(1, reader.GetByte("attempts"));
        }

        // Verificar que q2 sigue en PENDIENTE
        await using (var conn = _fixture.CreateRawConnection())
        {
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT `status`, `attempts` FROM `email_queue` WHERE `id` = @id;";
            cmd.Parameters.AddWithValue("@id", q2);
            await using var reader = await cmd.ExecuteReaderAsync();
            Assert.True(await reader.ReadAsync());
            Assert.Equal("PENDIENTE", reader.GetString("status"));
            Assert.Equal(1, reader.GetByte("attempts"));
        }
    }

    #endregion

    private class FakeVotingService : IVotingService
    {
        public bool HasVotedReturnValue { get; set; } = true;
        public Task<bool> HasVotedAsync(int voterId, int eventId) => Task.FromResult(HasVotedReturnValue);
        public Task<bool> CastVoteAsync(int voterId, int eventId, int candidateId, string ipAddress) => Task.FromResult(true);
        public Task<List<VotingEvent>> GetActiveEventsForVoterAsync(int voterId) => Task.FromResult(new List<VotingEvent>());
        public Task<List<VotingEvent>> GetEventsForVoterDashboardAsync(int voterId) => Task.FromResult(new List<VotingEvent>());
        public Task<List<Candidate>> GetCandidatesForEventAsync(int eventId) => Task.FromResult(new List<Candidate>());
        public Task<string?> GetVoterGradeNameAsync(int voterId) => Task.FromResult<string?>("11°");
    }

    private async Task InsertVoteAsync(uint eventId, uint candidateId)
    {
        await using var conn = _fixture.CreateRawConnection();
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();

        cmd.CommandText = @"
            INSERT INTO `votes` (`voting_event_id`, `candidate_id`, `vote_hash`, `voted_at`)
            VALUES (@eventId, @candidateId, @voteHash, NOW());";

        cmd.Parameters.AddWithValue("@eventId", eventId);
        cmd.Parameters.AddWithValue("@candidateId", candidateId);
        cmd.Parameters.AddWithValue("@voteHash", Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));

        await cmd.ExecuteNonQueryAsync();
    }
}
