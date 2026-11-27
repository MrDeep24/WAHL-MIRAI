// ============================================================
// Archivo:       AuthorizationAndResultsHttpTests.cs
// Módulo:        Autorización HTTP, Resultados y Antiforgery (M01 / M03 / M06 / M09)
// Desarrollador: Kevin
// Descripción:   Pruebas HTTP integrales sobre políticas de autorización por rol
//                (ADMIN, ELECTOR, SUPER_ADMIN), cuentas ELIMINADO, control de acceso
//                a resultados en vivo y finalizados (RN-4, RN-4.1, RN-5), validación
//                antiforgery en AdminAccountsController y documentación de brechas
//                antiforgery en controladores fuera de alcance (R3).
// Requerimientos: RN-4, RN-4.1, RN-5, RN-14, RF-M09-01.
// ============================================================
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using WahlMirai.Tests.Common;
using WahlMirai.Web.Models;

namespace WahlMirai.Tests.Http;

/// <summary>
/// Pruebas HTTP end-to-end para políticas de autorización por rol, cuentas eliminadas (RN-14),
/// acceso a resultados electorales (RN-4, RN-4.1, RN-5) y cobertura CSRF en controladores MVC.
/// </summary>
[Collection(HttpPipelineCollection.CollectionName)]
public sealed class AuthorizationAndResultsHttpTests
{
    private const string SharedPassword = "RolePass!2026";
    private const string SuperAdminDoc = "3000000001";
    private const string AdminDoc = "3000000002";
    private const string ElectorDoc = "3000000003";
    private const string DeletedElectorDoc = "3000000004";

