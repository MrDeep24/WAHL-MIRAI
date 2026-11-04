# Diccionario de Datos — Sistema Wahl Mirai (Versión 3.1)
## Esquema Relacional de Base de Datos (`wahl_mirai_db`)

El presente documento constituye la especificación formal del modelo de persistencia relacional del sistema de elecciones digitales **Wahl Mirai**, de acuerdo con el estándar técnico exigido por el Manual Técnico de Referencia (Sección 3.2). 

La base de datos opera sobre el motor **MySQL 8.0+** (o MariaDB 10.4+ en entornos XAMPP), configurada con juego de caracteres `utf8mb4` y colación `utf8mb4_unicode_ci`. Está conformada por **17 tablas físicas normalizadas** bajo el motor transaccional `InnoDB` y **5 vistas SQL optimizadas** mapeadas en Entity Framework Core.

---

### Convenciones de Llaves
* **PK:** Llave Primaria (Primary Key).
* **FK:** Llave Foránea (Foreign Key) con integridad referencial declarada.
* **UK:** Llave Única (Unique Key / Índice de unicidad).
* **N/A:** Campo estándar no indizado o con índice ordinario de búsqueda (`KEY`).

---

## 1. Diccionario de Tablas Físicas (17 Tablas)

### 1.1 Tabla: `roles`
Catálogo maestro de roles y niveles de autorización en la aplicación.

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `roles` | `id` | TINYINT UNSIGNED | 3 | PK | Identificador autonumérico del rol (1=ELECTOR, 2=ADMIN, 3=SUPER_ADMIN). |
| `roles` | `name` | VARCHAR | 30 | UK | Nombre único del rol (`ELECTOR`, `ADMIN`, `SUPER_ADMIN`). |
| `roles` | `description` | VARCHAR | 100 | N/A | Descripción del alcance operativo y facultades del rol. |

---

### 1.2 Tabla: `academic_years`
Control del año lectivo vigente y barrera contra doble ejecución de promoción anual (RF-M02-02).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `academic_years` | `id` | SMALLINT UNSIGNED | 5 | PK | Identificador autonumérico del año escolar. |
| `academic_years` | `year` | SMALLINT UNSIGNED | 5 | UK | Año lectivo en cuatro dígitos (ej. 2026). Único en la tabla. |
| `academic_years` | `is_current` | TINYINT(1) | 1 | N/A | Bandera booleana: `1` = año lectivo activo (único permitido en 1). |
| `academic_years` | `promotion_executed_at` | DATETIME | - | N/A | Marca de tiempo en que se ejecutó la promoción masiva. `NULL` si aún no corre. |
| `academic_years` | `created_at` | DATETIME | - | N/A | Fecha y hora UTC del registro del año lectivo. |

---

### 1.3 Tabla: `grades`
Catálogo secuencial de grados académicos, base del cálculo para la promoción automática.

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `grades` | `id` | TINYINT UNSIGNED | 3 | PK | Identificador único del grado. |
| `grades` | `name` | VARCHAR | 10 | UK | Nombre representativo del grado (ej. 6°, 7°, ..., 11°). |
| `grades` | `sequence_order` | TINYINT UNSIGNED | 3 | UK | Orden ordinal para transicionar al siguiente grado en la promoción anual. |
| `grades` | `is_last_grade` | TINYINT(1) | 1 | N/A | `1` = último grado institucional (al promover, el elector pasa a estado `EGRESADO`). |

---

