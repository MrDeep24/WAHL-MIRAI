# Manual Técnico — Diagramas del Sistema Wahl Mirai
## Versión 3.1 (Alineada con Manual Técnico de Referencia SIGA-UNI y ERS v3.1)

Este documento consolida y estandariza las **5 Figuras y el Diccionario de Datos** exigidos por la plantilla de especificación técnica institucional (Manual Técnico SIGA-UNI, Secciones 2 y 3), reflejando con exactitud la arquitectura real implementada en **ASP.NET Core 9.0 MVC**, C# 13, MySQL 8.0+ y Entity Framework Core 9.

---

### Matriz de Correspondencia con el Manual Técnico de Referencia

| Figura / Sección | Exigencia del Manual (SIGA-UNI) | Estado Wahl Mirai v3.1 | Archivo Independiente |
| :--- | :--- | :--- | :--- |
| **Figura 1** | **Arquitectura General y Despliegue** (Topología de red, nodos físicos, proxy, puertos, almacenamiento) | **Completado (Ajustado a despliegue físico)** | [figura_1_arquitectura_y_despliegue_v3_1.mmd](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_1_arquitectura_y_despliegue_v3_1.mmd) |
| **Figura 2** | **Diagrama de Casos de Uso por Rol** (4 Actores: Visitante, Elector, Administrador, Súper Admin) | **Completado (Elaborado)** | [figura_2_casos_de_uso_por_rol_v3_1.mmd](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_2_casos_de_uso_por_rol_v3_1.mmd) |
| **Figura 3** | **Diagrama de Componentes y Dependencias** (Capa Cliente, Backend MVC + RAM + Dominio M01-M09, Persistencia) | **Completado (Elaborado)** | [figura_3_componentes_y_dependencias_v3_1.mmd](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_3_componentes_y_dependencias_v3_1.mmd) |
| **Figura 4** | **Diagramas de Secuencia de Procesos Clave** (Auto-registro OTP, Autopostulación, Votación y Escrutinio) | **Completado (3 flujos formalizados)** | [figura_4_1_secuencia_autoregistro_otp_v3_1.mmd](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_4_1_secuencia_autoregistro_otp_v3_1.mmd)<br/>[figura_4_2_secuencia_autopostulacion_v3_1.mmd](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_4_2_secuencia_autopostulacion_v3_1.mmd)<br/>[figura_4_3_secuencia_votacion_escrutinio_v3_1.mmd](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_4_3_secuencia_votacion_escrutinio_v3_1.mmd) |
| **Figura 5** | **Diagrama Entidad-Relación (DER)** (17 tablas físicas + relaciones íntegras) | **Completado** | [figura_5_entidad_relacion_v3_1.mmd](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/figura_5_entidad_relacion_v3_1.mmd) |
| **Sección 3.2** | **Diccionario de Datos Estructurado** (Tabla, Campo, Tipo, Longitud, Llave, Descripción) | **Completado (17 tablas + 5 vistas)** | [diccionario_de_datos_v3_1.md](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/diccionario_de_datos_v3_1.md) |

---

## 1. Figura 1: Arquitectura General y Despliegue Físico (Sección 2.1 / 2.3)

La Figura 1 ilustra la topología de red y los nodos físicos/lógicos de infraestructura sobre los que opera el sistema:
1. **Nodo Cliente:** Dispositivos de usuario final (escritorio y móviles) que consumen la aplicación web mediante HTTPS (puerto 443 TCP) y canales WebSockets persistentes (`wss://` hacia `/hubs/resultsHub`).
2. **Capa Perimetral (Proxy Inverso):** Servidor Nginx o Apache que gestiona la terminación SSL/TLS, mitiga ataques DDoS y reenvía el tráfico con preservación de cabeceras estándar (`X-Forwarded-For`, `X-Forwarded-Proto`, `X-Forwarded-Host`) hacia el host de aplicaciones.
3. **Servidor de Aplicaciones (.NET 9.0 Host):** Proceso Kestrel ejecutando `WahlMirai.Web`, conteniendo el motor MVC, los almacenes volátiles singleton en memoria RAM (`IActiveSessionStore`, `IRegistrationOtpStore`, `ILoginAttemptStore`, `IPasswordResetTokenStore`), el worker en segundo plano `EmailQueueBackgroundService`, y el sistema de archivos local protegido fuera de `wwwroot` (`App_Data/candidacy-uploads/candidacies/` para binarios cifrados `.enc` y la carpeta `keys/` para llaves maestras AES-256).
4. **Capa de Persistencia y Servicios Externos:** Servidor MySQL 8.0+ escuchando en el puerto 3306 TCP (con base de datos `wahl_mirai_db`) y Servidor SMTP institucional o relay externo conectado mediante STARTTLS (puerto 587 TCP).

