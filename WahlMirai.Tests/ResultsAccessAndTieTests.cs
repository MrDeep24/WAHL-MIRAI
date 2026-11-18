using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;
using Xunit;

namespace WahlMirai.Tests;

public class ResultsAccessAndTieTests
{
    private class FakeVotingService : IVotingService
    {
        public bool HasVotedReturnValue { get; set; }
        public Task<bool> HasVotedAsync(int voterId, int eventId) => Task.FromResult(HasVotedReturnValue);
        public Task<bool> CastVoteAsync(int voterId, int eventId, int candidateId, string ipAddress) => Task.FromResult(true);
        public Task<List<VotingEvent>> GetActiveEventsForVoterAsync(int voterId) => Task.FromResult(new List<VotingEvent>());
        public Task<List<VotingEvent>> GetEventsForVoterDashboardAsync(int voterId) => Task.FromResult(new List<VotingEvent>());
        public Task<List<Candidate>> GetCandidatesForEventAsync(int eventId) => Task.FromResult(new List<Candidate>());
        public Task<string?> GetVoterGradeNameAsync(int voterId) => Task.FromResult<string?>("11°");
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
        public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
    }

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

        context.ElectionPositions.Add(
            new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = DateTime.UtcNow });

        context.SaveChanges();
        return context;
    }

    private static ResultsController CreateController(WahlMiraiDbContext context, IVotingService votingService, string role, uint userId)
    {
        var controller = new ResultsController(context, votingService);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var user = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext { User = user };
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        var tempData = new TempDataDictionary(httpContext, new TestTempDataProvider());
        controller.TempData = tempData;

        return controller;
    }

    [Theory]
    [InlineData("PROGRAMADA")]
    [InlineData("INSCRIPCION")]
    [InlineData("PROPUESTAS")]
    public async Task Elector_DeniedAccess_WhenEventNotActiveOrFinalized(string status)
    {
        await using var context = CreateContext();
        var fakeVotingService = new FakeVotingService();

        var ev = new VotingEvent
        {
            Id = 10,
            Title = "Elección No Disponible",
            ElectionType = "PERSONAS",
            PositionId = 1,
            Status = status,
            CreatedByVoterId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(DateTime.Today),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(DateTime.Today),
            RegistrationEndTime = new TimeOnly(12, 0),
            ProposalsStartDate = DateOnly.FromDateTime(DateTime.Today),
            ProposalsStartTime = new TimeOnly(12, 0),
            ProposalsEndDate = DateOnly.FromDateTime(DateTime.Today),
            ProposalsEndTime = new TimeOnly(14, 0),
            VotingStartDate = DateOnly.FromDateTime(DateTime.Today),
            VotingStartTime = new TimeOnly(14, 0),
            VotingEndDate = DateOnly.FromDateTime(DateTime.Today),
            VotingEndTime = new TimeOnly(18, 0)
        };
        context.VotingEvents.Add(ev);

        var voter = new Voter
        {
            Id = 100,
            RoleId = 3,
            FullName = "Elector Test",
            GradeId = 11,
            Status = "ACTIVO",
            ContactEmail = "elector@test.com",
            PasswordHash = "hash",
            DocumentHash = "doc",
            EncryptedDocument = "enc",
            RegisteredAt = DateTime.UtcNow
        };
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        var controller = CreateController(context, fakeVotingService, "ELECTOR", voter.Id);

        // Index
        var indexResult = await controller.Index((int)ev.Id);
        var redirect = Assert.IsType<RedirectToActionResult>(indexResult);
        Assert.Equal("Dashboard", redirect.ActionName);
        Assert.Equal("Elector", redirect.ControllerName);
        Assert.True(controller.TempData.ContainsKey("ErrorMessage"));

        // GetLiveData
        var liveResult = await controller.GetLiveData((int)ev.Id);
        Assert.IsType<ForbidResult>(liveResult);
    }

    [Fact]
    public async Task Elector_DeniedAccess_WhenEventActiveAndHasNotVoted()
    {
        await using var context = CreateContext();
        var fakeVotingService = new FakeVotingService { HasVotedReturnValue = false };

        var ev = new VotingEvent
        {
            Id = 10,
            Title = "Elección Activa",
            ElectionType = "PERSONAS",
            PositionId = 1,
            Status = "ACTIVA",
            CreatedByVoterId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(DateTime.Today),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(DateTime.Today),
            RegistrationEndTime = new TimeOnly(12, 0),
            ProposalsStartDate = DateOnly.FromDateTime(DateTime.Today),
            ProposalsStartTime = new TimeOnly(12, 0),
            ProposalsEndDate = DateOnly.FromDateTime(DateTime.Today),
            ProposalsEndTime = new TimeOnly(14, 0),
            VotingStartDate = DateOnly.FromDateTime(DateTime.Today),
            VotingStartTime = new TimeOnly(14, 0),
            VotingEndDate = DateOnly.FromDateTime(DateTime.Today),
            VotingEndTime = new TimeOnly(18, 0)
        };
        context.VotingEvents.Add(ev);

        var voter = new Voter
        {
            Id = 100,
            RoleId = 3,
            FullName = "Elector Test",
            GradeId = 11,
            Status = "ACTIVO",
            ContactEmail = "elector@test.com",
            PasswordHash = "hash",
            DocumentHash = "doc",
            EncryptedDocument = "enc",
            RegisteredAt = DateTime.UtcNow
        };
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        var controller = CreateController(context, fakeVotingService, "ELECTOR", voter.Id);

        var indexResult = await controller.Index((int)ev.Id);
        var redirect = Assert.IsType<RedirectToActionResult>(indexResult);
        Assert.Equal("Dashboard", redirect.ActionName);

        var liveResult = await controller.GetLiveData((int)ev.Id);
        Assert.IsType<ForbidResult>(liveResult);
    }

    [Fact]
    public async Task Elector_AllowedAccess_WhenEventActiveAndHasVoted()
    {
        await using var context = CreateContext();
        var fakeVotingService = new FakeVotingService { HasVotedReturnValue = true };

        var ev = new VotingEvent
        {
            Id = 10,
            Title = "Elección Activa",
            ElectionType = "PERSONAS",
            PositionId = 1,
            Status = "ACTIVA",
            CreatedByVoterId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(DateTime.Today),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(DateTime.Today),
            RegistrationEndTime = new TimeOnly(12, 0),
            ProposalsStartDate = DateOnly.FromDateTime(DateTime.Today),
            ProposalsStartTime = new TimeOnly(12, 0),
            ProposalsEndDate = DateOnly.FromDateTime(DateTime.Today),
            ProposalsEndTime = new TimeOnly(14, 0),
            VotingStartDate = DateOnly.FromDateTime(DateTime.Today),
            VotingStartTime = new TimeOnly(14, 0),
            VotingEndDate = DateOnly.FromDateTime(DateTime.Today),
            VotingEndTime = new TimeOnly(18, 0)
        };
        context.VotingEvents.Add(ev);

        var voter = new Voter
        {
            Id = 100,
            RoleId = 3,
            FullName = "Elector Test",
            GradeId = 11,
            Status = "ACTIVO",
            ContactEmail = "elector@test.com",
            PasswordHash = "hash",
            DocumentHash = "doc",
            EncryptedDocument = "enc",
            RegisteredAt = DateTime.UtcNow
        };
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        var controller = CreateController(context, fakeVotingService, "ELECTOR", voter.Id);

        var indexResult = await controller.Index((int)ev.Id);
        var viewResult = Assert.IsType<ViewResult>(indexResult);
        Assert.NotNull(viewResult);

        var liveResult = await controller.GetLiveData((int)ev.Id);
        var jsonResult = Assert.IsType<JsonResult>(liveResult);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task Elector_AllowedAccess_WhenEventFinalizedAndGradeEnabled()
    {
        await using var context = CreateContext();
        var fakeVotingService = new FakeVotingService();

        var ev = new VotingEvent
        {
            Id = 10,
            Title = "Elección Finalizada",
            ElectionType = "PERSONAS",
            PositionId = 1,
            Status = "FINALIZADA",
            CreatedByVoterId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-5)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-4)),
            RegistrationEndTime = new TimeOnly(12, 0),
            ProposalsStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-4)),
            ProposalsStartTime = new TimeOnly(12, 0),
            ProposalsEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-2)),
            ProposalsEndTime = new TimeOnly(14, 0),
            VotingStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(-1)),
            VotingEndTime = new TimeOnly(16, 0)
        };
        context.VotingEvents.Add(ev);
        context.EventGrades.Add(new EventGrade { VotingEventId = ev.Id, GradeId = 11 });

        var voter = new Voter
        {
            Id = 100,
            RoleId = 3,
            FullName = "Elector Test",
            GradeId = 11, // Grade enabled
            Status = "ACTIVO",
            ContactEmail = "elector@test.com",
            PasswordHash = "hash",
            DocumentHash = "doc",
            EncryptedDocument = "enc",
            RegisteredAt = DateTime.UtcNow
        };
        context.Voters.Add(voter);
        await context.SaveChangesAsync();

        var controller = CreateController(context, fakeVotingService, "ELECTOR", voter.Id);

        var indexResult = await controller.Index((int)ev.Id);
        var viewResult = Assert.IsType<ViewResult>(indexResult);
        Assert.NotNull(viewResult);

        var liveResult = await controller.GetLiveData((int)ev.Id);
        var jsonResult = Assert.IsType<JsonResult>(liveResult);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public async Task Admin_UnrestrictedAccess_AcrossAllStatuses()
    {
        await using var context = CreateContext();
        var fakeVotingService = new FakeVotingService();

        var ev = new VotingEvent
        {
            Id = 20,
            Title = "Elección Programada Admin",
            ElectionType = "PERSONAS",
            PositionId = 1,
            Status = "PROGRAMADA",
            CreatedByVoterId = 1,
            RegistrationStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            RegistrationEndTime = new TimeOnly(12, 0),
            ProposalsStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(2)),
            ProposalsStartTime = new TimeOnly(12, 0),
            ProposalsEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(3)),
            ProposalsEndTime = new TimeOnly(14, 0),
            VotingStartDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4)),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = DateOnly.FromDateTime(DateTime.Today.AddDays(4)),
            VotingEndTime = new TimeOnly(16, 0)
        };
        context.VotingEvents.Add(ev);
        await context.SaveChangesAsync();

        var controller = CreateController(context, fakeVotingService, "ADMIN", 1);

        // Even though status is PROGRAMADA and admin has no vote, admin has unrestricted access (RN-5)
        var indexResult = await controller.Index((int)ev.Id);
        var viewResult = Assert.IsType<ViewResult>(indexResult);
        Assert.NotNull(viewResult);

        var liveResult = await controller.GetLiveData((int)ev.Id);
        var jsonResult = Assert.IsType<JsonResult>(liveResult);
        Assert.NotNull(jsonResult.Value);
    }

    [Fact]
    public void TieDetection_IdentifiesMultipleApprovedCandidatesWithSameMaxVotes()
    {
        var candidates = new List<VwVoteCount>
        {
            new() { CandidateId = 1, CandidateName = "Candidato A", TotalVotes = 50, IsBlankVote = false },
            new() { CandidateId = 2, CandidateName = "Candidato B", TotalVotes = 50, IsBlankVote = false },
            new() { CandidateId = 3, CandidateName = "Candidato C", TotalVotes = 20, IsBlankVote = false },
            new() { CandidateId = 4, CandidateName = "Voto en Blanco", TotalVotes = 10, IsBlankVote = true }
        };

        var approvedCandidates = candidates.Where(c => !c.IsBlankVote).ToList();
        var maxVotes = approvedCandidates.Any() ? approvedCandidates.Max(c => c.TotalVotes) : 0;
        var topCandidates = approvedCandidates.Where(c => c.TotalVotes == maxVotes && maxVotes > 0).ToList();
        bool isTie = topCandidates.Count > 1;

        Assert.True(isTie);
        Assert.Equal(2, topCandidates.Count);
        Assert.Equal(50, maxVotes);
    }

    [Fact]
    public void TieDetection_IdentifiesSingleWinnerWhenVotesDiffer()
    {
        var candidates = new List<VwVoteCount>
        {
            new() { CandidateId = 1, CandidateName = "Candidato A", TotalVotes = 70, IsBlankVote = false },
            new() { CandidateId = 2, CandidateName = "Candidato B", TotalVotes = 50, IsBlankVote = false },
            new() { CandidateId = 3, CandidateName = "Voto en Blanco", TotalVotes = 10, IsBlankVote = true }
        };

        var approvedCandidates = candidates.Where(c => !c.IsBlankVote).ToList();
        var maxVotes = approvedCandidates.Any() ? approvedCandidates.Max(c => c.TotalVotes) : 0;
        var topCandidates = approvedCandidates.Where(c => c.TotalVotes == maxVotes && maxVotes > 0).ToList();
        bool isTie = topCandidates.Count > 1;

        Assert.False(isTie);
        Assert.Single(topCandidates);
        Assert.Equal("Candidato A", topCandidates[0].CandidateName);
    }
}