### 1.4 Tabla: `census_whitelist`
Lista blanca institucional preautorizada para auto-registro (RN-1, RN-1.1, RF-M02-00). No constituye cuenta de acceso.

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `census_whitelist` | `id` | INT UNSIGNED | 10 | PK | Identificador único de la entrada censal preautorizada. |
| `census_whitelist` | `document_hash` | CHAR | 64 | UK | Hash determinístico SHA-256 del documento de identidad para búsqueda exacta. |
| `census_whitelist` | `encrypted_document` | VARCHAR | 500 | N/A | Documento de identidad cifrado reversiblemente con AES-256 (Data Protection). |
| `census_whitelist` | `full_name` | VARCHAR | 150 | N/A | Nombres y apellidos completos del estudiante. |
| `census_whitelist` | `contact_email` | VARCHAR | 150 | N/A | Correo preautorizado para verificación OTP en auto-registro. `NULL` si no fue provisto. |
| `census_whitelist` | `grade_id` | TINYINT UNSIGNED | 3 | FK | Referencia a `grades.id`. Grado escolar que cursa el estudiante. |
| `census_whitelist` | `excluir_de_promocion` | TINYINT(1) | 1 | N/A | `1` = estudiante repitente; se omite en la promoción masiva. |
| `census_whitelist` | `claimed_at` | DATETIME | - | N/A | Fecha y hora en que la entrada fue reclamada en auto-registro. `NULL` = sin reclamar. |
| `census_whitelist` | `claimed_by_user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` de la cuenta de elector creada. `NULL` mientras no se reclame. |
| `census_whitelist` | `uploaded_by_user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` de la cuenta administrativa que cargó la entrada o CSV. |
| `census_whitelist` | `created_at` | DATETIME | - | N/A | Fecha y hora UTC de la carga inicial de la entrada en la lista blanca. |

---

### 1.5 Tabla: `users`
Cuentas operativas del sistema: electores auto-registrados y personal administrativo (RN-2, RN-6, RN-13).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `users` | `id` | INT UNSIGNED | 10 | PK | Identificador autonumérico de la cuenta de usuario. |
| `users` | `role_id` | TINYINT UNSIGNED | 3 | FK | Referencia a `roles.id` (`ELECTOR`, `ADMIN`, `SUPER_ADMIN`). |
| `users` | `grade_id` | TINYINT UNSIGNED | 3 | FK | Referencia a `grades.id`. Grado escolar (obligatorio para electores, `NULL` para admins). |
| `users` | `document_hash` | CHAR | 64 | UK | Hash determinístico SHA-256 del documento para autenticación y unicidad. |
| `users` | `encrypted_document` | VARCHAR | 500 | N/A | Documento de identidad cifrado reversiblemente con AES-256. |
| `users` | `full_name` | VARCHAR | 150 | N/A | Nombres y apellidos completos del usuario. |
| `users` | `contact_email` | VARCHAR | 150 | N/A | Correo electrónico de contacto para notificaciones y recuperación (RN-2.1). |
| `users` | `password_hash` | VARCHAR | 255 | N/A | Hash seguro de la contraseña generado mediante algoritmo BCrypt. |
| `users` | `position_title` | VARCHAR | 100 | N/A | Cargo institucional en texto libre (solo cuentas `ADMIN` o `SUPER_ADMIN`). |
| `users` | `excluir_de_promocion` | TINYINT(1) | 1 | N/A | `1` = omitir al elector de la promoción masiva anual (repitente). |
| `users` | `status` | ENUM | - | N/A | Estado de la cuenta: `'ACTIVO'`, `'INACTIVO'`, `'ELIMINADO'`, `'EGRESADO'`. |
| `users` | `deleted_at` | DATETIME | - | N/A | Marca temporal de baja lógica (soft-delete); `NULL` si la cuenta está vigente. |
| `users` | `registered_at` | DATETIME | - | N/A | Fecha y hora UTC del registro o creación de la cuenta. |
| `users` | `updated_at` | DATETIME | - | N/A | Fecha y hora UTC de la última actualización del registro (automático MySQL). |

---

### 1.6 Tabla: `email_queue`
Cola transaccional para envío progresivo de notificaciones con control de tasa (RN-9).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `email_queue` | `id` | BIGINT UNSIGNED | 20 | PK | Identificador autonumérico del mensaje en cola. |
| `email_queue` | `user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` del destinatario del correo. |
| `email_queue` | `email_type` | ENUM | - | N/A | Tipo de notificación: `'RECUPERACION_ACCESO'`, `'REASIGNACION_ADMIN'`, `'CAMBIO_PERFIL'`, `'RESPUESTA_PQR'`, `'CANDIDATURA_APROBADA'`, `'CANDIDATURA_RECHAZADA'`. |
| `email_queue` | `status` | ENUM | - | N/A | Estado del despacho: `'PENDIENTE'`, `'ENVIADO'`, `'FALLIDO'`. |
| `email_queue` | `attempts` | TINYINT UNSIGNED | 3 | N/A | Contador de intentos de envío efectuados (máximo 3). |
| `email_queue` | `error_message` | TEXT | - | N/A | Detalle técnico del fallo SMTP; `NULL` si el envío fue exitoso. |
| `email_queue` | `created_at` | DATETIME | - | N/A | Fecha y hora UTC de encolamiento del mensaje. |
| `email_queue` | `sent_at` | DATETIME | - | N/A | Fecha y hora UTC en que el worker completó el envío exitoso. |