```mermaid
flowchart TD
    subgraph ClientLayer ["Nodo Cliente / Dispositivos de Usuario"]
        DeskBrowser["Navegador Web Escritorio<br/>(Chrome, Firefox, Edge, Safari)"]
        MobileBrowser["Navegador Web Móvil<br/>(Chrome Android, Safari iOS)"]
    end

    subgraph DMZLayer ["Capa Perimetral / Proxy Inverso (Recomendado / Producción)"]
        ReverseProxy["Servidor Proxy Inverso (Nginx / Apache)<br/>Terminación SSL/TLS (Puerto 443 TCP)<br/>Reenvío de Cabeceras:<br/>• X-Forwarded-For<br/>• X-Forwarded-Proto<br/>• X-Forwarded-Host<br/>Proxy Pass WebSockets (/hubs/resultsHub)"]
    end

    subgraph AppServerNode ["Nodo Servidor de Aplicaciones (.NET 9.0 Host / Kestrel)"]
        subgraph AppRuntime ["Runtime ASP.NET Core 9.0 (Kestrel Web Server)"]
            AppEngine["WahlMirai.Web Engine (C# 13 / .NET 9.0 MVC)<br/>• Pipeline Middleware & ForwardedHeaders<br/>• RateLimiting (AuthLogin, AuthRegister, AuthRecovery)<br/>• CookieAuthenticationEvents.OnValidatePrincipal"]
            SignalREndpoint["Endpoint SignalR Hub<br/>(/hubs/resultsHub)<br/>Transmisión en vivo de escrutinio"]
            BackgroundWorker["Worker Asíncrono en Segundo Plano<br/>(EmailQueueBackgroundService : IHostedService)<br/>Despacho controlado 1-60 correos/min (RN-9)"]
        end

        subgraph RAMState ["Almacenes de Estado Volátil en Memoria RAM (Singleton)"]
            ActiveSessionStore["IActiveSessionStore<br/>Control de Sesión Única Activa (RN-14)<br/>ConcurrentDictionary&lt;uint, SessionEntry&gt;"]
            OtpStore["IRegistrationOtpStore<br/>Tickets SHA-256 + OTP 6 dígitos (TTL 15 min)<br/>Límite 5 intentos + Cooldown 60 s (RF-M01-00)"]
            LockoutStore["ILoginAttemptStore<br/>Protección Brute-Force (RN-15)<br/>Buckets IP por ClientIpKey"]
            ResetTokenStore["IPasswordResetTokenStore / IPendingPasswordStore<br/>Tokens efímeros de recuperación y contraseñas temporales"]
        end

        subgraph ProtectedStorage ["Sistema de Archivos Local Protegido (Fuera de wwwroot)"]
            EncryptedDocs["App_Data/candidacy-uploads/candidacies/<br/>Archivos Cifrados *.enc (Fotos, Planes, Soportes)<br/>Acceso exclusivo vía /AdminCandidates/ServeDocument"]
            DataProtectionKeys["keys/<br/>Repositorio de Llaves Criptográficas XML<br/>(AES-256 ASP.NET Core Data Protection)"]
        end
    end

    subgraph DatabaseNode ["Nodo de Base de Datos Relacional (MySQL / XAMPP)"]
        MySQLInstance["Servidor MySQL 8.0+ / MariaDB (Puerto 3306 TCP)<br/>Base de Datos: wahl_mirai_db<br/>• 17 Tablas Físicas Normalizadas (InnoDB / utf8mb4)<br/>• 5 Vistas SQL Optimizadas (Keyless EF Core)"]
    end

    subgraph ExternalRelay ["Servicios Externos de Red"]
        SMTPServer["Servidor SMTP Institucional / Relay Externo<br/>Puertos 587 (STARTTLS) / 465 (TLS/SSL)<br/>Despacho transaccional de correos"]
    end

    DeskBrowser -->|"HTTPS / TLS 1.3 (Puerto 443 TCP)"| ReverseProxy
    DeskBrowser -.->|"WSS / WebSockets (Puerto 443 TCP)"| ReverseProxy
    MobileBrowser -->|"HTTPS / TLS 1.3 (Puerto 443 TCP)"| ReverseProxy
    MobileBrowser -.->|"WSS / WebSockets (Puerto 443 TCP)"| ReverseProxy

    ReverseProxy -->|"HTTP / Loopback (Puerto 5000/5001 o Unix Socket)<br/>Cabeceras X-Forwarded-*"| AppEngine
    ReverseProxy -.->|"Upgrade: websocket<br/>Proxy Pass a /hubs/resultsHub"| SignalREndpoint

    AppEngine <-->|"In-Memory (Inyección de Dependencias / Singleton)"| RAMState
    AppEngine <-->|"Lectura / Escritura de Cifrado (File I/O Stream)"| ProtectedStorage
    AppEngine -.->|"Disparo de Evento de Escrutinio"| SignalREndpoint
    BackgroundWorker <-->|"Consulta de Secretos / Tokens en RAM"| RAMState

    AppEngine -->|"TCP / IP (Puerto 3306)<br/>Protocolo MySQL nativo (Pomelo EF Core 9)"| MySQLInstance
    BackgroundWorker -->|"TCP / IP (Puerto 3306)<br/>Lectura vw_pending_email_queue y UPDATE status"| MySQLInstance

    AppEngine -->|"SMTP / STARTTLS (Puerto 587 TCP)<br/>Despacho Síncrono OTP de Auto-registro"| SMTPServer
    BackgroundWorker -->|"SMTP / STARTTLS (Puerto 587 TCP)<br/>Despacho Asíncrono MailKitEmailSender"| SMTPServer
```

---

## 2. Figura 2: Diagrama de Casos de Uso por Rol (Sección 2.3)

