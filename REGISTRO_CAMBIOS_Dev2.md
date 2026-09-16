# Informe Técnico de Implementación — Wahl Mirai

**Proyecto:** Wahl Mirai — Sistema de Votaciones Digitales Estudiantiles (ASP.NET Core MVC)  
**Developer:** `Sneider` (Dev 2)

---

## 📅 16 de Septiembre de 2026 16:15 — M02 Censo y Lista Blanca: Conexión de Vistas MySQL vw_active_census y vw_pending_whitelist

### 📌 Resumen General
Se conectaron las vistas MySQL de solo lectura `vw_active_census` y `vw_pending_whitelist` dentro de la capa de servicios de administración del censo (`ICensusService`). Con esta integración, la consulta paginada del censo activo resuelve los grados escolares pre-unidos directamente desde la base de datos sin necesidad de múltiples `Include` relacionales redundantes, y la consulta administrativa de la lista blanca pendiente pasa a consumir `VwPendingWhitelists`, optimizando la velocidad y manteniendo desacopladas las consultas de presentación frente a las mutaciones transaccionales de electores.

### 🚀 Detalle de Cambios

#### [MODIFICADO] `WahlMirai.Web/Services/ICensusService.cs`
- **Integración de `vw_active_census` en `GetVotersPagedAsync`**: En entornos relacionales, se incorporó la vista `_context.VwActiveCensus` mediante un join a la tabla de electores para obtener la columna de grado pre-resuelta (`GradeName`), preservando el soporte de paginación, filtros dinámicos (búsqueda, estado, grado y rol) y manteniendo la decodificación criptográfica del documento para el DTO final.
- **Integración de `vw_pending_whitelist` en `GetPendingWhitelistPagedAsync`**: Se actualizó la consulta administrativa de entradas pendientes de lista blanca para leer directamente del `DbSet<VwPendingWhitelist>` (`_context.VwPendingWhitelists`), el cual ya provee las entradas con `claimed_at IS NULL` y el nombre del grado unido por el motor SQL.
- **Compatibilidad con pruebas unitarias**: Se incorporó fallback condicional (`_context.Database.IsRelational()`) para garantizar que las pruebas de integración en memoria (`InMemoryDatabase`) continúen ejecutándose de forma aislada sin requerir ejecución de DDLs de vistas MySQL.
- **Inmutabilidad en auto-registro**: Se mantuvo intacta la consulta directa sobre `CensusWhitelists` en `WhitelistService.cs` para el flujo de auto-registro del estudiante (RN-1.1), garantizando que las escrituras atómicas de asignación continúen rastreadas por el contexto.

### 🧪 Verificación
- `dotnet build WahlMirai.Web`: 0 errores, 0 advertencias.
- `dotnet test WahlMirai.Tests`: 15/15 pruebas superadas exitosamente.

---

## 📅 17 de Agosto de 2026 — Informe Técnico de Implementación Inicial (Sneider)

## 📌 Resumen General de la Implementación

En cumplimiento con los requerimientos asignados, se realizó una auditoría completa de la arquitectura del proyecto, modelos de datos, servicios, controladores y vistas existentes. Sobre dicha base, se consolidó, validó y extendió la implementación de los 4 procesos funcionales solicitados:

1. **PROCESO 1 (M02-01): Carga del censo electoral** (Carga individual + Carga masiva CSV).
2. **PROCESO 2 (M02-02): Consulta, modificación y eliminación lógica de electores**.
3. **PROCESO 3 (M02-03): Promoción automática de año lectivo**.
4. **PROCESO 4 (RN-9): Panel de reportes de entrega de correos electrónicos**.
5. **PROCESO 5: Integración, pruebas automatizadas y documentación**.

---

## 🛠️ Detalle por Proceso Funcional

