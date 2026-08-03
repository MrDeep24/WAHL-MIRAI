# Registro de Cambios y Desarrollo — Wahl Mirai

**Proyecto:** Wahl Mirai — Sistema de Votaciones Digitales Estudiantiles (ASP.NET Core MVC)  
**Developer:** `Kevin`

---
## 📅 3 de Agosto de 2026 13:26 — Depuración Final de Perfil y Sincronización de Documentación del Proyecto

### 📌 Resumen General
Se completó la depuración del módulo de perfil eliminando el campo redundante de "Contraseña Actual" en el formulario principal de actualización de datos, desacoplando totalmente la modificación de correo de contacto de la confirmación de clave. Asimismo, se realizó un análisis exhaustivo y actualización de todos los documentos normativos del proyecto (`docs/`), registrando los nuevos requerimientos de complejidad, el modal interactivo de 2 pasos, el correo obligatorio en el censo y la migración tecnológica a .NET 9.0.

---

### 🚀 Detalle de Cambios

#### 1. Depuración de Vista y Controlador de Perfil (RF-M07-01)
- **[MODIFICADO] `Views/Profile/Index.cshtml`**:
  - Se removió el contenedor y campo `CurrentPassword` del formulario principal. El formulario de "Configuración de Cuenta" ahora solo gestiona la actualización del correo de contacto.
- **[MODIFICADO] `Controllers/ProfileController.cs`**:
  - Se eliminó la validación obligatoria de `CurrentPassword` en la acción `Update`, permitiendo actualizar el correo de contacto sin ingresar la contraseña actual en el formulario principal.
- **[MODIFICADO] `Services/IProfileService.cs` y `ProfileService.cs`**:
  - `UpdateProfileAsync` fue actualizado para aceptar `currentPassword` como nullable (`string?`) y verificar el hash BCrypt únicamente si el valor es suministrado (por ejemplo, desde el modal AJAX).

#### 2. Sincronización de la Documentación del Proyecto (`docs/`)
- **[MODIFICADO] `docs/ers_wahl_mirai_v2_5.md`**:
  - Reescritura del requerimiento **RF-M07-01** (Consulta y Edición de Perfil Propio) para detallar el flujo del modal flotante en 2 pasos con validación asíncrona (AJAX), reglas visuales de complejidad (8+ caracteres, mayúscula `[A-Z]` y símbolo especial `!@#$%...`) y la independencia de la actualización de correo de contacto.
- **[MODIFICADO] `docs/2_5_Arquitectura_y_Diseno.md`**:
  - Sección 1: Actualización de la pila tecnológica de backend a **.NET 9.0 / ASP.NET Core**.
  - Sección 5.7: Actualización de la especificación técnica de **RF-M07-01** con los detalles del modal en 2 pasos y reglas de complejidad.
- **[MODIFICADO] `docs/3_Manual_Desarrollador.md`**:
  - Requisitos previos actualizados a **.NET 9.0 SDK**.
- **[MODIFICADO] `docs/4_Manual_Usuario.md`**:
  - Inclusión de la **Sección 4 (Mi Perfil y Autogestión de Cuenta)** describiendo los pasos para actualizar el correo de contacto y el procedimiento interactivo de cambio de contraseña mediante el modal de 2 pasos.
- **[MODIFICADO] `README.md`**:
  - Requisitos previos actualizados a **.NET 9.0 SDK**.

---

## 📅 3 de Agosto de 2026 13:11 — Modal de Cambio de Contraseña en 2 Pasos, Requisitos de Complejidad y Migración a .NET 9.0

### 📌 Resumen General
Se rediseñó completamente el flujo de cambio de contraseña en la vista "Mi Perfil", implementando un modal interactivo de 2 pasos con verificación AJAX. Se establecieron nuevos requisitos de complejidad para las contraseñas (mínimo 8 caracteres, al menos una mayúscula y al menos un símbolo especial). Adicionalmente se corrigieron dos bugs reportados (cambio de contraseña bloqueado, correo no obligatorio en nuevo elector) y se migró el proyecto a .NET 9.0 con los paquetes de EF Core y Pomelo actualizados.

