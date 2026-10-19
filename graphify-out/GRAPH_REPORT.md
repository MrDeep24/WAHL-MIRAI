# Graph Report - WAHL-MIRAI  (2026-10-18)

## Corpus Check
- 208 files · ~437,199 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 28 file(s) not represented in the graph (top: .resx 14, (none) 8, .css 3)

## Summary
- 2041 nodes · 4686 edges · 182 communities (83 shown, 99 thin omitted)
- Extraction: 82% EXTRACTED · 18% INFERRED · 0% AMBIGUOUS · INFERRED: 824 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `f7f5f33e`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- ILoginAttemptStore
- ProfileController
- CandidateReviewDetailDto
- AdminEventsController
- CensusWhitelist
- .CreateService
- Candidate
- AdminAccountsController
- FakeAuditService
- .CreateCensusService
- EmailReportViewModel
- VotingEvent
- Task
- PqrTicket
- WahlMiraiDbContext
- ActiveSessionStore
- Voter
- AdminCensusController
- wahl_mirai_db_v3_0_completo.sql
- LoginAttemptStore
- EventService
- FakeUrlHelper
- WahlMirai.Tests
- .OnModelCreating
- AdminAccountService
- ayuda-chatbot.js
- http
- Grade
- pqr-manage.js
- IAuthService
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
- .UseForcePasswordChange
- ElectionPosition
- VwActiveCensu
- VwPendingWhitelist
- DESIGN.md
- PostulationSubmitDto
- Vote
- VwVoteCount
- RegistrationOtpStore
- VoterEventParticipation
- check-i18n-coverage.ps1
- .LogAsync
- .CreateController
- PostulationFormDto
- theme.js
- PostulableEventDto
- microsoft_entityframeworkcore
- VotingService
- Census Whitelist Self-Registration
- ESPECIFICACIÓN DE REQUERIMIENTOS DE SOFTWARE
- WahlMirai.Web.ViewModels
- .BuildOptions
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
- ProfileViewModel
- Real-Time Scrutiny
- CandidateReviewService
- WahlMirai.Web
- RegistroStep2ViewModel
- 5. Matriz de Módulos, Controladores y Reglas de Negocio
- InstitutionSettings
- 4. Requerimientos Específicos por Módulo
- MIT License
- MIT License
- MIT License
- locale-core.js
- VerificarCodigo.cshtml
- memory_maintenance.md
- AdminCandidatesController
- .CreateController
- AuditLog
- RequirementInputDto
- .From
- ICandidacyService
- LegalController
- WahlMirai.Web.Services
- WahlMirai.Web.ViewModels.RestablecerViewModel
- Program.cs
- LegalViewModel
- 4.3 M03 — Gestión de Elecciones, Cargos y Etapas
- PositionRequirement
- Directrices y Reglas del Proyecto WAHL-MIRAI
- EmailQueueItemDto
- AdminAccountListItem
- .CreateTestContextAsync
- Política de Refresco de Graphify
- Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.0 (Sincronizada con Implementación Real)
- RequirementCheckDto
- IEventService
- EventGrade
- Serena memory maintenance
- EmailType
- HomeController
- HttpContext
- Contacto.cshtml
- EmailRetryPolicyTests
- CandidateProposal
- Contacto.Body.cshtml
- Cookies.cshtml
- Cookies.Body.cshtml
- 4.8 M08 — Ayuda, Tutorial, PQR y Chatbot
- .SearchVoterAsync
- Legal/Index.cshtml
- System.Text.Encodings.Web
- WhitelistStatsDto
- Privacidad.cshtml
- Privacidad.Body.cshtml
- Terminos.cshtml
- Terminos.Body.cshtml

## God Nodes (most connected - your core abstractions)
1. `WahlMiraiDbContext` - 91 edges
2. `FakeAuditService` - 73 edges
3. `WahlMirai.Web.Models` - 69 edges
4. `VotingEvent` - 65 edges
5. `WahlMirai.Web.Services` - 64 edges
6. `Voter` - 61 edges
7. `LoginAttemptStore` - 44 edges
8. `Candidate` - 41 edges
9. `FakeLoginAttemptStore` - 40 edges
10. `CensusWhitelist` - 39 edges