La Figura 2 modela los 4 actores del sistema y su relación con los casos de uso principales definidos en el ERS v3.1:
1. **Visitante / Usuario Anónimo:** Consulta centro de ayuda e infografías, interactúa con el Chatbot FAQ por reglas, solicita recuperación de acceso con enlace de token efímero y ejecuta el auto-registro en 3 pasos con validación OTP (RF-M01-00).
2. **Elector (`ELECTOR`):** Inicia sesión validando sesión única (RN-14), consulta propuestas en etapa activa, emite voto secreto y único (M05, RN-3), se autopostula a cargos cargando soportes cifrados (M04), consulta resultados en vivo condicionado (RN-4/4.1), autogestiona su perfil (M07) y radica tickets PQR (M08).
3. **Administrador (`ADMIN`):** Administra lista blanca manual o masiva por CSV y censo activo (M02), ejecuta la promoción anual de grado (RF-M02-02), configura cargos, requisitos y elecciones por etapas (M03), revisa, aprueba (con/sin excepción) o rechaza candidaturas (M04), consulta escrutinio irrestricto en tiempo real (RN-5), atiende y resuelve PQR (M08) y supervisa la cola de correos (RN-9).
4. **Súper Administrador (`SUPER_ADMIN`):** Hereda todas las facultades de `ADMIN` y gestiona de forma exclusiva las cuentas administrativas (creación, edición de cargo institucional y baja lógica sin auto-eliminación, M09 / RN-13).

```mermaid
flowchart LR
    subgraph Actores ["Actores del Sistema"]
        direction TB
        Anon["fa:fa-user Visitante / Anónimo"]
        Elector["fa:fa-user-graduate Elector (ELECTOR)"]
        Admin["fa:fa-user-tie Administrador (ADMIN)"]
        SuperAdmin["fa:fa-user-shield Súper Administrador (SUPER_ADMIN)"]
    end

    SuperAdmin -->|Hereda permisos operativos| Admin

    subgraph WahlMiraiSystem ["Sistema Electoral Wahl Mirai — Casos de Uso por Dominio"]

        subgraph SubPublico ["Acceso Público y Soporte Inicial (M01 / M08)"]
            CU_Ayuda["(CU-01) Consultar centro de ayuda e infografías (/Ayuda)"]
            CU_Chatbot["(CU-02) Interactuar con Chatbot FAQ por reglas"]
            CU_Recuperar["(CU-03) Solicitar recuperación de acceso con token efímero"]
            CU_AutoRegistro["(CU-04) Auto-registro en 3 pasos con validación OTP (RF-M01-00)"]
        end

        subgraph SubElector ["Participación Electoral y Autogestión (M01, M04-M08)"]
            CU_Login["(CU-05) Iniciar sesión con documento y validar sesión única (RN-14)"]
            CU_Propuestas["(CU-06) Consultar propuestas y planes de gobierno en etapa activa"]
            CU_Votar["(CU-07) Emitir voto secreto y único con hash SHA-256 (M05 / RN-3)"]
            CU_Postularse["(CU-08) Autopostularse cargando soportes y plan en PDF (M04 / RN-11)"]
            CU_ResultadosElector["(CU-09) Consultar resultados en vivo condicionado (RN-4 / RN-4.1)"]
            CU_Perfil["(CU-10) Autogestionar correo y cambiar contraseña en perfil (M07)"]
            CU_PqrElector["(CU-11) Radicar ticket y consultar historial personal de PQR (M08)"]
        end

        subgraph SubAdmin ["Administración y Operación Electoral (M02, M03, M04, M06, M08)"]
            CU_Whitelist["(CU-12) Gestionar lista blanca (individual / CSV RFC-4180) y censo activo (M02)"]
            CU_Promocion["(CU-13) Ejecutar promoción anual de grado escolar (RF-M02-02)"]
            CU_Elecciones["(CU-14) Configurar cargos, requisitos y elecciones por etapas (M03 / RN-12)"]
            CU_RevisarCand["(CU-15) Revisar, aprobar (con/sin excepción) o rechazar candidaturas (M04 / RN-10)"]
            CU_EscrutinioAdmin["(CU-16) Visualizar escrutinio irrestricto en vivo (RN-5 / M06)"]
            CU_ResponderPqr["(CU-17) Responder y cerrar tickets PQR con resolución oficial (M08)"]
            CU_ColaCorreos["(CU-18) Monitorear y reintentar despachos en cola de correos (RN-9)"]
        end

        subgraph SubSuperAdmin ["Alta Administración y Gobierno del Sistema (M09)"]
            CU_CuentasAdmin["(CU-19) Gestionar cuentas de administradores (crear, cargo cerrado y soft-delete) (M09 / RN-13)"]
        end
    end

    Anon --> CU_Ayuda
    Anon --> CU_Chatbot
    Anon --> CU_Recuperar
    Anon --> CU_AutoRegistro

    Elector --> CU_Login
    Elector --> CU_Propuestas
    Elector --> CU_Votar
    Elector --> CU_Postularse
    Elector --> CU_ResultadosElector
    Elector --> CU_Perfil
    Elector --> CU_PqrElector

    Admin --> CU_Whitelist
    Admin --> CU_Promocion
    Admin --> CU_Elecciones
    Admin --> CU_RevisarCand
    Admin --> CU_EscrutinioAdmin
    Admin --> CU_ResponderPqr
    Admin --> CU_ColaCorreos

    SuperAdmin --> CU_CuentasAdmin
```

---

## 3. Figura 3: Diagrama de Componentes y Dependencias (Sección 2.3)

