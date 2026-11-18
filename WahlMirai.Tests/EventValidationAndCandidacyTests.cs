using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class EventValidationAndCandidacyTests
{
    private static WahlMiraiDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<WahlMiraiDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var context = new WahlMiraiDbContext(options);

        context.Roles.AddRange(
            new Role { Id = 1, Name = "SUPER_ADMIN" },
            new Role { Id = 2, Name = "ADMIN" },
            new Role { Id = 3, Name = "ELECTOR" });

        context.Grades.AddRange(
            new Grade { Id = 10, Name = "10°", SequenceOrder = 5, IsLastGrade = false },
            new Grade { Id = 11, Name = "11°", SequenceOrder = 6, IsLastGrade = true });

        context.ElectionPositions.AddRange(
            new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = DateTime.UtcNow },
            new ElectionPosition { Id = 2, Name = "Contralor", Status = "ACTIVO", CreatedAt = DateTime.UtcNow });

        context.SaveChanges();
        return context;
    }

    [Fact]
    public async Task CreateEvent_ThrowsIfVotingIsNotSingleDay()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var newEvent = new VotingEvent
        {
            Title = "Elección Personero 2026",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(4)),
            ProposalsEndTime = new TimeOnly(17, 0),
            // Distintas fechas para votación (inicio día 5, fin día 6) -> Debe fallar
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(5)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(6)),
            VotingEndTime = new TimeOnly(16, 0),
            CreatedByVoterId = 1
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(newEvent, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("un solo día", ex.Message);
    }

    [Fact]
    public async Task CreateEvent_ThrowsIfVotingEndTimeIsNotAfterStartTime()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var newEvent = new VotingEvent
        {
            Title = "Elección Personero 2026",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(2)),
            ProposalsStartTime = new TimeOnly(17, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(5)),
            ProposalsEndTime = new TimeOnly(16, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(5)),
            VotingStartTime = new TimeOnly(16, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(5)),
            VotingEndTime = new TimeOnly(10, 0), // Hora de fin menor que la de inicio -> Debe fallar
            CreatedByVoterId = 1
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(newEvent, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("posterior", ex.Message);
    }

    [Fact]
    public async Task CreateEvent_ThrowsIfActiveEventAlreadyExistsForSamePosition()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var firstEvent = new VotingEvent
        {
            Title = "Elección Personero Activa",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(2)),
            ProposalsStartTime = new TimeOnly(17, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(4)),
            ProposalsEndTime = new TimeOnly(17, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(4)),
            VotingStartTime = new TimeOnly(17, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(4)),
            VotingEndTime = new TimeOnly(20, 0),
            CreatedByVoterId = 1
        };

        await service.CreateEventAsync(firstEvent, new List<byte> { 10, 11 }, "127.0.0.1");

        // Intentar crear un segundo evento para el mismo cargo (Personero) mientras el primero está en curso
        var secondEvent = new VotingEvent
        {
            Title = "Segunda Elección Personero",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(10)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(12)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(12)),
            ProposalsStartTime = new TimeOnly(17, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(14)),
            ProposalsEndTime = new TimeOnly(17, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(14)),
            VotingStartTime = new TimeOnly(17, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(14)),
            VotingEndTime = new TimeOnly(20, 0),
            CreatedByVoterId = 1
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateEventAsync(secondEvent, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("Solo puede estar activa una votación por cargo", ex.Message);
    }

    [Fact]
    public async Task AddCandidate_RejectsNonGrade11_ForPersonero()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var personeroEvent = new VotingEvent
        {
            Title = "Elección Personero 2026",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(3)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(4)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(5)),
            ProposalsEndTime = new TimeOnly(17, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(6)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(6)),
            VotingEndTime = new TimeOnly(16, 0),
            CreatedByVoterId = 1,
            Status = "PROGRAMADA"
        };
        context.VotingEvents.Add(personeroEvent);

        // Elector de Grado 10°
        var voterGrade10 = new Voter
        {
            Id = 101,
            RoleId = 3,
            GradeId = 10,
            FullName = "Carlos Estudiante 10",
            ContactEmail = "carlos10@example.com",
            PasswordHash = "hashed_pass",
            DocumentHash = "hash10",
            EncryptedDocument = "enc10",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        };
        context.Voters.Add(voterGrade10);
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AddCandidateAsync(personeroEvent.Id, voterGrade10.Id, "Mi lema", null, "127.0.0.1"));

        Assert.Contains("solo pueden aspirar e inscribirse los estudiantes de grado 11", ex.Message);
    }

    [Fact]
    public async Task AddCandidate_AllowsGrade11_ForPersonero()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var personeroEvent = new VotingEvent
        {
            Title = "Elección Personero 2026",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(3)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(4)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(5)),
            ProposalsEndTime = new TimeOnly(17, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(6)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(6)),
            VotingEndTime = new TimeOnly(16, 0),
            CreatedByVoterId = 1,
            Status = "PROGRAMADA"
        };
        context.VotingEvents.Add(personeroEvent);

        // Elector de Grado 11°
        var voterGrade11 = new Voter
        {
            Id = 102,
            RoleId = 3,
            GradeId = 11,
            FullName = "Laura Estudiante 11",
            ContactEmail = "laura11@example.com",
            PasswordHash = "hashed_pass",
            DocumentHash = "hash11",
            EncryptedDocument = "enc11",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        };
        context.Voters.Add(voterGrade11);
        await context.SaveChangesAsync();

        var candidate = await service.AddCandidateAsync(personeroEvent.Id, voterGrade11.Id, "Por un mejor colegio", null, "127.0.0.1");

        Assert.NotNull(candidate);
        Assert.Equal(voterGrade11.Id, candidate.VoterId);
    }

    [Fact]
    public async Task CandidacyService_RejectsGrade10StudentForPersonero()
    {
        await using var context = CreateContext();
        var config = new ConfigurationBuilder().Build();
        var candidacyService = new CandidacyService(config, new FakeEncryptionService(), context, new FakeAuditService());

        var today = DateTime.Now;
        var personeroEvent = new VotingEvent
        {
            Title = "Elección Personero 2026",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(-1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(4)),
            ProposalsEndTime = new TimeOnly(17, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(5)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(5)),
            VotingEndTime = new TimeOnly(16, 0),
            CreatedByVoterId = 1,
            Status = "INSCRIPCION"
        };
        context.VotingEvents.Add(personeroEvent);

        // Elector de Grado 10°
        var voterGrade10 = new Voter
        {
            Id = 201,
            RoleId = 3,
            GradeId = 10,
            FullName = "Andrés Grado 10",
            ContactEmail = "andres10@example.com",
            PasswordHash = "hashed_pass",
            DocumentHash = "hash201",
            EncryptedDocument = "enc201",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        };
        context.Voters.Add(voterGrade10);
        await context.SaveChangesAsync();

        var submitDto = new PostulationSubmitDto
        {
            EventId = personeroEvent.Id,
            Slogan = "Mi campaña",
            Proposals = new List<string> { "Propuesta 1" },
            AcceptPublicationConsent = true
        };

        var result = await candidacyService.SubmitPostulationAsync(submitDto, (int)voterGrade10.Id, "127.0.0.1", "C:\\fake");

        Assert.False(result.Success);
        Assert.Contains("solo pueden aspirar e inscribirse los estudiantes de grado 11", result.ErrorMessage);

        // También verificar que no aparece como evento elegible para grado 10
        var eligibleEvents = await candidacyService.GetEligibleEventsForPostulationAsync((int)voterGrade10.Id);
        Assert.DoesNotContain(eligibleEvents, e => e.Id == personeroEvent.Id);
    }

    [Fact]
    public async Task CandidacyService_RejectsPostulationWithoutPublicationConsent()
    {
        await using var context = CreateContext();
        var config = new ConfigurationBuilder().Build();
        var candidacyService = new CandidacyService(config, new FakeEncryptionService(), context, new FakeAuditService());

        var today = DateTime.Now;
        var personeroEvent = new VotingEvent
        {
            Title = "Elección Personero 2026",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(-1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(4)),
            ProposalsEndTime = new TimeOnly(17, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(5)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(5)),
            VotingEndTime = new TimeOnly(16, 0),
            CreatedByVoterId = 1,
            Status = "INSCRIPCION"
        };
        context.VotingEvents.Add(personeroEvent);

        var voterGrade11 = new Voter
        {
            Id = 301,
            RoleId = 3,
            GradeId = 11,
            FullName = "Carlos Grado 11",
            ContactEmail = "carlos11@example.com",
            PasswordHash = "hashed_pass",
            DocumentHash = "hash301",
            EncryptedDocument = "enc301",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        };
        context.Voters.Add(voterGrade11);
        await context.SaveChangesAsync();

        var submitDto = new PostulationSubmitDto
        {
            EventId = personeroEvent.Id,
            Slogan = "Mi campaña",
            Proposals = new List<string> { "Propuesta 1" },
            AcceptPublicationConsent = false // Sin consentimiento
        };

        var result = await candidacyService.SubmitPostulationAsync(submitDto, (int)voterGrade11.Id, "127.0.0.1", "C:\\fake");

        Assert.False(result.Success);
        Assert.Contains("autorizar la exhibición pública", result.ErrorMessage);
    }

    [Fact]
    public async Task CreateEvent_ThrowsIfProposalsStartDoesNotMatchRegistrationEnd()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var newEvent = new VotingEvent
        {
            Title = "Elección Contigüidad 1",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),
            // Brecha de 1 día: inicia el día 3 en vez del día 2 a las 17:00
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(4)),
            ProposalsEndTime = new TimeOnly(17, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(4)),
            VotingStartTime = new TimeOnly(17, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(4)),
            VotingEndTime = new TimeOnly(20, 0),
            CreatedByVoterId = 1
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(newEvent, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("la consulta de propuestas debe iniciar exactamente cuando finaliza la inscripción", ex.Message);
    }

    [Fact]
    public async Task CreateEvent_ThrowsIfVotingStartDoesNotMatchProposalsEnd()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var newEvent = new VotingEvent
        {
            Title = "Elección Contigüidad 2",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(2)),
            ProposalsStartTime = new TimeOnly(17, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsEndTime = new TimeOnly(17, 0),
            // Brecha de 1 hora: inicia a las 18:00 en vez de las 17:00
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            VotingStartTime = new TimeOnly(18, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(3)),
            VotingEndTime = new TimeOnly(21, 0),
            CreatedByVoterId = 1
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(newEvent, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("la votación debe iniciar exactamente cuando finaliza la consulta de propuestas", ex.Message);
    }

    [Fact]
    public async Task CreateEvent_SucceedsWithStrictlyContiguousStages()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var newEvent = new VotingEvent
        {
            Title = "Elección Perfectamente Contigua",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(2)),
            ProposalsStartTime = new TimeOnly(17, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsEndTime = new TimeOnly(8, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(3)),
            VotingEndTime = new TimeOnly(16, 0),
            CreatedByVoterId = 1
        };

        var created = await service.CreateEventAsync(newEvent, new List<byte> { 10, 11 }, "127.0.0.1");

        Assert.NotNull(created);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task CreateEvent_ThrowsIfElectionTypeIsNotPersonas()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var newEvent = new VotingEvent
        {
            Title = "Elección de Temas No Permitida",
            ElectionType = "TEMAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(2)),
            ProposalsStartTime = new TimeOnly(17, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsEndTime = new TimeOnly(8, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(3)),
            VotingEndTime = new TimeOnly(16, 0),
            CreatedByVoterId = 1
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.CreateEventAsync(newEvent, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("PERSONAS", ex.Message);
    }

    [Fact]
    public async Task UpdateEvent_ThrowsIfElectionStatusIsFinalizada()
    {
        await using var context = CreateContext();
        var service = new EventService(context, new FakeAuditService());

        var today = DateTime.Now;
        var finalizadaEvent = new VotingEvent
        {
            Title = "Elección Finalizada Inmutable",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(-10)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(-8)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(-8)),
            ProposalsStartTime = new TimeOnly(17, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(-5)),
            ProposalsEndTime = new TimeOnly(8, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(-5)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(-5)),
            VotingEndTime = new TimeOnly(16, 0),
            CreatedByVoterId = 1,
            Status = "FINALIZADA"
        };
        context.VotingEvents.Add(finalizadaEvent);
        await context.SaveChangesAsync();

        var updatedEvent = new VotingEvent
        {
            Id = finalizadaEvent.Id,
            Title = "Intento Modificar Titulo",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = finalizadaEvent.RegistrationStartDate,
            RegistrationStartTime = finalizadaEvent.RegistrationStartTime,
            RegistrationEndDate = finalizadaEvent.RegistrationEndDate,
            RegistrationEndTime = finalizadaEvent.RegistrationEndTime,
            ProposalsStartDate = finalizadaEvent.ProposalsStartDate,
            ProposalsStartTime = finalizadaEvent.ProposalsStartTime,
            ProposalsEndDate = finalizadaEvent.ProposalsEndDate,
            ProposalsEndTime = finalizadaEvent.ProposalsEndTime,
            VotingStartDate = finalizadaEvent.VotingStartDate,
            VotingStartTime = finalizadaEvent.VotingStartTime,
            VotingEndDate = finalizadaEvent.VotingEndDate,
            VotingEndTime = finalizadaEvent.VotingEndTime,
            CreatedByVoterId = 1
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateEventAsync(updatedEvent, new List<byte> { 10, 11 }, "127.0.0.1"));

        Assert.Contains("FINALIZADA", ex.Message);
    }

    [Fact]
    public void CalculateStatus_NeverRevertsToProgramadaOnceStarted()
    {
        var today = DateTime.Now;
        var ev = new VotingEvent
        {
            Title = "Test No Regreso Programada",
            RegistrationStartDate = DateOnly.FromDateTime(today.AddDays(-2)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(today.AddDays(-1)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(-1)),
            ProposalsStartTime = new TimeOnly(17, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(1)),
            ProposalsEndTime = new TimeOnly(8, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(1)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(1)),
            VotingEndTime = new TimeOnly(16, 0),
            Status = "INSCRIPCION"
        };

        // Si now ya superó el inicio de inscripción, el cálculo dinámico nunca debe ser PROGRAMADA
        var calculated = EventService.CalculateStatus(ev, today);
        Assert.NotEqual("PROGRAMADA", calculated);
        Assert.Equal("PROPUESTAS", calculated);

        // Si la elección ya tenía estatus ACTIVA pero se evalúa con una fecha previa, no retrocede
        ev.Status = "ACTIVA";
        var calculatedPast = EventService.CalculateStatus(ev, today.AddDays(-5));
        Assert.Equal("ACTIVA", calculatedPast);
    }

    [Fact]
    public async Task ApproveCandidate_ThrowsIfElectionStatusIsFinalizada()
    {
        await using var context = CreateContext();
        var audit = new FakeAuditService();
        var reviewService = new CandidateReviewService(context, audit);

        var finalizadaEvent = new VotingEvent
        {
            Title = "Elección Finalizada Candidatos",
            ElectionType = "PERSONAS",
            PositionId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-10)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-8)),
            RegistrationEndTime = new TimeOnly(17, 0),
            ProposalsStartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-8)),
            ProposalsStartTime = new TimeOnly(17, 0),
            ProposalsEndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)),
            ProposalsEndTime = new TimeOnly(8, 0),
            VotingStartDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(DateTime.Now.AddDays(-5)),
            VotingEndTime = new TimeOnly(16, 0),
            CreatedByVoterId = 1,
            Status = "FINALIZADA"
        };
        context.VotingEvents.Add(finalizadaEvent);

        var candidate = new Candidate
        {
            VotingEventId = finalizadaEvent.Id,
            Name = "Candidato 1",
            Status = "PENDIENTE",
            IsBlankVote = false,
            Slogan = "Campaña"
        };
        context.Candidates.Add(candidate);
        await context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            reviewService.ApproveCandidateAsync(candidate.Id, false, null, 1, "127.0.0.1"));

        Assert.Contains("No se pueden dictaminar candidaturas de un proceso electoral FINALIZADA", ex.Message);
    }
}
