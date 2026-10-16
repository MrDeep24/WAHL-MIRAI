# Graph Report - WAHL-MIRAI  (2026-10-16)

## Corpus Check
- 201 files · ~877,377 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 273 file(s) not represented in the graph (top: .dll 177, .br 22, .gz 22)

## Summary
- 1896 nodes · 4285 edges · 171 communities (78 shown, 93 thin omitted)
- Extraction: 84% EXTRACTED · 16% INFERRED · 0% AMBIGUOUS · INFERRED: 673 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e3229c07`
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
- FakeAuditService
- PromotionResult
- EmailReportViewModel
- VotingEvent
- .LogAsync
- PqrTicket
- WahlMiraiDbContext
- ActiveSessionStore
- Voter
- AdminCensusController
- AdminAccountsController
- LoginAttemptStore
- EventService
- RegistrationOtpStore
- WahlMirai.Tests
- .OnModelCreating
- AdminAccountService
- ayuda-chatbot.js
- http
- Grade
- WahlMirai.Web/wwwroot/js/pqr-manage.js
- PromotionPreview
- VoterDetailDto
- .Apply
- AuthController
- CandidacyDocument
- WhitelistDetailDto
- MyPostulationDto
- PagedResult
- PasswordResetTokenStore
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
- VotingService
- Census Whitelist
- ASP.NET Core MVC Architecture
- WahlMirai.Web.ViewModels
- .BuildOptions
- Development Environment
- Administrator Voting Workflow
- publish/wwwroot/js/ayuda-chatbot.js
- dynamic
- Graphify Repository Instructions
- List<WahlMirai.Web.Models.VotingEvent>
- List<WahlMirai.Web.Models.VwVoteCount>
- List<WahlMirai.Web.Services.CandidateReviewDetailDto>
- graphify-refresh.ps1
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
- ProfileViewModel
- Real-Time Scrutiny
- publish/wwwroot/js/pqr-manage.js
- WahlMirai.Web
- RegistroStep2ViewModel
- CandidateProposal
- Contextual Layouts
- Localization Audit
- RecuperacionAccesoControllerTests.cs
- MIT License
- MIT License
- MIT License
- .CreateTestContextAsync
- wahl_mirai_db_v2_9_completo.sql
- memory_maintenance.md
- .CreateController
- AuditLog
- RequirementInputDto
- .From
- .GetMyPostulationsAsync
- AcademicYear
- TestContext
- WahlMirai.Web.ViewModels.RestablecerViewModel
- .RetryEmail
- IWhitelistService
- PositionRequirement
- Directrices y Reglas del Proyecto WAHL-MIRAI
- EmailType
- EmailQueueItemDto
- EmailSettings
- Política de Refresco de Graphify
- EmailQueueBackgroundService
- EmailRetryPolicyTests
- IEventService
- EventGrade
- Serena memory maintenance
- publish/wwwroot/js/locale.js
- TestEmailSender
- .HasVotedAsync
- AdminAccountViewModels.cs
- publish/wwwroot/js/theme.js
- ResultsHub
- FakeAuthService
- .IsPasswordCarryingType
- DocumentEncryptionService
- VoterSearchResultDto
- System.Text.Encodings.Web

## God Nodes (most connected - your core abstractions)
1. `WahlMiraiDbContext` - 90 edges
2. `FakeAuditService` - 66 edges
3. `VotingEvent` - 65 edges
4. `WahlMirai.Web.Models` - 64 edges
5. `WahlMirai.Web.Services` - 63 edges
6. `Voter` - 58 edges
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

## Communities (171 total, 93 thin omitted)

### Community 2 - "ProfileService"
Cohesion: 0.12
Nodes (7): ChangePasswordRequest, ProfileController, UpdateEmailRequest, VerifyPasswordRequest, ILoginAttemptStore, IProfileService, ProfileService

### Community 3 - "CandidateReviewDetailDto"
Cohesion: 0.06
Nodes (33): AdminCandidatesController, CandidateReviewService, CandidateReviewDetailDto, AllowCorrection, ApprovedWithExceptions, CandidateEmail, CandidateId, CandidateName (+25 more)

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
Cohesion: 0.09
Nodes (10): ErrorController, HomeController, MediaController, ErrorViewModel, ExceptionMessage, Message, ShowHelpButton, ShowHomeButton (+2 more)

### Community 9 - "FakeAuditService"
Cohesion: 0.17
Nodes (14): FakeAuditService, Actions, Details, IpAddresses, FakeLoginAttemptStore, FailureCallCount, SuccessCallCount, FakeCredentialService (+6 more)

### Community 10 - "PromotionResult"
Cohesion: 0.17
Nodes (11): IPromotionService, PromotionResult, ActivePromotedCount, ErrorCount, GraduatedCount, Message, PromotedCount, RetainedCount (+3 more)

### Community 11 - "EmailReportViewModel"
Cohesion: 0.15
Nodes (12): EmailReportViewModel, EmailTypeFilter, EndDate, FailedEmails, Items, PendingEmails, Search, SentEmails (+4 more)

### Community 12 - "VotingEvent"
Cohesion: 0.06
Nodes (29): VotingEvent, Candidates, CreatedAt, CreatedByVoter, CreatedByVoterId, DeletedAt, Description, ElectionType (+21 more)

### Community 13 - ".LogAsync"
Cohesion: 0.13
Nodes (4): IActiveSessionStore, IAuditService, CensusService, ICensusService

### Community 14 - "PqrTicket"
Cohesion: 0.09
Nodes (20): PqrListTests, PqrController, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt (+12 more)

### Community 15 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (23): WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals, Candidates, CensusWhitelists, ElectionPositions (+15 more)

### Community 16 - "ActiveSessionStore"
Cohesion: 0.16
Nodes (7): ActiveSessionStoreTests, ActiveSessionStore, SessionEntry, SessionInvalidationReason, AccountDisabledOrDeleted, LoggedOut, ReplacedByNewDevice

### Community 17 - "Voter"
Cohesion: 0.08
Nodes (23): Voter, AuditLogs, Candidates, ContactEmail, DeletedAt, DocumentHash, EmailQueues, EncryptedDocument (+15 more)

### Community 19 - "AdminAccountsController"
Cohesion: 0.14
Nodes (14): AdminAccountsController, AdminAccountEditViewModel, Id, AdminAccountFormViewModel, ContactEmail, Document, FullName, PositionTitle (+6 more)

### Community 20 - "LoginAttemptStore"
Cohesion: 0.11
Nodes (13): AuthServiceTests, FakeTimeProvider, LoginAttemptStoreTests, AuthService, LoginAttemptPolicy, LockoutThreshold, WindowDuration, LoginAttemptResult (+5 more)

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
Cohesion: 0.11
Nodes (15): AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id, PositionTitle, RegisteredAt (+7 more)

### Community 26 - "ayuda-chatbot.js"
Cohesion: 0.33
Nodes (16): addMessage(), bindEscalationButton(), bindEvents(), bindResolutionButtons(), disableResolutionBlock(), escalateToPqr(), escapeHtml(), findTopicByKeyword() (+8 more)

### Community 27 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 28 - "Grade"
Cohesion: 0.25
Nodes (7): Grade, EventGrades, Id, IsLastGrade, Name, SequenceOrder, Voters

### Community 29 - "WahlMirai.Web/wwwroot/js/pqr-manage.js"
Cohesion: 0.27
Nodes (13): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), formatDate(), getCsrfToken(), initials(), openModal() (+5 more)

### Community 30 - "PromotionPreview"
Cohesion: 0.11
Nodes (17): PromotionPreview, ActiveVotersCount, CurrentYear, EligibleCount, ExcludedCount, HasRunThisYear, PendingWhitelistCount, PreviewList (+9 more)

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
Cohesion: 0.11
Nodes (18): MyPostulationDto, AllowCorrection, ApprovedWithExceptions, CandidateId, EnrolledAt, EventId, EventTitle, ExceptionsDetail (+10 more)

### Community 37 - "PagedResult"
Cohesion: 0.17
Nodes (10): PagedResult, EndItemIndex, HasNextPage, HasPreviousPage, Items, PageNumber, PageSize, StartItemIndex (+2 more)

### Community 38 - "PasswordResetTokenStore"
Cohesion: 0.07
Nodes (16): CredentialServiceTests, FailingWahlMiraiDbContext, FailOnSave, FakeActiveSessionStore, LastInvalidatedReason, LastInvalidatedUserId, SpyingTokenStore, LastIssuedToken (+8 more)

### Community 39 - "EmailQueue"
Cohesion: 0.18
Nodes (10): EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt, Status (+2 more)

### Community 40 - "VwPendingCandidacy"
Cohesion: 0.18
Nodes (10): VwPendingCandidacy, CandidateId, CandidateName, EnrolledAt, EventTitle, MandatoryDocumentsUploaded, MandatoryRequirements, PositionId (+2 more)

### Community 41 - "CandidacyService"
Cohesion: 0.22
Nodes (5): CandidacyService, PostulationResult, CandidateId, ErrorMessage, Success

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
Cohesion: 0.25
Nodes (7): PostulationFormDto, EventId, EventTitle, PositionDescription, PositionName, RegistrationEndDateTime, Requirements

### Community 56 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 57 - "WhitelistStatsDto"
Cohesion: 0.50
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 58 - "WahlMirai.Web.Services"
Cohesion: 0.15
Nodes (3): WahlMirai.Web.Services, WahlMirai.Tests, WahlMirai.Tests.Common

### Community 61 - "Census Whitelist"
Cohesion: 0.67
Nodes (4): Annual Grade Promotion, Atomic Self-Registration, Census Whitelist, Census Whitelist Self-Registration

### Community 62 - "ASP.NET Core MVC Architecture"
Cohesion: 0.33
Nodes (5): Active Session Store, ASP.NET Core MVC Architecture, MySQL Electoral Data Model, Requisitos Wahl Mirai v2.9, Electoral Voting Platform

### Community 65 - "WahlMirai.Web.ViewModels"
Cohesion: 0.21
Nodes (3): WahlMirai.Web.Controllers, WahlMirai.Web.ViewModels, WahlMirai.Web

### Community 69 - "publish/wwwroot/js/ayuda-chatbot.js"
Cohesion: 0.33
Nodes (16): addMessage(), bindEscalationButton(), bindEvents(), bindResolutionButtons(), disableResolutionBlock(), escalateToPqr(), escapeHtml(), findTopicByKeyword() (+8 more)

### Community 95 - "ProfileViewModel"
Cohesion: 0.12
Nodes (14): ChangePasswordViewModel, ConfirmPassword, NewPassword, LoginViewModel, Document, Password, ProfileViewModel, ContactEmail (+6 more)

### Community 97 - "publish/wwwroot/js/pqr-manage.js"
Cohesion: 0.28
Nodes (13): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), filteredTickets(), formatDate(), getCsrfToken(), initials() (+5 more)

### Community 98 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 99 - "RegistroStep2ViewModel"
Cohesion: 0.14
Nodes (13): RegistroStep1ViewModel, Document, RegistroStep2ViewModel, ConfirmPassword, ContactEmail, Document, FullName, GradeName (+5 more)

### Community 100 - "CandidateProposal"
Cohesion: 0.33
Nodes (6): CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id

### Community 129 - "wahl_mirai_db_v2_9_completo.sql"
Cohesion: 0.21
Nodes (23): `vw_pending_whitelist`, `academic_years`, `audit_log`, `candidacy_documents`, `candidate_proposals`, `candidates`, `census_whitelist`, `election_positions` (+15 more)

### Community 132 - ".CreateController"
Cohesion: 0.11
Nodes (10): RecuperacionAccesoControllerTests, RecuperacionAccesoController, ICredentialService, RecuperarAccesoViewModel, Documento, RestablecerViewModel, ConfirmPassword, IsInvalidLink (+2 more)

### Community 133 - "AuditLog"
Cohesion: 0.12
Nodes (14): AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue, OccurredAt (+6 more)

### Community 134 - "RequirementInputDto"
Cohesion: 0.50
Nodes (4): RequirementInputDto, Description, Id, IsMandatory

### Community 137 - "AcademicYear"
Cohesion: 0.29
Nodes (6): AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year

### Community 138 - "TestContext"
Cohesion: 0.18
Nodes (5): TestContext, EmailSender, PasswordStore, Worker, TestWahlMiraiDbContext

### Community 143 - "PositionRequirement"
Cohesion: 0.22
Nodes (8): PositionRequirement, CandidacyDocuments, Description, DisplayOrder, Id, IsMandatory, Position, PositionId

### Community 144 - "Directrices y Reglas del Proyecto WAHL-MIRAI"
Cohesion: 0.50
Nodes (3): Directrices y Reglas del Proyecto WAHL-MIRAI, Política de Actualización del Grafo de Conocimiento (Graphify), Reglas de aplicación:

### Community 145 - "EmailType"
Cohesion: 0.25
Nodes (7): EmailType, CAMBIO_PERFIL, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA, REASIGNACION_ADMIN, RECUPERACION_ACCESO, RESPUESTA_PQR

### Community 146 - "EmailQueueItemDto"
Cohesion: 0.18
Nodes (11): EmailQueueItemDto, Attempts, CreatedAt, EmailType, ErrorMessage, Id, RecipientEmail, SentAt (+3 more)

### Community 147 - "EmailSettings"
Cohesion: 0.14
Nodes (10): EmailSettings, BaseUrl, RateLimitPerMinute, SenderDisplayName, SenderEmail, SenderPassword, SmtpHost, SmtpPort (+2 more)

### Community 152 - "EventGrade"
Cohesion: 0.33
Nodes (6): EventGrade, Grade, GradeId, Id, VotingEvent, VotingEventId

### Community 153 - "Serena memory maintenance"
Cohesion: 0.40
Nodes (4): Discovery model, Maintenance, Serena memory maintenance, Style and scope

### Community 154 - "publish/wwwroot/js/locale.js"
Cohesion: 0.42
Nodes (8): applyLocalizedThemeLabels(), getCultureFromCookie(), initSelects(), setCultureCookie(), setupMutationObserver(), syncHtmlLang(), translateDOM(), getTranslation()

### Community 155 - "TestEmailSender"
Cohesion: 0.25
Nodes (4): TestEmailSender, SecretAvailableAtSend, SentBodies, SentSubjects

### Community 158 - "publish/wwwroot/js/theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 164 - "VoterSearchResultDto"
Cohesion: 0.40
Nodes (5): VoterSearchResultDto, Documento, FullName, GradeName, Id

## Knowledge Gaps
- **580 isolated node(s):** `LookupCount`, `RegistrationCount`, `RegistrationException`, `LookupResult`, `Actions` (+575 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 825 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **93 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `.CreateTestContextAsync`, `.CreateCensusService`, `ProfileService`, `CandidateReviewDetailDto`, `.CreateController`, `AdminEventsController`, `.CreateService`, `WahlMirai.Web.Models`, `AuditLog`, `FakeAuditService`, `TestContext`, `AcademicYear`, `Candidate`, `.RetryEmail`, `PqrTicket`, `PositionRequirement`, `VotingEvent`, `Voter`, `AdminCensusController`, `.LogAsync`, `LoginAttemptStore`, `EventService`, `EmailQueueBackgroundService`, `EventGrade`, `.OnModelCreating`, `CensusWhitelist`, `AdminAccountService`, `.HasVotedAsync`, `Grade`, `CandidacyDocument`, `PasswordResetTokenStore`, `EmailQueue`, `VwPendingCandidacy`, `CandidacyService`, `ElectionPosition`, `VwActiveCensu`, `VwPendingWhitelist`, `Vote`, `VwVoteCount`, `VoterEventParticipation`, `PromotionResult`, `VotingService`, `CandidateProposal`?**
  _High betweenness centrality (0.246) - this node is a cross-community bridge._
- **Are the 58 inferred relationships involving `FakeAuditService` (e.g. with `.CreateAdmin_StoresAccountAndInitialCredentialQueue()` and `.CreateService()`) actually correct?**
  _`FakeAuditService` has 58 INFERRED edges - model-reasoned connections that need verification._
- **What connects `LookupCount`, `RegistrationCount`, `RegistrationException` to the rest of the system?**
  _580 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ProfileService` be split into smaller, more focused modules?**
  _Cohesion score 0.12162162162162163 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Services` to `WahlMirai.Web.Models`, `WahlMirai.Web.ViewModels`, `.IsPasswordCarryingType`, `ProfileService`, `MailKitEmailSender.cs`, `PasswordResetTokenStore`, `AuthControllerRegistrationTests.cs`, `RecuperacionAccesoControllerTests.cs`, `FakeAuditService`, `AuthController.cs`, `ActiveSessionStore`, `EmailType`, `AdminAccountService`, `AdminAccountViewModels.cs`?**
  _High betweenness centrality (0.085) - this node is a cross-community bridge._
- **Are the 6 inferred relationships involving `VotingEvent` (e.g. with `.AddCandidate_AllowsGrade11_ForPersonero()` and `.AddCandidate_RejectsNonGrade11_ForPersonero()`) actually correct?**
  _`VotingEvent` has 6 INFERRED edges - model-reasoned connections that need verification._
- **Should `CandidateReviewDetailDto` be split into smaller, more focused modules?**
  _Cohesion score 0.05628415300546448 - nodes in this community are weakly interconnected._