La Figura 3 estructura los componentes de software en tres bloques jerárquicos:
1. **Capa Cliente / Presentación:** Layouts por contexto (`_Layout`, `_ElectorLayout`, `_AdminLayout`), motor de internacionalización (`locale-core.js` + diccionarios en 6 idiomas), gestor de temas (`theme.js`), motor de reglas FAQ (`ayuda-chatbot.js`), cliente SignalR y scripts reactivos AJAX (`pqr-manage.js`, `auth-completar.js`).
2. **Capa de Back-end (ASP.NET Core 9.0):**
   * **Pipeline y Seguridad:** `ForwardedHeaders`, `RateLimiting` (`ClientIpKey`), `CookieAuthenticationEvents.OnValidatePrincipal` y almacenes volátiles en memoria RAM (`IActiveSessionStore`, `ILoginAttemptStore`, `IRegistrationOtpStore`, `IPasswordResetTokenStore`).
   * **Módulos de Dominio (Controladores + Servicios):** M01 a M09 y el worker asíncrono `EmailQueueBackgroundService`.
   * **Servicios Transversales:** `AuditService` (trazabilidad forense; consumido por todos excepto PQR), `DocumentEncryptionService` (cifrado AES-256 Data Protection) y `MailKitEmailSender`.
3. **Capa de Persistencia y Datos:** Contexto `WahlMiraiDbContext` (EF Core 9 con Pomelo MySQL Provider) conectado al esquema `wahl_mirai_db` (17 tablas físicas y 5 vistas keyless).

```mermaid
flowchart TD
    subgraph ClientPresentationLayer ["Capa de Presentación / Cliente (Navegador Web + Razor Engine)"]
        subgraph LayoutsView ["Layouts de Navegación por Contexto"]
            LayoutPublic["_Layout.cshtml<br/>(Cabecera global, selector idioma, tema, acceso público)"]
            LayoutElector["_ElectorLayout.cshtml<br/>(Navegación contextual para el Elector)"]
            LayoutAdmin["_AdminLayout.cshtml<br/>(Panel lateral y dashboard para Administradores)"]
        end

        subgraph ClientScripts ["Componentes Frontend (Vanilla JS & SignalR Client)"]
            I18nEngine["Motor i18n Cliente<br/>(locale-core.js + locale.&lt;lang&gt;.js en 6 idiomas)"]
            ThemeManager["Gestor de Tema<br/>(theme.js - Dark/Light con localStorage)"]
            ChatbotEngine["Motor Chatbot FAQ<br/>(ayuda-chatbot.js - Reglas sin IA + Lightbox)"]
            SignalRClient["Cliente SignalR<br/>(resultsHub client - Suscripción ReceiveResultsUpdate)"]
            AjaxModules["Scripts AJAX Reactivos<br/>(pqr-manage.js, auth-completar.js)"]
        end
    end

    subgraph BackendAppLayer ["Capa de Back-end (ASP.NET Core 9.0 MVC Host)"]

        subgraph SecurityPipeline ["Pipeline de Solicitudes y Seguridad Transversal"]
            ForwardedHdrs["ForwardedHeaders Middleware<br/>(Procesamiento de X-Forwarded-For y Proto)"]
            RateLimiter["Motor RateLimiting<br/>(AuthLogin, AuthRegister, AuthRecovery con ClientIpKey)"]
            CookieAuth["Autenticación por Cookies y Claims<br/>CookieAuthenticationEvents.OnValidatePrincipal (RN-14)"]

            subgraph RAMStores ["Almacenes en Memoria RAM (Singleton / ConcurrentDictionary / IMemoryCache)"]
                ActiveSessionStore["IActiveSessionStore (Sesión Única Activa)"]
                LoginAttemptStore["ILoginAttemptStore (Control Lockout por IP)"]
                RegOtpStore["IRegistrationOtpStore (OTP + Tickets SHA-256)"]
                ResetTokenStore["IPasswordResetTokenStore / IPendingPasswordStore"]
            end
        end

        subgraph DomainModules ["Módulos de Dominio (Controladores MVC + Servicios Especializados)"]
            M01["M01: Autenticación, Registro y Recuperación<br/>[AuthController, RecuperacionAccesoController]<br/>(AuthService, CredentialService)"]
            M02["M02: Censo Electoral y Lista Blanca<br/>[AdminCensusController]<br/>(CensusService, WhitelistService, PromotionService)"]
            M03["M03: Eventos Electorales y Cargos<br/>[AdminEventsController]<br/>(EventService)"]
            M04["M04: Autopostulación y Revisión Candidaturas<br/>[CandidacyController, AdminCandidatesController]<br/>(CandidacyService, CandidateReviewService)"]
            M05["M05: Votación y Control de Voto Único<br/>[ElectorController]<br/>(VotingService)"]
            M06["M06: Escrutinio y Resultados en Vivo<br/>[ResultsController, ResultsHub (SignalR)]"]
            M07["M07: Perfil de Usuario y Autogestión<br/>[ProfileController]<br/>(ProfileService)"]
            M08["M08: Ayuda, Chatbot FAQ y Gestión PQR<br/>[PqrController]<br/>(PqrService)"]
            M09["M09: Cuentas Administrativas<br/>[AdminAccountsController]<br/>(AdminAccountService)"]
            WorkerEmail["Worker Asíncrono en Segundo Plano<br/>[EmailQueueBackgroundService (IHostedService)]"]
        end

        subgraph CrossCuttingServices ["Servicios Transversales Internos"]
            AuditService["AuditService<br/>(Trazabilidad forense en tabla audit_log)"]
            CryptoService["DocumentEncryptionService<br/>(Cifrado AES-256 ASP.NET Data Protection para .enc)"]
            EmailSender["MailKitEmailSender (IEmailSender)<br/>(Despacho síncrono/asíncrono SMTP/STARTTLS)"]
        end
    end

    subgraph DataPersistenceLayer ["Capa de Datos y Persistencia (MySQL Server)"]
        DbContext["WahlMiraiDbContext<br/>(Entity Framework Core 9.0 + Pomelo MySQL Provider)<br/>Configuración SplitQuery"]

        subgraph MySQLSchema ["wahl_mirai_db (Esquema Relacional)"]
            PhysicalTables["17 Tablas Físicas Normalizadas<br/>(users, roles, grades, academic_years, census_whitelist,<br/>voting_events, election_positions, position_requirements,<br/>event_grades, candidates, candidate_proposals, candidacy_documents,<br/>votes, event_participations, audit_log, pqr_tickets, email_queue)"]
            SqlViews["5 Vistas MySQL Mapeadas Keyless<br/>(vw_vote_counts, vw_active_census, vw_pending_whitelist,<br/>vw_pending_email_queue, vw_pending_candidacies)"]
        end
    end

    ClientPresentationLayer -->|"Peticiones HTTP/HTTPS & AJAX"| ForwardedHdrs
    ForwardedHdrs --> RateLimiter
    RateLimiter --> CookieAuth
    CookieAuth --> DomainModules
    CookieAuth <-->|"Validación de SessionStamp en RAM"| ActiveSessionStore

    DomainModules <-->|"Control de Intentos y Lockout"| LoginAttemptStore
    M01 <-->|"Gestión OTP y Tickets"| RegOtpStore
    M01 <-->|"Tokens y Passwords Efímeros"| ResetTokenStore
    M01 <-->|"Registro / Revocación Sesión"| ActiveSessionStore
    M02 <-->|"Revocación Inmediata de Sesión (RN-14)"| ActiveSessionStore
    M07 <-->|"Revocación Inmediata de Sesión (RN-14)"| ActiveSessionStore
    M09 <-->|"Revocación Inmediata de Sesión (RN-14)"| ActiveSessionStore

    M01 --> AuditService
    M02 --> AuditService
    M03 --> AuditService
    M04 --> AuditService
    M05 --> AuditService
    M06 --> AuditService
    M07 --> AuditService
    M09 --> AuditService

    M02 --> CryptoService
    M04 --> CryptoService
    M07 --> CryptoService

    M01 --> EmailSender
    M02 --> EmailSender
    WorkerEmail --> EmailSender

    SignalRClient <-->|"WebSockets (WSS)"| M06
    M05 -.->|"Notificación de nuevo voto"| M06

    DomainModules --> DbContext
    WorkerEmail --> DbContext
    AuditService --> DbContext

    DbContext --> PhysicalTables
    DbContext --> SqlViews
```

