# Graph Report - WAHL-MIRAI  (2026-11-04)

## Corpus Check
- 212 files · ~221,965 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 34 file(s) not represented in the graph (top: .resx 14, (none) 8, .mmd 7)

## Summary
- 1966 nodes · 5254 edges · 171 communities (81 shown, 90 thin omitted)
- Extraction: 80% EXTRACTED · 20% INFERRED · 0% AMBIGUOUS · INFERRED: 1034 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `990c9004`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- AuthService
- ValidateAntiForgeryToken
- CandidateReviewDetailDto
- AdminEventsController
- CensusWhitelist
- AdminAccountServiceTests.CreateService
- Candidate
- AdminAccountListItem
- FakeLoginAttemptStore
- PromotionPreview
- EmailReportViewModel
- VotingEvent
- IAuditService.LogAsync
- PqrTicket
- WahlMiraiDbContext
- LoginAttemptStore
- Voter
- IActionResult
- CensusWhitelistTests.CreateCensusService
- Fact
- EventService
- FakeUrlHelper
- WahlMirai.Tests
- WahlMiraiDbContext.OnModelCreating
- Task
- ayuda-chatbot.js
- http
- DateTime
- pqr-manage.js
- 1. Diccionario de Tablas Físicas (17 Tablas)
- ICensusService
- ActiveSessionStore
- AuthController
- CandidacyDocument
- WhitelistDetailDto
- MyPostulationDto
- Vote
- CredentialService
- EmailQueue
- VwPendingCandidacy
- project.yml
- PqrController
- PositionRequirement
- VwActiveCensu
- VwPendingWhitelist
- DESIGN.md
- PasswordResetTokenStore
- 1. Arquitectura del Sistema
- VwVoteCount
- RegistrationOtpStore
- VoterEventParticipation
- check-i18n-coverage.ps1
- CsvImportResult
- FakeAuditService
- Manual Técnico — Diagramas del Sistema Wahl Mirai
- theme.js
- SpyingTokenStore
- WahlMirai.Web.Services
- VotingService
- Census Whitelist Self-Registration
- ProfileViewModel
- WahlMirai.Web.ViewModels
- WahlMirai.Web.Controllers
- ForwardedHeadersConfiguration.BuildOptions
- Development Environment
- Administrator Voting Workflow
- Controller
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
- Real-Time Scrutiny
- AcademicYear
- WahlMirai.Web
- RegistroStep2ViewModel
- 4. Requerimientos Específicos por Módulo
- InstitutionSettings
- AuthController.CambiarClave
- EmailQueueItemDto
- MIT License
- MIT License
- MIT License
- locale-core.js
- VerificarCodigo.cshtml
- memory_maintenance.md
- PagedResult
- RecuperacionAccesoControllerTests.CreateController
- AuditLog
- IPendingPasswordStore
- EmailRetryPolicyTests
- VoterPromotionDetail
- HttpGet
- WahlMirai.Web.Hubs
- WahlMirai.Web.ViewModels.RestablecerViewModel
- WhitelistStatsDto
- Índice de Documentos y Archivos
- RequirementInputDto
- LoginViewModel
- Directrices y Reglas del Proyecto WAHL-MIRAI
- EmailQueueBackgroundServiceTests.CreateTestContextAsync
- Política de Refresco de Graphify
- List
- EventGrade
- Serena memory maintenance
- EmailType
- Contacto.cshtml
- CandidateProposal
- Contacto.Body.cshtml
- Cookies.cshtml
- Cookies.Body.cshtml
- EventService.SearchVoterAsync
- Legal/Index.cshtml
- System.Text.Encodings.Web
- Privacidad.cshtml
- Privacidad.Body.cshtml
- Terminos.cshtml
- Terminos.Body.cshtml