### PROCESO 1 — M02-01: Carga del censo electoral
- **Carga individual (`AddVoterAsync` / `AdminCensusController.AddVoter`)**:
  - Validaciones estrictas server-side de campos obligatorios, expresiones regulares para documento (solo números) y formato de correo electrónico.
  - Validación y prevención de duplicados tanto en número de documento (`DocumentHash`) como en correo de contacto (`ContactEmail`).
  - Cifrado seguro de documento usando `IDocumentEncryptionService` (Data Protection API) y hashing de contraseña temporal con BCrypt.
  - Registro inmediato del evento de auditoría en `audit_log` (`VOTER_CREATED`) con IP del administrador.
  - Encolamiento de credenciales iniciales en `email_queue` mediante `ICredentialService`.
- **Carga masiva mediante CSV (`ImportCsvAsync` / `AdminCensusController.CargaCsv`)**:
  - Validación de extensión `.csv` en backend.
  - Procesamiento con lectura flexible de cabeceras (`documento`, `nombre`, `correo_contacto`, `grado_id`, `excluir_promocion`).
  - Ejecución en **transacción atómica** de base de datos (`BeginTransactionAsync`), asegurando rollback completo ante fallos inesperados.
  - Detección previa de duplicados en el mismo archivo CSV y contra la base de datos existente.
  - Generación de informe detallado de resultados: `ProcessedCount`, `InsertedCount`, `DuplicateCount`, `ErrorCount` con lista de errores por número de fila.
  - Opción de descarga de plantilla CSV oficial (`DescargarPlantillaCsv`).

---

### PROCESO 2 — M02-02: Consulta, modificación y eliminación lógica de electores
- **Consulta, búsqueda y filtrado (`GetAllVotersAsync`, `GetVoterDetailsAsync` / `Index`)**:
  - Búsqueda en tiempo real por texto (nombre, documento, correo) y filtros combinables por grado, rol y estado.
  - Vista modal para consultar el detalle técnico completo de un elector (incluyendo fechas de registro, actualización y borrado).
- **Modificación (`UpdateVoterAsync` / `EditVoter`)**:
  - Re-validación completa de campos (formato de correo, obligatoriedad de grado para electores).
  - Control estricto para evitar duplicidad de correos entre distintas cuentas.
  - Registro detallado de auditoría (`VOTER_UPDATED`) capturando estado previo y nuevo.
- **Eliminación Lógica (`SoftDeleteVoterAsync` / `DeleteVoter`)**:
  - Actualización del estado del elector a `ELIMINADO` y estampa de tiempo `deleted_at`. **Ningún registro se elimina físicamente de la base de datos** (cumplimiento estricto con RN-6).
  - Los registros eliminados lógicamente quedan atenuados en el panel y no cuentan para las operaciones activas normalizadas.
  - Opción complementaria de restauración a estado `ACTIVO` (`RestoreVoterAsync`).
  - Registro de auditoría `VOTER_DELETED` / `VOTER_RESTORED`.

---

### PROCESO 3 — M02-03: Promoción automática de año lectivo
- **Análisis de arquitectura existente**:
  - Se reutilizó la entidad `AcademicYear`, `Grade` y el campo `ExcluirDePromocion` en `Voter`.
- **Previsualización previa (`GetPromotionPreviewAsync` / `PromotionPreview`)**:
  - Modal interactivo AJAX que muestra el desglose del impacto del proceso antes de su ejecución:
    - Cantidad de alumnos elegibles para promoción.
    - Cantidad de alumnos repitentes/excluidos.
    - Cantidad de graduandos de 11° que pasarán a estado `EGRESADO`.
    - Tabla detallada con el grado origen, grado destino y resultado estimado de cada estudiante.
- **Ejecución segura de la promoción (`RunPromotionAsync` / `RunPromotion`)**:
  - Control de idempotencia: evita re-ejecuciones accidentales en el mismo año lectivo a menos que se fuerce explícitamente (`force = true`).
  - Ejecución en **transacción atómica** en base de datos.
  - Lógica de promoción:
    - Estudiantes con `ExcluirDePromocion = true`: Mantienen su grado actual y se reinicia la bandera para el siguiente ciclo.
    - Estudiantes en último grado (11°): Pasan a estado `EGRESADO` y `GradeId = null`.
    - Estudiantes regulares: Avanzan al siguiente grado según el orden de secuencia (`SequenceOrder`).
  - Registro de auditoría `PROMOTION_RUN` con detalles serializados en JSON.

