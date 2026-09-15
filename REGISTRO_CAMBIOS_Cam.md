# Registro de Cambios y Desarrollo — Wahl Mirai

**Proyecto:** Wahl Mirai — Sistema de Votaciones Digitales Estudiantiles (ASP.NET Core MVC)  
**Developer:** `Cam`

---

## 📅 12 de Agosto de 2026 — Actualizaciones de UI, Vistas de Autenticación, Votación e Index

### 📌 Resumen General
Registro inicial de cambios realizados por **Cam**. Se realizaron optimizaciones de diseño, estructuración visual y mejoras de interfaz en múltiples vistas Razor del proyecto (`Index`, `Login`, `CambiarClave`, `Votar`, `Recuperar`, `Exito`), así como ajustes en las hojas de estilo CSS e interfaz de servicios.

---

### 🚀 Detalle de Cambios

#### 1. Vistas de Autenticación y Acceso
- **[MODIFICADO] [CambiarClave.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Auth/CambiarClave.cshtml)**:
  - Ajustes de diseño e interfaz en el formulario de cambio de clave.
- **[MODIFICADO] [Login.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Auth/Login.cshtml)**:
  - Actualizaciones visuales y mejoras de estilos en la vista de inicio de sesión.
- **[MODIFICADO] [Recuperar.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/RecuperacionAcceso/Recuperar.cshtml)**:
  - Rediseño y optimizaciones visuales en el flujo de recuperación de acceso.
- **[MODIFICADO] [Exito.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/RecuperacionAcceso/Exito.cshtml)**:
  - Ajustes de presentación en la pantalla de confirmación exitosa de recuperación.

#### 2. Vistas de Inicio y Votación
- **[MODIFICADO] [Index.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Home/Index.cshtml)**:
  - Reestructuración de la página principal del sistema con mejoras de maquetación UI/UX.
- **[MODIFICADO] [Votar.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Elector/Votar.cshtml)**:
  - Refactorización de la pantalla de votación para electores.

#### 3. Estilos y Servicios
- **[MODIFICADO] [site.css](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/wwwroot/css/site.css)**:
  - Ajustes generales en los estilos globales de la aplicación.
- **[MODIFICADO] [IVotingService.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Services/IVotingService.cs)**:
  - Inyección de `IHubContext<ResultsHub>` y emisión de eventos `ReceiveResultsUpdate` en tiempo real tras la confirmación de cada voto.
- **[NUEVO] Archivos multimedia en `wwwroot/images/`**:
  - Incorporación de assets visuales (`imagen_voto.png`, `login-background.jpg.jpg`).

---

## 📅 12 de Agosto de 2026 — Implementación de Conteo y Resultados en Tiempo Real (SignalR)

### 📌 Resumen General
Se implementó el conteo de votos y la actualización en vivo de la gráfica de resultados utilizando **ASP.NET Core SignalR**. Cada vez que un elector emite su voto, el sistema notifica en tiempo real a los usuarios viendo los resultados, actualizando el conteo total, porcentajes y barras animadas sin recargar la página.

---

### 🚀 Detalle de Cambios

#### 1. SignalR Hub & Configuración
- **[NUEVO] [ResultsHub.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Hubs/ResultsHub.cs)**:
  - Implementación de Hub SignalR para gestionar salas por elección (`JoinEventGroup` / `LeaveEventGroup`).
- **[MODIFICADO] [Program.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Program.cs)**:
  - Registro del servicio SignalR (`AddSignalR`) y mapeo de ruta `/hubs/resultsHub`.

#### 2. Controlador y Servicios
- **[MODIFICADO] [ResultsController.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Controllers/ResultsController.cs)**:
  - Adición del endpoint `GET /Results/GetLiveData/{id}` con verificación de permisos para obtener el conteo dinámico en formato JSON.
  - Inclusión de `ViewBag.EventId` en el action `Index`.

#### 3. Vista Frontend
- **[MODIFICADO] [Index.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Results/Index.cshtml)**:
  - Integración del cliente JS de SignalR, eventos reactivos para actualizar el DOM en tiempo real y mecanismo de respaldo por polling inteligente cada 5 segundos.

---

## 📅 12 de Agosto de 2026 — Botón para Mostrar/Ocultar Contraseña en el Login

### 📌 Resumen General
Se agregó un botón toggle interactivo con icono de ojo (`visibility` / `visibility_off`) dentro del campo de entrada de contraseña en la vista de **Login**, permitiendo a los usuarios visualizar u ocultar su contraseña ingresada antes de enviar el formulario.