---

### 1.7 Tabla: `election_positions`
Catálogo maestro de cargos de representación escolar (Personero, Contralor, etc., RF-M03-00, RN-11).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `election_positions` | `id` | INT UNSIGNED | 10 | PK | Identificador autonumérico del cargo electoral. |
| `election_positions` | `name` | VARCHAR | 100 | UK | Nombre descriptivo único del cargo institucional. |
| `election_positions` | `description` | TEXT | - | N/A | Detalle y responsabilidades del cargo escolar. |
| `election_positions` | `status` | ENUM | - | N/A | Estado del cargo: `'ACTIVO'`, `'INACTIVO'`. |
| `election_positions` | `created_at` | DATETIME | - | N/A | Fecha y hora UTC de creación del cargo. |

---

### 1.8 Tabla: `position_requirements`
Requisitos y soportes documentales exigidos para postularse a un cargo electoral (RN-11).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `position_requirements` | `id` | INT UNSIGNED | 10 | PK | Identificador autonumérico del requisito documental. |
| `position_requirements` | `position_id` | INT UNSIGNED | 10 | FK | Referencia a `election_positions.id` al que aplica el requisito. |
| `position_requirements` | `description` | VARCHAR | 255 | N/A | Texto descriptivo del requisito (ej. "Certificado de notas"). |
| `position_requirements` | `is_mandatory` | TINYINT(1) | 1 | N/A | `1` = requisito indispensable para postulación; `0` = opcional. |
| `position_requirements` | `display_order` | TINYINT UNSIGNED | 3 | N/A | Orden correlativo de presentación en el formulario de postulación. |

---

### 1.9 Tabla: `voting_events`
Procesos electorales configurados con las tres ventanas cronológicas de etapa (RN-12).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `voting_events` | `id` | INT UNSIGNED | 10 | PK | Identificador autonumérico del evento electoral. |
| `voting_events` | `created_by_user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` de la cuenta administrativa que creó la elección. |
| `voting_events` | `position_id` | INT UNSIGNED | 10 | FK | Referencia a `election_positions.id` que se elige en este proceso. |
| `voting_events` | `title` | VARCHAR | 200 | N/A | Título oficial de la jornada o proceso electoral. |
| `voting_events` | `description` | TEXT | - | N/A | Descripción ampliada o instrucciones de la elección. |
| `voting_events` | `election_type` | ENUM | - | N/A | Tipo de elección: `'PERSONAS'` (candidatos) o `'TEMAS'` (plebiscitos). |
| `voting_events` | `registration_start_date` | DATE | - | N/A | Etapa 1 (Inscripción): Fecha de inicio. |
| `voting_events` | `registration_start_time` | TIME | - | N/A | Etapa 1 (Inscripción): Hora de inicio. |
| `voting_events` | `registration_end_date` | DATE | - | N/A | Etapa 1 (Inscripción): Fecha de cierre. |
| `voting_events` | `registration_end_time` | TIME | - | N/A | Etapa 1 (Inscripción): Hora de cierre. |
| `voting_events` | `proposals_start_date` | DATE | - | N/A | Etapa 2 (Propuestas): Fecha de inicio. |
| `voting_events` | `proposals_start_time` | TIME | - | N/A | Etapa 2 (Propuestas): Hora de inicio. |
| `voting_events` | `proposals_end_date` | DATE | - | N/A | Etapa 2 (Propuestas): Fecha de cierre. |
| `voting_events` | `proposals_end_time` | TIME | - | N/A | Etapa 2 (Propuestas): Hora de cierre. |
| `voting_events` | `voting_start_date` | DATE | - | N/A | Etapa 3 (Votación): Fecha de apertura del sufragio. |
| `voting_events` | `voting_start_time` | TIME | - | N/A | Etapa 3 (Votación): Hora de apertura del sufragio. |
| `voting_events` | `voting_end_date` | DATE | - | N/A | Etapa 3 (Votación): Fecha de clausura del sufragio. |
| `voting_events` | `voting_end_time` | TIME | - | N/A | Etapa 3 (Votación): Hora de clausura del sufragio. |
| `voting_events` | `status` | ENUM | - | N/A | Estado calculado/asignado: `'PROGRAMADA'`, `'INSCRIPCION'`, `'PROPUESTAS'`, `'ACTIVA'`, `'FINALIZADA'`, `'ELIMINADO'`. |
| `voting_events` | `deleted_at` | DATETIME | - | N/A | Marca temporal de baja lógica (RN-7.1); `NULL` si está vigente. |
| `voting_events` | `created_at` | DATETIME | - | N/A | Fecha y hora UTC de creación del registro. |
| `voting_events` | `updated_at` | DATETIME | - | N/A | Fecha y hora UTC de la última modificación en base de datos. |