---

## 4. Figura 4: Diagramas de Secuencia de Procesos Clave (Sección 2.3)

### 4.1 Secuencia de Auto-registro con OTP en 3 Pasos (RF-M01-00)
```mermaid
sequenceDiagram
    actor Elector as Estudiante / Elector
    participant Auth as AuthController
    participant WS as WhitelistService
    participant OTP as IRegistrationOtpStore
    participant Mail as IEmailSender (SMTP)
    participant DB as MySQL (wahl_mirai_db)

    Note over Elector, DB: Paso 1: Validación de Documento y Emisión de OTP
    Elector->>Auth: GET /Auth/Registro
    Elector->>Auth: POST /Auth/Registro (Documento)
    Auth->>WS: GetEntryByDocumentHashAsync(documentHash)
    WS->>DB: SELECT * FROM census_whitelist WHERE document_hash = ?
    alt Documento no encontrado
        WS-->>Auth: null
        Auth-->>Elector: HTTP 200 View(NotFound) + ModelState ("no se encuentra registrado")
    else Documento ya reclamado (claimed_at != null)
        WS-->>Auth: entry (ClaimedAt != null)
        Auth-->>Elector: HTTP 200 View(AlreadyClaimed) + Enlaces a Login / Recuperar
    else Sin correo de contacto (contact_email null o vacío)
        WS-->>Auth: entry (ContactEmail vacío)
        Auth-->>Elector: HTTP 200 View(NoContactEmail) + Remisión a Coordinación
    else Entrada válida y no reclamada
        WS-->>Auth: entry válida (id, full_name, contact_email)
        Auth->>OTP: IssueRegistrationTicket(doc, id, email, hash, currentTicket, fullName)
        alt Cooldown activo (solicitud dentro de 60 s)
            alt Con cookie wm_reg_ticket de la misma entrada
                OTP-->>Auth: HasValidSessionForEntry = true
                Auth-->>Elector: HTTP 302 Redirect /Auth/VerificarCodigo (Conserva cookie)
            else Sin cookie o de otra entrada
                OTP-->>Auth: HasValidSessionForEntry = false, CooldownRemainingSeconds
                Auth-->>Elector: HTTP 200 View(Cooldown) con segundos restantes (No suma a bucket IP)
            end
        else Cooldown superado o primera emisión
            OTP-->>Auth: ticketResult (ticket, rawOtp)
            Auth->>Mail: SendAsync(contactEmail, "Código de verificación...", rawOtp)
            alt Falla de envío SMTP (excepción)
                Mail-->>Auth: Excepción SMTP
                Auth->>OTP: Revoke(ticket) (Descarta ticket y libera cooldown)
                Auth-->>Elector: HTTP 200 View(SendFailed) "intenta en unos minutos" (No suma a bucket IP)
            else Envío exitoso
                Mail-->>Auth: OK
                Auth-->>Elector: HTTP 302 Set-Cookie: wm_reg_ticket -> Redirect /Auth/VerificarCodigo
            end
        end
    end

    Note over Elector, DB: Paso 2: Verificación del Código OTP de 6 Dígitos
    Elector->>Auth: GET /Auth/VerificarCodigo (Lee cookie wm_reg_ticket)
    Auth->>OTP: TryGetTicketDisplayInfo(ticket, out maskedName, out maskedEmail)
    Auth-->>Elector: Renderiza formulario con MaskedFullName (ej. "Ke*** Ló***") y MaskedEmail
    Elector->>Auth: POST /Auth/VerificarCodigo (OtpCode)
    Auth->>OTP: TryValidateOtp(ticket, otpCode, out error, out isLockoutEligible)
    alt Código válido
        OTP-->>Auth: true (Transiciona ticket a IsVerified = true)
        Auth-->>Elector: HTTP 302 Redirect /Auth/Completar
    else Código incorrecto o expirado
        OTP-->>Auth: false (Incrementa contador de fallos de ticket y bucket IP register:{clientIpKey})
        Auth-->>Elector: HTTP 200 View con error e intentos restantes
    end

    Note over Elector, DB: Paso 3: Asignación de Contraseña y Auto-login
    Elector->>Auth: GET /Auth/Completar (Valida ticket verificado)
    Auth->>OTP: TryGetVerifiedTicket(ticket, out whitelistId, out document)
    Auth->>WS: GetUnclaimedEntryByIdAsync(whitelistId)
    Auth-->>Elector: Renderiza formulario (Nombre completo y grado en solo lectura, términos y privacidad)
    Elector->>Auth: POST /Auth/Completar (Password, ConfirmPassword, AcceptTermsAndPrivacy)
    Auth->>OTP: TryConsumeVerifiedTicket(ticket, out whitelistId, out document) (Consumo único atómico)
    Auth->>WS: RegisterElectorAsync(document, whitelistId, email, password, ip)
    WS->>DB: Transacción: INSERT users + UPDATE census_whitelist.claimed_at + INSERT audit_log
    Auth-->>Elector: HTTP 302 Set-Cookie: .AspNetCore.Cookies (Claims) + Expira wm_reg_ticket -> Redirect /Elector/Dashboard
```

