# Graph Report - WAHL-MIRAI  (2026-10-27)

## Corpus Check
- 208 files · ~209,705 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 27 file(s) not represented in the graph (top: .resx 14, (none) 8, .css 3)

## Summary
- 1895 nodes · 5048 edges · 167 communities (74 shown, 93 thin omitted)
- Extraction: 80% EXTRACTED · 20% INFERRED · 0% AMBIGUOUS · INFERRED: 996 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `e15c2ee4`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- AuthService
- ValidateAntiForgeryToken
- CandidateReviewDetailDto
- AdminEventsController
- CensusWhitelist
- .CreateService
- Candidate
- AdminAccountListItem
- Fact
- PromotionPreview
- EmailReportViewModel
- VotingEvent
- Task
- PqrTicket
- WahlMiraiDbContext
- LoginAttemptStore
- Voter
- IActionResult
- .CreateCensusService
- Fact
- EventService
- FakeUrlHelper
- WahlMirai.Tests
- .OnModelCreating
- CancellationToken
- ayuda-chatbot.js
- http
- Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.1 (Sincronizada con Implementación Real)
- pqr-manage.js
- IDocumentEncryptionService
- VoterDetailDto
- VerificarCodigoViewModel
- AuthController
- CandidacyDocument
- WhitelistDetailDto
- MyPostulationDto
- Vote
- PasswordResetTokenStore
- EmailQueue
- VwPendingCandidacy
- project.yml
- PqrController
- PositionRequirement
- VwActiveCensu
- VwPendingWhitelist
- DESIGN.md
- TestEmailSender
- VotingService
- VwVoteCount
- RegistrationOtpStore
- VoterEventParticipation
- check-i18n-coverage.ps1
- .ImportCsvAsync
- .CreateController
- .CreateTestContextAsync
- theme.js
- EmailSettings
- WahlMirai.Web.Services
- IVotingService
- Census Whitelist Self-Registration
- ProfileViewModel
- RegistroFailureReason
- WahlMirai.Web.ViewModels
- .BuildOptions
- Development Environment
- Administrator Voting Workflow
- .Resolve
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
- PromotionResult
- WahlMirai.Web
- RegistroStep2ViewModel
- 4. Requerimientos Específicos por Módulo
- InstitutionSettings
- AuthViewModels.cs
- PqrDtos.cs
- MIT License
- MIT License
- MIT License
- locale-core.js
- VerificarCodigo.cshtml
- memory_maintenance.md
- EmailQueueBackgroundService
- .CreateController
- AuditLog
- EmailRetryPolicyTests
- LegalController
- Program.cs
- WahlMirai.Web.ViewModels.RestablecerViewModel
- Directrices y Reglas del Proyecto WAHL-MIRAI
- TestContext
- Política de Refresco de Graphify
- RecuperacionAccesoController
- List
- EventGrade
- Serena memory maintenance
- EmailType
- Contacto.cshtml
- CandidateProposal
- Contacto.Body.cshtml
- Cookies.cshtml
- Cookies.Body.cshtml
- .SearchVoterAsync
- Legal/Index.cshtml
- System.Text.Encodings.Web
- Privacidad.cshtml
- Privacidad.Body.cshtml
- Terminos.cshtml
- Terminos.Body.cshtml

## God Nodes (most connected - your core abstractions)
1. `WahlMiraiDbContext` - 91 edges
2. `FakeAuditService` - 80 edges
3. `WahlMirai.Web.Models` - 69 edges
4. `VotingEvent` - 66 edges
5. `WahlMirai.Web.Services` - 64 edges
6. `Voter` - 61 edges
7. `LoginAttemptStore` - 52 edges
8. `CensusWhitelist` - 47 edges
9. `FakeLoginAttemptStore` - 42 edges
10. `Candidate` - 41 edges

