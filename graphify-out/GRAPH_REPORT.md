# Graph Report - WAHL-MIRAI  (2026-11-11)

## Corpus Check
- 212 files · ~721,283 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 35 file(s) not represented in the graph (top: .resx 14, (none) 9, .mmd 7)

## Summary
- 2203 nodes · 5165 edges · 189 communities (87 shown, 102 thin omitted)
- Extraction: 82% EXTRACTED · 18% INFERRED · 0% AMBIGUOUS · INFERRED: 933 edges (avg confidence: 0.86)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `2a0c585a`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- AuthService
- wahl_mirai_db_v3_0_completo.sql
- CandidateReviewDetailDto
- AdminEventsController
- CensusWhitelist
- .CreateService
- Candidate
- .Apply
- FakeLoginAttemptStore
- HttpContext
- EmailReportViewModel
- VotingEvent
- AdminAccountService
- PqrTicket
- WahlMiraiDbContext
- LoginAttemptStore
- Voter
- AdminCensusController
- .LogAsync
- .From
- EventService
- FakeUrlHelper
- WahlMirai.Tests
- .OnModelCreating
- CandidateReviewService
- ayuda-chatbot.js
- http
- .RetryEmail
- pqr-manage.js
- 1. Diccionario de Tablas Físicas (17 Tablas)
- VoterDetailDto
- ActiveSessionStore
- AuthController
- CandidacyDocument
- WhitelistDetailDto
- PostulationFormDto
- Vote
- PasswordResetTokenStore
- EmailQueue
- VwPendingCandidacy
- project.yml
- .List
- ElectionPosition
- VwActiveCensu
- VwPendingWhitelist
- DESIGN.md
- 5.1 M01 — Gestión de Acceso, Auto-registro y Sesión
- 5. Matriz de Módulos, Controladores y Reglas de Negocio
- VwVoteCount
- RegistrationOtpStore
- VoterEventParticipation
- check-i18n-coverage.ps1
- .ImportCsvAsync
- FakeAuditService
- Manual Técnico — Diagramas del Sistema Wahl Mirai
- theme.js
- AdminCandidatesController
- WahlMirai.Web.Services
- VotingService
- Census Whitelist Self-Registration
- ILoginAttemptStore
- NotFound
- WahlMirai.Web.ViewModels
- .BuildOptions
- Development Environment
- Administrator Voting Workflow
- ErrorViewModel
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
- MyPostulationDto
- Real-Time Scrutiny
- .CreateCensusService
- WahlMirai.Web
- RegistroStep2ViewModel
- 4. Requerimientos Específicos por Módulo
- InstitutionSettings
- AuthViewModels.cs
- EmailQueueItemDto
- MIT License
- MIT License
- MIT License
- locale-core.js
- VerificarCodigo.cshtml
- memory_maintenance.md
- PagedResult
- .CreateController
- AuditLog
- EmailRetryPolicyTests
- EmailQueueBackgroundService
- Controller
- Program.cs
- WahlMirai.Web.ViewModels.RestablecerViewModel
- WhitelistStatsDto
- Índice de Documentos y Archivos
- RequirementInputDto
- 3. Figura 3: Diagrama de Componentes y Dependencias (Sección 2.3)
- Directrices y Reglas del Proyecto WAHL-MIRAI
- CandidacyService
- TestContext
- .CreateTestContextAsync
- Política de Refresco de Graphify
- EmailSettings
- WhitelistService
- PostulationSubmitDto
- EventGrade
- Serena memory maintenance
- EmailType
- Grade
- Arquitectura y Diseño de Software
- Contacto.cshtml
- ElectorController
- CandidateProposal
- Contacto.Body.cshtml
- Cookies.cshtml
- Cookies.Body.cshtml
- PositionRequirement
- IEventService
- Legal/Index.cshtml
- TestEmailSender
- System.Text.Encodings.Web
- RequirementCheckDto
- IActiveSessionStore
- Privacidad.cshtml
- Privacidad.Body.cshtml
- Terminos.cshtml
- Terminos.Body.cshtml
- FakeEmailSender
- LicensingSettings
- AdminAccountViewModels.cs

