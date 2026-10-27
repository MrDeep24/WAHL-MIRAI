# Graph Report - WAHL-MIRAI  (2026-10-27)

## Corpus Check
- 208 files · ~206,951 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 28 file(s) not represented in the graph (top: .resx 14, (none) 8, .css 3)

## Summary
- 1864 nodes · 4906 edges · 177 communities (85 shown, 92 thin omitted)
- Extraction: 81% EXTRACTED · 19% INFERRED · 0% AMBIGUOUS · INFERRED: 946 edges (avg confidence: 0.85)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `c6dab582`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- WahlMirai.Web.Models
- 4.1 M01 — Gestión de Acceso, Auto-registro y Sesión
- ValidateAntiForgeryToken
- CandidateReviewDetailDto
- AdminEventsController
- CensusWhitelist
- .CreateService
- Candidate
- AdminAccountFormViewModel
- Fact
- PromotionPreview
- EmailReportViewModel
- VotingEvent
- Task
- PqrTicket
- WahlMiraiDbContext
- DateTimeOffset
- Voter
- IActionResult
- .CreateCensusService
- LoginAttemptStore
- EventService
- FakeUrlHelper
- WahlMirai.Tests
- .OnModelCreating
- CancellationToken
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
- project.yml
- PqrController
- PositionRequirement
- VwActiveCensu
- VwPendingWhitelist
- DESIGN.md
- IEmailSender
- DateTime
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
- ESPECIFICACIÓN DE REQUERIMIENTOS DE SOFTWARE
- AdminAccountService
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
- .GetPromotionPreviewAsync_ReflectsConsolidatedTotals
- Real-Time Scrutiny
- PromotionResult
- WahlMirai.Web
- RegistroStep2ViewModel
- Arquitectura y Diseño de Software
- InstitutionSettings
- 4. Requerimientos Específicos por Módulo
- MIT License
- MIT License
- MIT License
- locale-core.js
- VerificarCodigo.cshtml
- memory_maintenance.md
- EmailQueueBackgroundService
- .CreateController
- AuditLog
- AcademicYear
- 1. Introducción
- HttpGet
- Program.cs
- WahlMirai.Web.ViewModels.RestablecerViewModel
- IDocumentEncryptionService
- 4.2 M02 — Gestión del Censo Electoral (Exclusivo Administrador)
- 4.3 M03 — Gestión de Elecciones, Cargos y Etapas
- ResultsHub
- Directrices y Reglas del Proyecto WAHL-MIRAI
- 4.4 M04 — Autopostulación y Aprobación de Candidatos
- AdminAccountListItem
- TestContext
- Política de Refresco de Graphify
- Controller
- List
- 5. Matriz de Módulos, Controladores y Reglas de Negocio
- EventGrade
- Serena memory maintenance
- EmailType
- 4.7 M07 — Perfil de Usuario y Autogestión de Credenciales
- Contacto.cshtml
- CandidateProposal
- Contacto.Body.cshtml
- Cookies.cshtml
- Cookies.Body.cshtml
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
4. `VotingEvent` - 66 edges
5. `WahlMirai.Web.Services` - 64 edges
6. `Voter` - 61 edges
7. `LoginAttemptStore` - 45 edges
8. `FakeLoginAttemptStore` - 42 edges
9. `Candidate` - 41 edges
10. `CensusWhitelist` - 39 edges

## Surprising Connections (you probably didn't know these)
- `On the horizon` --references--> `Voter`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_0.md → WahlMirai.Web/Models/Voter.cs
- `RF-M01-00 — Auto-registro de Electores Contra Lista Blanca` --references--> `IEmailSender`  [INFERRED]
  docs/ers_wahl_mirai_v3_0.md → WahlMirai.Web/Services/IEmailSender.cs
- `5.3 M03 — Gestión de Elecciones, Cargos y Etapas` --references--> `IEventService`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_0.md → WahlMirai.Web/Services/IEventService.cs
- `5.5 M05 — Proceso de Votación y Control de Voto Único` --references--> `IVotingService`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_0.md → WahlMirai.Web/Services/IVotingService.cs
- `1. Arquitectura del Sistema` --references--> `AdminAccountsController`  [INFERRED]
  docs/Arquitectura_y_Diseno_v3_0.md → WahlMirai.Web/Controllers/AdminAccountsController.cs

