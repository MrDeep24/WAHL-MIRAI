# Graph Report - WAHL-MIRAI  (2026-10-08)

## Corpus Check
- 171 files · ~479,225 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 28 file(s) not represented in the graph (top: .resx 14, (none) 8, .css 3)

## Summary
- 1664 nodes · 3561 edges · 162 communities (70 shown, 92 thin omitted)
- Extraction: 86% EXTRACTED · 14% INFERRED · 0% AMBIGUOUS · INFERRED: 484 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `6b437b77`
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
- VotingService
- PqrTicket
- PromotionResult
- EmailReportViewModel
- VotingEvent
- .LogAsync
- AdminAccountListItem
- WahlMiraiDbContext
- ActiveSessionStore
- Voter
- AdminCensusController
- EmailSettings
- LoginAttemptStore
- EventService
- AdminAccountsController
- WahlMirai.Tests
- .OnModelCreating
- CancellationToken
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
- CsvImportResult
- TestWhitelistService
- PostulationFormDto
- theme.js
- WhitelistStatsDto
- WahlMirai.Tests
- AdminAccountService
- Census Whitelist
- ASP.NET Core MVC Architecture
- WahlMirai.Web.Services
- .BuildOptions
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
- CandidateReviewService
- MIT License
- MIT License
- MIT License
- RequirementCheckDto
- wahl_mirai_db_v2_9_completo.sql
- memory_maintenance.md
- ErrorViewModel
- .Recuperar
- AuditLog
- RequirementInputDto
- .From
- .GetMyPostulationsAsync
- AcademicYear
- PendingPasswordStore
- AdminAccountFormViewModel
- microsoft_entityframeworkcore
- .RetryEmail
- DocumentEncryptionService
- PositionRequirement
- TestContext
- EmailType
- EmailQueueItemDto
- .CreateTestContextAsync
- EmailQueueBackgroundService
- CredentialService
- EmailRetryPolicyTests
- .HasVotedAsync
- EventGrade
- Serena memory maintenance
- ResultsHub
- Controller
- WahlMirai.Web.ViewModels
- IAuditService
- FakeAuthService
- .IsPasswordCarryingType

## God Nodes (most connected - your core abstractions)
1. `WahlMiraiDbContext` - 82 edges
2. `WahlMirai.Web.Models` - 60 edges
3. `VotingEvent` - 59 edges
4. `WahlMirai.Web.Services` - 57 edges
5. `Voter` - 52 edges
6. `FakeAuditService` - 49 edges
7. `Candidate` - 41 edges
8. `LoginAttemptStore` - 36 edges
9. `CensusWhitelist` - 31 edges
10. `CandidateReviewDetailDto` - 31 edges

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

## Communities (162 total, 92 thin omitted)

### Community 2 - "ProfileService"
Cohesion: 0.09
Nodes (15): ChangePasswordRequest, ProfileController, UpdateEmailRequest, VerifyPasswordRequest, ILoginAttemptStore, IProfileService, ProfileService, ProfileViewModel (+7 more)

### Community 3 - "CandidateReviewDetailDto"
Cohesion: 0.09
Nodes (23): CandidateReviewDetailDto, AllowCorrection, ApprovedWithExceptions, CandidateEmail, CandidateId, CandidateName, EnrolledAt, EventId (+15 more)

### Community 5 - "CensusWhitelist"
Cohesion: 0.12
Nodes (14): CensusWhitelist, ClaimedAt, ClaimedByUserId, ClaimedByVoter, CreatedAt, DocumentHash, EncryptedDocument, ExcluirDePromocion (+6 more)

### Community 6 - ".CreateService"
Cohesion: 0.21
Nodes (7): AdminAccountServiceTests, FakeEncryptionService, Role, Description, Id, Name, Voters

### Community 7 - "Candidate"
Cohesion: 0.08
Nodes (23): Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt, ExceptionsDetail, GovernmentPlanUrl (+15 more)

### Community 9 - "PqrTicket"
Cohesion: 0.09
Nodes (19): PqrController, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt, RespondedBy (+11 more)

### Community 10 - "PromotionResult"
Cohesion: 0.22
Nodes (9): PromotionResult, ActivePromotedCount, ErrorCount, GraduatedCount, Message, PromotedCount, RetainedCount, Success (+1 more)