---

### 🚀 Detalle de Cambios

#### 1. Modal de Cambio de Contraseña — 2 Pasos (RF-M07-01)
- **[MODIFICADO] `Views/Profile/Index.cshtml`**:
  - Se eliminó la sección colapsable de cambio de contraseña integrada en el formulario principal.
  - Se añadió un botón "Cambiar contraseña" que abre un **modal flotante de 2 pasos**:
    - **Paso 1:** Solicita y verifica la contraseña actual mediante una llamada AJAX a `/Profile/VerifyCurrentPassword`. Si es incorrecta, muestra el mensaje de error dentro del modal sin redirigir.
    - **Paso 2:** Muestra los campos de nueva contraseña y confirmación, junto con un panel de **requisitos visuales en tiempo real** (indicadores ✔/○ que se actualizan mientras el usuario escribe): mínimo 8 caracteres, al menos una mayúscula, al menos un símbolo especial y que las contraseñas coincidan.
    - **Paso 3 (Resultado):** Al confirmar, muestra el resultado directamente dentro del modal con un ícono de éxito (✔ verde) o error (✘ rojo) y el motivo en caso de fallo. El botón guardar solo se habilita cuando todos los requisitos están cumplidos.
  - Se agregó la funcionalidad de toggle de visibilidad (ojo) para todos los campos de contraseña del modal.
  - El formulario principal del perfil quedó simplificado: solo gestiona el cambio de correo de contacto con confirmación de contraseña actual.
- **[MODIFICADO] `Controllers/ProfileController.cs`**:
  - Nuevo endpoint `POST /Profile/VerifyCurrentPassword` (AJAX, retorna JSON `{ ok, message }`): verifica la contraseña actual del usuario autenticado contra el hash BCrypt en la BD.
  - Nuevo endpoint `POST /Profile/ChangePassword` (AJAX, retorna JSON `{ ok, message }`): valida y aplica el cambio de contraseña, incluyendo validación server-side de complejidad.
  - Se añadió validación server-side de la regex de complejidad también en el método `Update` (para cobertura defensiva del formulario clásico).
- **[MODIFICADO] `ViewModels/ProfileViewModel.cs`**:
  - Se añadió atributo `[RegularExpression]` al campo `NewPassword` para exigir al menos una mayúscula y un símbolo especial, complementando el `[MinLength(8)]` ya existente.

#### 2. Corrección de Bugs (sesión anterior — 3 de Agosto de 2026 12:55)
- **[CORREGIDO] Bug: No se podía cambiar contraseña desde el perfil**:
  - El campo `CurrentPassword` estaba oculto dentro del bloque colapsable `#passwordSection`, de modo que nunca se enviaba al hacer submit sin expandirlo primero. Se resolvió moviéndolo fuera del bloque colapsable para que siempre fuera visible (solución reemplazada por el nuevo modal en esta misma sesión).
- **[CORREGIDO] Bug: El correo no era obligatorio al agregar un nuevo elector**:
  - **`Views/AdminCensus/Index.cshtml`**: Se añadió `required` al input de `contactEmail` en el modal "Nuevo elector", se actualizó el label con asterisco rojo y se añadió nota descriptiva.
  - **`Controllers/AdminCensusController.cs`**: Se eliminó el fallback que generaba un email ficticio (`documento@colegio.edu.co`) cuando el campo llegaba vacío. Ahora se retorna un error en `TempData["Error"]` si el correo no se proporciona, cumpliendo la validación tanto en cliente como en servidor.
- **[MODIFICADO] `Services/ProfileService.cs` e `IProfileService.cs`**:
  - El parámetro `newContactEmail` cambió de `string` a `string?` para soportar el caso de "cambio solo de contraseña" (desde el modal AJAX) sin modificar el email del usuario.

