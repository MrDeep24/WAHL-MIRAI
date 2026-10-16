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
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(4)),
            ProposalsEndTime = new TimeOnly(17, 0),
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
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(3)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(4)),
            ProposalsEndTime = new TimeOnly(17, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(5)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(5)),
            VotingEndTime = new TimeOnly(16, 0),
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
            ProposalsStartDate = DateOnly.FromDateTime(today.AddDays(13)),
            ProposalsStartTime = new TimeOnly(8, 0),
            ProposalsEndDate = DateOnly.FromDateTime(today.AddDays(14)),
            ProposalsEndTime = new TimeOnly(17, 0),
            VotingStartDate = DateOnly.FromDateTime(today.AddDays(15)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(today.AddDays(15)),
            VotingEndTime = new TimeOnly(16, 0),
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
            Proposals = new List<string> { "Propuesta 1" }
        };

        var result = await candidacyService.SubmitPostulationAsync(submitDto, (int)voterGrade10.Id, "127.0.0.1", "C:\\fake");

        Assert.False(result.Success);
        Assert.Contains("solo pueden aspirar e inscribirse los estudiantes de grado 11", result.ErrorMessage);

        // También verificar que no aparece como evento elegible para grado 10
        var eligibleEvents = await candidacyService.GetEligibleEventsForPostulationAsync((int)voterGrade10.Id);
        Assert.DoesNotContain(eligibleEvents, e => e.Id == personeroEvent.Id);
    }
}
