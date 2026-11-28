// ============================================================
// Archivo:       AdminCandidatesControllerTests.cs
// Módulo:        M04 — Revisión de Candidaturas (AdminCandidatesController)
// Desarrollador: Kevin
// Descripción:   Pruebas unitarias para AdminCandidatesController y CandidateReviewService:
//                Index, GetCandidateDetail, Approve (normal y con excepción RN-10.1),
//                Reject (con/sin subsanación RN-10), Withdraw (solo desde APROBADO),
//                bloqueo RN-7.2 en FINALIZADA, validación local de returnUrl,
//                ServeDocument (anti path-traversal y descifrado .enc por MIME),
//                encolado en EmailQueue y documentación CSRF (R3).
// Requerimientos: RF-M04-02, RN-7.2, RN-9, RN-10, RN-10.1.
// ============================================================
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Controllers;
using WahlMirai.Web.Models;
using WahlMirai.Web.Services;

namespace WahlMirai.Tests;

/// <summary>
/// Pruebas unitarias de <see cref="AdminCandidatesController"/> sobre EF Core InMemory
/// y almacenamiento temporal aislado para soportes cifrados <c>.enc</c>
/// (RF-M04-02, RN-7.2, RN-9, RN-10, RN-10.1).
/// </summary>
public sealed class AdminCandidatesControllerTests : IDisposable
{
    private readonly string _tempUploadsBasePath;

    public AdminCandidatesControllerTests()
    {
        _tempUploadsBasePath = Path.Combine(
            AppContext.BaseDirectory,
            ".test-temp-candidates",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempUploadsBasePath, "candidacies"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempUploadsBasePath))
        {
            Directory.Delete(_tempUploadsBasePath, recursive: true);
        }
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

    private sealed class LocalValidatingUrlHelper : IUrlHelper
    {
        public ActionContext ActionContext => new();
        public string? Action(UrlActionContext actionContext) =>
            "/" + (actionContext.Controller ?? "AdminCandidates") + "/" + actionContext.Action;
        public string? Content(string? contentPath) => contentPath;
        public bool IsLocalUrl(string? url)
        {
            if (string.IsNullOrEmpty(url)) return false;
            if (url[0] == '/')
            {
                return url.Length == 1 || (url[1] != '/' && url[1] != '\\');
            }
            return url.StartsWith("~/", StringComparison.Ordinal);
        }
        public string? Link(string? routeName, object? values) => "/" + routeName;
        public string? RouteUrl(UrlRouteContext routeContext) => "/" + routeContext.RouteName;
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

        context.Grades.Add(new Grade { Id = 11, Name = "11°", SequenceOrder = 6, IsLastGrade = true });

        context.ElectionPositions.Add(new ElectionPosition
        {
            Id = 1,
            Name = "Personero",
            Status = "ACTIVO",
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        context.Voters.Add(new Voter
        {
            Id = 1,
            RoleId = Roles.Admin,
            GradeId = null,
            DocumentHash = "admin-hash",
            EncryptedDocument = "enc-10000001",
            FullName = "Admin Revisor",
            ContactEmail = "admin@test.local",
            PasswordHash = "hash",
            Status = "ACTIVO",
            RegisteredAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });

        context.SaveChanges();
        return context;
    }

    private AdminCandidatesController CreateController(
        WahlMiraiDbContext context,
        FakeAuditService? auditService = null,
        IDocumentEncryptionService? encryptionService = null,
        uint adminUserId = 1)
    {
        var audit = auditService ?? new FakeAuditService();
        var reviewService = new CandidateReviewService(context, audit);
        var eventService = new EventService(
            context,
            audit,
            new FakeTimeProvider(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)));

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CandidacyUploads:BasePath"] = _tempUploadsBasePath
            })
            .Build();

