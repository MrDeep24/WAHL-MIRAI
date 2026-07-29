# Registro de Cambios y Desarrollo — Wahl Mirai

**Proyecto:** Wahl Mirai — Sistema de Votaciones Digitales Estudiantiles (ASP.NET Core MVC)  
**Rama:** `rama.kevin`

---

## 📅 26 de Julio de 2026 — Mejoras en creación de eventos y estados dinámicos

### 📌 Resumen General
Se ajustó el flujo de creación de Procesos Electorales para redirigir automáticamente a la vista de edición tras su creación, permitiendo al administrador agregar candidatos o temas de forma inmediata. Adicionalmente, se implementó una actualización dinámica del estado de los procesos electorales (`PROGRAMADA` -> `ACTIVA` -> `FINALIZADA`) que se evalúa en tiempo de lectura para que los procesos inicien automáticamente según las fechas y horas configuradas.

### 🚀 Detalle de Archivos Modificados
- `Controllers/AdminEventsController.cs`: Se modificó el método `Create (POST)` para hacer un `RedirectToAction("Edit")` con el `Id` del evento recién creado, guiando al administrador.
- `Services/EventService.cs`: Se introdujo el método privado `UpdateEventStatusesAsync` para evaluar si las fechas de inicio/fin han sido superadas, actualizando el estado de forma dinámica en `GetEventsAsync` y `GetEventByIdAsync`.
- `Services/IVotingService.cs` (`VotingService`): Se incorporó la lógica de actualización dinámica de estados al recuperar los procesos activos para un votante en `GetActiveEventsForVoterAsync`.

## 📅 26 de Julio de 2026 — Extensión de Esquema y Especificaciones v2.4: Eliminación Lógica de Procesos Electorales

### 📌 Resumen General
Se formalizó e integró la **Versión 2.4** de la Especificación de Requerimientos de Software (ERS), la base de datos SQL consolidada y el Diagrama ERD (Mermaid). Se añadió soporte nativo para la eliminación lógica (*soft-delete*) de procesos electorales (`voting_events`) mediante el estado `ELIMINADO` y la columna `deleted_at`. **Nota:** Las versiones previas se preservaron intactas en la carpeta `docs/documentos antiguos` para control histórico de cambios.

---

### 🚀 Detalle de Artefactos Modificados (Versión 2.4)

#### 1. Script SQL Consolidado (`docs/wahl_mirai_db_v2.3_completo.sql` → v2.4)
- **`CREATE TABLE voting_events`:**
  - `status`: Ampliado a `ENUM('PROGRAMADA','ACTIVA','FINALIZADA','ELIMINADO')`.
  - `deleted_at`: Adición de columna `DATETIME NULL DEFAULT NULL` para registrar la fecha/hora de baja lógica (mismo patrón que `voters.deleted_at`).
- **Vista `vw_vote_counts`:**
  - Adición de filtro `WHERE ve.status != 'ELIMINADO'` para excluir de las métricas en vivo los procesos dados de baja, preservando inmutables sus votos para auditoría (RN-7.1).
- **Instrucción ALTER TABLE para bases de datos existentes:**
  - Para instancias en ejecución, se provee el comando independiente:
    ```sql
    ALTER TABLE `voting_events` 
    MODIFY COLUMN `status` ENUM('PROGRAMADA','ACTIVA','FINALIZADA','ELIMINADO') NOT NULL DEFAULT 'PROGRAMADA',
    ADD COLUMN `deleted_at` DATETIME NULL DEFAULT NULL AFTER `status`;
    ```

#### 2. Diagrama ERD (`docs/wahl_mirai_erd_v2.3.mermaid` → v2.4)
- Actualización de entidad `voting_events` agregando el atributo `DATETIME deleted_at` y el valor `ELIMINADO` a la anotación del `ENUM status`.

#### 3. Modelo EF Core C# (`WahlMirai.Web/Models/VotingEvent.cs` y `WahlMiraiDbContext.cs`)
- Edición manual del modelo `VotingEvent.cs` agregando `public DateTime? DeletedAt { get; set; }`.
- Actualización de la configuración Fluent API en `WahlMiraiDbContext.cs` ajustando la propiedad `Status` y mapeando la columna `deleted_at`. (Se utilizó edición manual por presentar 0 riesgo de sobrescribir personalizaciones del DbContext frente a un re-scaffold completo).

