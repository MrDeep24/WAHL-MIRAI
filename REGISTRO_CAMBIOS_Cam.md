# Registro de Cambios — Wahl Mirai
**Archivo:** `REGISTRO_CAMBIOS_Cam.md`  
**Fecha:** 21 de septiembre de 2026  
**Descripción:** Detalle técnico y funcional de las modificaciones realizadas en el sistema de votación estudiantil **Wahl Mirai**.

---

## 1. Modificación de Documento de Identidad en el Censo Electoral

### Problema
En el censo electoral, el campo de documento aparecía en modo solo lectura (`readonly`) y el backend no recibía ni procesaba cambios sobre el número de documento de los electores, impidiendo a administradores y super administradores corregir errores tipográficos en el documento de identidad.

### Solución Implementada
- **Vista (`WahlMirai.Web/Views/AdminCensus/Index.cshtml`):**
  - Se convirtió el campo `editDocument` en un campo editable con `name="document"`, validación de dígitos numéricos (`pattern="\d+"`), límites de longitud (5 a 15 caracteres) y texto de ayuda explicativo.
  - La función JavaScript `openEditModal(id)` vincula el documento desencriptado retornado por el API al campo editable.
- **Controlador (`WahlMirai.Web/Controllers/AdminCensusController.cs`):**
  - El método `EditVoter` ahora recibe el parámetro `string document` y lo envía a la capa de servicios (`newDocument`).
- **Servicio (`WahlMirai.Web/Services/ICensusService.cs`):**
  - En `UpdateVoterAsync`, si se envía un nuevo documento:
    1. Se valida formato numérico estricto con expresión regular (`^\d+$`).
    2. Se calcula el hash criptográfico determinístico `DocumentHash` mediante `_authService.HashDocument(newDocument)`.
    3. Se valida la unicidad en la base de datos para evitar colisiones con otros electores registrados.
    4. Se actualiza tanto `DocumentHash` (utilizado para el inicio de sesión) como `EncryptedDocument` (cifrado AES para visualización segura).

---

## 2. Visualización de Fotos y Planes de Gobierno para Electores en Todas las Fases

### Problema
1. `ElectorController.Propuestas` bloqueaba el acceso si el proceso no estaba estrictamente en la etapa `PROPUESTAS` (impidiendo consultar candidatos y propuestas durante la votación `ACTIVA` o tras haber finalizado el proceso).
2. Las fotos y planes de gobierno subidos se guardaban cifrados (`.enc`) en `App_Data/candidacy-uploads/`. Las vistas del elector (`Propuestas.cshtml` y `Votar.cshtml`) intentaban cargarlos directamente como recursos estáticos (`/uploads/candidacies/...enc`), provocando errores 404 o archivos ilegibles.
3. El endpoint administrativo existente `ServeDocument` estaba restringido únicamente a roles `ADMIN` y `SUPER_ADMIN`.

### Solución Implementada
- **Nuevo Controlador de Medios Cifrados (`WahlMirai.Web/Controllers/MediaController.cs`):**
  - Se creó el controlador `MediaController` con el endpoint autenticado `/Media/Serve?path=...`.
  - Descifra en memoria bajo demanda mediante `IDocumentEncryptionService` y entrega el archivo con su MIME type correcto (`image/jpeg`, `image/png`, `application/pdf`, etc.).
  - Aplica validación de seguridad granular: si el usuario es `ELECTOR`, restringe el acceso estrictamente a archivos públicos de campaña (`photo_*` y `plan_*`), bloqueando documentos internos de postulación (`doc_*`).
- **Controlador de Electores (`WahlMirai.Web/Controllers/ElectorController.cs`):**
  - Se actualizó el método `Propuestas(int id)` para permitir el acceso tanto en etapa `PROPUESTAS` como en `ACTIVA` y `FINALIZADA`.
- **Vistas del Elector (`Propuestas.cshtml`, `Votar.cshtml` y `Dashboard.cshtml`):**
  - Se incorporó la función auxiliar `ResolveFileUrl` para dirigir automáticamente los archivos `.enc` hacia `/Media/Serve?path=...`.
  - En `Propuestas.cshtml`: Las fotos de los candidatos y el botón «Ver Documento» del Plan de Gobierno oficial cargan de forma inmediata.
  - En `Votar.cshtml`: El tarjetón electoral muestra la foto del candidato y su modal permite consultar el Plan de Gobierno en PDF.
  - En `Dashboard.cshtml`: Se añadieron enlaces directos para consultar propuestas tanto en eventos activos como en eventos donde el elector ya votó.

