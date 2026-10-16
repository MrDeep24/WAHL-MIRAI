# Graph Report - WAHL-MIRAI  (2026-10-15)

## Corpus Check
- 180 files · ~493,341 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 28 file(s) not represented in the graph (top: .resx 14, (none) 8, .css 3)

## Summary
- 1820 nodes · 4095 edges · 155 communities (72 shown, 83 thin omitted)
- Extraction: 85% EXTRACTED · 15% INFERRED · 0% AMBIGUOUS · INFERRED: 628 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `865faaa1`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- .CreateCensusService
- ProfileService
- CandidateReviewDetailDto
- AdminEventsController
- CensusWhitelist
- .CreateService
- Candidate
- Controller
- ProfileViewModel
- PromotionResult
- EmailReportViewModel
- VotingEvent
- .LogAsync
- PqrTicket
- WahlMiraiDbContext
- ActiveSessionStore
- Voter
- AdminCensusController
- ICandidateReviewService
- LoginAttemptStore
- EventService
- RegistrationOtpStore
- WahlMirai.Tests
- .OnModelCreating
- AdminAccountService
- ayuda-chatbot.js
- http
- Grade
- pqr-manage.js
- PromotionPreview
- VoterDetailDto
- .Apply
- AuthController
- CandidacyDocument
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
- .ImportCsvAsync
- TestWhitelistService
- PostulationFormDto
- theme.js
- WhitelistStatsDto
- WahlMirai.Web.Services
- PostulableEventDto
- Census Whitelist
- ASP.NET Core MVC Architecture
- WahlMirai.Web.ViewModels
- .BuildOptions
- Development Environment
- Administrator Voting Workflow
- VoterPromotionDetail
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
- system_componentmodel_dataannotations
- Real-Time Scrutiny
- AdminCandidatesController
- WahlMirai.Web
- RegistroStep2ViewModel
- CandidateProposal
- Contextual Layouts
- Localization Audit
- CandidateReviewService
- MIT License
- MIT License
- MIT License
- RequirementCheckDto
- wahl_mirai_db_v2_9_completo.sql
- memory_maintenance.md
- IAuthService
- .CreateController
- AuditLog
- RequirementInputDto
- .From
- .GetMyPostulationsAsync
- AcademicYear
- ICandidacyService.cs
- WahlMirai.Web.ViewModels.RestablecerViewModel
- Program.cs
- .RetryEmail
- IWhitelistService
- PositionRequirement
- Directrices y Reglas del Proyecto WAHL-MIRAI
- EmailType
- EmailQueueItemDto
- .CreateTestContextAsync
- Graphify Auto-Refresh
- EmailRetryPolicyTests
- EventGrade
- Serena memory maintenance
- AdminAccountViewModels.cs
- FakeAuthService
- .IsPasswordCarryingType

## God Nodes (most connected - your core abstractions)
1. `WahlMiraiDbContext` - 89 edges
2. `WahlMirai.Web.Models` - 63 edges
3. `WahlMirai.Web.Services` - 62 edges
4. `FakeAuditService` - 60 edges
5. `VotingEvent` - 59 edges
6. `Voter` - 55 edges
7. `Candidate` - 41 edges
8. `FakeLoginAttemptStore` - 40 edges
9. `LoginAttemptStore` - 36 edges
10. `CensusWhitelist` - 32 edges

## Surprising Connections (you probably didn't know these)
- `Atomic Self-Registration` --semantically_similar_to--> `Census Whitelist Self-Registration`  [INFERRED] [semantically similar]
  docs/MODULO_CENSO_WHITELIST_v2.8.md → README.md
- `TestWhitelistService` --references--> `CensusWhitelist`  [EXTRACTED]
  WahlMirai.Tests/AuthControllerRegistrationTests.cs → WahlMirai.Web/Models/CensusWhitelist.cs
- `TestWhitelistService` --implements--> `IWhitelistService`  [EXTRACTED]
  WahlMirai.Tests/AuthControllerRegistrationTests.cs → WahlMirai.Web/Services/WhitelistService.cs