#### 4. Especificación ERS (`docs/ers_wahl_mirai_v2_3.md` → v2.4)
- **Actualización de encabezado:** Elevación a **Versión 2.4**.
- **Propósito (1.1):** Adición del punto 8 formalizando la eliminación lógica de elecciones y preservación de inmutabilidad de votos.
- **Regla de Negocio RN-7.1:** Creación de la regla explícita *Eliminación Lógica de Procesos Electorales*, declarando que un evento eliminado deja de ser visible/operable para electores sin afectar votos históricos.
- **Requerimiento Funcional RF-M03-02:** Adición del RF *Edición y Eliminación Lógica de Procesos Electorales* con precondiciones, postcondiciones y flujos.

---

## 📅 26 de Julio de 2026 — Implementación de AdminEventsController y Vistas de Elecciones y Candidatos (M03 + M04)

### 📌 Resumen General
Se completó la implementación del módulo de **Gestión de Elecciones y Candidatos (M03/M04)** en `WahlMirai.Web`, agregando servicios, controlador, y vistas Razor con Tailwind para la parametrización de procesos tipo `PERSONAS` y `TEMAS`, inscripción de candidatos y opciones, eliminación lógica de eventos y trazabilidad en auditoría.

---

### 🚀 Detalle de Cambios Recientes
- Implementación de `ProfileController` y `ProfileService` para cumplir con RF-M07-01 y RF-M07-02 (módulo "Mi Perfil y Autogestión de Credenciales").
- Creación de `ProfileViewModel` para vista unificada de lectura de perfil y actualización de correo y contraseña.
- Actualización de los layouts base (`_AdminLayout` y `_ElectorLayout`) incorporando la ruta "Mi Perfil".
- Integración con `IAuditService` para el registro de auditoría ante cualquier cambio de credenciales, con confirmación de contraseña actual obligatoria.

#### 1. Base de Datos y Modelo (`WahlMirai.Web/migration.sql`, `Models/VotingEvent.cs`, `Models/WahlMiraiDbContext.cs`)
- **Eliminación Lógica de Eventos (RN-7):**
  - Script SQL (`migration.sql`) para incluir el estado `'ELIMINADO'` en el `ENUM status` y la columna `deleted_at DATETIME` en la tabla `voting_events`.
  - Actualización del modelo `VotingEvent.cs` con la propiedad `DeletedAt`.
  - Mapeo en `WahlMiraiDbContext.cs` ajustando la columna `status` y `deleted_at`.

#### 2. Capa de Servicios (`Services/IEventService.cs`, `Services/EventService.cs`)
- **`IEventService` / `EventService`:**
  - `GetEventsAsync()`: Consulta de eventos activos (excluyendo eliminados) con grados y candidatos.
  - `GetEventByIdAsync(id)`: Consulta detallada de evento con candidatos, propuestas y grados.
  - `CreateEventAsync(...)`: Valida fechas, establece estado `PROGRAMADA`, sincroniza `event_grades` y autogenera el candidato "Voto en Blanco" **únicamente para elecciones de tipo `PERSONAS`**. Registra auditoría en `audit_log`.
  - `UpdateEventAsync(...)`: Actualiza parámetros y re-sincroniza `event_grades`. Registra auditoría.
  - `SoftDeleteEventAsync(id)`: Marcado de eliminación lógica (`status = 'ELIMINADO'`) con fecha `deleted_at`. Registra auditoría.
  - `AddCandidateAsync(...)`: Validación de precondición (evento en estado `PROGRAMADA` y tipo `PERSONAS`), asociación de elector activo y registro en auditoría.
  - `AddProposalOptionAsync(...)`: Validación de precondición (evento en estado `PROGRAMADA` y tipo `TEMAS`), creación de opción con `voter_id = NULL` y registro en auditoría.
  - `SearchVoterAsync(...)`: Búsqueda de electores activos por documento (búsqueda por hash) o por nombre, proyectando a un DTO seguro (`VoterSearchResultDto`) que no expone hashes ni datos sensibles.

#### 3. Inyección de Dependencias (`Program.cs`)
- Registro de `IEventService` con su implementación `EventService` en el contenedor DI scoped.

#### 4. Capa de Controladores (`Controllers/AdminEventsController.cs`)
- Implementación completa de acciones protegidas con `[Authorize(Roles = "ADMIN")]`:
  - `Index()`: Dashboard de elecciones.
  - `Create()` (GET/POST): Formulario de creación de procesos electorales.
  - `Edit(id)` (GET/POST): Configuración y edición de procesos electorales.
  - `Delete(id)` (POST): Eliminación lógica de un proceso electoral.
  - `AddCandidate(...)` (POST): Inserción de candidatos a procesos tipo `PERSONAS`.
  - `AddProposalOption(...)` (POST): Inserción de opciones a procesos tipo `TEMAS`.
  - `SearchVoter(term)` (GET): Endpoint AJAX para la búsqueda de electores en el modal.

