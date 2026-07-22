# PROMPT PARA AGENTE DE CÓDIGO — Wahl Mirai → ASP.NET Core MVC (Razor) + MySQL (XAMPP)

Copia todo el contenido de este archivo como instrucción inicial para tu agente (Claude Code, Cursor, etc.), junto con los archivos de referencia mencionados en la sección 0.

---

## 0. Archivos de referencia que el agente DEBE leer antes de escribir código

Coloca estos archivos en la raíz del repositorio antes de arrancar al agente:

1. `docs/wahl_mirai_db_v2.2.sql` — DDL autoritativo de la base de datos. **No lo modifiques**; el modelo de datos de C# se deriva de aquí, no al revés.
2. `docs/ers_wahl_mirai_v2_2.md` — Especificación de requisitos (IEEE 830). Fuente de verdad para reglas de negocio (RN-1 a RN-8) y requisitos funcionales (RF-M01 a RF-M06).
3. `docs/wahl_mirai_web.html` — Prototipo de front-end ya construido (HTML/JS con Tailwind) que define look & feel, textos, componentes y flujos de pantalla. Úsalo como referencia visual y de UX, no como código a reutilizar literalmente.
4. `docs/DESIGN.md` — Tokens de diseño (colores, tipografía, espaciado, elevación). Conserva esta paleta; no inventes una nueva.

Si alguno de estos archivos no está disponible, el agente debe pedir que se agregue antes de continuar, no asumir su contenido de memoria.

---

## 1. Objetivo

Convertir el prototipo estático `wahl_mirai_web.html` en una aplicación **ASP.NET Core MVC con Razor Views**, en C#, con arquitectura **MVC real y por capas** (no Razor Pages, no Blazor), conectada a MySQL corriendo en **XAMPP** (phpMyAdmin/MySQL local, puerto 3306, usuario `root` sin contraseña por defecto salvo que se indique otra cosa).

El resultado debe ser un proyecto ejecutable con `dotnet run` que, al abrir el navegador, muestre exactamente los flujos ya validados en el prototipo, pero con backend real, persistencia real en MySQL y sin ningún dato "hardcodeado" en JavaScript.

---

## 2. Stack técnico obligatorio

- **.NET 8 (LTS)**, ASP.NET Core MVC.
- **Entity Framework Core** + **Pomelo.EntityFrameworkCore.MySql** como proveedor (es el estándar para MySQL/XAMPP en EF Core; no uses el conector oficial de Oracle salvo justificación explícita).
- **Database First**: genera el `DbContext` y las entidades con `Scaffold-DbContext` apuntando al `wahl_mirai_db` ya creado por el script SQL, en vez de generar migraciones desde cero. El schema SQL es la fuente de verdad; el código C# se adapta a él, no al revés.
- **BCrypt.Net-Next** para hash de contraseñas (coherente con el comentario `password_hash` del DDL).
- **Cookie Authentication** de ASP.NET Core (no Identity completo) con un **claim de rol** (`ADMIN` / `ELECTOR`) leído desde la tabla `roles`. La tabla `roles` ya existe en el DDL, así que el sistema de roles NO debe reemplazarse por el de ASP.NET Identity.
- **AutoMapper** (opcional pero recomendado) para mapear entidades EF ↔ ViewModels, evitando exponer las entidades de base de datos directamente en las vistas.
- Front-end: conserva **Tailwind CSS** y los tokens exactos de `DESIGN.md` (colores, tipografía Inter + Courier Prime, radios de borde). Compílalo con el CLI de Tailwind integrado al pipeline de build de ASP.NET; no lo dejes vía CDN en la versión final. No rediseñes: traduce.

## 3. Cadena de conexión (XAMPP)

```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "WahlMiraiDb": "Server=localhost;Port=3306;Database=wahl_mirai_db;User=root;Password=;AllowUserVariables=True;"
  }
}
```

Documenta en el `README.md` del proyecto los pasos previos:
1. Levantar Apache + MySQL desde el panel de XAMPP.
2. Ejecutar `wahl_mirai_db_v2.2.sql` en phpMyAdmin (o `mysql -u root < wahl_mirai_db_v2.2.sql`) para crear el schema y los datos semilla.
3. Ajustar usuario/contraseña en `appsettings.Development.json` si el XAMPP local tiene contraseña de root distinta a vacía.

## 4. Arquitectura de carpetas (MVC estricto)

```
WahlMirai.Web/
├── Controllers/
│   ├── AuthController.cs         (login, cambio de clave forzado, logout)
│   ├── ElectorController.cs      (dashboard, tarjetón, voto, resultados con RN-4)
│   ├── AdminCensusController.cs  (CRUD censo, carga CSV, promoción de año lectivo)
│   ├── AdminEventsController.cs  (CRUD de voting_events + event_grades + candidates + candidate_proposals)
│   └── ResultsController.cs      (vista compartida de resultados; acceso irrestricto para ADMIN por RN-5)
├── Models/                       (entidades generadas por Scaffold-DbContext: Voter, Role, Grade, VotingEvent, Candidate, CandidateProposal, Vote, VoterEventParticipation, AuditLog, AcademicYear)
├── ViewModels/                   (LoginViewModel, BallotViewModel, CensusRowViewModel, PromotionPreviewViewModel, ResultsViewModel, etc.)
├── Services/
│   ├── IAuthService.cs / AuthService.cs           (validación de login, generación de clave documento.año, exigir cambio)
│   ├── ICensusService.cs / CensusService.cs       (alta, edición, eliminación lógica, restauración, búsqueda)
│   ├── IPromotionService.cs / PromotionService.cs (promoción automática transaccional, RF-M02-03)
│   ├── IVotingService.cs / VotingService.cs       (emitir voto sin voter_id, registrar participación, anti-duplicado)
│   └── IAuditService.cs / AuditService.cs         (registrar en audit_log: field_name/old_value/new_value)
├── Data/
│   └── WahlMiraiDbContext.cs      (generado por scaffolding; no editado a mano salvo relaciones)
├── Views/
│   ├── Auth/ (Login.cshtml, CambiarClave.cshtml)
│   ├── Elector/ (Dashboard.cshtml, Votar.cshtml, _ProposalModal.cshtml [partial])
│   ├── AdminCensus/ (Index.cshtml, _VoterModal.cshtml, _PromotionModal.cshtml, _CsvModal.cshtml)
│   ├── AdminEvents/ (Index.cshtml)
│   ├── Results/ (Index.cshtml)
│   └── Shared/ (_AdminLayout.cshtml con sidebar, _ElectorLayout.cshtml con navbar superior — replican los dos shells del prototipo)
└── wwwroot/ (css/, tailwind.config.js, imágenes)
```