## God Nodes (most connected - your core abstractions)
1. `WahlMiraiDbContext` - 94 edges
2. `FakeAuditService` - 81 edges
3. `WahlMirai.Web.Models` - 71 edges
4. `VotingEvent` - 65 edges
5. `WahlMirai.Web.Services` - 64 edges
6. `Voter` - 63 edges
7. `LoginAttemptStore` - 51 edges
8. `CensusWhitelist` - 47 edges
9. `Candidate` - 41 edges
10. `FakeLoginAttemptStore` - 40 edges

## Surprising Connections (you probably didn't know these)
- `On the horizon` --references--> `Voter`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Models/Voter.cs
- `2.4 Vista: `vw_pending_email_queue` (`VwPendingEmailQueue.cs`)` --references--> `EmailQueueBackgroundService`  [INFERRED]
  docs/diagramas/diccionario_de_datos_v3_1.md → WahlMirai.Web/Services/EmailQueueBackgroundService.cs
- `5.3 M03 — Gestión de Elecciones, Cargos y Etapas` --references--> `IEventService`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Services/IEventService.cs
- `5.5 M05 — Proceso de Votación y Control de Voto Único` --references--> `IVotingService`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Services/IVotingService.cs
- `1. Arquitectura del Sistema` --references--> `AdminAccountsController`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_1.md → WahlMirai.Web/Controllers/AdminAccountsController.cs

## Import Cycles
- None detected.

## Communities (189 total, 102 thin omitted)

### Community 0 - "WahlMirai.Web.Models"
Cohesion: 0.15
Nodes (3): WahlMirai.Web.Models, LegalDocuments, Roles

### Community 2 - "wahl_mirai_db_v3_0_completo.sql"
Cohesion: 0.23
Nodes (22): `academic_years`, `audit_log`, `candidacy_documents`, `candidate_proposals`, `candidates`, `census_whitelist`, `election_positions`, `email_queue` (+14 more)

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

### Community 8 - ".Apply"
Cohesion: 0.19
Nodes (3): 5.4 M04 — Autopostulación y Revisión de Candidaturas, CandidacyController, ICandidacyService

### Community 9 - "FakeLoginAttemptStore"
Cohesion: 0.19
Nodes (10): FakeLoginAttemptStore, FailureCallCount, SuccessCallCount, FakeCredentialService, FakeEmailSender, Messages, ShouldFail, ShouldFailFirst (+2 more)

### Community 11 - "EmailReportViewModel"
Cohesion: 0.14
Nodes (13): EmailReportViewModel, EmailTypeFilter, EndDate, FailedEmails, Items, PagedItems, PendingEmails, Search (+5 more)

### Community 12 - "VotingEvent"
Cohesion: 0.06
Nodes (29): VotingEvent, Candidates, CreatedAt, CreatedByVoter, CreatedByVoterId, DeletedAt, Description, ElectionType (+21 more)

### Community 13 - "AdminAccountService"
Cohesion: 0.07
Nodes (27): AdminAccountsController, AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id, PositionTitle (+19 more)

### Community 14 - "PqrTicket"
Cohesion: 0.16
Nodes (14): PqrListTests, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt, RespondedBy (+6 more)

### Community 15 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (23): WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals, Candidates, CensusWhitelists, ElectionPositions (+15 more)

### Community 16 - "LoginAttemptStore"
Cohesion: 0.25
Nodes (3): FakeTimeProvider, LoginAttemptStoreTests, LoginAttemptStore

### Community 17 - "Voter"
Cohesion: 0.08
Nodes (23): Voter, AuditLogs, Candidates, ContactEmail, DeletedAt, DocumentHash, EmailQueues, EncryptedDocument (+15 more)

### Community 18 - "AdminCensusController"
Cohesion: 0.21
Nodes (5): 4.2 M02 — Gestión del Censo Electoral (Exclusivo Administrador), RF-M02-00 — Carga y Mantenimiento de la Lista Blanca del Censo, RF-M02-01 — Consulta, Modificación y Eliminación Lógica de Usuarios y Lista Blanca, RF-M02-02 — Promoción Automática de Año Lectivo, AdminCensusController

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