---

### 1.10 Tabla: `event_grades`
Mapeo de los grados escolares habilitados para votar en cada elección concreta.

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `event_grades` | `id` | INT UNSIGNED | 10 | PK | Identificador autonumérico del registro de habilitación. |
| `event_grades` | `voting_event_id` | INT UNSIGNED | 10 | FK | Referencia a `voting_events.id`. Elección configurada. |
| `event_grades` | `grade_id` | TINYINT UNSIGNED | 3 | FK | Referencia a `grades.id`. Grado escolar con derecho al sufragio en la elección. |

---

### 1.11 Tabla: `candidates`
Registro de candidaturas autopostuladas y votos en blanco institucionales (RF-M04-01, RN-10).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `candidates` | `id` | INT UNSIGNED | 10 | PK | Identificador autonumérico de la candidatura. |
| `candidates` | `voting_event_id` | INT UNSIGNED | 10 | FK | Referencia a `voting_events.id` en el que participa el candidato. |
| `candidates` | `user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` del elector postulado (`NULL` si es voto en blanco). |
| `candidates` | `name` | VARCHAR | 150 | N/A | Nombre oficial que aparece en el tarjetón electoral. |
| `candidates` | `slogan` | TEXT | - | N/A | Lema de campaña electoral del candidato. |
| `candidates` | `photo_url` | VARCHAR | 500 | N/A | Ruta al archivo de imagen o fotografía cifrada (`.enc`). |
| `candidates` | `government_plan_url` | VARCHAR | 500 | N/A | Ruta al archivo PDF del plan de gobierno cifrado (`.enc`). |
| `candidates` | `is_blank_vote` | TINYINT(1) | 1 | N/A | `1` = casilla de Voto en Blanco institucional. |
| `candidates` | `status` | ENUM | - | N/A | Estado de revisión: `'PENDIENTE'`, `'APROBADO'`, `'RECHAZADO'`, `'RETIRADO'`. |
| `candidates` | `approved_with_exceptions` | TINYINT(1) | 1 | N/A | `1` = aprobado administrativamente con requisitos faltantes (RN-10.1). |
| `candidates` | `exceptions_detail` | TEXT | - | N/A | Detalle de los requisitos pendientes al aprobar con excepción. |
| `candidates` | `rejection_reason` | TEXT | - | N/A | Motivo obligatorio registrado por el administrador al rechazar. |
| `candidates` | `allow_correction` | TINYINT(1) | 1 | N/A | `1` = el estudiante tiene permiso para subsanar y reenviar soportes. |
| `candidates` | `reviewed_by_user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` del administrador que dictaminó la candidatura. |
| `candidates` | `reviewed_at` | DATETIME | - | N/A | Fecha y hora UTC de la aprobación, rechazo o dictamen administrativo. |
| `candidates` | `enrolled_at` | DATETIME | - | N/A | Fecha y hora UTC en que el elector radicó la autopostulación inicial. |

---

### 1.12 Tabla: `candidate_proposals`
Puntos programáticos y propuestas resumidas presentadas por el candidato (RF-M04-01).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `candidate_proposals` | `id` | INT UNSIGNED | 10 | PK | Identificador autonumérico de la propuesta. |
| `candidate_proposals` | `candidate_id` | INT UNSIGNED | 10 | FK | Referencia a `candidates.id` al que pertenece la propuesta. |
| `candidate_proposals` | `content` | TEXT | - | N/A | Texto de la propuesta programática. |
| `candidate_proposals` | `display_order` | TINYINT UNSIGNED | 3 | N/A | Posición de despliegue en la ventana modal de consulta previa al voto. |