## Surprising Connections (you probably didn't know these)
- `On the horizon` --references--> `Voter`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_0.md → WahlMirai.Web/Models/Voter.cs
- `5.5 M05 — Proceso de Votación y Control de Voto Único` --references--> `IVotingService`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_0.md → WahlMirai.Web/Services/IVotingService.cs
- `1. Arquitectura del Sistema` --references--> `AdminAccountsController`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_0.md → WahlMirai.Web/Controllers/AdminAccountsController.cs
- `Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.0 (Sincronizada con Implementación Real)` --references--> `AdminAccountsController`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_0.md → WahlMirai.Web/Controllers/AdminAccountsController.cs
- `Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.0 (Sincronizada con Implementación Real)` --references--> `AdminCandidatesController`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_0.md → WahlMirai.Web/Controllers/AdminCandidatesController.cs

## Import Cycles
- None detected.

## Communities (182 total, 99 thin omitted)

### Community 0 - "WahlMirai.Web.Models"
Cohesion: 0.16
Nodes (3): WahlMirai.Web.Models, WahlMirai.Web, Roles

### Community 1 - "ILoginAttemptStore"
Cohesion: 0.18
Nodes (8): 5.1 M01 — Gestión de Acceso, Auto-registro y Sesión, 4.1 M01 — Gestión de Acceso, Auto-registro y Sesión, RF-M01-00 — Auto-registro de Electores Contra Lista Blanca, RF-M01-01 — Autenticación de Usuarios por Identificador Único, RF-M01-02 — Recuperación de Acceso y Restablecimiento por Token de un Solo Uso, IEmailSender, ILoginAttemptStore, ProfileService

### Community 2 - "ProfileController"
Cohesion: 0.17
Nodes (8): 5.7 M07 — Perfil de Usuario y Autogestión, 3. Reglas de Negocio Transversales, ChangePasswordRequest, ProfileController, UpdateEmailRequest, VerifyPasswordRequest, ClientIpKey, IProfileService

### Community 3 - "CandidateReviewDetailDto"
Cohesion: 0.09
Nodes (23): CandidateReviewDetailDto, AllowCorrection, ApprovedWithExceptions, CandidateEmail, CandidateId, CandidateName, EnrolledAt, EventId (+15 more)

### Community 5 - "CensusWhitelist"
Cohesion: 0.12
Nodes (15): CensusWhitelist, ClaimedAt, ClaimedByUserId, ClaimedByVoter, ContactEmail, CreatedAt, DocumentHash, EncryptedDocument (+7 more)

### Community 6 - ".CreateService"
Cohesion: 0.15
Nodes (8): AdminAccountServiceTests, FakeAuthService, FakeEncryptionService, Role, Description, Id, Name, Voters

### Community 7 - "Candidate"
Cohesion: 0.08
Nodes (23): Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt, ExceptionsDetail, GovernmentPlanUrl (+15 more)

### Community 8 - "AdminAccountsController"
Cohesion: 0.13
Nodes (15): AdminAccountsController, AdminAccountEditViewModel, Id, AdminAccountFormViewModel, ContactEmail, Document, FullName, PositionTitle (+7 more)

### Community 9 - "FakeAuditService"
Cohesion: 0.19
Nodes (13): FakeAuditService, Actions, Details, IpAddresses, FakeLoginAttemptStore, FailureCallCount, SuccessCallCount, FakeCredentialService (+5 more)

### Community 10 - ".CreateCensusService"
Cohesion: 0.06
Nodes (35): CensusWhitelistTests, AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year, IPromotionService (+27 more)

### Community 11 - "EmailReportViewModel"
Cohesion: 0.15
Nodes (12): EmailReportViewModel, EmailTypeFilter, EndDate, FailedEmails, Items, PendingEmails, Search, SentEmails (+4 more)

### Community 12 - "VotingEvent"
Cohesion: 0.06
Nodes (29): VotingEvent, Candidates, CreatedAt, CreatedByVoter, CreatedByVoterId, DeletedAt, Description, ElectionType (+21 more)

### Community 13 - "Task"
Cohesion: 0.13
Nodes (4): 5.2 M02 — Censo Electoral y Lista Blanca (Exclusivo Administrador), CensusService, ICensusService, IDocumentEncryptionService