## God Nodes (most connected - your core abstractions)
1. `WahlMiraiDbContext` - 94 edges
2. `FakeAuditService` - 81 edges
3. `WahlMirai.Web.Models` - 70 edges
4. `VotingEvent` - 66 edges
5. `WahlMirai.Web.Services` - 64 edges
6. `Voter` - 63 edges
7. `LoginAttemptStore` - 52 edges
8. `CensusWhitelist` - 47 edges
9. `FakeLoginAttemptStore` - 42 edges
10. `Candidate` - 41 edges

## Surprising Connections (you probably didn't know these)
- `On the horizon` --references--> `Voter`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Models/Voter.cs
- `2.4 Vista: `vw_pending_email_queue` (`VwPendingEmailQueue.cs`)` --references--> `EmailQueueBackgroundService`  [INFERRED]
  docs/diagramas/diccionario_de_datos_v3_1.md → WahlMirai.Web/Services/EmailQueueBackgroundService.cs
- `RF-M01-00 — Auto-registro de Electores Contra Lista Blanca` --references--> `IEmailSender`  [INFERRED]
  docs/ers_wahl_mirai_v3_1.md → WahlMirai.Web/Services/IEmailSender.cs
- `5.3 M03 — Gestión de Elecciones, Cargos y Etapas` --references--> `IEventService`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Services/IEventService.cs
- `5.5 M05 — Proceso de Votación y Control de Voto Único` --references--> `IVotingService`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Services/IVotingService.cs

## Import Cycles
- None detected.

## Communities (171 total, 90 thin omitted)

### Community 0 - "WahlMirai.Web.Models"
Cohesion: 0.14
Nodes (3): WahlMirai.Web.Models, LegalDocuments, Roles

### Community 3 - "CandidateReviewDetailDto"
Cohesion: 0.07
Nodes (31): CandidateReviewService, CandidateReviewDetailDto, AllowCorrection, ApprovedWithExceptions, CandidateEmail, CandidateId, CandidateName, EnrolledAt (+23 more)

### Community 4 - "AdminEventsController"
Cohesion: 0.17
Nodes (3): AdminEventsController, ICandidateReviewService, IEventService

### Community 5 - "CensusWhitelist"
Cohesion: 0.10
Nodes (16): CensusWhitelist, ClaimedAt, ClaimedByUserId, ClaimedByVoter, ContactEmail, CreatedAt, DocumentHash, EncryptedDocument (+8 more)

### Community 6 - "AdminAccountServiceTests.CreateService"
Cohesion: 0.23
Nodes (3): AdminAccountServiceTests, FakeAuthService, FakeEncryptionService

### Community 7 - "Candidate"
Cohesion: 0.08
Nodes (23): Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt, ExceptionsDetail, GovernmentPlanUrl (+15 more)

### Community 8 - "AdminAccountListItem"
Cohesion: 0.13
Nodes (15): AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id, PositionTitle, RegisteredAt (+7 more)

### Community 9 - "FakeLoginAttemptStore"
Cohesion: 0.19
Nodes (9): FakeLoginAttemptStore, FailureCallCount, SuccessCallCount, FakeEmailSender, Messages, ShouldFail, ShouldFailFirst, ProfileServiceTests (+1 more)

### Community 10 - "PromotionPreview"
Cohesion: 0.20
Nodes (10): PromotionPreview, ActiveVotersCount, CurrentYear, EligibleCount, ExcludedCount, HasRunThisYear, PendingWhitelistCount, PreviewList (+2 more)

### Community 11 - "EmailReportViewModel"
Cohesion: 0.15
Nodes (13): EmailReportViewModel, EmailTypeFilter, EndDate, FailedEmails, Items, PagedItems, PendingEmails, Search (+5 more)

### Community 12 - "VotingEvent"
Cohesion: 0.06
Nodes (29): VotingEvent, Candidates, CreatedAt, CreatedByVoter, CreatedByVoterId, DeletedAt, Description, ElectionType (+21 more)

### Community 13 - "IAuditService.LogAsync"
Cohesion: 0.15
Nodes (7): AdminAccountService, AuditService, IAuditService, IAuthService, CensusService, IDocumentEncryptionService, WhitelistService

