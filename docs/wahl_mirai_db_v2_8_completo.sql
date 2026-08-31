-- ============================================================
--  WAHL MIRAI — Sistema de Votaciones Digitales Estudiantiles
--  Script DDL ÚNICO Y CONSOLIDADO — MySQL 8.0+
--  Base de datos: wahl_mirai_db
--  Versión: 2.8 — Alineado con ERS v2.8
--
--  Este archivo reemplaza cualquier versión anterior. Ejecútalo una sola vez
--  sobre una base de datos vacía (o recién eliminada) y queda todo listo:
--  schema completo + datos semilla.
--
--  Cambios v2.8 vs v2.7:
--  - `voters` se renombra a `users` (aloja electores y cuentas administrativas).
--  - Nueva tabla `census_whitelist` (lista blanca): reemplaza el alta directa
--    de cuentas; el Administrador ya no crea credenciales, solo autoriza
--    documentos. El elector completa su propio registro (RF-M01-00).
--  - `roles` agrega `SUPER_ADMIN`. `users` agrega `position_title` (texto
--    libre, solo informativo, para cuentas administrativas).
--  - Nuevas tablas `election_positions` y `position_requirements`: catálogo
--    de cargos electorales y sus requisitos documentales (RF-M03-00).
--  - `voting_events`: agrega `position_id`, se abren las tres ventanas de
--    etapa (inscripción, propuestas, votación) y el ENUM `status` incorpora
--    `INSCRIPCION` y `PROPUESTAS`.
--  - `candidates`: ahora se autopostula (antes lo asignaba el Admin).
--    Agrega `government_plan_url`, `approved_with_exceptions`,
--    `exceptions_detail` y `rejection_reason`.
--  - Nueva tabla `candidacy_documents`: soportes documentales cargados por
--    el candidato para cumplir los requisitos del cargo.
--  - `voter_event_participations` se renombra a `event_participations`
--    (columna `voter_id` -> `user_id`), en línea con el rename de `users`.
--  - `email_queue.email_type` agrega `CANDIDATURA_APROBADA` y
--    `CANDIDATURA_RECHAZADA`.
--  - Todas las columnas `voter_id` / `created_by_voter_id` /
--    `responded_by_voter_id` se renombran a `user_id` / `created_by_user_id` /
--    `responded_by_user_id` en las tablas que referencian a `users`.
--  - Se corrige el dato semilla del Administrador: el documento pasa a
--    `1020304050`; el `document_hash` se recalcula en consecuencia (en v2.7
--    el hash correspondía al documento anterior `admin.electoral` y había
--    quedado desincronizado tras el cambio de documento).
--
--  Cambios v2.7 vs v2.6: nueva tabla `pqr_tickets`, `email_queue.email_type`
--  agrega 'RESPUESTA_PQR'. Decisión de diseño: PQR no pasa por audit_log.
-- ============================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------------
-- Base de datos
-- ------------------------------------------------------------
DROP DATABASE IF EXISTS `wahl_mirai_db`;