---

### 🚀 Detalle de Cambios

#### 1. Vista de Inicio de Sesión
---

## 📅 17 de Agosto de 2026 — Correcciones Generales, Reglas de Negocio, Filtros Censo y Ganador 50% + 1

### 📌 Resumen General
Se resolvieron múltiples problemas detectados en el flujo electoral y la administración de usuarios, y se implementaron nuevas funcionalidades clave requeridas para el sistema:
1. **Acceso a Elecciones Finalizadas para Electores**: Los electores ahora pueden consultar la vista de resultados de una elección finalizada aun cuando no hayan emitido voto.
2. **Validación de Fechas en Creación/Edición de Elecciones**: Se impide la programación de elecciones con fecha de inicio anterior al día actual.
3. **Filtros Interactivos en Dashboard Admin**: Se activaron los filtros de estado (Todos, Programados, En Curso, Finalizados) en la vista del panel electoral.
4. **Cálculo de Ganador por Mayoría Absoluta (50% + 1)**: Integración de la regla de mayoría absoluta en la vista de resultados con actualización en vivo vía SignalR.
5. **Restricción de Votación para Usuarios Eliminados**: Validación estricta del estado `ACTIVO` del usuario antes de permitir la emisión de votos.
6. **Formato de Fechas Unificado**: Presentación estandarizada en formato `DD/MM/YYYY`.
7. **Filtros Múltiples en Censo Electoral**: Búsqueda combinada en tiempo real por Nombre, Grado (6° a 11°) y Estado (Activo, Eliminado, Egresado).
8. **Validación de Campos**: Expresiones regulares y restricciones HTML5 para validar formato numérico de documento en Login y Censo.
9. **Creación de Usuarios (Elector / Admin)**: Cambio del botón a "Nuevo usuario" e inclusión de selector de Rol (Elector / Admin) con ocultamiento inteligente de campo de Grado.

---

### 🚀 Detalle de Cambios

#### 1. Controlador y Servicios Electoral
- **[MODIFICADO] [EventService.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Services/EventService.cs)**:
  - Adición de validación para `StartDate >= hoy` en `CreateEventAsync` y `UpdateEventAsync`.
- **[MODIFICADO] [IVotingService.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Services/IVotingService.cs)**:
  - Verificación del estado `voter.Status == "ACTIVO"` en `CastVoteAsync` antes de procesar votos.
- **[MODIFICADO] [ElectorController.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Controllers/ElectorController.cs)**:
  - Verificación del estado activo del elector antes de renderizar la vista de votación.

#### 2. Vistas del Elector y Resultados
- **[MODIFICADO] [Dashboard.cshtml (Elector)](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Elector/Dashboard.cshtml)**:
  - Inclusión del estado "Finalizada" y botón "Ver resultados finalizados" para electores que no votaron.
- **[MODIFICADO] [Index.cshtml (Results)](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Results/Index.cshtml)**:
  - Cálculo de mayoría absoluta `(TotalVotos / 2) + 1`, insignia de ganador y actualización dinámica en el cliente mediante SignalR.

#### 3. Vistas de Administración (Eventos y Censo)
- **[MODIFICADO] [Form.cshtml (AdminEvents)](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/AdminEvents/Form.cshtml)**:
  - Atributo `min` en el selector de fecha `StartDate`.
- **[MODIFICADO] [Index.cshtml (AdminEvents)](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/AdminEvents/Index.cshtml)**:
  - Script e interacción JavaScript para los botones de pestañas por estado (`TODOS`, `PROGRAMADA`, `ACTIVA`, `FINALIZADA`).
- **[MODIFICADO] [Index.cshtml (AdminCensus)](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/AdminCensus/Index.cshtml)**:
  - Cambio de etiqueta del botón a "Nuevo usuario".
  - Filtros desplegables por Grado y Estado con script JS `filterTable()` multinivel.
  - Modificación del modal `voter-modal` con selector de Rol (`roleId`), validación numéricas de documento y toggle interactivo de Grado.

#### 4. Autenticación y Modelos
- **[MODIFICADO] [AuthViewModels.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/ViewModels/AuthViewModels.cs)**:
  - Atributo `[RegularExpression(@"^\d+$")]` en `LoginViewModel.Document`.
- **[MODIFICADO] [Login.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Auth/Login.cshtml)**:
  - Atributo `pattern="\d+"` y mensajes descriptivos de validación en HTML.

---