## Import Cycles
- None detected.

## Communities (177 total, 92 thin omitted)

### Community 0 - "WahlMirai.Web.Models"
Cohesion: 0.15
Nodes (3): WahlMirai.Web.Models, LegalDocuments, Roles

### Community 1 - "4.1 M01 — Gestión de Acceso, Auto-registro y Sesión"
Cohesion: 0.50
Nodes (4): 4.1 M01 — Gestión de Acceso, Auto-registro y Sesión, RF-M01-00 — Auto-registro de Electores Contra Lista Blanca, RF-M01-01 — Autenticación de Usuarios por Identificador Único, RF-M01-02 — Recuperación de Acceso y Restablecimiento por Token de un Solo Uso

### Community 3 - "CandidateReviewDetailDto"
Cohesion: 0.09
Nodes (23): CandidateReviewDetailDto, AllowCorrection, ApprovedWithExceptions, CandidateEmail, CandidateId, CandidateName, EnrolledAt, EventId (+15 more)

### Community 4 - "AdminEventsController"
Cohesion: 0.18
Nodes (6): AdminEventsController, RequirementInputDto, Description, Id, IsMandatory, IEventService

### Community 5 - "CensusWhitelist"
Cohesion: 0.11
Nodes (15): CensusWhitelist, ClaimedAt, ClaimedByUserId, ClaimedByVoter, ContactEmail, CreatedAt, DocumentHash, EncryptedDocument (+7 more)

### Community 7 - "Candidate"
Cohesion: 0.08
Nodes (23): Candidate, AllowCorrection, ApprovedWithExceptions, CandidacyDocuments, CandidateProposals, EnrolledAt, ExceptionsDetail, GovernmentPlanUrl (+15 more)

### Community 8 - "AdminAccountFormViewModel"
Cohesion: 0.20
Nodes (9): AdminAccountEditViewModel, Id, AdminAccountFormViewModel, ContactEmail, Document, FullName, PositionTitle, RoleName (+1 more)

### Community 9 - "Fact"
Cohesion: 0.10
Nodes (19): 5.7 M07 — Perfil de Usuario y Autogestión, AuthServiceTests, ClientIpKeyTests, FakeAuditService, Actions, Details, IpAddresses, FakeLoginAttemptStore (+11 more)

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
Cohesion: 0.12
Nodes (6): CandidateReviewService, AuditService, IAuditService, ICandidateReviewService, CensusService, ICensusService

### Community 14 - "PqrTicket"
Cohesion: 0.16
Nodes (14): PqrListTests, PqrTicket, AdminResponse, CreatedAt, Id, Message, RespondedAt, RespondedBy (+6 more)

### Community 15 - "WahlMiraiDbContext"
Cohesion: 0.07
Nodes (23): WahlMiraiDbContext, AcademicYears, AuditLogs, CandidacyDocuments, CandidateProposals, Candidates, CensusWhitelists, ElectionPositions (+15 more)

### Community 16 - "DateTimeOffset"
Cohesion: 0.13
Nodes (9): LoginAttemptPolicy, LockoutThreshold, WindowDuration, LoginAttemptResult, TicketEntry, LoginAttemptEntry, FailedCount, LockedUntilUtc (+1 more)

### Community 17 - "Voter"
Cohesion: 0.07
Nodes (28): Role, Description, Id, Name, Voters, Voter, AuditLogs, Candidates (+20 more)

### Community 20 - "LoginAttemptStore"
Cohesion: 0.36
Nodes (3): FakeTimeProvider, LoginAttemptStoreTests, LoginAttemptStore

### Community 23 - "WahlMirai.Tests"
Cohesion: 0.18
Nodes (9): coverlet.collector (6.0.4), Microsoft.EntityFrameworkCore.InMemory (9.0.7), Microsoft.EntityFrameworkCore.Relational (9.0.7), Microsoft.NET.Test.Sdk (17.14.1), xunit (2.9.3), xunit.runner.visualstudio (3.1.4), Microsoft.NET.Sdk, WahlMirai.Tests (+1 more)