#### 3. Migración a .NET 9.0
- **[MODIFICADO] `WahlMirai.Web.csproj`**:
  - `TargetFramework`: `net8.0` → `net9.0`.
  - `Microsoft.EntityFrameworkCore.Design` y `Microsoft.EntityFrameworkCore.Tools`: `8.0.6` → `9.0.7`.
  - `Pomelo.EntityFrameworkCore.MySql`: `8.0.2` → `9.0.0`.
- **[MODIFICADO] `README.md`**: Actualizado el enlace de descarga del SDK a .NET 9.0.

---

## 📅 29 de Julio de 2026 16:31 — Visualización de Curso en Dashboard de Elector y Botón Volver en Login

### 📌 Resumen General
Se mejoró la experiencia de usuario y la visibilidad de información en dos áreas clave del sistema:
1. En el **Dashboard del Elector**, se incorporó la visualización en tiempo real del grado/curso actual al que pertenece el estudiante autenticado junto al rol "Estudiante".
2. En la vista de **Login de Autenticación**, se añadió un botón de navegación "Volver al inicio" con ícono que permite regresar directamente al menú/página principal de bienvenida (`Home/Index`).

---

### 🚀 Detalle de Cambios

#### 1. Visualización de Curso Actual del Elector
- **[MODIFICADO] `Services/IVotingService.cs` y `VotingService.cs`**: 
  - Actualizada la interfaz `IVotingService` y su implementación para incluir la consulta del `Grade` asociado al votante mediante `.Include(v => v.Grade)` al obtener datos del estudiante autenticado (`GetVoterByDocumentAsync`).
- **[MODIFICADO] `Services/IAuthService.cs` y `AuthService.cs`**:
  - Actualizado el método `ValidateLoginAsync` para que retorne el objeto `Voter` incluyendo la navegación `.Include(v => v.Grade)`.
- **[MODIFICADO] `Controllers/AuthController.cs`**:
  - Al iniciar sesión exitosamente como elector, se agrega un nuevo Claim `GradeName` en la cookie de autenticación con el nombre del curso del estudiante.
- **[MODIFICADO] `Controllers/ElectorController.cs`**:
  - Transmisión del nombre del curso a la vista mediante `ViewBag.GradeName` en la acción `Dashboard`.
- **[MODIFICADO] `Views/Elector/Dashboard.cshtml`**:
  - Renderizado del badge con el curso del estudiante (ej. `Grado 11° - 1101` o badge equivalente) justo al lado del indicador "Estudiante".

#### 2. Botón "Volver al Inicio" en Login
- **[MODIFICADO] `Views/Auth/Login.cshtml`**:
  - Se agregó un botón interactivo/enlace de navegación con ícono de flecha hacia atrás (`arrow_back`) posicionado en la parte superior izquierda de la tarjeta/contenedor del Login, permitiendo regresar a la vista inicial (`/Home/Index`).

---

## 📅 29 de Julio de 2026 15:23 — Correcciones RF-M07-01 y RF-M07-02 (Perfil y Reasignación)

### 📌 Resumen General
Se resolvieron tres brechas de cumplimiento respecto a los requerimientos M07 en la ERS v2.4. Se reemplazó la simulación de correos en la actualización de perfil por el envío real a través de `IEmailSender`. Se mejoró la UX de la vista "Mi Perfil" ocultando por defecto el formulario de cambio de contraseña e incluyendo un botón de "No recuerdo mi contraseña" que envía una nueva clave de recuperación por correo. Finalmente, se corrigió críticamente la reasignación de contraseña por parte del administrador (RF-M07-02) para que utilice `ICredentialService`, generando una clave segura, aleatoria y enviándola por correo al usuario sin que el administrador la conozca, cumpliendo con la regla de negocio RN-2 y RN-9.

---

### 🚀 Detalle de Cambios

