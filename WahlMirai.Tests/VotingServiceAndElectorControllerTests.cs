// ============================================================
// Archivo:       VotingServiceAndElectorControllerTests.cs
// Módulo:        M05 — Emisión de Voto, Tarjetón y Controlador Elector
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias para VotingService (validaciones P3, voto válido,
//                voto en blanco, anonimato RN-3, filtrado de tarjetón) y ElectorController
//                (Dashboard, Votar, Propuestas, ConfirmarVoto y control de etapas).
// Requerimientos: RF-M05-00, RF-M05-01, RN-3, RN-4.1, RN-12.
// ============================================================
using System.Net;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Hubs;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias de <see cref="VotingService"/> y <see cref="ElectorController"/>
/// sobre EF Core InMemory y <see cref="FakeTimeProvider"/> (RF-M05-00, RF-M05-01, RN-3, RN-4.1, RN-12).
/// </summary>
public sealed class VotingServiceAndElectorControllerTests
{
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

    private sealed class InMemoryTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();

        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var kv in values)
            {
                if (kv.Value != null)
                {
                    _data[kv.Key] = kv.Value;
                }
            }
        }
    }

    private sealed class FakeCandidacyService : ICandidacyService
    {
        public List<MyPostulationDto> Postulations { get; set; } = new();

        public Task<List<PostulableEventDto>> GetEligibleEventsForPostulationAsync(int voterId)
            => Task.FromResult(new List<PostulableEventDto>());

        public Task<PostulationFormDto?> GetPostulationFormDetailAsync(int eventId, int voterId)
            => Task.FromResult<PostulationFormDto?>(null);

        public Task<PostulationResult> SubmitPostulationAsync(PostulationSubmitDto dto, int voterId, string clientIp, string webRootPath)
            => Task.FromResult(new PostulationResult { Success = false });

        public Task<List<MyPostulationDto>> GetMyPostulationsAsync(int voterId)
            => Task.FromResult(Postulations);
    }

    private sealed class StubVotingService : IVotingService
    {
        public List<VotingEvent> DashboardEvents { get; set; } = new();
        public List<VotingEvent> ActiveEvents { get; set; } = new();
        public List<Candidate> Candidates { get; set; } = new();
        public bool HasVoted { get; set; }

        public Task<bool> HasVotedAsync(int voterId, int eventId) => Task.FromResult(HasVoted);
        public Task<bool> CastVoteAsync(int voterId, int eventId, int candidateId, string ipAddress) => Task.FromResult(false);
        public Task<List<VotingEvent>> GetActiveEventsForVoterAsync(int voterId) => Task.FromResult(ActiveEvents);
        public Task<List<VotingEvent>> GetEventsForVoterDashboardAsync(int voterId) => Task.FromResult(DashboardEvents);
        public Task<List<Candidate>> GetCandidatesForEventAsync(int eventId) => Task.FromResult(Candidates);
        public Task<string?> GetVoterGradeNameAsync(int voterId) => Task.FromResult<string?>("11°");
    }

    private static WahlMiraiDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new WahlMiraiDbContext(options);

        context.Roles.AddRange(
            new Role { Id = 1, Name = "ELECTOR", Description = "Elector" },
            new Role { Id = 2, Name = "ADMIN", Description = "Admin" },
            new Role { Id = 3, Name = "SUPER_ADMIN", Description = "Super Admin" });

        context.Grades.AddRange(
            new Grade { Id = 10, Name = "10°", SequenceOrder = 5, IsLastGrade = false },
            new Grade { Id = 11, Name = "11°", SequenceOrder = 6, IsLastGrade = true });

        context.ElectionPositions.Add(
            new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

        context.SaveChanges();
        return context;
    }

    private static FakeTimeProvider CreateLocalFakeTimeProvider(DateTime localDateTime)
    {
        var unspecified = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);
        var offset = TimeZoneInfo.Local.GetUtcOffset(unspecified);
        var localDto = new DateTimeOffset(unspecified, offset);
        return new FakeTimeProvider(localDto.ToUniversalTime());
    }

    private static VotingEvent BuildEventWithDayWindows(
        uint id,
        DateOnly day,
        string status = "ACTIVA",
        DateTime? deletedAt = null)
    {
        return new VotingEvent
        {
            Id = id,
            CreatedByVoterId = 1,
            PositionId = 1,
            Title = $"Elección {id}",
            ElectionType = "PERSONAS",
            Status = status,
            DeletedAt = deletedAt,
            CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            RegistrationStartDate = day,
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = day,
            RegistrationEndTime = new TimeOnly(10, 0),
            ProposalsStartDate = day,
            ProposalsStartTime = new TimeOnly(10, 0),
            ProposalsEndDate = day,
            ProposalsEndTime = new TimeOnly(12, 0),
            VotingStartDate = day,
            VotingStartTime = new TimeOnly(12, 0),
            VotingEndDate = day,
            VotingEndTime = new TimeOnly(18, 0)
        };
    }

    private static Voter BuildVoter(uint id, byte? gradeId = 11, string status = "ACTIVO", DateTime? deletedAt = null, byte roleId = 1)
    {
        return new Voter
        {
            Id = id,
            RoleId = roleId,
            GradeId = gradeId,
            DocumentHash = $"hash-{id}".PadRight(64, '0'),
            EncryptedDocument = $"enc-{id}",
            FullName = $"Elector {id}",
            ContactEmail = $"elector{id}@test.local",
            PasswordHash = "$2b$11$dummy",
            Status = status,
            DeletedAt = deletedAt
        };
    }

    private static Candidate BuildCandidate(uint id, uint eventId, string status = "APROBADO", bool isBlankVote = false, uint? voterId = null)
    {
        return new Candidate
        {
            Id = id,
            VotingEventId = eventId,
            VoterId = isBlankVote ? null : voterId,
            Name = isBlankVote ? "Voto en Blanco" : $"Candidato {id}",
            IsBlankVote = isBlankVote,
            Status = status,
            EnrolledAt = new DateTime(2026, 6, 1, 9, 0, 0, DateTimeKind.Utc)
        };
    }

    private static VotingService CreateVotingService(WahlMiraiDbContext context, FakeTimeProvider timeProvider, IAuditService? auditService = null)
    {
        return new VotingService(
            context,
            auditService ?? new FakeAuditService(),
            new NoOpHubContext(),
            timeProvider);
    }

    private static ElectorController CreateElectorController(
        IVotingService votingService,
        ICandidacyService? candidacyService = null,
        uint? userId = 100)
    {
        var controller = new ElectorController(votingService, candidacyService ?? new FakeCandidacyService());

        var claims = new List<Claim>();
        if (userId.HasValue)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            claims.Add(new Claim(ClaimTypes.Role, "ELECTOR"));
        }

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(claims, userId.HasValue ? "TestAuth" : null))
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.25");

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider());
        return controller;
    }

    private static readonly DateOnly EventDay = new(2026, 6, 15);
    private static readonly DateTime DuringVotingLocal = new(2026, 6, 15, 14, 0, 0);

    // ─── 1. P3 Rejections in VotingService ────────────────────────────────────────────────────

    /// <summary>Rechaza identificadores inválidos (<= 0) en CastVoteDetailedAsync y CastVoteAsync (P3).</summary>
    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(-1, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, -5, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, -2)]
    public async Task CastVote_InvalidIds_ReturnsInvalidRequestAndFalse(int voterId, int eventId, int candidateId)
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(DuringVotingLocal);
        var service = CreateVotingService(context, clock);

        var detailed = await service.CastVoteDetailedAsync(voterId, eventId, candidateId, "127.0.0.1");
        var legacyBool = await service.CastVoteAsync(voterId, eventId, candidateId, "127.0.0.1");

        Assert.False(detailed.Success);
        Assert.Equal("INVALID_REQUEST", detailed.ErrorCode);
        Assert.False(legacyBool);
        Assert.Empty(context.Votes);
        Assert.Empty(context.VoterEventParticipations);
    }

    /// <summary>Rechaza voto cuando el elector no existe, está INACTIVO o tiene DeletedAt asignado (P3).</summary>
    [Theory]
    [InlineData(false, "ACTIVO", false)]
    [InlineData(true, "INACTIVO", false)]
    [InlineData(true, "ELIMINADO", false)]
    [InlineData(true, "ACTIVO", true)]
    public async Task CastVote_MissingInactiveOrSoftDeletedVoter_ReturnsVoterIneligible(bool exists, string status, bool isSoftDeleted)
    {
        await using var context = CreateInMemoryContext();
        var ev = BuildEventWithDayWindows(1, EventDay, "ACTIVA");
        context.VotingEvents.Add(ev);
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.Add(BuildCandidate(10, 1, "APROBADO", voterId: 200));
        if (exists)
        {
            context.Voters.Add(BuildVoter(100, 11, status, isSoftDeleted ? new DateTime(2026, 6, 10, 0, 0, 0, DateTimeKind.Utc) : null));
        }
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));

        var detailed = await service.CastVoteDetailedAsync(100, 1, 10, "127.0.0.1");
        var legacy = await service.CastVoteAsync(100, 1, 10, "127.0.0.1");

        Assert.False(detailed.Success);
        Assert.Equal("VOTER_INELIGIBLE", detailed.ErrorCode);
        Assert.False(legacy);
        Assert.Empty(context.Votes);
    }

    /// <summary>Rechaza voto cuando el evento no existe, está ELIMINADO o tiene DeletedAt (P3, RN-7.1).</summary>
    [Theory]
    [InlineData(false, "ACTIVA", false)]
    [InlineData(true, "ELIMINADO", false)]
    [InlineData(true, "ACTIVA", true)]
    public async Task CastVote_MissingOrEliminatedEvent_ReturnsEventNotFound(bool exists, string status, bool isSoftDeleted)
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, 11));
        if (exists)
        {
            context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, status, isSoftDeleted ? new DateTime(2026, 6, 12, 0, 0, 0, DateTimeKind.Utc) : null));
            context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
            context.Candidates.Add(BuildCandidate(10, 1, "APROBADO", isBlankVote: true));
        }
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));

        var detailed = await service.CastVoteDetailedAsync(100, 1, 10, "127.0.0.1");
        var legacy = await service.CastVoteAsync(100, 1, 10, "127.0.0.1");

        Assert.False(detailed.Success);
        Assert.Equal("EVENT_NOT_FOUND", detailed.ErrorCode);
        Assert.False(legacy);
        Assert.Empty(context.Votes);
    }

    /// <summary>Rechaza voto cuando el elector tiene GradeId nulo o su grado no pertenece a event_grades (P3).</summary>
    [Theory]
    [InlineData(null)]
    [InlineData((byte)10)]
    public async Task CastVote_NullOrUnassignedGrade_ReturnsGradeNotEligible(byte? voterGradeId)
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, voterGradeId));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, "ACTIVA"));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.Add(BuildCandidate(10, 1, "APROBADO", isBlankVote: true));
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));

        var detailed = await service.CastVoteDetailedAsync(100, 1, 10, "127.0.0.1");
        var legacy = await service.CastVoteAsync(100, 1, 10, "127.0.0.1");

        Assert.False(detailed.Success);
        Assert.Equal("GRADE_NOT_ELIGIBLE", detailed.ErrorCode);
        Assert.False(legacy);
        Assert.Empty(context.Votes);
    }

    /// <summary>Rechaza voto cuando el proceso no está en etapa ACTIVA según el reloj inyectado (P3, RN-12).</summary>
    [Theory]
    [InlineData("PROGRAMADA", 7, 30)]
    [InlineData("INSCRIPCION", 9, 0)]
    [InlineData("PROPUESTAS", 11, 0)]
    [InlineData("FINALIZADA", 19, 0)]
    [InlineData("ACTIVA", 18, 0)] // Exactamente al cerrar votación => FINALIZADA
    public async Task CastVote_EventNotInActiveStage_ReturnsEventNotActive(string dbStatus, int hour, int minute)
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, 11));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, dbStatus));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.Add(BuildCandidate(10, 1, "APROBADO", isBlankVote: true));
        await context.SaveChangesAsync();

        var clock = CreateLocalFakeTimeProvider(new DateTime(2026, 6, 15, hour, minute, 0));
        var service = CreateVotingService(context, clock);

        var detailed = await service.CastVoteDetailedAsync(100, 1, 10, "127.0.0.1");
        var legacy = await service.CastVoteAsync(100, 1, 10, "127.0.0.1");

        Assert.False(detailed.Success);
        Assert.Equal("EVENT_NOT_ACTIVE", detailed.ErrorCode);
        Assert.False(legacy);
        Assert.Empty(context.Votes);
    }

    /// <summary>Rechaza voto cuando un evento inicialmente ACTIVA avanza en FakeTimeProvider más allá de VotingEnd (P2, P3, RN-12).</summary>
    [Fact]
    public async Task CastVote_ActiveEventCrossesVotingEndViaFakeTimeProvider_RejectsVote()
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, 11));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, "ACTIVA"));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.Add(BuildCandidate(10, 1, "APROBADO", isBlankVote: true));
        await context.SaveChangesAsync();

        var clock = CreateLocalFakeTimeProvider(new DateTime(2026, 6, 15, 17, 59, 59));
        var service = CreateVotingService(context, clock);

        clock.Advance(TimeSpan.FromSeconds(1));

        var detailed = await service.CastVoteDetailedAsync(100, 1, 10, "127.0.0.1");

        Assert.False(detailed.Success);
        Assert.Equal("EVENT_NOT_ACTIVE", detailed.ErrorCode);
        Assert.Empty(context.Votes);
    }

    /// <summary>Rechaza voto si el candidato no existe, pertenece a otro evento o su estado no es APROBADO (P3).</summary>
    [Theory]
    [InlineData(999u, "APROBADO", 1u, false)]
    [InlineData(20u, "APROBADO", 2u, true)]
    [InlineData(21u, "PENDIENTE", 1u, true)]
    [InlineData(22u, "RECHAZADO", 1u, true)]
    [InlineData(23u, "RETIRADO", 1u, true)]
    public async Task CastVote_InvalidOrUnapprovedCandidate_ReturnsInvalidCandidate(
        uint candidateId, string candidateStatus, uint candidateEventId, bool seedCandidate)
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, 11));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, "ACTIVA"));
        context.VotingEvents.Add(BuildEventWithDayWindows(2, EventDay, "ACTIVA"));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.EventGrades.Add(new EventGrade { Id = 2, VotingEventId = 2, GradeId = 11 });

        if (seedCandidate)
        {
            context.Candidates.Add(BuildCandidate(candidateId, candidateEventId, candidateStatus, voterId: 200));
        }

        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));

        var detailed = await service.CastVoteDetailedAsync(100, 1, (int)candidateId, "127.0.0.1");
        var legacy = await service.CastVoteAsync(100, 1, (int)candidateId, "127.0.0.1");

        Assert.False(detailed.Success);
        Assert.Equal("INVALID_CANDIDATE", detailed.ErrorCode);
        Assert.False(legacy);
        Assert.Empty(context.Votes);
    }

    /// <summary>Rechaza segundo voto del mismo elector en el mismo evento (HasVotedAsync / ALREADY_VOTED, RN-3).</summary>
    [Fact]
    public async Task CastVote_DuplicateVote_ReturnsAlreadyVotedAndPersistsSingleVote()
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, 11));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, "ACTIVA"));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.Add(BuildCandidate(10, 1, "APROBADO", voterId: 200));
        context.Candidates.Add(BuildCandidate(11, 1, "APROBADO", isBlankVote: true));
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));

        var first = await service.CastVoteDetailedAsync(100, 1, 10, "198.51.100.1");
        var second = await service.CastVoteDetailedAsync(100, 1, 11, "198.51.100.1");
        var secondBool = await service.CastVoteAsync(100, 1, 11, "198.51.100.1");

        Assert.True(first.Success);
        Assert.False(second.Success);
        Assert.Equal("ALREADY_VOTED", second.ErrorCode);
        Assert.False(secondBool);
        Assert.True(await service.HasVotedAsync(100, 1));
        Assert.Equal(1, await context.Votes.CountAsync());
        Assert.Equal(1, await context.VoterEventParticipations.CountAsync());
    }

    // ─── 2. Valid Voting & RN-3 Anonymity ─────────────────────────────────────────────────────

    /// <summary>Permite voto válido por candidato APROBADO, garantiza anonimato RN-3 y audita VOTE_CAST sin identidad del candidato.</summary>
    [Fact]
    public async Task CastVote_ApprovedCandidate_PersistsAnonymousVoteParticipationAndAuditWithoutCandidateIdentity()
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, 11));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, "ACTIVA"));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.Add(BuildCandidate(10, 1, "APROBADO", voterId: 200));
        await context.SaveChangesAsync();

        var clock = CreateLocalFakeTimeProvider(DuringVotingLocal);
        var audit = new AuditService(context);
        var service = CreateVotingService(context, clock, audit);

        var result = await service.CastVoteDetailedAsync(100, 1, 10, "198.51.100.77");

        Assert.True(result.Success);
        Assert.Null(result.ErrorCode);

        // RN-3: la entidad Vote no posee ninguna propiedad ni clave foránea que apunte al elector/usuario
        var votePropertyNames = typeof(Vote).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .ToList();
        Assert.DoesNotContain(votePropertyNames, n => n.Contains("Voter", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(votePropertyNames, n => n.Contains("User", StringComparison.OrdinalIgnoreCase));

        var savedVote = await context.Votes.SingleAsync();
        Assert.Equal(1u, savedVote.VotingEventId);
        Assert.Equal(10u, savedVote.CandidateId);
        Assert.Equal(64, savedVote.VoteHash.Length);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, savedVote.VotedAt);

        var participation = await context.VoterEventParticipations.SingleAsync();
        Assert.Equal(100u, participation.VoterId);
        Assert.Equal(1u, participation.VotingEventId);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, participation.ParticipatedAt);

        var auditLog = await context.AuditLogs.SingleAsync();
        Assert.Equal("VOTE_CAST", auditLog.Action);
        Assert.Equal(100u, auditLog.VoterId);
        Assert.Equal("votes", auditLog.TargetEntity);
        Assert.Null(auditLog.TargetId);
        Assert.Equal("Event: 1", auditLog.Details);
        Assert.Equal("198.51.100.77", auditLog.IpAddress);
        Assert.DoesNotContain("10", auditLog.Details);
        Assert.DoesNotContain("Candidato", auditLog.Details, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Permite voto válido en blanco (IsBlankVote = true, Status = APROBADO) mediante CastVoteAsync (RF-M05-00, RN-3).</summary>
    [Fact]
    public async Task CastVoteAsync_ApprovedBlankVote_SucceedsAndRecordsParticipation()
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(101, 11));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, "ACTIVA"));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.Add(BuildCandidate(11, 1, "APROBADO", isBlankVote: true));
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));

        var ok = await service.CastVoteAsync(101, 1, 11, "198.51.100.88");

        Assert.True(ok);
        var vote = await context.Votes.SingleAsync();
        Assert.Equal(11u, vote.CandidateId);
        Assert.True(await service.HasVotedAsync(101, 1));
    }

    // ─── 3. Ballot Filtering & Dashboard Queries ──────────────────────────────────────────────

    /// <summary>GetCandidatesForEventAsync retorna solo candidatos APROBADO y voto en blanco del evento (RF-M05-00).</summary>
    [Fact]
    public async Task GetCandidatesForEventAsync_ReturnsOnlyApprovedAndBlankVoteForEvent()
    {
        await using var context = CreateInMemoryContext();
        context.Voters.AddRange(BuildVoter(201, 11), BuildVoter(202, 11), BuildVoter(203, 11), BuildVoter(204, 11));
        context.VotingEvents.AddRange(
            BuildEventWithDayWindows(1, EventDay, "ACTIVA"),
            BuildEventWithDayWindows(2, EventDay, "ACTIVA"));

        context.Candidates.AddRange(
            BuildCandidate(1, 1, "APROBADO", voterId: 201),
            BuildCandidate(2, 1, "APROBADO", isBlankVote: true),
            BuildCandidate(3, 1, "PENDIENTE", voterId: 202),
            BuildCandidate(4, 1, "RECHAZADO", voterId: 203),
            BuildCandidate(5, 1, "RETIRADO", voterId: 204),
            BuildCandidate(6, 2, "APROBADO", isBlankVote: true));
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));

        var ballot = await service.GetCandidatesForEventAsync(1);

        Assert.Equal(2, ballot.Count);
        Assert.Contains(ballot, c => c.Id == 1 && !c.IsBlankVote);
        Assert.Contains(ballot, c => c.Id == 2 && c.IsBlankVote);
        Assert.DoesNotContain(ballot, c => c.Status != "APROBADO" || c.VotingEventId != 1);
    }

    /// <summary>GetActiveEventsForVoterAsync y GetEventsForVoterDashboardAsync filtran por grado, estado no eliminado y transición de etapa (RN-4.1, RN-12).</summary>
    [Fact]
    public async Task GetActiveAndDashboardEvents_FilterByGradeDeletedAndStageTransition()
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, 11));

        // Evento 1: ACTIVA para grado 11°
        var evActive = BuildEventWithDayWindows(1, EventDay, "PROPUESTAS");
        // Evento 2: INSCRIPCION para grado 11° (fecha futura de propuestas/votación)
        var evInsc = BuildEventWithDayWindows(2, EventDay.AddDays(1), "PROGRAMADA");
        evInsc.RegistrationStartDate = EventDay;
        evInsc.RegistrationStartTime = new TimeOnly(8, 0);
        // Evento 3: FINALIZADA para grado 11°
        var evFin = BuildEventWithDayWindows(3, EventDay.AddDays(-1), "ACTIVA");
        // Evento 4: PROGRAMADA futura para grado 11° (excluida en dashboard y activos)
        var evProg = BuildEventWithDayWindows(4, EventDay.AddDays(5), "PROGRAMADA");
        // Evento 5: ELIMINADO para grado 11° (nunca visible)
        var evDel = BuildEventWithDayWindows(5, EventDay, "ELIMINADO");
        // Evento 6: ACTIVA pero solo para grado 10° (no visible para elector de 11°)
        var evOtherGrade = BuildEventWithDayWindows(6, EventDay, "ACTIVA");

        context.VotingEvents.AddRange(evActive, evInsc, evFin, evProg, evDel, evOtherGrade);
        context.EventGrades.AddRange(
            new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 },
            new EventGrade { Id = 2, VotingEventId = 2, GradeId = 11 },
            new EventGrade { Id = 3, VotingEventId = 3, GradeId = 11 },
            new EventGrade { Id = 4, VotingEventId = 4, GradeId = 11 },
            new EventGrade { Id = 5, VotingEventId = 5, GradeId = 11 },
            new EventGrade { Id = 6, VotingEventId = 6, GradeId = 10 });
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));

        var active = await service.GetActiveEventsForVoterAsync(100);
        var dashboard = await service.GetEventsForVoterDashboardAsync(100);

        Assert.Single(active);
        Assert.Equal(1u, active[0].Id);
        Assert.Equal("ACTIVA", active[0].Status);

        Assert.Equal(3, dashboard.Count);
        Assert.Contains(dashboard, e => e.Id == 1 && e.Status == "ACTIVA");
        Assert.Contains(dashboard, e => e.Id == 2 && e.Status == "INSCRIPCION");
        Assert.Contains(dashboard, e => e.Id == 3 && e.Status == "FINALIZADA");
        Assert.DoesNotContain(dashboard, e => e.Id is 4u or 5u or 6u);
    }

    // ─── 4. ElectorController: Dashboard, Votar, Propuestas, ConfirmarVoto ────────────────────

    /// <summary>Dashboard calcula métricas y carga eventos elegibles para el elector autenticado (RF-M05-00, RN-4.1).</summary>
    [Fact]
    public async Task ElectorController_Dashboard_PopulatesViewBagMetricsAndEvents()
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, 11));
        context.VotingEvents.AddRange(
            BuildEventWithDayWindows(1, EventDay, "ACTIVA"),
            BuildEventWithDayWindows(2, EventDay.AddDays(-1), "FINALIZADA"));
        context.EventGrades.AddRange(
            new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 },
            new EventGrade { Id = 2, VotingEventId = 2, GradeId = 11 });
        context.VoterEventParticipations.Add(new VoterEventParticipation
        {
            Id = 1,
            VoterId = 100,
            VotingEventId = 2,
            ParticipatedAt = new DateTime(2026, 6, 14, 15, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));
        var controller = CreateElectorController(service);

        var result = await controller.Dashboard();

        Assert.IsType<ViewResult>(result);
        Assert.Equal("11°", (string?)controller.ViewData["GradeName"]);
        Assert.Equal(2, (int?)controller.ViewData["TotalEvents"]);
        Assert.Equal(1, (int?)controller.ViewData["TotalVoted"]);
        Assert.Equal(1, (int?)controller.ViewData["TotalActiveToVote"]);
        Assert.Equal(0, (int?)controller.ViewData["TotalInscriptions"]);
        Assert.Equal(50, (int?)controller.ViewData["ParticipationPercent"]);
        var eventsList = Assert.IsAssignableFrom<IEnumerable<object>>(controller.ViewData["Events"]);
        Assert.Equal(2, eventsList.Count());
    }

    /// <summary>Dashboard y ConfirmarVoto retornan Unauthorized cuando falta el claim NameIdentifier.</summary>
    [Fact]
    public async Task ElectorController_MissingUserClaim_ReturnsUnauthorized()
    {
        var controller = CreateElectorController(new StubVotingService(), userId: null);

        Assert.IsType<UnauthorizedResult>(await controller.Dashboard());
        Assert.IsType<UnauthorizedResult>(await controller.Votar(1));
        Assert.IsType<UnauthorizedResult>(await controller.Propuestas(1));
        Assert.IsType<UnauthorizedResult>(await controller.ConfirmarVoto(1, 1));
    }

    /// <summary>Votar en evento ACTIVA entrega solo candidatos APROBADO y voto en blanco en el tarjetón (RF-M05-00).</summary>
    [Fact]
    public async Task ElectorController_Votar_ActiveEvent_ReturnsBallotWithOnlyApprovedAndBlankVote()
    {
        await using var context = CreateInMemoryContext();
        context.Voters.AddRange(BuildVoter(100, 11), BuildVoter(201, 11), BuildVoter(202, 11));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, "ACTIVA"));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.AddRange(
            BuildCandidate(10, 1, "APROBADO", voterId: 201),
            BuildCandidate(11, 1, "APROBADO", isBlankVote: true),
            BuildCandidate(12, 1, "PENDIENTE", voterId: 202),
            BuildCandidate(13, 1, "RECHAZADO"),
            BuildCandidate(14, 1, "RETIRADO"));
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));
        var controller = CreateElectorController(service);

        var result = await controller.Votar(1);

        Assert.IsType<ViewResult>(result);
        var ballot = Assert.IsAssignableFrom<List<Candidate>>(controller.ViewData["Candidates"]);
        Assert.Equal(2, ballot.Count);
        Assert.All(ballot, c => Assert.Equal("APROBADO", c.Status));
        Assert.Contains(ballot, c => c.Id == 10 && !c.IsBlankVote);
        Assert.Contains(ballot, c => c.Id == 11 && c.IsBlankVote);
    }

    /// <summary>Votar aplica control de etapa (INSCRIPCION, PROPUESTAS, FINALIZADA, PROGRAMADA), grado no elegible y doble voto (RN-12, RF-M05-00).</summary>
    [Fact]
    public async Task ElectorController_Votar_StageGatingGradeAndDuplicateVote_RedirectsAccordingly()
    {
        // 1. Grado no habilitado / evento inexistente => Error + Dashboard
        await using (var context = CreateInMemoryContext())
        {
            context.Voters.Add(BuildVoter(100, 10));
            context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, "ACTIVA"));
            context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
            await context.SaveChangesAsync();

            var controller = CreateElectorController(CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal)));
            var redirect = Assert.IsType<RedirectToActionResult>(await controller.Votar(1));
            Assert.Equal(nameof(ElectorController.Dashboard), redirect.ActionName);
            Assert.NotNull(controller.TempData["Error"]);
        }

        // 2. INSCRIPCION sin postulación => Candidacy/Apply; con postulación => Candidacy/Status
        var candidacy = new FakeCandidacyService();
        var stubVoting = new StubVotingService
        {
            DashboardEvents = new List<VotingEvent> { BuildEventWithDayWindows(1, EventDay, "INSCRIPCION") }
        };
        var ctrlInsc = CreateElectorController(stubVoting, candidacy);
        var redirApply = Assert.IsType<RedirectToActionResult>(await ctrlInsc.Votar(1));
        Assert.Equal("Apply", redirApply.ActionName);
        Assert.Equal("Candidacy", redirApply.ControllerName);

        candidacy.Postulations.Add(new MyPostulationDto { CandidateId = 77, EventId = 1 });
        var redirStatus = Assert.IsType<RedirectToActionResult>(await ctrlInsc.Votar(1));
        Assert.Equal("Status", redirStatus.ActionName);
        Assert.Equal("Candidacy", redirStatus.ControllerName);

        // 3. PROPUESTAS => Elector/Propuestas
        stubVoting.DashboardEvents = new List<VotingEvent> { BuildEventWithDayWindows(1, EventDay, "PROPUESTAS") };
        var redirProp = Assert.IsType<RedirectToActionResult>(await CreateElectorController(stubVoting).Votar(1));
        Assert.Equal(nameof(ElectorController.Propuestas), redirProp.ActionName);

        // 4. FINALIZADA => Results/Index
        stubVoting.DashboardEvents = new List<VotingEvent> { BuildEventWithDayWindows(1, EventDay, "FINALIZADA") };
        var redirFin = Assert.IsType<RedirectToActionResult>(await CreateElectorController(stubVoting).Votar(1));
        Assert.Equal("Index", redirFin.ActionName);
        Assert.Equal("Results", redirFin.ControllerName);

        // 5. PROGRAMADA => Dashboard + TempData["Info"]
        stubVoting.DashboardEvents = new List<VotingEvent> { BuildEventWithDayWindows(1, EventDay, "PROGRAMADA") };
        var ctrlProg = CreateElectorController(stubVoting);
        var redirProg = Assert.IsType<RedirectToActionResult>(await ctrlProg.Votar(1));
        Assert.Equal(nameof(ElectorController.Dashboard), redirProg.ActionName);
        Assert.NotNull(ctrlProg.TempData["Info"]);

        // 6. ACTIVA pero ya votó => Results/Index + TempData["Info"]
        stubVoting.DashboardEvents = new List<VotingEvent> { BuildEventWithDayWindows(1, EventDay, "ACTIVA") };
        stubVoting.HasVoted = true;
        var ctrlVoted = CreateElectorController(stubVoting);
        var redirVoted = Assert.IsType<RedirectToActionResult>(await ctrlVoted.Votar(1));
        Assert.Equal("Index", redirVoted.ActionName);
        Assert.Equal("Results", redirVoted.ControllerName);
        Assert.NotNull(ctrlVoted.TempData["Info"]);
    }

    /// <summary>Propuestas permite consulta en PROPUESTAS/ACTIVA/FINALIZADA con candidatos APROBADO+blanco y bloquea INSCRIPCION/PROGRAMADA (RN-12).</summary>
    [Theory]
    [InlineData("PROPUESTAS", true)]
    [InlineData("ACTIVA", true)]
    [InlineData("FINALIZADA", true)]
    [InlineData("INSCRIPCION", false)]
    [InlineData("PROGRAMADA", false)]
    public async Task ElectorController_Propuestas_StageGatingAndApprovedBallot(string stage, bool shouldRenderView)
    {
        var stubVoting = new StubVotingService
        {
            DashboardEvents = new List<VotingEvent> { BuildEventWithDayWindows(1, EventDay, stage) },
            Candidates = new List<Candidate>
            {
                BuildCandidate(10, 1, "APROBADO", voterId: 201),
                BuildCandidate(11, 1, "APROBADO", isBlankVote: true)
            }
        };

        var controller = CreateElectorController(stubVoting);
        var result = await controller.Propuestas(1);

        if (shouldRenderView)
        {
            Assert.IsType<ViewResult>(result);
            var candidates = Assert.IsAssignableFrom<List<Candidate>>(controller.ViewData["Candidates"]);
            Assert.Equal(2, candidates.Count);
        }
        else
        {
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(nameof(ElectorController.Dashboard), redirect.ActionName);
            Assert.NotNull(controller.TempData["Info"]);
        }
    }

    /// <summary>ConfirmarVoto con voto válido establece TempData["Success"] y redirige a Dashboard (RF-M05-00).</summary>
    [Fact]
    public async Task ElectorController_ConfirmarVoto_ValidVote_SetsTempDataSuccessAndRedirectsToDashboard()
    {
        await using var context = CreateInMemoryContext();
        context.Voters.Add(BuildVoter(100, 11));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, "ACTIVA"));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.Add(BuildCandidate(10, 1, "APROBADO", voterId: 200));
        await context.SaveChangesAsync();

        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(DuringVotingLocal));
        var controller = CreateElectorController(service);

        var result = await controller.ConfirmarVoto(1, 10);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ElectorController.Dashboard), redirect.ActionName);
        Assert.NotNull(controller.TempData["Success"]);
        Assert.Null(controller.TempData["Error"]);
        Assert.Equal(1, await context.Votes.CountAsync());
        Assert.Equal(1, await context.VoterEventParticipations.CountAsync());
    }

    /// <summary>ConfirmarVoto ante rechazos P3 (etapa incorrecta, grado no habilitado, candidato inválido o voto duplicado) fija TempData["Error"] y redirige a Dashboard sin lanzar excepción (P3).</summary>
    [Theory]
    [InlineData("PROPUESTAS", 11, 10, false)]
    [InlineData("ACTIVA", 10, 10, false)]
    [InlineData("ACTIVA", 11, 999, false)]
    [InlineData("ACTIVA", 11, 12, false)] // Candidato PENDIENTE
    [InlineData("ACTIVA", 11, 10, true)]  // Voto duplicado
    public async Task ElectorController_ConfirmarVoto_P3Failure_SetsTempDataErrorAndRedirectsWithoutThrowing(
        string stage, byte voterGradeId, int candidateId, bool alreadyVoted)
    {
        await using var context = CreateInMemoryContext();
        context.Voters.AddRange(BuildVoter(100, voterGradeId), BuildVoter(201, 11), BuildVoter(202, 11));
        context.VotingEvents.Add(BuildEventWithDayWindows(1, EventDay, stage));
        context.EventGrades.Add(new EventGrade { Id = 1, VotingEventId = 1, GradeId = 11 });
        context.Candidates.AddRange(
            BuildCandidate(10, 1, "APROBADO", voterId: 201),
            BuildCandidate(12, 1, "PENDIENTE", voterId: 202));
        if (alreadyVoted)
        {
            context.VoterEventParticipations.Add(new VoterEventParticipation
            {
                Id = 1,
                VoterId = 100,
                VotingEventId = 1,
                ParticipatedAt = new DateTime(2026, 6, 15, 13, 0, 0, DateTimeKind.Utc)
            });
        }
        await context.SaveChangesAsync();

        var time = stage == "PROPUESTAS"
            ? new DateTime(2026, 6, 15, 11, 0, 0)
            : DuringVotingLocal;
        var service = CreateVotingService(context, CreateLocalFakeTimeProvider(time));
        var controller = CreateElectorController(service);

        var result = await controller.ConfirmarVoto(1, candidateId);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ElectorController.Dashboard), redirect.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Null(controller.TempData["Success"]);
        Assert.Empty(context.Votes);
    }

    /// <summary>Documenta el estado actual de ElectorController.ConfirmarVoto sin atributo ValidateAntiForgeryToken (R3).</summary>
    [Fact]
    public void ElectorController_ConfirmarVoto_CurrentBehaviorLacksValidateAntiForgeryTokenAttribute()
    {
        // NOTA: R3 — ElectorController.ConfirmarVoto ([HttpPost]) actualmente carece de [ValidateAntiForgeryToken];
        // fuera de alcance de P1–P4, se registra el comportamiento actual para seguimiento posterior.
        var method = typeof(ElectorController).GetMethod(nameof(ElectorController.ConfirmarVoto));
        Assert.NotNull(method);
        var antiforgery = method!.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>();
        Assert.Null(antiforgery);
    }
}