### Community 14 - "PqrTicket"
Cohesion: 0.16
Nodes (14): PqrListTests, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt, RespondedBy (+6 more)

### Community 15 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (23): WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals, Candidates, CensusWhitelists, ElectionPositions (+15 more)

### Community 16 - "LoginAttemptStore"
Cohesion: 0.15
Nodes (11): FakeTimeProvider, LoginAttemptStoreTests, LoginAttemptPolicy, LockoutThreshold, WindowDuration, LoginAttemptResult, LoginAttemptEntry, FailedCount (+3 more)

### Community 17 - "Voter"
Cohesion: 0.09
Nodes (23): Voter, AuditLogs, Candidates, ContactEmail, DeletedAt, DocumentHash, EmailQueues, EncryptedDocument (+15 more)

### Community 23 - "WahlMirai.Tests"
Cohesion: 0.18
Nodes (9): coverlet.collector (6.0.4), Microsoft.EntityFrameworkCore.InMemory (9.0.7), Microsoft.EntityFrameworkCore.Relational (9.0.7), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, WahlMirai.Tests (+1 more)

### Community 24 - "WahlMiraiDbContext.OnModelCreating"
Cohesion: 0.21
Nodes (8): VwPendingEmailQueue, Attempts, ContactEmail, CreatedAt, EmailType, FullName, Id, VoterId

### Community 25 - "Task"
Cohesion: 0.10
Nodes (9): 5.7 M07 — Perfil de Usuario y Autogestión, RF-M07-01 — Consulta y Edición de Perfil Propio, FakeCredentialService, AdminAccountsController, IAdminAccountService, ICredentialService, IEmailSender, IProfileService (+1 more)

### Community 26 - "ayuda-chatbot.js"
Cohesion: 0.33
Nodes (16): addMessage(), bindEscalationButton(), bindEvents(), bindResolutionButtons(), disableResolutionBlock(), escalateToPqr(), escapeHtml(), findTopicByKeyword() (+8 more)

### Community 27 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 29 - "pqr-manage.js"
Cohesion: 0.28
Nodes (14): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), formatDate(), getCsrfToken(), initials(), openModal() (+6 more)

### Community 30 - "1. Diccionario de Tablas Físicas (17 Tablas)"
Cohesion: 0.07
Nodes (27): 1.10 Tabla: `event_grades`, 1.11 Tabla: `candidates`, 1.12 Tabla: `candidate_proposals`, 1.13 Tabla: `candidacy_documents`, 1.14 Tabla: `votes`, 1.15 Tabla: `event_participations`, 1.16 Tabla: `audit_log`, 1.17 Tabla: `pqr_tickets` (+19 more)

### Community 31 - "ICensusService"
Cohesion: 0.11
Nodes (15): ICensusService, VoterDetailDto, ContactEmail, DeletedAt, Document, ExcluirDePromocion, FullName, GradeId (+7 more)

### Community 32 - "ActiveSessionStore"
Cohesion: 0.20
Nodes (7): ActiveSessionStoreTests, ActiveSessionStore, SessionEntry, SessionInvalidationReason, AccountDisabledOrDeleted, LoggedOut, ReplacedByNewDevice

### Community 33 - "AuthController"
Cohesion: 0.18
Nodes (5): AuthController, VerificarCodigoViewModel, MaskedEmail, MaskedFullName, OtpCode

### Community 34 - "CandidacyDocument"
Cohesion: 0.25
Nodes (8): CandidacyDocument, Candidate, CandidateId, FileUrl, Id, Requirement, RequirementId, UploadedAt

### Community 35 - "WhitelistDetailDto"
Cohesion: 0.12
Nodes (13): WhitelistDetailDto, ClaimedAt, ClaimedByUserId, ContactEmail, CreatedAt, Document, DocumentHash, ExcluirDePromocion (+5 more)