### Community 14 - "PqrTicket"
Cohesion: 0.08
Nodes (25): Comprobación permanente de cobertura i18n y Localización de Alertas/AJAX, Estándar de Localización de Alertas, Modales y Respuestas Asíncronas (AJAX), Herramientas auxiliares y entorno local, Navegación por contexto, Rendimiento del módulo PQR, PqrListTests, PqrController, PqrTicket (+17 more)

### Community 15 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (23): WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals, Candidates, CensusWhitelists, ElectionPositions (+15 more)

### Community 16 - "ActiveSessionStore"
Cohesion: 0.16
Nodes (7): ActiveSessionStoreTests, ActiveSessionStore, SessionEntry, SessionInvalidationReason, AccountDisabledOrDeleted, LoggedOut, ReplacedByNewDevice

### Community 17 - "Voter"
Cohesion: 0.08
Nodes (23): Voter, AuditLogs, Candidates, ContactEmail, DeletedAt, DocumentHash, EmailQueues, EncryptedDocument (+15 more)

### Community 18 - "AdminCensusController"
Cohesion: 0.21
Nodes (5): 4.2 M02 — Gestión del Censo Electoral (Exclusivo Administrador), RF-M02-00 — Carga y Mantenimiento de la Lista Blanca del Censo, RF-M02-01 — Consulta, Modificación y Eliminación Lógica de Usuarios y Lista Blanca, RF-M02-02 — Promoción Automática de Año Lectivo, AdminCensusController

### Community 19 - "wahl_mirai_db_v3_0_completo.sql"
Cohesion: 0.23
Nodes (22): `academic_years`, `audit_log`, `candidacy_documents`, `candidate_proposals`, `candidates`, `census_whitelist`, `election_positions`, `email_queue` (+14 more)

### Community 20 - "LoginAttemptStore"
Cohesion: 0.11
Nodes (13): AuthServiceTests, FakeTimeProvider, LoginAttemptStoreTests, AuthService, LoginAttemptPolicy, LockoutThreshold, WindowDuration, LoginAttemptResult (+5 more)

### Community 22 - "FakeUrlHelper"
Cohesion: 0.16
Nodes (3): FakeUrlHelper, ActionContext, FakeUrlHelperFactory

### Community 23 - "WahlMirai.Tests"
Cohesion: 0.18
Nodes (9): coverlet.collector (6.0.4), Microsoft.EntityFrameworkCore.InMemory (9.0.7), Microsoft.EntityFrameworkCore.Relational (9.0.7), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, WahlMirai.Tests (+1 more)

### Community 24 - ".OnModelCreating"
Cohesion: 0.16
Nodes (8): VwPendingEmailQueue, Attempts, ContactEmail, CreatedAt, EmailType, FullName, Id, VoterId

### Community 25 - "AdminAccountService"
Cohesion: 0.22
Nodes (4): 5.9 M09 — Cuentas Administrativas (Exclusivo Súper Administrador), IActiveSessionStore, AdminAccountService, IAdminAccountService

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
Nodes (14): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), formatDate(), getCsrfToken(), initials(), openModal() (+6 more)

### Community 30 - "IAuthService"
Cohesion: 0.22
Nodes (3): IAuthService, IWhitelistService, WhitelistService

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
Cohesion: 0.17
Nodes (10): PagedResult, EndItemIndex, HasNextPage, HasPreviousPage, Items, PageNumber, PageSize, StartItemIndex (+2 more)

### Community 38 - "PasswordResetTokenStore"
Cohesion: 0.07
Nodes (17): 1. Arquitectura del Sistema, CredentialServiceTests, FailingWahlMiraiDbContext, FailOnSave, FakeActiveSessionStore, LastInvalidatedReason, LastInvalidatedUserId, SpyingTokenStore (+9 more)

### Community 39 - "EmailQueue"
Cohesion: 0.18
Nodes (10): EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt, Status (+2 more)

### Community 40 - "VwPendingCandidacy"
Cohesion: 0.18
Nodes (10): VwPendingCandidacy, CandidateId, CandidateName, EnrolledAt, EventTitle, MandatoryDocumentsUploaded, MandatoryRequirements, PositionId (+2 more)