---

## 3. Elecciones de Tipo TEMAS (Sin Inscripción ni Cargo Electoral)

### Problema
Al crear o configurar una elección de tipo `TEMAS` (consultas temáticas / proyectos), el formulario requería innecesariamente la Etapa 1 (Inscripción de Candidatos), la configuración de requisitos documentales y la asignación de un Cargo Electoral (figuras que solo aplican a candidaturas de personas).

### Solución Implementada
- **Vista de Configuración (`WahlMirai.Web/Views/AdminEvents/Form.cshtml`):**
  - Se identificaron con IDs y clases condicionales el contenedor de Cargo Electoral (`cargoElectoralContainer`), la tarjeta de Etapa 1 (`stage1RegistrationContainer`) y la sección de requisitos documentales.
  - Se programó la función JavaScript `updateElectionTypeUI` que, al seleccionar `TEMAS`, oculta automáticamente estos tres bloques y deshabilita (`disabled = true`) los inputs de la Etapa 1 para que las validaciones HTML5 nativas no interrumpan el envío.
- **Controlador (`WahlMirai.Web/Controllers/AdminEventsController.cs`):**
  - En las acciones `Create` y `Edit`, cuando `ElectionType == "TEMAS"`:
    - Se sincronizan internamente las fechas de inscripción con el inicio de consulta de propuestas (`RegistrationStart = ProposalsStart`, `RegistrationEnd = ProposalsStart`).
    - Se asigna un cargo por defecto en caso de no venir especificado.
- **Lógica de Dominio (`EventService.cs` e `IVotingService.cs`):**
  - `ValidateStageWindows`: En elecciones `TEMAS` se omiten las validaciones cronológicas de la etapa de inscripción.
  - `CalculateStatus`: Se omite el estado `INSCRIPCION` para elecciones `TEMAS`, permitiendo que el flujo avance directamente: `PROGRAMADA` → `PROPUESTAS` → `ACTIVA` → `FINALIZADA`.

---

## 4. Visualización de Resultados para Administradores

### Problema
Los administradores no disponían de un acceso directo a los resultados electorales desde las tarjetas de eventos en el panel administrativo.

### Solución Implementada
- **Panel de Eventos (`WahlMirai.Web/Views/AdminEvents/Index.cshtml`):**
  - Se agregó un botón destacado «Ver Resultados» (`/Results/Index/@ev.Id`) con icono `insights` en cada tarjeta de proceso electoral.
- **Edición de Eventos (`WahlMirai.Web/Views/AdminEvents/Form.cshtml`):**
  - Se incluyó un botón de acceso directo a «Ver Resultados» en la cabecera superior al consultar o editar cualquier proceso existente.

---

## 5 y 7. Modo Oscuro en Flujos de Autenticación y Corrección de Doble Botón de Contraseña

### Problema
1. En navegadores Chromium y Microsoft Edge, el campo de contraseña mostraba dos botones superpuestos: el botón nativo de revelación del navegador y el botón toggle personalizado.
2. En las vistas de autenticación (`Login`, `Registro`, `Completar`, `Recuperar`, `CambiarClave`), los contenedores con opacidad `bg-white/95` no se transformaban a modo oscuro, manteniendo un fondo blanco brillante e inconsistente.

### Solución Implementada
- **Hojas de Estilo (`WahlMirai.Web/wwwroot/css/theme.css`):**
  - Se suprimió el botón nativo del navegador mediante las pseudo-clases:
    ```css
    input[type="password"]::-ms-reveal,
    input[type="password"]::-ms-clear,
    input[type="password"]::-webkit-credentials-auto-fill-button {
        display: none !important;
        visibility: hidden !important;
        pointer-events: none !important;
        width: 0 !important;
        height: 0 !important;
    }
    ```
  - Se añadieron selectores para soportar opacidades en modo oscuro:
    ```css
    html.dark .bg-white,
    html.dark [class*="bg-white/"],
    html.dark .bg-white\/95 {
        background-color: rgba(15, 23, 42, 0.95) !important;
    }
    html.dark [class*="border-white/"],
    html.dark .border-white\/40 {
        border-color: rgba(51, 65, 85, 0.6) !important;
    }
    ```