### Community 24 - ".OnModelCreating"
Cohesion: 0.21
Nodes (8): VwPendingEmailQueue, Attempts, ContactEmail, CreatedAt, EmailType, FullName, Id, VoterId

### Community 25 - "CancellationToken"
Cohesion: 0.20
Nodes (3): FakeCredentialService, IAdminAccountService, ICredentialService

### Community 26 - "ayuda-chatbot.js"
Cohesion: 0.33
Nodes (16): addMessage(), bindEscalationButton(), bindEvents(), bindResolutionButtons(), disableResolutionBlock(), escalateToPqr(), escapeHtml(), findTopicByKeyword() (+8 more)

### Community 27 - "http"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 28 - "Grade"
Cohesion: 0.29
Nodes (7): Grade, EventGrades, Id, IsLastGrade, Name, SequenceOrder, Voters

### Community 29 - "pqr-manage.js"
Cohesion: 0.28
Nodes (14): bindEvents(), closeModal(), escapeHtml(), fetchTickets(), formatDate(), getCsrfToken(), initials(), openModal() (+6 more)

### Community 30 - "IAuthService"
Cohesion: 0.24
Nodes (5): 5.1 M01 — Gestión de Acceso, Auto-registro y Sesión, FakeAuthService, IAuthService, IWhitelistService, WhitelistService

### Community 31 - "VoterDetailDto"
Cohesion: 0.13
Nodes (14): VoterDetailDto, ContactEmail, DeletedAt, Document, ExcluirDePromocion, FullName, GradeId, GradeName (+6 more)

### Community 33 - "AuthController"
Cohesion: 0.18
Nodes (5): AuthController, RegistroStep1ViewModel, Document, VerificarCodigoViewModel, OtpCode

### Community 34 - "CandidacyDocument"
Cohesion: 0.25
Nodes (8): CandidacyDocument, Candidate, CandidateId, FileUrl, Id, Requirement, RequirementId, UploadedAt

### Community 35 - "WhitelistDetailDto"
Cohesion: 0.13
Nodes (13): WhitelistDetailDto, ClaimedAt, ClaimedByUserId, ContactEmail, CreatedAt, Document, DocumentHash, ExcluirDePromocion (+5 more)

### Community 36 - "MyPostulationDto"
Cohesion: 0.05
Nodes (39): 5.4 M04 — Autopostulación y Revisión de Candidaturas, CandidacyService, ICandidacyService, MyPostulationDto, AllowCorrection, ApprovedWithExceptions, CandidateId, EnrolledAt (+31 more)

### Community 37 - "PagedResult"
Cohesion: 0.12
Nodes (15): AdminAccountsIndexViewModel, Accounts, Ascending, Search, SortBy, PagedResult, EndItemIndex, HasNextPage (+7 more)

### Community 38 - "PasswordResetTokenStore"
Cohesion: 0.06
Nodes (26): 1. Arquitectura del Sistema, 5.10 Servicio de Cola de Correo Progresivo (RN-9), ActiveSessionStoreTests, CredentialServiceTests, FailingWahlMiraiDbContext, FailOnSave, FakeActiveSessionStore, LastInvalidatedReason (+18 more)

### Community 39 - "EmailQueue"
Cohesion: 0.20
Nodes (10): EmailQueue, Attempts, CreatedAt, EmailType, ErrorMessage, Id, SentAt, Status (+2 more)

### Community 40 - "VwPendingCandidacy"
Cohesion: 0.18
Nodes (10): VwPendingCandidacy, CandidateId, CandidateName, EnrolledAt, EventTitle, MandatoryDocumentsUploaded, MandatoryRequirements, PositionId (+2 more)

### Community 41 - "project.yml"
Cohesion: 0.08
Nodes (25): activation_command, activation_command_timeout, added_modes, agent_interface, default_modes, encoding, excluded_apis, excluded_tools (+17 more)

### Community 42 - "PqrController"
Cohesion: 0.22
Nodes (6): Comprobación permanente de cobertura i18n y Localización de Alertas/AJAX, Estándar de Localización de Alertas, Modales y Respuestas Asíncronas (AJAX), Herramientas auxiliares y entorno local, Navegación por contexto, Rendimiento del módulo PQR, PqrController