---

### 1.13 Tabla: `candidacy_documents`
Archivos de soporte radicados por el candidato para sustentar requisitos del cargo (RN-11).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `candidacy_documents` | `id` | INT UNSIGNED | 10 | PK | Identificador autonumérico del soporte documental radicado. |
| `candidacy_documents` | `candidate_id` | INT UNSIGNED | 10 | FK | Referencia a `candidates.id` de la candidatura postulada. |
| `candidacy_documents` | `requirement_id` | INT UNSIGNED | 10 | FK | Referencia a `position_requirements.id` que este documento sustenta. |
| `candidacy_documents` | `file_url` | VARCHAR | 500 | N/A | Ruta al archivo en disco protegido cifrado con AES-256 (`.enc`). |
| `candidacy_documents` | `uploaded_at` | DATETIME | - | N/A | Fecha y hora UTC en que el aspirante cargó el documento. |

---

### 1.14 Tabla: `votes`
Urna electrónica: votos inmutables emitidos de forma estrictamente anónima (RN-3). No almacena `user_id`.

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `votes` | `id` | BIGINT UNSIGNED | 20 | PK | Identificador autonumérico del sufragio en la urna. |
| `votes` | `voting_event_id` | INT UNSIGNED | 10 | FK | Referencia a `voting_events.id`. Elección en la que se sufragó. |
| `votes` | `candidate_id` | INT UNSIGNED | 10 | FK | Referencia a `candidates.id` por quien se emitió el voto. |
| `votes` | `vote_hash` | VARCHAR | 64 | UK | Hash criptográfico SHA-256 generado al votar, para auditoría de integridad. |
| `votes` | `voted_at` | DATETIME | - | N/A | Marca de tiempo UTC en que se depositó el voto anónimo. |

---

### 1.15 Tabla: `event_participations`
Padrón de control anti-duplicado del sufragio (RN-3). No se relaciona con `votes`.

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `event_participations` | `id` | INT UNSIGNED | 10 | PK | Identificador autonumérico del registro de participación. |
| `event_participations` | `user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` del elector que ya emitió su voto. |
| `event_participations` | `voting_event_id` | INT UNSIGNED | 10 | FK | Referencia a `voting_events.id` en el que participó. |
| `event_participations` | `participated_at` | DATETIME | - | N/A | Fecha y hora UTC en que el elector consumió su derecho a voto en el evento. |

---

### 1.16 Tabla: `audit_log`
Pista forense inmutable de eventos de seguridad y operaciones sensibles (RN-8).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `audit_log` | `id` | BIGINT UNSIGNED | 20 | PK | Identificador autonumérico de la entrada de auditoría. |
| `audit_log` | `user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` del autor de la acción (`NULL` si fue el sistema). |
| `audit_log` | `action` | VARCHAR | 100 | N/A | Código de acción (`LOGIN_SUCCESS`, `SELF_REGISTER`, `USER_DELETED`, etc.). |
| `audit_log` | `target_entity` | VARCHAR | 200 | N/A | Nombre de la entidad o tabla objeto de la operación. |
| `audit_log` | `target_id` | INT | 11 | N/A | Identificador del registro objeto afectado. |
| `audit_log` | `field_name` | VARCHAR | 100 | N/A | Nombre del atributo específico modificado (si aplica). |
| `audit_log` | `old_value` | TEXT | - | N/A | Valor previo antes de la mutación. |
| `audit_log` | `new_value` | TEXT | - | N/A | Nuevo valor asentado tras la mutación. |
| `audit_log` | `details` | TEXT | - | N/A | Metadatos contextuales o estructura JSON adicional de la operación. |
| `audit_log` | `ip_address` | VARCHAR | 45 | N/A | Dirección IP remota observada del cliente (IPv4 o IPv6). |
| `audit_log` | `occurred_at` | DATETIME | - | N/A | Fecha y hora UTC exacta del evento forense. |

---

### 1.17 Tabla: `pqr_tickets`
Módulo institucional de peticiones, quejas, reclamos y sugerencias (RF-M08-01, RF-M08-02).