## 📅 1 de Septiembre de 2026 — Módulo de Revisión y Autopostulación de Candidaturas

### 📌 Resumen General
Se implementó de forma completa el flujo de Autopostulación de Candidatos (M04-02), delegando el registro de postulaciones directamente a los electores durante la etapa `INSCRIPCION` y eliminando la asignación manual por parte del Administrador. Asimismo, se implementó el panel de revisión administrativa para dictaminar las postulaciones y se corrigió el manejo de correos de estado de candidatura en segundo plano. Finalmente, se actualizó la Especificación de Requisitos (SRS) para definir el rechazo definitivo o con opción de subsanación.

---

### 🚀 Detalle de Cambios

#### 1. Módulo de Autopostulación de Electores
- **[NUEVO] [ICandidacyService.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Services/ICandidacyService.cs) & [CandidacyService.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Services/CandidacyService.cs)**:
  - Servicio que gestiona la carga de la foto, plan de gobierno (PDF), lista interactiva de propuestas y soportes documentales según el catálogo de cargos (`position_requirements`).
- **[NUEVO] [CandidacyController.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Controllers/CandidacyController.cs)**:
  - Controlador protegido para electores con acciones `Index`, `Apply` y `Status`.
- **[NUEVO] Vistas de Autopostulación (`Views/Candidacy/`)**:
  - `Index.cshtml`: Dashboard del elector para postularse a procesos abiertos y ver su historial.
  - `Apply.cshtml`: Formulario dinámico con subida multipart y validación de requisitos documentales obligatorios.
  - `Status.cshtml`: Pantalla de seguimiento del dictamen con los mensajes de retroalimentación de la administración.
- **[MODIFICADO] [_ElectorLayout.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Shared/_ElectorLayout.cshtml)**:
  - Se agregó el enlace a "Mis Candidaturas" en la navegación.

#### 2. Modelos, DB y Servicios de Fondo
- **[MODIFICADO] [WahlMiraiDbContext.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Models/WahlMiraiDbContext.cs)**:
  - Corrección de la relación de clave foránea `fk_pr_position` usando explícitamente `WithMany(p => p.PositionRequirements)`.
- **[MODIFICADO] [ElectionPosition.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Models/ElectionPosition.cs)**:
  - Inclusión de la colección de navegación `PositionRequirements`.
- **[MODIFICADO] [EmailQueueBackgroundService.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Services/EmailQueueBackgroundService.cs)**:
  - Refactorización de la lógica para soportar envíos de correo sin necesidad de contraseña en RAM (para notificaciones de `CANDIDATURA_APROBADA` y `CANDIDATURA_RECHAZADA`), solucionando el error de "Contraseña en memoria perdida".