#### 5. Capa de Vistas (`Views/AdminEvents/`)
- `Index.cshtml`: Dashboard con tarjetas por proceso electoral, badges de estado (`Programada`, `En Curso`, `Finalizada`), contadores de opciones y tabs de filtrado.
- `Form.cshtml`: Vista única reusable para `Create` y `Edit` que incluye renderizado condicional del bloque de configuración según el tipo de proceso (`PERSONAS` o `TEMAS`).
- `_ConfigurePersonas.cshtml`: Sub-vista parcial para la lista de candidatos inscritos y botón para abrir el modal de vinculación.
- `_ConfigureTemas.cshtml`: Sub-vista parcial para la lista de opciones/proyectos temáticos y botón para el modal de nuevas opciones.
- `_AddCandidateModal.cshtml`: Modal interactivo AJAX con búsqueda de elector por documento/nombre (muestra únicamente el grado, sin secciones, y documento desencriptado para UI cumpliendo RN-6) e inputs para lema y foto.
- `_AddProposalOptionModal.cshtml`: Modal interactivo para creación de opciones temáticas.
- `_DeleteConfirmModal.cshtml`: Modal interactivo de confirmación para borrado lógico de elecciones.

---

## 📅 26 de Julio de 2026 — Actualización DB v2.3, Requerimientos ERS v2.3 y Nuevos Usuarios Iniciales

### 📌 Resumen General
En la fecha de hoy se integró la **Versión 2.3** de la base de datos y la Especificación de Requerimientos de Software (ERS IEEE 830-1998 v2.3). Se consolidó el script DDL único `docs/wahl_mirai_db_v2.3_completo.sql`, se actualizaron las reglas de negocio de credenciales y correo de contacto obligatorio, y se actualizaron los usuarios iniciales de prueba tanto en la base de datos como en la documentación principal (`README.md`).

---

### 🚀 Detalle de Cambios Realizados

#### 1. Esquema DDL Consolidado de Base de Datos (`docs/wahl_mirai_db_v2.3_completo.sql`)
- **Script Único Consolidado:** Reemplazo de la secuencia previa de múltiples scripts por un único archivo SQL DDL completo (schema + datos semilla v2.3) listo para ejecución en MySQL 8.0+.
- **Tabla `voters` (Censo Electoral Persistente):**
  - Adición del campo `contact_email` (VARCHAR 150, UNIQUE, NOT NULL) para registrar el correo del estudiante/acudiente (RN-2.1).
  - Adición de `excluir_de_promocion` (TINYINT) para eximir electores repitentes de la promoción anual masiva.
  - Ampliación del estado `status` a ENUM (`ACTIVO`, `INACTIVO`, `ELIMINADO`, `EGRESADO`).
  - Adición de `deleted_at` (DATETIME) para soporte de eliminación lógica (*soft-delete*).
- **Tabla `email_queue` (Notificaciones en Cola — RN-9):**
  - Nueva tabla y vista `vw_pending_email_queue` para el procesamiento progresivo y asíncrono de correos con credenciales y recuperación con control de tasa.
- **Tabla `audit_log` (Trazabilidad Criptográfica — RN-8):**
  - Esquema expandido con campos para entidad afectada (`target_entity`), ID (`target_id`), valor anterior (`old_value`), nuevo valor (`new_value`), detalles en JSON (`details`) e IP del cliente (`ip_address`).
- **Control de Promoción Anual (`academic_years` y `grades`):**
  - Campo `promotion_executed_at` en `academic_years` para prevención de doble promoción en el mismo año lectivo.
  - Flag `is_last_grade` en `grades` para promover automáticamente estudiantes de 11° a estado `EGRESADO`.

#### 2. Requerimientos de Software (ERS IEEE 830 v2.3)
- **RN-2 & RN-2.1 (Credenciales Aleatorias y Correo Obligatorio):**
  - Se mantiene la autenticación por documento de identidad (no por correo).
  - Se elimina el esquema predecible de contraseña (`documento + año`) y la obligatoriedad del cambio de clave en el primer inicio de sesión.
  - Las contraseñas se generan de manera aleatoria por el sistema y se entregan al correo de contacto registrado.