### Community 36 - "MyPostulationDto"
Cohesion: 0.05
Nodes (39): CandidacyController, CandidacyService, ICandidacyService, MyPostulationDto, AllowCorrection, ApprovedWithExceptions, CandidateId, EnrolledAt (+31 more)

### Community 37 - "Vote"
Cohesion: 0.25
Nodes (8): Vote, Candidate, CandidateId, Id, VotedAt, VoteHash, VotingEvent, VotingEventId

### Community 38 - "CredentialService"
Cohesion: 0.20
Nodes (8): CredentialServiceTests, FailingWahlMiraiDbContext, FailOnSave, FakeActiveSessionStore, LastInvalidatedReason, LastInvalidatedUserId, CredentialService, PendingPasswordStore

### Community 39 - "EmailQueue"
Cohesion: 0.21
Nodes (11): AdminEmailReportListTests, EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt (+3 more)

### Community 40 - "VwPendingCandidacy"
Cohesion: 0.20
Nodes (10): VwPendingCandidacy, CandidateId, CandidateName, EnrolledAt, EventTitle, MandatoryDocumentsUploaded, MandatoryRequirements, PositionId (+2 more)

### Community 41 - "project.yml"
Cohesion: 0.08
Nodes (25): activation_command, activation_command_timeout, added_modes, agent_interface, default_modes, encoding, excluded_apis, excluded_tools (+17 more)

### Community 42 - "PqrController"
Cohesion: 0.14
Nodes (11): Comprobación permanente de cobertura i18n y Localización de Alertas/AJAX, Estándar de Localización de Alertas, Modales y Respuestas Asíncronas (AJAX), Herramientas auxiliares y entorno local, Navegación por contexto, Rendimiento del módulo PQR, PqrController, PqrCreateDto, Message (+3 more)

### Community 43 - "PositionRequirement"
Cohesion: 0.10
Nodes (21): ElectionPosition, CreatedAt, Description, Id, Name, PositionRequirements, Status, VotingEvents (+13 more)

### Community 44 - "VwActiveCensu"
Cohesion: 0.20
Nodes (9): VwActiveCensu, ContactEmail, ExcluirDePromocion, FullName, Grade, Id, RegisteredAt, Status (+1 more)

### Community 45 - "VwPendingWhitelist"
Cohesion: 0.20
Nodes (10): VwPendingWhitelist, ContactEmail, CreatedAt, DocumentHash, EncryptedDocument, ExcluirDePromocion, FullName, Grade (+2 more)

### Community 46 - "DESIGN.md"
Cohesion: 0.22
Nodes (8): Brand & Style, Colors, Components, Elevation & Depth, Graphify (knowledge graph para agentes de IA), Layout & Spacing, Shapes, Typography

### Community 47 - "PasswordResetTokenStore"
Cohesion: 0.37
Nodes (3): PasswordResetTokenStoreTests, PasswordResetTokenStore, TokenEntry

### Community 48 - "1. Arquitectura del Sistema"
Cohesion: 0.10
Nodes (25): 1. Arquitectura del Sistema, 2. Estructura de Directorios del Proyecto, 4.1 Auto-registro del Elector con Verificación OTP de 3 Pasos (RF-M01-00), 4.2 Autopostulación y Revisión de Candidatura (RF-M04-01, RF-M04-02), 4.3 Proceso de Votación y Escrutinio en Tiempo Real, 4. Diagramas de Flujo de Procesos Clave, 5.11 Control Dinámico de Etapas Electorales (RN-12), 5.1 M01 — Gestión de Acceso, Auto-registro y Sesión (+17 more)

### Community 49 - "VwVoteCount"
Cohesion: 0.22
Nodes (9): VwVoteCount, CandidateId, CandidateName, EventId, EventStatus, EventTitle, IsBlankVote, TotalVotes (+1 more)

### Community 50 - "RegistrationOtpStore"
Cohesion: 0.12
Nodes (7): RegistrationOtpStoreTests, IRegistrationOtpStore, RegistrationOtpStore, RegistrationTicketResult, ResendOtpResult, TicketEntry, WhitelistCooldownInfo