- **Vistas Actualizadas con Modo Oscuro y Selector de Tema/Idioma:**
  - `WahlMirai.Web/Views/Auth/Login.cshtml`: Se añadió `autocomplete="current-password"`.
  - `WahlMirai.Web/Views/Auth/Registro.cshtml`: Barra superior con selector de idioma y toggle oscuro/claro, inputs con estilos oscuros y contrastes optimizados.
  - `WahlMirai.Web/Views/Auth/Completar.cshtml`: Adaptación completa a modo oscuro, indicadores de pasos legibles y barra de tema.
  - `WahlMirai.Web/Views/Auth/CambiarClave.cshtml`: Fondo oscuro y toggle de tema.
  - `WahlMirai.Web/Views/RecuperacionAcceso/Recuperar.cshtml` y `Exito.cshtml`: Tarjetas y textos adaptados con soporte para modo oscuro e internacionalización.

---

## 6. Contraste de Texto en Consulta de Propuestas (Modo Oscuro)

### Problema
En `Propuestas.cshtml`, los textos de descripción y las etiquetas de secciones utilizaban clases secundarias atenuadas (`text-on-surface-variant`), las cuales tenían muy bajo contraste sobre fondos oscuros.

### Solución Implementada
- **Vista (`WahlMirai.Web/Views/Elector/Propuestas.cshtml`):**
  - Se reemplazaron las clases de color por tipografía de alto contraste: `text-slate-100`, `text-slate-200`, `text-slate-300`, `text-on-surface dark:text-slate-50`.
  - Los encabezados de sección («Lema de Campaña», «Propuestas Registradas», «Plan de Gobierno Oficial») ahora destacan nítidamente tanto en tema claro como en tema oscuro.

---

## 8. Nitidez y Visibilidad del Patrón de Fondo del Dashboard

### Problema
El patrón de fondo de urnas y votos (`voting-pattern.svg`) era prácticamente invisible en pantalla (opacidad `0.05` en claro y `0.04` en oscuro), y el archivo SVG contenía un rectángulo blanco sólido de fondo (`<rect fill="#ffffff">`) que lavaba los colores del diseño.

### Solución Implementada
- **Gráfico Vectorial (`WahlMirai.Web/wwwroot/images/voting-pattern.svg`):**
  - Se removió el elemento `<rect fill="#ffffff">`, haciendo el fondo del SVG completamente transparente para mostrar únicamente los trazos vectoriales.
- **Estilos (`WahlMirai.Web/wwwroot/css/theme.css`):**
  - Se incrementó la opacidad de `.voting-pattern-bg::before` a `0.12` en modo claro y `0.10` en modo oscuro. El patrón ahora resulta claramente perceptible y estético sin obstruir la lectura.

---

## 9. Exclusión de Imágenes y Archivos Subidos en Git y GitHub

### Problema
Las fotografías de candidatos, documentos de acreditación y planes de gobierno en PDF se estaban guardando y versionando dentro del repositorio Git (`WahlMirai.Web/wwwroot/uploads/` y `WahlMirai.Web/App_Data/candidacy-uploads/`), subiéndose a GitHub.

### Solución Implementada
- **Configuración de Ignorados (`.gitignore`):**
  - Se agregaron las siguientes reglas para excluir los almacenes locales de archivos y archivos cifrados:
    ```gitignore
    # Archivos subidos por usuarios (fotos, planes de gobierno, documentos)
    WahlMirai.Web/wwwroot/uploads/**
    !WahlMirai.Web/wwwroot/uploads/.gitkeep
    WahlMirai.Web/App_Data/candidacy-uploads/**
    !WahlMirai.Web/App_Data/candidacy-uploads/.gitkeep
    **/candidacy-uploads/**
    *.enc
    ```
- **Preservación de Directorios:**
  - Se crearon los archivos `.gitkeep` en `WahlMirai.Web/wwwroot/uploads/.gitkeep` y `WahlMirai.Web/App_Data/candidacy-uploads/.gitkeep` para conservar la estructura de carpetas en nuevos clones.
