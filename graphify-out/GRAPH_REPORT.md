# Graph Report - WAHL-MIRAI  (2026-09-29)

## Corpus Check
- 164 files · ~470,748 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 28 file(s) not represented in the graph (top: .resx 14, (none) 8, .css 3)

## Summary
- 1501 nodes · 3049 edges · 139 communities (77 shown, 62 thin omitted)
- Extraction: 87% EXTRACTED · 13% INFERRED · 0% AMBIGUOUS · INFERRED: 406 edges (avg confidence: 0.83)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `305cc1da`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- .CreateCensusService
- .UpdateContactEmailAsync
- CandidateReviewDetailDto
- AdminEventsController
- CensusWhitelist
- .CreateService
- Candidate
- VotingService
- PqrTicket
- PromotionResult
- EmailReportViewModel
- VotingEvent
- Task
- IDocumentEncryptionService
- WahlMiraiDbContext
- .Login
- Voter
- AdminCensusController
- EmailType
- LoginAttemptStore
- EventService
- ErrorViewModel
- WahlMirai.Tests
- VwPendingEmailQueue
- .LogAsync
- ayuda-chatbot.js
- http
- Grade
- pqr-manage.js
- PromotionPreview
- VoterDetailDto
- .Apply
- EmailQueueItemDto
- PositionRequirement
- WhitelistDetailDto
- MyPostulationDto
- PagedResult
- FakeAuditService
- EmailQueue
- VwPendingCandidacy
- CandidacyService
- ForcePasswordChangeMiddleware
- ElectionPosition
- VwActiveCensu
- VwPendingWhitelist
- DESIGN.md
- PostulationSubmitDto
- Vote
- VwVoteCount
- locale.js
- VoterEventParticipation
- check-i18n-coverage.ps1
- PostulableEventDto
- .GetMyPostulationsAsync
- PostulationFormDto
- theme.js
- WhitelistStatsDto
- CandidacyDocument
- ICandidacyService.cs
- Census Whitelist
- ASP.NET Core MVC Architecture
- Graphify Knowledge Graph
- Full Graphify Pipeline
- Development Environment
- Administrator Voting Workflow
- Requisitos Wahl Mirai v2.9
- dynamic
- Graphify Repository Instructions
- List<WahlMirai.Web.Models.VotingEvent>
- List<WahlMirai.Web.Models.VwVoteCount>
- List<WahlMirai.Web.Services.CandidateReviewDetailDto>
- WahlMirai.Web.Controllers.EmailReportViewModel
- WahlMirai.Web.Services.MyPostulationDto
- WahlMirai.Web.Services.PostulationFormDto
- WahlMirai.Web.Services.PromotionPreview
- WahlMirai.Web.ViewModels.AdminAccountsIndexViewModel
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
- Real-Time Scrutiny
- AdminCandidatesController
- WahlMirai.Web
- IEventService
- CandidateProposal
- Contextual Layouts
- Localization Audit
- Controller
- CandidateReviewService
- MIT License
- MIT License
- MIT License
- RequirementCheckDto
- ICandidateReviewService
- Memory Maintenance
- .OnModelCreating
- AdminEmailReportController
- VoterSearchResultDto
- RequirementInputDto
- IAuditService
- wahlmirai_web_models
- AcademicYear
- VoterPromotionDetail

## God Nodes (most connected - your core abstractions)
1. `WahlMiraiDbContext` - 79 edges
2. `VotingEvent` - 59 edges
3. `WahlMirai.Web.Models` - 58 edges
4. `WahlMirai.Web.Services` - 48 edges
5. `Voter` - 45 edges
6. `Candidate` - 39 edges
7. `FakeAuditService` - 34 edges
8. `CandidateReviewDetailDto` - 31 edges
9. `CensusWhitelist` - 28 edges
10. `LoginAttemptStore` - 26 edges

## Surprising Connections (you probably didn't know these)
- `Atomic Self-Registration` --semantically_similar_to--> `Census Whitelist Self-Registration`  [INFERRED] [semantically similar]
  docs/MODULO_CENSO_WHITELIST_v2.8.md → README.md
