# Graph Report - WAHL-MIRAI  (2026-09-22)

## Corpus Check
- 176 files · ~1,034,325 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 34 file(s) not represented in the graph (top: .resx 14, (none) 7, .mermaid 5)

## Summary
- 1522 nodes · 2844 edges · 180 communities (79 shown, 101 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 315 edges (avg confidence: 0.84)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `44db971f`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- VotingEvent
- Voter
- AdminAccountService
- VotingEvent
- Voter
- FakeAuditService
- AdminCensusController
- .Completar
- Candidate
- EmailReportViewModel
- .LogAsync
- WahlMiraiDbContext
- Controller
- VotingService
- EmailSettings
- CandidateReviewDetailDto
- ICredentialService
- ActiveSessionStore
- ProfileService
- VoterDetailDto
- ayuda-chatbot.js
- .CreateService
- .OnModelCreating
- http
- WhitelistDetailDto
- pqr-manage.js
- AdminCandidatesController
- FakeEmailSender
- PagedResult
- .Apply
- AdminAccountsController
- MyPostulationDto
- WahlMirai.Web
- WahlMirai.Tests
- EmailQueue
- VwPendingCandidacy
- CandidacyService
- PromotionPreview
- PromotionResult
- ProfileController
- ForcePasswordChangeMiddleware
- ElectionPosition
- VwActiveCensu
- VwPendingWhitelist
- CensusWhitelist
- PostulationFormDto
- PostulationSubmitDto
- PendingPasswordStore
- PositionRequirement
- VwPendingEmailQueue
- VwVoteCount
- CandidateReviewService
- CsvImportResult
- locale.js
- check-i18n-coverage.ps1
- CandidacyDocument
- RequirementCheckDto
- ICandidateReviewService
- .GetMyPostulationsAsync
- IAuditService
- theme.js
- PQR Management
- Role
- WhitelistStatsDto
- Active Session Store
- Electoral Census
- Electoral Ballot
- Census Whitelist
- Authentication
- Administrator Profile Management
- Thematic Proposals
- Real-Time Results Dashboard
- Graphify Knowledge Graph
- Full Graphify Pipeline
- Development Environment
- Administrator Voting Workflow
- Censo electoral persistente
- Eliminación lógica de procesos electorales
- Arquitectura ASP.NET Core y SPA
- Correo de contacto compartido
- Arquitectura ASP.NET Core y SPA v2.6
- Apertura de resultados al finalizar
- Arquitectura ASP.NET Core y SPA v2.7
- PQR del elector
- Arquitectura ASP.NET MVC con Razor Views
- Autoregistro mediante lista blanca
- Census whitelist
- Arquitectura ASP.NET MVC con Razor Views
- Etapas electorales y jerarquía administrativa
- Requisitos Wahl Mirai v2.9
- dynamic
- Graphify Repository Instructions
- List<WahlMirai.Web.Models.VotingEvent>
- List<WahlMirai.Web.Models.VwVoteCount>
- List<WahlMirai.Web.Services.AdminAccountListItem>
- List<WahlMirai.Web.Services.CandidateReviewDetailDto>
- Autenticación por documento y contraseña
- Administrator Dashboard
- Dashboard del administrador
- Anonymous Voting
- Dashboard del elector
- wahl_mirai_db_v2_9_completo.sql
- Modal para agregar opción o proyecto
- Enabled Voting Grades
- Archived Inactive Process
- Electoral Process Management
- Confirmation-Driven Design
- Three-Click Voting Path
- Encrypted Anonymous Voting
- Error Page
- Recuperación mediante correo vinculado
- Authentication Flow
- Census Document Editing
- Atomic Census Operations
- WahlMirai.Web.Controllers.EmailReportViewModel
- WahlMirai.Web.Services.MyPostulationDto
- WahlMirai.Web.Services.PostulationFormDto
- WahlMirai.Web.Services.PromotionPreview
- WahlMirai.Web.ViewModels.PagedResult<WahlMirai.Web.Services.VoterDetailDto>
- WahlMirai.Web.ViewModels.PagedResult<WahlMirai.Web.Services.WhitelistDetailDto>
- _AddCandidateModal.cshtml
- _AddProposalOptionModal.cshtml
- _ConfigurePersonas.cshtml
- _ConfigureTemas.cshtml
- Form.cshtml
- CambiarClave.cshtml
- Completar.cshtml
- Login.cshtml
- Registro.cshtml
- Error/Index.cshtml
- Profile/Index.cshtml
- Recuperar.cshtml
- Identidad visual electoral
- Logo Wahl Mirai
- Project Proposal
- Campaign Proposals
- Wahl Mirai Design System
- Wahl Mirai Design System
- Real-Time Results Visualization
- Navegación no encontrada
- Pantalla de error 404
- Light Institutional Interface
- Secure Account Recovery
- Wahl Mirai Design System
- Institutional Integrity
- Electoral Voting Platform
- Contextual Layouts
- Localization Audit
- MIT License
- MIT License
- MIT License
- FakeAuthService
- AdminAccountListItem
- .Recuperar
- Vote
- AcademicYear
- DocumentEncryptionService

## God Nodes (most connected - your core abstractions)
1. `WahlMiraiDbContext` - 79 edges
2. `WahlMirai.Web.Models` - 58 edges
3. `WahlMirai.Web.Services` - 44 edges
4. `VotingEvent` - 39 edges
5. `Voter` - 33 edges
6. `CandidateReviewDetailDto` - 31 edges
7. `Candidate` - 29 edges
8. `CensusWhitelist` - 27 edges
9. `VotingEvent` - 26 edges
10. `CensusService` - 25 edges

## Surprising Connections (you probably didn't know these)
- `Active Session Store` --semantically_similar_to--> `Active Session Invalidation`  [INFERRED] [semantically similar]
  docs/Arquitectura_y_Diseno_v2_9.md → REGISTRO_CAMBIOS_kevin.md
- `Atomic Self-Registration` --semantically_similar_to--> `Census Whitelist Self-Registration`  [INFERRED] [semantically similar]
  docs/MODULO_CENSO_WHITELIST_v2.8.md → README.md
- `FakeAuthService` --implements--> `IAuthService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IAuthService.cs
- `FakeEncryptionService` --implements--> `IDocumentEncryptionService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IDocumentEncryptionService.cs
- `FakeAuditService` --implements--> `IAuditService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IAuditService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Wahl Mirai Confirmation-Oriented Design Language** — mockups_m08_ayuda_pqr_wahl_mirai_design_confirmation_driven_design, mockups_pagina_inicial_de_bienvenida_design_design_system, mockups_wahl_mirai_design_design_system [EXTRACTED 1.00]
- **Authentication and Recovery Flow** — mockups_acceso_y_seguridad_code_authentication, mockups_acceso_y_seguridad_code_forced_password_change, mockups_recuperacon_de_contrase_a_code_password_recovery [EXTRACTED 1.00]
- **Census Self-Registration and Security Architecture** — readme_census_whitelist_self_registration, docs_modulo_censo_whitelist_v2_8_atomic_self_registration, docs_arquitectura_y_diseno_v2_9_active_session_store [INFERRED 0.75]
- **Electoral Ballot, Confirmation, and Results Flow** — mockups_tarjet_n_de_votaci_n_code_electoral_ballot, mockups_ventana_de_propuestas_y_confirmaci_n_de_voto_code_vote_confirmation, mockups_resultados_en_tiempo_real_code_real_time_results_dashboard [INFERRED 0.85]
- **Election Configuration Flow** — mockups_m03_m04_procesos_candidatos_configuraci_n_r_pida_de_proceso_admin_code_process_configuration, mockups_m03_m04_procesos_candidatos_gesti_n_de_elecciones_y_candidatos_admin_code_election_candidate_management, mockups_m03_m04_procesos_candidatos_gesti_n_de_procesos_electorales_admin_code_electoral_process_management [INFERRED 0.85]
- **PQR Support Flow** — mockups_m08_ayuda_pqr_centro_de_ayuda_votante_m08_code_pqr_submission, mockups_m08_ayuda_pqr_gesti_n_administrativa_de_pqr_m08_code_pqr_management, mockups_m08_ayuda_pqr_responder_pqr_administrador_m08_code_pqr_response [INFERRED 0.85]
- **Evolución arquitectónica de Wahl Mirai** — docs_documentos_antiguos_2_3_ers_wahl_mirai_v2_3_sistema_votaciones_digitales, docs_documentos_antiguos_2_8_ers_wahl_mirai_v2_8_requisitos_v28, docs_ers_wahl_mirai_v2_9_requisitos_v29 [INFERRED 0.85]

## Communities (180 total, 101 thin omitted)

### Community 0 - "WahlMirai.Web.Models"
Cohesion: 0.06
Nodes (37): WahlMirai.Web.Models, WahlMirai.Web.Services, WahlMirai.Web.Hubs, WahlMirai.Web.Controllers, WahlMirai.Web.ViewModels, WahlMirai.Tests, mailkit_net_smtp, mailkit_security (+29 more)

### Community 1 - "VotingEvent"
Cohesion: 0.09
Nodes (26): Exception, IEnumerable, HttpGet, HttpPost, IActionResult, List, Task, AdminEventsController (+18 more)

### Community 2 - "Voter"
Cohesion: 0.07
Nodes (33): Fact, Task, AuthServiceTests, DateTime, AuditLog, Action, Details, FieldName (+25 more)

### Community 3 - "AdminAccountService"
Cohesion: 0.23
Nodes (6): CancellationToken, List, Task, AdminAccountService, IAdminAccountService, IDocumentEncryptionService

### Community 4 - "VotingEvent"
Cohesion: 0.06
Nodes (33): DateOnly, TimeOnly, DateTime, ICollection, VotingEvent, Candidates, CreatedAt, CreatedByVoter (+25 more)

### Community 5 - "Voter"
Cohesion: 0.04
Nodes (47): DateTime, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt, RespondedBy (+39 more)

### Community 6 - "FakeAuditService"
Cohesion: 0.35
Nodes (7): Fact, InvalidOperationException, Task, CensusWhitelistTests, List, FakeAuditService, Actions

### Community 7 - "AdminCensusController"
Cohesion: 0.26
Nodes (6): HttpGet, HttpPost, IActionResult, IFormFile, Task, AdminCensusController

### Community 8 - ".Completar"
Cohesion: 0.05
Nodes (50): system_componentmodel_dataannotations, AllowAnonymous, Authorize, HttpGet, HttpPost, IActionResult, Task, ValidateAntiForgeryToken (+42 more)

### Community 9 - "Candidate"
Cohesion: 0.08
Nodes (24): DateTime, ICollection, Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt (+16 more)

### Community 10 - "EmailReportViewModel"
Cohesion: 0.08
Nodes (29): DateTime, HttpPost, IActionResult, List, Task, AdminEmailReportController, EmailQueueItemDto, Attempts (+21 more)

### Community 11 - ".LogAsync"
Cohesion: 0.14
Nodes (5): IActiveSessionStore, List, Task, CensusService, ICensusService

### Community 12 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (26): DbContext, DbContextOptionsBuilder, DbSet, WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals (+18 more)

### Community 13 - "Controller"
Cohesion: 0.09
Nodes (20): Controller, IExceptionHandlerPathFeature, IStatusCodeReExecuteFeature, IActionResult, ILogger, Route, ErrorController, IActionResult (+12 more)

### Community 14 - "VotingService"
Cohesion: 0.11
Nodes (17): Hub, IHubContext, HttpPost, IActionResult, Task, ElectorController, HttpGet, IActionResult (+9 more)

### Community 15 - "EmailSettings"
Cohesion: 0.10
Nodes (19): BackgroundService, HashSet, IServiceScopeFactory, EmailSettings, RateLimitPerMinute, SenderDisplayName, SenderEmail, SenderPassword (+11 more)

### Community 16 - "CandidateReviewDetailDto"
Cohesion: 0.09
Nodes (23): CandidateReviewDetailDto, AllowCorrection, ApprovedWithExceptions, CandidateEmail, CandidateId, CandidateName, EnrolledAt, EventId (+15 more)

### Community 17 - "ICredentialService"
Cohesion: 0.18
Nodes (10): CancellationToken, Task, EmailType, CAMBIO_PERFIL, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA, REASIGNACION_ADMIN, RECUPERACION_ACCESO (+2 more)

### Community 18 - "ActiveSessionStore"
Cohesion: 0.17
Nodes (14): InlineData, Theory, Fact, ActiveSessionStoreTests, ConcurrentDictionary, IsValid, ActiveSessionStore, SessionEntry (+6 more)

### Community 19 - "ProfileService"
Cohesion: 0.14
Nodes (15): Success, CancellationToken, Task, IEmailSender, EmailSaved, NotificationSent, Task, IProfileService (+7 more)

### Community 20 - "VoterDetailDto"
Cohesion: 0.14
Nodes (14): VoterDetailDto, ContactEmail, DeletedAt, Document, ExcluirDePromocion, FullName, GradeId, GradeName (+6 more)

### Community 21 - "ayuda-chatbot.js"
Cohesion: 0.33
Nodes (16): addMessage(), bindEscalationButton(), bindEvents(), bindResolutionButtons(), disableResolutionBlock(), escalateToPqr(), escapeHtml(), findTopicByKeyword() (+8 more)

### Community 22 - ".CreateService"
Cohesion: 0.32
Nodes (6): ArgumentException, Fact, InvalidOperationException, Task, AdminAccountServiceTests, FakeEncryptionService

### Community 23 - ".OnModelCreating"
Cohesion: 0.14
Nodes (14): ModelBuilder, CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id, Grade (+6 more)

### Community 24 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 25 - "WhitelistDetailDto"
Cohesion: 0.15
Nodes (13): DateTime, WhitelistDetailDto, ClaimedAt, ClaimedByUserId, CreatedAt, Document, DocumentHash, ExcluirDePromocion (+5 more)

### Community 26 - "pqr-manage.js"
Cohesion: 0.28
Nodes (13): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), filteredTickets(), formatDate(), getCsrfToken(), initials() (+5 more)

### Community 27 - "AdminCandidatesController"
Cohesion: 0.31
Nodes (7): HttpGet, HttpPost, IActionResult, IConfiguration, Task, AdminCandidatesController, StatusCode

### Community 28 - "FakeEmailSender"
Cohesion: 0.26
Nodes (10): CancellationToken, EmailType, Fact, List, Task, FakeCredentialService, FakeEmailSender, Messages (+2 more)

### Community 29 - "PagedResult"
Cohesion: 0.13
Nodes (11): List, PagedResult, EndItemIndex, HasNextPage, HasPreviousPage, Items, PageNumber, PageSize (+3 more)

### Community 30 - ".Apply"
Cohesion: 0.26
Nodes (9): IWebHostEnvironment, HttpGet, HttpPost, IActionResult, IFormFile, List, Task, ValidateAntiForgeryToken (+1 more)

### Community 31 - "AdminAccountsController"
Cohesion: 0.18
Nodes (16): CancellationToken, HttpGet, HttpPost, IActionResult, ILogger, Task, ValidateAntiForgeryToken, AdminAccountsController (+8 more)

### Community 32 - "MyPostulationDto"
Cohesion: 0.17
Nodes (12): MyPostulationDto, AllowCorrection, ApprovedWithExceptions, CandidateId, EnrolledAt, EventId, EventTitle, ExceptionsDetail (+4 more)

### Community 33 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 34 - "WahlMirai.Tests"
Cohesion: 0.18
Nodes (10): coverlet.collector (6.0.4), Microsoft.EntityFrameworkCore.InMemory (9.0.7), Microsoft.EntityFrameworkCore.Relational (9.0.7), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, WahlMirai.Tests (+2 more)

### Community 35 - "EmailQueue"
Cohesion: 0.18
Nodes (11): DateTime, EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt (+3 more)

### Community 36 - "VwPendingCandidacy"
Cohesion: 0.18
Nodes (11): DateTime, VwPendingCandidacy, CandidateId, CandidateName, EnrolledAt, EventTitle, MandatoryDocumentsUploaded, MandatoryRequirements (+3 more)

### Community 37 - "CandidacyService"
Cohesion: 0.22
Nodes (8): IConfiguration, IFormFile, List, Task, CandidacyService, PostulationResult, CandidateId, ErrorMessage

### Community 38 - "PromotionPreview"
Cohesion: 0.11
Nodes (19): DateTime, List, PromotionPreview, ActiveVotersCount, CurrentYear, EligibleCount, ExcludedCount, HasRunThisYear (+11 more)

### Community 39 - "PromotionResult"
Cohesion: 0.18
Nodes (11): Task, IPromotionService, PromotionResult, ActivePromotedCount, ErrorCount, GraduatedCount, PromotedCount, RetainedCount (+3 more)

### Community 40 - "ProfileController"
Cohesion: 0.30
Nodes (9): HttpGet, HttpPost, IActionResult, Task, ValidateAntiForgeryToken, ChangePasswordRequest, ProfileController, UpdateEmailRequest (+1 more)

### Community 41 - "ForcePasswordChangeMiddleware"
Cohesion: 0.22
Nodes (7): WahlMirai.Web.Middleware, HttpContext, IApplicationBuilder, RequestDelegate, Task, ForcePasswordChangeMiddleware, ForcePasswordChangeMiddlewareExtensions

### Community 42 - "ElectionPosition"
Cohesion: 0.20
Nodes (10): DateTime, ICollection, ElectionPosition, CreatedAt, Description, Id, Name, PositionRequirements (+2 more)

### Community 43 - "VwActiveCensu"
Cohesion: 0.20
Nodes (10): DateTime, VwActiveCensu, ContactEmail, ExcluirDePromocion, FullName, Grade, Id, RegisteredAt (+2 more)

### Community 44 - "VwPendingWhitelist"
Cohesion: 0.20
Nodes (10): DateTime, VwPendingWhitelist, CreatedAt, DocumentHash, EncryptedDocument, ExcluirDePromocion, FullName, Grade (+2 more)

### Community 45 - "CensusWhitelist"
Cohesion: 0.14
Nodes (14): DateTime, CensusWhitelist, ClaimedAt, ClaimedByUserId, ClaimedByVoter, CreatedAt, DocumentHash, EncryptedDocument (+6 more)

### Community 46 - "PostulationFormDto"
Cohesion: 0.14
Nodes (14): DateTime, PostulableEventDto, Description, Id, PositionName, RegistrationEndDateTime, Title, PostulationFormDto (+6 more)

### Community 47 - "PostulationSubmitDto"
Cohesion: 0.22
Nodes (9): Dictionary, IFormFile, PostulationSubmitDto, Documents, EventId, GovernmentPlan, Photo, Proposals (+1 more)

### Community 48 - "PendingPasswordStore"
Cohesion: 0.25
Nodes (4): system_collections_concurrent, ConcurrentDictionary, IPendingPasswordStore, PendingPasswordStore

### Community 49 - "PositionRequirement"
Cohesion: 0.22
Nodes (9): ICollection, PositionRequirement, CandidacyDocuments, Description, DisplayOrder, Id, IsMandatory, Position (+1 more)

### Community 50 - "VwPendingEmailQueue"
Cohesion: 0.22
Nodes (9): DateTime, VwPendingEmailQueue, Attempts, ContactEmail, CreatedAt, EmailType, FullName, Id (+1 more)

### Community 51 - "VwVoteCount"
Cohesion: 0.22
Nodes (9): VwVoteCount, CandidateId, CandidateName, EventId, EventStatus, EventTitle, IsBlankVote, TotalVotes (+1 more)

### Community 52 - "CandidateReviewService"
Cohesion: 0.42
Nodes (3): List, Task, CandidateReviewService

### Community 53 - "CsvImportResult"
Cohesion: 0.21
Nodes (10): Stream, CsvImportResult, DuplicateCount, ErrorCount, Errors, InsertedCount, ProcessedCount, CsvRowError (+2 more)

### Community 54 - "locale.js"
Cohesion: 0.42
Nodes (8): applyLocalizedThemeLabels(), getCultureFromCookie(), initSelects(), setCultureCookie(), setupMutationObserver(), syncHtmlLang(), translateDOM(), getTranslation()

### Community 55 - "check-i18n-coverage.ps1"
Cohesion: 0.32
Nodes (3): Add-ExtractedString(), Get-FilterCategory(), Test-UserFacingString()

### Community 56 - "CandidacyDocument"
Cohesion: 0.25
Nodes (8): DateTime, CandidacyDocument, CandidateId, FileUrl, Id, Requirement, RequirementId, UploadedAt

### Community 57 - "RequirementCheckDto"
Cohesion: 0.25
Nodes (8): DateTime, RequirementCheckDto, Description, FileUrl, IsMandatory, IsUploaded, RequirementId, UploadedAt

### Community 58 - "ICandidateReviewService"
Cohesion: 0.39
Nodes (3): List, Task, ICandidateReviewService

### Community 59 - ".GetMyPostulationsAsync"
Cohesion: 0.53
Nodes (3): List, Task, ICandidacyService

### Community 60 - "IAuditService"
Cohesion: 0.28
Nodes (6): RandomNumberGenerator, CancellationToken, EmailType, Task, CredentialService, IAuditService

### Community 61 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 62 - "PQR Management"
Cohesion: 0.33
Nodes (6): Help Center, PQR Submission, PQR Management, PQR Status Workflow, Automatic PQR Resolution, PQR Response

### Community 63 - "Role"
Cohesion: 0.33
Nodes (6): ICollection, Role, Description, Id, Name, Voters

### Community 64 - "WhitelistStatsDto"
Cohesion: 0.50
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 65 - "Active Session Store"
Cohesion: 0.40
Nodes (5): Active Session Store, ASP.NET Core MVC Architecture, MySQL Electoral Data Model, Active Session Invalidation, Contextual Navigation

### Community 66 - "Electoral Census"
Cohesion: 0.40
Nodes (5): Electoral Census, Logical Deletion, Candidate Registration, Candidate Proposals, Election and Candidate Management

### Community 67 - "Electoral Ballot"
Cohesion: 0.40
Nodes (5): Confirmation-Driven Design, Electoral Ballot, Proposal Confirmation Modal, Candidate Proposal Review, Vote Confirmation

### Community 69 - "Census Whitelist"
Cohesion: 0.67
Nodes (4): Annual Grade Promotion, Atomic Self-Registration, Census Whitelist, Census Whitelist Self-Registration

### Community 70 - "Authentication"
Cohesion: 0.50
Nodes (4): Authentication, Forced Password Change, Password Recovery, Recovery Success State

### Community 71 - "Administrator Profile Management"
Cohesion: 0.50
Nodes (4): Administrative Account Settings, Administrator Profile Management, Elector Profile Management, Password Security Settings

### Community 72 - "Thematic Proposals"
Cohesion: 0.67
Nodes (3): Thematic Option, Thematic Process Management, Thematic Proposals

### Community 73 - "Real-Time Results Dashboard"
Cohesion: 0.67
Nodes (3): Real-Time Results Dashboard, Vote Distribution Chart, Real-Time Scrutiny

### Community 104 - "wahl_mirai_db_v2_9_completo.sql"
Cohesion: 0.23
Nodes (22): `academic_years`, `audit_log`, `candidacy_documents`, `candidate_proposals`, `candidates`, `census_whitelist`, `election_positions`, `email_queue` (+14 more)

### Community 184 - "AdminAccountListItem"
Cohesion: 0.18
Nodes (11): DateTime, AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id, PositionTitle (+3 more)

### Community 185 - ".Recuperar"
Cohesion: 0.22
Nodes (7): HttpGet, HttpPost, IActionResult, Task, RecuperacionAccesoController, RecuperarAccesoViewModel, Documento

### Community 186 - "Vote"
Cohesion: 0.22
Nodes (9): DateTime, Vote, Candidate, CandidateId, Id, VotedAt, VoteHash, VotingEvent (+1 more)

### Community 189 - "AcademicYear"
Cohesion: 0.29
Nodes (7): DateTime, AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year

### Community 191 - "DocumentEncryptionService"
Cohesion: 0.40
Nodes (3): IDataProtector, ILogger, DocumentEncryptionService

## Knowledge Gaps
- **599 isolated node(s):** `Actions`, `ShouldFail`, `Messages`, `net9.0`, `coverlet.collector (6.0.4)` (+594 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 791 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **101 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `VotingEvent`, `Voter`, `AdminAccountService`, `Voter`, `FakeAuditService`, `AdminCensusController`, `.Completar`, `EmailReportViewModel`, `.LogAsync`, `VotingService`, `EmailSettings`, `ProfileService`, `.CreateService`, `.OnModelCreating`, `AdminCandidatesController`, `FakeEmailSender`, `EmailQueue`, `VwPendingCandidacy`, `CandidacyService`, `PromotionResult`, `ProfileController`, `ElectionPosition`, `VwActiveCensu`, `VwPendingWhitelist`, `CensusWhitelist`, `PositionRequirement`, `VwPendingEmailQueue`, `VwVoteCount`, `CandidateReviewService`, `CandidacyDocument`, `.Recuperar`, `Vote`, `IAuditService`, `AcademicYear`, `Role`?**
  _High betweenness centrality (0.169) - this node is a cross-community bridge._
- **Why does `WahlMirai.Web.Models` connect `WahlMirai.Web.Models` to `EmailSettings`?**
  _High betweenness centrality (0.125) - this node is a cross-community bridge._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Models` to `AdminAccountService`, `PendingPasswordStore`, `ICredentialService`, `ActiveSessionStore`, `ProfileService`?**
  _High betweenness centrality (0.067) - this node is a cross-community bridge._
- **What connects `Actions`, `ShouldFail`, `Messages` to the rest of the system?**
  _599 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `WahlMirai.Web.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.05825242718446602 - nodes in this community are weakly interconnected._
- **Should `VotingEvent` be split into smaller, more focused modules?**
  _Cohesion score 0.09074410163339383 - nodes in this community are weakly interconnected._
- **Should `Voter` be split into smaller, more focused modules?**
  _Cohesion score 0.07205387205387205 - nodes in this community are weakly interconnected._