- **Limpieza del Rastreo de Git:**
  - Se ejecutó `git rm -r --cached` sobre los directorios de archivos subidos, eliminándolos del seguimiento del repositorio sin borrarlos físicamente del disco de trabajo.

---

## Resumen de Archivos Modificados y Creados

| Tipo | Archivo |
| :--- | :--- |
| **Nuevo** | `WahlMirai.Web/Controllers/MediaController.cs` |
| **Nuevo** | `WahlMirai.Web/wwwroot/uploads/.gitkeep` |
| **Nuevo** | `WahlMirai.Web/App_Data/candidacy-uploads/.gitkeep` |
| **Nuevo** | `REGISTRO_CAMBIOS_Cam.md` |
| **Modificado** | `.gitignore` |
| **Modificado** | `WahlMirai.Web/Controllers/AdminCensusController.cs` |
| **Modificado** | `WahlMirai.Web/Services/ICensusService.cs` |
| **Modificado** | `WahlMirai.Web/Views/AdminCensus/Index.cshtml` |
| **Modificado** | `WahlMirai.Web/Controllers/ElectorController.cs` |
| **Modificado** | `WahlMirai.Web/Views/Elector/Propuestas.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Elector/Votar.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Elector/Dashboard.cshtml` |
| **Modificado** | `WahlMirai.Web/Controllers/AdminEventsController.cs` |
| **Modificado** | `WahlMirai.Web/Services/EventService.cs` |
| **Modificado** | `WahlMirai.Web/Services/IVotingService.cs` |
| **Modificado** | `WahlMirai.Web/Views/AdminEvents/Form.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/AdminEvents/Index.cshtml` |
| **Modificado** | `WahlMirai.Web/wwwroot/css/theme.css` |
| **Modificado** | `WahlMirai.Web/wwwroot/images/voting-pattern.svg` |
| **Modificado** | `WahlMirai.Web/Views/Auth/Login.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Auth/Registro.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Auth/Completar.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Auth/CambiarClave.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/RecuperacionAcceso/Recuperar.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/RecuperacionAcceso/Exito.cshtml` |

---

## Verificación de Compilación
```bash
dotnet build WahlMirai.Web/WahlMirai.Web.csproj
```
- **Resultado:** Compilación correcta.
- **Advertencias:** 0
- **Errores:** 0

---

## 10. Visibilidad Completa de Hora (AM/PM) en Ventanas de Etapas Electorales

### Problema
Los inputs `<input type="time">` en el formulario de configuración de etapas electorales tenían clase `w-24` (96px), lo que resultaba insuficiente para mostrar el indicador AM/PM del reloj nativo del navegador. El usuario no podía ver si la hora configurada era AM o PM.

### Solución Implementada
- **Vista (`WahlMirai.Web/Views/AdminEvents/Form.cshtml`):**
  - Se amplió la clase de ancho de **`w-24`** a **`w-36`** (144px) en los **6 inputs de tipo `time`** presentes en las tres etapas electorales:
    - Inicio y fin de Inscripción (`RegistrationStartTime`, `RegistrationEndTime`)
    - Inicio y fin de Propuestas (`ProposalsStartTime`, `ProposalsEndTime`)
    - Inicio y fin de Votación (`VotingStartTime`, `VotingEndTime`)
  - El mayor ancho permite al selector nativo mostrar correctamente el bloque `hh:mm AM/PM` sin recorte.

### Archivos Modificados
| Archivo | Cambio |
|---|---|
| `WahlMirai.Web/Views/AdminEvents/Form.cshtml` | Cambio de `w-24` → `w-36` en los 6 inputs de hora |

---

# Registro de Cambios — 22 de Septiembre de 2026

**Fecha:** 22 de septiembre de 2026  
**Módulos intervenidos:** Procesos Electorales (Admin), Candidaturas, Visor de Documentos, Sistema de Alertas/Modales y Dashboard del Elector.

---

## 11. Validación en Tiempo Real de Fechas y Horas (Votación en un Solo Día y Restricción de Pasado)