## Surprising Connections (you probably didn't know these)
- `On the horizon` --references--> `Voter`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Models/Voter.cs
- `RF-M01-00 — Auto-registro de Electores Contra Lista Blanca` --references--> `IEmailSender`  [INFERRED]
  docs/ers_wahl_mirai_v3_1.md → WahlMirai.Web/Services/IEmailSender.cs
- `5.3 M03 — Gestión de Elecciones, Cargos y Etapas` --references--> `IEventService`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Services/IEventService.cs
- `5.5 M05 — Proceso de Votación y Control de Voto Único` --references--> `IVotingService`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Services/IVotingService.cs
- `1. Arquitectura del Sistema` --references--> `AdminAccountsController`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Controllers/AdminAccountsController.cs

## Import Cycles
- None detected.

## Communities (167 total, 93 thin omitted)

### Community 0 - "WahlMirai.Web.Models"
Cohesion: 0.14
Nodes (3): WahlMirai.Web.Models, LegalDocuments, Roles

### Community 2 - "ValidateAntiForgeryToken"
Cohesion: 0.21
Nodes (4): AdminAccountsController, ChangePasswordRequest, ProfileController, UpdateEmailRequest

### Community 3 - "CandidateReviewDetailDto"
Cohesion: 0.06
Nodes (33): 5.4 M04 — Autopostulación y Revisión de Candidaturas, CandidateReviewService, CandidateReviewDetailDto, AllowCorrection, ApprovedWithExceptions, CandidateEmail, CandidateId, CandidateName (+25 more)

### Community 4 - "AdminEventsController"
Cohesion: 0.13
Nodes (6): AdminEventsController, RequirementInputDto, Description, Id, IsMandatory, IEventService

### Community 5 - "CensusWhitelist"
Cohesion: 0.08
Nodes (17): ResultsHub, CensusWhitelist, ClaimedAt, ClaimedByUserId, ClaimedByVoter, ContactEmail, CreatedAt, DocumentHash (+9 more)

### Community 7 - "Candidate"
Cohesion: 0.09
Nodes (23): Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt, ExceptionsDetail, GovernmentPlanUrl (+15 more)

### Community 8 - "AdminAccountListItem"
Cohesion: 0.08
Nodes (24): AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id, PositionTitle, RegisteredAt (+16 more)

### Community 9 - "Fact"
Cohesion: 0.09
Nodes (29): 5.1 M01 — Gestión de Acceso, Auto-registro y Sesión, 5.7 M07 — Perfil de Usuario y Autogestión, 3. Reglas de Negocio Transversales, 4.7 M07 — Perfil de Usuario y Autogestión de Credenciales, RF-M07-01 — Consulta y Edición de Perfil Propio, RF-M07-02 — Reasignación de Contraseña por el Administrador, FakeEmailSender, SentMessages (+21 more)

### Community 10 - "PromotionPreview"
Cohesion: 0.12
Nodes (17): PromotionPreview, ActiveVotersCount, CurrentYear, EligibleCount, ExcludedCount, HasRunThisYear, PendingWhitelistCount, PreviewList (+9 more)

### Community 11 - "EmailReportViewModel"
Cohesion: 0.09
Nodes (23): EmailQueueItemDto, Attempts, CreatedAt, EmailType, ErrorMessage, Id, RecipientEmail, SentAt (+15 more)

### Community 12 - "VotingEvent"
Cohesion: 0.06
Nodes (29): VotingEvent, Candidates, CreatedAt, CreatedByVoter, CreatedByVoterId, DeletedAt, Description, ElectionType (+21 more)

### Community 13 - "Task"
Cohesion: 0.11
Nodes (8): AuditService, IAuditService, CensusService, ICensusService, WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 14 - "PqrTicket"
Cohesion: 0.16
Nodes (14): PqrListTests, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt, RespondedBy (+6 more)

### Community 15 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (23): WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals, Candidates, CensusWhitelists, ElectionPositions (+15 more)