**Restricción importante**: no colapses controladores/lógica en una sola clase "God Controller". Cada controlador debe delegar la lógica de negocio a su `Service` correspondiente — los controladores solo orquestan HTTP (recibir request, llamar servicio, devolver vista/redirect).

## 5. Reglas de negocio que el agente debe implementar en los Services (no en las vistas ni en JS)

Toma esto directamente de `ers_wahl_mirai_v2_2.md`; si hay ambigüedad, la ERS manda sobre el prototipo HTML.

- **RN-1**: todo alta de elector es creada por un Administrador (censo centralizado); no exponer ningún endpoint de auto-registro público.
- **RN-2**: login por documento, no por correo. Clave inicial = `{documento}.{añoLectivoVigente}`, generada por el backend al crear el elector, nunca por el cliente. Forzar `requiere_cambio_clave = true` en el alta.
- **RN-3 / RF-M05-01**: el voto se inserta en `votes` **sin** `voter_id`; el anti-duplicado se controla exclusivamente contra `voter_event_participations` dentro de la misma transacción de `POST /Elector/ConfirmarVoto`. Si la inserción en `votes` falla, el registro en `voter_event_participations` debe revertirse (transacción atómica).
- **RN-4**: bloquear `ResultsController` para electores si no existe su registro en `voter_event_participations` para ese `voting_event_id` — validar en el servidor, no confiar en el estado del cliente.
- **RN-5**: el rol `ADMIN` nunca pasa por la validación de RN-4.
- **RN-6 / RF-M02-03**: la promoción automática es una transacción que recorre `voters` con `status = 'ACTIVO'` y `excluir_de_promocion = 0`, usa `grades.sequence_order` para calcular el siguiente grado, marca `status = 'EGRESADO'` a quienes estén en `grades.is_last_grade = 1`, y al final escribe `academic_years.promotion_executed_at`. Debe impedir una segunda ejecución en el mismo año lectivo salvo confirmación explícita adicional (segundo POST con flag `force=true`).
- **RN-7**: "eliminar" un elector es un `UPDATE voters SET status='ELIMINADO', deleted_at=NOW()`. Ningún controlador debe ejecutar `DELETE FROM voters` bajo ninguna circunstancia.
- **RN-8**: cualquier `UPDATE`/`INSERT` sensible sobre `voters`, `voting_events` o `candidates` debe generar una fila en `audit_log` con `field_name`, `old_value`, `new_value` (no solo un JSON libre en `details`).

## 6. Autenticación y autorización

- Cookie authentication con dos políticas: `[Authorize(Roles="ADMIN")]` para `AdminCensusController`/`AdminEventsController`, `[Authorize(Roles="ELECTOR")]` para `ElectorController`.
- Middleware que, si `requiere_cambio_clave == true`, redirige cualquier request (excepto `Auth/CambiarClave`) a la vista de cambio de clave — replica el `ForcePasswordView` del prototipo pero como guard de servidor, no como pantalla que el cliente puede saltarse.

## 7. Qué NO debe hacer el agente

- No reintroducir un campo de "sección/salón paralelo" (11A, 10B) — el prototipo lo corrigió a solo grado (`11°`) por RN-6; mantenlo así en la base de datos y las vistas.
- No implementar recuperación de contraseña por correo electrónico para electores (contradice RN-2); el reset de clave de un elector es una acción del Administrador desde `AdminCensusController` (endpoint `ResetPassword` que reasigna `documento.añoVigente` y vuelve a poner `requiere_cambio_clave = true`).
- No usar Tailwind vía CDN en la versión final (`cdn.tailwindcss.com` es solo para prototipado); compílalo con el CLI de Tailwind como parte del build.
- No pongas lógica de negocio (cálculo de promoción, validación de voto único, generación de clave) en JavaScript del lado del cliente ni en los `.cshtml`; todo eso vive en `Services/`.

## 8. Entregable esperado del agente

1. Proyecto ASP.NET Core MVC compilable con `dotnet build` y ejecutable con `dotnet run`, apuntando a MySQL de XAMPP.
2. Las pantallas del prototipo (login, cambio de clave, dashboard elector, tarjetón + modal de propuestas, resultados con bloqueo condicional, dashboard admin, censo con CRUD + eliminación lógica + restauración + carga CSV, modal de promoción, resultados admin) migradas a Razor Views, visualmente fieles a `wahl_mirai_web.html` y `DESIGN.md`.
3. Un `README.md` con los pasos de arranque local sobre XAMPP (sección 3 de este documento).
4. Al terminar, que el agente liste explícitamente qué reglas de negocio de la sección 5 quedaron completamente implementadas y cuáles quedaron pendientes o simplificadas, para que puedan revisarse antes de considerar el proyecto listo para producción.