- `FakeAuthService` --implements--> `IAuthService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IAuthService.cs
- `FakeEncryptionService` --implements--> `IDocumentEncryptionService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IDocumentEncryptionService.cs
- `FakeAuditService` --implements--> `IAuditService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IAuditService.cs
- `FakeLoginAttemptStore` --implements--> `ILoginAttemptStore`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/ILoginAttemptStore.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Census Self-Registration and Security Architecture** — readme_census_whitelist_self_registration, docs_modulo_censo_whitelist_v2_8_atomic_self_registration, docs_arquitectura_y_diseno_v2_9_active_session_store [INFERRED 0.75]

## Communities (139 total, 62 thin omitted)

### Community 0 - "WahlMirai.Web.Models"
Cohesion: 0.06
Nodes (41): WahlMirai.Web.Models, WahlMirai.Web.Services, WahlMirai.Web.Hubs, WahlMirai.Web.Controllers, WahlMirai.Web.ViewModels, WahlMirai.Tests, WahlMirai.Tests.Common, mailkit_net_smtp (+33 more)

### Community 1 - ".CreateCensusService"
Cohesion: 0.48
Nodes (4): Fact, InvalidOperationException, Task, CensusWhitelistTests

### Community 2 - ".UpdateContactEmailAsync"
Cohesion: 0.07
Nodes (32): HttpGet, HttpPost, IActionResult, Task, ValidateAntiForgeryToken, ChangePasswordRequest, ProfileController, UpdateEmailRequest (+24 more)

### Community 3 - "CandidateReviewDetailDto"
Cohesion: 0.09
Nodes (23): CandidateReviewDetailDto, AllowCorrection, ApprovedWithExceptions, CandidateEmail, CandidateId, CandidateName, EnrolledAt, EventId (+15 more)

### Community 4 - "AdminEventsController"
Cohesion: 0.31
Nodes (7): Exception, HttpGet, HttpPost, IActionResult, List, Task, AdminEventsController

### Community 5 - "CensusWhitelist"
Cohesion: 0.13
Nodes (15): DateTime, CensusWhitelist, ClaimedAt, ClaimedByUserId, ClaimedByVoter, CreatedAt, DocumentHash, EncryptedDocument (+7 more)

### Community 6 - ".CreateService"
Cohesion: 0.14
Nodes (13): ArgumentException, RandomNumberGenerator, Fact, InvalidOperationException, Task, AdminAccountServiceTests, FakeEncryptionService, CancellationToken (+5 more)

### Community 7 - "Candidate"
Cohesion: 0.08
Nodes (25): DateTime, ICollection, Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt (+17 more)

### Community 8 - "VotingService"
Cohesion: 0.11
Nodes (17): Hub, IHubContext, HttpPost, IActionResult, Task, ElectorController, HttpGet, IActionResult (+9 more)

### Community 9 - "PqrTicket"
Cohesion: 0.06
Nodes (40): system_componentmodel_dataannotations, AllowAnonymous, Authorize, HttpGet, HttpPost, IActionResult, Route, Task (+32 more)

### Community 10 - "PromotionResult"
Cohesion: 0.17
Nodes (12): Task, IPromotionService, PromotionResult, ActivePromotedCount, ErrorCount, GraduatedCount, Message, PromotedCount (+4 more)

### Community 11 - "EmailReportViewModel"
Cohesion: 0.15
Nodes (13): List, EmailReportViewModel, EmailTypeFilter, EndDate, FailedEmails, Items, PendingEmails, Search (+5 more)

### Community 12 - "VotingEvent"
Cohesion: 0.06
Nodes (33): DateOnly, TimeOnly, DateTime, ICollection, VotingEvent, Candidates, CreatedAt, CreatedByVoter (+25 more)

### Community 13 - "Task"
Cohesion: 0.19
Nodes (4): List, Task, CensusService, ICensusService

### Community 14 - "IDocumentEncryptionService"
Cohesion: 0.15
Nodes (9): IDataProtector, HttpGet, IActionResult, IConfiguration, Route, MediaController, ILogger, DocumentEncryptionService (+1 more)

### Community 15 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (26): DbContext, DbContextOptionsBuilder, DbSet, WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals (+18 more)

### Community 16 - ".Login"
Cohesion: 0.06
Nodes (43): InlineData, Theory, Fact, ActiveSessionStoreTests, AllowAnonymous, Authorize, EnableRateLimiting, HttpGet (+35 more)

### Community 17 - "Voter"
Cohesion: 0.05
Nodes (43): DateTime, AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue (+35 more)

### Community 18 - "AdminCensusController"
Cohesion: 0.26
Nodes (6): HttpGet, HttpPost, IActionResult, IFormFile, Task, AdminCensusController

### Community 19 - "EmailType"
Cohesion: 0.05
Nodes (38): BackgroundService, HashSet, IMemoryCache, IServiceScopeFactory, EnableRateLimiting, HttpGet, HttpPost, IActionResult (+30 more)

### Community 20 - "LoginAttemptStore"
Cohesion: 0.14
Nodes (20): system_collections_concurrent, TimeProvider, Fact, Task, AuthServiceTests, DateTimeOffset, TimeSpan, FakeTimeProvider (+12 more)

### Community 21 - "EventService"
Cohesion: 0.24
Nodes (5): IEnumerable, DateTime, List, Task, EventService

### Community 22 - "ErrorViewModel"
Cohesion: 0.18
Nodes (10): IExceptionHandlerPathFeature, IStatusCodeReExecuteFeature, IActionResult, Route, ErrorViewModel, ExceptionMessage, Message, ShowHelpButton (+2 more)

### Community 23 - "WahlMirai.Tests"
Cohesion: 0.18
Nodes (10): coverlet.collector (6.0.4), Microsoft.EntityFrameworkCore.InMemory (9.0.7), Microsoft.EntityFrameworkCore.Relational (9.0.7), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, WahlMirai.Tests (+2 more)

### Community 24 - "VwPendingEmailQueue"
Cohesion: 0.22
Nodes (9): DateTime, VwPendingEmailQueue, Attempts, ContactEmail, CreatedAt, EmailType, FullName, Id (+1 more)

### Community 25 - ".LogAsync"
Cohesion: 0.05
Nodes (50): IReadOnlyList, Stream, CancellationToken, HttpGet, HttpPost, IActionResult, ILogger, Task (+42 more)

### Community 26 - "ayuda-chatbot.js"
Cohesion: 0.33
Nodes (16): addMessage(), bindEscalationButton(), bindEvents(), bindResolutionButtons(), disableResolutionBlock(), escalateToPqr(), escapeHtml(), findTopicByKeyword() (+8 more)

### Community 27 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 28 - "Grade"
Cohesion: 0.14
Nodes (14): EventGrade, Grade, GradeId, Id, VotingEvent, VotingEventId, ICollection, Grade (+6 more)

### Community 29 - "pqr-manage.js"
Cohesion: 0.28
Nodes (13): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), filteredTickets(), formatDate(), getCsrfToken(), initials() (+5 more)

### Community 30 - "PromotionPreview"
Cohesion: 0.17
Nodes (12): DateTime, List, PromotionPreview, ActiveVotersCount, CurrentYear, EligibleCount, ExcludedCount, HasRunThisYear (+4 more)

### Community 31 - "VoterDetailDto"
Cohesion: 0.13
Nodes (14): VoterDetailDto, ContactEmail, DeletedAt, Document, ExcluirDePromocion, FullName, GradeId, GradeName (+6 more)

### Community 32 - ".Apply"
Cohesion: 0.26
Nodes (9): IWebHostEnvironment, HttpGet, HttpPost, IActionResult, IFormFile, List, Task, ValidateAntiForgeryToken (+1 more)

### Community 33 - "EmailQueueItemDto"
Cohesion: 0.18
Nodes (11): EmailQueueItemDto, Attempts, CreatedAt, EmailType, ErrorMessage, Id, RecipientEmail, SentAt (+3 more)

### Community 34 - "PositionRequirement"
Cohesion: 0.22
Nodes (9): ICollection, PositionRequirement, CandidacyDocuments, Description, DisplayOrder, Id, IsMandatory, Position (+1 more)

### Community 35 - "WhitelistDetailDto"
Cohesion: 0.14
Nodes (13): DateTime, WhitelistDetailDto, ClaimedAt, ClaimedByUserId, CreatedAt, Document, DocumentHash, ExcluirDePromocion (+5 more)

### Community 36 - "MyPostulationDto"
Cohesion: 0.17
Nodes (12): MyPostulationDto, AllowCorrection, ApprovedWithExceptions, CandidateId, EnrolledAt, EventId, EventTitle, ExceptionsDetail (+4 more)

### Community 37 - "PagedResult"
Cohesion: 0.15
Nodes (11): List, PagedResult, EndItemIndex, HasNextPage, HasPreviousPage, Items, PageNumber, PageSize (+3 more)

### Community 38 - "FakeAuditService"
Cohesion: 0.14
Nodes (19): List, Task, TimeSpan, FakeAuditService, Actions, IpAddresses, FakeAuthService, FakeLoginAttemptStore (+11 more)

### Community 39 - "EmailQueue"
Cohesion: 0.18
Nodes (11): DateTime, EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt (+3 more)

### Community 40 - "VwPendingCandidacy"
Cohesion: 0.18
Nodes (11): DateTime, VwPendingCandidacy, CandidateId, CandidateName, EnrolledAt, EventTitle, MandatoryDocumentsUploaded, MandatoryRequirements (+3 more)

### Community 41 - "CandidacyService"
Cohesion: 0.31
Nodes (5): IConfiguration, IFormFile, List, Task, CandidacyService

### Community 42 - "ForcePasswordChangeMiddleware"
Cohesion: 0.22
Nodes (7): WahlMirai.Web.Middleware, HttpContext, IApplicationBuilder, RequestDelegate, Task, ForcePasswordChangeMiddleware, ForcePasswordChangeMiddlewareExtensions

### Community 43 - "ElectionPosition"
Cohesion: 0.20
Nodes (10): DateTime, ICollection, ElectionPosition, CreatedAt, Description, Id, Name, PositionRequirements (+2 more)

### Community 44 - "VwActiveCensu"
Cohesion: 0.20
Nodes (10): DateTime, VwActiveCensu, ContactEmail, ExcluirDePromocion, FullName, Grade, Id, RegisteredAt (+2 more)

### Community 45 - "VwPendingWhitelist"
Cohesion: 0.20
Nodes (10): DateTime, VwPendingWhitelist, CreatedAt, DocumentHash, EncryptedDocument, ExcluirDePromocion, FullName, Grade (+2 more)

### Community 46 - "DESIGN.md"
Cohesion: 0.22
Nodes (8): Brand & Style, Colors, Components, Elevation & Depth, Graphify (knowledge graph para agentes de IA), Layout & Spacing, Shapes, Typography

### Community 47 - "PostulationSubmitDto"
Cohesion: 0.22
Nodes (9): Dictionary, IFormFile, PostulationSubmitDto, Documents, EventId, GovernmentPlan, Photo, Proposals (+1 more)

### Community 48 - "Vote"
Cohesion: 0.22
Nodes (9): DateTime, Vote, Candidate, CandidateId, Id, VotedAt, VoteHash, VotingEvent (+1 more)

### Community 49 - "VwVoteCount"
Cohesion: 0.22
Nodes (9): VwVoteCount, CandidateId, CandidateName, EventId, EventStatus, EventTitle, IsBlankVote, TotalVotes (+1 more)

### Community 50 - "locale.js"
Cohesion: 0.42
Nodes (8): applyLocalizedThemeLabels(), getCultureFromCookie(), initSelects(), setCultureCookie(), setupMutationObserver(), syncHtmlLang(), translateDOM(), getTranslation()

### Community 51 - "VoterEventParticipation"
Cohesion: 0.25
Nodes (8): DateTime, VoterEventParticipation, Id, ParticipatedAt, Voter, VoterId, VotingEvent, VotingEventId

### Community 52 - "check-i18n-coverage.ps1"
Cohesion: 0.38
Nodes (3): Add-ExtractedString(), Get-FilterCategory(), Test-UserFacingString()

### Community 53 - "PostulableEventDto"
Cohesion: 0.29
Nodes (7): DateTime, PostulableEventDto, Description, Id, PositionName, RegistrationEndDateTime, Title

### Community 54 - ".GetMyPostulationsAsync"
Cohesion: 0.48
Nodes (3): List, Task, ICandidacyService

### Community 55 - "PostulationFormDto"
Cohesion: 0.29
Nodes (7): PostulationFormDto, EventId, EventTitle, PositionDescription, PositionName, RegistrationEndDateTime, Requirements

### Community 56 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 57 - "WhitelistStatsDto"
Cohesion: 0.40
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 58 - "CandidacyDocument"
Cohesion: 0.22
Nodes (9): DateTime, CandidacyDocument, Candidate, CandidateId, FileUrl, Id, Requirement, RequirementId (+1 more)

### Community 59 - "ICandidacyService.cs"
Cohesion: 0.40
Nodes (4): PostulationResult, CandidateId, ErrorMessage, Success

### Community 61 - "Census Whitelist"
Cohesion: 0.67
Nodes (4): Annual Grade Promotion, Atomic Self-Registration, Census Whitelist, Census Whitelist Self-Registration

### Community 62 - "ASP.NET Core MVC Architecture"
Cohesion: 0.67
Nodes (3): Active Session Store, ASP.NET Core MVC Architecture, MySQL Electoral Data Model

### Community 97 - "AdminCandidatesController"
Cohesion: 0.31
Nodes (7): HttpGet, HttpPost, IActionResult, IConfiguration, Task, AdminCandidatesController, StatusCode

### Community 98 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 99 - "IEventService"
Cohesion: 0.36
Nodes (3): List, Task, IEventService

### Community 100 - "CandidateProposal"
Cohesion: 0.33
Nodes (6): CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id

### Community 103 - "Controller"
Cohesion: 0.33
Nodes (5): Controller, ILogger, ErrorController, IActionResult, HomeController

### Community 104 - "CandidateReviewService"
Cohesion: 0.42
Nodes (3): List, Task, CandidateReviewService

### Community 128 - "RequirementCheckDto"
Cohesion: 0.25
Nodes (8): DateTime, RequirementCheckDto, Description, FileUrl, IsMandatory, IsUploaded, RequirementId, UploadedAt

### Community 129 - "ICandidateReviewService"
Cohesion: 0.39
Nodes (3): List, Task, ICandidateReviewService

### Community 130 - "Memory Maintenance"
Cohesion: 0.33
Nodes (5): Add/update threshold, Discovery Model, Maintenance Actions, Memory Maintenance, Style

### Community 131 - ".OnModelCreating"
Cohesion: 0.25
Nodes (7): ModelBuilder, ICollection, Role, Description, Id, Name, Voters

### Community 132 - "AdminEmailReportController"
Cohesion: 0.38
Nodes (5): DateTime, HttpPost, IActionResult, Task, AdminEmailReportController

### Community 133 - "VoterSearchResultDto"
Cohesion: 0.40
Nodes (5): VoterSearchResultDto, Documento, FullName, GradeName, Id

### Community 134 - "RequirementInputDto"
Cohesion: 0.50
Nodes (4): RequirementInputDto, Description, Id, IsMandatory

### Community 137 - "AcademicYear"
Cohesion: 0.29
Nodes (7): DateTime, AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year

### Community 138 - "VoterPromotionDetail"
Cohesion: 0.29
Nodes (7): VoterPromotionDetail, CurrentGrade, FullName, Id, Outcome, SourceType, TargetGrade

## Knowledge Gaps
- **554 isolated node(s):** `Actions`, `IpAddresses`, `ShouldFail`, `ShouldFailFirst`, `Messages` (+549 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 754 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **62 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `.CreateCensusService`, `.UpdateContactEmailAsync`, `.OnModelCreating`, `AdminEmailReportController`, `AdminEventsController`, `.CreateService`, `Candidate`, `VotingService`, `PqrTicket`, `AcademicYear`, `CensusWhitelist`, `VotingEvent`, `IAuditService`, `Task`, `PromotionResult`, `Voter`, `AdminCensusController`, `EmailType`, `LoginAttemptStore`, `EventService`, `VwPendingEmailQueue`, `.LogAsync`, `Grade`, `PositionRequirement`, `FakeAuditService`, `EmailQueue`, `VwPendingCandidacy`, `CandidacyService`, `ElectionPosition`, `VwActiveCensu`, `VwPendingWhitelist`, `Vote`, `VwVoteCount`, `VoterEventParticipation`, `CandidacyDocument`, `AdminCandidatesController`, `CandidateProposal`, `CandidateReviewService`?**
  _High betweenness centrality (0.276) - this node is a cross-community bridge._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Models` to `.UpdateContactEmailAsync`, `FakeAuditService`, `IAuditService`, `.CreateService`, `IDocumentEncryptionService`, `.Login`, `Voter`, `EmailType`, `LoginAttemptStore`, `.LogAsync`, `ICandidacyService.cs`?**
  _High betweenness centrality (0.094) - this node is a cross-community bridge._
- **Why does `WahlMirai.Web.Models` connect `WahlMirai.Web.Models` to `FakeAuditService`, `IAuditService`, `Voter`, `EmailType`, `.LogAsync`, `ICandidacyService.cs`?**
  _High betweenness centrality (0.075) - this node is a cross-community bridge._
- **What connects `Actions`, `IpAddresses`, `ShouldFail` to the rest of the system?**
  _554 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `WahlMirai.Web.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.05695970695970696 - nodes in this community are weakly interconnected._
- **Should `.UpdateContactEmailAsync` be split into smaller, more focused modules?**
  _Cohesion score 0.07407407407407407 - nodes in this community are weakly interconnected._
- **Should `CandidateReviewDetailDto` be split into smaller, more focused modules?**
  _Cohesion score 0.08695652173913043 - nodes in this community are weakly interconnected._