### Community 11 - "EmailReportViewModel"
Cohesion: 0.15
Nodes (12): EmailReportViewModel, EmailTypeFilter, EndDate, FailedEmails, Items, PendingEmails, Search, SentEmails (+4 more)

### Community 12 - "VotingEvent"
Cohesion: 0.06
Nodes (29): VotingEvent, Candidates, CreatedAt, CreatedByVoter, CreatedByVoterId, DeletedAt, Description, ElectionType (+21 more)

### Community 14 - "AdminAccountListItem"
Cohesion: 0.12
Nodes (15): AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id, PositionTitle, RegisteredAt (+7 more)

### Community 15 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (23): WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals, Candidates, CensusWhitelists, ElectionPositions (+15 more)

### Community 16 - "ActiveSessionStore"
Cohesion: 0.19
Nodes (8): ActiveSessionStoreTests, ActiveSessionStore, SessionEntry, IActiveSessionStore, SessionInvalidationReason, AccountDisabledOrDeleted, LoggedOut, ReplacedByNewDevice

### Community 17 - "Voter"
Cohesion: 0.08
Nodes (23): Voter, AuditLogs, Candidates, ContactEmail, DeletedAt, DocumentHash, EmailQueues, EncryptedDocument (+15 more)

### Community 19 - "EmailSettings"
Cohesion: 0.15
Nodes (9): EmailSettings, RateLimitPerMinute, SenderDisplayName, SenderEmail, SenderPassword, SmtpHost, SmtpPort, UseStartTls (+1 more)

### Community 20 - "LoginAttemptStore"
Cohesion: 0.11
Nodes (13): AuthServiceTests, FakeTimeProvider, LoginAttemptStoreTests, AuthService, LoginAttemptPolicy, LockoutThreshold, WindowDuration, LoginAttemptResult (+5 more)

### Community 23 - "WahlMirai.Tests"
Cohesion: 0.18
Nodes (9): coverlet.collector (6.0.4), Microsoft.EntityFrameworkCore.InMemory (9.0.7), Microsoft.EntityFrameworkCore.Relational (9.0.7), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, WahlMirai.Tests (+1 more)

### Community 24 - ".OnModelCreating"
Cohesion: 0.16
Nodes (8): VwPendingEmailQueue, Attempts, ContactEmail, CreatedAt, EmailType, FullName, Id, VoterId

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
Cohesion: 0.28
Nodes (13): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), filteredTickets(), formatDate(), getCsrfToken(), initials() (+5 more)

### Community 30 - "PromotionPreview"
Cohesion: 0.11
Nodes (17): PromotionPreview, ActiveVotersCount, CurrentYear, EligibleCount, ExcludedCount, HasRunThisYear, PendingWhitelistCount, PreviewList (+9 more)

### Community 31 - "VoterDetailDto"
Cohesion: 0.14
Nodes (14): VoterDetailDto, ContactEmail, DeletedAt, Document, ExcluirDePromocion, FullName, GradeId, GradeName (+6 more)

### Community 33 - "AuthController"
Cohesion: 0.11
Nodes (8): AuthController, IWhitelistService, ChangePasswordViewModel, ConfirmPassword, NewPassword, LoginViewModel, Document, Password

### Community 34 - "CandidacyDocument"
Cohesion: 0.22
Nodes (8): CandidacyDocument, Candidate, CandidateId, FileUrl, Id, Requirement, RequirementId, UploadedAt

### Community 35 - "WhitelistDetailDto"
Cohesion: 0.15
Nodes (12): WhitelistDetailDto, ClaimedAt, ClaimedByUserId, CreatedAt, Document, DocumentHash, ExcluirDePromocion, FullName (+4 more)

### Community 36 - "MyPostulationDto"
Cohesion: 0.11
Nodes (18): MyPostulationDto, AllowCorrection, ApprovedWithExceptions, CandidateId, EnrolledAt, EventId, EventTitle, ExceptionsDetail (+10 more)

### Community 37 - "PagedResult"
Cohesion: 0.15
Nodes (10): PagedResult, EndItemIndex, HasNextPage, HasPreviousPage, Items, PageNumber, PageSize, StartItemIndex (+2 more)