### Community 51 - "VoterEventParticipation"
Cohesion: 0.20
Nodes (10): 3.1 Mapeo de Entidades C# vs Tablas MySQL, 3.2 Vistas MySQL Mapeadas en Entity Framework Core, 3. Modelo de Datos (17 Tablas Físicas y 5 Vistas MySQL), VoterEventParticipation, Id, ParticipatedAt, Voter, VoterId (+2 more)

### Community 52 - "check-i18n-coverage.ps1"
Cohesion: 0.21
Nodes (5): Add-ExtractedString(), Get-FilterCategory(), Test-UserFacingString(), Get-DictionaryKeys(), Get-MissingTranslations()

### Community 53 - "CsvImportResult"
Cohesion: 0.20
Nodes (10): CsvImportResult, DuplicateCount, ErrorCount, Errors, InsertedCount, ProcessedCount, CsvRowError, Identifier (+2 more)

### Community 54 - "FakeAuditService"
Cohesion: 0.10
Nodes (25): AuthControllerRegistrationTests, FakeEmailSender, SentMessages, ShouldFail, FakeUrlHelperFactory, TestWhitelistService, LookupCount, LookupResult (+17 more)

### Community 55 - "Manual Técnico — Diagramas del Sistema Wahl Mirai"
Cohesion: 0.18
Nodes (10): 2. Figura 2: Diagrama de Casos de Uso por Rol (Sección 2.3), 4.1 Secuencia de Auto-registro con OTP en 3 Pasos (RF-M01-00), 4.2 Secuencia de Autopostulación y Revisión de Candidaturas (RF-M04-01, RF-M04-02), 4.3 Secuencia de Votación y Escrutinio en Tiempo Real (RF-M05-00, RF-M06-00), 4. Figura 4: Diagramas de Secuencia de Procesos Clave (Sección 2.3), 5. Figura 5: Diagrama Entidad-Relación (DER) (Sección 3.1), 6. Sección 3.2: Diccionario de Datos, Manual Técnico — Diagramas del Sistema Wahl Mirai (+2 more)

### Community 56 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 57 - "SpyingTokenStore"
Cohesion: 0.18
Nodes (3): SpyingTokenStore, LastIssuedToken, LastRevokedUserId

### Community 58 - "WahlMirai.Web.Services"
Cohesion: 0.14
Nodes (3): WahlMirai.Web.Services, WahlMirai.Tests, WahlMirai.Tests.Common

### Community 59 - "VotingService"
Cohesion: 0.13
Nodes (5): ElectorController, ResultsController, ResultsHub, IVotingService, VotingService

### Community 62 - "ProfileViewModel"
Cohesion: 0.22
Nodes (8): ProfileViewModel, ContactEmail, DocumentDisplay, FullName, GradeName, PositionTitle, Role, Status

### Community 63 - "WahlMirai.Web.ViewModels"
Cohesion: 0.08
Nodes (19): WahlMirai.Web.ViewModels, WahlMirai.Web, AdminAccountEditViewModel, Id, AdminAccountFormViewModel, ContactEmail, Document, FullName (+11 more)

### Community 65 - "WahlMirai.Web.Controllers"
Cohesion: 0.18
Nodes (4): WahlMirai.Web.Controllers, ChangePasswordRequest, UpdateEmailRequest, VerifyPasswordRequest

### Community 66 - "ForwardedHeadersConfiguration.BuildOptions"
Cohesion: 0.08
Nodes (7): WahlMirai.Web.Middleware, FakeAuthenticationService, TestTempDataProvider, ForwardedHeadersConfigurationTests, ForcePasswordChangeMiddleware, ForcePasswordChangeMiddlewareExtensions, ForwardedHeadersConfiguration

