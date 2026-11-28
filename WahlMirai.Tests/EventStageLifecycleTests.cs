// ============================================================
// Archivo:       EventStageLifecycleTests.cs
// Módulo:        M03 / M05 — Ciclo de Vida de Etapas Electorales
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias deterministas para el ciclo de vida de etapas
//                electorales en EventService y VotingService usando FakeTimeProvider:
//                fronteras exactas (>=), ventanas contiguas RN-12, rango monótono
//                (no regreso a PROGRAMADA), inmutabilidad en FINALIZADA (RN-7.2) y
//                eliminación lógica en cualquier etapa (RN-7.1).
// Requerimientos: RF-M03-00, RF-M03-01, RF-M05-00, RN-4.1, RN-7.1, RN-7.2, RN-12.
// ============================================================
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Hubs;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias deterministas del ciclo de vida de etapas electorales en
/// <see cref="EventService"/> y <see cref="VotingService"/> sobre EF Core InMemory y
/// <see cref="FakeTimeProvider"/> (RF-M03-00, RF-M03-01, RF-M05-00, RN-4.1, RN-7.1, RN-7.2, RN-12).
/// </summary>
public sealed class EventStageLifecycleTests
{
    private static readonly DateOnly EventDay = new(2026, 6, 15);
    private static readonly DateTime RegStartLocal = EventDay.ToDateTime(new TimeOnly(8, 0));
    private static readonly DateTime RegEndLocal = EventDay.ToDateTime(new TimeOnly(10, 0));
    private static readonly DateTime PropStartLocal = EventDay.ToDateTime(new TimeOnly(10, 0));
    private static readonly DateTime PropEndLocal = EventDay.ToDateTime(new TimeOnly(12, 0));
    private static readonly DateTime VotStartLocal = EventDay.ToDateTime(new TimeOnly(12, 0));
    private static readonly DateTime VotEndLocal = EventDay.ToDateTime(new TimeOnly(18, 0));

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

    private static WahlMiraiDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new WahlMiraiDbContext(options);

        context.Roles.AddRange(
            new Role { Id = Roles.Elector, Name = Roles.ElectorName, Description = "Elector" },
            new Role { Id = Roles.Admin, Name = Roles.AdminName, Description = "Admin" },
            new Role { Id = Roles.SuperAdmin, Name = Roles.SuperAdminName, Description = "Super Admin" });

        context.Grades.AddRange(
            new Grade { Id = 10, Name = "10°", SequenceOrder = 5, IsLastGrade = false },
            new Grade { Id = 11, Name = "11°", SequenceOrder = 6, IsLastGrade = true });