- **RF-M07 (Perfil de Usuario y Autogestión):**
  - Nuevo módulo de autogestión para consulta de datos y actualización voluntaria de correo de contacto y contraseña por parte del usuario autenticado.

#### 3. Nuevos Usuarios Iniciales (Semilla v2.3)
Se definieron y cargaron los nuevos usuarios iniciales de prueba en la base de datos:

| Rol | Nombre | Documento | Contraseña | Correo de Contacto | Estado |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **ADMIN** | Coordinación Electoral | `admin.electoral` | `Admin#2026!` | `coordinacion.electoral@colegio.edu.co` | Activo |
| **ELECTOR** | Ana María López Pérez | `1001234567` | `1001234567.2026` | `acudiente.ana.lopez@example.com` | Activo (Grado 11°) |

> **Nota:** `1001234567.2026` es la contraseña de ejemplo para entorno de prueba. En producción el sistema genera una contraseña aleatoria y la entrega por correo (RN-2).

#### 4. Actualización del Código C# — Alineación con DB v2.3

Se actualizaron los siguientes archivos del proyecto `WahlMirai.Web` para reflejar los cambios de esquema de la base de datos:

- **`Models/Voter.cs`:**
  - ❌ Eliminado: `RequiereCambioClave` (campo removido en DB v2.3).
  - ✅ Agregado: `ContactEmail` (string, NOT NULL) — correo de contacto obligatorio (RN-2.1).

- **`Models/VwActiveCensu.cs`:**
  - ❌ Eliminado: `RequiereCambioClave`.
  - ✅ Agregado: `ContactEmail` — alineado con la vista `vw_active_census` actualizada.

- **`Models/WahlMiraiDbContext.cs`:**
  - ❌ Eliminado: mapeo de la columna `requiere_cambio_clave` en `voters` y `vw_active_census`.
  - ✅ Agregado: mapeo de `contact_email` (VARCHAR 150) en `voters` y `vw_active_census`.
  - ✅ Agregado: índice único `uq_voters_contact_email` en la entidad `Voter`.

- **`Data/DbInitializer.cs`:**
  - Reducido a 2 usuarios semilla (admin + elector), alineados con la semilla SQL v2.3.
  - ❌ Eliminado: usuario elector con `RequiereCambioClave = true` y las credenciales antiguas (`admin123`, `estudiante123`, `1007654321.2026`).
  - ✅ Actualizado: contraseñas y correos de contacto correctos por usuario.

- **`Services/IAuthService.cs`:**
  - ❌ Eliminado: `voter.RequiereCambioClave = false` en `ChangePasswordAsync` (campo inexistente en v2.3).

- **`Services/ICensusService.cs`:**
  - ✅ Actualizado: firma de `AddVoterAsync` para incluir `string contactEmail` como parámetro obligatorio.
  - ❌ Eliminado: `RequiereCambioClave = true` al crear y resetear contraseña de electores.

- **`Controllers/AuthController.cs`:**
  - ❌ Eliminado: `Claim("RequiereCambioClave", ...)` al generar la cookie de autenticación.
  - ❌ Eliminado: lógica de renovación de claim `RequiereCambioClave` al cambiar contraseña.

- **`Middleware/ForcePasswordChangeMiddleware.cs`:**
  - Desactivado: el middleware ya no intercepta peticiones para forzar cambio de contraseña.
  - Se conserva como stub por compatibilidad con el pipeline registrado en `Program.cs`.

#### 5. Documentación y README
- Actualización de `README.md` con las nuevas credenciales de prueba de la versión 2.3.
- Actualización de las instrucciones de instalación para indicar la importación de `docs/wahl_mirai_db_v2.3_completo.sql`.

---

## 📅 22 de Julio de 2026 — Migración Inicial ASP.NET Core MVC

### 📌 Resumen General

Durante este día se realizó la migración completa del prototipo estático HTML/Tailwind hacia una arquitectura **ASP.NET Core 8 MVC** con **Entity Framework Core (Database First)** conectada a **MySQL (XAMPP)**, garantizando el cumplimiento de las reglas de negocio (RN-1 a RN-8) y requisitos funcionales ERS IEEE 830.

---

### 🚀 Detalle de Cambios Realizados