#### 1. Perfil de Usuario (RF-M07-01)
- **[MODIFICADO] `Services/IProfileService.cs` y `ProfileService.cs`**: Inyección de `IEmailSender` y reemplazo de `Console.WriteLine` por envío de correo real. Adición del método `RequestPasswordResetAsync` utilizando `ICredentialService`.
- **[MODIFICADO] `Controllers/ProfileController.cs`**: Nuevo endpoint `POST /Profile/SendPasswordReset` para solicitar una nueva clave desde el perfil.
- **[MODIFICADO] `Views/Profile/Index.cshtml`**: Ocultamiento de los campos de cambio de contraseña (se muestran al presionar un botón). Inclusión del botón para enviar nueva clave al correo registrado, invocando a `/Profile/SendPasswordReset`.

#### 2. Reasignación de Contraseña por Administrador (RF-M07-02)
- **[MODIFICADO] `Services/ICensusService.cs` y `CensusService.cs`**: Inyección de `ICredentialService`. El método `ResetPasswordAsync` ya no genera contraseñas predecibles (documento.año), sino que verifica que el elector tenga un correo de contacto registrado y, de ser así, utiliza `ICredentialService.IssueNewPasswordAsync` con el tipo de correo `REASIGNACION_ADMIN`.
- **[MODIFICADO] `Controllers/AdminCensusController.cs`**: Actualización de los mensajes `TempData` devueltos en la acción `ResetPassword` para reflejar el nuevo flujo (generación aleatoria y envío por correo).
- **[MODIFICADO] `Views/AdminCensus/Index.cshtml`**: Ajuste del mensaje de confirmación (`confirm`) en el botón de reasignar contraseña para aclarar que el administrador no verá la nueva clave.

#### 3. Correcciones adicionales de UX y consistencia (misma sesión)
- **[MODIFICADO] `Services/ICensusService.cs` → `AddVoterAsync`**: El alta de nuevos electores ahora también utiliza `ICredentialService.IssueNewPasswordAsync(CREDENCIAL_INICIAL)` en lugar de la fórmula predecible `documento.año`. Se usa un hash placeholder temporal que es sobreescrito de inmediato.
- **[MODIFICADO] `Controllers/AdminCensusController.cs` → `AddVoter`**: Mensaje `TempData["Success"]` actualizado para reflejar el nuevo flujo (ya no menciona la clave inicial en pantalla).
- **[MODIFICADO] `Views/Profile/Index.cshtml`**: La sección "Confirmación Requerida" (campo de contraseña actual) fue movida al interior del bloque colapsable `#passwordSection`, de modo que aparece junto a los campos de nueva contraseña al desplegar el toggle. Las alertas de éxito/error ahora también leen de `ViewBag.Success`/`ViewBag.Error` para cubrir el path de validación de modelo sin redirección.
- **[MODIFICADO] `Controllers/ProfileController.cs` → `Update`**: Se añade `ViewBag.Error` en el path de modelo inválido para que el banner de error sea visible en todos los casos de fallo.
- **[ELIMINADO] `Views/AdminCensus/Index.cshtml`**: Botón "Migrar documentos" eliminado definitivamente — la migración automática ya ocurre al inicio de la aplicación (auto-migración en `Program.cs`) y no se requiere intervención manual.

---
## 📅 28 de Julio de 2026 21:23 — Módulo de Recuperación de Acceso (RF-M01-02)

### 📌 Resumen General
Implementación completa del flujo de recuperación de acceso usando el paquete MailKit y un BackgroundService alojado en ASP.NET Core. Este enfoque asegura que la entrega de credenciales sea progresiva y con control de tasa, manteniendo las contraseñas en claro completamente fuera de la base de datos a través de un almacén en memoria (`IPendingPasswordStore`) y registrando hashes persistentes en BCrypt. Se implementó una respuesta genérica anti-enumeración.

---

### 🚀 Detalle de Cambios

#### 1. Configuración y Envío de Correo (MailKit)
- **[NUEVO] `Models/EmailSettings.cs`**: Estructura para configuración de SMTP (inyectada en `appsettings.json` sin credenciales, éstas se gestionan vía `dotnet user-secrets`).
- **[NUEVO] `Services/IEmailSender.cs` / `MailKitEmailSender.cs`**: Integración robusta con `MailKit.Net.Smtp` y `MimeKit`.