### Community 28 - ".RetryEmail"
Cohesion: 0.22
Nodes (3): 5.10 Servicio de Cola de Correo Progresivo (RN-9), Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.1 (Sincronizada con Implementación Real), AdminEmailReportController

### Community 29 - "pqr-manage.js"
Cohesion: 0.28
Nodes (14): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), formatDate(), getCsrfToken(), initials(), openModal() (+6 more)

### Community 30 - "1. Diccionario de Tablas Físicas (17 Tablas)"
Cohesion: 0.07
Nodes (27): 1.10 Tabla: `event_grades`, 1.11 Tabla: `candidates`, 1.12 Tabla: `candidate_proposals`, 1.13 Tabla: `candidacy_documents`, 1.14 Tabla: `votes`, 1.15 Tabla: `event_participations`, 1.16 Tabla: `audit_log`, 1.17 Tabla: `pqr_tickets` (+19 more)

### Community 31 - "VoterDetailDto"
Cohesion: 0.13
Nodes (14): VoterDetailDto, ContactEmail, DeletedAt, Document, ExcluirDePromocion, FullName, GradeId, GradeName (+6 more)

### Community 32 - "ActiveSessionStore"
Cohesion: 0.17
Nodes (7): ActiveSessionStoreTests, ActiveSessionStore, SessionEntry, SessionInvalidationReason, AccountDisabledOrDeleted, LoggedOut, ReplacedByNewDevice

### Community 34 - "CandidacyDocument"
Cohesion: 0.22
Nodes (8): CandidacyDocument, Candidate, CandidateId, FileUrl, Id, Requirement, RequirementId, UploadedAt

### Community 35 - "WhitelistDetailDto"
Cohesion: 0.12
Nodes (13): WhitelistDetailDto, ClaimedAt, ClaimedByUserId, ContactEmail, CreatedAt, Document, DocumentHash, ExcluirDePromocion (+5 more)

### Community 36 - "PostulationFormDto"
Cohesion: 0.11
Nodes (17): PostulableEventDto, Description, Id, PositionName, RegistrationEndDateTime, Title, PostulationFormDto, EventId (+9 more)

### Community 37 - "Vote"
Cohesion: 0.22
Nodes (8): Vote, Candidate, CandidateId, Id, VotedAt, VoteHash, VotingEvent, VotingEventId

### Community 38 - "PasswordResetTokenStore"
Cohesion: 0.07
Nodes (16): CredentialServiceTests, FailingWahlMiraiDbContext, FailOnSave, FakeActiveSessionStore, LastInvalidatedReason, LastInvalidatedUserId, SpyingTokenStore, LastIssuedToken (+8 more)

### Community 39 - "EmailQueue"
Cohesion: 0.21
Nodes (11): AdminEmailReportListTests, EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt (+3 more)

### Community 40 - "VwPendingCandidacy"
Cohesion: 0.17
Nodes (10): VwPendingCandidacy, CandidateId, CandidateName, EnrolledAt, EventTitle, MandatoryDocumentsUploaded, MandatoryRequirements, PositionId (+2 more)

### Community 41 - "project.yml"
Cohesion: 0.08
Nodes (25): activation_command, activation_command_timeout, added_modes, agent_interface, default_modes, encoding, excluded_apis, excluded_tools (+17 more)

### Community 42 - ".List"
Cohesion: 0.14
Nodes (11): Comprobación permanente de cobertura i18n y Localización de Alertas/AJAX, Estándar de Localización de Alertas, Modales y Respuestas Asíncronas (AJAX), Herramientas auxiliares y entorno local, Navegación por contexto, Rendimiento del módulo PQR, PqrController, PqrCreateDto, Message (+3 more)

### Community 43 - "ElectionPosition"
Cohesion: 0.20
Nodes (8): ElectionPosition, CreatedAt, Description, Id, Name, PositionRequirements, Status, VotingEvents