#### 1. Inicialización y Estructura del Proyecto (.NET 8)
- Creación de la solución y proyecto `WahlMirai.Web`.
- Instalación de paquetes NuGet: `Pomelo.EntityFrameworkCore.MySql`, `BCrypt.Net-Next`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Tools` y `AutoMapper`.
- Configuración de la cadena de conexión MySQL a XAMPP en `appsettings.Development.json`.
- Integración de la compilación automática de Tailwind CSS CLI en `WahlMirai.Web.csproj`.

#### 2. Capa de Datos y Modelos (EF Core Database First)
- Scaffolding de la base de datos `wahl_mirai_db` a clases C# en la carpeta `Models/` (`Voter`, `Role`, `Grade`, `VotingEvent`, `Candidate`, `CandidateProposal`, `Vote`, `VoterEventParticipation`, `AuditLog`, `AcademicYear`, etc.).
- Mapeo de vistas SQL `VwVoteCount` y `VwActiveCensus` para lecturas optimizadas de resultados y censo.

#### 3. Servicios y Lógica de Negocio (`Services/`)
- `AuthService.cs`: Hashing BCrypt, autenticación por documento, validación de usuarios activos, reseteo de claves y hashing SHA-256 de números de documento.
- `CensusService.cs`: Gestión del censo electoral, alta de usuarios, eliminación lógica *soft-delete* (`status = 'ELIMINADO'`), restauración de electores y reseteo de contraseñas.
- `VotingService.cs`: Emisión de voto anónimo criptográfico en `votes` (sin guardar `voter_id`) y registro atómico anti-duplicados en `voter_event_participations` (RN-3).
- `PromotionService.cs`: Algoritmo transaccional de promoción automática por secuencia de grados (`sequence_order`) y egreso de estudiantes en grado final (RN-6).
- `AuditService.cs`: Sistema de auditoría criptográfica en la tabla `audit_log` para trazabilidad de cambios sensibles (RN-8).

#### 4. Seguridad, Autenticación y Middleware (`Middleware/`)
- Configuración de Cookie Authentication con políticas de autorización para los roles `ADMIN` y `ELECTOR`.
- `ForcePasswordChangeMiddleware.cs`: Middleware que actúa como guard de servidor, interceptando peticiones de electores con `requiere_cambio_clave = true` y forzando la redirección a `Auth/CambiarClave` (RN-2).

#### 5. Controladores (`Controllers/`)
- `AuthController.cs`: Gestión de inicios de sesión, cierres de sesión y flujo de cambio forzado de clave.
- `ElectorController.cs`: Dashboard del estudiante, tarjetón electoral interactivo y recepción del voto.
- `AdminCensusController.cs`: Panel de gestión del censo con modales interactivos y ejecución de la promoción de año lectivo.
- `AdminEventsController.cs`: Vista del listado de procesos electorales configurados.
- `ResultsController.cs`: Visualización de resultados en tiempo real. Aplica restricción de acceso si el elector no ha votado (RN-4) e inmunidad para el administrador (RN-5).
- `HomeController.cs`: Controlador para la página principal de bienvenida.

#### 6. Semillero Automático de Base de Datos (`Data/DbInitializer.cs`)
- Implementación de un inicializador automático en C# que, al arrancar la aplicación, verifica si la BD está vacía y crea automáticamente:
  - Año lectivo `2026`.
  - Usuarios de prueba con roles `ADMIN` y `ELECTOR`.
  - Elección de prueba activa (*Personería Estudiantil 2026*) con candidatos, propuestas y fotos.

#### 7. Vistas e Interfaz Visual (`Views/`)
- **Página Inicial de Bienvenida (`Views/Home/Index.cshtml`)**: Creada a partir del mockup `pagina inicial de bienvenida`, configurada como la vista inicial del sistema con botón hacia el Login.
- **Vistas del Elector:** `Dashboard.cshtml` y `Votar.cshtml` con modales de propuestas.
- **Vistas del Administrador:** `AdminCensus/Index.cshtml` (con parcial `_PromotionModal.cshtml`) y `AdminEvents/Index.cshtml`.
- **Vista de Resultados:** `Results/Index.cshtml` con gráficos porcentuales y banderas de estado en vivo.
- **Vistas de Autenticación:** `Login.cshtml` y `CambiarClave.cshtml`.
- **Layouts Reutilizables:** `_AdminLayout.cshtml`, `_ElectorLayout.cshtml` y `_Layout.cshtml`.

#### 8. Documentación y Optimización de Repositorio
- `README.md`: Documento actualizado con requisitos, guía de ejecución en XAMPP y tabla de credenciales de acceso de prueba.
- `.gitignore`: Creado en la raíz para excluir binarios pesados (`bin/`, `obj/`, `tailwindcss.exe`), evitando bloqueos en GitHub por el límite de 100 MB.