    /// <summary>
    /// RN-14: Una cuenta en estado <c>ELIMINADO</c> no puede iniciar sesión; además, si una cuenta
    /// activa es marcada como <c>ELIMINADO</c> (o con <c>DeletedAt</c>) en mitad de la sesión,
    /// la siguiente petición HTTP revoca la cookie hacia <c>/Auth/Login?reason=disabled</c>.
    /// </summary>
    [Fact]
    public async Task EliminadoUser_LoginBlocked_AndMidSessionSoftDeleteRevokedWithDisabledReason()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 11, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory);

        await factory.SeedAsync(db =>
        {
            db.Voters.Add(new Voter
            {
                Id = 4,
                RoleId = Roles.Elector,
                GradeId = 11,
                DocumentHash = Sha256Hex(DeletedElectorDoc),
                EncryptedDocument = $"enc-{DeletedElectorDoc}",
                FullName = "Deleted Elector",
                ContactEmail = "deleted@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(SharedPassword, workFactor: 4),
                Status = "ELIMINADO",
                DeletedAt = clock.GetUtcNow().UtcDateTime,
                RegisteredAt = clock.GetUtcNow().UtcDateTime
            });
            return Task.CompletedTask;
        });

        using var deletedClient = factory.CreateNoRedirectClient(useHttps: true);
        var blockedLogin = await PostLoginRawAsync(deletedClient, DeletedElectorDoc, SharedPassword);
        Assert.Equal(HttpStatusCode.OK, blockedLogin.StatusCode);
        Assert.Contains("Documento o contraseña incorrectos, o cuenta inactiva", WebUtility.HtmlDecode(await blockedLogin.Content.ReadAsStringAsync()));

        // Usuario activo inicia sesión y luego es eliminado lógicamente.
        using var activeClient = factory.CreateNoRedirectClient(useHttps: true);
        await LoginAsync(activeClient, ElectorDoc, SharedPassword);
        Assert.Equal(HttpStatusCode.OK, (await activeClient.GetAsync("/Elector/Dashboard")).StatusCode);

        await factory.SeedAsync(async db =>
        {
            var elector = await db.Voters.SingleAsync(v => v.Id == 3);
            elector.Status = "ELIMINADO";
            elector.DeletedAt = clock.GetUtcNow().UtcDateTime;
        });

        var revokedRes = await activeClient.GetAsync("/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.Redirect, revokedRes.StatusCode);
        Assert.Equal("/Auth/Login?reason=disabled", revokedRes.Headers.Location?.OriginalString);
    }

    /// <summary>
    /// RN-5: Las rutas administrativas de eventos, candidaturas y censo permiten <c>ADMIN</c>
    /// y <c>SUPER_ADMIN</c> (200 OK / acción válida) y deniegan a <c>ELECTOR</c> (<c>403 Forbidden</c>
    /// en peticiones AJAX; redirección a <c>AccessDeniedPath</c> en navegación estándar).
    /// </summary>
    [Theory]
    [InlineData("/AdminEvents", HttpStatusCode.OK)]
    [InlineData("/AdminCensus", HttpStatusCode.OK)]
    [InlineData("/AdminCandidates/GetCandidateDetail/999999", HttpStatusCode.NotFound)]
    public async Task AdminPolicy_AllowsAdminAndSuperAdmin_DeniesElectorWith403(
        string protectedPath,
        HttpStatusCode expectedAuthorizedStatus)
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedStandardActorsAsync(factory);

        using var adminClient = factory.CreateNoRedirectClient(useHttps: true);
        using var superAdminClient = factory.CreateNoRedirectClient(useHttps: true);
        using var electorClient = factory.CreateNoRedirectClient(useHttps: true);

        await LoginAsync(adminClient, AdminDoc, SharedPassword);
        await LoginAsync(superAdminClient, SuperAdminDoc, SharedPassword);
        await LoginAsync(electorClient, ElectorDoc, SharedPassword);

        Assert.Equal(expectedAuthorizedStatus, (await adminClient.GetAsync(protectedPath)).StatusCode);
        Assert.Equal(expectedAuthorizedStatus, (await superAdminClient.GetAsync(protectedPath)).StatusCode);

        // NOTA: Program.cs configura AccessDeniedPath = "/Auth/Login" sin sobreescribir
        // OnRedirectToAccessDenied; por defecto ASP.NET Core CookieAuth responde 403 Forbidden
        // cuando recibe X-Requested-With: XMLHttpRequest y 302 hacia AccessDeniedPath en navegación HTML.
        var ajaxDenied = await SendAjaxGetAsync(electorClient, protectedPath);
        Assert.Equal(HttpStatusCode.Forbidden, ajaxDenied.StatusCode);

        var browserDenied = await electorClient.GetAsync(protectedPath);
        Assert.Equal(HttpStatusCode.Redirect, browserDenied.StatusCode);
        var deniedLocation = browserDenied.Headers.Location?.OriginalString ?? string.Empty;
        var deniedUri = deniedLocation.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? new Uri(deniedLocation)
            : new Uri(new Uri("https://localhost"), deniedLocation);
        Assert.Equal("/Auth/Login", deniedUri.AbsolutePath);
    }

    /// <summary>
    /// La política <c>ELECTOR</c> (<c>/Elector/Dashboard</c>) permite <c>ELECTOR</c> (200 OK)
    /// y deniega a <c>ADMIN</c> con <c>403 Forbidden</c> (AJAX) / redirección a login (navegación).
    /// </summary>
    [Fact]
    public async Task ElectorPolicy_AllowsElector_DeniesAdminWith403()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedStandardActorsAsync(factory);

        using var electorClient = factory.CreateNoRedirectClient(useHttps: true);
        using var adminClient = factory.CreateNoRedirectClient(useHttps: true);

        await LoginAsync(electorClient, ElectorDoc, SharedPassword);
        await LoginAsync(adminClient, AdminDoc, SharedPassword);

        var electorRes = await electorClient.GetAsync("/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.OK, electorRes.StatusCode);

        var adminAjaxDenied = await SendAjaxGetAsync(adminClient, "/Elector/Dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, adminAjaxDenied.StatusCode);
    }

    /// <summary>
    /// RF-M09-01: <c>AdminAccountsController</c> (<c>[Authorize(Roles = "SUPER_ADMIN")]</c>)
    /// permite a <c>SUPER_ADMIN</c> (200 OK) y deniega tanto a <c>ADMIN</c> como a <c>ELECTOR</c> (<c>403 Forbidden</c>).
    /// </summary>
    [Fact]
    public async Task SuperAdminPolicy_AllowsSuperAdmin_DeniesAdminAndElectorWith403()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedStandardActorsAsync(factory);

        using var superAdminClient = factory.CreateNoRedirectClient(useHttps: true);
        using var adminClient = factory.CreateNoRedirectClient(useHttps: true);
        using var electorClient = factory.CreateNoRedirectClient(useHttps: true);

        await LoginAsync(superAdminClient, SuperAdminDoc, SharedPassword);
        await LoginAsync(adminClient, AdminDoc, SharedPassword);
        await LoginAsync(electorClient, ElectorDoc, SharedPassword);

        var superRes = await superAdminClient.GetAsync("/AdminAccounts");
        Assert.Equal(HttpStatusCode.OK, superRes.StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await SendAjaxGetAsync(adminClient, "/AdminAccounts")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAjaxGetAsync(electorClient, "/AdminAccounts")).StatusCode);
    }

    /// <summary>
    /// RN-4, RN-5: Consultar resultados de una elección inexistente devuelve <c>404 NotFound</c>
    /// tanto para <c>ELECTOR</c> como para <c>ADMIN</c>.
    /// </summary>
    [Fact]
    public async Task Results_MissingEvent_Returns404NotFound()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedStandardActorsAsync(factory);

        using var electorClient = factory.CreateNoRedirectClient(useHttps: true);
        using var adminClient = factory.CreateNoRedirectClient(useHttps: true);

        await LoginAsync(electorClient, ElectorDoc, SharedPassword);
        await LoginAsync(adminClient, AdminDoc, SharedPassword);

        Assert.Equal(HttpStatusCode.NotFound, (await electorClient.GetAsync("/Results/Index/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await electorClient.GetAsync("/Results/GetLiveData/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminClient.GetAsync("/Results/Index/999999")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await adminClient.GetAsync("/Results/GetLiveData/999999")).StatusCode);
    }

    /// <summary>
    /// RN-4, RN-4.1: Verifica sobre HTTP las reglas de acceso a resultados para <c>ELECTOR</c>:
    /// denegado en <c>PROGRAMADA</c>/<c>INSCRIPCION</c>/<c>PROPUESTAS</c>/<c>ELIMINADO</c> y en
    /// <c>ACTIVA</c> sin voto previo; permitido en <c>ACTIVA</c> tras votar y en <c>FINALIZADA</c>
    /// únicamente si su grado está habilitado.
    /// </summary>
    [Fact]
    public async Task Results_ElectorAccess_EnforcesRn4AndRn41Rules()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedStandardActorsAsync(factory);

        await factory.SeedAsync(db =>
        {
            db.ElectionPositions.Add(new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = DateTime.UtcNow });

            db.VotingEvents.AddRange(
                CreateEvent(id: 10, title: "Elección Programada", status: "PROGRAMADA"),
                CreateEvent(id: 11, title: "Elección Activa Sin Voto", status: "ACTIVA"),
                CreateEvent(id: 12, title: "Elección Activa Con Voto", status: "ACTIVA"),
                CreateEvent(id: 13, title: "Elección Finalizada Otro Grado", status: "FINALIZADA"),
                CreateEvent(id: 14, title: "Elección Finalizada Grado Habilitado", status: "FINALIZADA"));

            // El elector (Id=3) pertenece al grado 11.
            db.EventGrades.AddRange(
                new EventGrade { Id = 1, VotingEventId = 11, GradeId = 11 },
                new EventGrade { Id = 2, VotingEventId = 12, GradeId = 11 },
                new EventGrade { Id = 3, VotingEventId = 13, GradeId = 10 },
                new EventGrade { Id = 4, VotingEventId = 14, GradeId = 11 });

            // Participación registrada solo para el evento 12.
            db.VoterEventParticipations.Add(new VoterEventParticipation
            {
                Id = 1,
                VoterId = 3,
                VotingEventId = 12,
                ParticipatedAt = DateTime.UtcNow
            });

            return Task.CompletedTask;
        });

        using var electorClient = factory.CreateNoRedirectClient(useHttps: true);
        await LoginAsync(electorClient, ElectorDoc, SharedPassword);

        // 1. Etapa previa a ACTIVA/FINALIZADA -> Index redirige a Dashboard, GetLiveData -> 403 Forbidden (AJAX).
        var progIndex = await electorClient.GetAsync("/Results/Index/10");
        Assert.Equal(HttpStatusCode.Redirect, progIndex.StatusCode);
        Assert.Equal("/Elector/Dashboard", progIndex.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAjaxGetAsync(electorClient, "/Results/GetLiveData/10")).StatusCode);

        // 2. ACTIVA sin voto emitido (RN-4) -> denegado.
        var activeNoVoteIndex = await electorClient.GetAsync("/Results/Index/11");
        Assert.Equal(HttpStatusCode.Redirect, activeNoVoteIndex.StatusCode);
        Assert.Equal("/Elector/Dashboard", activeNoVoteIndex.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAjaxGetAsync(electorClient, "/Results/GetLiveData/11")).StatusCode);

        // 3. ACTIVA con voto emitido (RN-4) -> 200 OK en Index y JSON válido en GetLiveData.
        var activeVotedIndex = await electorClient.GetAsync("/Results/Index/12");
        Assert.Equal(HttpStatusCode.OK, activeVotedIndex.StatusCode);
        var activeVotedLive = await electorClient.GetAsync("/Results/GetLiveData/12");
        Assert.Equal(HttpStatusCode.OK, activeVotedLive.StatusCode);
        using (var json = JsonDocument.Parse(await activeVotedLive.Content.ReadAsStringAsync()))
        {
            Assert.True(json.RootElement.GetProperty("ok").GetBoolean());
            Assert.Equal(12, json.RootElement.GetProperty("eventId").GetInt32());
        }

        // 4. FINALIZADA con grado NO habilitado (RN-4.1) -> denegado.
        var finWrongGradeIndex = await electorClient.GetAsync("/Results/Index/13");
        Assert.Equal(HttpStatusCode.Redirect, finWrongGradeIndex.StatusCode);
        Assert.Equal("/Elector/Dashboard", finWrongGradeIndex.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.Forbidden, (await SendAjaxGetAsync(electorClient, "/Results/GetLiveData/13")).StatusCode);

        // 5. FINALIZADA con grado habilitado incluso sin haber votado (RN-4.1) -> permitido (200 OK).
        var finAllowedIndex = await electorClient.GetAsync("/Results/Index/14");
        Assert.Equal(HttpStatusCode.OK, finAllowedIndex.StatusCode);
        var finAllowedLive = await electorClient.GetAsync("/Results/GetLiveData/14");
        Assert.Equal(HttpStatusCode.OK, finAllowedLive.StatusCode);
    }

    /// <summary>
    /// RN-5: <c>ADMIN</c> y <c>SUPER_ADMIN</c> tienen acceso irrestricto a <c>Results/Index</c>
    /// y <c>Results/GetLiveData</c> en cualquier etapa sin haber emitido voto ni tener grado.
    /// </summary>
    [Theory]
    [InlineData(AdminDoc)]
    [InlineData(SuperAdminDoc)]
    public async Task Results_AdminAndSuperAdmin_HaveUnrestrictedAccessAcrossStages(string adminDocument)
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedStandardActorsAsync(factory);

        await factory.SeedAsync(db =>
        {
            db.ElectionPositions.Add(new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = DateTime.UtcNow });
            db.VotingEvents.AddRange(
                CreateEvent(id: 21, title: "Elección Programada", status: "PROGRAMADA"),
                CreateEvent(id: 22, title: "Elección Activa", status: "ACTIVA"),
                CreateEvent(id: 23, title: "Elección Finalizada", status: "FINALIZADA"));
            return Task.CompletedTask;
        });

        using var client = factory.CreateNoRedirectClient(useHttps: true);
        await LoginAsync(client, adminDocument, SharedPassword);

        foreach (var eventId in new[] { 21, 22, 23 })
        {
            var indexRes = await client.GetAsync($"/Results/Index/{eventId}");
            Assert.Equal(HttpStatusCode.OK, indexRes.StatusCode);

            var liveRes = await client.GetAsync($"/Results/GetLiveData/{eventId}");
            Assert.Equal(HttpStatusCode.OK, liveRes.StatusCode);
        }
    }

    /// <summary>
    /// RF-M09-01, RN-14: <c>AdminAccountsController</c> aplica <c>[ValidateAntiForgeryToken]</c>
    /// en sus acciones POST (<c>Create</c>, <c>Edit</c>, <c>Delete</c>, <c>Restore</c>), rechazando
    /// con <c>400 BadRequest</c> cuando falta el token y ejecutando la operación cuando es válido.
    /// </summary>
    [Fact]
    public async Task AdminAccounts_PostActions_EnforceAntiforgeryToken()
    {
        await using var factory = new WahlMiraiWebFactory();
        await SeedStandardActorsAsync(factory);

        using var superAdminClient = factory.CreateNoRedirectClient(useHttps: true);
        await LoginAsync(superAdminClient, SuperAdminDoc, SharedPassword);

        // 1. Peticiones sin __RequestVerificationToken -> 400 BadRequest en todas las acciones POST.
        Assert.Equal(HttpStatusCode.BadRequest, (await superAdminClient.PostAsync("/AdminAccounts/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Document"] = "99001122",
            ["FullName"] = "Nuevo Admin",
            ["ContactEmail"] = "nuevo@example.com",
            ["RoleName"] = "ADMIN"
        }))).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await superAdminClient.PostAsync("/AdminAccounts/Edit", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Id"] = "2",
            ["Document"] = AdminDoc,
            ["FullName"] = "Admin Editado",
            ["ContactEmail"] = "admin@example.com",
            ["RoleName"] = "ADMIN"
        }))).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await superAdminClient.PostAsync("/AdminAccounts/Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = "2"
        }))).StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await superAdminClient.PostAsync("/AdminAccounts/Restore", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["id"] = "2"
        }))).StatusCode);

        // 2. Con token antiforgery válido obtenido desde GET /AdminAccounts -> Create, Delete y Restore funcionan.
        var indexGet = await superAdminClient.GetAsync("/AdminAccounts");
        Assert.Equal(HttpStatusCode.OK, indexGet.StatusCode);
        var csrfToken = ExtractAntiforgeryToken(await indexGet.Content.ReadAsStringAsync());

        var createRes = await superAdminClient.PostAsync("/AdminAccounts/Create", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = csrfToken,
            ["Document"] = "3000000099",
            ["FullName"] = "Coordinador Nuevo",
            ["ContactEmail"] = "coord@example.com",
            ["RoleName"] = "ADMIN",
            ["PositionTitle"] = "Rector(a)"
        }));
        Assert.Equal(HttpStatusCode.Redirect, createRes.StatusCode);
        Assert.Equal("/AdminAccounts", createRes.Headers.Location?.OriginalString);

        var createdExists = await factory.QueryDbAsync(db =>
            db.Voters.AnyAsync(v => v.DocumentHash == Sha256Hex("3000000099") && v.Status == "ACTIVO"));
        Assert.True(createdExists);

        var deleteRes = await superAdminClient.PostAsync("/AdminAccounts/Delete", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = csrfToken,
            ["id"] = "2"
        }));
        Assert.Equal(HttpStatusCode.Redirect, deleteRes.StatusCode);

        var adminDeleted = await factory.QueryDbAsync(db => db.Voters.SingleAsync(v => v.Id == 2));
        Assert.Equal("ELIMINADO", adminDeleted.Status);

        var restoreRes = await superAdminClient.PostAsync("/AdminAccounts/Restore", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = csrfToken,
            ["id"] = "2"
        }));
        Assert.Equal(HttpStatusCode.Redirect, restoreRes.StatusCode);

        var adminRestored = await factory.QueryDbAsync(db => db.Voters.SingleAsync(v => v.Id == 2));
        Assert.Equal("ACTIVO", adminRestored.Status);
    }

    /// <summary>
    /// Brecha documentada R3: Verifica que los endpoints POST de <c>ElectorController.ConfirmarVoto</c>,
    /// <c>AdminEventsController</c>, <c>AdminCandidatesController</c> y <c>AdminCensusController</c>
    /// actualmente aceptan solicitudes sin token antiforgery (por ausencia de <c>[ValidateAntiForgeryToken]</c>).
    /// </summary>
    [Fact]
    public async Task AntiforgeryGap_ElectorAndAdminControllers_AcceptPostsWithoutToken_DocumentedR3()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 10, 20, 14, 0, 0, TimeSpan.Zero));
        await using var factory = new WahlMiraiWebFactory(timeProvider: clock);
        await SeedStandardActorsAsync(factory);

        var localNow = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.Local).DateTime;
        await factory.SeedAsync(db =>
        {
            db.ElectionPositions.Add(new ElectionPosition { Id = 1, Name = "Personero", Status = "ACTIVO", CreatedAt = DateTime.UtcNow });

            // Evento ACTIVA determinista según FakeTimeProvider localNow
            db.VotingEvents.Add(new VotingEvent
            {
                Id = 50,
                Title = "Elección Activa CSRF Gap",
                ElectionType = "PERSONAS",
                PositionId = 1,
                Status = "ACTIVA",
                CreatedByVoterId = 2,
                RegistrationStartDate = DateOnly.FromDateTime(localNow.AddDays(-3)),
                RegistrationStartTime = new TimeOnly(8, 0),
                RegistrationEndDate = DateOnly.FromDateTime(localNow.AddDays(-2)),
                RegistrationEndTime = new TimeOnly(18, 0),
                ProposalsStartDate = DateOnly.FromDateTime(localNow.AddDays(-2)),
                ProposalsStartTime = new TimeOnly(18, 0),
                ProposalsEndDate = DateOnly.FromDateTime(localNow.AddDays(-1)),
                ProposalsEndTime = new TimeOnly(18, 0),
                VotingStartDate = DateOnly.FromDateTime(localNow.AddHours(-2)),
                VotingStartTime = TimeOnly.FromDateTime(localNow.AddHours(-2)),
                VotingEndDate = DateOnly.FromDateTime(localNow.AddHours(2)),
                VotingEndTime = TimeOnly.FromDateTime(localNow.AddHours(2))
            });

            db.EventGrades.Add(new EventGrade { Id = 10, VotingEventId = 50, GradeId = 11 });

            db.Candidates.AddRange(
                new Candidate
                {
                    Id = 100,
                    VotingEventId = 50,
                    VoterId = 3,
                    Name = "Candidato Aprobado",
                    IsBlankVote = false,
                    Status = "APROBADO",
                    EnrolledAt = DateTime.UtcNow
                },
                new Candidate
                {
                    Id = 101,
                    VotingEventId = 50,
                    VoterId = null,
                    Name = "Candidato Pendiente",
                    IsBlankVote = false,
                    Status = "PENDIENTE",
                    EnrolledAt = DateTime.UtcNow
                });

            db.Voters.Add(new Voter
            {
                Id = 5,
                RoleId = Roles.Elector,
                GradeId = 11,
                DocumentHash = Sha256Hex("3000000005"),
                EncryptedDocument = "enc-3000000005",
                FullName = "Elector Censo Borrable",
                ContactEmail = "borrable@example.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(SharedPassword, workFactor: 4),
                Status = "ACTIVO",
                RegisteredAt = DateTime.UtcNow
            });

            return Task.CompletedTask;
        });

        using var electorClient = factory.CreateNoRedirectClient(useHttps: true);
        using var adminClient = factory.CreateNoRedirectClient(useHttps: true);

        await LoginAsync(electorClient, ElectorDoc, SharedPassword);
        await LoginAsync(adminClient, AdminDoc, SharedPassword);

        // NOTA: Brecha antiforgery fuera de alcance (R3): ElectorController.ConfirmarVoto ([HttpPost])
        // no declara [ValidateAntiForgeryToken]; el POST sin token registra el voto y redirige (no retorna 400).
        var voteRes = await electorClient.PostAsync("/Elector/ConfirmarVoto", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["eventId"] = "50",
            ["candidateId"] = "100"
        }));
        Assert.Equal(HttpStatusCode.Redirect, voteRes.StatusCode);
        Assert.Equal("/Elector/Dashboard", voteRes.Headers.Location?.OriginalString);
        Assert.True(await factory.QueryDbAsync(db => db.Votes.AnyAsync(v => v.VotingEventId == 50 && v.CandidateId == 100)));

        // NOTA: Brecha antiforgery fuera de alcance (R3): AdminCandidatesController (Approve, Reject, Withdraw)
        // no declara [ValidateAntiForgeryToken]; el POST sin token aprueba la candidatura (no retorna 400).
        var approveRes = await adminClient.PostAsync("/AdminCandidates/Approve", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["candidateId"] = "101",
            ["withExceptions"] = "false"
        }));
        Assert.Equal(HttpStatusCode.Redirect, approveRes.StatusCode);
        var approvedCandidate = await factory.QueryDbAsync(db => db.Candidates.SingleAsync(c => c.Id == 101));
        Assert.Equal("APROBADO", approvedCandidate.Status);

        // NOTA: Brecha antiforgery fuera de alcance (R3): AdminCensusController (11 acciones POST incluyendo DeleteVoter)
        // no declara [ValidateAntiForgeryToken]; el POST sin token ejecuta el borrado lógico (no retorna 400).
        var deleteVoterRes = await adminClient.PostAsync("/AdminCensus/DeleteVoter/5", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.Redirect, deleteVoterRes.StatusCode);
        var deletedVoter = await factory.QueryDbAsync(db => db.Voters.SingleAsync(v => v.Id == 5));
        Assert.Equal("ELIMINADO", deletedVoter.Status);

        // NOTA: Brecha antiforgery fuera de alcance (R3): AdminEventsController (Create, Edit, Delete, AddCandidate, AddProposalOption)
        // no declara [ValidateAntiForgeryToken]; el POST sin token elimina lógicamente el evento (no retorna 400).
        var deleteEventRes = await adminClient.PostAsync("/AdminEvents/Delete/50", new FormUrlEncodedContent(new Dictionary<string, string>()));
        Assert.Equal(HttpStatusCode.Redirect, deleteEventRes.StatusCode);
        var deletedEvent = await factory.QueryDbAsync(db => db.VotingEvents.SingleAsync(e => e.Id == 50));
        Assert.Equal("ELIMINADO", deletedEvent.Status);
    }

    private static async Task SeedStandardActorsAsync(WahlMiraiWebFactory factory)
    {
        await factory.SeedAsync(db =>
        {
            db.Roles.AddRange(
                new Role { Id = Roles.Elector, Name = Roles.ElectorName },
                new Role { Id = Roles.Admin, Name = Roles.AdminName },
                new Role { Id = Roles.SuperAdmin, Name = Roles.SuperAdminName });

            db.Grades.AddRange(
                new Grade { Id = 10, Name = "10°", SequenceOrder = 5, IsLastGrade = false },
                new Grade { Id = 11, Name = "11°", SequenceOrder = 6, IsLastGrade = true });

            var hash = BCrypt.Net.BCrypt.HashPassword(SharedPassword, workFactor: 4);

            db.Voters.AddRange(
                new Voter
                {
                    Id = 1,
                    RoleId = Roles.SuperAdmin,
                    GradeId = null,
                    DocumentHash = Sha256Hex(SuperAdminDoc),
                    EncryptedDocument = $"enc-{SuperAdminDoc}",
                    FullName = "Super Admin",
                    ContactEmail = "superadmin@example.com",
                    PasswordHash = hash,
                    Status = "ACTIVO",
                    RegisteredAt = DateTime.UtcNow
                },
                new Voter
                {
                    Id = 2,
                    RoleId = Roles.Admin,
                    GradeId = null,
                    DocumentHash = Sha256Hex(AdminDoc),
                    EncryptedDocument = $"enc-{AdminDoc}",
                    FullName = "Admin Electoral",
                    ContactEmail = "admin@example.com",
                    PasswordHash = hash,
                    Status = "ACTIVO",
                    RegisteredAt = DateTime.UtcNow
                },
                new Voter
                {
                    Id = 3,
                    RoleId = Roles.Elector,
                    GradeId = 11,
                    DocumentHash = Sha256Hex(ElectorDoc),
                    EncryptedDocument = $"enc-{ElectorDoc}",
                    FullName = "Elector Activo",
                    ContactEmail = "elector@example.com",
                    PasswordHash = hash,
                    Status = "ACTIVO",
                    RegisteredAt = DateTime.UtcNow
                });

            return Task.CompletedTask;
        });
    }

    private static VotingEvent CreateEvent(uint id, string title, string status)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new VotingEvent
        {
            Id = id,
            Title = title,
            ElectionType = "PERSONAS",
            PositionId = 1,
            Status = status,
            CreatedByVoterId = 2,
            RegistrationStartDate = today.AddDays(-5),
            RegistrationStartTime = new TimeOnly(8, 0),
            RegistrationEndDate = today.AddDays(-4),
            RegistrationEndTime = new TimeOnly(18, 0),
            ProposalsStartDate = today.AddDays(-4),
            ProposalsStartTime = new TimeOnly(18, 0),
            ProposalsEndDate = today.AddDays(-2),
            ProposalsEndTime = new TimeOnly(18, 0),
            VotingStartDate = today.AddDays(-1),
            VotingStartTime = new TimeOnly(8, 0),
            VotingEndDate = today.AddDays(1),
            VotingEndTime = new TimeOnly(18, 0)
        };
    }

    private static async Task LoginAsync(HttpClient client, string document, string password)
    {
        var res = await PostLoginRawAsync(client, document, password);
        Assert.Equal(HttpStatusCode.Redirect, res.StatusCode);
    }

    private static async Task<HttpResponseMessage> PostLoginRawAsync(HttpClient client, string document, string password)
    {
        var getLogin = await client.GetAsync("/Auth/Login");
        getLogin.EnsureSuccessStatusCode();
        var csrfToken = ExtractAntiforgeryToken(await getLogin.Content.ReadAsStringAsync());

        return await client.PostAsync("/Auth/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = csrfToken,
            ["Document"] = document,
            ["Password"] = password
        }));
    }

    private static async Task<HttpResponseMessage> SendAjaxGetAsync(HttpClient client, string path)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, path);
        req.Headers.Add("X-Requested-With", "XMLHttpRequest");
        return await client.SendAsync(req);
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        var match = Regex.Match(
            html,
            @"<input[^>]*name=""__RequestVerificationToken""[^>]*value=""([^""]+)""|<input[^>]*value=""([^""]+)""[^>]*name=""__RequestVerificationToken""",
            RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            throw new InvalidOperationException("No se encontró __RequestVerificationToken en la respuesta HTML.");
        }

        return match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
    }

    private static string Sha256Hex(string input)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(input))).ToLowerInvariant();
    }
}