### Community 44 - "VwActiveCensu"
Cohesion: 0.20
Nodes (9): VwActiveCensu, ContactEmail, ExcluirDePromocion, FullName, Grade, Id, RegisteredAt, Status (+1 more)

### Community 45 - "VwPendingWhitelist"
Cohesion: 0.17
Nodes (10): VwPendingWhitelist, ContactEmail, CreatedAt, DocumentHash, EncryptedDocument, ExcluirDePromocion, FullName, Grade (+2 more)

### Community 46 - "DESIGN.md"
Cohesion: 0.22
Nodes (8): Brand & Style, Colors, Components, Elevation & Depth, Graphify (knowledge graph para agentes de IA), Layout & Spacing, Shapes, Typography

### Community 47 - "5.1 M01 — Gestión de Acceso, Auto-registro y Sesión"
Cohesion: 0.14
Nodes (5): WahlMirai.Web.Middleware, 5.1 M01 — Gestión de Acceso, Auto-registro y Sesión, ForcePasswordChangeMiddleware, ForcePasswordChangeMiddlewareExtensions, IAuthService

### Community 48 - "5. Matriz de Módulos, Controladores y Reglas de Negocio"
Cohesion: 0.25
Nodes (8): 5.11 Control Dinámico de Etapas Electorales (RN-12), 5.3 M03 — Gestión de Elecciones, Cargos y Etapas, 5.5 M05 — Proceso de Votación y Control de Voto Único, 5.6 M06 — Escrutinio y Resultados en Tiempo Real, 5.8 M08 — Ayuda, Chatbot FAQ y PQR, 5.9 M09 — Cuentas Administrativas (Exclusivo Súper Administrador), 5. Matriz de Módulos, Controladores y Reglas de Negocio, On the horizon

### Community 49 - "VwVoteCount"
Cohesion: 0.22
Nodes (9): VwVoteCount, CandidateId, CandidateName, EventId, EventStatus, EventTitle, IsBlankVote, TotalVotes (+1 more)

### Community 50 - "RegistrationOtpStore"
Cohesion: 0.10
Nodes (7): RegistrationOtpStoreTests, IRegistrationOtpStore, RegistrationOtpStore, RegistrationTicketResult, ResendOtpResult, TicketEntry, WhitelistCooldownInfo

### Community 51 - "VoterEventParticipation"
Cohesion: 0.25
Nodes (7): VoterEventParticipation, Id, ParticipatedAt, Voter, VoterId, VotingEvent, VotingEventId

### Community 52 - "check-i18n-coverage.ps1"
Cohesion: 0.38
Nodes (3): Add-ExtractedString(), Get-FilterCategory(), Test-UserFacingString()

### Community 53 - ".ImportCsvAsync"
Cohesion: 0.13
Nodes (13): CsvImportResult, DuplicateCount, ErrorCount, Errors, InsertedCount, ProcessedCount, CsvRowData, Fields (+5 more)

### Community 54 - "FakeAuditService"
Cohesion: 0.13
Nodes (15): AuthControllerRegistrationTests, FakeUrlHelperFactory, TestWhitelistService, LookupCount, LookupResult, RegistrationCount, RegistrationException, FakeAuditService (+7 more)

### Community 55 - "Manual Técnico — Diagramas del Sistema Wahl Mirai"
Cohesion: 0.17
Nodes (11): 1. Figura 1: Arquitectura General y Despliegue Físico (Sección 2.1 / 2.3), 2. Figura 2: Diagrama de Casos de Uso por Rol (Sección 2.3), 4.1 Secuencia de Auto-registro con OTP en 3 Pasos (RF-M01-00), 4.2 Secuencia de Autopostulación y Revisión de Candidaturas (RF-M04-01, RF-M04-02), 4.3 Secuencia de Votación y Escrutinio en Tiempo Real (RF-M05-00, RF-M06-00), 4. Figura 4: Diagramas de Secuencia de Procesos Clave (Sección 2.3), 5. Figura 5: Diagrama Entidad-Relación (DER) (Sección 3.1), 6. Sección 3.2: Diccionario de Datos (+3 more)

