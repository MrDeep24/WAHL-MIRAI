# Graph Report - WAHL-MIRAI  (2026-09-22)

## Corpus Check
- 154 files · ~455,002 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 28 file(s) not represented in the graph (top: .resx 14, (none) 7, .css 4)

## Summary
- 1418 nodes · 2789 edges · 122 communities (63 shown, 59 thin omitted)
- Extraction: 89% EXTRACTED · 11% INFERRED · 0% AMBIGUOUS · INFERRED: 305 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `71aec8a4`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- VotingEvent
- CensusWhitelist
- .LogAsync
- VotingEvent
- Voter
- PqrController
- AdminCensusController
- ProfileViewModel
- Candidate
- EmailReportViewModel
- Task
- WahlMiraiDbContext
- Controller
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
- AdminAccountsController
- MyPostulationDto
- WahlMirai.Web
- WahlMirai.Tests
- EmailQueue
- VwPendingCandidacy
- CandidacyService
- PostulationFormDto
- ForcePasswordChangeMiddleware
- ElectionPosition
- VwActiveCensu
- VwPendingWhitelist
- PostulableEventDto
- PostulationSubmitDto
- PendingPasswordStore
- PositionRequirement
- VwPendingEmailQueue
- VwVoteCount
- CsvImportResult
- locale.js
- check-i18n-coverage.ps1
- CandidacyDocument
- .GetMyPostulationsAsync
- CredentialService
- theme.js
- Role
- WhitelistStatsDto
- ASP.NET Core MVC Architecture
- Census Whitelist
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
- AdminAccountListItem
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
- `Atomic Self-Registration` --semantically_similar_to--> `Census Whitelist Self-Registration`  [INFERRED] [semantically similar]
  docs/MODULO_CENSO_WHITELIST_v2.8.md → README.md
- `FakeAuthService` --implements--> `IAuthService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IAuthService.cs
- `FakeEncryptionService` --implements--> `IDocumentEncryptionService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IDocumentEncryptionService.cs
- `FakeCredentialService` --implements--> `ICredentialService`  [EXTRACTED]
  WahlMirai.Tests/ProfileServiceTests.cs → WahlMirai.Web/Services/ICredentialService.cs