### Community 16 - "LoginAttemptStore"
Cohesion: 0.16
Nodes (11): FakeTimeProvider, LoginAttemptStoreTests, LoginAttemptPolicy, LockoutThreshold, WindowDuration, LoginAttemptResult, LoginAttemptEntry, FailedCount (+3 more)

### Community 17 - "Voter"
Cohesion: 0.09
Nodes (23): Voter, AuditLogs, Candidates, ContactEmail, DeletedAt, DocumentHash, EmailQueues, EncryptedDocument (+15 more)

### Community 19 - ".CreateCensusService"
Cohesion: 0.17
Nodes (4): 5.2 M02 — Censo Electoral y Lista Blanca (Exclusivo Administrador), CensusWhitelistTests, IPromotionService, PromotionService

### Community 23 - "WahlMirai.Tests"
Cohesion: 0.18
Nodes (9): coverlet.collector (6.0.4), Microsoft.EntityFrameworkCore.InMemory (9.0.7), Microsoft.EntityFrameworkCore.Relational (9.0.7), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, WahlMirai.Tests (+1 more)

### Community 24 - ".OnModelCreating"
Cohesion: 0.13
Nodes (14): AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year, VwPendingEmailQueue, Attempts (+6 more)

### Community 26 - "ayuda-chatbot.js"
Cohesion: 0.33
Nodes (16): addMessage(), bindEscalationButton(), bindEvents(), bindResolutionButtons(), disableResolutionBlock(), escalateToPqr(), escapeHtml(), findTopicByKeyword() (+8 more)

### Community 27 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 28 - "Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.1 (Sincronizada con Implementación Real)"
Cohesion: 0.20
Nodes (8): 2. Estructura de Directorios del Proyecto, 4.1 Auto-registro del Elector con Verificación OTP de 3 Pasos (RF-M01-00), 4.2 Autopostulación y Revisión de Candidatura (RF-M04-01, RF-M04-02), 4.3 Proceso de Votación y Escrutinio en Tiempo Real, 4. Diagramas de Flujo de Procesos Clave, Arquitectura y Diseño de Software, Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.1 (Sincronizada con Implementación Real), AdminEmailReportController

### Community 29 - "pqr-manage.js"
Cohesion: 0.28
Nodes (14): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), formatDate(), getCsrfToken(), initials(), openModal() (+6 more)

### Community 30 - "IDocumentEncryptionService"
Cohesion: 0.14
Nodes (5): FakeAuthService, FakeEncryptionService, IAuthService, IDocumentEncryptionService, WhitelistService

### Community 31 - "VoterDetailDto"
Cohesion: 0.07
Nodes (24): VoterDetailDto, ContactEmail, DeletedAt, Document, ExcluirDePromocion, FullName, GradeId, GradeName (+16 more)

### Community 32 - "VerificarCodigoViewModel"
Cohesion: 0.20
Nodes (6): RecuperarAccesoViewModel, Documento, VerificarCodigoViewModel, MaskedEmail, MaskedFullName, OtpCode

### Community 34 - "CandidacyDocument"
Cohesion: 0.25
Nodes (8): CandidacyDocument, Candidate, CandidateId, FileUrl, Id, Requirement, RequirementId, UploadedAt

### Community 35 - "WhitelistDetailDto"
Cohesion: 0.15
Nodes (13): WhitelistDetailDto, ClaimedAt, ClaimedByUserId, ContactEmail, CreatedAt, Document, DocumentHash, ExcluirDePromocion (+5 more)

### Community 36 - "MyPostulationDto"
Cohesion: 0.05
Nodes (39): CandidacyController, CandidacyService, ICandidacyService, MyPostulationDto, AllowCorrection, ApprovedWithExceptions, CandidateId, EnrolledAt (+31 more)

### Community 37 - "Vote"
Cohesion: 0.25
Nodes (8): Vote, Candidate, CandidateId, Id, VotedAt, VoteHash, VotingEvent, VotingEventId