### Community 69 - "Controller"
Cohesion: 0.09
Nodes (11): ErrorController, HomeController, MediaController, DocumentEncryptionService, ErrorViewModel, ExceptionMessage, Message, ShowHelpButton (+3 more)

### Community 97 - "AcademicYear"
Cohesion: 0.11
Nodes (18): 5.2 M02 — Censo Electoral y Lista Blanca (Exclusivo Administrador), AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year, IPromotionService (+10 more)

### Community 98 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 99 - "RegistroStep2ViewModel"
Cohesion: 0.22
Nodes (9): RegistroStep2ViewModel, AcceptTermsAndPrivacy, ConfirmPassword, ContactEmail, Document, FullName, GradeName, Password (+1 more)

### Community 100 - "4. Requerimientos Específicos por Módulo"
Cohesion: 0.05
Nodes (42): 1.1 Propósito, 1.2 Alcance del Sistema, 1.3 Definiciones, Acrónimos y Abreviaturas, 1.4 Referencias, 1. Introducción, 2.1 Perspectiva del Producto, 2.2 Funciones Principales del Sistema, 2. Descripción General del Sistema (+34 more)

### Community 101 - "InstitutionSettings"
Cohesion: 0.11
Nodes (15): InstitutionSettings, Address, City, ContactEmail, DataProtectionEmail, HostingProvider, HostingRegion, Name (+7 more)

### Community 102 - "AuthController.CambiarClave"
Cohesion: 0.50
Nodes (3): ChangePasswordViewModel, ConfirmPassword, NewPassword

### Community 104 - "EmailQueueItemDto"
Cohesion: 0.18
Nodes (11): EmailQueueItemDto, Attempts, CreatedAt, EmailType, ErrorMessage, Id, RecipientEmail, SentAt (+3 more)

### Community 128 - "locale-core.js"
Cohesion: 0.29
Nodes (13): applyLocalizedThemeLabels(), computeTranslation(), flushPendingNodes(), getCulture(), getLanguageData(), getTranslation(), initSelects(), processAddedSubtree() (+5 more)

### Community 131 - "PagedResult"
Cohesion: 0.18
Nodes (10): PagedResult, EndItemIndex, HasNextPage, HasPreviousPage, Items, PageNumber, PageSize, StartItemIndex (+2 more)

### Community 132 - "RecuperacionAccesoControllerTests.CreateController"
Cohesion: 0.23
Nodes (8): RecuperacionAccesoControllerTests, RecuperarAccesoViewModel, Documento, RestablecerViewModel, ConfirmPassword, IsInvalidLink, Password, Token

### Community 133 - "AuditLog"
Cohesion: 0.15
Nodes (13): AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue, OccurredAt (+5 more)

### Community 136 - "VoterPromotionDetail"
Cohesion: 0.29
Nodes (7): VoterPromotionDetail, CurrentGrade, FullName, Id, Outcome, SourceType, TargetGrade

### Community 140 - "WhitelistStatsDto"
Cohesion: 0.40
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 141 - "Índice de Documentos y Archivos"
Cohesion: 0.40
Nodes (4): 1. Documentos Consolidados, 2. Archivos Mermaid Independientes (`.mmd`), Diagramas del Sistema Wahl Mirai (Versión 3.1), Índice de Documentos y Archivos

### Community 142 - "RequirementInputDto"
Cohesion: 0.50
Nodes (4): RequirementInputDto, Description, Id, IsMandatory

### Community 143 - "LoginViewModel"
Cohesion: 0.67
Nodes (3): LoginViewModel, Document, Password

### Community 144 - "Directrices y Reglas del Proyecto WAHL-MIRAI"
Cohesion: 0.50
Nodes (3): Directrices y Reglas del Proyecto WAHL-MIRAI, Política de Actualización del Grafo de Conocimiento (Graphify), Reglas de aplicación:

### Community 147 - "EmailQueueBackgroundServiceTests.CreateTestContextAsync"
Cohesion: 0.07
Nodes (20): EmailQueueBackgroundServiceTests, TestContext, EmailSender, PasswordStore, Worker, TestEmailSender, SecretAvailableAtSend, SentBodies (+12 more)