- `FakeAuditService` --implements--> `IAuditService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IAuditService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Census Self-Registration and Security Architecture** — readme_census_whitelist_self_registration, docs_modulo_censo_whitelist_v2_8_atomic_self_registration, docs_arquitectura_y_diseno_v2_9_active_session_store [INFERRED 0.75]

## Communities (122 total, 59 thin omitted)

### Community 0 - "WahlMirai.Web.Models"
Cohesion: 0.06
Nodes (36): WahlMirai.Web.Models, WahlMirai.Web.Services, WahlMirai.Web.Hubs, WahlMirai.Web.Controllers, WahlMirai.Web.ViewModels, WahlMirai.Tests, mailkit_net_smtp, mailkit_security (+28 more)

### Community 1 - "VotingEvent"
Cohesion: 0.09
Nodes (26): Exception, IEnumerable, HttpGet, HttpPost, IActionResult, List, Task, AdminEventsController (+18 more)

### Community 2 - "CensusWhitelist"
Cohesion: 0.07
Nodes (38): Fact, Task, AuthServiceTests, Fact, InvalidOperationException, Task, CensusWhitelistTests, List (+30 more)

### Community 3 - ".LogAsync"
Cohesion: 0.21
Nodes (6): CancellationToken, List, Task, AdminAccountService, IAdminAccountService, IDocumentEncryptionService

### Community 4 - "VotingEvent"
Cohesion: 0.05
Nodes (41): DateOnly, TimeOnly, DateTime, VoterEventParticipation, Id, ParticipatedAt, Voter, VoterId (+33 more)

### Community 5 - "Voter"
Cohesion: 0.05
Nodes (39): DateTime, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt, RespondedBy (+31 more)

### Community 6 - "PqrController"
Cohesion: 0.19
Nodes (14): AllowAnonymous, Authorize, HttpGet, HttpPost, IActionResult, Route, Task, ValidateAntiForgeryToken (+6 more)

### Community 7 - "AdminCensusController"
Cohesion: 0.07
Nodes (37): system_text_json, HttpGet, HttpPost, IActionResult, IFormFile, Task, AdminCensusController, DateTime (+29 more)

### Community 8 - "ProfileViewModel"
Cohesion: 0.05
Nodes (39): system_componentmodel_dataannotations, HttpGet, HttpPost, IActionResult, Task, RecuperacionAccesoController, CancellationToken, Task (+31 more)

### Community 9 - "Candidate"
Cohesion: 0.06
Nodes (33): DateTime, ICollection, Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt (+25 more)

### Community 10 - "EmailReportViewModel"
Cohesion: 0.08
Nodes (29): DateTime, HttpPost, IActionResult, List, Task, AdminEmailReportController, EmailQueueItemDto, Attempts (+21 more)

### Community 11 - "Task"
Cohesion: 0.18
Nodes (4): List, Task, CensusService, ICensusService

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
Cohesion: 0.05
Nodes (44): HttpGet, HttpPost, IActionResult, IConfiguration, Task, AdminCandidatesController, List, Task (+36 more)

### Community 17 - "AuditLog"
Cohesion: 0.15
Nodes (13): DateTime, AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue (+5 more)

### Community 18 - "ActiveSessionStore"
Cohesion: 0.09
Nodes (29): InlineData, Theory, Fact, ActiveSessionStoreTests, AllowAnonymous, Authorize, HttpGet, HttpPost (+21 more)

### Community 19 - "ProfileController"
Cohesion: 0.07
Nodes (35): CancellationToken, EmailType, Fact, List, Task, FakeCredentialService, FakeEmailSender, Messages (+27 more)

### Community 20 - "VoterDetailDto"
Cohesion: 0.14
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
Cohesion: 0.15
Nodes (13): DateTime, WhitelistDetailDto, ClaimedAt, ClaimedByUserId, CreatedAt, Document, DocumentHash, ExcluirDePromocion (+5 more)

### Community 26 - "pqr-manage.js"
Cohesion: 0.28
Nodes (13): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), filteredTickets(), formatDate(), getCsrfToken(), initials() (+5 more)

### Community 27 - "Voter"
Cohesion: 0.42
Nodes (4): Voter, Task, IWhitelistService, WhitelistService

### Community 28 - "Grade"
Cohesion: 0.25
Nodes (8): ICollection, Grade, EventGrades, Id, IsLastGrade, Name, SequenceOrder, Voters

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
Cohesion: 0.31
Nodes (5): IConfiguration, IFormFile, List, Task, CandidacyService

### Community 38 - "PostulationFormDto"
Cohesion: 0.29
Nodes (7): PostulationFormDto, EventId, EventTitle, PositionDescription, PositionName, RegistrationEndDateTime, Requirements

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

### Community 46 - "PostulableEventDto"
Cohesion: 0.20
Nodes (9): DateTime, PostulableEventDto, Description, Id, PositionName, RegistrationEndDateTime, Title, PostulationResult (+1 more)

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

### Community 59 - ".GetMyPostulationsAsync"
Cohesion: 0.48
Nodes (3): List, Task, ICandidacyService

### Community 60 - "CredentialService"
Cohesion: 0.32
Nodes (5): RandomNumberGenerator, CancellationToken, EmailType, Task, CredentialService

### Community 61 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 63 - "Role"
Cohesion: 0.33
Nodes (6): ICollection, Role, Description, Id, Name, Voters

### Community 64 - "WhitelistStatsDto"
Cohesion: 0.40
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 65 - "ASP.NET Core MVC Architecture"
Cohesion: 0.67
Nodes (3): Active Session Store, ASP.NET Core MVC Architecture, MySQL Electoral Data Model

### Community 69 - "Census Whitelist"
Cohesion: 0.67
Nodes (4): Annual Grade Promotion, Atomic Self-Registration, Census Whitelist, Census Whitelist Self-Registration

### Community 104 - "wahl_mirai_db_v2_9_completo.sql"
Cohesion: 0.23
Nodes (22): `academic_years`, `audit_log`, `candidacy_documents`, `candidate_proposals`, `candidates`, `census_whitelist`, `election_positions`, `email_queue` (+14 more)

### Community 184 - "AdminAccountListItem"
Cohesion: 0.18
Nodes (11): DateTime, AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id, PositionTitle (+3 more)

### Community 191 - "DocumentEncryptionService"
Cohesion: 0.40
Nodes (3): IDataProtector, ILogger, DocumentEncryptionService

## Knowledge Gaps
- **524 isolated node(s):** `Actions`, `ShouldFail`, `Messages`, `net9.0`, `coverlet.collector (6.0.4)` (+519 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 704 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **59 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `VotingEvent`, `CensusWhitelist`, `.LogAsync`, `VotingEvent`, `Voter`, `PqrController`, `AdminCensusController`, `ProfileViewModel`, `Candidate`, `EmailReportViewModel`, `Task`, `VotingService`, `EmailSettings`, `CandidateReviewDetailDto`, `AuditLog`, `ProfileController`, `.CreateService`, `.OnModelCreating`, `Voter`, `EmailQueue`, `VwPendingCandidacy`, `CandidacyService`, `ElectionPosition`, `VwActiveCensu`, `VwPendingWhitelist`, `PositionRequirement`, `VwPendingEmailQueue`, `VwVoteCount`, `CandidacyDocument`, `CredentialService`, `Role`?**
  _High betweenness centrality (0.224) - this node is a cross-community bridge._
- **Why does `WahlMirai.Web.Models` connect `WahlMirai.Web.Models` to `CensusWhitelist`, `AdminCensusController`, `PostulableEventDto`, `EmailSettings`, `Voter`?**
  _High betweenness centrality (0.138) - this node is a cross-community bridge._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Models` to `CensusWhitelist`, `.LogAsync`, `AdminCensusController`, `ProfileViewModel`, `PostulableEventDto`, `PendingPasswordStore`, `ActiveSessionStore`, `ProfileController`, `Voter`?**
  _High betweenness centrality (0.063) - this node is a cross-community bridge._
- **What connects `Actions`, `ShouldFail`, `Messages` to the rest of the system?**
  _524 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `WahlMirai.Web.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.062066063538817585 - nodes in this community are weakly interconnected._
- **Should `VotingEvent` be split into smaller, more focused modules?**
  _Cohesion score 0.09074410163339383 - nodes in this community are weakly interconnected._
- **Should `CensusWhitelist` be split into smaller, more focused modules?**
  _Cohesion score 0.06921529175050302 - nodes in this community are weakly interconnected._