### Community 38 - "FakeAuditService"
Cohesion: 0.18
Nodes (14): FakeAuditService, Actions, Details, IpAddresses, FakeLoginAttemptStore, FailureCallCount, SuccessCallCount, FakeCredentialService (+6 more)

### Community 39 - "EmailQueue"
Cohesion: 0.18
Nodes (10): EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt, Status (+2 more)

### Community 40 - "VwPendingCandidacy"
Cohesion: 0.17
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
Nodes (9): VwPendingWhitelist, CreatedAt, DocumentHash, EncryptedDocument, ExcluirDePromocion, FullName, Grade, GradeId (+1 more)

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

### Community 53 - "CsvImportResult"
Cohesion: 0.19
Nodes (10): CsvImportResult, DuplicateCount, ErrorCount, Errors, InsertedCount, ProcessedCount, CsvRowError, Identifier (+2 more)

### Community 54 - "TestWhitelistService"
Cohesion: 0.10
Nodes (15): AuthControllerRegistrationTests, TestTempDataProvider, TestWhitelistService, LookupCount, LookupResult, RegistrationCount, RegistrationException, RegistroStep2ViewModel (+7 more)

### Community 55 - "PostulationFormDto"
Cohesion: 0.25
Nodes (7): PostulationFormDto, EventId, EventTitle, PositionDescription, PositionName, RegistrationEndDateTime, Requirements

### Community 56 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 57 - "WhitelistStatsDto"
Cohesion: 0.40
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 59 - "AdminAccountService"
Cohesion: 0.28
Nodes (4): AdminAccountService, IAuthService, IDocumentEncryptionService, WhitelistService

### Community 61 - "Census Whitelist"
Cohesion: 0.67
Nodes (4): Annual Grade Promotion, Atomic Self-Registration, Census Whitelist, Census Whitelist Self-Registration

### Community 62 - "ASP.NET Core MVC Architecture"
Cohesion: 0.67
Nodes (3): Active Session Store, ASP.NET Core MVC Architecture, MySQL Electoral Data Model

### Community 98 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 99 - "IEventService"
Cohesion: 0.21
Nodes (6): IEventService, VoterSearchResultDto, Documento, FullName, GradeName, Id

### Community 100 - "CandidateProposal"
Cohesion: 0.33
Nodes (6): CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id

### Community 128 - "RequirementCheckDto"
Cohesion: 0.25
Nodes (7): RequirementCheckDto, Description, FileUrl, IsMandatory, IsUploaded, RequirementId, UploadedAt

### Community 129 - "wahl_mirai_db_v2_9_completo.sql"
Cohesion: 0.23
Nodes (22): `academic_years`, `audit_log`, `candidacy_documents`, `candidate_proposals`, `candidates`, `census_whitelist`, `election_positions`, `email_queue` (+14 more)

### Community 131 - "ErrorViewModel"
Cohesion: 0.16
Nodes (7): ErrorController, ErrorViewModel, ExceptionMessage, Message, ShowHelpButton, ShowHomeButton, Title

### Community 133 - "AuditLog"
Cohesion: 0.12
Nodes (14): AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue, OccurredAt (+6 more)

### Community 134 - "RequirementInputDto"
Cohesion: 0.50
Nodes (4): RequirementInputDto, Description, Id, IsMandatory

### Community 137 - "AcademicYear"
Cohesion: 0.29
Nodes (6): AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year

### Community 139 - "AdminAccountFormViewModel"
Cohesion: 0.25
Nodes (8): AdminAccountEditViewModel, Id, AdminAccountFormViewModel, ContactEmail, Document, FullName, PositionTitle, RoleName

### Community 143 - "PositionRequirement"
Cohesion: 0.22
Nodes (8): PositionRequirement, CandidacyDocuments, Description, DisplayOrder, Id, IsMandatory, Position, PositionId

### Community 144 - "TestContext"
Cohesion: 0.18
Nodes (5): TestContext, EmailSender, PasswordStore, Worker, TestWahlMiraiDbContext

### Community 145 - "EmailType"
Cohesion: 0.25
Nodes (7): EmailType, CAMBIO_PERFIL, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA, REASIGNACION_ADMIN, RECUPERACION_ACCESO, RESPUESTA_PQR