### Community 56 - "theme.js"
Cohesion: 0.43
Nodes (4): applyTheme(), bindToggleEvents(), translateThemeLabel(), updateIcons()

### Community 58 - "WahlMirai.Web.Services"
Cohesion: 0.19
Nodes (3): WahlMirai.Web.Services, WahlMirai.Tests, WahlMirai.Tests.Common

### Community 59 - "VotingService"
Cohesion: 0.16
Nodes (4): ResultsController, ResultsHub, IVotingService, VotingService

### Community 62 - "ILoginAttemptStore"
Cohesion: 0.06
Nodes (25): 5.7 M07 — Perfil de Usuario y Autogestión, 3. Reglas de Negocio Transversales, ChangePasswordRequest, ProfileController, UpdateEmailRequest, VerifyPasswordRequest, ILoginAttemptStore, LoginAttemptPolicy (+17 more)

### Community 63 - "NotFound"
Cohesion: 0.18
Nodes (8): RegistroFailureReason, AlreadyClaimed, Cooldown, Locked, NoContactEmail, None, NotFound, SendFailed

### Community 65 - "WahlMirai.Web.ViewModels"
Cohesion: 0.19
Nodes (3): WahlMirai.Web.Controllers, WahlMirai.Web.ViewModels, WahlMirai.Web

### Community 69 - "ErrorViewModel"
Cohesion: 0.16
Nodes (7): ErrorController, ErrorViewModel, ExceptionMessage, Message, ShowHelpButton, ShowHomeButton, Title

### Community 95 - "MyPostulationDto"
Cohesion: 0.17
Nodes (12): MyPostulationDto, AllowCorrection, ApprovedWithExceptions, CandidateId, EnrolledAt, EventId, EventTitle, ExceptionsDetail (+4 more)

### Community 97 - ".CreateCensusService"
Cohesion: 0.06
Nodes (38): 5.2 M02 — Censo Electoral y Lista Blanca (Exclusivo Administrador), CensusWhitelistTests, AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year (+30 more)

### Community 98 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 99 - "RegistroStep2ViewModel"
Cohesion: 0.12
Nodes (13): RegistroStep2ViewModel, AcceptTermsAndPrivacy, ConfirmPassword, ContactEmail, Document, FullName, GradeName, Password (+5 more)

### Community 100 - "4. Requerimientos Específicos por Módulo"
Cohesion: 0.05
Nodes (36): 1.1 Propósito, 1.2 Alcance del Sistema, 1.3 Definiciones, Acrónimos y Abreviaturas, 1.4 Referencias, 1. Introducción, 2.1 Perspectiva del Producto, 2.2 Funciones Principales del Sistema, 2. Descripción General del Sistema (+28 more)

### Community 101 - "InstitutionSettings"
Cohesion: 0.12
Nodes (16): LegalController, InstitutionSettings, Address, City, ContactEmail, DataProtectionEmail, HostingProvider, HostingRegion (+8 more)

### Community 102 - "AuthViewModels.cs"
Cohesion: 0.29
Nodes (6): ChangePasswordViewModel, ConfirmPassword, NewPassword, LoginViewModel, Document, Password

### Community 104 - "EmailQueueItemDto"
Cohesion: 0.18
Nodes (11): EmailQueueItemDto, Attempts, CreatedAt, EmailType, ErrorMessage, Id, RecipientEmail, SentAt (+3 more)

### Community 128 - "locale-core.js"
Cohesion: 0.30
Nodes (13): applyLocalizedThemeLabels(), computeTranslation(), flushPendingNodes(), getCulture(), getLanguageData(), getTranslation(), initSelects(), processAddedSubtree() (+5 more)

### Community 131 - "PagedResult"
Cohesion: 0.17
Nodes (10): PagedResult, EndItemIndex, HasNextPage, HasPreviousPage, Items, PageNumber, PageSize, StartItemIndex (+2 more)

### Community 132 - ".CreateController"
Cohesion: 0.11
Nodes (10): RecuperacionAccesoControllerTests, RecuperacionAccesoController, ICredentialService, RecuperarAccesoViewModel, Documento, RestablecerViewModel, ConfirmPassword, IsInvalidLink (+2 more)

