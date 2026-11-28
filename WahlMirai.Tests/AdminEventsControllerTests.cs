// ============================================================
// Archivo:       AdminEventsControllerTests.cs
// Módulo:        M03 — Gestión de Procesos Electorales (AdminEventsController)
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias para AdminEventsController e integración directa
//                con EventService y CandidateReviewService: Index, Create (GET/POST),
//                Edit (GET/POST + RN-7.2), Delete (RN-7.1), AddCandidate,
//                AddProposalOption, GetPositionRequirements, SearchVoter y
//                verificación de ausencia de [ValidateAntiForgeryToken] (R3).
// Requerimientos: RF-M03-00, RF-M03-01, RN-7.1, RN-7.2, RN-12.
// ============================================================
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias de <see cref="AdminEventsController"/> sobre EF Core InMemory y
/// <see cref="FakeTimeProvider"/> (RF-M03-00, RF-M03-01, RN-7.1, RN-7.2, RN-12).
/// </summary>
public sealed class AdminEventsControllerTests
{
    private static readonly DateOnly EventDay = new(2026, 6, 15);
    private static readonly DateTime AnchorBeforeRegLocal = EventDay.ToDateTime(new TimeOnly(7, 0));

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

        context.Voters.Add(new Voter
        {
            Id = 1,
            RoleId = Roles.Admin,
            GradeId = null,
            DocumentHash = ComputeSha256("10000001"),
            EncryptedDocument = "enc-10000001",
            FullName = "Admin Principal",
            ContactEmail = "admin@test.local",
            PasswordHash = "hash",
            Status = "ACTIVO",
            RegisteredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

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

    private static AdminEventsController CreateController(
        WahlMiraiDbContext context,
        FakeTimeProvider timeProvider,
        FakeAuditService? auditService = null,
        uint adminUserId = 1)
    {
        var audit = auditService ?? new FakeAuditService();
        var eventService = new EventService(context, audit, timeProvider);
        var candidateReviewService = new CandidateReviewService(context, audit);

        var httpContext = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("127.0.0.10") },
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()),
                new Claim(ClaimTypes.Role, Roles.AdminName)
            }, "TestAuth"))
        };

        return new AdminEventsController(eventService, candidateReviewService, context)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider())
        };
    }

    private static VotingEvent BuildContiguousEvent(
        uint id = 0,
        uint positionId = 1,
        string electionType = "PERSONAS",
        string status = "PROGRAMADA",
        DateOnly? day = null)
    {
        var d = day ?? EventDay;
        return new VotingEvent
        {
            Id = id,
            CreatedByVoterId = 1,
            PositionId = positionId,
            Title = $"Elección {positionId} - {status}",
            Description = "Descripción de prueba",
            ElectionType = electionType,
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
            VotingEndTime = new TimeOnly(18, 0),
            Status = status,
            CreatedAt = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }

    private static string ComputeSha256(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(bytes);
    }

    /// <summary>Verifica que Index excluye procesos ELIMINADO y actualiza etapas vencidas según el reloj (RN-7.1, RN-12).</summary>
    [Fact]
    public async Task Index_ReturnsViewWithNonDeletedEventsAndAutoAdvancesStages()
    {
        using var context = CreateInMemoryContext();
        var pastDay = new DateOnly(2026, 6, 10);
        var futureDay = new DateOnly(2026, 6, 20);

        context.VotingEvents.AddRange(
            BuildContiguousEvent(id: 1, positionId: 1, status: "ACTIVA", day: pastDay),
            BuildContiguousEvent(id: 2, positionId: 2, status: "PROGRAMADA", day: futureDay),
            BuildContiguousEvent(id: 3, positionId: 1, status: "ELIMINADO", day: pastDay));
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal));

        var result = await controller.Index();

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<List<VotingEvent>>(view.Model);
        Assert.Equal(2, model.Count);
        Assert.DoesNotContain(model, e => e.Status == "ELIMINADO");

        var expired = Assert.Single(model, e => e.Id == 1);
        Assert.Equal("FINALIZADA", expired.Status);
        var upcoming = Assert.Single(model, e => e.Id == 2);
        Assert.Equal("PROGRAMADA", upcoming.Status);
    }

    /// <summary>Verifica que Create (GET) prepara ViewBags, posiciones activas y modelo PERSONAS por defecto (RF-M03-00, RF-M03-01).</summary>
    [Fact]
    public async Task Create_Get_PopulatesViewBagsAndDefaultsPersonasEvent()
    {
        using var context = CreateInMemoryContext();
        var activeFuture = DateOnly.FromDateTime(DateTime.Now.AddDays(10));
        var pastFinished = DateOnly.FromDateTime(DateTime.Now.AddDays(-5));

        context.VotingEvents.AddRange(
            BuildContiguousEvent(id: 1, positionId: 1, status: "PROGRAMADA", day: activeFuture),
            BuildContiguousEvent(id: 2, positionId: 2, status: "FINALIZADA", day: pastFinished));
        context.PositionRequirements.Add(new PositionRequirement
        {
            Id = 1,
            PositionId = 2,
            Description = "Certificado académico",
            IsMandatory = true,
            DisplayOrder = 1
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal));

        var result = controller.Create();

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Form", view.ViewName);
        var model = Assert.IsType<VotingEvent>(view.Model);
        Assert.Equal("PERSONAS", model.ElectionType);
        Assert.Equal(2u, model.PositionId);

        var activeIds = Assert.IsType<List<uint>>(controller.ViewBag.ActivePositionIds);
        Assert.Contains(1u, activeIds);
        Assert.DoesNotContain(2u, activeIds);

        var grades = Assert.IsType<List<Grade>>(controller.ViewBag.Grades);
        Assert.Equal(2, grades.Count);

        var reqs = Assert.IsType<List<PositionRequirement>>(controller.ViewBag.PositionRequirements);
        Assert.Single(reqs);
    }

    /// <summary>Verifica que Create (POST) persiste evento PERSONAS contiguo, grados, Voto en Blanco y requisitos (RF-M03-00, RF-M03-01, RN-12).</summary>
    [Fact]
    public async Task Create_Post_ValidContiguousPersonasEvent_PersistsEventGradesBlankVoteRequirementsAndRedirectsToEdit()
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();
        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal), audit);
        var model = BuildContiguousEvent(positionId: 1);
        var reqJson = JsonSerializer.Serialize(new[]
        {
            new RequirementInputDto { Description = "Carta de motivación", IsMandatory = true },
            new RequirementInputDto { Description = "Hoja de vida", IsMandatory = false }
        });

        var result = await controller.Create(model, new List<byte> { 10, 11 }, reqJson);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminEventsController.Edit), redirect.ActionName);
        Assert.NotNull(controller.TempData["Success"]);
        Assert.Null(controller.TempData["Error"]);

        var created = await context.VotingEvents
            .Include(e => e.EventGrades)
            .Include(e => e.Candidates)
            .SingleAsync();
        Assert.Equal("PROGRAMADA", created.Status);
        Assert.Equal(2, created.EventGrades.Count);

        var blankVote = Assert.Single(created.Candidates);
        Assert.True(blankVote.IsBlankVote);
        Assert.Equal("Voto en Blanco", blankVote.Name);
        Assert.Equal("APROBADO", blankVote.Status);
        Assert.Null(blankVote.VoterId);

        var reqs = await context.PositionRequirements.Where(r => r.PositionId == 1).OrderBy(r => r.DisplayOrder).ToListAsync();
        Assert.Equal(2, reqs.Count);
        Assert.Equal("Carta de motivación", reqs[0].Description);
        Assert.True(reqs[0].IsMandatory);
        Assert.Equal("Hoja de vida", reqs[1].Description);
        Assert.False(reqs[1].IsMandatory);

        Assert.Contains("EVENT_CREATED", audit.Actions);
    }

    /// <summary>Verifica que Create (POST) rechaza TEMAS, votación multidía, huecos/solapamientos RN-12 y cargo con proceso activo.</summary>
    [Theory]
    [InlineData("TEMAS")]
    [InlineData("MULTI_DAY")]
    [InlineData("GAP_RN12")]
    [InlineData("OVERLAP_RN12")]
    [InlineData("DUPLICATE_ACTIVE_POSITION")]
    public async Task Create_Post_InvalidInputs_SetsTempDataErrorAndReturnsFormView(string scenario)
    {
        using var context = CreateInMemoryContext();
        if (scenario == "DUPLICATE_ACTIVE_POSITION")
        {
            context.VotingEvents.Add(BuildContiguousEvent(id: 99, positionId: 1, status: "INSCRIPCION"));
            await context.SaveChangesAsync();
        }

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal));
        var model = BuildContiguousEvent(positionId: 1);

        switch (scenario)
        {
            case "TEMAS":
                model.ElectionType = "TEMAS";
                break;
            case "MULTI_DAY":
                model.VotingEndDate = EventDay.AddDays(1);
                break;
            case "GAP_RN12":
                model.ProposalsStartTime = new TimeOnly(10, 30);
                break;
            case "OVERLAP_RN12":
                model.VotingStartTime = new TimeOnly(11, 30);
                break;
        }

        var result = await controller.Create(model, new List<byte> { 10, 11 }, null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Form", view.ViewName);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Null(controller.TempData["Success"]);

        var expectedCount = scenario == "DUPLICATE_ACTIVE_POSITION" ? 1 : 0;
        Assert.Equal(expectedCount, await context.VotingEvents.CountAsync());
    }

    /// <summary>Verifica que Edit (GET) devuelve 404 para procesos inexistentes o eliminados lógicamente (RN-7.1).</summary>
    [Theory]
    [InlineData(999u, false)]
    [InlineData(10u, true)]
    public async Task Edit_Get_MissingOrEliminado_ReturnsNotFound(uint id, bool seedEliminado)
    {
        using var context = CreateInMemoryContext();
        if (seedEliminado)
        {
            context.VotingEvents.Add(BuildContiguousEvent(id: id, status: "ELIMINADO"));
            await context.SaveChangesAsync();
        }

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal));

        var result = await controller.Edit(id);

        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>Verifica que Edit (GET) expone ViewBag.CandidatesForReview (sin voto en blanco) y ViewBag.IsReadOnly según estado (RN-7.2).</summary>
    [Theory]
    [InlineData("ACTIVA", false)]
    [InlineData("FINALIZADA", true)]
    public async Task Edit_Get_PersonasEvent_PopulatesCandidatesForReviewAndIsReadOnlyFlag(string stage, bool expectedReadOnly)
    {
        using var context = CreateInMemoryContext();
        var voter = new Voter
        {
            Id = 20,
            RoleId = Roles.Elector,
            GradeId = 11,
            DocumentHash = ComputeSha256("20202020"),
            EncryptedDocument = "enc-20202020",
            FullName = "Laura Gómez",
            ContactEmail = "laura@test.local",
            PasswordHash = "hash",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        };
        context.Voters.Add(voter);

        var ev = BuildContiguousEvent(id: 1, positionId: 1, status: stage);
        context.VotingEvents.Add(ev);
        context.Candidates.AddRange(
            new Candidate { Id = 1, VotingEventId = 1, VoterId = null, Name = "Voto en Blanco", IsBlankVote = true, Status = "APROBADO", EnrolledAt = DateTime.UtcNow },
            new Candidate { Id = 2, VotingEventId = 1, VoterId = 20, Name = "Laura Gómez", IsBlankVote = false, Status = "PENDIENTE", EnrolledAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var clock = stage == "FINALIZADA"
            ? EventDay.ToDateTime(new TimeOnly(19, 0))
            : EventDay.ToDateTime(new TimeOnly(13, 0));
        var controller = CreateController(context, CreateLocalFakeTimeProvider(clock));

        var result = await controller.Edit(1);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Form", view.ViewName);
        Assert.Equal(expectedReadOnly, (bool)controller.ViewBag.IsReadOnly);

        var candidates = Assert.IsType<List<CandidateReviewDetailDto>>(controller.ViewBag.CandidatesForReview);
        var only = Assert.Single(candidates);
        Assert.Equal(2u, only.CandidateId);
        Assert.Equal("Laura Gómez", only.CandidateName);
    }

    /// <summary>Verifica que Edit (POST) rechaza edición en FINALIZADA (almacenada o expirada por reloj) preservando BD (RN-7.2).</summary>
    [Theory]
    [InlineData("FINALIZADA")]
    [InlineData("ACTIVA")]
    public async Task Edit_Post_Finalizada_StoredOrExpiredClock_RejectsWithRn72ErrorAndPreservesDb(string initialStatus)
    {
        using var context = CreateInMemoryContext();
        var original = BuildContiguousEvent(id: 1, positionId: 1, status: initialStatus);
        original.Title = "Título Original Inmutable";
        context.VotingEvents.Add(original);
        context.EventGrades.Add(new EventGrade { VotingEventId = 1, GradeId = 11 });
        await context.SaveChangesAsync();

        var afterVotingEnd = EventDay.ToDateTime(new TimeOnly(19, 0));
        var controller = CreateController(context, CreateLocalFakeTimeProvider(afterVotingEnd));

        var updateAttempt = BuildContiguousEvent(id: 1, positionId: 1, status: "ACTIVA");
        updateAttempt.Title = "Título Modificado Prohibido";

        var result = await controller.Edit(updateAttempt, new List<byte> { 10 }, null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirect.ActionName);
        var error = Assert.IsType<string>(controller.TempData["Error"]);
        Assert.Contains("RN-7.2", error);
        Assert.Null(controller.TempData["Success"]);

        var reloaded = await context.VotingEvents.Include(e => e.EventGrades).SingleAsync(e => e.Id == 1);
        Assert.Equal("Título Original Inmutable", reloaded.Title);
        Assert.Equal("FINALIZADA", reloaded.Status);
        Assert.Single(reloaded.EventGrades);
        Assert.Equal((byte)11, reloaded.EventGrades.Single().GradeId);
    }

    /// <summary>Verifica que Edit (POST) válido actualiza metadatos, sincroniza grados y registra auditoría.</summary>
    [Fact]
    public async Task Edit_Post_ValidUpdate_UpdatesFieldsSyncsGradesAndRedirects()
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();
        context.VotingEvents.Add(BuildContiguousEvent(id: 1, positionId: 1, status: "PROGRAMADA"));
        context.EventGrades.Add(new EventGrade { VotingEventId = 1, GradeId = 10 });
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal), audit);
        var updatedModel = BuildContiguousEvent(id: 1, positionId: 1, status: "PROGRAMADA");
        updatedModel.Title = "Elección Personero Actualizada";
        updatedModel.VotingEndTime = new TimeOnly(17, 0);

        var reqJson = JsonSerializer.Serialize(new[]
        {
            new RequirementInputDto { Description = "Paz y salvo", IsMandatory = true }
        });

        var result = await controller.Edit(updatedModel, new List<byte> { 11 }, reqJson);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirect.ActionName);
        Assert.NotNull(controller.TempData["Success"]);
        Assert.Null(controller.TempData["Error"]);

        var persisted = await context.VotingEvents.Include(e => e.EventGrades).SingleAsync(e => e.Id == 1);
        Assert.Equal("Elección Personero Actualizada", persisted.Title);
        Assert.Equal(new TimeOnly(17, 0), persisted.VotingEndTime);
        var grade = Assert.Single(persisted.EventGrades);
        Assert.Equal((byte)11, grade.GradeId);
        Assert.Contains("EVENT_UPDATED", audit.Actions);
        Assert.Single(context.PositionRequirements);
    }

    /// <summary>Verifica que Edit (POST) inválido retorna View("Form") con TempData["Error"] y CandidatesForReview repoblado.</summary>
    [Fact]
    public async Task Edit_Post_InvalidContiguityOrTemas_SetsTempDataErrorAndReturnsFormWithCandidates()
    {
        using var context = CreateInMemoryContext();
        context.VotingEvents.Add(BuildContiguousEvent(id: 1, positionId: 1, status: "PROGRAMADA"));
        context.Candidates.Add(new Candidate
        {
            Id = 5,
            VotingEventId = 1,
            VoterId = 1,
            Name = "Candidato Prueba",
            IsBlankVote = false,
            Status = "PENDIENTE",
            EnrolledAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal));
        var invalid = BuildContiguousEvent(id: 1, positionId: 1, status: "PROGRAMADA");
        invalid.ProposalsStartTime = new TimeOnly(10, 15); // rompe RN-12

        var result = await controller.Edit(invalid, new List<byte> { 10, 11 }, null);

        var view = Assert.IsType<ViewResult>(result);
        Assert.Equal("Form", view.ViewName);
        Assert.NotNull(controller.TempData["Error"]);
        var candidates = Assert.IsType<List<CandidateReviewDetailDto>>(controller.ViewBag.CandidatesForReview);
        Assert.Single(candidates);
    }

    /// <summary>Verifica que Delete (POST) aplica borrado lógico (RN-7.1) en cualquier etapa del proceso.</summary>
    [Theory]
    [InlineData("PROGRAMADA")]
    [InlineData("INSCRIPCION")]
    [InlineData("PROPUESTAS")]
    [InlineData("ACTIVA")]
    [InlineData("FINALIZADA")]
    public async Task Delete_Post_AllowedInAnyStage_SoftDeletesAndRedirectsToIndex(string stage)
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();
        context.VotingEvents.Add(BuildContiguousEvent(id: 1, positionId: 1, status: stage));
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal), audit);

        var result = await controller.Delete(1);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminEventsController.Index), redirect.ActionName);
        Assert.NotNull(controller.TempData["Success"]);

        var deleted = await context.VotingEvents.SingleAsync(e => e.Id == 1);
        Assert.Equal("ELIMINADO", deleted.Status);
        Assert.NotNull(deleted.DeletedAt);
        Assert.Contains("EVENT_DELETED", audit.Actions);
    }

    /// <summary>Verifica que Delete (POST) establece TempData["Error"] si el proceso no existe o ya fue eliminado.</summary>
    [Theory]
    [InlineData(999u, false)]
    [InlineData(1u, true)]
    public async Task Delete_Post_MissingOrAlreadyEliminado_SetsTempDataError(uint id, bool seedEliminado)
    {
        using var context = CreateInMemoryContext();
        if (seedEliminado)
        {
            context.VotingEvents.Add(BuildContiguousEvent(id: id, status: "ELIMINADO"));
            await context.SaveChangesAsync();
        }

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal));

        var result = await controller.Delete(id);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(AdminEventsController.Index), redirect.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Null(controller.TempData["Success"]);
    }

    /// <summary>Verifica que AddCandidate retorna 404 para proceso inexistente y bloquea procesos FINALIZADA (RN-7.2).</summary>
    [Fact]
    public async Task AddCandidate_Post_MissingReturnsNotFound_AndFinalizadaBlockedWithRn72Error()
    {
        using var context = CreateInMemoryContext();
        context.VotingEvents.Add(BuildContiguousEvent(id: 1, positionId: 1, status: "FINALIZADA"));
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateLocalFakeTimeProvider(EventDay.ToDateTime(new TimeOnly(19, 0))));

        var missing = await controller.AddCandidate(999, 1, "Lema", null);
        Assert.IsType<NotFoundResult>(missing);

        var finalizada = await controller.AddCandidate(1, 1, "Lema", null);
        var redirect = Assert.IsType<RedirectToActionResult>(finalizada);
        Assert.Equal("Edit", redirect.ActionName);
        var err = Assert.IsType<string>(controller.TempData["Error"]);
        Assert.Contains("RN-7.2", err);
    }

    /// <summary>Verifica que AddCandidate bloquea etapas distintas a PROGRAMADA y exige grado 11 (IsLastGrade) para Personero.</summary>
    [Theory]
    [InlineData("INSCRIPCION", 11)]
    [InlineData("ACTIVA", 11)]
    [InlineData("PROGRAMADA", 10)]
    public async Task AddCandidate_Post_BlockedOutsideProgramadaOrNonLastGradePersonero_SetsTempDataError(string stage, byte voterGradeId)
    {
        using var context = CreateInMemoryContext();
        context.VotingEvents.Add(BuildContiguousEvent(id: 1, positionId: 1, status: stage));
        context.Voters.Add(new Voter
        {
            Id = 50,
            RoleId = Roles.Elector,
            GradeId = voterGradeId,
            DocumentHash = ComputeSha256("50505050"),
            EncryptedDocument = "enc-50505050",
            FullName = "Estudiante Prueba",
            ContactEmail = "est@test.local",
            PasswordHash = "hash",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var clock = stage switch
        {
            "INSCRIPCION" => EventDay.ToDateTime(new TimeOnly(9, 0)),
            "ACTIVA" => EventDay.ToDateTime(new TimeOnly(13, 0)),
            _ => AnchorBeforeRegLocal
        };
        var controller = CreateController(context, CreateLocalFakeTimeProvider(clock));

        var result = await controller.AddCandidate(1, 50, "Por el cambio", null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirect.ActionName);
        Assert.NotNull(controller.TempData["Error"]);
        Assert.Empty(context.Candidates);
    }

    /// <summary>Verifica que AddCandidate en PROGRAMADA con elector ACTIVO de grado 11 persiste candidato APROBADO y audita.</summary>
    [Fact]
    public async Task AddCandidate_Post_ValidProgramadaPersoneroWithGrade11_PersistsCandidateAndAudits()
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();
        context.VotingEvents.Add(BuildContiguousEvent(id: 1, positionId: 1, status: "PROGRAMADA"));
        context.Voters.Add(new Voter
        {
            Id = 51,
            RoleId = Roles.Elector,
            GradeId = 11,
            DocumentHash = ComputeSha256("51515151"),
            EncryptedDocument = "enc-51515151",
            FullName = "Carlos Mendoza",
            ContactEmail = "carlos@test.local",
            PasswordHash = "hash",
            Status = "ACTIVO",
            RegisteredAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal), audit);

        var result = await controller.AddCandidate(1, 51, "Más deporte", "/img/c51.png");

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Edit", redirect.ActionName);
        Assert.NotNull(controller.TempData["Success"]);

        var cand = await context.Candidates.SingleAsync();
        Assert.Equal(51u, cand.VoterId);
        Assert.Equal("Carlos Mendoza", cand.Name);
        Assert.Equal("APROBADO", cand.Status);
        Assert.False(cand.IsBlankVote);
        Assert.Contains("CANDIDATE_ADDED", audit.Actions);
    }

    /// <summary>Verifica validaciones y flujo exitoso de AddProposalOption (404, RN-7.2 FINALIZADA, PERSONAS/no-PROGRAMADA y TEMAS PROGRAMADA).</summary>
    [Fact]
    public async Task AddProposalOption_Post_ValidatesMissingFinalizadaTypeAndStage_AndPersistsOnProgramadaTemas()
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();
        context.VotingEvents.AddRange(
            BuildContiguousEvent(id: 1, positionId: 1, electionType: "PERSONAS", status: "PROGRAMADA"),
            BuildContiguousEvent(id: 2, positionId: 2, electionType: "TEMAS", status: "FINALIZADA"),
            BuildContiguousEvent(id: 3, positionId: 2, electionType: "TEMAS", status: "ACTIVA"),
            BuildContiguousEvent(id: 4, positionId: 2, electionType: "TEMAS", status: "PROGRAMADA"));
        await context.SaveChangesAsync();

        var controllerBefore = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal), audit);

        // 404
        Assert.IsType<NotFoundResult>(await controllerBefore.AddProposalOption(999, "Opción X", null, null));

        // PERSONAS rechazado
        await controllerBefore.AddProposalOption(1, "Opción X", null, null);
        Assert.NotNull(controllerBefore.TempData["Error"]);

        // TEMAS PROGRAMADA válido
        var validResult = await controllerBefore.AddProposalOption(4, "Horario Extendido", "Más biblioteca", null);
        var validRedirect = Assert.IsType<RedirectToActionResult>(validResult);
        Assert.Equal("Edit", validRedirect.ActionName);
        Assert.NotNull(controllerBefore.TempData["Success"]);

        var option = await context.Candidates.SingleAsync();
        Assert.Equal(4u, option.VotingEventId);
        Assert.Null(option.VoterId);
        Assert.Equal("Horario Extendido", option.Name);
        Assert.Equal("APROBADO", option.Status);
        Assert.False(option.IsBlankVote);
        Assert.Contains("PROPOSAL_OPTION_ADDED", audit.Actions);

        // TEMAS ACTIVA rechazado por etapa
        var controllerActive = CreateController(context, CreateLocalFakeTimeProvider(EventDay.ToDateTime(new TimeOnly(13, 0))));
        await controllerActive.AddProposalOption(3, "Otra Opción", null, null);
        Assert.NotNull(controllerActive.TempData["Error"]);

        // TEMAS FINALIZADA rechazado por RN-7.2
        var controllerFinished = CreateController(context, CreateLocalFakeTimeProvider(EventDay.ToDateTime(new TimeOnly(19, 0))));
        await controllerFinished.AddProposalOption(2, "Opción Tardía", null, null);
        var rn72Error = Assert.IsType<string>(controllerFinished.TempData["Error"]);
        Assert.Contains("RN-7.2", rn72Error);
    }

    /// <summary>Verifica que GetPositionRequirements devuelve los requisitos del cargo ordenados por DisplayOrder en JSON.</summary>
    [Fact]
    public async Task GetPositionRequirements_ReturnsOrderedJson()
    {
        using var context = CreateInMemoryContext();
        context.PositionRequirements.AddRange(
            new PositionRequirement { Id = 1, PositionId = 1, Description = "Segundo", IsMandatory = false, DisplayOrder = 2 },
            new PositionRequirement { Id = 2, PositionId = 1, Description = "Primero", IsMandatory = true, DisplayOrder = 1 },
            new PositionRequirement { Id = 3, PositionId = 2, Description = "Otro cargo", IsMandatory = true, DisplayOrder = 1 });
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal));

        var result = await controller.GetPositionRequirements(1);

        var json = Assert.IsType<JsonResult>(result);
        var array = JsonSerializer.SerializeToElement(json.Value);
        Assert.Equal(JsonValueKind.Array, array.ValueKind);
        Assert.Equal(2, array.GetArrayLength());
        Assert.Equal("Primero", array[0].GetProperty("description").GetString());
        Assert.True(array[0].GetProperty("isMandatory").GetBoolean());
        Assert.Equal("Segundo", array[1].GetProperty("description").GetString());
        Assert.False(array[1].GetProperty("isMandatory").GetBoolean());
    }

    /// <summary>Verifica que SearchVoter filtra solo electores ACTIVO, busca por nombre o SHA-256 de documento y enmascara el documento.</summary>
    [Fact]
    public async Task SearchVoter_FiltersActiveElectors_SearchesNameAndSha256Document_AndMasksDocument()
    {
        using var context = CreateInMemoryContext();
        context.Voters.AddRange(
            new Voter
            {
                Id = 10,
                RoleId = Roles.Elector,
                GradeId = 11,
                DocumentHash = ComputeSha256("12345678"),
                EncryptedDocument = "CIPHERTEXT9876",
                FullName = "Sofía Ramírez",
                ContactEmail = "sofia@test.local",
                PasswordHash = "hash",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            },
            new Voter
            {
                Id = 11,
                RoleId = Roles.Elector,
                GradeId = 10,
                DocumentHash = ComputeSha256("99887766"),
                EncryptedDocument = "PENDIENTE_CIFRAR:99887766",
                FullName = "Sofía Inactiva",
                ContactEmail = "inactiva@test.local",
                PasswordHash = "hash",
                Status = "INACTIVO",
                RegisteredAt = DateTime.UtcNow
            },
            new Voter
            {
                Id = 12,
                RoleId = Roles.Admin,
                GradeId = null,
                DocumentHash = ComputeSha256("77778888"),
                EncryptedDocument = "CIPHERADMIN1234",
                FullName = "Sofía Admin",
                ContactEmail = "sadmin@test.local",
                PasswordHash = "hash",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var controller = CreateController(context, CreateLocalFakeTimeProvider(AnchorBeforeRegLocal));

        var emptyResult = Assert.IsType<JsonResult>(await controller.SearchVoter("   "));
        Assert.Empty(Assert.IsType<List<VoterSearchResultDto>>(emptyResult.Value));

        var byName = Assert.IsType<JsonResult>(await controller.SearchVoter("sofía"));
        var nameMatches = Assert.IsType<List<VoterSearchResultDto>>(byName.Value);
        var only = Assert.Single(nameMatches);
        Assert.Equal(10u, only.Id);
        Assert.Equal("******9876", only.Documento);
        Assert.Equal("11°", only.GradeName);

        var byDoc = Assert.IsType<JsonResult>(await controller.SearchVoter("12345678"));
        var docMatches = Assert.IsType<List<VoterSearchResultDto>>(byDoc.Value);
        Assert.Single(docMatches);
        Assert.Equal(10u, docMatches[0].Id);
    }

    /// <summary>Documenta el estado actual de los 5 métodos [HttpPost] de AdminEventsController sin [ValidateAntiForgeryToken] (R3).</summary>
    [Fact]
    public void AdminEventsController_PostActions_CurrentBehaviorLacksValidateAntiForgeryTokenAttribute()
    {
        // NOTA: R3 — Ninguna de las 5 acciones [HttpPost] de AdminEventsController
        // (Create, Edit, Delete, AddCandidate, AddProposalOption) declara [ValidateAntiForgeryToken].
        // Fuera de alcance de P1–P4: no se modifica producción, se documenta aquí el comportamiento actual.
        var postMethods = typeof(AdminEventsController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        Assert.Equal(5, postMethods.Count);
        foreach (var method in postMethods)
        {
            Assert.Null(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }
    }
}