#### 2. Seguridad y Colas en Segundo Plano
- **[NUEVO] `Services/IPendingPasswordStore.cs`**: Diccionario concurrente en memoria que garantiza que el background service reciba la contraseña temporal para enviarla, sin persistirla nunca en la BD de forma plana.
- **[NUEVO] `Services/ICredentialService.cs` / `CredentialService.cs`**: Generación fuerte y aleatoria de claves, hashing con BCrypt, inserción en la cola y registro en `AuditLog`.
- **[NUEVO] `Services/EmailQueueBackgroundService.cs`**: Procesador en segundo plano que consume la tabla `email_queue` respetando un rate limit y logueando los intentos de envío.

#### 3. Interfaz y Controladores (Anti-Enumeración)
- **[NUEVO] `Controllers/RecuperacionAccesoController.cs`**: Endpoint POST que, respetando políticas anti-enumeración, muestra siempre éxito independientemente de la existencia del documento.
- **[NUEVO] `Views/RecuperacionAcceso/Recuperar.cshtml` y `Exito.cshtml`**: UI adaptada que solicita únicamente el número de documento.
- **[MODIFICAR] `Views/Auth/Login.cshtml`**: Cambio del flujo "Olvidé mi clave", apuntando al nuevo módulo y eliminando el modal.

---

## 📅 28 de Julio de 2026 — Módulo de Perfil, Cifrado de Documentos y Auto-migración

### 📌 Resumen General
Se implementaron tres mejoras relacionadas en la sesión de hoy: el módulo de autogestión de perfil de usuario (RF-M07), el cifrado real del campo `voters.encrypted_document` usando ASP.NET Core Data Protection API (deuda de seguridad MVP), y una rutina de auto-migración al arranque que elimina cualquier paso manual al desplegar en un PC nuevo o en producción.

---

### 🚀 Detalle de Cambios

#### 1. Módulo "Mi Perfil y Autogestión de Credenciales" (RF-M07-01 / RF-M07-02)
- **[NUEVO] `Services/IProfileService.cs` / `ProfileService.cs`**: Actualización de correo de contacto y contraseña con verificación obligatoria de clave actual. Integración con `IAuditService` para trazabilidad de cada cambio de credencial.
- **[NUEVO] `ViewModels/ProfileViewModel.cs`**: Vista unificada de lectura de perfil y edición de credenciales.
- **[NUEVO] `Controllers/ProfileController.cs`**: Patrón PRG (Post-Redirect-Get). Responde al rol del usuario para seleccionar el layout correcto (`_AdminLayout` / `_ElectorLayout`).
- **[MODIFICADO] `Views/Shared/_AdminLayout.cshtml` y `_ElectorLayout.cshtml`**: Incorporada la ruta "Mi Perfil" en la barra de navegación de ambos roles.

#### 2. Cifrado real de `voters.encrypted_document` (Seguridad — deuda MVP)

**Problema:** El campo `encrypted_document` se almacenaba en texto plano desde el MVP, exponiendo los documentos de identidad a cualquier persona con acceso directo a la BD.

**Solución:** Se implementó cifrado usando **ASP.NET Core Data Protection API** (sin gestión manual de claves ni AES propio).

- **[NUEVO] `Services/IDocumentEncryptionService.cs`**: Interfaz con `Encrypt(string)` y `Decrypt(string)`.
- **[NUEVO] `Services/DocumentEncryptionService.cs`**: Implementación con `IDataProtector`, purpose string versionado `"WahlMirai.DocumentEncryption.v1"`. `Decrypt()` incluye fallback temporal con log de advertencia para el período de migración (marcado con `TODO` para eliminar post-migración).
- **[MODIFICADO] `appsettings.json`**: Sección `DataProtection:KeysPath` (default: `"keys"`, configurable vía variable de entorno `DataProtection__KeysPath`).
- **[MODIFICADO] `Program.cs`**: `AddDataProtection()` + `PersistKeysToFileSystem()` con ruta configurable. Servicio registrado como Singleton.
- **[MODIFICADO] `Services/ICensusService.cs`** (`CensusService`):
  - `AddVoterAsync`: `EncryptedDocument = _encryptionService.Encrypt(document)` en lugar de texto plano.
  - `ResetPasswordAsync`: `Decrypt()` antes de pasar el documento a `GenerateInitialPasswordAsync`.