### Community 150 - "List"
Cohesion: 0.28
Nodes (3): CsvRowData, Fields, RowNumber

### Community 152 - "EventGrade"
Cohesion: 0.33
Nodes (6): EventGrade, Grade, GradeId, Id, VotingEvent, VotingEventId

### Community 153 - "Serena memory maintenance"
Cohesion: 0.40
Nodes (4): Discovery model, Maintenance, Serena memory maintenance, Style and scope

### Community 154 - "EmailType"
Cohesion: 0.15
Nodes (8): EmailRetryPolicy, EmailType, CAMBIO_PERFIL, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA, REASIGNACION_ADMIN, RECUPERACION_ACCESO, RESPUESTA_PQR

### Community 159 - "CandidateProposal"
Cohesion: 0.33
Nodes (6): CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id

### Community 164 - "EventService.SearchVoterAsync"
Cohesion: 0.22
Nodes (5): VoterSearchResultDto, Documento, FullName, GradeName, Id

## Knowledge Gaps
- **591 isolated node(s):** `ShouldFail`, `SentMessages`, `LookupCount`, `RegistrationCount`, `RegistrationException` (+586 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 850 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **90 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `AuthService`, `ValidateAntiForgeryToken`, `CandidateReviewDetailDto`, `RecuperacionAccesoControllerTests.CreateController`, `AdminEventsController`, `AdminAccountServiceTests.CreateService`, `AuditLog`, `Candidate`, `FakeLoginAttemptStore`, `CensusWhitelist`, `VotingEvent`, `IAuditService.LogAsync`, `PqrTicket`, `Voter`, `IActionResult`, `CensusWhitelistTests.CreateCensusService`, `EmailQueueBackgroundServiceTests.CreateTestContextAsync`, `EventService`, `EventGrade`, `WahlMiraiDbContext.OnModelCreating`, `DateTime`, `CandidateProposal`, `CandidacyDocument`, `MyPostulationDto`, `Vote`, `CredentialService`, `EmailQueue`, `VwPendingCandidacy`, `PqrController`, `PositionRequirement`, `VwActiveCensu`, `VwPendingWhitelist`, `1. Arquitectura del Sistema`, `VwVoteCount`, `VoterEventParticipation`, `FakeAuditService`, `VotingService`, `AcademicYear`?**
  _High betweenness centrality (0.094) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `WahlMiraiDbContext` (e.g. with `3.2 Vistas MySQL Mapeadas en Entity Framework Core` and `3. Figura 3: Diagrama de Componentes y Dependencias (Sección 2.3)`) actually correct?**
  _`WahlMiraiDbContext` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `ShouldFail`, `SentMessages`, `LookupCount` to the rest of the system?**
  _591 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `WahlMirai.Web.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.13513513513513514 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Models` connect `WahlMirai.Web.Models` to `WahlMirai.Web.Controllers`, `MyPostulationDto`, `InstitutionSettings`, `AdminAccountServiceTests.CreateService`, `AuthControllerRegistrationTests.cs`, `IAuditService.LogAsync`, `EmailQueueBackgroundServiceTests.CreateTestContextAsync`, `WahlMirai.Web.Services`, `WahlMirai.Web.ViewModels`?**
  _High betweenness centrality (0.054) - this node is a cross-community bridge._
- **Are the 73 inferred relationships involving `FakeAuditService` (e.g. with `AdminAccountServiceTests.CreateAdmin_StoresAccountAndInitialCredentialQueue()` and `AdminAccountServiceTests.CreateService()`) actually correct?**
  _`FakeAuditService` has 73 INFERRED edges - model-reasoned connections that need verification._
- **Should `CandidateReviewDetailDto` be split into smaller, more focused modules?**
  _Cohesion score 0.06666666666666667 - nodes in this community are weakly interconnected._