### 4.2 Secuencia de Autopostulación y Revisión de Candidaturas (RF-M04-01, RF-M04-02)
```mermaid
sequenceDiagram
    actor Elector
    participant Cand as CandidacyController
    participant CS as CandidacyService
    participant Disk as App_Data (Cifrado .enc)
    actor Admin as ADMIN / SUPER_ADMIN
    participant Rev as AdminCandidatesController
    participant CRS as CandidateReviewService
    participant DB as MySQL (wahl_mirai_db)

    Note over Elector, DB: Elección en etapa INSCRIPCION (RN-12)
    Elector->>Cand: GET /Candidacy/Apply/{eventId}
    Cand->>CS: GetPostulationFormDetailAsync(eventId, userId)
    CS-->>Cand: Requisitos obligatorios y opcionales del cargo
    Cand-->>Elector: Formulario: slogan, foto, plan gobierno (PDF) y soportes
    Elector->>Cand: POST /Candidacy/Apply (Multipart Form)
    Cand->>CS: SubmitPostulationAsync(dto, userId, ip)
    CS->>Disk: Guarda foto, plan y soportes cifrados (.enc) con Data Protection
    CS->>DB: INSERT INTO candidates (status='PENDIENTE', allow_correction=0)
    CS->>DB: INSERT INTO candidate_proposals (...)
    CS->>DB: INSERT INTO candidacy_documents (...)
    CS-->>Elector: Postulación en revisión (RN-10) -> Redirect /Candidacy/Status/{id}

    Note over Admin, DB: Revisión Administrativa
    Admin->>Rev: GET /AdminCandidates/GetCandidateDetail(id)
    Rev->>CRS: GetCandidateReviewDetailAsync(candidateId)
    CRS->>DB: Consulta vw_pending_candidacies y cruza con candidacy_documents
    CRS-->>Rev: CandidateReviewDetailDto (Requisitos cargados y faltantes)
    Rev-->>Admin: Modal de revisión con enlaces a archivos
    Admin->>Rev: GET /AdminCandidates/ServeDocument?path=...
    Rev->>Disk: Lee archivo .enc, descifra al vuelo y devuelve binario (PDF/Imagen)
    alt Aprobar Directamente
        Admin->>Rev: POST /AdminCandidates/Approve (candidateId, withExceptions=0)
        Rev->>CRS: ApproveCandidateAsync(...)
        CRS->>DB: UPDATE candidates SET status='APROBADO', reviewed_by_user_id, reviewed_at
    else Aprobar con Excepción (RN-10.1)
        Admin->>Rev: POST /AdminCandidates/Approve (candidateId, withExceptions=1, exceptionsDetail)
        CRS->>DB: UPDATE candidates SET status='APROBADO', approved_with_exceptions=1, exceptions_detail
    else Rechazar con Subsanación
        Admin->>Rev: POST /AdminCandidates/Reject (candidateId, rejectionReason, allowCorrection=1)
        CRS->>DB: UPDATE candidates SET status='RECHAZADO', rejection_reason, allow_correction=1
    end
    CRS->>DB: INSERT INTO audit_log (...)
    CRS->>DB: INSERT INTO email_queue (CANDIDATURA_APROBADA o CANDIDATURA_RECHAZADA)
    Rev-->>Admin: Candidatura gestionada correctamente
```