- `FakeAuthService` --implements--> `IAuthService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IAuthService.cs
- `FakeEncryptionService` --implements--> `IDocumentEncryptionService`  [EXTRACTED]
  WahlMirai.Tests/Common/SharedFakes.cs → WahlMirai.Web/Services/IDocumentEncryptionService.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Census Self-Registration and Security Architecture** — readme_census_whitelist_self_registration, docs_modulo_censo_whitelist_v2_8_atomic_self_registration, docs_arquitectura_y_diseno_v2_9_active_session_store [INFERRED 0.75]

## Communities (155 total, 83 thin omitted)

### Community 2 - "ProfileService"
Cohesion: 0.12
Nodes (7): ChangePasswordRequest, ProfileController, UpdateEmailRequest, VerifyPasswordRequest, ILoginAttemptStore, IProfileService, ProfileService

### Community 3 - "CandidateReviewDetailDto"
Cohesion: 0.09
Nodes (23): CandidateReviewDetailDto, AllowCorrection, ApprovedWithExceptions, CandidateEmail, CandidateId, CandidateName, EnrolledAt, EventId (+15 more)

### Community 5 - "CensusWhitelist"
Cohesion: 0.12
Nodes (15): CensusWhitelist, ClaimedAt, ClaimedByUserId, ClaimedByVoter, ContactEmail, CreatedAt, DocumentHash, EncryptedDocument (+7 more)

### Community 6 - ".CreateService"
Cohesion: 0.21
Nodes (7): AdminAccountServiceTests, FakeEncryptionService, Role, Description, Id, Name, Voters

### Community 7 - "Candidate"
Cohesion: 0.08
Nodes (23): Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt, ExceptionsDetail, GovernmentPlanUrl (+15 more)

### Community 8 - "Controller"
Cohesion: 0.06
Nodes (15): ElectorController, ErrorController, HomeController, MediaController, ResultsController, ResultsHub, IVotingService, VotingService (+7 more)

### Community 9 - "ProfileViewModel"
Cohesion: 0.25
Nodes (8): ProfileViewModel, ContactEmail, DocumentDisplay, FullName, GradeName, PositionTitle, Role, Status

### Community 10 - "PromotionResult"
Cohesion: 0.17
Nodes (11): IPromotionService, PromotionResult, ActivePromotedCount, ErrorCount, GraduatedCount, Message, PromotedCount, RetainedCount (+3 more)

### Community 11 - "EmailReportViewModel"
Cohesion: 0.15
Nodes (12): EmailReportViewModel, EmailTypeFilter, EndDate, FailedEmails, Items, PendingEmails, Search, SentEmails (+4 more)

### Community 12 - "VotingEvent"
Cohesion: 0.06
Nodes (29): VotingEvent, Candidates, CreatedAt, CreatedByVoter, CreatedByVoterId, DeletedAt, Description, ElectionType (+21 more)

### Community 14 - "PqrTicket"
Cohesion: 0.09
Nodes (20): PqrListTests, PqrController, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt (+12 more)

### Community 15 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (23): WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals, Candidates, CensusWhitelists, ElectionPositions (+15 more)

### Community 16 - "ActiveSessionStore"
Cohesion: 0.16
Nodes (8): ActiveSessionStoreTests, ActiveSessionStore, SessionEntry, IActiveSessionStore, SessionInvalidationReason, AccountDisabledOrDeleted, LoggedOut, ReplacedByNewDevice

### Community 17 - "Voter"
Cohesion: 0.08
Nodes (23): Voter, AuditLogs, Candidates, ContactEmail, DeletedAt, DocumentHash, EmailQueues, EncryptedDocument (+15 more)

### Community 20 - "LoginAttemptStore"
Cohesion: 0.12
Nodes (13): AuthServiceTests, FakeTimeProvider, LoginAttemptStoreTests, AuthService, LoginAttemptPolicy, LockoutThreshold, WindowDuration, LoginAttemptResult (+5 more)

### Community 21 - "EventService"
Cohesion: 0.14
Nodes (7): EventService, IEventService, VoterSearchResultDto, Documento, FullName, GradeName, Id

### Community 22 - "RegistrationOtpStore"
Cohesion: 0.21
Nodes (3): IRegistrationOtpStore, OtpEntry, RegistrationOtpStore

### Community 23 - "WahlMirai.Tests"
Cohesion: 0.18
Nodes (9): coverlet.collector (6.0.4), Microsoft.EntityFrameworkCore.InMemory (9.0.7), Microsoft.EntityFrameworkCore.Relational (9.0.7), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, WahlMirai.Tests (+1 more)

### Community 24 - ".OnModelCreating"
Cohesion: 0.16
Nodes (8): VwPendingEmailQueue, Attempts, ContactEmail, CreatedAt, EmailType, FullName, Id, VoterId

### Community 25 - "AdminAccountService"
Cohesion: 0.06
Nodes (29): AdminAccountsController, DocumentEncryptionService, AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id (+21 more)

### Community 26 - "ayuda-chatbot.js"
Cohesion: 0.33
Nodes (16): addMessage(), bindEscalationButton(), bindEvents(), bindResolutionButtons(), disableResolutionBlock(), escalateToPqr(), escapeHtml(), findTopicByKeyword() (+8 more)

### Community 27 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 28 - "Grade"
Cohesion: 0.25
Nodes (7): Grade, EventGrades, Id, IsLastGrade, Name, SequenceOrder, Voters

### Community 29 - "pqr-manage.js"
Cohesion: 0.27
Nodes (13): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), formatDate(), getCsrfToken(), initials(), openModal() (+5 more)

### Community 30 - "PromotionPreview"
Cohesion: 0.17
Nodes (10): PromotionPreview, ActiveVotersCount, CurrentYear, EligibleCount, ExcludedCount, HasRunThisYear, PendingWhitelistCount, PreviewList (+2 more)

### Community 31 - "VoterDetailDto"
Cohesion: 0.14
Nodes (14): VoterDetailDto, ContactEmail, DeletedAt, Document, ExcluirDePromocion, FullName, GradeId, GradeName (+6 more)

### Community 34 - "CandidacyDocument"
Cohesion: 0.22
Nodes (8): CandidacyDocument, Candidate, CandidateId, FileUrl, Id, Requirement, RequirementId, UploadedAt

### Community 35 - "WhitelistDetailDto"
Cohesion: 0.14
Nodes (13): WhitelistDetailDto, ClaimedAt, ClaimedByUserId, ContactEmail, CreatedAt, Document, DocumentHash, ExcluirDePromocion (+5 more)

### Community 36 - "MyPostulationDto"
Cohesion: 0.17
Nodes (12): MyPostulationDto, AllowCorrection, ApprovedWithExceptions, CandidateId, EnrolledAt, EventId, EventTitle, ExceptionsDetail (+4 more)

### Community 37 - "PagedResult"
Cohesion: 0.15
Nodes (10): PagedResult, EndItemIndex, HasNextPage, HasPreviousPage, Items, PageNumber, PageSize, StartItemIndex (+2 more)

### Community 38 - "FakeAuditService"
Cohesion: 0.05
Nodes (30): FakeAuditService, Actions, Details, IpAddresses, FakeLoginAttemptStore, FailureCallCount, SuccessCallCount, CredentialServiceTests (+22 more)

### Community 39 - "EmailQueue"
Cohesion: 0.18
Nodes (10): EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt, Status (+2 more)

### Community 40 - "VwPendingCandidacy"
Cohesion: 0.18
Nodes (10): VwPendingCandidacy, CandidateId, CandidateName, EnrolledAt, EventTitle, MandatoryDocumentsUploaded, MandatoryRequirements, PositionId (+2 more)

### Community 42 - "ForcePasswordChangeMiddleware"
Cohesion: 0.22
Nodes (3): WahlMirai.Web.Middleware, ForcePasswordChangeMiddleware, ForcePasswordChangeMiddlewareExtensions

### Community 43 - "ElectionPosition"
Cohesion: 0.20
Nodes (8): ElectionPosition, CreatedAt, Description, Id, Name, PositionRequirements, Status, VotingEvents

### Community 44 - "VwActiveCensu"
Cohesion: 0.20
Nodes (9): VwActiveCensu, ContactEmail, ExcluirDePromocion, FullName, Grade, Id, RegisteredAt, Status (+1 more)

### Community 45 - "VwPendingWhitelist"
Cohesion: 0.18
Nodes (10): VwPendingWhitelist, ContactEmail, CreatedAt, DocumentHash, EncryptedDocument, ExcluirDePromocion, FullName, Grade (+2 more)

### Community 46 - "DESIGN.md"
Cohesion: 0.22
Nodes (8): Brand & Style, Colors, Components, Elevation & Depth, Graphify (knowledge graph para agentes de IA), Layout & Spacing, Shapes, Typography

### Community 47 - "PostulationSubmitDto"
Cohesion: 0.22
Nodes (7): PostulationSubmitDto, Documents, EventId, GovernmentPlan, Photo, Proposals, Slogan

### Community 48 - "Vote"
Cohesion: 0.22
Nodes (8): Vote, Candidate, CandidateId, Id, VotedAt, VoteHash, VotingEvent, VotingEventId

### Community 49 - "VwVoteCount"
Cohesion: 0.22
Nodes (9): VwVoteCount, CandidateId, CandidateName, EventId, EventStatus, EventTitle, IsBlankVote, TotalVotes (+1 more)

### Community 50 - "locale.js"
Cohesion: 0.42
Nodes (8): applyLocalizedThemeLabels(), getCultureFromCookie(), initSelects(), setCultureCookie(), setupMutationObserver(), syncHtmlLang(), translateDOM(), getTranslation()

### Community 51 - "VoterEventParticipation"
Cohesion: 0.25
Nodes (7): VoterEventParticipation, Id, ParticipatedAt, Voter, VoterId, VotingEvent, VotingEventId

### Community 52 - "check-i18n-coverage.ps1"
Cohesion: 0.38
Nodes (3): Add-ExtractedString(), Get-FilterCategory(), Test-UserFacingString()

### Community 53 - ".ImportCsvAsync"
Cohesion: 0.19
Nodes (10): CsvImportResult, DuplicateCount, ErrorCount, Errors, InsertedCount, ProcessedCount, CsvRowError, Identifier (+2 more)

### Community 54 - "TestWhitelistService"
Cohesion: 0.14
Nodes (7): AuthControllerRegistrationTests, TestTempDataProvider, TestWhitelistService, LookupCount, LookupResult, RegistrationCount, RegistrationException

### Community 55 - "PostulationFormDto"
Cohesion: 0.29
Nodes (7): PostulationFormDto, EventId, EventTitle, PositionDescription, PositionName, RegistrationEndDateTime, Requirements

### Community 56 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 57 - "WhitelistStatsDto"
Cohesion: 0.40
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 58 - "WahlMirai.Web.Services"
Cohesion: 0.14
Nodes (3): WahlMirai.Web.Services, WahlMirai.Tests, WahlMirai.Tests.Common

### Community 59 - "PostulableEventDto"
Cohesion: 0.29
Nodes (6): PostulableEventDto, Description, Id, PositionName, RegistrationEndDateTime, Title

### Community 61 - "Census Whitelist"
Cohesion: 0.67
Nodes (4): Annual Grade Promotion, Atomic Self-Registration, Census Whitelist, Census Whitelist Self-Registration

### Community 62 - "ASP.NET Core MVC Architecture"
Cohesion: 0.33
Nodes (5): Active Session Store, ASP.NET Core MVC Architecture, MySQL Electoral Data Model, Requisitos Wahl Mirai v2.9, Electoral Voting Platform

### Community 65 - "WahlMirai.Web.ViewModels"
Cohesion: 0.19
Nodes (3): WahlMirai.Web.Controllers, WahlMirai.Web.ViewModels, WahlMirai.Web

### Community 69 - "VoterPromotionDetail"
Cohesion: 0.29
Nodes (7): VoterPromotionDetail, CurrentGrade, FullName, Id, Outcome, SourceType, TargetGrade

### Community 95 - "system_componentmodel_dataannotations"
Cohesion: 0.17
Nodes (8): ChangePasswordViewModel, ConfirmPassword, NewPassword, LoginViewModel, Document, Password, RegistroStep1ViewModel, Document

### Community 98 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 99 - "RegistroStep2ViewModel"
Cohesion: 0.18
Nodes (11): RegistroStep2ViewModel, ConfirmPassword, ContactEmail, Document, FullName, GradeName, MaskedEmail, OtpCode (+3 more)

### Community 100 - "CandidateProposal"
Cohesion: 0.33
Nodes (6): CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id

### Community 128 - "RequirementCheckDto"
Cohesion: 0.25
Nodes (7): RequirementCheckDto, Description, FileUrl, IsMandatory, IsUploaded, RequirementId, UploadedAt

### Community 129 - "wahl_mirai_db_v2_9_completo.sql"
Cohesion: 0.23
Nodes (22): `academic_years`, `audit_log`, `candidacy_documents`, `candidate_proposals`, `candidates`, `census_whitelist`, `election_positions`, `email_queue` (+14 more)

### Community 132 - ".CreateController"
Cohesion: 0.11
Nodes (10): RecuperacionAccesoControllerTests, RecuperacionAccesoController, ICredentialService, RecuperarAccesoViewModel, Documento, RestablecerViewModel, ConfirmPassword, IsInvalidLink (+2 more)

### Community 133 - "AuditLog"
Cohesion: 0.11
Nodes (15): AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue, OccurredAt (+7 more)

### Community 134 - "RequirementInputDto"
Cohesion: 0.50
Nodes (4): RequirementInputDto, Description, Id, IsMandatory

### Community 137 - "AcademicYear"
Cohesion: 0.29
Nodes (6): AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year

### Community 138 - "ICandidacyService.cs"
Cohesion: 0.40
Nodes (4): PostulationResult, CandidateId, ErrorMessage, Success

### Community 143 - "PositionRequirement"
Cohesion: 0.22
Nodes (8): PositionRequirement, CandidacyDocuments, Description, DisplayOrder, Id, IsMandatory, Position, PositionId

### Community 145 - "EmailType"
Cohesion: 0.25
Nodes (7): EmailType, CAMBIO_PERFIL, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA, REASIGNACION_ADMIN, RECUPERACION_ACCESO, RESPUESTA_PQR

### Community 146 - "EmailQueueItemDto"
Cohesion: 0.18
Nodes (11): EmailQueueItemDto, Attempts, CreatedAt, EmailType, ErrorMessage, Id, RecipientEmail, SentAt (+3 more)

### Community 147 - ".CreateTestContextAsync"
Cohesion: 0.06
Nodes (21): EmailQueueBackgroundServiceTests, TestContext, EmailSender, PasswordStore, Worker, TestEmailSender, SecretAvailableAtSend, SentBodies (+13 more)

### Community 152 - "EventGrade"
Cohesion: 0.33
Nodes (6): EventGrade, Grade, GradeId, Id, VotingEvent, VotingEventId

### Community 153 - "Serena memory maintenance"
Cohesion: 0.40
Nodes (4): Discovery model, Maintenance, Serena memory maintenance, Style and scope

## Knowledge Gaps
- **554 isolated node(s):** `LookupCount`, `RegistrationCount`, `RegistrationException`, `LookupResult`, `Actions` (+549 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 813 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **83 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `.CreateCensusService`, `ProfileService`, `.CreateController`, `AdminEventsController`, `.CreateService`, `AuditLog`, `Controller`, `AcademicYear`, `Candidate`, `CensusWhitelist`, `VotingEvent`, `.RetryEmail`, `PqrTicket`, `PositionRequirement`, `.LogAsync`, `Voter`, `AdminCensusController`, `.CreateTestContextAsync`, `LoginAttemptStore`, `EventService`, `EventGrade`, `.OnModelCreating`, `AdminAccountService`, `Grade`, `CandidacyDocument`, `FakeAuditService`, `EmailQueue`, `VwPendingCandidacy`, `CandidacyService`, `ElectionPosition`, `VwActiveCensu`, `VwPendingWhitelist`, `Vote`, `VwVoteCount`, `VoterEventParticipation`, `PromotionResult`, `AdminCandidatesController`, `CandidateProposal`, `CandidateReviewService`?**
  _High betweenness centrality (0.253) - this node is a cross-community bridge._
- **Are the 52 inferred relationships involving `FakeAuditService` (e.g. with `.CreateAdmin_StoresAccountAndInitialCredentialQueue()` and `.CreateService()`) actually correct?**
  _`FakeAuditService` has 52 INFERRED edges - model-reasoned connections that need verification._
- **What connects `LookupCount`, `RegistrationCount`, `RegistrationException` to the rest of the system?**
  _554 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `WahlMirai.Web.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.14761904761904762 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Services` to `WahlMirai.Web.Models`, `WahlMirai.Web.ViewModels`, `.IsPasswordCarryingType`, `ProfileService`, `AuditLog`, `FakeAuditService`, `AuthControllerRegistrationTests.cs`, `ICandidacyService.cs`, `Program.cs`, `ActiveSessionStore`, `EmailType`, `AdminAccountService`, `AdminAccountViewModels.cs`?**
  _High betweenness centrality (0.115) - this node is a cross-community bridge._
- **Should `ProfileService` be split into smaller, more focused modules?**
  _Cohesion score 0.12162162162162163 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Models` connect `WahlMirai.Web.Models` to `WahlMirai.Web.ViewModels`, `AuditLog`, `AuthControllerRegistrationTests.cs`, `ICandidacyService.cs`, `Program.cs`, `.CreateTestContextAsync`, `WahlMirai.Web.Services`?**
  _High betweenness centrality (0.061) - this node is a cross-community bridge._