        var httpContext = new DefaultHttpContext
        {
            Connection = { RemoteIpAddress = IPAddress.Parse("127.0.0.20") },
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, adminUserId.ToString()),
                new Claim(ClaimTypes.Role, Roles.AdminName)
            }, "TestAuth"))
        };

        return new AdminCandidatesController(
            reviewService,
            eventService,
            context,
            encryptionService ?? new FakeEncryptionService(),
            config)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new InMemoryTempDataProvider()),
            Url = new LocalValidatingUrlHelper()
        };
    }

    private static (VotingEvent Event, Candidate Candidate) SeedEventAndCandidate(
        WahlMiraiDbContext context,
        string eventStatus = "INSCRIPCION",
        string candidateStatus = "PENDIENTE",
        bool withVoter = true,
        string contactEmail = "postulante@test.local",
        bool isBlankVote = false)
    {
        var ev = new VotingEvent
        {
            Id = 1,
            CreatedByVoterId = 1,
            PositionId = 1,
            Title = "Personero 2026",
            ElectionType = "PERSONAS",
            RegistrationStartDate = new DateOnly(2026, 6, 15),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = new DateOnly(2026, 6, 15),
            RegistrationEndTime = new TimeOnly(10, 0),
            ProposalsStartDate = new DateOnly(2026, 6, 15),
            ProposalsStartTime = new TimeOnly(10, 0),
            ProposalsEndDate = new DateOnly(2026, 6, 15),
            ProposalsEndTime = new TimeOnly(12, 0),
            VotingStartDate = new DateOnly(2026, 6, 15),
            VotingStartTime = new TimeOnly(12, 0),
            VotingEndDate = new DateOnly(2026, 6, 15),
            VotingEndTime = new TimeOnly(18, 0),
            Status = eventStatus,
            CreatedAt = DateTime.UtcNow
        };
        context.VotingEvents.Add(ev);

        uint? voterId = null;
        if (withVoter && !isBlankVote)
        {
            voterId = 10;
            context.Voters.Add(new Voter
            {
                Id = 10,
                RoleId = Roles.Elector,
                GradeId = 11,
                DocumentHash = "cand-hash-10",
                EncryptedDocument = "enc-20202020",
                FullName = "Mariana López",
                ContactEmail = contactEmail,
                PasswordHash = "hash",
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            });
        }

        var candidate = new Candidate
        {
            Id = 100,
            VotingEventId = 1,
            VoterId = voterId,
            Name = isBlankVote ? "Voto en Blanco" : "Mariana López",
            Status = candidateStatus,
            IsBlankVote = isBlankVote,
            EnrolledAt = DateTime.UtcNow
        };
        context.Candidates.Add(candidate);
        context.SaveChanges();
        return (ev, candidate);
    }

    /// <summary>Verifica que Index redirige a AdminEvents/Index.</summary>
    [Fact]
    public void Index_RedirectsToAdminEventsIndex()
    {
        using var context = CreateInMemoryContext();
        var controller = CreateController(context);

        var result = controller.Index();

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("AdminEvents", redirect.ControllerName);
    }

    /// <summary>Verifica que GetCandidateDetail retorna NotFound para ID inexistente o voto en blanco.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetCandidateDetail_MissingOrBlankVote_ReturnsNotFound(bool seedBlankVote)
    {
        using var context = CreateInMemoryContext();
        uint queryId = 999;
        if (seedBlankVote)
        {
            var (_, blank) = SeedEventAndCandidate(context, candidateStatus: "APROBADO", isBlankVote: true);
            queryId = blank.Id;
        }

        var controller = CreateController(context);

        var result = await controller.GetCandidateDetail(queryId);

        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>Verifica que GetCandidateDetail devuelve CandidateReviewDetailDto con requisitos ordenados y elegibilidad.</summary>
    [Fact]
    public async Task GetCandidateDetail_ExistingCandidate_ReturnsJsonWithRequirementsAndEligibility()
    {
        using var context = CreateInMemoryContext();
        var (_, candidate) = SeedEventAndCandidate(context);

        context.PositionRequirements.AddRange(
            new PositionRequirement { Id = 1, PositionId = 1, Description = "Carta opcional", IsMandatory = false, DisplayOrder = 2 },
            new PositionRequirement { Id = 2, PositionId = 1, Description = "Boletín de notas", IsMandatory = true, DisplayOrder = 1 });
        context.CandidacyDocuments.Add(new CandidacyDocument
        {
            Id = 1,
            CandidateId = candidate.Id,
            RequirementId = 2,
            FileUrl = "/uploads/candidacies/doc_2.pdf.enc",
            UploadedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var controller = CreateController(context);

        var result = await controller.GetCandidateDetail(candidate.Id);

        var json = Assert.IsType<JsonResult>(result);
        var dto = Assert.IsType<CandidateReviewDetailDto>(json.Value);
        Assert.Equal(candidate.Id, dto.CandidateId);
        Assert.Equal("Personero 2026", dto.EventTitle);
        Assert.Equal("Mariana López", dto.CandidateName);
        Assert.Equal("postulante@test.local", dto.CandidateEmail);
        Assert.Equal("11°", dto.GradeName);
        Assert.Equal(1, dto.MandatoryCount);
        Assert.Equal(1, dto.MandatoryUploadedCount);
        Assert.True(dto.IsEligible);
        Assert.Equal(2, dto.Requirements.Count);
        Assert.Equal("Boletín de notas", dto.Requirements[0].Description);
        Assert.True(dto.Requirements[0].IsUploaded);
        Assert.Equal("Carta opcional", dto.Requirements[1].Description);
        Assert.False(dto.Requirements[1].IsUploaded);
    }

    /// <summary>Verifica que Approve normal cambia estado a APROBADO, audita y encola correo CANDIDATURA_APROBADA en EmailQueue (RF-M04-02).</summary>
    [Fact]
    public async Task Approve_Post_Normal_SetsApprovedClearsRejectionAuditAndEnqueuesEmail()
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();
        var (_, candidate) = SeedEventAndCandidate(context, candidateStatus: "PENDIENTE");
        candidate.RejectionReason = "Motivo previo";
        await context.SaveChangesAsync();

        var controller = CreateController(context, audit);

        var result = await controller.Approve(candidate.Id, withExceptions: false, exceptionsDetail: null);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("AdminEvents", redirect.ControllerName);
        Assert.Equal("Candidatura aprobada correctamente.", controller.TempData["Success"]);

        var updated = await context.Candidates.SingleAsync(c => c.Id == candidate.Id);
        Assert.Equal("APROBADO", updated.Status);
        Assert.False(updated.ApprovedWithExceptions);
        Assert.Null(updated.ExceptionsDetail);
        Assert.Null(updated.RejectionReason);
        Assert.Equal(1u, updated.ReviewedByUserId);
        Assert.NotNull(updated.ReviewedAt);

        Assert.Contains("CANDIDACY_APPROVED", audit.Actions);

        var email = await context.EmailQueues.SingleAsync();
        Assert.Equal(10u, email.VoterId);
        Assert.Equal("CANDIDATURA_APROBADA", email.EmailType);
        Assert.Equal("PENDIENTE", email.Status);
        Assert.Equal((byte)0, email.Attempts);
    }

    /// <summary>Verifica que Approve no encola EmailQueue si el candidato carece de VoterId o de ContactEmail.</summary>
    [Theory]
    [InlineData(false, "")]
    [InlineData(true, "")]
    public async Task Approve_Post_WithoutVoterOrEmptyContactEmail_ApprovesWithoutEnqueueingEmail(bool withVoter, string contactEmail)
    {
        using var context = CreateInMemoryContext();
        var (_, candidate) = SeedEventAndCandidate(context, withVoter: withVoter, contactEmail: contactEmail);
        var controller = CreateController(context);

        await controller.Approve(candidate.Id, withExceptions: false, exceptionsDetail: null);

        var updated = await context.Candidates.SingleAsync(c => c.Id == candidate.Id);
        Assert.Equal("APROBADO", updated.Status);
        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica Approve con excepción válida (RN-10.1) y rechazo cuando exceptionsDetail está vacío o en blanco.</summary>
    [Fact]
    public async Task Approve_Post_WithExceptions_ValidDetailSucceeds()
    {
        using var context = CreateInMemoryContext();
        var (_, candidate) = SeedEventAndCandidate(context);
        var controller = CreateController(context);

        await controller.Approve(candidate.Id, withExceptions: true, exceptionsDetail: "Entrega certificado físico pendiente");

        Assert.Equal("Candidatura aprobada con excepción correctamente.", controller.TempData["Success"]);
        var updated = await context.Candidates.SingleAsync(c => c.Id == candidate.Id);
        Assert.Equal("APROBADO", updated.Status);
        Assert.True(updated.ApprovedWithExceptions);
        Assert.Equal("Entrega certificado físico pendiente", updated.ExceptionsDetail);
        Assert.Single(context.EmailQueues);
    }

    /// <summary>Verifica que Approve con withExceptions=true y detalle vacío/espacios es rechazado preservando estado PENDIENTE y sin encolar correo (RN-10.1).</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Approve_Post_WithExceptions_BlankDetailRejected(string? blankDetail)
    {
        using var context = CreateInMemoryContext();
        var (_, candidate) = SeedEventAndCandidate(context);
        var controller = CreateController(context);

        await controller.Approve(candidate.Id, withExceptions: true, exceptionsDetail: blankDetail);

        Assert.NotNull(controller.TempData["Error"]);
        Assert.Null(controller.TempData["Success"]);
        var reloaded = await context.Candidates.SingleAsync(c => c.Id == candidate.Id);
        Assert.Equal("PENDIENTE", reloaded.Status);
        Assert.False(reloaded.ApprovedWithExceptions);
        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica que Approve en proceso FINALIZADA es rechazado (RN-7.2) y no encola correo.</summary>
    [Fact]
    public async Task Approve_Post_FinalizadaEvent_RejectedWithRn72ErrorAndPreservesStatus()
    {
        using var context = CreateInMemoryContext();
        var (_, candidate) = SeedEventAndCandidate(context, eventStatus: "FINALIZADA");
        var controller = CreateController(context);

        await controller.Approve(candidate.Id, withExceptions: false, exceptionsDetail: null);

        var err = Assert.IsType<string>(controller.TempData["Error"]);
        Assert.Contains("RN-7.2", err);
        Assert.Equal("PENDIENTE", (await context.Candidates.SingleAsync(c => c.Id == candidate.Id)).Status);
        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica que Reject con allowCorrection=true/false persiste flags, audita y encola CANDIDATURA_RECHAZADA (RN-10).</summary>
    [Theory]
    [InlineData(true, "subsanar")]
    [InlineData(false, "definitivamente")]
    public async Task Reject_Post_AllowCorrectionTrueAndFalse_PersistsFlagsAuditAndEnqueuesEmail(bool allowCorrection, string expectedFragment)
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();
        var (_, candidate) = SeedEventAndCandidate(context);
        var controller = CreateController(context, audit);

        await controller.Reject(candidate.Id, "Documento ilegible", allowCorrection);

        var success = Assert.IsType<string>(controller.TempData["Success"]);
        Assert.Contains(expectedFragment, success);

        var updated = await context.Candidates.SingleAsync(c => c.Id == candidate.Id);
        Assert.Equal("RECHAZADO", updated.Status);
        Assert.Equal("Documento ilegible", updated.RejectionReason);
        Assert.Equal(allowCorrection, updated.AllowCorrection);
        Assert.False(updated.ApprovedWithExceptions);
        Assert.Null(updated.ExceptionsDetail);

        Assert.Contains("CANDIDACY_REJECTED", audit.Actions);
        var email = await context.EmailQueues.SingleAsync();
        Assert.Equal(10u, email.VoterId);
        Assert.Equal("CANDIDATURA_RECHAZADA", email.EmailType);
        Assert.Equal("PENDIENTE", email.Status);
        Assert.Equal((byte)0, email.Attempts);
    }

    /// <summary>Verifica que Reject rechaza motivo vacío (RN-10) o evento FINALIZADA (RN-7.2) sin modificar BD ni encolar correo.</summary>
    [Theory]
    [InlineData("INSCRIPCION", null)]
    [InlineData("INSCRIPCION", "")]
    [InlineData("INSCRIPCION", "   ")]
    [InlineData("FINALIZADA", "Motivo válido")]
    public async Task Reject_Post_BlankReasonOrFinalizadaEvent_SetsTempDataErrorAndPreservesStatus(string eventStatus, string? reason)
    {
        using var context = CreateInMemoryContext();
        var (_, candidate) = SeedEventAndCandidate(context, eventStatus: eventStatus);
        var controller = CreateController(context);

        await controller.Reject(candidate.Id, reason!, allowCorrection: true);

        Assert.NotNull(controller.TempData["Error"]);
        Assert.Null(controller.TempData["Success"]);
        Assert.Equal("PENDIENTE", (await context.Candidates.SingleAsync(c => c.Id == candidate.Id)).Status);
        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica que Withdraw desde APROBADO cambia a RETIRADO con AllowCorrection=false y no encola EmailQueue.</summary>
    [Fact]
    public async Task Withdraw_Post_FromApproved_SetsRetiradoWithoutEnqueueingEmail()
    {
        using var context = CreateInMemoryContext();
        var audit = new FakeAuditService();
        var (_, candidate) = SeedEventAndCandidate(context, candidateStatus: "APROBADO");
        candidate.ApprovedWithExceptions = true;
        candidate.ExceptionsDetail = "Detalle previo";
        await context.SaveChangesAsync();

        var controller = CreateController(context, audit);

        await controller.Withdraw(candidate.Id, "Renuncia voluntaria");

        Assert.Equal("Candidatura retirada exitosamente. El cambio es definitivo.", controller.TempData["Success"]);
        var updated = await context.Candidates.SingleAsync(c => c.Id == candidate.Id);
        Assert.Equal("RETIRADO", updated.Status);
        Assert.False(updated.AllowCorrection);
        Assert.False(updated.ApprovedWithExceptions);
        Assert.Null(updated.ExceptionsDetail);
        Assert.Equal("Renuncia voluntaria", updated.RejectionReason);
        Assert.Contains("CANDIDACY_WITHDRAWN", audit.Actions);
        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica que Withdraw solo se permite desde APROBADO y rechaza FINALIZADA (RN-7.2) o motivo vacío.</summary>
    [Theory]
    [InlineData("INSCRIPCION", "PENDIENTE", "Motivo válido")]
    [InlineData("INSCRIPCION", "RECHAZADO", "Motivo válido")]
    [InlineData("INSCRIPCION", "RETIRADO", "Motivo válido")]
    [InlineData("FINALIZADA", "APROBADO", "Motivo válido")]
    [InlineData("INSCRIPCION", "APROBADO", "   ")]
    public async Task Withdraw_Post_FromNonApprovedOrFinalizadaOrBlankReason_RejectsAndPreservesStatus(
        string eventStatus,
        string initialStatus,
        string reason)
    {
        using var context = CreateInMemoryContext();
        var (_, candidate) = SeedEventAndCandidate(context, eventStatus: eventStatus, candidateStatus: initialStatus);
        var controller = CreateController(context);

        await controller.Withdraw(candidate.Id, reason);

        Assert.NotNull(controller.TempData["Error"]);
        Assert.Null(controller.TempData["Success"]);
        Assert.Equal(initialStatus, (await context.Candidates.SingleAsync(c => c.Id == candidate.Id)).Status);
        Assert.Empty(context.EmailQueues);
    }

    /// <summary>Verifica que Approve, Reject y Withdraw solo redirigen a returnUrl local y bloquean redirecciones externas.</summary>
    [Theory]
    [InlineData("/AdminEvents/Edit/1", true)]
    [InlineData("https://evil.com", false)]
    [InlineData("//evil.com", false)]
    public async Task PostActions_ReturnUrlHandling_RedirectsLocalOnlyAndRejectsExternal(string returnUrl, bool expectLocalRedirect)
    {
        using var context = CreateInMemoryContext();
        var (_, candidate) = SeedEventAndCandidate(context);
        var controller = CreateController(context);

        var r1 = await controller.Approve(candidate.Id, false, null, returnUrl);
        var r2 = await controller.Reject(candidate.Id, "Motivo", false, returnUrl);
        await controller.Approve(candidate.Id, false, null); // volver a APROBADO para Withdraw
        var r3 = await controller.Withdraw(candidate.Id, "Retiro", returnUrl);

        foreach (var res in new[] { r1, r2, r3 })
        {
            if (expectLocalRedirect)
            {
                var local = Assert.IsType<RedirectResult>(res);
                Assert.Equal(returnUrl, local.Url);
            }
            else
            {
                var fallback = Assert.IsType<RedirectToActionResult>(res);
                Assert.Equal("Index", fallback.ActionName);
                Assert.Equal("AdminEvents", fallback.ControllerName);
            }
        }
    }

    /// <summary>Verifica que ServeDocument rechaza rutas vacías, no-.enc, path traversal fuera de candidacies y faltantes.</summary>
    [Fact]
    public void ServeDocument_InvalidOrTraversalOrMissingPath_ReturnsBadRequestOrNotFound()
    {
        using var context = CreateInMemoryContext();
        var controller = CreateController(context);

        // Archivo colocado deliberadamente fuera de candidacies/ para comprobar neutralización de traversal
        var outsideFile = Path.Combine(_tempUploadsBasePath, "outside.enc");
        File.WriteAllText(outsideFile, "enc-" + Convert.ToBase64String(Encoding.UTF8.GetBytes("secreto")));

        Assert.IsType<BadRequestObjectResult>(controller.ServeDocument(null!));
        Assert.IsType<BadRequestObjectResult>(controller.ServeDocument(""));
        Assert.IsType<BadRequestObjectResult>(controller.ServeDocument("   "));
        Assert.IsType<BadRequestObjectResult>(controller.ServeDocument("/uploads/candidacies/doc.pdf"));
        Assert.IsType<BadRequestObjectResult>(controller.ServeDocument("../../etc/passwd"));
        Assert.IsType<BadRequestObjectResult>(controller.ServeDocument("/uploads/candidacies/"));

        Assert.IsType<NotFoundObjectResult>(controller.ServeDocument("../outside.enc"));
        Assert.IsType<NotFoundObjectResult>(controller.ServeDocument("/uploads/candidacies/missing.pdf.enc"));
    }

    /// <summary>Verifica que ServeDocument retorna 500 cuando el contenido cifrado está corrupto o no es Base64 válido.</summary>
    [Fact]
    public void ServeDocument_CorruptedCipher_Returns500()
    {
        using var context = CreateInMemoryContext();
        var controller = CreateController(context);

        var filePath = Path.Combine(_tempUploadsBasePath, "candidacies", "corrupt.pdf.enc");
        File.WriteAllText(filePath, "enc-not-valid-base64!!!");

        var result = controller.ServeDocument("/uploads/candidacies/corrupt.pdf.enc");

        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, objectResult.StatusCode);
    }

    /// <summary>Verifica que ServeDocument descifra archivos .enc válidos y devuelve bytes exactos y Content-Type esperado.</summary>
    [Theory]
    [InlineData("soporte.pdf.enc", "application/pdf")]
    [InlineData("foto.png.enc", "image/png")]
    [InlineData("foto.jpg.enc", "image/jpeg")]
    [InlineData("foto.jpeg.enc", "image/jpeg")]
    [InlineData("nota.txt.enc", "application/octet-stream")]
    public void ServeDocument_ValidEncryptedFiles_DecryptsAndReturnsExactMimeType(string fileName, string expectedMime)
    {
        using var context = CreateInMemoryContext();
        var encryptor = new FakeEncryptionService();
        var controller = CreateController(context, encryptionService: encryptor);

        var rawBytes = Encoding.UTF8.GetBytes($"contenido-binario-{fileName}");
        var cipher = encryptor.Encrypt(Convert.ToBase64String(rawBytes));
        File.WriteAllText(Path.Combine(_tempUploadsBasePath, "candidacies", fileName), cipher);

        var result = controller.ServeDocument($"/uploads/candidacies/{fileName}");

        var fileResult = Assert.IsType<FileContentResult>(result);
        Assert.Equal(expectedMime, fileResult.ContentType);
        Assert.Equal(rawBytes, fileResult.FileContents);
    }

    /// <summary>Documenta el estado actual de los 3 métodos [HttpPost] de AdminCandidatesController sin [ValidateAntiForgeryToken] (R3).</summary>
    [Fact]
    public void AdminCandidatesController_PostActions_CurrentBehaviorLacksValidateAntiForgeryTokenAttribute()
    {
        // NOTA: R3 — Ninguna de las 3 acciones [HttpPost] de AdminCandidatesController
        // (Approve, Reject, Withdraw) declara [ValidateAntiForgeryToken].
        // Fuera de alcance de P1–P4: no se modifica producción, se documenta aquí el comportamiento actual.
        var postMethods = typeof(AdminCandidatesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null)
            .ToList();

        Assert.Equal(3, postMethods.Count);
        foreach (var method in postMethods)
        {
            Assert.Null(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }
    }
}