### Community 133 - "AuditLog"
Cohesion: 0.14
Nodes (13): AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue, OccurredAt (+5 more)

### Community 140 - "WhitelistStatsDto"
Cohesion: 0.40
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

### Community 141 - "Índice de Documentos y Archivos"
Cohesion: 0.40
Nodes (4): 1. Documentos Consolidados, 2. Archivos Mermaid Independientes (`.mmd`), Diagramas del Sistema Wahl Mirai (Versión 3.1), Índice de Documentos y Archivos

### Community 142 - "RequirementInputDto"
Cohesion: 0.50
Nodes (4): RequirementInputDto, Description, Id, IsMandatory

### Community 143 - "3. Figura 3: Diagrama de Componentes y Dependencias (Sección 2.3)"
Cohesion: 0.18
Nodes (4): 3. Figura 3: Diagrama de Componentes y Dependencias (Sección 2.3), ClientIpKey, DocumentEncryptionService, MailKitEmailSender

### Community 144 - "Directrices y Reglas del Proyecto WAHL-MIRAI"
Cohesion: 0.50
Nodes (3): Directrices y Reglas del Proyecto WAHL-MIRAI, Política de Actualización del Grafo de Conocimiento (Graphify), Reglas de aplicación:

### Community 146 - "TestContext"
Cohesion: 0.20
Nodes (4): TestContext, EmailSender, PasswordStore, Worker

### Community 149 - "EmailSettings"
Cohesion: 0.20
Nodes (9): EmailSettings, BaseUrl, RateLimitPerMinute, SenderDisplayName, SenderEmail, SenderPassword, SmtpHost, SmtpPort (+1 more)

### Community 151 - "PostulationSubmitDto"
Cohesion: 0.22
Nodes (7): PostulationSubmitDto, Documents, EventId, GovernmentPlan, Photo, Proposals, Slogan

### Community 152 - "EventGrade"
Cohesion: 0.33
Nodes (6): EventGrade, Grade, GradeId, Id, VotingEvent, VotingEventId

### Community 153 - "Serena memory maintenance"
Cohesion: 0.40
Nodes (4): Discovery model, Maintenance, Serena memory maintenance, Style and scope

### Community 154 - "EmailType"
Cohesion: 0.18
Nodes (8): EmailRetryPolicy, EmailType, CAMBIO_PERFIL, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA, REASIGNACION_ADMIN, RECUPERACION_ACCESO, RESPUESTA_PQR

### Community 155 - "Grade"
Cohesion: 0.22
Nodes (8): 1. Arquitectura del Sistema, Grade, EventGrades, Id, IsLastGrade, Name, SequenceOrder, Voters

### Community 156 - "Arquitectura y Diseño de Software"
Cohesion: 0.22
Nodes (9): 2. Estructura de Directorios del Proyecto, 3.1 Mapeo de Entidades C# vs Tablas MySQL, 3.2 Vistas MySQL Mapeadas en Entity Framework Core, 3. Modelo de Datos (17 Tablas Físicas y 5 Vistas MySQL), 4.1 Auto-registro del Elector con Verificación OTP de 3 Pasos (RF-M01-00), 4.2 Autopostulación y Revisión de Candidatura (RF-M04-01, RF-M04-02), 4.3 Proceso de Votación y Escrutinio en Tiempo Real, 4. Diagramas de Flujo de Procesos Clave (+1 more)

### Community 159 - "CandidateProposal"
Cohesion: 0.33
Nodes (6): CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id

### Community 163 - "PositionRequirement"
Cohesion: 0.22
Nodes (8): PositionRequirement, CandidacyDocuments, Description, DisplayOrder, Id, IsMandatory, Position, PositionId

### Community 164 - "IEventService"
Cohesion: 0.21
Nodes (6): IEventService, VoterSearchResultDto, Documento, FullName, GradeName, Id

### Community 166 - "TestEmailSender"
Cohesion: 0.25
Nodes (4): TestEmailSender, SecretAvailableAtSend, SentBodies, SentSubjects