        context.ElectionPositions.AddRange(
            new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ElectionPosition { Id = 2, Name = "Contralor", Status = "ACTIVO", CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) });

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

    private static EventService CreateEventService(
        WahlMiraiDbContext context,
        FakeTimeProvider timeProvider,
        FakeAuditService? auditService = null)
    {
        return new EventService(context, auditService ?? new FakeAuditService(), timeProvider);
    }

    private static VotingService CreateVotingService(
        WahlMiraiDbContext context,
        FakeTimeProvider timeProvider,
        FakeAuditService? auditService = null)
    {
        return new VotingService(context, auditService ?? new FakeAuditService(), new NoOpHubContext(), timeProvider);
    }

    private static VotingEvent BuildContiguousEvent(
        uint id = 0,
        DateOnly? day = null,
        string status = "PROGRAMADA",
        string electionType = "PERSONAS",
        uint positionId = 1,
        uint createdByVoterId = 1,
        string title = "Elección Personero 2026")
    {
        var d = day ?? EventDay;
        return new VotingEvent
        {
            Id = id,
            CreatedByVoterId = createdByVoterId,
            PositionId = positionId,
            Title = title,
            Description = "Proceso electoral institucional",
            ElectionType = electionType,
            Status = status,
            CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            RegistrationStartDate = d,
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = d,
            RegistrationEndTime = new TimeOnly(10, 0),
            ProposalsStartDate = d,
            ProposalsStartTime = new TimeOnly(10, 0),
            ProposalsEndDate = d,
            ProposalsEndTime = new TimeOnly(12, 0),
            VotingStartDate = d,
            VotingStartTime = new TimeOnly(12, 0),
            VotingEndDate = d,
            VotingEndTime = new TimeOnly(18, 0)
        };
    }

    private static Voter BuildVoter(uint id, byte? gradeId = 11, byte roleId = Roles.Elector, string status = "ACTIVO")
    {
        return new Voter
        {
            Id = id,
            RoleId = roleId,
            GradeId = gradeId,
            DocumentHash = $"doc-hash-{id}".PadRight(64, '0'),
            EncryptedDocument = $"enc-doc-{id}",
            FullName = $"Usuario {id}",
            ContactEmail = $"usuario{id}@test.local",
            PasswordHash = "$2b$11$dummyhash",
            Status = status
        };
    }

    private static Candidate BuildApprovedCandidate(uint id, uint eventId, bool isBlankVote = false, uint? voterId = null)
    {
        return new Candidate
        {
            Id = id,
            VotingEventId = eventId,
            VoterId = isBlankVote ? null : voterId,
            Name = isBlankVote ? "Voto en Blanco" : $"Candidato {id}",
            IsBlankVote = isBlankVote,
            Status = "APROBADO",
            EnrolledAt = new DateTime(2026, 6, 15, 8, 30, 0, DateTimeKind.Utc)
        };
    }

    // =========================================================================
    // 1. EventService.CalculateStatus: fronteras exactas (>=) y persistencia
    // =========================================================================

    /// <summary>
    /// Verifica que <see cref="EventService.CalculateStatus"/> evalúa las fronteras con
    /// precisión de ticks y milisegundos (<c>&gt;=</c>) en todas las transiciones (RN-12).
    /// </summary>
    [Theory]
    [InlineData(8, 0, -1L, "PROGRAMADA")]
    [InlineData(8, 0, 0L, "INSCRIPCION")]
    [InlineData(8, 0, TimeSpan.TicksPerMillisecond, "INSCRIPCION")]
    [InlineData(10, 0, -1L, "INSCRIPCION")]
    [InlineData(10, 0, 0L, "PROPUESTAS")]
    [InlineData(10, 0, TimeSpan.TicksPerMillisecond, "PROPUESTAS")]
    [InlineData(12, 0, -1L, "PROPUESTAS")]
    [InlineData(12, 0, 0L, "ACTIVA")]
    [InlineData(12, 0, TimeSpan.TicksPerMillisecond, "ACTIVA")]
    [InlineData(18, 0, -1L, "ACTIVA")]
    [InlineData(18, 0, 0L, "FINALIZADA")]
    [InlineData(18, 0, TimeSpan.TicksPerMillisecond, "FINALIZADA")]
    public void CalculateStatus_ExactStageBoundaries_TransitionsAtExactInstant(
        int hour, int minute, long tickDelta, string expectedStatus)
    {
        var ev = BuildContiguousEvent(id: 1, status: "PROGRAMADA");
        var instant = EventDay.ToDateTime(new TimeOnly(hour, minute)).AddTicks(tickDelta);

        var actual = EventService.CalculateStatus(ev, instant);

        Assert.Equal(expectedStatus, actual);
    }

    /// <summary>
    /// Verifica que <see cref="EventService.GetEventByIdAsync"/> y <see cref="EventService.GetEventsAsync"/>
    /// persisten automáticamente en BD cada transición de etapa al avanzar <see cref="FakeTimeProvider"/> (RN-12).
    /// </summary>
    [Fact]
    public async Task GetEventByIdAndGetEvents_AutoPersistStageTransitionsAsClockAdvances()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal.AddTicks(-1));
        var eventService = CreateEventService(context, clock);

        var ev1 = BuildContiguousEvent(id: 10, positionId: 1, status: "PROGRAMADA");
        var ev2 = BuildContiguousEvent(id: 11, positionId: 2, status: "PROGRAMADA");
        context.VotingEvents.AddRange(ev1, ev2);
        await context.SaveChangesAsync();

        // Antes de 08:00:00 -> PROGRAMADA
        var beforeReg = await eventService.GetEventByIdAsync(10);
        Assert.NotNull(beforeReg);
        Assert.Equal("PROGRAMADA", beforeReg.Status);

        // Exacto 08:00:00 -> INSCRIPCION (persistido vía GetEventByIdAsync)
        clock.Advance(TimeSpan.FromTicks(1));
        var atReg = await eventService.GetEventByIdAsync(10);
        Assert.Equal("INSCRIPCION", atReg!.Status);
        Assert.Equal("INSCRIPCION", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 10)).Status);

        // Exacto 10:00:00 -> PROPUESTAS (persistido vía GetEventsAsync para ambos eventos)
        clock.Advance(PropStartLocal - RegStartLocal);
        var atPropList = await eventService.GetEventsAsync();
        Assert.All(atPropList, e => Assert.Equal("PROPUESTAS", e.Status));
        Assert.Equal("PROPUESTAS", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 11)).Status);

        // Exacto 12:00:00 -> ACTIVA
        clock.Advance(VotStartLocal - PropStartLocal);
        var atVot = await eventService.GetEventByIdAsync(10);
        Assert.Equal("ACTIVA", atVot!.Status);

        // Exacto 18:00:00 -> FINALIZADA
        clock.Advance(VotEndLocal - VotStartLocal);
        var atEndList = await eventService.GetEventsAsync();
        Assert.All(atEndList, e => Assert.Equal("FINALIZADA", e.Status));
        Assert.Equal("FINALIZADA", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 10)).Status);
        Assert.Equal("FINALIZADA", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 11)).Status);
    }

    // =========================================================================
    // 2. Ventanas contiguas RN-12 y ValidateStageWindows (Create / Update)
    // =========================================================================

    /// <summary>
    /// Verifica que <see cref="EventService.CreateEventAsync"/> rechaza tipos de elección
    /// distintos a <c>PERSONAS</c> (RF-M03-01).
    /// </summary>
    [Fact]
    public async Task CreateEventAsync_RejectsNonPersonasElectionType()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal.AddHours(-1));
        var service = CreateEventService(context, clock);

        var ev = BuildContiguousEvent(electionType: "TEMAS");

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(ev, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("PERSONAS", ex.Message);
    }

    /// <summary>
    /// Verifica que <see cref="EventService.CreateEventAsync"/> rechaza votaciones que abarcan
    /// más de un día calendario (<c>VotingEndDate != VotingStartDate</c>) (RF-M03-01).
    /// </summary>
    [Fact]
    public async Task CreateEventAsync_RejectsMultiDayVoting()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal.AddHours(-1));
        var service = CreateEventService(context, clock);

        var ev = BuildContiguousEvent();
        ev.VotingEndDate = EventDay.AddDays(1);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(ev, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("un solo día", ex.Message);
    }

    /// <summary>
    /// Verifica la tolerancia exacta de 15 minutos en creación (<c>isNew == true</c>):
    /// rechaza <c>now.AddMinutes(-15).AddTicks(-1)</c> y acepta exactamente <c>now.AddMinutes(-15)</c> (RN-12).
    /// </summary>
    [Fact]
    public async Task CreateEventAsync_RegistrationStartTolerance15Minutes_EnforcesExactBoundary()
    {
        await using var context = CreateInMemoryContext();
        var nowLocal = RegStartLocal.AddMinutes(15);
        var clock = CreateLocalFakeTimeProvider(nowLocal);
        var service = CreateEventService(context, clock);

        // 1 tick antes de la tolerancia de -15 minutos -> rechazado
        var tooOld = BuildContiguousEvent();
        tooOld.RegistrationStartTime = TimeOnly.FromDateTime(nowLocal.AddMinutes(-15).AddTicks(-1));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(tooOld, new List<byte> { 11 }, "127.0.0.1"));

        // Exactamente now.AddMinutes(-15) -> aceptado con estado INSCRIPCION
        var exactBoundary = BuildContiguousEvent();
        var created = await service.CreateEventAsync(exactBoundary, new List<byte> { 11 }, "127.0.0.1");
        Assert.Equal("INSCRIPCION", created.Status);
    }

    /// <summary>
    /// Verifica que <see cref="EventService.CreateEventAsync"/> rechaza ventanas de duración cero
    /// o invertidas (<c>end &lt;= start</c>) en cualquiera de las tres etapas (RN-12).
    /// </summary>
    [Theory]
    [InlineData("REG_EQUAL")]
    [InlineData("REG_INVERTED")]
    [InlineData("PROP_EQUAL")]
    [InlineData("PROP_INVERTED")]
    [InlineData("VOT_EQUAL")]
    [InlineData("VOT_INVERTED")]
    public async Task CreateEventAsync_RejectsZeroLengthOrInvertedStageBounds(string invalidCase)
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal.AddHours(-1));
        var service = CreateEventService(context, clock);

        var ev = BuildContiguousEvent();
        switch (invalidCase)
        {
            case "REG_EQUAL":
                ev.RegistrationEndTime = ev.RegistrationStartTime;
                ev.ProposalsStartTime = ev.RegistrationEndTime;
                break;
            case "REG_INVERTED":
                ev.RegistrationEndTime = ev.RegistrationStartTime.AddMinutes(-1);
                ev.ProposalsStartTime = ev.RegistrationEndTime;
                break;
            case "PROP_EQUAL":
                ev.ProposalsEndTime = ev.ProposalsStartTime;
                ev.VotingStartTime = ev.ProposalsEndTime;
                break;
            case "PROP_INVERTED":
                ev.ProposalsEndTime = ev.ProposalsStartTime.AddMinutes(-1);
                ev.VotingStartTime = ev.ProposalsEndTime;
                break;
            case "VOT_EQUAL":
                ev.VotingEndTime = ev.VotingStartTime;
                break;
            case "VOT_INVERTED":
                ev.VotingEndTime = ev.VotingStartTime.AddMinutes(-1);
                break;
        }

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(ev, new List<byte> { 11 }, "127.0.0.1"));

        Assert.Contains("posterior a su inicio", ex.Message);
    }

    /// <summary>
    /// Verifica que <see cref="EventService.CreateEventAsync"/> rechaza cualquier brecha (gap)
    /// o traslape (overlap) entre etapas contiguas, desde 1 tick hasta 1 minuto (RN-12).
    /// </summary>
    [Theory]
    [InlineData("PROP_START", 1L)]
    [InlineData("PROP_START", TimeSpan.TicksPerMinute)]
    [InlineData("PROP_START", -1L)]
    [InlineData("PROP_START", -TimeSpan.TicksPerMinute)]
    [InlineData("VOT_START", 1L)]
    [InlineData("VOT_START", TimeSpan.TicksPerMinute)]
    [InlineData("VOT_START", -1L)]
    [InlineData("VOT_START", -TimeSpan.TicksPerMinute)]
    public async Task CreateEventAsync_RejectsNonContiguousStageWindows_GapsAndOverlaps(
        string boundaryToShift, long tickOffset)
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal.AddHours(-1));
        var service = CreateEventService(context, clock);

        var ev = BuildContiguousEvent();
        if (boundaryToShift == "PROP_START")
        {
            var shifted = PropStartLocal.AddTicks(tickOffset);
            ev.ProposalsStartDate = DateOnly.FromDateTime(shifted);
            ev.ProposalsStartTime = TimeOnly.FromDateTime(shifted);
        }
        else
        {
            var shifted = VotStartLocal.AddTicks(tickOffset);
            ev.VotingStartDate = DateOnly.FromDateTime(shifted);
            ev.VotingStartTime = TimeOnly.FromDateTime(shifted);
        }

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(ev, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("RN-12", ex.Message);
    }

    /// <summary>
    /// Verifica que <see cref="EventService.CreateEventAsync"/> persiste <c>CreatedAt</c> UTC,
    /// genera el candidato <c>Voto en Blanco</c> aprobado, asocia <c>EventGrades</c>, registra auditoría
    /// <c>EVENT_CREATED</c> y bloquea una segunda elección activa para el mismo cargo hasta alcanzar <c>votEnd</c>
    /// o eliminar la anterior (RF-M03-00, RF-M03-01, RN-12).
    /// </summary>
    [Fact]
    public async Task CreateEventAsync_ValidContiguous_PersistsArtifactsAndEnforcesSingleActivePerPosition()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal.AddHours(-1));
        var audit = new FakeAuditService();
        var service = CreateEventService(context, clock, audit);

        var first = BuildContiguousEvent(positionId: 1, title: "Personero I");
        var created = await service.CreateEventAsync(first, new List<byte> { 10, 11 }, "10.0.0.5");

        Assert.Equal("PROGRAMADA", created.Status);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, created.CreatedAt);
        Assert.Contains("EVENT_CREATED", audit.Actions);

        var grades = await context.EventGrades.Where(eg => eg.VotingEventId == created.Id).Select(eg => eg.GradeId).ToListAsync();
        Assert.Equal(new byte[] { 10, 11 }, grades.OrderBy(g => g).ToArray());

        var blank = await context.Candidates.SingleAsync(c => c.VotingEventId == created.Id);
        Assert.Equal("Voto en Blanco", blank.Name);
        Assert.True(blank.IsBlankVote);
        Assert.Equal("APROBADO", blank.Status);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, blank.EnrolledAt);

        // Segundo evento para el mismo cargo bloqueado antes de votEnd (incluso 1 tick antes)
        clock.SetUtcNow(CreateLocalFakeTimeProvider(VotEndLocal.AddTicks(-1)).GetUtcNow());
        var secondBlocked = BuildContiguousEvent(day: EventDay.AddDays(1), positionId: 1, title: "Personero II");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateEventAsync(secondBlocked, new List<byte> { 11 }, "10.0.0.5"));

        // Al alcanzar votEnd (aún sin refrescar DB Status), CalculateStatus devuelve FINALIZADA y desbloquea
        clock.Advance(TimeSpan.FromTicks(1));
        var secondAllowed = BuildContiguousEvent(day: EventDay.AddDays(1), positionId: 1, title: "Personero II");
        var createdSecond = await service.CreateEventAsync(secondAllowed, new List<byte> { 11 }, "10.0.0.5");
        Assert.Equal("PROGRAMADA", createdSecond.Status);

        // Si el segundo evento se elimina (ELIMINADO), un tercero para el mismo cargo vuelve a estar permitido
        Assert.True(await service.SoftDeleteEventAsync(createdSecond.Id, "10.0.0.5"));
        var thirdAllowed = BuildContiguousEvent(day: EventDay.AddDays(2), positionId: 1, title: "Personero III");
        var createdThird = await service.CreateEventAsync(thirdAllowed, new List<byte> { 10 }, "10.0.0.5");
        Assert.Equal("PROGRAMADA", createdThird.Status);
    }

    /// <summary>
    /// Verifica que <see cref="EventService.UpdateEventAsync"/> permite <c>regStart &lt; now.AddMinutes(-15)</c>
    /// (<c>isNew == false</c>), sigue validando contigüidad RN-12, sincroniza <c>EventGrades</c>, registra
    /// <c>EVENT_UPDATED</c> y devuelve <c>null</c> para eventos inexistentes o en estado <c>ELIMINADO</c> (RN-12).
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_AllowsPastRegistrationStart_ValidatesContiguity_AndReturnsNullIfMissingOrEliminado()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal.AddHours(1)); // 09:00 -> regStart (08:00) < now - 15m
        var audit = new FakeAuditService();
        var service = CreateEventService(context, clock, audit);

        var existing = BuildContiguousEvent(id: 50, positionId: 1, status: "INSCRIPCION", title: "Original");
        var deleted = BuildContiguousEvent(id: 51, positionId: 2, status: "ELIMINADO", title: "Eliminado");
        deleted.DeletedAt = clock.GetUtcNow().UtcDateTime;
        context.VotingEvents.AddRange(existing, deleted);
        context.EventGrades.Add(new EventGrade { VotingEventId = 50, GradeId = 10 });
        await context.SaveChangesAsync();

        // Edición válida aunque regStart sea 1 hora atrás
        var validUpdate = BuildContiguousEvent(id: 50, positionId: 1, title: "Actualizado");
        validUpdate.VotingEndTime = new TimeOnly(19, 0);
        var updated = await service.UpdateEventAsync(validUpdate, new List<byte> { 11 }, "10.0.0.9");

        Assert.NotNull(updated);
        Assert.Equal("Actualizado", updated.Title);
        Assert.Equal("INSCRIPCION", updated.Status);
        Assert.Contains("EVENT_UPDATED", audit.Actions);
        var syncedGrades = await context.EventGrades.Where(eg => eg.VotingEventId == 50).Select(eg => eg.GradeId).ToListAsync();
        Assert.Equal(new byte[] { 11 }, syncedGrades);

        // Edición con brecha RN-12 en UpdateEventAsync debe fallar
        var invalidGapUpdate = BuildContiguousEvent(id: 50, positionId: 1, title: "Con Gap");
        invalidGapUpdate.ProposalsStartTime = new TimeOnly(10, 5);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.UpdateEventAsync(invalidGapUpdate, new List<byte> { 11 }, "10.0.0.9"));
        Assert.Contains("RN-12", ex.Message);

        // Inexistente y ELIMINADO retornan null
        var missingUpdate = BuildContiguousEvent(id: 9999, positionId: 1);
        Assert.Null(await service.UpdateEventAsync(missingUpdate, new List<byte> { 11 }, "10.0.0.9"));

        var deletedUpdate = BuildContiguousEvent(id: 51, positionId: 2);
        Assert.Null(await service.UpdateEventAsync(deletedUpdate, new List<byte> { 11 }, "10.0.0.9"));
    }

    // =========================================================================
    // 3. Guardia de rango monótono (EventService y VotingService)
    // =========================================================================

    /// <summary>
    /// Verifica que <see cref="EventService.CalculateStatus"/> nunca regresa a <c>PROGRAMADA</c>
    /// ni a una etapa de menor rango, mantiene <c>ELIMINADO</c> y promueve <c>PROGRAMADA</c> al alcanzar <c>regStart</c> (RN-12).
    /// </summary>
    [Theory]
    [InlineData("INSCRIPCION", 7, 0, "INSCRIPCION")]
    [InlineData("PROPUESTAS", 7, 0, "PROPUESTAS")]
    [InlineData("ACTIVA", 7, 0, "ACTIVA")]
    [InlineData("ACTIVA", 9, 0, "ACTIVA")]
    [InlineData("FINALIZADA", 7, 0, "FINALIZADA")]
    [InlineData("FINALIZADA", 14, 0, "FINALIZADA")]
    [InlineData("ELIMINADO", 7, 0, "ELIMINADO")]
    [InlineData("ELIMINADO", 14, 0, "ELIMINADO")]
    [InlineData("ELIMINADO", 19, 0, "ELIMINADO")]
    [InlineData("PROGRAMADA", 8, 0, "INSCRIPCION")]
    public void CalculateStatus_MonotonicRankGuard_NeverRegressesToProgramadaOrEarlierStage(
        string persistedStatus, int hour, int minute, string expectedStatus)
    {
        var ev = BuildContiguousEvent(id: 1, status: persistedStatus);
        var instant = EventDay.ToDateTime(new TimeOnly(hour, minute));

        var result = EventService.CalculateStatus(ev, instant);

        Assert.Equal(expectedStatus, result);
    }

    /// <summary>
    /// Verifica que al editar una elección <c>ACTIVA</c> desplazando sus ventanas hacia el futuro,
    /// <see cref="EventService.UpdateEventAsync"/> preserva el rango monótono y no regresa a etapas anteriores (RN-12).
    /// </summary>
    [Fact]
    public async Task UpdateEventAsync_ShiftingWindowsForward_PreservesMonotonicRank()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(VotStartLocal.AddHours(1)); // 13:00
        var service = CreateEventService(context, clock);

        var existing = BuildContiguousEvent(id: 60, positionId: 1, status: "ACTIVA", title: "En Curso");
        context.VotingEvents.Add(existing);
        await context.SaveChangesAsync();

        // Desplazar ventanas al día siguiente (ahora < regStart nuevo), estando previamente en ACTIVA
        var shiftedFuture = BuildContiguousEvent(id: 60, day: EventDay.AddDays(1), positionId: 1, title: "Reprogramada");
        var updated = await service.UpdateEventAsync(shiftedFuture, new List<byte> { 11 }, "127.0.0.1");

        Assert.NotNull(updated);
        Assert.Equal("ACTIVA", updated.Status);
    }

    /// <summary>
    /// Verifica que <see cref="VotingService"/> nunca regresa un evento a <c>PROGRAMADA</c> ni a una
    /// etapa de rango inferior en <see cref="VotingService.GetEventsForVoterDashboardAsync"/>,
    /// <see cref="VotingService.GetActiveEventsForVoterAsync"/> ni <see cref="VotingService.CastVoteDetailedAsync"/> (RN-12).
    /// </summary>
    [Theory]
    [InlineData("INSCRIPCION", 7, 0, "INSCRIPCION", false)]
    [InlineData("PROPUESTAS", 9, 0, "PROPUESTAS", false)]
    [InlineData("ACTIVA", 7, 0, "ACTIVA", true)]
    [InlineData("ACTIVA", 9, 0, "ACTIVA", true)]
    [InlineData("FINALIZADA", 14, 0, "FINALIZADA", false)]
    public async Task VotingService_MonotonicRankGuard_NeverRegressesToEarlierStageOrProgramada(
        string initialStatus, int hour, int minute, string expectedPersistedStatus, bool expectActiveAndVoteAllowed)
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(EventDay.ToDateTime(new TimeOnly(hour, minute)));
        var votingService = CreateVotingService(context, clock);

        context.Voters.Add(BuildVoter(100, gradeId: 11));
        context.VotingEvents.Add(BuildContiguousEvent(id: 70, status: initialStatus));
        context.EventGrades.Add(new EventGrade { VotingEventId = 70, GradeId = 11 });
        context.Candidates.Add(BuildApprovedCandidate(500, eventId: 70, isBlankVote: true));
        await context.SaveChangesAsync();

        var dashboard = await votingService.GetEventsForVoterDashboardAsync(100);
        Assert.Single(dashboard);
        Assert.Equal(expectedPersistedStatus, dashboard[0].Status);

        var active = await votingService.GetActiveEventsForVoterAsync(100);
        if (expectActiveAndVoteAllowed)
        {
            Assert.Single(active);
            var voteResult = await votingService.CastVoteDetailedAsync(100, 70, 500, "127.0.0.1");
            Assert.True(voteResult.Success);
        }
        else
        {
            Assert.Empty(active);
            var voteResult = await votingService.CastVoteDetailedAsync(100, 70, 500, "127.0.0.1");
            Assert.False(voteResult.Success);
            Assert.Equal("EVENT_NOT_ACTIVE", voteResult.ErrorCode);
        }

        var reloaded = await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 70);
        Assert.Equal(expectedPersistedStatus, reloaded.Status);
    }

    // =========================================================================
    // 4. Solo lectura en FINALIZADA (RN-7.2)
    // =========================================================================

    /// <summary>
    /// Verifica que <see cref="EventService.UpdateEventAsync"/> rechaza con <see cref="InvalidOperationException"/>
    /// cualquier edición cuando la elección está en <c>FINALIZADA</c> (tanto por estado persistido como por
    /// tiempo vencido <c>now &gt;= votEnd</c>), manteniendo intactos la entidad y sus grados (RN-7.2).
    /// </summary>
    [Theory]
    [InlineData("FINALIZADA", 14, 0)]
    [InlineData("ACTIVA", 18, 0)]
    [InlineData("INSCRIPCION", 19, 0)]
    public async Task UpdateEventAsync_Finalizada_ThrowsInvalidOperationException_AndPreservesDbState(
        string persistedStatus, int hour, int minute)
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(EventDay.ToDateTime(new TimeOnly(hour, minute)));
        var service = CreateEventService(context, clock);

        var original = BuildContiguousEvent(id: 80, positionId: 1, status: persistedStatus, title: "Título Intacto");
        context.VotingEvents.Add(original);
        context.EventGrades.Add(new EventGrade { VotingEventId = 80, GradeId = 10 });
        await context.SaveChangesAsync();

        var updateAttempt = BuildContiguousEvent(id: 80, positionId: 1, title: "Título Modificado");
        updateAttempt.VotingEndTime = new TimeOnly(20, 0);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateEventAsync(updateAttempt, new List<byte> { 11 }, "127.0.0.1"));

        Assert.Contains("FINALIZADA", ex.Message);
        Assert.Contains("RN-7.2", ex.Message);

        var dbEvent = await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 80);
        Assert.Equal("Título Intacto", dbEvent.Title);
        Assert.Equal(new TimeOnly(18, 0), dbEvent.VotingEndTime);

        var dbGrades = await context.EventGrades.AsNoTracking().Where(eg => eg.VotingEventId == 80).Select(eg => eg.GradeId).ToListAsync();
        Assert.Equal(new byte[] { 10 }, dbGrades);
    }

    // =========================================================================
    // 5. Eliminación lógica en cualquier etapa (RN-7.1)
    // =========================================================================

    /// <summary>
    /// Verifica que <see cref="EventService.SoftDeleteEventAsync"/> está permitido en cualquiera de las
    /// cinco etapas (<c>PROGRAMADA</c>, <c>INSCRIPCION</c>, <c>PROPUESTAS</c>, <c>ACTIVA</c>, <c>FINALIZADA</c>),
    /// establece <c>Status = "ELIMINADO"</c> y <c>DeletedAt</c> UTC, oculta el evento en consultas de administración
    /// y elector, y preserva intactos candidatos y votos (RN-7.1).
    /// </summary>
    [Theory]
    [InlineData("PROGRAMADA", 7, 0)]
    [InlineData("INSCRIPCION", 9, 0)]
    [InlineData("PROPUESTAS", 11, 0)]
    [InlineData("ACTIVA", 14, 0)]
    [InlineData("FINALIZADA", 19, 0)]
    public async Task SoftDeleteEventAsync_AllowedInEveryStage_HidesEventAndPreservesCandidatesAndVotes(
        string stage, int hour, int minute)
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(EventDay.ToDateTime(new TimeOnly(hour, minute)));
        var audit = new FakeAuditService();
        var eventService = CreateEventService(context, clock, audit);
        var votingService = CreateVotingService(context, clock, audit);

        context.Voters.Add(BuildVoter(100, gradeId: 11));
        var ev = BuildContiguousEvent(id: 90, status: stage);
        context.VotingEvents.Add(ev);
        context.EventGrades.Add(new EventGrade { VotingEventId = 90, GradeId = 11 });
        context.Candidates.Add(BuildApprovedCandidate(901, eventId: 90, isBlankVote: true));
        context.Votes.Add(new Vote
        {
            Id = 9001,
            VotingEventId = 90,
            CandidateId = 901,
            VoteHash = "a".PadRight(64, 'a'),
            VotedAt = clock.GetUtcNow().UtcDateTime
        });
        await context.SaveChangesAsync();

        var deleted = await eventService.SoftDeleteEventAsync(90, "10.0.0.15");

        Assert.True(deleted);
        Assert.Contains("EVENT_DELETED", audit.Actions);

        var reloaded = await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 90);
        Assert.Equal("ELIMINADO", reloaded.Status);
        Assert.Equal(clock.GetUtcNow().UtcDateTime, reloaded.DeletedAt);

        // Oculto en EventService y VotingService
        Assert.Empty(await eventService.GetEventsAsync());
        Assert.Null(await eventService.GetEventByIdAsync(90));
        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(100));
        Assert.Empty(await votingService.GetEventsForVoterDashboardAsync(100));

        var voteAttempt = await votingService.CastVoteDetailedAsync(100, 90, 901, "10.0.0.15");
        Assert.False(voteAttempt.Success);
        Assert.Equal("EVENT_NOT_FOUND", voteAttempt.ErrorCode);

        // Candidatos y votos históricos permanecen inmutables en BD
        Assert.Single(await context.Candidates.AsNoTracking().Where(c => c.VotingEventId == 90).ToListAsync());
        Assert.Single(await context.Votes.AsNoTracking().Where(v => v.VotingEventId == 90).ToListAsync());
    }

    /// <summary>
    /// Verifica que <see cref="EventService.SoftDeleteEventAsync"/> retorna <c>false</c>
    /// cuando el evento no existe o ya se encuentra en estado <c>ELIMINADO</c> (RN-7.1).
    /// </summary>
    [Fact]
    public async Task SoftDeleteEventAsync_ReturnsFalse_WhenMissingOrAlreadyEliminado()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal);
        var service = CreateEventService(context, clock);

        context.VotingEvents.Add(BuildContiguousEvent(id: 95, status: "ACTIVA"));
        await context.SaveChangesAsync();

        Assert.True(await service.SoftDeleteEventAsync(95, "127.0.0.1"));
        Assert.False(await service.SoftDeleteEventAsync(95, "127.0.0.1"));
        Assert.False(await service.SoftDeleteEventAsync(9999, "127.0.0.1"));
    }

    // =========================================================================
    // 6. VotingService: fronteras exactas, persistencia vs memoria y grados
    // =========================================================================

    /// <summary>
    /// Verifica en <see cref="VotingService"/> la frontera exacta de inicio de inscripción:
    /// 1 tick antes de <c>08:00</c> permanece <c>PROGRAMADA</c> (fuera del dashboard), y en el
    /// instante exacto <c>08:00</c> transiciona y persiste <c>INSCRIPCION</c> (RN-12).
    /// </summary>
    [Fact]
    public async Task VotingService_RegistrationBoundary_OneTickBeforeIsProgramada_ExactInstantIsInscripcion()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal.AddTicks(-1));
        var votingService = CreateVotingService(context, clock);

        context.Voters.Add(BuildVoter(100, gradeId: 11));
        context.VotingEvents.Add(BuildContiguousEvent(id: 101, status: "PROGRAMADA"));
        context.EventGrades.Add(new EventGrade { VotingEventId = 101, GradeId = 11 });
        context.Candidates.Add(BuildApprovedCandidate(1001, eventId: 101, isBlankVote: true));
        await context.SaveChangesAsync();

        Assert.Empty(await votingService.GetEventsForVoterDashboardAsync(100));
        Assert.Equal("PROGRAMADA", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 101)).Status);
        var beforeVote = await votingService.CastVoteDetailedAsync(100, 101, 1001, "127.0.0.1");
        Assert.Equal("EVENT_NOT_ACTIVE", beforeVote.ErrorCode);

        clock.Advance(TimeSpan.FromTicks(1));

        var dashboard = await votingService.GetEventsForVoterDashboardAsync(100);
        Assert.Single(dashboard);
        Assert.Equal("INSCRIPCION", dashboard[0].Status);
        Assert.Equal("INSCRIPCION", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 101)).Status);
        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(100));
        var atBoundaryVote = await votingService.CastVoteDetailedAsync(100, 101, 1001, "127.0.0.1");
        Assert.Equal("EVENT_NOT_ACTIVE", atBoundaryVote.ErrorCode);
    }

    /// <summary>
    /// Verifica en <see cref="VotingService"/> la frontera exacta de inicio de propuestas:
    /// 1 tick antes de <c>10:00</c> permanece <c>INSCRIPCION</c>, y en <c>10:00</c> transiciona
    /// y persiste <c>PROPUESTAS</c> (RN-12).
    /// </summary>
    [Fact]
    public async Task VotingService_ProposalsBoundary_OneTickBeforeIsInscripcion_ExactInstantIsPropuestas()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(PropStartLocal.AddTicks(-1));
        var votingService = CreateVotingService(context, clock);

        context.Voters.Add(BuildVoter(100, gradeId: 11));
        context.VotingEvents.Add(BuildContiguousEvent(id: 102, status: "INSCRIPCION"));
        context.EventGrades.Add(new EventGrade { VotingEventId = 102, GradeId = 11 });
        context.Candidates.Add(BuildApprovedCandidate(1002, eventId: 102, isBlankVote: true));
        await context.SaveChangesAsync();

        var beforeDashboard = await votingService.GetEventsForVoterDashboardAsync(100);
        Assert.Single(beforeDashboard);
        Assert.Equal("INSCRIPCION", beforeDashboard[0].Status);
        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(100));

        clock.Advance(TimeSpan.FromTicks(1));

        var atDashboard = await votingService.GetEventsForVoterDashboardAsync(100);
        Assert.Single(atDashboard);
        Assert.Equal("PROPUESTAS", atDashboard[0].Status);
        Assert.Equal("PROPUESTAS", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 102)).Status);
        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(100));
        var voteResult = await votingService.CastVoteDetailedAsync(100, 102, 1002, "127.0.0.1");
        Assert.Equal("EVENT_NOT_ACTIVE", voteResult.ErrorCode);
    }

    /// <summary>
    /// Verifica en <see cref="VotingService"/> la frontera exacta de inicio de votación:
    /// 1 tick antes de <c>12:00</c> permanece <c>PROPUESTAS</c> (voto rechazado) y en <c>12:00</c>
    /// pasa a <c>ACTIVA</c> permitiendo el voto (RF-M05-00, RN-12).
    /// </summary>
    [Fact]
    public async Task VotingService_VotingStartBoundary_OneTickBeforeIsPropuestas_ExactInstantIsActiva()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(VotStartLocal.AddTicks(-1));
        var votingService = CreateVotingService(context, clock);

        context.Voters.Add(BuildVoter(100, gradeId: 11));
        context.VotingEvents.Add(BuildContiguousEvent(id: 103, status: "PROPUESTAS"));
        context.EventGrades.Add(new EventGrade { VotingEventId = 103, GradeId = 11 });
        context.Candidates.Add(BuildApprovedCandidate(1003, eventId: 103, isBlankVote: true));
        await context.SaveChangesAsync();

        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(100));
        var beforeVote = await votingService.CastVoteDetailedAsync(100, 103, 1003, "127.0.0.1");
        Assert.Equal("EVENT_NOT_ACTIVE", beforeVote.ErrorCode);

        clock.Advance(TimeSpan.FromTicks(1));

        var active = await votingService.GetActiveEventsForVoterAsync(100);
        Assert.Single(active);
        Assert.Equal("ACTIVA", active[0].Status);
        Assert.Equal("ACTIVA", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 103)).Status);

        var voteResult = await votingService.CastVoteDetailedAsync(100, 103, 1003, "127.0.0.1");
        Assert.True(voteResult.Success);
        Assert.Single(await context.Votes.AsNoTracking().Where(v => v.VotingEventId == 103).ToListAsync());
    }

    /// <summary>
    /// Verifica en <see cref="VotingService"/> la frontera exacta de cierre de votación:
    /// 1 tick antes de <c>18:00</c> sigue <c>ACTIVA</c>, y en <c>18:00</c> rechaza el voto
    /// con <c>EVENT_NOT_ACTIVE</c> y persiste <c>FINALIZADA</c> al consultar eventos (RN-4.1, RN-12).
    /// </summary>
    [Fact]
    public async Task VotingService_VotingEndBoundary_OneTickBeforeIsActiva_ExactInstantIsFinalizada()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(VotEndLocal.AddTicks(-1));
        var votingService = CreateVotingService(context, clock);

        context.Voters.Add(BuildVoter(100, gradeId: 11));
        context.VotingEvents.Add(BuildContiguousEvent(id: 104, status: "ACTIVA"));
        context.EventGrades.Add(new EventGrade { VotingEventId = 104, GradeId = 11 });
        context.Candidates.Add(BuildApprovedCandidate(1004, eventId: 104, isBlankVote: true));
        await context.SaveChangesAsync();

        Assert.Single(await votingService.GetActiveEventsForVoterAsync(100));

        clock.Advance(TimeSpan.FromTicks(1));

        // CastVoteDetailedAsync rechaza en 18:00:00 sin mutar DB Status
        var rejectedVote = await votingService.CastVoteDetailedAsync(100, 104, 1004, "127.0.0.1");
        Assert.False(rejectedVote.Success);
        Assert.Equal("EVENT_NOT_ACTIVE", rejectedVote.ErrorCode);
        Assert.Equal("ACTIVA", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 104)).Status);

        // GetActiveEventsForVoterAsync persiste FINALIZADA y devuelve lista vacía; dashboard incluye FINALIZADA
        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(100));
        Assert.Equal("FINALIZADA", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 104)).Status);

        var dashboard = await votingService.GetEventsForVoterDashboardAsync(100);
        Assert.Single(dashboard);
        Assert.Equal("FINALIZADA", dashboard[0].Status);
    }

    /// <summary>
    /// Verifica el recorrido completo con un único reloj desde <c>PROGRAMADA</c> hasta <c>FINALIZADA</c>
    /// en <see cref="VotingService"/> sin brechas entre etapas contiguas (RN-12).
    /// </summary>
    [Fact]
    public async Task VotingService_FullContiguousLifecycle_SingleClockWalkHasNoGaps()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(RegStartLocal.AddSeconds(-1));
        var votingService = CreateVotingService(context, clock);

        context.Voters.Add(BuildVoter(100, gradeId: 11));
        context.VotingEvents.Add(BuildContiguousEvent(id: 105, status: "PROGRAMADA"));
        context.EventGrades.Add(new EventGrade { VotingEventId = 105, GradeId = 11 });
        await context.SaveChangesAsync();

        Assert.Empty(await votingService.GetEventsForVoterDashboardAsync(100));

        clock.Advance(TimeSpan.FromSeconds(1)); // 08:00:00
        Assert.Equal("INSCRIPCION", (await votingService.GetEventsForVoterDashboardAsync(100)).Single().Status);
        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(100));

        clock.Advance(PropStartLocal - RegStartLocal); // 10:00:00
        Assert.Equal("PROPUESTAS", (await votingService.GetEventsForVoterDashboardAsync(100)).Single().Status);
        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(100));

        clock.Advance(VotStartLocal - PropStartLocal); // 12:00:00
        Assert.Equal("ACTIVA", (await votingService.GetActiveEventsForVoterAsync(100)).Single().Status);
        Assert.Equal("ACTIVA", (await votingService.GetEventsForVoterDashboardAsync(100)).Single().Status);

        clock.Advance(VotEndLocal - VotStartLocal); // 18:00:00
        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(100));
        Assert.Equal("FINALIZADA", (await votingService.GetEventsForVoterDashboardAsync(100)).Single().Status);
    }

    /// <summary>
    /// Verifica que <see cref="VotingService.CastVoteDetailedAsync"/> evalúa la etapa en memoria
    /// sin mutar el estado persistido en BD, mientras que las consultas de eventos sí lo persisten (RN-12).
    /// </summary>
    [Fact]
    public async Task VotingService_CastVoteDetailedAsync_EvaluatesStageInMemoryWithoutPersistingStatus()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(VotStartLocal.AddHours(2)); // 14:00 (ACTIVA)
        var votingService = CreateVotingService(context, clock);

        context.Voters.Add(BuildVoter(100, gradeId: 11));
        context.VotingEvents.Add(BuildContiguousEvent(id: 106, status: "PROPUESTAS"));
        context.EventGrades.Add(new EventGrade { VotingEventId = 106, GradeId = 11 });
        context.Candidates.Add(BuildApprovedCandidate(1006, eventId: 106, isBlankVote: true));
        await context.SaveChangesAsync();

        var voteResult = await votingService.CastVoteDetailedAsync(100, 106, 1006, "127.0.0.1");
        Assert.True(voteResult.Success);

        // El estado en BD sigue siendo PROPUESTAS hasta que se consulte el listado
        Assert.Equal("PROPUESTAS", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 106)).Status);

        var active = await votingService.GetActiveEventsForVoterAsync(100);
        Assert.Single(active);
        Assert.Equal("ACTIVA", (await context.VotingEvents.AsNoTracking().SingleAsync(e => e.Id == 106)).Status);
    }

    /// <summary>
    /// Verifica que <see cref="VotingService.CastVoteDetailedAsync"/> rechaza votos en todas las etapas
    /// distintas a <c>ACTIVA</c> (<c>EVENT_NOT_ACTIVE</c>) y en <c>ELIMINADO</c> (<c>EVENT_NOT_FOUND</c>) (RF-M05-00, RN-7.1, RN-12).
    /// </summary>
    [Theory]
    [InlineData("PROGRAMADA", 7, 0, "EVENT_NOT_ACTIVE")]
    [InlineData("INSCRIPCION", 9, 0, "EVENT_NOT_ACTIVE")]
    [InlineData("PROPUESTAS", 11, 0, "EVENT_NOT_ACTIVE")]
    [InlineData("FINALIZADA", 19, 0, "EVENT_NOT_ACTIVE")]
    [InlineData("ELIMINADO", 14, 0, "EVENT_NOT_FOUND")]
    public async Task VotingService_CastVoteDetailedAsync_RejectsNonActiveStagesAndEliminado(
        string status, int hour, int minute, string expectedErrorCode)
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(EventDay.ToDateTime(new TimeOnly(hour, minute)));
        var votingService = CreateVotingService(context, clock);

        context.Voters.Add(BuildVoter(100, gradeId: 11));
        var ev = BuildContiguousEvent(id: 107, status: status);
        if (status == "ELIMINADO")
        {
            ev.DeletedAt = clock.GetUtcNow().UtcDateTime;
        }
        context.VotingEvents.Add(ev);
        context.EventGrades.Add(new EventGrade { VotingEventId = 107, GradeId = 11 });
        context.Candidates.Add(BuildApprovedCandidate(1007, eventId: 107, isBlankVote: true));
        await context.SaveChangesAsync();

        var result = await votingService.CastVoteDetailedAsync(100, 107, 1007, "127.0.0.1");

        Assert.False(result.Success);
        Assert.Equal(expectedErrorCode, result.ErrorCode);
    }

    /// <summary>
    /// Verifica que <see cref="VotingService.GetActiveEventsForVoterAsync"/> y
    /// <see cref="VotingService.GetEventsForVoterDashboardAsync"/> filtran estrictamente por los grados
    /// habilitados en <c>EventGrades</c> (RN-4.1).
    /// </summary>
    [Fact]
    public async Task VotingService_GradeFiltering_OnlyEnabledGradesSeeDashboardAndActiveEvents()
    {
        await using var context = CreateInMemoryContext();
        var clock = CreateLocalFakeTimeProvider(VotStartLocal.AddHours(2)); // 14:00 (ACTIVA)
        var votingService = CreateVotingService(context, clock);

        context.Voters.AddRange(
            BuildVoter(100, gradeId: 11),
            BuildVoter(101, gradeId: 10));
        context.VotingEvents.Add(BuildContiguousEvent(id: 108, status: "PROPUESTAS"));
        context.EventGrades.Add(new EventGrade { VotingEventId = 108, GradeId = 11 });
        await context.SaveChangesAsync();

        // Grado 10 no habilitado -> listas vacías
        Assert.Empty(await votingService.GetActiveEventsForVoterAsync(101));
        Assert.Empty(await votingService.GetEventsForVoterDashboardAsync(101));

        // Grado 11 habilitado -> ve evento transicionado a ACTIVA
        var active = await votingService.GetActiveEventsForVoterAsync(100);
        Assert.Single(active);
        Assert.Equal("ACTIVA", active[0].Status);

        var dashboard = await votingService.GetEventsForVoterDashboardAsync(100);
        Assert.Single(dashboard);
        Assert.Equal("ACTIVA", dashboard[0].Status);
    }
}