### Community 38 - "PasswordResetTokenStore"
Cohesion: 0.06
Nodes (27): 1. Arquitectura del Sistema, 5.10 Servicio de Cola de Correo Progresivo (RN-9), RF-M01-02 — Recuperación de Acceso y Restablecimiento por Token de un Solo Uso, ActiveSessionStoreTests, CredentialServiceTests, FailingWahlMiraiDbContext, FailOnSave, FakeActiveSessionStore (+19 more)

### Community 39 - "EmailQueue"
Cohesion: 0.20
Nodes (10): EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt, Status (+2 more)

### Community 40 - "VwPendingCandidacy"
Cohesion: 0.20
Nodes (10): VwPendingCandidacy, CandidateId, CandidateName, EnrolledAt, EventTitle, MandatoryDocumentsUploaded, MandatoryRequirements, PositionId (+2 more)

### Community 41 - "project.yml"
Cohesion: 0.08
Nodes (25): activation_command, activation_command_timeout, added_modes, agent_interface, default_modes, encoding, excluded_apis, excluded_tools (+17 more)

### Community 42 - "PqrController"
Cohesion: 0.22
Nodes (6): Comprobación permanente de cobertura i18n y Localización de Alertas/AJAX, Estándar de Localización de Alertas, Modales y Respuestas Asíncronas (AJAX), Herramientas auxiliares y entorno local, Navegación por contexto, Rendimiento del módulo PQR, PqrController

### Community 43 - "PositionRequirement"
Cohesion: 0.10
Nodes (21): ElectionPosition, CreatedAt, Description, Id, Name, PositionRequirements, Status, VotingEvents (+13 more)

### Community 44 - "VwActiveCensu"
Cohesion: 0.22
Nodes (9): VwActiveCensu, ContactEmail, ExcluirDePromocion, FullName, Grade, Id, RegisteredAt, Status (+1 more)

### Community 45 - "VwPendingWhitelist"
Cohesion: 0.20
Nodes (10): VwPendingWhitelist, ContactEmail, CreatedAt, DocumentHash, EncryptedDocument, ExcluirDePromocion, FullName, Grade (+2 more)

### Community 46 - "DESIGN.md"
Cohesion: 0.22
Nodes (8): Brand & Style, Colors, Components, Elevation & Depth, Graphify (knowledge graph para agentes de IA), Layout & Spacing, Shapes, Typography

### Community 47 - "TestEmailSender"
Cohesion: 0.33
Nodes (4): TestEmailSender, SecretAvailableAtSend, SentBodies, SentSubjects

### Community 48 - "VotingService"
Cohesion: 0.14
Nodes (9): 5.11 Control Dinámico de Etapas Electorales (RN-12), 5.3 M03 — Gestión de Elecciones, Cargos y Etapas, 5.5 M05 — Proceso de Votación y Control de Voto Único, 5.6 M06 — Escrutinio y Resultados en Tiempo Real, 5.8 M08 — Ayuda, Chatbot FAQ y PQR, 5.9 M09 — Cuentas Administrativas (Exclusivo Súper Administrador), 5. Matriz de Módulos, Controladores y Reglas de Negocio, On the horizon (+1 more)

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

### Community 53 - ".ImportCsvAsync"
Cohesion: 0.20
Nodes (10): CsvImportResult, DuplicateCount, ErrorCount, Errors, InsertedCount, ProcessedCount, CsvRowError, Identifier (+2 more)

### Community 54 - ".CreateController"
Cohesion: 0.12
Nodes (18): AuthControllerRegistrationTests, FakeUrlHelperFactory, TestWhitelistService, LookupCount, LookupResult, RegistrationCount, RegistrationException, Grade (+10 more)

### Community 56 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 57 - "EmailSettings"
Cohesion: 0.20
Nodes (9): EmailSettings, BaseUrl, RateLimitPerMinute, SenderDisplayName, SenderEmail, SenderPassword, SmtpHost, SmtpPort (+1 more)