### Community 42 - ".UseForcePasswordChange"
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

### Community 50 - "RegistrationOtpStore"
Cohesion: 0.13
Nodes (7): RegistrationOtpStoreTests, IRegistrationOtpStore, RegistrationOtpStore, RegistrationTicketResult, ResendOtpResult, TicketEntry, WhitelistCooldownInfo

### Community 51 - "VoterEventParticipation"
Cohesion: 0.18
Nodes (10): 3.1 Mapeo de Entidades C# vs Tablas MySQL, 3.2 Vistas MySQL Mapeadas en Entity Framework Core, 3. Modelo de Datos (17 Tablas Físicas y 5 Vistas MySQL), VoterEventParticipation, Id, ParticipatedAt, Voter, VoterId (+2 more)

### Community 52 - "check-i18n-coverage.ps1"
Cohesion: 0.38
Nodes (3): Add-ExtractedString(), Get-FilterCategory(), Test-UserFacingString()

### Community 53 - ".LogAsync"
Cohesion: 0.14
Nodes (11): IAuditService, CsvImportResult, DuplicateCount, ErrorCount, Errors, InsertedCount, ProcessedCount, CsvRowError (+3 more)

### Community 54 - ".CreateController"
Cohesion: 0.17
Nodes (6): AuthControllerRegistrationTests, TestWhitelistService, LookupCount, LookupResult, RegistrationCount, RegistrationException

### Community 55 - "PostulationFormDto"
Cohesion: 0.25
Nodes (7): PostulationFormDto, EventId, EventTitle, PositionDescription, PositionName, RegistrationEndDateTime, Requirements

### Community 56 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 57 - "PostulableEventDto"
Cohesion: 0.29
Nodes (6): PostulableEventDto, Description, Id, PositionName, RegistrationEndDateTime, Title

### Community 59 - "VotingService"
Cohesion: 0.11
Nodes (5): ElectorController, ResultsController, ResultsHub, IVotingService, VotingService

### Community 62 - "ESPECIFICACIÓN DE REQUERIMIENTOS DE SOFTWARE"
Cohesion: 0.13
Nodes (13): 1.1 Propósito, 1.2 Alcance del Sistema, 1.3 Definiciones, Acrónimos y Abreviaturas, 1.4 Referencias, 1. Introducción, 2.1 Perspectiva del Producto, 2.2 Funciones Principales del Sistema, 2. Descripción General del Sistema (+5 more)

### Community 69 - "Controller"
Cohesion: 0.11
Nodes (9): ErrorController, MediaController, ErrorViewModel, ExceptionMessage, Message, ShowHelpButton, ShowHomeButton, StatusCode (+1 more)

### Community 95 - "ProfileViewModel"
Cohesion: 0.09
Nodes (18): ChangePasswordViewModel, ConfirmPassword, NewPassword, LoginViewModel, Document, Password, ProfileViewModel, ContactEmail (+10 more)

### Community 98 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 99 - "RegistroStep2ViewModel"
Cohesion: 0.22
Nodes (9): RegistroStep2ViewModel, AcceptTermsAndPrivacy, ConfirmPassword, ContactEmail, Document, FullName, GradeName, Password (+1 more)

### Community 100 - "5. Matriz de Módulos, Controladores y Reglas de Negocio"
Cohesion: 0.17
Nodes (12): 2. Estructura de Directorios del Proyecto, 4.1 Auto-registro del Elector con Verificación OTP de 3 Pasos (RF-M01-00), 4.2 Autopostulación y Revisión de Candidatura (RF-M04-01, RF-M04-02), 4.3 Proceso de Votación y Escrutinio en Tiempo Real, 4. Diagramas de Flujo de Procesos Clave, 5.11 Control Dinámico de Etapas Electorales (RN-12), 5.5 M05 — Proceso de Votación y Control de Voto Único, 5.6 M06 — Escrutinio y Resultados en Tiempo Real (+4 more)

### Community 101 - "InstitutionSettings"
Cohesion: 0.17
Nodes (11): InstitutionSettings, Address, City, ContactEmail, DataProtectionEmail, HostingProvider, HostingRegion, Name (+3 more)