### 4.3 Secuencia de Votación y Escrutinio en Tiempo Real (RF-M05-00, RF-M06-00)
```mermaid
sequenceDiagram
    actor Elector
    participant Elec as ElectorController
    participant VS as VotingService
    participant DB as MySQL (wahl_mirai_db)
    participant Hub as ResultsHub (SignalR)
    actor Admin as ADMIN / SUPER_ADMIN
    participant Res as ResultsController

    Note over Elector, DB: Elección en etapa ACTIVA (Votación)
    Elector->>Elec: GET /Elector/Votar/{eventId}
    Elec->>VS: Valida etapa ACTIVA y HasVotedAsync(userId, eventId)
    alt Ya sufragó en esta elección
        Elec-->>Elector: Redirige a /Results/{eventId} ("Ya has participado")
    else Apto para votar (Sufragio único y secreto)
        Elec-->>Elector: Renderiza Tarjetón Electoral (Candidatos APROBADOS + Blanco)
        Elector->>Elec: POST /Elector/EmitirVoto (eventId, candidateId)
        Elec->>VS: CastVoteAsync(userId, eventId, candidateId)
        VS->>DB: INSERT INTO event_participations (user_id, voting_event_id)
        VS->>DB: INSERT INTO votes (voting_event_id, candidate_id, vote_hash) [ANÓNIMO, sin user_id]
        VS->>Hub: Clients.Group(eventId).SendAsync("ReceiveResultsUpdate")
        Hub-->>Res: Notifica actualización en vivo a clientes conectados
        VS-->>Elec: Voto confirmado
        Elec-->>Elector: Redirect /Elector/ConfirmacionVoto/{eventId}
    end

    Note over Admin, Elector: Consulta de Resultados (/Results/{eventId})
    Admin->>Res: GET /Results/{eventId} (Acceso irrestricto RN-5)
    Elector->>Res: GET /Results/{eventId} (Acceso permitido si ya votó RN-4 o elección FINALIZADA RN-4.1)
    Res->>DB: SELECT * FROM vw_vote_counts WHERE event_id = ?
    Res-->>Admin: Renderiza barras de progreso y porcentajes en vivo
    Res-->>Elector: Renderiza barras de progreso y porcentajes en vivo
```

---

## 5. Figura 5: Diagrama Entidad-Relación (DER) (Sección 3.1)

El modelo de datos relacional opera sobre el esquema `wahl_mirai_db`, normalizado en 17 tablas físicas bajo motor `InnoDB`:

```mermaid
erDiagram
    roles ||--o{ users : "asigna_rol"
    grades ||--o{ users : "pertenece_a_grado"
    grades ||--o{ census_whitelist : "pertenece_a_grado"
    users ||--o{ census_whitelist : "carga_lista_blanca"
    users |o--o| census_whitelist : "reclama_registro"
    grades ||--o{ event_grades : "habilitado_en"
    election_positions ||--o{ position_requirements : "define_requisitos"
    election_positions ||--o{ voting_events : "clasifica_cargo"
    voting_events ||--o{ event_grades : "clasifica_grados"
    users ||--o{ email_queue : "receptores_cola"
    users ||--o{ voting_events : "creado_por"
    voting_events ||--o{ candidates : "recibe_postulaciones"
    users |o--o{ candidates : "se_autopostula"
    users ||--o{ candidates : "revisa_candidatura"
    candidates ||--o{ candidate_proposals : "posee_propuestas"
    candidates ||--o{ candidacy_documents : "adjunta_soportes"
    position_requirements ||--o{ candidacy_documents : "exige_soporte"
    voting_events ||--o{ votes : "recibe_votos"
    candidates ||--o{ votes : "acumula_votos"
    users ||--o{ event_participations : "registra_participacion"
    voting_events ||--o{ event_participations : "rastrea_emision"
    users ||--o{ audit_log : "ejecuta_accion"
    users ||--o{ pqr_tickets : "crea_ticket"
    users |o--o{ pqr_tickets : "resuelve_ticket"

    roles {
        TINYINT id PK
        VARCHAR_30 name "UK - ELECTOR, ADMIN, SUPER_ADMIN"
        VARCHAR_100 description
    }

    academic_years {
        SMALLINT id PK
        SMALLINT year "UK"
        TINYINT_1 is_current
        DATETIME promotion_executed_at
        DATETIME created_at
    }

    grades {
        TINYINT id PK
        VARCHAR_10 name "UK"
        TINYINT sequence_order "UK"
        TINYINT_1 is_last_grade
    }

    census_whitelist {
        INT id PK
        CHAR_64 document_hash "UK"
        VARCHAR_500 encrypted_document
        VARCHAR_150 full_name
        VARCHAR_150 contact_email "Nullable"
        TINYINT grade_id FK
        TINYINT_1 excluir_de_promocion
        DATETIME claimed_at "NULL = no reclamado"
        INT claimed_by_user_id FK "Nullable"
        INT uploaded_by_user_id FK
        DATETIME created_at
    }

    users {
        INT id PK
        TINYINT role_id FK
        TINYINT grade_id FK "NULL para cuentas administrativas"
        CHAR_64 document_hash "UK"
        VARCHAR_500 encrypted_document
        VARCHAR_150 full_name
        VARCHAR_150 contact_email
        VARCHAR_255 password_hash
        VARCHAR_100 position_title "Solo ADMIN/SUPER_ADMIN"
        TINYINT_1 excluir_de_promocion
        ENUM status "ACTIVO, INACTIVO, ELIMINADO, EGRESADO"
        DATETIME deleted_at
        DATETIME registered_at
        DATETIME updated_at
    }

    email_queue {
        BIGINT id PK
        INT user_id FK
        ENUM email_type "RECUPERACION_ACCESO, REASIGNACION_ADMIN, CAMBIO_PERFIL, RESPUESTA_PQR, CANDIDATURA_APROBADA, CANDIDATURA_RECHAZADA"
        ENUM status "PENDIENTE, ENVIADO, FALLIDO"
        TINYINT attempts
        TEXT error_message
        DATETIME created_at
        DATETIME sent_at
    }

    election_positions {
        INT id PK
        VARCHAR_100 name "UK - Personero, Contralor, etc."
        TEXT description
        ENUM status "ACTIVO, INACTIVO"
        DATETIME created_at
    }

    position_requirements {
        INT id PK
        INT position_id FK
        VARCHAR_255 description
        TINYINT_1 is_mandatory
        TINYINT display_order
    }

    voting_events {
        INT id PK
        INT created_by_user_id FK
        INT position_id FK
        VARCHAR_200 title
        ENUM election_type "PERSONAS, TEMAS"
        DATE registration_start_date
        TIME registration_start_time
        DATE registration_end_date
        TIME registration_end_time
        DATE proposals_start_date
        TIME proposals_start_time
        DATE proposals_end_date
        TIME proposals_end_time
        DATE voting_start_date
        TIME voting_start_time
        DATE voting_end_date
        TIME voting_end_time
        ENUM status "PROGRAMADA, INSCRIPCION, PROPUESTAS, ACTIVA, FINALIZADA, ELIMINADO"
        DATETIME deleted_at
        DATETIME created_at
        DATETIME updated_at
    }

    event_grades {
        INT id PK
        INT voting_event_id FK
        TINYINT grade_id FK
    }

    candidates {
        INT id PK
        INT voting_event_id FK
        INT user_id FK "Nullable si es voto en blanco"
        VARCHAR_150 name
        TEXT slogan
        VARCHAR_500 photo_url
        VARCHAR_500 government_plan_url
        TINYINT_1 is_blank_vote
        ENUM status "PENDIENTE, APROBADO, RECHAZADO, RETIRADO"
        TINYINT_1 approved_with_exceptions
        TEXT exceptions_detail
        TEXT rejection_reason
        TINYINT_1 allow_correction
        INT reviewed_by_user_id FK "Nullable"
        DATETIME reviewed_at
        DATETIME enrolled_at
    }

    candidate_proposals {
        INT id PK
        INT candidate_id FK
        TEXT content
        TINYINT display_order
    }

    candidacy_documents {
        INT id PK
        INT candidate_id FK
        INT requirement_id FK
        VARCHAR_500 file_url
        DATETIME uploaded_at
    }

    votes {
        BIGINT id PK
        INT voting_event_id FK
        INT candidate_id FK
        VARCHAR_64 vote_hash "UK - SHA-256 criptografico"
        DATETIME voted_at
    }

    event_participations {
        INT id PK
        INT user_id FK
        INT voting_event_id FK
        DATETIME participated_at
    }

    audit_log {
        BIGINT id PK
        INT user_id FK "Nullable si fue el sistema"
        VARCHAR_100 action
        VARCHAR_200 target_entity
        INT target_id
        VARCHAR_100 field_name
        TEXT old_value
        TEXT new_value
        TEXT details
        VARCHAR_45 ip_address
        DATETIME occurred_at
    }

    pqr_tickets {
        BIGINT id PK
        INT user_id FK
        VARCHAR_200 subject
        TEXT message
        ENUM status "ABIERTO, RESUELTO"
        TEXT admin_response
        INT responded_by_user_id FK "Nullable"
        DATETIME responded_at
        DATETIME created_at
        DATETIME updated_at
    }
```

---

## 6. Sección 3.2: Diccionario de Datos

La especificación exhaustiva de cada tabla, campo, tipo, longitud, llaves y descripciones se encuentra documentada en el archivo técnico complementario:
* 👉 **[Diccionario de Datos Completo (17 Tablas Físicas + 5 Vistas SQL)](file:///c:/Users/APRENDIZ/Documents/xpr/WAHL-MIRAI/docs/diagramas/diccionario_de_datos_v3_1.md)**

A continuación se presenta un resumen de las entidades principales:
* **Entidades de Seguridad y Censo:** `roles`, `academic_years`, `grades`, `census_whitelist`, `users`, `audit_log`, `email_queue`.
* **Entidades Electorales y Candidaturas:** `election_positions`, `position_requirements`, `voting_events`, `event_grades`, `candidates`, `candidate_proposals`, `candidacy_documents`.
* **Entidades de Sufragio y Participación:** `votes` (urna anónima inmutable sin `user_id`, RN-3), `event_participations` (padrón anti-duplicado).
* **Entidades de Soporte y Atención:** `pqr_tickets`.
* **Vistas SQL Mapeadas Keyless:** `vw_vote_counts`, `vw_active_census`, `vw_pending_whitelist`, `vw_pending_email_queue`, `vw_pending_candidacies`.