### Problema
1. Las fechas y horas de las 3 etapas (Inscripción, Propuestas y Votación) se validaban únicamente al enviar el formulario al servidor, permitiendo al usuario ingresar inconsistencias cronológicas sin retroalimentación inmediata.
2. La jornada de votación podía configurarse a través de múltiples días, cuando la directriz electoral exige que la votación ocurra en una única jornada de un solo día.
3. El formulario permitía seleccionar fechas y horas anteriores al momento actual.

### Solución Implementada
- **Vista (`WahlMirai.Web/Views/AdminEvents/Form.cshtml`):**
  - **Sincronización de un solo día:** Al modificar `VotingStartDate`, el campo `VotingEndDate` se asigna idéntico y queda bloqueado en modo `readonly`.
  - **Control de pasado (`min`):** Todos los selectores de fecha tienen su atributo `min` fijado en la fecha actual (`yyyy-MM-dd`).
  - **Reloj desde la hora actual:** Si la fecha seleccionada es hoy, el `min` de hora se bloquea a la hora actual (`HH:mm`). Si se introduce una hora pasada, se corrige automáticamente y se notifica con un modal de advertencia.
  - Oyentes en tiempo real (`input` y `change`) que validan la secuencia cronológica entre etapas con indicadores de error inmediatos.
- **Controlador (`WahlMirai.Web/Controllers/AdminEventsController.cs`):**
  - En la acción `Create` (GET), se inicializa `RegistrationStartTime` con `new TimeOnly(today.Hour, today.Minute)` en lugar de un valor estático matutino.
- **Servicio de Dominio (`WahlMirai.Web/Services/EventService.cs`):**
  - En `ValidateStageWindows`, se valida de forma estricta que `VotingStartDate == VotingEndDate` y que `VotingEndTime > VotingStartTime`.

---

## 12. Restricción de una Sola Votación Activa por Cargo

### Problema
El sistema permitía crear múltiples elecciones de tipo `PERSONAS` para un mismo cargo escolar (ej. Personero) simultáneamente en curso, generando conflictos de representación y duplicidad en el tarjetón electoral.

### Solución Implementada
- **Controlador (`WahlMirai.Web/Controllers/AdminEventsController.cs`):**
  - En las acciones `Create` y `Edit`, se consulta la base de datos para identificar qué cargos tienen procesos en curso y se envían a la vista en `ViewBag.ActivePositionIds`.
- **Vista (`WahlMirai.Web/Views/AdminEvents/Form.cshtml`):**
  - En el selector de cargos (`PositionId`), las opciones con votación activa se inhabilitan (`disabled = true`) y se rotulan con el distintivo `— [Votación en curso]`.
- **Servicio de Dominio (`WahlMirai.Web/Services/EventService.cs`):**
  - En `CreateEventAsync` y `UpdateEventAsync`, se valida a nivel de negocio que no exista otro evento activo para el mismo cargo (`PositionId`). Si existe, se lanza `InvalidOperationException` impidiendo el guardado.

---

## 13. Visor Modal de Archivos y Documentos en la Misma Página

### Problema
Al consultar soportes de requisitos de candidatos, actas o planes de gobierno, los enlaces abrían pestañas externas en blanco (`target="_blank"`), interrumpiendo el flujo de revisión administrativa y la navegación del elector.

### Solución Implementada
- **Nuevo Componente Modal (`WahlMirai.Web/Views/Shared/_DocumentViewerModal.cshtml`):**
  - Modal previsualizador con capa de visualización segura (`z-index: 70`), compatible con documentos **PDF** (renderizados mediante `<iframe>`) e **imágenes** (`.png`, `.jpg`, `.jpeg`, `.webp`) con spinner de carga integrado.
  - Cierre intuitivo mediante botón flotante, clic fuera del recuadro o tecla `Escape`.
  - API JavaScript global: `window.openDocumentViewer(url, title, extension)`.