### Community 43 - "PositionRequirement"
Cohesion: 0.12
Nodes (16): ElectionPosition, CreatedAt, Description, Id, Name, PositionRequirements, Status, VotingEvents (+8 more)

### Community 44 - "VwActiveCensu"
Cohesion: 0.22
Nodes (9): VwActiveCensu, ContactEmail, ExcluirDePromocion, FullName, Grade, Id, RegisteredAt, Status (+1 more)

### Community 45 - "VwPendingWhitelist"
Cohesion: 0.18
Nodes (10): VwPendingWhitelist, ContactEmail, CreatedAt, DocumentHash, EncryptedDocument, ExcluirDePromocion, FullName, Grade (+2 more)

### Community 46 - "DESIGN.md"
Cohesion: 0.22
Nodes (8): Brand & Style, Colors, Components, Elevation & Depth, Graphify (knowledge graph para agentes de IA), Layout & Spacing, Shapes, Typography

### Community 47 - "IEmailSender"
Cohesion: 0.20
Nodes (6): TestEmailSender, SecretAvailableAtSend, SentBodies, SentSubjects, IEmailSender, MailKitEmailSender

### Community 48 - "DateTime"
Cohesion: 0.14
Nodes (9): Vote, Candidate, CandidateId, Id, VotedAt, VoteHash, VotingEvent, VotingEventId (+1 more)

### Community 49 - "VwVoteCount"
Cohesion: 0.22
Nodes (9): VwVoteCount, CandidateId, CandidateName, EventId, EventStatus, EventTitle, IsBlankVote, TotalVotes (+1 more)

### Community 50 - "RegistrationOtpStore"
Cohesion: 0.13
Nodes (6): RegistrationOtpStoreTests, IRegistrationOtpStore, RegistrationOtpStore, RegistrationTicketResult, ResendOtpResult, WhitelistCooldownInfo

### Community 51 - "VoterEventParticipation"
Cohesion: 0.20
Nodes (10): 3.1 Mapeo de Entidades C# vs Tablas MySQL, 3.2 Vistas MySQL Mapeadas en Entity Framework Core, 3. Modelo de Datos (17 Tablas Físicas y 5 Vistas MySQL), VoterEventParticipation, Id, ParticipatedAt, Voter, VoterId (+2 more)

### Community 52 - "check-i18n-coverage.ps1"
Cohesion: 0.21
Nodes (5): Add-ExtractedString(), Get-FilterCategory(), Test-UserFacingString(), Get-DictionaryKeys(), Get-MissingTranslations()

### Community 53 - ".ImportCsvAsync"
Cohesion: 0.13
Nodes (13): CsvImportResult, DuplicateCount, ErrorCount, Errors, InsertedCount, ProcessedCount, CsvRowData, Fields (+5 more)

### Community 54 - ".CreateController"
Cohesion: 0.17
Nodes (7): AuthControllerRegistrationTests, FakeUrlHelperFactory, TestWhitelistService, LookupCount, LookupResult, RegistrationCount, RegistrationException

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
Cohesion: 0.28
Nodes (3): ElectorController, ResultsController, IVotingService

### Community 62 - "ESPECIFICACIÓN DE REQUERIMIENTOS DE SOFTWARE"
Cohesion: 0.18
Nodes (9): 2.1 Perspectiva del Producto, 2.2 Funciones Principales del Sistema, 2. Descripción General del Sistema, 3. Reglas de Negocio Transversales, 5. Referencias Bibliográficas, ESPECIFICACIÓN DE REQUERIMIENTOS DE SOFTWARE, Sistema de Votaciones Digitales Estudiantiles, TABLA DE CONTENIDO (+1 more)

### Community 65 - "WahlMirai.Web.ViewModels"
Cohesion: 0.15
Nodes (6): WahlMirai.Web.Controllers, WahlMirai.Web.ViewModels, WahlMirai.Web, ChangePasswordRequest, UpdateEmailRequest, VerifyPasswordRequest