### Community 104 - "4. Requerimientos Específicos por Módulo"
Cohesion: 0.14
Nodes (14): 4.10 Requerimientos No Funcionales (RNF), 4.4 M04 — Autopostulación y Aprobación de Candidatos, 4.5 M05 — Proceso de Votación y Control de Voto Único, 4.6 M06 — Escrutinio y Resultados en Tiempo Real (Acceso Condicionado), 4.7 M07 — Perfil de Usuario y Autogestión de Credenciales, 4.9 M09 — Gestión de Cuentas Administrativas (Exclusivo Súper Administrador), 4. Requerimientos Específicos por Módulo, RF-M04-01 — Autopostulación de Candidatos con Documentos de Requisito (+6 more)

### Community 128 - "locale-core.js"
Cohesion: 0.30
Nodes (13): applyLocalizedThemeLabels(), computeTranslation(), flushPendingNodes(), getCulture(), getLanguageData(), getTranslation(), initSelects(), processAddedSubtree() (+5 more)

### Community 132 - ".CreateController"
Cohesion: 0.11
Nodes (10): RecuperacionAccesoControllerTests, RecuperacionAccesoController, ICredentialService, RecuperarAccesoViewModel, Documento, RestablecerViewModel, ConfirmPassword, IsInvalidLink (+2 more)

### Community 133 - "AuditLog"
Cohesion: 0.12
Nodes (14): AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue, OccurredAt (+6 more)

### Community 134 - "RequirementInputDto"
Cohesion: 0.50
Nodes (4): RequirementInputDto, Description, Id, IsMandatory

### Community 136 - "ICandidacyService"
Cohesion: 0.23
Nodes (6): 5.4 M04 — Autopostulación y Revisión de Candidaturas, ICandidacyService, PostulationResult, CandidateId, ErrorMessage, Success

### Community 141 - "LegalViewModel"
Cohesion: 0.29
Nodes (4): LegalViewModel, LastUpdated, Settings, Version

### Community 142 - "4.3 M03 — Gestión de Elecciones, Cargos y Etapas"
Cohesion: 0.50
Nodes (4): 4.3 M03 — Gestión de Elecciones, Cargos y Etapas, RF-M03-00 — Catálogo de Cargos Electorales y sus Requisitos, RF-M03-01 — Creación y Parametrización de Elecciones por Etapas, RF-M03-02 — Edición y Eliminación Lógica de Procesos Electorales

### Community 143 - "PositionRequirement"
Cohesion: 0.22
Nodes (8): PositionRequirement, CandidacyDocuments, Description, DisplayOrder, Id, IsMandatory, Position, PositionId

### Community 144 - "Directrices y Reglas del Proyecto WAHL-MIRAI"
Cohesion: 0.50
Nodes (3): Directrices y Reglas del Proyecto WAHL-MIRAI, Política de Actualización del Grafo de Conocimiento (Graphify), Reglas de aplicación:

### Community 145 - "EmailQueueItemDto"
Cohesion: 0.18
Nodes (11): EmailQueueItemDto, Attempts, CreatedAt, EmailType, ErrorMessage, Id, RecipientEmail, SentAt (+3 more)

### Community 146 - "AdminAccountListItem"
Cohesion: 0.18
Nodes (10): AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id, PositionTitle, RegisteredAt (+2 more)

### Community 147 - ".CreateTestContextAsync"
Cohesion: 0.06
Nodes (21): EmailQueueBackgroundServiceTests, TestContext, EmailSender, PasswordStore, Worker, TestEmailSender, SecretAvailableAtSend, SentBodies (+13 more)

### Community 149 - "Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.0 (Sincronizada con Implementación Real)"
Cohesion: 0.24
Nodes (3): 5.10 Servicio de Cola de Correo Progresivo (RN-9), Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.0 (Sincronizada con Implementación Real), AdminEmailReportController

### Community 150 - "RequirementCheckDto"
Cohesion: 0.25
Nodes (7): RequirementCheckDto, Description, FileUrl, IsMandatory, IsUploaded, RequirementId, UploadedAt

### Community 152 - "EventGrade"
Cohesion: 0.33
Nodes (6): EventGrade, Grade, GradeId, Id, VotingEvent, VotingEventId

