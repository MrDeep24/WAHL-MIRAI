# Graph Report - WAHL-MIRAI  (2026-09-23)

## Corpus Check
- 158 files · ~459,620 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 28 file(s) not represented in the graph (top: .resx 14, (none) 7, .css 4)

## Summary
- 1458 nodes · 2826 edges · 131 communities (69 shown, 62 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 305 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `106a7cfa`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- VotingEvent
- .CreateCensusService
- FakeAuditService
- VotingEvent
- Voter
- PqrController
- 4. Requerimientos Específicos por Módulo
- CensusWhitelist
- Candidate
- EmailReportViewModel
- .LogAsync
- WahlMiraiDbContext
- IAuditService
- VotingService
- EmailSettings
- CandidateReviewDetailDto
- AuditLog
- ActiveSessionStore
- ProfileController
- VoterDetailDto
- ayuda-chatbot.js
- .CreateService
- .OnModelCreating
- http
- WhitelistDetailDto
- pqr-manage.js
- Voter
- Grade
- PagedResult
- .Apply
- AdminAccountService
- MyPostulationDto
- WahlMirai.Web
- WahlMirai.Tests
- EmailQueue
- VwPendingCandidacy
- CandidacyService
- PostulationFormDto
- AdminCensusController
- EmailQueueItemDto
- ForcePasswordChangeMiddleware
- ElectionPosition
- VwActiveCensu
- VwPendingWhitelist
- Vote
- PostulableEventDto
- PostulationSubmitDto
- PendingPasswordStore
- PositionRequirement
- VwPendingEmailQueue
- VwVoteCount
- ErrorController
- CsvImportResult
- locale.js
- check-i18n-coverage.ps1
- CandidacyDocument
- CredentialService
- DESIGN.md
- .GetMyPostulationsAsync
- theme.js
- Controller
- Role
- WhitelistStatsDto
- ASP.NET Core MVC Architecture
- AdminEmailReportController
- ErrorViewModel
- Census Whitelist
- HomeController
- Real-Time Scrutiny
- Graphify Knowledge Graph
- Full Graphify Pipeline
- Development Environment
- Administrator Voting Workflow
- Requisitos Wahl Mirai v2.9
- dynamic
- Graphify Repository Instructions
- List<WahlMirai.Web.Models.VotingEvent>
- List<WahlMirai.Web.Models.VwVoteCount>
- List<WahlMirai.Web.Services.AdminAccountListItem>
- List<WahlMirai.Web.Services.CandidateReviewDetailDto>
- wahl_mirai_db_v2_9_completo.sql
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
- Electoral Voting Platform
- Contextual Layouts
- Localization Audit
- MIT License
- MIT License
- MIT License
- FakeAuthService
- IDocumentEncryptionService

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
- `Atomic Self-Registration` --semantically_similar_to--> `Census Whitelist Self-Registration`  [INFERRED] [semantically similar]
  docs/MODULO_CENSO_WHITELIST_v2.8.md → README.md
- `FakeAuthService` --implements--> `IAuthService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IAuthService.cs
- `FakeEncryptionService` --implements--> `IDocumentEncryptionService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IDocumentEncryptionService.cs
- `FakeAuditService` --implements--> `IAuditService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IAuditService.cs
- `FakeEmailSender` --implements--> `IEmailSender`  [EXTRACTED]
  WahlMirai.Tests/ProfileServiceTests.cs → WahlMirai.Web/Services/IEmailSender.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Census Self-Registration and Security Architecture** — readme_census_whitelist_self_registration, docs_modulo_censo_whitelist_v2_8_atomic_self_registration, docs_arquitectura_y_diseno_v2_9_active_session_store [INFERRED 0.75]

## Communities (131 total, 62 thin omitted)

### Community 0 - "WahlMirai.Web.Models"
Cohesion: 0.06
Nodes (37): WahlMirai.Web.Models, WahlMirai.Web.Services, WahlMirai.Web.Hubs, WahlMirai.Web.Controllers, WahlMirai.Web.ViewModels, WahlMirai.Tests, mailkit_net_smtp, mailkit_security (+29 more)

### Community 1 - "VotingEvent"
Cohesion: 0.09
Nodes (26): Exception, IEnumerable, HttpGet, HttpPost, IActionResult, List, Task, AdminEventsController (+18 more)

### Community 2 - ".CreateCensusService"
Cohesion: 0.06
Nodes (43): Fact, InvalidOperationException, Task, CensusWhitelistTests, FakeEncryptionService, DateTime, AcademicYear, CreatedAt (+35 more)

### Community 3 - "FakeAuditService"
Cohesion: 0.22
Nodes (9): Fact, Task, AuthServiceTests, List, FakeAuditService, Actions, Task, AuthService (+1 more)

### Community 4 - "VotingEvent"
Cohesion: 0.06
Nodes (33): DateOnly, TimeOnly, DateTime, ICollection, VotingEvent, Candidates, CreatedAt, CreatedByVoter (+25 more)

### Community 5 - "Voter"
Cohesion: 0.04
Nodes (47): DateTime, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt, RespondedBy (+39 more)

### Community 6 - "PqrController"
Cohesion: 0.19
Nodes (14): AllowAnonymous, Authorize, HttpGet, HttpPost, IActionResult, Route, Task, ValidateAntiForgeryToken (+6 more)

### Community 7 - "4. Requerimientos Específicos por Módulo"
Cohesion: 0.07
Nodes (28): 1.1 Propósito, 1.2 Alcance del Sistema, 1.3 Definiciones, Acrónimos y Abreviaturas, 1.4 Referencias, 1. Introducción, 2.1 Perspectiva del Producto, 2.2 Funciones Principales del Sistema, 2. Descripción General del Sistema (+20 more)

### Community 8 - "CensusWhitelist"
Cohesion: 0.14
Nodes (14): DateTime, CensusWhitelist, ClaimedAt, ClaimedByUserId, ClaimedByVoter, CreatedAt, DocumentHash, EncryptedDocument (+6 more)

### Community 9 - "Candidate"
Cohesion: 0.08
Nodes (24): DateTime, ICollection, Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt (+16 more)

### Community 10 - "EmailReportViewModel"
Cohesion: 0.15
Nodes (13): List, EmailReportViewModel, EmailTypeFilter, EndDate, FailedEmails, Items, PendingEmails, Search (+5 more)

### Community 11 - ".LogAsync"
Cohesion: 0.17
Nodes (4): List, Task, CensusService, ICensusService

### Community 12 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (26): DbContext, DbContextOptionsBuilder, DbSet, WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals (+18 more)

### Community 13 - "IAuditService"
Cohesion: 0.07
Nodes (32): CancellationToken, EmailType, Fact, List, Task, FakeCredentialService, FakeEmailSender, Messages (+24 more)

### Community 14 - "VotingService"
Cohesion: 0.11
Nodes (17): Hub, IHubContext, HttpPost, IActionResult, Task, ElectorController, HttpGet, IActionResult (+9 more)

### Community 15 - "EmailSettings"
Cohesion: 0.10
Nodes (19): BackgroundService, HashSet, IServiceScopeFactory, EmailSettings, RateLimitPerMinute, SenderDisplayName, SenderEmail, SenderPassword (+11 more)

### Community 16 - "CandidateReviewDetailDto"
Cohesion: 0.05
Nodes (44): HttpGet, HttpPost, IActionResult, IConfiguration, Task, AdminCandidatesController, List, Task (+36 more)

### Community 17 - "AuditLog"
Cohesion: 0.15
Nodes (13): DateTime, AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue (+5 more)

### Community 18 - "ActiveSessionStore"
Cohesion: 0.05
Nodes (51): InlineData, system_componentmodel_dataannotations, Theory, Fact, ActiveSessionStoreTests, AllowAnonymous, Authorize, HttpGet (+43 more)

### Community 19 - "ProfileController"
Cohesion: 0.11
Nodes (22): HttpGet, HttpPost, IActionResult, Task, ValidateAntiForgeryToken, ChangePasswordRequest, ProfileController, UpdateEmailRequest (+14 more)

### Community 20 - "VoterDetailDto"
Cohesion: 0.12
Nodes (14): VoterDetailDto, ContactEmail, DeletedAt, Document, ExcluirDePromocion, FullName, GradeId, GradeName (+6 more)

### Community 21 - "ayuda-chatbot.js"
Cohesion: 0.33
Nodes (16): addMessage(), bindEscalationButton(), bindEvents(), bindResolutionButtons(), disableResolutionBlock(), escalateToPqr(), escapeHtml(), findTopicByKeyword() (+8 more)

### Community 22 - ".CreateService"
Cohesion: 0.44
Nodes (5): ArgumentException, Fact, InvalidOperationException, Task, AdminAccountServiceTests

### Community 23 - ".OnModelCreating"
Cohesion: 0.14
Nodes (14): ModelBuilder, CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id, Grade (+6 more)

### Community 24 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 25 - "WhitelistDetailDto"
Cohesion: 0.14
Nodes (13): DateTime, WhitelistDetailDto, ClaimedAt, ClaimedByUserId, CreatedAt, Document, DocumentHash, ExcluirDePromocion (+5 more)

### Community 26 - "pqr-manage.js"
Cohesion: 0.28
Nodes (13): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), filteredTickets(), formatDate(), getCsrfToken(), initials() (+5 more)

### Community 27 - "Voter"
Cohesion: 0.46
Nodes (4): Voter, Task, IWhitelistService, WhitelistService

### Community 28 - "Grade"
Cohesion: 0.25
Nodes (8): ICollection, Grade, EventGrades, Id, IsLastGrade, Name, SequenceOrder, Voters

### Community 29 - "PagedResult"
Cohesion: 0.15
Nodes (11): List, PagedResult, EndItemIndex, HasNextPage, HasPreviousPage, Items, PageNumber, PageSize (+3 more)

### Community 30 - ".Apply"
Cohesion: 0.26
Nodes (9): IWebHostEnvironment, HttpGet, HttpPost, IActionResult, IFormFile, List, Task, ValidateAntiForgeryToken (+1 more)

### Community 31 - "AdminAccountService"
Cohesion: 0.08
Nodes (32): CancellationToken, HttpGet, HttpPost, IActionResult, ILogger, Task, ValidateAntiForgeryToken, AdminAccountsController (+24 more)

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
Cohesion: 0.24
Nodes (8): IConfiguration, IFormFile, List, Task, CandidacyService, ICandidacyService, PostulationResult, CandidateId

### Community 38 - "PostulationFormDto"
Cohesion: 0.25
Nodes (7): PostulationFormDto, EventId, EventTitle, PositionDescription, PositionName, RegistrationEndDateTime, Requirements

### Community 39 - "AdminCensusController"
Cohesion: 0.26
Nodes (6): HttpGet, HttpPost, IActionResult, IFormFile, Task, AdminCensusController

### Community 40 - "EmailQueueItemDto"
Cohesion: 0.18
Nodes (11): EmailQueueItemDto, Attempts, CreatedAt, EmailType, ErrorMessage, Id, RecipientEmail, SentAt (+3 more)

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

### Community 45 - "Vote"
Cohesion: 0.22
Nodes (9): DateTime, Vote, Candidate, CandidateId, Id, VotedAt, VoteHash, VotingEvent (+1 more)

### Community 46 - "PostulableEventDto"
Cohesion: 0.29
Nodes (7): DateTime, PostulableEventDto, Description, Id, PositionName, RegistrationEndDateTime, Title

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

### Community 52 - "ErrorController"
Cohesion: 0.32
Nodes (6): IExceptionHandlerPathFeature, IStatusCodeReExecuteFeature, IActionResult, ILogger, Route, ErrorController

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

### Community 57 - "CredentialService"
Cohesion: 0.32
Nodes (5): RandomNumberGenerator, CancellationToken, EmailType, Task, CredentialService

### Community 58 - "DESIGN.md"
Cohesion: 0.22
Nodes (8): Brand & Style, Colors, Components, Elevation & Depth, Graphify (knowledge graph para agentes de IA), Layout & Spacing, Shapes, Typography

### Community 61 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 62 - "Controller"
Cohesion: 0.29
Nodes (6): Controller, HttpGet, IActionResult, IConfiguration, Route, MediaController

### Community 63 - "Role"
Cohesion: 0.33
Nodes (6): ICollection, Role, Description, Id, Name, Voters

### Community 64 - "WhitelistStatsDto"
Cohesion: 0.40
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 65 - "ASP.NET Core MVC Architecture"
Cohesion: 0.67
Nodes (3): Active Session Store, ASP.NET Core MVC Architecture, MySQL Electoral Data Model

### Community 66 - "AdminEmailReportController"
Cohesion: 0.38
Nodes (5): DateTime, HttpPost, IActionResult, Task, AdminEmailReportController

### Community 67 - "ErrorViewModel"
Cohesion: 0.29
Nodes (6): ErrorViewModel, ExceptionMessage, Message, ShowHelpButton, ShowHomeButton, Title

### Community 69 - "Census Whitelist"
Cohesion: 0.67
Nodes (4): Annual Grade Promotion, Atomic Self-Registration, Census Whitelist, Census Whitelist Self-Registration

### Community 104 - "wahl_mirai_db_v2_9_completo.sql"
Cohesion: 0.23
Nodes (22): `academic_years`, `audit_log`, `candidacy_documents`, `candidate_proposals`, `candidates`, `census_whitelist`, `election_positions`, `email_queue` (+14 more)

### Community 191 - "IDocumentEncryptionService"
Cohesion: 0.25
Nodes (4): IDataProtector, ILogger, DocumentEncryptionService, IDocumentEncryptionService

## Knowledge Gaps
- **551 isolated node(s):** `Actions`, `ShouldFail`, `Messages`, `net9.0`, `coverlet.collector (6.0.4)` (+546 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 733 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **62 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `VotingEvent`, `.CreateCensusService`, `FakeAuditService`, `Voter`, `PqrController`, `CensusWhitelist`, `.LogAsync`, `IAuditService`, `VotingService`, `EmailSettings`, `CandidateReviewDetailDto`, `AuditLog`, `ProfileController`, `.CreateService`, `.OnModelCreating`, `Voter`, `AdminAccountService`, `EmailQueue`, `VwPendingCandidacy`, `CandidacyService`, `AdminCensusController`, `ElectionPosition`, `VwActiveCensu`, `VwPendingWhitelist`, `Vote`, `PositionRequirement`, `VwPendingEmailQueue`, `VwVoteCount`, `CandidacyDocument`, `CredentialService`, `Role`, `AdminEmailReportController`?**
  _High betweenness centrality (0.204) - this node is a cross-community bridge._
- **Why does `WahlMirai.Web.Models` connect `WahlMirai.Web.Models` to `IAuditService`, `EmailSettings`?**
  _High betweenness centrality (0.132) - this node is a cross-community bridge._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Models` to `IAuditService`, `PendingPasswordStore`, `ActiveSessionStore`, `ProfileController`, `IDocumentEncryptionService`?**
  _High betweenness centrality (0.064) - this node is a cross-community bridge._
- **What connects `Actions`, `ShouldFail`, `Messages` to the rest of the system?**
  _551 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `WahlMirai.Web.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.05901766647252961 - nodes in this community are weakly interconnected._
- **Should `VotingEvent` be split into smaller, more focused modules?**
  _Cohesion score 0.09074410163339383 - nodes in this community are weakly interconnected._
- **Should `.CreateCensusService` be split into smaller, more focused modules?**
  _Cohesion score 0.06120218579234973 - nodes in this community are weakly interconnected._