### Community 66 - ".BuildOptions"
Cohesion: 0.08
Nodes (7): WahlMirai.Web.Middleware, FakeAuthenticationService, TestTempDataProvider, ForwardedHeadersConfigurationTests, ForcePasswordChangeMiddleware, ForcePasswordChangeMiddlewareExtensions, ForwardedHeadersConfiguration

### Community 69 - ".Resolve"
Cohesion: 0.15
Nodes (7): ErrorViewModel, ExceptionMessage, Message, ShowHelpButton, ShowHomeButton, StatusCode, Title

### Community 95 - ".GetPromotionPreviewAsync_ReflectsConsolidatedTotals"
Cohesion: 0.33
Nodes (3): 5.2 M02 — Censo Electoral y Lista Blanca (Exclusivo Administrador), IPromotionService, PromotionService

### Community 97 - "PromotionResult"
Cohesion: 0.22
Nodes (9): PromotionResult, ActivePromotedCount, ErrorCount, GraduatedCount, Message, PromotedCount, RetainedCount, Success (+1 more)

### Community 98 - "WahlMirai.Web"
Cohesion: 0.18
Nodes (11): AutoMapper (16.2.0), BCrypt.Net-Next (4.2.0), MailKit (4.17.0), Microsoft.EntityFrameworkCore.Design (9.0.7), Microsoft.EntityFrameworkCore.Tools (9.0.7), Pomelo.EntityFrameworkCore.MySql (9.0.0), Serilog.AspNetCore (10.0.0), Serilog.Sinks.File (7.0.0) (+3 more)

### Community 99 - "RegistroStep2ViewModel"
Cohesion: 0.22
Nodes (9): RegistroStep2ViewModel, AcceptTermsAndPrivacy, ConfirmPassword, ContactEmail, Document, FullName, GradeName, Password (+1 more)

### Community 100 - "Arquitectura y Diseño de Software"
Cohesion: 0.22
Nodes (7): 2. Estructura de Directorios del Proyecto, 4.1 Auto-registro del Elector con Verificación OTP de 3 Pasos (RF-M01-00), 4.2 Autopostulación y Revisión de Candidatura (RF-M04-01, RF-M04-02), 4.3 Proceso de Votación y Escrutinio en Tiempo Real, 4. Diagramas de Flujo de Procesos Clave, Arquitectura y Diseño de Software, Electoral Voting Platform

### Community 101 - "InstitutionSettings"
Cohesion: 0.12
Nodes (15): InstitutionSettings, Address, City, ContactEmail, DataProtectionEmail, HostingProvider, HostingRegion, Name (+7 more)

### Community 104 - "4. Requerimientos Específicos por Módulo"
Cohesion: 0.15
Nodes (13): 4.10 Requerimientos No Funcionales (RNF), 4.5 M05 — Proceso de Votación y Control de Voto Único, 4.6 M06 — Escrutinio y Resultados en Tiempo Real (Acceso Condicionado), 4.8 M08 — Ayuda, Tutorial, PQR y Chatbot, 4.9 M09 — Gestión de Cuentas Administrativas (Exclusivo Súper Administrador), 4. Requerimientos Específicos por Módulo, RF-M05-01 — Emisión de Voto y Control de Sufragio Único, RF-M06-01 — Visualización de Resultados en Tiempo Real Condicionada (+5 more)

### Community 128 - "locale-core.js"
Cohesion: 0.29
Nodes (13): applyLocalizedThemeLabels(), computeTranslation(), flushPendingNodes(), getCulture(), getLanguageData(), getTranslation(), initSelects(), processAddedSubtree() (+5 more)

### Community 132 - ".CreateController"
Cohesion: 0.07
Nodes (27): RecuperacionAccesoControllerTests, ChangePasswordViewModel, ConfirmPassword, NewPassword, LoginViewModel, Document, Password, PqrCreateDto (+19 more)

### Community 133 - "AuditLog"
Cohesion: 0.15
Nodes (13): AuditLog, Action, Details, FieldName, Id, IpAddress, NewValue, OccurredAt (+5 more)