CREATE DATABASE `wahl_mirai_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `wahl_mirai_db`;

-- ============================================================
-- 1. ROLES — Catálogo de roles del sistema
-- ============================================================
CREATE TABLE `roles` (
    `id`          TINYINT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `name`        VARCHAR(30)         NOT NULL COMMENT 'Ej: ELECTOR, ADMIN, SUPER_ADMIN',
    `description` VARCHAR(100)        NULL     DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_roles_name` (`name`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Catálogo de roles del sistema';

INSERT INTO `roles` (`name`, `description`) VALUES
    ('ELECTOR',    'Estudiante con derecho a votar y postularse como candidato'),
    ('ADMIN',      'Administrador electoral: censo, elecciones, candidaturas y PQR'),
    ('SUPER_ADMIN','Mismos permisos operativos que ADMIN, más la gestión exclusiva de cuentas administrativas (M09)');

-- ============================================================
-- 2. ACADEMIC_YEARS — Control del año lectivo vigente (RF-M02-02)
-- ============================================================
CREATE TABLE `academic_years` (
    `id`                     SMALLINT UNSIGNED  NOT NULL AUTO_INCREMENT,
    `year`                   SMALLINT UNSIGNED  NOT NULL COMMENT 'Ej: 2026',
    `is_current`             TINYINT(1)         NOT NULL DEFAULT 0 COMMENT '1 = año lectivo activo, solo uno a la vez',
    `promotion_executed_at`  DATETIME           NULL     DEFAULT NULL COMMENT 'NULL = aún no se corre la promoción este año',
    `created_at`             DATETIME           NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_academic_years_year` (`year`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Año lectivo vigente; controla bloqueo de doble promoción';

INSERT INTO `academic_years` (`year`, `is_current`) VALUES (2026, 1);

-- ============================================================
-- 3. GRADES — Catálogo secuencial de grados (RF-M02-02)
-- ============================================================
CREATE TABLE `grades` (
    `id`              TINYINT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `name`            VARCHAR(10)         NOT NULL COMMENT 'Ej: 6°, 7°, ..., 11°',
    `sequence_order`  TINYINT UNSIGNED    NOT NULL COMMENT 'Orden para calcular el siguiente grado al promover',
    `is_last_grade`   TINYINT(1)          NOT NULL DEFAULT 0 COMMENT '1 = al promover, el elector pasa a EGRESADO',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_grades_name`           (`name`),
    UNIQUE KEY `uq_grades_sequence_order` (`sequence_order`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Catálogo de grados escolares en orden, base de la promoción automática';

INSERT INTO `grades` (`name`, `sequence_order`, `is_last_grade`) VALUES
    ('6°',  1, 0),
    ('7°',  2, 0),
    ('8°',  3, 0),
    ('9°',  4, 0),
    ('10°', 5, 0),
    ('11°', 6, 1);

-- ============================================================
-- 4. CENSUS_WHITELIST — Lista blanca de auto-registro (RN-1, RN-1.1, RF-M02-00)
--     Reemplaza el alta directa de cuentas. Por sí sola NO es una cuenta de
--     acceso: no tiene correo ni contraseña. El elector la "reclama" al
--     completar su auto-registro (RF-M01-00), creando su fila en `users`.
-- ============================================================
CREATE TABLE `census_whitelist` (
    `id`                    INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `document_hash`         CHAR(64)        NOT NULL COMMENT 'SHA-256 determinístico del documento, para búsqueda en el auto-registro',
    `encrypted_document`    VARCHAR(500)    NOT NULL COMMENT 'Documento cifrado AES-256, mostrado en paneles administrativos',
    `full_name`             VARCHAR(150)    NOT NULL,
    `grade_id`               TINYINT UNSIGNED NOT NULL,
    `excluir_de_promocion`  TINYINT(1)      NOT NULL DEFAULT 0 COMMENT '1 = repitente, se omite en la promoción masiva mientras no se auto-registre',
    `claimed_at`            DATETIME        NULL     DEFAULT NULL COMMENT 'NULL = aún no reclamado; se completa al auto-registrarse (RN-1.1)',
    `claimed_by_user_id`    INT UNSIGNED    NULL     DEFAULT NULL COMMENT 'FK a users.id una vez reclamado',
    `uploaded_by_user_id`   INT UNSIGNED    NOT NULL COMMENT 'Administrador que cargó esta entrada',
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_whitelist_document_hash` (`document_hash`),
    KEY `idx_whitelist_grade_id`   (`grade_id`),
    KEY `idx_whitelist_claimed_at` (`claimed_at`),
    CONSTRAINT `fk_whitelist_grade`
        FOREIGN KEY (`grade_id`) REFERENCES `grades` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
    -- fk_whitelist_claimed_by y fk_whitelist_uploaded_by se agregan tras crear `users` (ver sección 5)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Lista blanca del censo: autoriza el auto-registro; no constituye cuenta de acceso (RN-1)';

-- ============================================================
-- 5. USERS — Cuentas del sistema (antes `voters`): electores y cuentas
--    administrativas (ADMIN / SUPER_ADMIN) (RN-2, RN-2.1, RN-6, RN-13)
-- ============================================================
CREATE TABLE `users` (
    `id`                     INT UNSIGNED        NOT NULL AUTO_INCREMENT,
    `role_id`                TINYINT UNSIGNED    NOT NULL,
    `grade_id`               TINYINT UNSIGNED    NULL     DEFAULT NULL COMMENT 'NULL para cuentas administrativas',
    `document_hash`          CHAR(64)            NOT NULL COMMENT 'SHA-256 determinístico del documento, usado para login/búsqueda',
    `encrypted_document`     VARCHAR(500)        NOT NULL COMMENT 'Documento cifrado AES-256 para mostrar/editar en UI',
    `full_name`              VARCHAR(150)        NOT NULL,
    `contact_email`          VARCHAR(150)        NOT NULL COMMENT 'Correo de contacto. Solo recuperación/notificaciones (RN-2.1), nunca login',
    `password_hash`          VARCHAR(255)        NOT NULL COMMENT 'Hash BCrypt; para electores la define el propio usuario en el auto-registro (RN-2)',
    `position_title`         VARCHAR(100)        NULL     DEFAULT NULL COMMENT 'Cargo institucional en texto libre (solo ADMIN/SUPER_ADMIN); no otorga permisos (RN-13)',
    `excluir_de_promocion`   TINYINT(1)          NOT NULL DEFAULT 0 COMMENT '1 = repitente, se omite en la promoción masiva',
    `status`                 ENUM('ACTIVO','INACTIVO','ELIMINADO','EGRESADO') NOT NULL DEFAULT 'ACTIVO',
    `deleted_at`             DATETIME            NULL     DEFAULT NULL COMMENT 'Fecha de eliminación lógica; NULL si no aplica',
    `registered_at`          DATETIME            NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`             DATETIME            NULL     DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_users_document_hash`  (`document_hash`),
    KEY `idx_users_contact_email`        (`contact_email`),
    KEY `idx_users_role_id`  (`role_id`),
    KEY `idx_users_grade_id` (`grade_id`),
    KEY `idx_users_status`   (`status`),
    CONSTRAINT `fk_users_role`
        FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `fk_users_grade`
        FOREIGN KEY (`grade_id`) REFERENCES `grades` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Cuentas del sistema: electores auto-registrados y cuentas administrativas (antes `voters`)';

-- Ahora que `users` existe, se completan las FKs pendientes de `census_whitelist`
ALTER TABLE `census_whitelist`
    ADD CONSTRAINT `fk_whitelist_claimed_by`
        FOREIGN KEY (`claimed_by_user_id`) REFERENCES `users` (`id`)
        ON DELETE SET NULL ON UPDATE CASCADE,
    ADD CONSTRAINT `fk_whitelist_uploaded_by`
        FOREIGN KEY (`uploaded_by_user_id`) REFERENCES `users` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE;

-- ============================================================
-- 6. EMAIL_QUEUE — Envío progresivo de notificaciones (RN-9)
-- ============================================================
CREATE TABLE `email_queue` (
    `id`            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `user_id`       INT UNSIGNED    NOT NULL,
    `email_type`    ENUM(
                        'RECUPERACION_ACCESO',
                        'REASIGNACION_ADMIN',
                        'CAMBIO_PERFIL',
                        'RESPUESTA_PQR',
                        'CANDIDATURA_APROBADA',
                        'CANDIDATURA_RECHAZADA'
                    ) NOT NULL COMMENT 'CREDENCIAL_INICIAL se retira en v2.8: el alta inicial ya no envía contraseña (RN-2)',
    `status`        ENUM('PENDIENTE','ENVIADO','FALLIDO') NOT NULL DEFAULT 'PENDIENTE',
    `attempts`      TINYINT UNSIGNED NOT NULL DEFAULT 0 COMMENT 'Número de intentos de envío realizados',
    `error_message` TEXT            NULL     DEFAULT NULL COMMENT 'Detalle del fallo; NULL si fue exitoso o aún no se procesa',
    `created_at`    DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `sent_at`       DATETIME        NULL     DEFAULT NULL COMMENT 'NULL hasta que la cola lo procese exitosamente',
    PRIMARY KEY (`id`),
    KEY `idx_eq_status`     (`status`),
    KEY `idx_eq_user`       (`user_id`),
    KEY `idx_eq_created_at` (`created_at`),
    CONSTRAINT `fk_eq_user`
        FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Cola de envío progresivo de notificaciones, con control de tasa (RN-9)';

-- ============================================================
-- 7. ELECTION_POSITIONS — Catálogo de cargos electorales (RF-M03-00, RN-11)
-- ============================================================
CREATE TABLE `election_positions` (
    `id`          INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `name`        VARCHAR(100)    NOT NULL COMMENT 'Ej: Personero, Contralor, Representante de Curso',
    `description` TEXT            NULL     DEFAULT NULL,
    `status`      ENUM('ACTIVO','INACTIVO') NOT NULL DEFAULT 'ACTIVO',
    `created_at`  DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_positions_name` (`name`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Catálogo reutilizable de cargos electorales';

-- ============================================================
-- 8. POSITION_REQUIREMENTS — Requisitos documentales por cargo (RN-11)
-- ============================================================
CREATE TABLE `position_requirements` (
    `id`             INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `position_id`    INT UNSIGNED    NOT NULL,
    `description`    VARCHAR(255)    NOT NULL COMMENT 'Ej: Certificado de haber cursado y aprobado 10°',
    `is_mandatory`   TINYINT(1)      NOT NULL DEFAULT 1,
    `display_order`  TINYINT UNSIGNED NOT NULL DEFAULT 1,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_pr_position_order` (`position_id`, `display_order`),
    KEY `idx_pr_position_id` (`position_id`),
    CONSTRAINT `fk_pr_position`
        FOREIGN KEY (`position_id`) REFERENCES `election_positions` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Requisitos de elegibilidad exigidos por cada cargo electoral';

-- ============================================================
-- 9. VOTING_EVENTS — Procesos electorales con etapas (RN-12)
-- ============================================================
CREATE TABLE `voting_events` (
    `id`                       INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `created_by_user_id`       INT UNSIGNED    NOT NULL COMMENT 'Cuenta administrativa creadora',
    `position_id`              INT UNSIGNED    NOT NULL COMMENT 'Cargo electoral asociado (RF-M03-00)',
    `title`                    VARCHAR(200)    NOT NULL COMMENT 'Nombre de la elección',
    `description`              TEXT            NULL     DEFAULT NULL,
    `election_type`            ENUM('PERSONAS','TEMAS') NOT NULL DEFAULT 'PERSONAS' COMMENT 'RF-M03-01',

    `registration_start_date`  DATE            NOT NULL COMMENT 'Etapa 1: Inscripción de Candidatos - inicio',
    `registration_start_time`  TIME            NOT NULL,
    `registration_end_date`    DATE            NOT NULL COMMENT 'Etapa 1: Inscripción de Candidatos - fin',
    `registration_end_time`    TIME            NOT NULL,

    `proposals_start_date`     DATE            NOT NULL COMMENT 'Etapa 2: Consulta de Propuestas - inicio',
    `proposals_start_time`     TIME            NOT NULL,
    `proposals_end_date`       DATE            NOT NULL COMMENT 'Etapa 2: Consulta de Propuestas - fin',
    `proposals_end_time`       TIME            NOT NULL,

    `voting_start_date`        DATE            NOT NULL COMMENT 'Etapa 3: Votación - inicio (antes start_date)',
    `voting_start_time`        TIME            NOT NULL,
    `voting_end_date`          DATE            NOT NULL COMMENT 'Etapa 3: Votación - fin (antes end_date)',
    `voting_end_time`          TIME            NOT NULL,

    `status`                   ENUM('PROGRAMADA','INSCRIPCION','PROPUESTAS','ACTIVA','FINALIZADA','ELIMINADO')
                                               NOT NULL DEFAULT 'PROGRAMADA'
                                               COMMENT 'Transición automática por fechas (RN-12); ELIMINADO = soft-delete (RN-7.1)',
    `deleted_at`               DATETIME        NULL     DEFAULT NULL COMMENT 'Fecha de eliminación lógica; NULL si no aplica',
    `created_at`               DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`                DATETIME        NULL     DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_ve_status`      (`status`),
    KEY `idx_ve_position_id` (`position_id`),
    KEY `idx_ve_registration` (`registration_start_date`, `registration_end_date`),
    KEY `idx_ve_proposals`    (`proposals_start_date`, `proposals_end_date`),
    KEY `idx_ve_voting`       (`voting_start_date`, `voting_end_date`),
    KEY `idx_ve_created_by`  (`created_by_user_id`),
    CONSTRAINT `fk_ve_created_by`
        FOREIGN KEY (`created_by_user_id`) REFERENCES `users` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `fk_ve_position`
        FOREIGN KEY (`position_id`) REFERENCES `election_positions` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `chk_ve_registration_dates`
        CHECK (
            (`registration_end_date` > `registration_start_date`)
            OR (`registration_end_date` = `registration_start_date` AND `registration_end_time` > `registration_start_time`)
        ),
    CONSTRAINT `chk_ve_proposals_dates`
        CHECK (
            (`proposals_end_date` > `proposals_start_date`)
            OR (`proposals_end_date` = `proposals_start_date` AND `proposals_end_time` > `proposals_start_time`)
        ),
    CONSTRAINT `chk_ve_voting_dates`
        CHECK (
            (`voting_end_date` > `voting_start_date`)
            OR (`voting_end_date` = `voting_start_date` AND `voting_end_time` > `voting_start_time`)
        ),
    CONSTRAINT `chk_ve_stage_sequence`
        CHECK (
            (`proposals_start_date` > `registration_end_date`)
            OR (`proposals_start_date` = `registration_end_date` AND `proposals_start_time` >= `registration_end_time`)
        )
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Procesos electorales configurados por etapas: Inscripción, Propuestas, Votación (RN-12)';

-- ============================================================
-- 10. EVENT_GRADES — Grados habilitados por elección
-- ============================================================
CREATE TABLE `event_grades` (
    `id`              INT UNSIGNED        NOT NULL AUTO_INCREMENT,
    `voting_event_id` INT UNSIGNED        NOT NULL,
    `grade_id`        TINYINT UNSIGNED    NOT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_eg_event_grade` (`voting_event_id`, `grade_id`),
    KEY `idx_eg_grade_id` (`grade_id`),
    CONSTRAINT `fk_eg_voting_event`
        FOREIGN KEY (`voting_event_id`) REFERENCES `voting_events` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_eg_grade`
        FOREIGN KEY (`grade_id`) REFERENCES `grades` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Grados escolares habilitados para participar en cada elección';

-- ============================================================
-- 11. CANDIDATES — Autopostulación de candidatos (RF-M04-01, RF-M04-02, RN-10)
-- ============================================================
CREATE TABLE `candidates` (
    `id`                        INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `voting_event_id`           INT UNSIGNED    NOT NULL,
    `user_id`                   INT UNSIGNED    NULL     DEFAULT NULL COMMENT 'NULL si es voto en blanco; autopostulación cuando no es NULL',
    `name`                      VARCHAR(150)    NOT NULL COMMENT 'Nombre visible en el tarjetón',
    `slogan`                    TEXT            NULL     DEFAULT NULL COMMENT 'Lema de campaña',
    `photo_url`                 VARCHAR(500)    NULL     DEFAULT NULL COMMENT 'URL foto o avatar',
    `government_plan_url`       VARCHAR(500)    NULL     DEFAULT NULL COMMENT 'Documento del plan de gobierno cargado por el candidato',
    `is_blank_vote`             TINYINT(1)      NOT NULL DEFAULT 0 COMMENT '1 = Voto en Blanco',
    `status`                    ENUM('PENDIENTE','APROBADO','RECHAZADO') NOT NULL DEFAULT 'PENDIENTE',
    `approved_with_exceptions`  TINYINT(1)      NOT NULL DEFAULT 0 COMMENT '1 = aprobado pese a requisitos documentales faltantes (RN-10.1)',
    `exceptions_detail`         TEXT            NULL     DEFAULT NULL COMMENT 'Detalle de qué requisitos quedaron pendientes al aprobar con excepción',
    `rejection_reason`          TEXT            NULL     DEFAULT NULL COMMENT 'Motivo obligatorio si status = RECHAZADO (RN-10)',
    `reviewed_by_user_id`       INT UNSIGNED    NULL     DEFAULT NULL COMMENT 'Cuenta administrativa que aprobó/rechazó',
    `reviewed_at`               DATETIME        NULL     DEFAULT NULL,
    `enrolled_at`               DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_cand_user_event` (`user_id`, `voting_event_id`),
    KEY `idx_cand_voting_event_id` (`voting_event_id`),
    KEY `idx_cand_status`          (`status`),
    KEY `idx_cand_is_blank`        (`is_blank_vote`),
    KEY `idx_cand_reviewed_by`     (`reviewed_by_user_id`),
    CONSTRAINT `fk_cand_voting_event`
        FOREIGN KEY (`voting_event_id`) REFERENCES `voting_events` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_cand_user`
        FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `fk_cand_reviewed_by`
        FOREIGN KEY (`reviewed_by_user_id`) REFERENCES `users` (`id`)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Candidatos autopostulados por elección, sujetos a aprobación administrativa; incluye el voto en blanco';

-- ============================================================
-- 12. CANDIDATE_PROPOSALS — Lista breve de propuestas del candidato (RF-M04-01)
-- ============================================================
CREATE TABLE `candidate_proposals` (
    `id`              INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `candidate_id`    INT UNSIGNED    NOT NULL,
    `content`         TEXT            NOT NULL COMMENT 'Un punto de la propuesta',
    `display_order`   TINYINT UNSIGNED NOT NULL DEFAULT 1 COMMENT 'Orden de aparición en la ventana emergente',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_cp_candidate_order` (`candidate_id`, `display_order`),
    CONSTRAINT `fk_cp_candidate`
        FOREIGN KEY (`candidate_id`) REFERENCES `candidates` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Propuestas breves de cada candidato, mostradas antes de confirmar el voto';

-- ============================================================
-- 13. CANDIDACY_DOCUMENTS — Soportes documentales de la candidatura (RN-11)
-- ============================================================
CREATE TABLE `candidacy_documents` (
    `id`              INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `candidate_id`    INT UNSIGNED    NOT NULL,
    `requirement_id`  INT UNSIGNED    NOT NULL COMMENT 'FK al requisito del cargo que este documento intenta cumplir',
    `file_url`        VARCHAR(500)    NOT NULL,
    `uploaded_at`      DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_cd_candidate_requirement` (`candidate_id`, `requirement_id`),
    KEY `idx_cd_requirement_id` (`requirement_id`),
    CONSTRAINT `fk_cd_candidate`
        FOREIGN KEY (`candidate_id`) REFERENCES `candidates` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_cd_requirement`
        FOREIGN KEY (`requirement_id`) REFERENCES `position_requirements` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Documentos de soporte cargados por el candidato para cumplir los requisitos de su cargo (RF-M04-01)';

-- ============================================================
-- 14. VOTES — Registro inmutable de votos emitidos
--     *** SIN user_id — secreto del voto garantizado (RN-3) ***
-- ============================================================
CREATE TABLE `votes` (
    `id`              BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `voting_event_id` INT UNSIGNED    NOT NULL,
    `candidate_id`    INT UNSIGNED    NOT NULL,
    `voted_at`        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `vote_hash`       VARCHAR(64)     NOT NULL COMMENT 'SHA-256 para integridad criptográfica',
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_votes_hash`      (`vote_hash`),
    KEY `idx_votes_event_id`        (`voting_event_id`),
    KEY `idx_votes_candidate_id`    (`candidate_id`),
    KEY `idx_votes_voted_at`        (`voted_at`),
    CONSTRAINT `fk_votes_voting_event`
        FOREIGN KEY (`voting_event_id`) REFERENCES `voting_events` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `fk_votes_candidate`
        FOREIGN KEY (`candidate_id`) REFERENCES `candidates` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Votos emitidos — inmutables y sin referencia directa al usuario';

-- ============================================================
-- 15. EVENT_PARTICIPATIONS — Control anti-duplicado (antes
--     `voter_event_participations`; RN-3)
-- ============================================================
CREATE TABLE `event_participations` (
    `id`              INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `user_id`         INT UNSIGNED    NOT NULL,
    `voting_event_id` INT UNSIGNED    NOT NULL,
    `participated_at` DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_ep_user_event` (`user_id`, `voting_event_id`),
    KEY `idx_ep_voting_event_id` (`voting_event_id`),
    CONSTRAINT `fk_ep_user`
        FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_ep_voting_event`
        FOREIGN KEY (`voting_event_id`) REFERENCES `voting_events` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Control anti-duplicado: usuario ya ejerció su voto en la elección';

-- ============================================================
-- 16. AUDIT_LOG — Trazabilidad de operaciones sensibles (RN-8)
-- ============================================================
CREATE TABLE `audit_log` (
    `id`            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `user_id`       INT UNSIGNED    NULL     DEFAULT NULL COMMENT 'NULL si fue el sistema (ej. promoción automática)',
    `action`        VARCHAR(100)    NOT NULL COMMENT 'LOGIN, SELF_REGISTER, WHITELIST_UPLOADED, USER_UPDATED, USER_DELETED, USER_RESTORED, PROMOTION_RUN, PASSWORD_REASSIGNED, PROFILE_UPDATED, CANDIDACY_APPROVED, CANDIDACY_REJECTED, ADMIN_ACCOUNT_CREATED, ADMIN_ACCOUNT_UPDATED...',
    `target_entity` VARCHAR(200)    NULL     DEFAULT NULL COMMENT 'Tabla/entidad afectada',
    `target_id`     INT             NULL     DEFAULT NULL COMMENT 'ID del registro afectado',
    `field_name`    VARCHAR(100)    NULL     DEFAULT NULL COMMENT 'Campo modificado; NULL si no aplica',
    `old_value`     TEXT            NULL     DEFAULT NULL COMMENT 'Valor anterior del campo',
    `new_value`     TEXT            NULL     DEFAULT NULL COMMENT 'Valor nuevo del campo',
    `details`       TEXT            NULL     DEFAULT NULL COMMENT 'Contexto adicional en JSON (ej. resumen de promoción masiva)',
    `ip_address`    VARCHAR(45)     NULL     DEFAULT NULL COMMENT 'IP IPv4/IPv6 del cliente',
    `occurred_at`   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_al_user_id`     (`user_id`),
    KEY `idx_al_action`      (`action`),
    KEY `idx_al_occurred_at` (`occurred_at`),
    CONSTRAINT `fk_al_user`
        FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Auditoría de operaciones sensibles del sistema (RN-8)';

-- ============================================================
-- 17. PQR_TICKETS — Peticiones, Quejas o Reclamos (RF-M08-01, RF-M08-02)
--     Nota de diseño (ERS): a diferencia de las tablas cubiertas por RN-8,
--     esta tabla NO se refleja en audit_log; su propia trazabilidad
--     (status, responded_by_user_id, responded_at) es suficiente.
-- ============================================================
CREATE TABLE `pqr_tickets` (
    `id`                    BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `user_id`               INT UNSIGNED    NOT NULL COMMENT 'Usuario que radica la PQR',
    `subject`               VARCHAR(200)    NOT NULL,
    `message`               TEXT            NOT NULL COMMENT 'Descripción libre de la solicitud',
    `status`                ENUM('ABIERTO','RESUELTO') NOT NULL DEFAULT 'ABIERTO',
    `admin_response`        TEXT            NULL     DEFAULT NULL COMMENT 'Respuesta única del Administrador; NULL mientras está ABIERTO',
    `responded_by_user_id`  INT UNSIGNED    NULL     DEFAULT NULL COMMENT 'Cuenta administrativa que resolvió el ticket',
    `responded_at`          DATETIME        NULL     DEFAULT NULL,
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`             DATETIME        NULL     DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_pqr_user_id`     (`user_id`),
    KEY `idx_pqr_status`      (`status`),
    KEY `idx_pqr_created_at`  (`created_at`),
    CONSTRAINT `fk_pqr_user`
        FOREIGN KEY (`user_id`) REFERENCES `users` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_pqr_responded_by`
        FOREIGN KEY (`responded_by_user_id`) REFERENCES `users` (`id`)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Peticiones, Quejas o Reclamos radicadas por usuarios y resueltas por cuentas administrativas (M08)';

-- ============================================================
-- VISTAS
-- ============================================================

-- Conteo de votos en tiempo real (Dashboard de resultados)
-- Filtra explícitamente status = 'ELIMINADO' para que procesos eliminados
-- no aparezcan en los dashboards de escrutinio (RN-7.1). Solo candidatos
-- APROBADO cuentan, coherente con RN-10 (nunca hay votos sobre PENDIENTE).
CREATE OR REPLACE VIEW `vw_vote_counts` AS
SELECT
    ve.id              AS event_id,
    ve.title           AS event_title,
    ve.status          AS event_status,
    c.id               AS candidate_id,
    c.name             AS candidate_name,
    c.is_blank_vote,
    COUNT(v.id)        AS total_votes,
    ROUND(
        COUNT(v.id) * 100.0 /
        NULLIF(SUM(COUNT(v.id)) OVER (PARTITION BY ve.id), 0),
    2)                 AS vote_percentage
FROM `voting_events` ve
JOIN `candidates`    c  ON c.voting_event_id = ve.id AND c.status = 'APROBADO'
LEFT JOIN `votes`    v  ON v.candidate_id    = c.id
WHERE ve.status != 'ELIMINADO'   -- excluye procesos con soft-delete (RN-7.1)
GROUP BY ve.id, ve.title, ve.status, c.id, c.name, c.is_blank_vote
ORDER BY ve.id, total_votes DESC;

-- Censo activo con grado legible (uso frecuente en M02); solo electores
CREATE OR REPLACE VIEW `vw_active_census` AS
SELECT
    u.id,
    u.full_name,
    u.contact_email,
    g.name        AS grade,
    u.status,
    u.excluir_de_promocion,
    u.registered_at,
    u.updated_at
FROM `users` u
JOIN `roles` r ON r.id = u.role_id AND r.name = 'ELECTOR'
LEFT JOIN `grades` g ON g.id = u.grade_id
WHERE u.status IN ('ACTIVO','INACTIVO');

-- Entradas de la lista blanca aún no reclamadas (uso en M02 para seguimiento)
CREATE OR REPLACE VIEW `vw_pending_whitelist` AS
SELECT
    w.id, w.full_name, g.name AS grade, w.created_at
FROM `census_whitelist` w
LEFT JOIN `grades` g ON g.id = w.grade_id
WHERE w.claimed_at IS NULL
ORDER BY w.created_at ASC;

-- Cola pendiente de correos, lista para que el worker de envío progresivo la consuma (RN-9)
CREATE OR REPLACE VIEW `vw_pending_email_queue` AS
SELECT
    eq.id, eq.user_id, u.contact_email, u.full_name,
    eq.email_type, eq.attempts, eq.created_at
FROM `email_queue` eq
JOIN `users` u ON u.id = eq.user_id
WHERE eq.status = 'PENDIENTE'
ORDER BY eq.created_at ASC;

-- Candidaturas pendientes de revisión administrativa (uso en M04)
CREATE OR REPLACE VIEW `vw_pending_candidacies` AS
SELECT
    c.id AS candidate_id,
    c.voting_event_id,
    ve.title AS event_title,
    ve.position_id,
    ep.name AS position_name,
    u.full_name AS candidate_name,
    c.enrolled_at,
    (SELECT COUNT(*) FROM position_requirements pr WHERE pr.position_id = ve.position_id AND pr.is_mandatory = 1) AS mandatory_requirements,
    (SELECT COUNT(*) FROM candidacy_documents cd
        JOIN position_requirements pr2 ON pr2.id = cd.requirement_id
        WHERE cd.candidate_id = c.id AND pr2.is_mandatory = 1) AS mandatory_documents_uploaded
FROM `candidates` c
JOIN `voting_events` ve ON ve.id = c.voting_event_id
JOIN `election_positions` ep ON ep.id = ve.position_id
JOIN `users` u ON u.id = c.user_id
WHERE c.status = 'PENDIENTE'
ORDER BY c.enrolled_at ASC;

-- ============================================================
-- DATOS SEMILLA — Usuarios iniciales para poder entrar de una vez
-- ============================================================

-- 1. SÚPER ADMINISTRADOR
--    Documento:   1020304050
--    Contraseña:  Admin#2026!
--    NOTA (fix v2.8): en v2.7 el documento del admin cambió a este valor
--    numérico pero el `document_hash` sembrado seguía correspondiendo al
--    documento anterior ('admin.electoral'), por lo que el login no
--    coincidía. Aquí el hash SHA-256 corresponde exactamente a '1020304050'.
INSERT INTO `users`
    (`role_id`, `grade_id`, `document_hash`, `encrypted_document`,
     `full_name`, `contact_email`, `password_hash`, `position_title`,
     `excluir_de_promocion`, `status`)
VALUES
    (3, NULL,
     '9e1f341dff9161b69d6afc54140ea30e44676ed3600dc98f507f0678fe64e320',
     'PENDIENTE_CIFRAR:1020304050',
     'Coordinación Electoral',
     'coordinacion.electoral@colegio.edu.co',
     '$2b$11$URzaSqbUkafEZVZY3U8Sq..58nCUsOSxC8BZVcl8oZdCRe9BB6TM.',
     'Súper Administrador',
     0, 'ACTIVO');

-- 2. ENTRADA DE LISTA BLANCA (aún no reclamada) — para probar RF-M01-00
--    Documento:   1015998877
INSERT INTO `census_whitelist`
    (`document_hash`, `encrypted_document`, `full_name`, `grade_id`,
     `excluir_de_promocion`, `uploaded_by_user_id`)
VALUES
    ('16ad24eeedca3411b3ab6e848d6789bdb0f6c864edb0b2b60e361a7277f69fbd',
     'PENDIENTE_CIFRAR:1015998877',
     'Sofía Ramírez Torres', 6,
     0, 1);

-- 3. ELECTOR DE PRUEBA YA AUTO-REGISTRADO (simula RF-M01-00 completado)
--    Documento:   1001234567
--    Contraseña:  1001234567.2026  (contraseña real verificada en entorno de
--                 prueba; en producción el propio elector la define al
--                 auto-registrarse, RN-2)
INSERT INTO `users`
    (`role_id`, `grade_id`, `document_hash`, `encrypted_document`,
     `full_name`, `contact_email`, `password_hash`,
     `excluir_de_promocion`, `status`)
VALUES
    (1, 6,
     'c7dc7b2f9562560342d1b7ba78febecc04199365383ea3452c44b9266a09f758',
     'PENDIENTE_CIFRAR:1001234567',
     'Ana María López Pérez',
     'acudiente.ana.lopez@example.com',
     '$2b$11$1eesMlQ9QK8kJwIOKzee0OFC8dJUE6a/lnXsJglCqwNmIwpOjiqM2',
     0, 'ACTIVO');

-- 4. CARGO ELECTORAL DE EJEMPLO Y SUS REQUISITOS (RF-M03-00)
INSERT INTO `election_positions` (`name`, `description`) VALUES
    ('Personero',              'Representante estudiantil ante la comunidad educativa'),
    ('Contralor',              'Vigilancia de la gestión de los recursos escolares'),
    ('Representante de Curso', 'Vocero de cada grupo ante el gobierno escolar');

INSERT INTO `position_requirements` (`position_id`, `description`, `is_mandatory`, `display_order`) VALUES
    (1, 'Certificado de haber cursado y aprobado 10°', 1, 1),
    (1, 'Carta de compromiso firmada por el acudiente', 1, 2),
    (2, 'Certificado de haber cursado y aprobado 9°', 1, 1),
    (3, 'Aval firmado por el director de grupo', 1, 1);

-- ============================================================
SET FOREIGN_KEY_CHECKS = 1;
-- FIN DEL SCRIPT — wahl_mirai_db v2.8 (schema + semillas, todo en uno)
-- ============================================================