### Community 168 - "RequirementCheckDto"
Cohesion: 0.25
Nodes (7): RequirementCheckDto, Description, FileUrl, IsMandatory, IsUploaded, RequirementId, UploadedAt

### Community 169 - "IActiveSessionStore"
Cohesion: 0.33
Nodes (5): 4.1 M01 — Gestión de Acceso, Auto-registro y Sesión, RF-M01-00 — Auto-registro de Electores Contra Lista Blanca, RF-M01-01 — Autenticación de Usuarios por Identificador Único, RF-M01-02 — Recuperación de Acceso y Restablecimiento por Token de un Solo Uso, IActiveSessionStore

### Community 183 - "FakeEmailSender"
Cohesion: 0.33
Nodes (3): FakeEmailSender, SentMessages, ShouldFail

### Community 184 - "LicensingSettings"
Cohesion: 0.50
Nodes (3): LicensingSettings, SafeSourceCodeUrl, SourceCodeUrl

## Knowledge Gaps
- **591 isolated node(s):** `ShouldFail`, `SentMessages`, `LookupCount`, `RegistrationCount`, `RegistrationException` (+586 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 975 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **102 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `AuthService`, `.CreateController`, `AdminEventsController`, `.CreateService`, `AuditLog`, `Candidate`, `FakeLoginAttemptStore`, `CensusWhitelist`, `EmailQueueBackgroundService`, `VotingEvent`, `AdminAccountService`, `PqrTicket`, `3. Figura 3: Diagrama de Componentes y Dependencias (Sección 2.3)`, `Voter`, `TestContext`, `.CreateTestContextAsync`, `AdminCensusController`, `EventService`, `CandidacyService`, `.LogAsync`, `EventGrade`, `.OnModelCreating`, `CandidateReviewService`, `Grade`, `.RetryEmail`, `Arquitectura y Diseño de Software`, `WhitelistService`, `CandidateProposal`, `CandidacyDocument`, `PositionRequirement`, `Vote`, `PasswordResetTokenStore`, `EmailQueue`, `VwPendingCandidacy`, `.List`, `ElectionPosition`, `VwActiveCensu`, `VwPendingWhitelist`, `VwVoteCount`, `VoterEventParticipation`, `AdminCandidatesController`, `VotingService`, `ILoginAttemptStore`, `.CreateCensusService`?**
  _High betweenness centrality (0.230) - this node is a cross-community bridge._
- **Are the 2 inferred relationships involving `WahlMiraiDbContext` (e.g. with `3.2 Vistas MySQL Mapeadas en Entity Framework Core` and `3. Figura 3: Diagrama de Componentes y Dependencias (Sección 2.3)`) actually correct?**
  _`WahlMiraiDbContext` has 2 INFERRED edges - model-reasoned connections that need verification._
- **What connects `ShouldFail`, `SentMessages`, `LookupCount` to the rest of the system?**
  _591 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `WahlMirai.Web.Models` be split into smaller, more focused modules?**
  _Cohesion score 0.1495798319327731 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Services` to `WahlMirai.Web.Models`, `WahlMirai.Web.ViewModels`, `ActiveSessionStore`, `.CreateCensusService`, `PostulationFormDto`, `PasswordResetTokenStore`, `AuthControllerRegistrationTests.cs`, `LoginAttemptStore.cs`, `FakeLoginAttemptStore`, `Program.cs`, `AdminAccountService`, `RegistrationOtpStore`, `MailKitEmailSender.cs`, `AdminAccountViewModels.cs`, `EmailType`, `ILoginAttemptStore`?**
  _High betweenness centrality (0.073) - this node is a cross-community bridge._
- **Are the 73 inferred relationships involving `FakeAuditService` (e.g. with `.CreateAdmin_StoresAccountAndInitialCredentialQueue()` and `.CreateService()`) actually correct?**
  _`FakeAuditService` has 73 INFERRED edges - model-reasoned connections that need verification._
- **Should `CandidateReviewDetailDto` be split into smaller, more focused modules?**
  _Cohesion score 0.08695652173913043 - nodes in this community are weakly interconnected._