#### 3. Documentación
- **[MODIFICADO] [ers_wahl_mirai_v2_8.md](file:///c:/Proyecto/WAHL-MIRAI/docs/ers_wahl_mirai_v2_8.md)**:
  - Se actualizó el caso de uso `RF-M04-02` estipulando explícitamente que al rechazar una candidatura, el administrador indicará si es de forma definitiva o si permite al elector volver a inscribirse editando sus requisitos (subsanación).

---

## 📅 15 de Septiembre de 2026 — Mejoras de Flujo Electoral, Requisitos de Candidatura, Traducción i18n y Tema de Login (Completado)

### 📌 Resumen General
Se implementaron con éxito las 6 mejoras esenciales requeridas para enriquecer la experiencia de usuario de electores y administradores en Wahl Mirai:
1. **Etapas Dinámicas en Elecciones Activas**: Presentación de la etapa real del proceso electoral (`Inscripción`, `Consulta de Propuestas`, `Votación Activa`, `Finalizada`) con insignias y estados cromáticos diferenciales en lugar de estados genéricos o confusos.
2. **Botones Reactivos según Etapa**: Adaptación de las acciones principales en las tarjetas del Dashboard de Elector (`Inscribirme como candidato` / `Ver mi postulación`, `Ver propuestas y plan de gobierno`, `Ir a votar`, `Ver resultados`, `Ver resultados finalizados`).
3. **Flujo Asistido de Votación y Consulta de Propuestas**: Redirección asistida al hacer clic en "Ir a votar" cuando la elección aún no esté en período de votación (llevando a la pantalla de inscripción si está en inscripción, o a la nueva vista de consulta de propuestas con su Plan de Gobierno en PDF). Manejo amigable cuando no hay candidatos aprobados.
4. **Selección Masiva de Cursos**: Botones interactivos "Seleccionar todos los cursos" y "Deseleccionar todos" para marcar o desmarcar todos los grados habilitados en un solo clic al crear o configurar procesos electorales.
5. **Configuración de Papeles/Documentos Exigidos**: Interfaz administrativa para definir, agregar, quitar y marcar obligatorios u opcionales los documentos requeridos para las postulaciones de candidatos (`position_requirements`), con sugerencias rápidas institucionales y persistencia inmediata.
6. **Corrección Integral de Traducción (i18n)**: Diccionario exhaustivo con todas las palabras y frases de los dashboards de admin y estudiante en los 6 idiomas (`es`, `en`, `de`, `fr`, `ja`, `zh`), motor de expresiones regulares para cadenas dinámicas (saludos, grados, conteos) y `MutationObserver` para traducción reactiva ante cambios en el DOM.
7. **Selector de Tema Claro/Oscuro en Login**: Barra superior en la pantalla de inicio de sesión con toggle de tema (`dark:` / `light:`) y selector de idioma con soporte glassmorphism y contraste optimizado.

---

### 🚀 Detalle de Cambios

#### 1. Módulo de Elector y Votación
- **[MODIFICADO] [ElectorController.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Controllers/ElectorController.cs)**:
  - Inyección de `ICandidacyService`.
  - Redirecciones asistidas según la etapa del proceso en `Votar(int id)` (`INSCRIPCION` -> postulación, `PROPUESTAS` -> consulta de propuestas, `FINALIZADA` -> resultados).
  - Nueva acción `Propuestas(int id)` para visualizar candidatos admitidos, sus propuestas y su Plan de Gobierno oficial.
  - Enriquecimiento del modelo de datos para el Dashboard de Elector con el objeto `MyPostulation`.
- **[MODIFICADO] [Dashboard.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Elector/Dashboard.cshtml)**:
  - Insignias dinámicas de etapa (`Inscripción`, `Propuestas`, `Voto Registrado`, `Votación Activa`, `Finalizada`).
  - Botones adaptativos según la etapa y el estado de postulación/participación del usuario.
- **[NUEVO] [Propuestas.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Elector/Propuestas.cshtml)**:
  - Interfaz de consulta de candidatos aprobados con visualización de eslogan, lista numerada de propuestas y acceso al documento del Plan de Gobierno en PDF.

#### 2. Módulo de Administración de Eventos y Requisitos
- **[MODIFICADO] [Form.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/AdminEvents/Form.cshtml)**:
  - Botones "Seleccionar todos los cursos" y "Deseleccionar todos" en la sección de grados habilitados.
  - Sección interactiva "Papeles y Documentos Exigidos para Candidatura" con sugerencias predefinidas (+ Certificado de notas, + Paz y salvo, + Carta de acudiente, + Matrícula vigente), toggle obligatorio/opcional y eliminación dinámica.
  - Carga dinámica de documentos al cambiar el selector de cargo electoral.
- **[MODIFICADO] [AdminEventsController.cs](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Controllers/AdminEventsController.cs)**:
  - Endpoint `GET /AdminEvents/GetPositionRequirements` para obtener los requerimientos documentales de un cargo.
  - Método `SavePositionRequirementsAsync` para sincronizar y persistir los requisitos documentales configurados en `Create` y `Edit` respetando restricciones de integridad referencial.
  - Clase DTO `RequirementInputDto`.

#### 3. Internacionalización (i18n) y Tema
- **[MODIFICADO] [locale.js](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/wwwroot/js/locale.js)**:
  - Ampliación exhaustiva del diccionario en inglés (`en`), alemán (`de`), francés (`fr`), japonés (`ja`) y chino (`zh`) con todo el vocabulario de procesos electorales, etapas, requisitos, botones y dashboards.
  - Coincidencia de patrones con expresiones regulares para nombres dinámicos (`¡Hola, {nombre}!`), grados (`Grado {x}`), opciones (`{x} opciones`) y candidatos (`Candidato {x}`).
  - Integración de `MutationObserver` para traducir reactivamente cualquier nuevo elemento añadido al DOM sin recargar la página.
- **[MODIFICADO] [Login.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Auth/Login.cshtml)**:
  - Inclusión de barra superior con toggle de tema (`.theme-toggle`) y selector de idioma (`.locale-select`).
  - Soporte completo para modo oscuro con clases Tailwind `dark:bg-slate-900/90`, `dark:border-slate-800`, `dark:text-white` y campos de entrada adaptados.