### Community 58 - "WahlMirai.Web.Services"
Cohesion: 0.17
Nodes (3): WahlMirai.Web.Services, WahlMirai.Tests, WahlMirai.Tests.Common

### Community 59 - "IVotingService"
Cohesion: 0.26
Nodes (3): ElectorController, ResultsController, IVotingService

### Community 62 - "ProfileViewModel"
Cohesion: 0.25
Nodes (8): ProfileViewModel, ContactEmail, DocumentDisplay, FullName, GradeName, PositionTitle, Role, Status

### Community 63 - "RegistroFailureReason"
Cohesion: 0.25
Nodes (8): RegistroFailureReason, AlreadyClaimed, Cooldown, Locked, NoContactEmail, None, NotFound, SendFailed

### Community 65 - "WahlMirai.Web.ViewModels"
Cohesion: 0.21
Nodes (4): WahlMirai.Web.Controllers, WahlMirai.Web.ViewModels, WahlMirai.Web, VerifyPasswordRequest

### Community 66 - ".BuildOptions"
Cohesion: 0.08
Nodes (7): WahlMirai.Web.Middleware, FakeAuthenticationService, TestTempDataProvider, ForwardedHeadersConfigurationTests, ForcePasswordChangeMiddleware, ForcePasswordChangeMiddlewareExtensions, ForwardedHeadersConfiguration

### Community 69 - ".Resolve"
Cohesion: 0.09
Nodes (11): ErrorController, HomeController, MediaController, DocumentEncryptionService, ErrorViewModel, ExceptionMessage, Message, ShowHelpButton (+3 more)

### Community 97 - "PromotionResult"
Cohesion: 0.22
Nodes (9): PromotionResult, ActivePromotedCount, ErrorCount, GraduatedCount, Message, PromotedCount, RetainedCount, Success (+1 more)

### Community 98 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 99 - "RegistroStep2ViewModel"
Cohesion: 0.22
Nodes (9): RegistroStep2ViewModel, AcceptTermsAndPrivacy, ConfirmPassword, ContactEmail, Document, FullName, GradeName, Password (+1 more)

### Community 100 - "4. Requerimientos Específicos por Módulo"
Cohesion: 0.05
Nodes (40): 1.1 Propósito, 1.2 Alcance del Sistema, 1.3 Definiciones, Acrónimos y Abreviaturas, 1.4 Referencias, 1. Introducción, 2.1 Perspectiva del Producto, 2.2 Funciones Principales del Sistema, 2. Descripción General del Sistema (+32 more)

### Community 101 - "InstitutionSettings"
Cohesion: 0.11
Nodes (15): InstitutionSettings, Address, City, ContactEmail, DataProtectionEmail, HostingProvider, HostingRegion, Name (+7 more)

### Community 102 - "AuthViewModels.cs"
Cohesion: 0.29
Nodes (6): ChangePasswordViewModel, ConfirmPassword, NewPassword, LoginViewModel, Document, Password

### Community 104 - "PqrDtos.cs"
Cohesion: 0.33
Nodes (5): PqrCreateDto, Message, Subject, PqrResponseDto, AdminResponse

### Community 128 - "locale-core.js"
Cohesion: 0.29
Nodes (13): applyLocalizedThemeLabels(), computeTranslation(), flushPendingNodes(), getCulture(), getLanguageData(), getTranslation(), initSelects(), processAddedSubtree() (+5 more)

### Community 132 - ".CreateController"
Cohesion: 0.28
Nodes (6): RecuperacionAccesoControllerTests, RestablecerViewModel, ConfirmPassword, IsInvalidLink, Password, Token

### Community 133 - "AuditLog"
Cohesion: 0.15
Nodes (13): AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue, OccurredAt (+5 more)

### Community 144 - "Directrices y Reglas del Proyecto WAHL-MIRAI"
Cohesion: 0.50
Nodes (3): Directrices y Reglas del Proyecto WAHL-MIRAI, Política de Actualización del Grafo de Conocimiento (Graphify), Reglas de aplicación:

### Community 147 - "TestContext"
Cohesion: 0.18
Nodes (5): TestContext, EmailSender, PasswordStore, Worker, TestWahlMiraiDbContext

### Community 150 - "List"
Cohesion: 0.21
Nodes (3): CsvRowData, Fields, RowNumber

### Community 152 - "EventGrade"
Cohesion: 0.33
Nodes (6): EventGrade, Grade, GradeId, Id, VotingEvent, VotingEventId

### Community 153 - "Serena memory maintenance"
Cohesion: 0.40
Nodes (4): Discovery model, Maintenance, Serena memory maintenance, Style and scope

### Community 154 - "EmailType"
Cohesion: 0.19
Nodes (8): EmailRetryPolicy, EmailType, CAMBIO_PERFIL, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA, REASIGNACION_ADMIN, RECUPERACION_ACCESO, RESPUESTA_PQR

### Community 159 - "CandidateProposal"
Cohesion: 0.33
Nodes (6): CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id

### Community 164 - ".SearchVoterAsync"
Cohesion: 0.22
Nodes (5): VoterSearchResultDto, Documento, FullName, GradeName, Id

## Knowledge Gaps
- **590 isolated node(s):** `ShouldFail`, `SentMessages`, `LookupCount`, `RegistrationCount`, `RegistrationException` (+585 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 813 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **93 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `AuthService`, `ValidateAntiForgeryToken`, `CandidateReviewDetailDto`, `.CreateController`, `AdminEventsController`, `.CreateService`, `AuditLog`, `Candidate`, `Fact`, `CensusWhitelist`, `EmailQueueBackgroundService`, `VotingEvent`, `Task`, `PqrTicket`, `Voter`, `IActionResult`, `.CreateCensusService`, `TestContext`, `EventService`, `RecuperacionAccesoController`, `.OnModelCreating`, `EventGrade`, `CancellationToken`, `Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.1 (Sincronizada con Implementación Real)`, `IDocumentEncryptionService`, `CandidateProposal`, `CandidacyDocument`, `MyPostulationDto`, `Vote`, `PasswordResetTokenStore`, `EmailQueue`, `VwPendingCandidacy`, `PqrController`, `PositionRequirement`, `VwActiveCensu`, `VwPendingWhitelist`, `VotingService`, `VwVoteCount`, `VoterEventParticipation`, `.CreateController`, `.CreateTestContextAsync`, `IVotingService`?**
  _High betweenness centrality (0.092) - this node is a cross-community bridge._
- **Are the 72 inferred relationships involving `FakeAuditService` (e.g. with `.CreateAdmin_StoresAccountAndInitialCredentialQueue()` and `.CreateService()`) actually correct?**
  _`FakeAuditService` has 72 INFERRED edges - model-reasoned connections that need verification._
- **What connects `ShouldFail`, `SentMessages`, `LookupCount` to the rest of the system?**
  _590 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `WahlMirai.Web.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.1380952380952381 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Models` connect `WahlMirai.Web.Models` to `WahlMirai.Web.ViewModels`, `InstitutionSettings`, `Program.cs`, `AuthControllerRegistrationTests.cs`, `Program.cs`, `Task`, `EmailSettings`, `WahlMirai.Web.Services`?**
  _High betweenness centrality (0.051) - this node is a cross-community bridge._
- **Should `CandidateReviewDetailDto` be split into smaller, more focused modules?**
  _Cohesion score 0.058693244739756366 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Services` to `WahlMirai.Web.Models`, `WahlMirai.Web.ViewModels`, `PasswordResetTokenStore`, `Program.cs`, `AuthControllerRegistrationTests.cs`, `Fact`, `Program.cs`, `AdminAccountListItem`, `Task`, `EmailType`, `IDocumentEncryptionService`?**
  _High betweenness centrality (0.047) - this node is a cross-community bridge._