| Tabla | Campo | Tipo | Longitud | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `pqr_tickets` | `id` | BIGINT UNSIGNED | 20 | PK | Identificador autonumérico del radicado PQR. |
| `pqr_tickets` | `user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` del usuario que radicó el ticket. |
| `pqr_tickets` | `subject` | VARCHAR | 200 | N/A | Asunto conciso o categorización del motivo de la PQR. |
| `pqr_tickets` | `message` | TEXT | - | N/A | Cuerpo detallado del requerimiento, queja o sugerencia. |
| `pqr_tickets` | `status` | ENUM | - | N/A | Estado del ticket: `'ABIERTO'`, `'RESUELTO'`. |
| `pqr_tickets` | `admin_response` | TEXT | - | N/A | Respuesta institucional emitida por la coordinación; `NULL` mientras esté ABIERTO. |
| `pqr_tickets` | `responded_by_user_id` | INT UNSIGNED | 10 | FK | Referencia a `users.id` de la cuenta administrativa que resolvió la PQR. |
| `pqr_tickets` | `responded_at` | DATETIME | - | N/A | Fecha y hora UTC de la emisión de la respuesta institucional. |
| `pqr_tickets` | `created_at` | DATETIME | - | N/A | Fecha y hora UTC de radicación de la PQR por el elector. |
| `pqr_tickets` | `updated_at` | DATETIME | - | N/A | Fecha y hora UTC de última actualización del ticket. |

---

## 2. Diccionario de Vistas SQL Mapeadas (5 Vistas Keyless)

### 2.1 Vista: `vw_vote_counts` (`VwVoteCount.cs`)
Cálculo en tiempo real del escrutinio y porcentajes por candidato y evento. Excluye elecciones eliminadas lógicamente (`status != 'ELIMINADO'`).

| Vista | Campo | Tipo | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- |
| `vw_vote_counts` | `event_id` | INT UNSIGNED | N/A | Identificador del evento electoral (`voting_events.id`). |
| `vw_vote_counts` | `event_title` | VARCHAR(200) | N/A | Título oficial de la jornada electoral. |
| `vw_vote_counts` | `event_status` | ENUM | N/A | Estado actual de la elección (`ACTIVA`, `FINALIZADA`, etc.). |
| `vw_vote_counts` | `candidate_id` | INT UNSIGNED | N/A | Identificador del candidato (`candidates.id`). |
| `vw_vote_counts` | `candidate_name` | VARCHAR(150) | N/A | Nombre visible del candidato o Voto en Blanco en tarjetón. |
| `vw_vote_counts` | `is_blank_vote` | TINYINT(1) | N/A | `1` si corresponde al voto en blanco; `0` si es candidato postulado. |
| `vw_vote_counts` | `total_votes` | BIGINT | N/A | Número total de votos válidos acumulados por la opción. |
| `vw_vote_counts` | `vote_percentage`| DECIMAL(5,2) | N/A | Porcentaje calculado sobre el total de sufragios emitidos en el evento. |

---

### 2.2 Vista: `vw_active_census` (`VwActiveCensu.cs`)
Proyección optimizada del censo de electores activos e inactivos con unión precalculada del nombre textual del grado escolar.

| Vista | Campo | Tipo | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- |
| `vw_active_census` | `id` | INT UNSIGNED | N/A | Identificador del elector (`users.id`). |
| `vw_active_census` | `full_name` | VARCHAR(150) | N/A | Nombre y apellidos completos del elector. |
| `vw_active_census` | `contact_email` | VARCHAR(150) | N/A | Correo electrónico de contacto del estudiante. |
| `vw_active_census` | `grade` | VARCHAR(10) | N/A | Nombre textual del grado escolar resuelto vía JOIN (`grades.name`). |
| `vw_active_census` | `status` | ENUM | N/A | Estado operativo del elector (`ACTIVO`, `INACTIVO`). |
| `vw_active_census` | `excluir_de_promocion` | TINYINT(1) | N/A | `1` = marcado como repitente. |
| `vw_active_census` | `registered_at` | DATETIME | N/A | Fecha de registro en la plataforma. |
| `vw_active_census` | `updated_at` | DATETIME | N/A | Fecha de última modificación. |

---

