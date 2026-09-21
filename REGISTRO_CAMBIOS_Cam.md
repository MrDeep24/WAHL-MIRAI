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