### Community 135 - "AcademicYear"
Cohesion: 0.33
Nodes (6): AcademicYear, CreatedAt, Id, IsCurrent, PromotionExecutedAt, Year

### Community 136 - "1. Introducción"
Cohesion: 0.40
Nodes (5): 1.1 Propósito, 1.2 Alcance del Sistema, 1.3 Definiciones, Acrónimos y Abreviaturas, 1.4 Referencias, 1. Introducción

### Community 140 - "IDocumentEncryptionService"
Cohesion: 0.22
Nodes (3): FakeEncryptionService, DocumentEncryptionService, IDocumentEncryptionService

### Community 141 - "4.2 M02 — Gestión del Censo Electoral (Exclusivo Administrador)"
Cohesion: 0.50
Nodes (4): 4.2 M02 — Gestión del Censo Electoral (Exclusivo Administrador), RF-M02-00 — Carga y Mantenimiento de la Lista Blanca del Censo, RF-M02-01 — Consulta, Modificación y Eliminación Lógica de Usuarios y Lista Blanca, RF-M02-02 — Promoción Automática de Año Lectivo

### Community 142 - "4.3 M03 — Gestión de Elecciones, Cargos y Etapas"
Cohesion: 0.50
Nodes (4): 4.3 M03 — Gestión de Elecciones, Cargos y Etapas, RF-M03-00 — Catálogo de Cargos Electorales y sus Requisitos, RF-M03-01 — Creación y Parametrización de Elecciones por Etapas, RF-M03-02 — Edición y Eliminación Lógica de Procesos Electorales

### Community 144 - "Directrices y Reglas del Proyecto WAHL-MIRAI"
Cohesion: 0.50
Nodes (3): Directrices y Reglas del Proyecto WAHL-MIRAI, Política de Actualización del Grafo de Conocimiento (Graphify), Reglas de aplicación:

### Community 145 - "4.4 M04 — Autopostulación y Aprobación de Candidatos"
Cohesion: 0.67
Nodes (3): 4.4 M04 — Autopostulación y Aprobación de Candidatos, RF-M04-01 — Autopostulación de Candidatos con Documentos de Requisito, RF-M04-02 — Aprobación o Rechazo de Candidaturas por el Administrador

### Community 146 - "AdminAccountListItem"
Cohesion: 0.17
Nodes (10): AdminAccountListItem, ContactEmail, DeletedAt, Document, FullName, Id, PositionTitle, RegisteredAt (+2 more)

### Community 147 - "TestContext"
Cohesion: 0.18
Nodes (5): TestContext, EmailSender, PasswordStore, Worker, TestWahlMiraiDbContext

### Community 149 - "Controller"
Cohesion: 0.16
Nodes (5): Sistema de Votaciones Digitales (Wahl Mirai) — Versión 3.0 (Sincronizada con Implementación Real), AdminEmailReportController, ErrorController, HomeController, RecuperacionAccesoController

### Community 150 - "List"
Cohesion: 0.17
Nodes (7): RequirementCheckDto, Description, FileUrl, IsMandatory, IsUploaded, RequirementId, UploadedAt

### Community 151 - "5. Matriz de Módulos, Controladores y Reglas de Negocio"
Cohesion: 0.25
Nodes (8): 5.11 Control Dinámico de Etapas Electorales (RN-12), 5.3 M03 — Gestión de Elecciones, Cargos y Etapas, 5.5 M05 — Proceso de Votación y Control de Voto Único, 5.6 M06 — Escrutinio y Resultados en Tiempo Real, 5.8 M08 — Ayuda, Chatbot FAQ y PQR, 5.9 M09 — Cuentas Administrativas (Exclusivo Súper Administrador), 5. Matriz de Módulos, Controladores y Reglas de Negocio, On the horizon

### Community 152 - "EventGrade"
Cohesion: 0.33
Nodes (6): EventGrade, Grade, GradeId, Id, VotingEvent, VotingEventId

### Community 153 - "Serena memory maintenance"
Cohesion: 0.40
Nodes (4): Discovery model, Maintenance, Serena memory maintenance, Style and scope