### 2.3 Vista: `vw_pending_whitelist` (`VwPendingWhitelist.cs`)
Listado de entradas de la lista blanca aún no reclamadas (`claimed_at IS NULL`), agilizando el seguimiento administrativo de M02.

| Vista | Campo | Tipo | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- |
| `vw_pending_whitelist` | `id` | INT UNSIGNED | N/A | Identificador de la entrada censal (`census_whitelist.id`). |
| `vw_pending_whitelist` | `document_hash` | CHAR(64) | N/A | Hash SHA-256 del documento preautorizado. |
| `vw_pending_whitelist` | `encrypted_document` | VARCHAR(500) | N/A | Documento cifrado con AES-256 para visualización administrativa. |
| `vw_pending_whitelist` | `full_name` | VARCHAR(150) | N/A | Nombre completo del estudiante autorizado. |
| `vw_pending_whitelist` | `contact_email` | VARCHAR(150) | N/A | Correo electrónico de contacto preautorizado para OTP. |
| `vw_pending_whitelist` | `grade_id` | TINYINT UNSIGNED | N/A | Identificador foráneo del grado escolar. |
| `vw_pending_whitelist` | `grade` | VARCHAR(10) | N/A | Nombre legible del grado escolar (`grades.name`). |
| `vw_pending_whitelist` | `excluir_de_promocion` | TINYINT(1) | N/A | `1` = excluido de promoción automática. |
| `vw_pending_whitelist` | `created_at` | DATETIME | N/A | Fecha de carga en lista blanca. |

---

### 2.4 Vista: `vw_pending_email_queue` (`VwPendingEmailQueue.cs`)
Cola optimizada de mensajes pendientes de envío consumida directamente por `EmailQueueBackgroundService`.

| Vista | Campo | Tipo | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- |
| `vw_pending_email_queue` | `id` | BIGINT UNSIGNED | N/A | Identificador del mensaje en cola (`email_queue.id`). |
| `vw_pending_email_queue` | `user_id` | INT UNSIGNED | N/A | Identificador del usuario destinatario (`users.id`). |
| `vw_pending_email_queue` | `contact_email` | VARCHAR(150) | N/A | Dirección de correo de destino resuelta desde `users.contact_email`. |
| `vw_pending_email_queue` | `full_name` | VARCHAR(150) | N/A | Nombre completo del destinatario. |
| `vw_pending_email_queue` | `email_type` | ENUM | N/A | Tipo o plantilla de la notificación institucional. |
| `vw_pending_email_queue` | `attempts` | TINYINT UNSIGNED | N/A | Número de reintentos ya ejecutados por el worker. |
| `vw_pending_email_queue` | `created_at` | DATETIME | N/A | Fecha y hora en que se originó el mensaje. |

---

### 2.5 Vista: `vw_pending_candidacies` (`VwPendingCandidacy.cs`)
Proyección para agilizar la revisión administrativa de postulaciones (M04), calculando en subconsultas los requisitos obligatorios satisfechos.

| Vista | Campo | Tipo | Llave | Descripción |
| :--- | :--- | :--- | :--- | :--- |
| `vw_pending_candidacies` | `candidate_id` | INT UNSIGNED | N/A | Identificador de la candidatura (`candidates.id`). |
| `vw_pending_candidacies` | `voting_event_id` | INT UNSIGNED | N/A | Identificador de la elección en curso. |
| `vw_pending_candidacies` | `event_title` | VARCHAR(200) | N/A | Título oficial de la jornada electoral. |
| `vw_pending_candidacies` | `position_id` | INT UNSIGNED | N/A | Identificador del cargo electoral en disputa. |
| `vw_pending_candidacies` | `position_name` | VARCHAR(100) | N/A | Nombre del cargo institucional (`election_positions.name`). |
| `vw_pending_candidacies` | `candidate_name` | VARCHAR(150) | N/A | Nombre del estudiante postulado. |
| `vw_pending_candidacies` | `enrolled_at` | DATETIME | N/A | Fecha y hora en que se radicó la postulación. |
| `vw_pending_candidacies` | `mandatory_requirements` | BIGINT | N/A | Conteo total de requisitos documentales obligatorios definidos para el cargo. |
| `vw_pending_candidacies` | `mandatory_documents_uploaded` | BIGINT | N/A | Conteo de soportes obligatorios cargados efectivamente por el candidato. |