- **Integración Global:**
  - Incluido en `_AdminLayout.cshtml` y `_ElectorLayout.cshtml`.
  - Conectado en:
    - [AdminEvents/_ConfigurePersonas.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/AdminEvents/_ConfigurePersonas.cshtml) (soportes y plan de gobierno de candidatos).
    - [AdminCandidates/Index.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/AdminCandidates/Index.cshtml) (revisión de postulaciones y dictamen).
    - [Elector/Votar.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Elector/Votar.cshtml) y [Elector/Propuestas.cshtml](file:///c:/Proyecto/WAHL-MIRAI/WahlMirai.Web/Views/Elector/Propuestas.cshtml) (consulta del plan de gobierno en PDF).

---

## 14. Restricción de Personería Exclusiva para Estudiantes de Grado 11

### Problema
Estudiantes de grados inferiores (10° o menores) podían visualizar la convocatoria, acceder al formulario de postulación e inscribirse a elecciones para el cargo de Personero Estudiantil, contraviniendo el artículo 28 del Decreto 1860 de 1994.

### Solución Implementada
- **Servicio de Candidaturas (`WahlMirai.Web/Services/CandidacyService.cs`):**
  - `GetEligibleEventsForPostulationAsync`: Si el cargo del evento corresponde a *Personero*, se restringe exclusivamente a estudiantes de grado 11 (`IsLastGrade == true` o nombre que contenga "11").
  - `GetPostulationFormDetailAsync`: Si un estudiante que no es de grado 11 intenta acceder por enlace directo, se rechaza la solicitud.
  - `SubmitPostulationAsync`: Validación de seguridad en el servidor que cancela el registro con el mensaje: *"A las elecciones de personero solo pueden aspirar e inscribirse los estudiantes de grado 11."*
- **Servicio de Eventos (`WahlMirai.Web/Services/EventService.cs`):**
  - `AddCandidateAsync`: Valida que el estudiante pertenezca al grado 11 al ser inscrito manualmente por un administrador.
- **Interfaz Administrativa (`WahlMirai.Web/Views/AdminCandidates/_AddCandidateModal.cshtml`):**
  - En la búsqueda de electores para elecciones de Personero, los estudiantes de otros grados se muestran deshabilitados con el distintivo `Solo grado 11°`.

---

## 15. Título y Descripción como Campos de Selección Institucionales

### Problema
El título y la descripción eran campos de texto libre (`<input type="text">` y `<textarea>`), propensos a discrepancias ortográficas, redacciones informales y títulos incoherentes con el cargo electoral seleccionado.

### Solución Implementada
- **Vista (`WahlMirai.Web/Views/AdminEvents/Form.cshtml`):**
  - Se sustituyó el input de título por un `<select name="Title" id="titleSelect">` estructurado con `<optgroup>` para *Cargos del Gobierno Escolar* (Personero, Contralor, Representante de Curso) y *Consultas Temáticas* (Presupuesto Participativo, Semana Cultural, Proyectos Estudiantiles), estampados dinámicamente con el año lectivo en curso.
  - Se sustituyó el textarea de descripción por un `<select name="Description" id="descriptionSelect">` con opciones institucionales formales.
  - **Sincronización Automática:** La función JavaScript `syncTitleAndDescription()` actualiza coherentemente el título y la descripción al cambiar de cargo o tipo de evento.
  - **Ajuste de Opciones:** A solicitud expresa, se suprimieron las opciones de *Personerito Primaria* y *Representante ante el Consejo Directivo*.
  - **Preservación de Datos:** En caso de editar un proceso histórico con redacción personalizada, el valor se mantiene como `<option selected>`.

---

## 16. Sistema Centralizado de Modales para Alertas y Confirmaciones

### Problema
Las notificaciones de éxito (`TempData["Success"]`), error (`TempData["Error"]`) y validaciones de formulario se renderizaban como cintas fijas o barras de texto en la parte superior e inferior de la pantalla, afectando la estética y la experiencia de usuario.

### Solución Implementada
- **Nuevo Componente Modal (`WahlMirai.Web/Views/Shared/_AlertModal.cshtml`):**
  - Diálogo modal centrado con fondo oscuro difuminado (`backdrop-blur-xs`), animación fluida de entrada (`zoom-in-95`) e iconos contextuales SVG según el tipo (`success`, `error`, `warning`, `info`, `confirm`).
  - Función global `window.showAppModal({ title, message, type, onConfirm, onCancel, confirmText, cancelText })`.
  - Disparo automático en carga para `TempData["Success"]`, `TempData["Error"]` y `TempData["Warning"]`.
- **Integración Global:**
  - Incluido en `_AdminLayout.cshtml` y `_ElectorLayout.cshtml`.
  - Se eliminaron los banners fijos en `Form.cshtml` y se enlazaron con el modal de validación interactivo.

---

## 17. Rediseño Integral del Dashboard del Elector (Estilo Bento Grid)

### Problema
Cuando un estudiante tenía pocas elecciones activas (o ninguna), el dashboard del elector quedaba con más del 65% del espacio en blanco, con una cabecera elemental de saludo y una columna lateral desaprovechada con texto estático.

### Solución Implementada
- **Controlador (`WahlMirai.Web/Controllers/ElectorController.cs`):**
  - Se añadieron métricas electorales calculadas en tiempo real: `TotalEvents`, `TotalVoted`, `TotalActiveToVote`, `TotalInscriptions` y `ParticipationPercent`.
- **Vista (`WahlMirai.Web/Views/Elector/Dashboard.cshtml`):**
  - **Hero Banner con 4 Mini KPIs Bento:**
    - Saludo cívico personalizado (`¡Hola, [Nombre]! 👋`), badge de grado y fecha completa en español.
    - Cuatro cuadrantes de métricas: *Procesos asignados*, *Tu participación con barra de progreso*, *Votación hoy (alerta pulsante)* y *Censo electoral verificado*.
  - **Tarjetas de Elección Bento:**
    - Acento superior temático por estado (verde para urnas abiertas, ámbar para inscripción, púrpura para propuestas, pizarra para cerradas).
    - Horarios de apertura y cierre de urnas destacados.
    - Botones de acción contextuales de alto impacto (`Emitir Mi Voto Ahora`, `Ver Propuestas y Candidatos`, `Mi Postulación`, `Ver Resultados`).
  - **Línea de Tiempo (Ruta del Proceso Electoral):**
    - Visualizador de las 4 fases del gobierno escolar: 1. Inscripción $\rightarrow$ 2. Propuestas $\rightarrow$ 3. Votación $\rightarrow$ 4. Escrutinio.
  - **Guía Rápida del Votante en 3 Pasos:**
    - Tarjetas de cultura democrática: Lectura de propuestas en PDF, secreto del voto y protección contra suplantación.
  - **Carnet Digital del Elector (Sidebar):**
    - Credencial con avatar de iniciales, nombre completo, grado, rol e indicador de habilitación en tiempo real, junto con el sello de voto anónimo disociado (RN-3).
  - **Historial y Soporte Estudiantil:**
    - Lista de eventos con badges de estado y enlaces directos al módulo de PQR y gestión de perfil.
  - **Empty State Ilustrado:**
    - Ilustración amigable y mensaje constructivo con enlaces cuando no haya elecciones asignadas.

---

## Resumen de Archivos Modificados / Creados el 22 de Septiembre de 2026

| Tipo | Archivo |
|---|---|
| **Nuevo** | `WahlMirai.Web/Views/Shared/_DocumentViewerModal.cshtml` |
| **Nuevo** | `WahlMirai.Web/Views/Shared/_AlertModal.cshtml` |
| **Nuevo** | `WahlMirai.Tests/EventValidationAndCandidacyTests.cs` |
| **Modificado** | `WahlMirai.Web/Controllers/AdminEventsController.cs` |
| **Modificado** | `WahlMirai.Web/Controllers/ElectorController.cs` |
| **Modificado** | `WahlMirai.Web/Services/EventService.cs` |
| **Modificado** | `WahlMirai.Web/Services/CandidacyService.cs` |
| **Modificado** | `WahlMirai.Web/Views/AdminEvents/Form.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/AdminEvents/_ConfigurePersonas.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/AdminCandidates/Index.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/AdminCandidates/_AddCandidateModal.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Elector/Dashboard.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Elector/Votar.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Elector/Propuestas.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Shared/_AdminLayout.cshtml` |
| **Modificado** | `WahlMirai.Web/Views/Shared/_ElectorLayout.cshtml` |

---

## Verificación de Pruebas Unitarias
```powershell
dotnet test WahlMirai.Tests/WahlMirai.Tests.csproj --no-build
```
- **Total pruebas:** 39
- **Superadas:** 39 (100%)
- **Fallidas:** 0
- **Duración:** 3 s