### Community 153 - "Serena memory maintenance"
Cohesion: 0.40
Nodes (4): Discovery model, Maintenance, Serena memory maintenance, Style and scope

### Community 154 - "EmailType"
Cohesion: 0.16
Nodes (8): EmailRetryPolicy, EmailType, CAMBIO_PERFIL, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA, REASIGNACION_ADMIN, RECUPERACION_ACCESO, RESPUESTA_PQR

### Community 159 - "CandidateProposal"
Cohesion: 0.33
Nodes (6): CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id

### Community 163 - "4.8 M08 — Ayuda, Tutorial, PQR y Chatbot"
Cohesion: 0.25
Nodes (7): 4.8 M08 — Ayuda, Tutorial, PQR y Chatbot, RF-M08-00 — Sección de Ayuda Estática Ilustrada, RF-M08-01 — Creación de PQR por el Usuario, RF-M08-02 — Gestión y Resolución de PQR por el Administrador, RF-M08-03 — Chatbot de Ayuda Basado en Reglas, RF-M08-04 — Consulta Pública de Documentación Legal, Privacidad y Términos Institucionales, LegalDocuments

### Community 164 - ".SearchVoterAsync"
Cohesion: 0.25
Nodes (5): VoterSearchResultDto, Documento, FullName, GradeName, Id

### Community 168 - "WhitelistStatsDto"
Cohesion: 0.50
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

## Knowledge Gaps
- **574 isolated node(s):** `LookupCount`, `RegistrationCount`, `RegistrationException`, `LookupResult`, `ActionContext` (+569 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 894 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **99 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `ILoginAttemptStore`, `ProfileController`, `AdminCandidatesController`, `.CreateController`, `AdminEventsController`, `.CreateService`, `AuditLog`, `Candidate`, `FakeAuditService`, `.CreateCensusService`, `CensusWhitelist`, `VotingEvent`, `Task`, `PqrTicket`, `PositionRequirement`, `Voter`, `AdminCensusController`, `.CreateTestContextAsync`, `LoginAttemptStore`, `EventService`, `Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.0 (Sincronizada con Implementación Real)`, `EventGrade`, `.OnModelCreating`, `AdminAccountService`, `Grade`, `IAuthService`, `CandidateProposal`, `CandidacyDocument`, `PasswordResetTokenStore`, `EmailQueue`, `VwPendingCandidacy`, `CandidacyService`, `ElectionPosition`, `VwActiveCensu`, `VwPendingWhitelist`, `Vote`, `VwVoteCount`, `VoterEventParticipation`, `VotingService`, `CandidateReviewService`?**
  _High betweenness centrality (0.204) - this node is a cross-community bridge._
- **Are the 65 inferred relationships involving `FakeAuditService` (e.g. with `.CreateAdmin_StoresAccountAndInitialCredentialQueue()` and `.CreateService()`) actually correct?**
  _`FakeAuditService` has 65 INFERRED edges - model-reasoned connections that need verification._
- **What connects `LookupCount`, `RegistrationCount`, `RegistrationException` to the rest of the system?**
  _574 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `CandidateReviewDetailDto` be split into smaller, more focused modules?**
  _Cohesion score 0.08695652173913043 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Services` to `WahlMirai.Web.Models`, `WahlMirai.Web.ViewModels`, `ILoginAttemptStore`, `ProfileController`, `PasswordResetTokenStore`, `AuthControllerRegistrationTests.cs`, `EmailType`, `ICandidacyService`, `.CreateCensusService`, `AdminAccountsController`, `Program.cs`, `Task`, `ActiveSessionStore`, `microsoft_entityframeworkcore`?**
  _High betweenness centrality (0.097) - this node is a cross-community bridge._
- **Are the 6 inferred relationships involving `VotingEvent` (e.g. with `.AddCandidate_AllowsGrade11_ForPersonero()` and `.AddCandidate_RejectsNonGrade11_ForPersonero()`) actually correct?**
  _`VotingEvent` has 6 INFERRED edges - model-reasoned connections that need verification._
- **Should `CensusWhitelist` be split into smaller, more focused modules?**
  _Cohesion score 0.125 - nodes in this community are weakly interconnected._