### Community 146 - "EmailQueueItemDto"
Cohesion: 0.18
Nodes (11): EmailQueueItemDto, Attempts, CreatedAt, EmailType, ErrorMessage, Id, RecipientEmail, SentAt (+3 more)

### Community 147 - ".CreateTestContextAsync"
Cohesion: 0.25
Nodes (3): EmailQueueBackgroundServiceTests, TestEmailSender, SecretAvailableAtSend

### Community 152 - "EventGrade"
Cohesion: 0.33
Nodes (6): EventGrade, Grade, GradeId, Id, VotingEvent, VotingEventId

### Community 153 - "Serena memory maintenance"
Cohesion: 0.40
Nodes (4): Discovery model, Maintenance, Serena memory maintenance, Style and scope

### Community 157 - "WahlMirai.Web.ViewModels"
Cohesion: 0.14
Nodes (7): WahlMirai.Web.ViewModels, WahlMirai.Web, AdminAccountPositionTitles, RecuperarAccesoViewModel, Documento, RegistroStep1ViewModel, Document

### Community 159 - "IAuditService"
Cohesion: 0.39
Nodes (3): IAuditService, IPromotionService, PromotionService

## Knowledge Gaps
- **537 isolated node(s):** `LookupCount`, `RegistrationCount`, `RegistrationException`, `LookupResult`, `Actions` (+532 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 782 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **92 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `.CreateCensusService`, `ProfileService`, `AdminEventsController`, `.Recuperar`, `.CreateService`, `AuditLog`, `Candidate`, `PqrTicket`, `AcademicYear`, `CensusWhitelist`, `VotingEvent`, `.RetryEmail`, `.LogAsync`, `PositionRequirement`, `TestContext`, `Voter`, `AdminCensusController`, `.CreateTestContextAsync`, `LoginAttemptStore`, `CredentialService`, `EmailQueueBackgroundService`, `.HasVotedAsync`, `EventGrade`, `.OnModelCreating`, `EventService`, `Grade`, `IAuditService`, `CandidacyDocument`, `FakeAuditService`, `EmailQueue`, `VwPendingCandidacy`, `CandidacyService`, `VotingService`, `ElectionPosition`, `VwActiveCensu`, `VwPendingWhitelist`, `Vote`, `VwVoteCount`, `VoterEventParticipation`, `AdminAccountService`, `AdminCandidatesController`, `CandidateProposal`, `CandidateReviewService`?**
  _High betweenness centrality (0.220) - this node is a cross-community bridge._
- **Are the 6 inferred relationships involving `Voter` (e.g. with `.AddToWhitelistAsync_ThrowsIfDocumentAlreadyInUsers()` and `.GetPromotionPreviewAsync_ReflectsConsolidatedTotals()`) actually correct?**
  _`Voter` has 6 INFERRED edges - model-reasoned connections that need verification._
- **What connects `LookupCount`, `RegistrationCount`, `RegistrationException` to the rest of the system?**
  _537 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ProfileService` be split into smaller, more focused modules?**
  _Cohesion score 0.0898989898989899 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Services` to `WahlMirai.Web.Models`, `.IsPasswordCarryingType`, `ProfileService`, `FakeAuditService`, `AuthControllerRegistrationTests.cs`, `PendingPasswordStore`, `microsoft_entityframeworkcore`, `MailKitEmailSender.cs`, `ActiveSessionStore`, `EmailType`, `LoginAttemptStore`, `WahlMirai.Tests`, `AdminAccountService`, `WahlMirai.Web.ViewModels`?**
  _High betweenness centrality (0.126) - this node is a cross-community bridge._
- **Should `CandidateReviewDetailDto` be split into smaller, more focused modules?**
  _Cohesion score 0.08695652173913043 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Models` connect `WahlMirai.Web.Models` to `WahlMirai.Web.Services`, `CensusWhitelist`, `AuthControllerRegistrationTests.cs`, `VwPendingCandidacy`, `microsoft_entityframeworkcore`, `VwPendingWhitelist`, `EmailSettings`, `WahlMirai.Tests`, `MailKitEmailSender.cs`, `WahlMirai.Web.ViewModels`?**
  _High betweenness centrality (0.076) - this node is a cross-community bridge._