### Community 154 - "EmailType"
Cohesion: 0.12
Nodes (10): EmailRetryPolicyTests, StatusAndTypeCases, EmailRetryPolicy, EmailType, CAMBIO_PERFIL, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA, REASIGNACION_ADMIN (+2 more)

### Community 155 - "4.7 M07 — Perfil de Usuario y Autogestión de Credenciales"
Cohesion: 0.67
Nodes (3): 4.7 M07 — Perfil de Usuario y Autogestión de Credenciales, RF-M07-01 — Consulta y Edición de Perfil Propio, RF-M07-02 — Reasignación de Contraseña por el Administrador

### Community 159 - "CandidateProposal"
Cohesion: 0.33
Nodes (6): CandidateProposal, Candidate, CandidateId, Content, DisplayOrder, Id

### Community 164 - ".SearchVoterAsync"
Cohesion: 0.22
Nodes (5): VoterSearchResultDto, Documento, FullName, GradeName, Id

### Community 168 - "WhitelistStatsDto"
Cohesion: 0.40
Nodes (4): WhitelistStatsDto, TotalClaimed, TotalPending, TotalUploaded

## Knowledge Gaps
- **577 isolated node(s):** `LookupCount`, `RegistrationCount`, `RegistrationException`, `LookupResult`, `ActionContext` (+572 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 800 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **92 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `WahlMiraiDbContext` connect `WahlMiraiDbContext` to `WahlMirai.Web.Models`, `ValidateAntiForgeryToken`, `EmailQueueBackgroundService`, `.CreateController`, `AdminEventsController`, `.CreateService`, `AcademicYear`, `AuditLog`, `Fact`, `Candidate`, `CensusWhitelist`, `VotingEvent`, `Task`, `PqrTicket`, `Voter`, `IActionResult`, `.CreateCensusService`, `TestContext`, `EventService`, `Controller`, `EventGrade`, `.OnModelCreating`, `Grade`, `IAuthService`, `CandidateProposal`, `CandidacyDocument`, `MyPostulationDto`, `PasswordResetTokenStore`, `EmailQueue`, `VwPendingCandidacy`, `PqrController`, `PositionRequirement`, `VwActiveCensu`, `VwPendingWhitelist`, `DateTime`, `VwVoteCount`, `VoterEventParticipation`, `.CreateTestContextAsync`, `IVotingService`, `AdminAccountService`, `.GetPromotionPreviewAsync_ReflectsConsolidatedTotals`?**
  _High betweenness centrality (0.097) - this node is a cross-community bridge._
- **Are the 65 inferred relationships involving `FakeAuditService` (e.g. with `.CreateAdmin_StoresAccountAndInitialCredentialQueue()` and `.CreateService()`) actually correct?**
  _`FakeAuditService` has 65 INFERRED edges - model-reasoned connections that need verification._
- **What connects `LookupCount`, `RegistrationCount`, `RegistrationException` to the rest of the system?**
  _577 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `CandidateReviewDetailDto` be split into smaller, more focused modules?**
  _Cohesion score 0.08695652173913043 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Models` connect `WahlMirai.Web.Models` to `WahlMirai.Web.ViewModels`, `CensusWhitelist`, `InstitutionSettings`, `AuthControllerRegistrationTests.cs`, `VwPendingCandidacy`, `MailKitEmailSender.cs`, `Program.cs`, `VwPendingWhitelist`, `EmailSettings`, `WahlMirai.Web.Services`?**
  _High betweenness centrality (0.057) - this node is a cross-community bridge._
- **Should `CensusWhitelist` be split into smaller, more focused modules?**
  _Cohesion score 0.10822510822510822 - nodes in this community are weakly interconnected._
- **Why does `WahlMirai.Web.Services` connect `WahlMirai.Web.Services` to `WahlMirai.Web.Models`, `WahlMirai.Web.ViewModels`, `PasswordResetTokenStore`, `AuthControllerRegistrationTests.cs`, `MailKitEmailSender.cs`, `Fact`, `Program.cs`, `AdminAccountFormViewModel`, `IDocumentEncryptionService`, `IEmailSender`, `DateTimeOffset`, `RegistrationOtpStore`, `EmailType`?**
  _High betweenness centrality (0.042) - this node is a cross-community bridge._