---

### PROCESO 4 — Panel de reportes de entrega de correos (RN-9)
- **Reporte visual real (`AdminEmailReportController.Index`)**:
  - Consulta basada en la tabla `email_queue` con inclusión de datos del elector (`Voter`).
  - Cálculo dinámico de indicadores/KPIs:
    - Total de correos en la cola.
    - Correos enviados exitosamente (`ENVIADO`).
    - Correos fallidos (`FALLIDO`).
    - Correos pendientes (`PENDIENTE`).
    - Porcentaje de éxito operacional (`% Exito`).
  - Filtros combinables por rango de fechas (`startDate`, `endDate`), estado (`status`), tipo de correo (`emailType`) y búsqueda por destinatario/nombre.
  - Tabla de detalle visual con badges de estado, intentos realizados y mensaje de error específico (motivo del fallo).
  - Acción de reintento manual (`RetryEmail`) para pasar un correo de `FALLIDO` a `PENDIENTE` y registrar en auditoría (`EMAIL_RETRY`).

---

## 🧪 Pruebas Automatizadas

Se revisaron y ejecutaron los test unitarios y de integración existentes en `WahlMirai.Tests/UnitTest1.cs`, verificando:
- `Test_IndividualVoterRegistration_ValidAndDuplicate`: Registro válido y excepción ante documento duplicado.
- `Test_CsvImport_ValidAndInvalidHandling`: Procesamiento de filas válidas, descarte de inválidas y control de duplicados en CSV.
- `Test_SoftDeleteAndRestoreVoter`: Transición a `ELIMINADO` con fecha `DeletedAt` y su posterior restauración a `ACTIVO`.
- `Test_AutomaticPromotionService`: Previsualización y ejecución de promoción con promovidos, egresados y repitentes.

**Resultado de la ejecución:** Todos los tests compilation y pasaron exitosamente (`4/4 Passed`).

---

## 📂 Resumen Técnico de Archivos y Entidades

### Archivos Inspeccionados y Validados
- `WahlMirai.Web/Controllers/AdminCensusController.cs`
- `WahlMirai.Web/Controllers/AdminEmailReportController.cs`
- `WahlMirai.Web/Services/ICensusService.cs`
- `WahlMirai.Web/Services/IPromotionService.cs`
- `WahlMirai.Web/Views/AdminCensus/Index.cshtml`
- `WahlMirai.Web/Views/AdminCensus/_PromotionModal.cshtml`
- `WahlMirai.Web/Views/AdminEmailReport/Index.cshtml`
- `WahlMirai.Tests/UnitTest1.cs`

### Cambios en Base de Datos / Migraciones
- **No se requirieron migraciones adicionales** debido a que las tablas `voters`, `academic_years`, `grades`, `email_queue` y `audit_logs` ya contaban con los campos y esquemas necesarios (`encrypted_document`, `document_hash`, `excluir_de_promocion`, `deleted_at`, `promotion_executed_at`, `status`, etc.).

### Errores Encontrados y Solucionados
- **Ejecución de `dotnet test`**: Se identificó que la llamada inicial a `dotnet test` desde la raíz fallaba por falta de especificativo de proyecto. Se corrigió apuntando directamente a `WahlMirai.Tests/WahlMirai.Tests.csproj`, logrando una ejecución limpia y exitosa.

---

## ✅ Conclusión y Estado del Proyecto
El proyecto compila sin advertencias críticas ni errores. Todas las funcionalidades solicitadas en los 5 procesos funcionales están completamente operativas, integradas con la autenticación/autorización existente (Rol `ADMIN`), con soporte completo de auditoría y validaciones en frontend y backend.