- **[MODIFICADO] `Controllers/ProfileController.cs`**: `DocumentDisplay` poblado con `Decrypt(voter.EncryptedDocument)` en GET y en re-render POST.
- **[MODIFICADO] `Controllers/AdminCensusController.cs`**: Endpoint `POST /AdminCensus/MigrateDocuments` (Admin, idempotente) como respaldo manual.
- **[MODIFICADO] `Views/AdminCensus/Index.cshtml`**: Botón "Migrar documentos" (amber) con confirmación JS como respaldo manual.

#### 3. Auto-migración al arranque (`Program.cs`)
- Al iniciar la aplicación, se recorren todos los registros de `voters` y se cifran automáticamente los que tengan `encrypted_document` en texto plano.
- **Idempotente**: los registros ya cifrados se detectan (`Decrypt(x) != x`) y se omiten sin modificación.
- **Impacto en flujo de trabajo**: clonar el repo + importar el SQL + `dotnet run` es suficiente en cualquier PC o entorno. No se requiere ningún paso manual adicional.
- Si la BD no está disponible al arrancar, el error se registra como advertencia sin interrumpir el inicio de la aplicación.

### ⚠️ Nota Crítica: Persistencia de Llaves de Data Protection
> **Si las llaves de Data Protection se pierden, todos los documentos cifrados en `encrypted_document` quedan IRRECUPERABLES.**

| Escenario | Configuración necesaria |
|:--|:--|
| Instancia única (servidor/VM) | `DataProtection:KeysPath` a ruta persistente fuera del directorio de la app |
| Múltiples instancias / load balancer | Proveedor compartido: Azure Blob, AWS S3, Redis, NFS compartido |
| Contenedores Docker | Volumen persistente montado (`-v /host/keys:/app/keys`), nunca dentro del contenedor |

Variable de entorno para sobreescribir: `DataProtection__KeysPath=/ruta/compartida/segura`

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

---

## 📅 29 de Julio de 2026 (17:12:07) — Actualización y Auditoría v2.5 (Corrección de Arquitectura y Flexibilización de Correos Compartidos)

### 📌 Resumen General

Se completó la migración y auditoría técnica a la versión **v2.5** del proyecto **Wahl Mirai**. Se corrigió la restricción de unicidad en los correos de contacto en la base de datos MySQL y en la especificación ERS IEEE 830 para permitir correos compartidos (ej. acudientes de hermanos), manteniendo la unicidad estricta basada únicamente en el documento de identidad (`document_hash`). Asimismo, se sincronizó de forma integral el documento de Arquitectura y Diseño (`2_Arquitectura_y_Diseno.md`), la especificación de requerimientos (`ers_wahl_mirai_v2_5.md`), el script DDL consolidado (`wahl_mirai_db_v2_5_completo.sql`), el diagrama ER Mermaid (`wahl_mirai_erd_v2_5.mermaid`) y el archivo de bienvenida del proyecto (`README.md`).

---

### 🚀 Detalle de Cambios Realizados

#### 1. Corrección de Restricción de Correo Compartido (v2.5)
- **Base de Datos (`wahl_mirai_db_v2_5_completo.sql`):** En la tabla `voters`, se reemplazó la restricción `UNIQUE KEY uq_voters_contact_email (contact_email)` por un índice no único `KEY idx_voters_contact_email (contact_email)`, permitiendo que múltiples electores (ej. hermanos) compartan el mismo correo de acudiente. La unicidad del elector queda garantizada exclusivamente mediante `document_hash` (CHAR(64)).
- **Especificación de Requerimientos (`ers_wahl_mirai_v2_5.md`):**
  - **RN-2.1:** Se actualizó la regla de negocio aclarando que un mismo correo de contacto puede asociarse a múltiples electores y que la unicidad del sistema se asegura únicamente por documento de identidad.
  - **RF-M02-01 (Paso 3 del flujo normal):** Se ajustó para indicar que la validación de duplicados durante la carga del censo se realiza por documento, permitiendo repetición del correo de contacto.

#### 2. Auditoría y Corrección de Arquitectura (`2_Arquitectura_y_Diseno.md`)
- **Stack Técnico Real:** Se documentó formalmente el uso de **JWT** (en lugar de cookies/sesiones), **Tailwind CSS** (en lugar de CSS Vanilla sin frameworks), y **BCrypt** + **SHA-256** (hexadecimal exacto de 64 caracteres en `document_hash`) + **AES-256** (`encrypted_document`).
- **Arquitectura SPA + Web API:** Se adaptó la descripción de la arquitectura a una **SPA única en HTML5 / JS / Tailwind CSS** consumiendo controladores ASP.NET Core y Entity Framework Core (Database First) con proveedor Pomelo MySql sobre XAMPP, eliminando esquemas obsoletos de múltiples vistas `.cshtml` y persistencia simulada en `DataService`.
- **Modelo Relacional Completo:** Se documentó el modelo completo de 12 tablas reales (`roles`, `academic_years`, `grades`, `voters`, `email_queue`, `voting_events`, `event_grades`, `candidates`, `candidate_proposals`, `votes`, `voter_event_participations`, `audit_log`).
- **Anonimato del Voto (RN-3):** Se especificó la desvinculación estructural en `votes` (sin FK a `voters`) y el control anti-duplicación a través de la tabla `voter_event_participations`.
- **Diagrama de Secuencia de Votación y Escrutinio:** Se actualizó incluyendo la ventana emergente obligatoria de propuestas (`candidate_proposals`) antes de confirmar voto (RF-M05-01), emisión de `vote_hash`, notificaciones vía **WebSockets** en tiempo real y el filtro de la vista `vw_vote_counts` excluyendo elecciones con soft-delete (`WHERE ve.status != 'ELIMINADO'`).
- **Módulos Faltantes:** Se agregaron las secciones formales para el módulo **M07 (Perfil de Usuario y Autogestión)**, **RF-M01-02 (Recuperación de Acceso)**, **RN-9 (`email_queue` con control de tasa)** y **RN-7.1 (Eliminación lógica de procesos electorales `voting_events`)**.

#### 3. Actualización del Diagrama ERD en Mermaid.js (`docs/wahl_mirai_erd_v2_5.mermaid`)
- Se renovó el diagrama ERD Mermaid reflejando fielmente el schema v2.5 de 12 tablas con sus columnas clave, tipos y comentarios actualizados.
- Se fijaron las relaciones de clave foránea (FK) reales: `votes` relacionado con `candidates` y `voting_events` (sin FK a `voters`), `voter_event_participations` como tabla puente anti-duplicado, `candidate_proposals` ligada a `candidates`, `event_grades` como tabla puente entre `voting_events` y `grades`, `email_queue` ligada a `voters`, y `audit_log` con FK nullable a `voters`.

#### 4. Renombrado de Archivos y Control de Versión v2.5
- Se renombraron los archivos activos en la carpeta `docs/`:
  - `ers_wahl_mirai_v2_4.md` ➔ `ers_wahl_mirai_v2_5.md`
  - `wahl_mirai_db_v2.4_completo.sql` ➔ `wahl_mirai_db_v2_5_completo.sql`
  - `wahl_mirai_erd_v2.4.mermaid` ➔ `wahl_mirai_erd_v2_5.mermaid`
- Se actualizaron las referencias internas de versión de v2.4/2.4 a v2.5/2.5 en los encabezados, tablas de contenido, comentarios del script SQL y pie de página de scripts.

#### 5. Sincronización del `README.md`
- Se actualizaron las referencias a las credenciales de prueba, guía de importación y novedades técnicas apuntando a la versión v2.5 y al script `docs/wahl_mirai_db_v2_5_completo.sql`.
