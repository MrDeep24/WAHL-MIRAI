-- ============================================================
--  WAHL MIRAI — Sistema de Votaciones Digitales Estudiantiles
--  Script DDL MySQL 8.0+
--  Base de datos: wahl_mirai_db
--  Versión: 2.2 — Alineado con ERS IEEE 830-1998 v2.2
--  Cambios frente a v1: censo persistente, promoción automática,
--  eliminación lógica, login por documento (sin correo),
--  tipo de elección, propuestas como lista y auditoría detallada.
-- ============================================================

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- ------------------------------------------------------------
-- Base de datos
-- ------------------------------------------------------------
CREATE DATABASE IF NOT EXISTS `wahl_mirai_db`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `wahl_mirai_db`;

-- ============================================================
-- 1. ROLES — Catálogo de roles del sistema
-- ============================================================
CREATE TABLE `roles` (
    `id`          TINYINT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `name`        VARCHAR(30)         NOT NULL COMMENT 'Ej: ADMIN, ELECTOR',
    `description` VARCHAR(100)        NULL     DEFAULT NULL,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_roles_name` (`name`)
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Catálogo de roles del sistema';

INSERT INTO `roles` (`name`, `description`) VALUES
    ('ADMIN',   'Administrador electoral con acceso total al sistema'),
    ('ELECTOR', 'Estudiante con derecho a votar y postularse como candidato');

-- ============================================================
-- 2. ACADEMIC_YEARS — Control del año lectivo vigente (RN-2, RF-M02-03)
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
  COMMENT='Año lectivo vigente; controla generación de clave inicial y bloqueo de doble promoción';

-- ============================================================
-- 3. GRADES — Catálogo secuencial de grados (RF-M02-03)
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
-- 4. VOTERS — Usuarios del sistema (censo persistente)
-- ============================================================
CREATE TABLE `voters` (
    `id`                     INT UNSIGNED        NOT NULL AUTO_INCREMENT,
    `role_id`                TINYINT UNSIGNED    NOT NULL,
    `grade_id`               TINYINT UNSIGNED    NULL     DEFAULT NULL COMMENT 'NULL para administradores',
    `document_hash`          CHAR(64)            NOT NULL COMMENT 'SHA-256 determinístico del documento, usado para login/búsqueda',
    `encrypted_document`     VARCHAR(500)        NOT NULL COMMENT 'Documento cifrado AES-256 para mostrar/editar en UI',
    `full_name`              VARCHAR(150)        NOT NULL,
    `password_hash`          VARCHAR(255)        NOT NULL COMMENT 'Hash BCrypt',
    `requiere_cambio_clave`  TINYINT(1)          NOT NULL DEFAULT 1 COMMENT '1 = debe cambiar clave autogenerada en próximo login (RF-M01-02)',
    `excluir_de_promocion`   TINYINT(1)          NOT NULL DEFAULT 0 COMMENT '1 = repitente, se omite en la promoción masiva',
    `status`                 ENUM('ACTIVO','INACTIVO','ELIMINADO','EGRESADO') NOT NULL DEFAULT 'ACTIVO',
    `deleted_at`              DATETIME            NULL     DEFAULT NULL COMMENT 'Fecha de eliminación lógica; NULL si no aplica',
    `registered_at`          DATETIME            NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`             DATETIME            NULL     DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_voters_document_hash` (`document_hash`),
    KEY `idx_voters_role_id` (`role_id`),
    KEY `idx_voters_grade_id` (`grade_id`),
    KEY `idx_voters_status`  (`status`),
    CONSTRAINT `fk_voters_role`
        FOREIGN KEY (`role_id`) REFERENCES `roles` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `fk_voters_grade`
        FOREIGN KEY (`grade_id`) REFERENCES `grades` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Censo persistente de usuarios: electores y administradores';

-- ============================================================
-- 5. VOTING_EVENTS — Procesos electorales
-- ============================================================
CREATE TABLE `voting_events` (
    `id`                    INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `created_by_voter_id`   INT UNSIGNED    NOT NULL COMMENT 'Administrador creador',
    `title`                 VARCHAR(200)    NOT NULL COMMENT 'Nombre de la elección',
    `description`           TEXT            NULL     DEFAULT NULL,
    `election_type`         ENUM('PERSONAS','TEMAS') NOT NULL DEFAULT 'PERSONAS' COMMENT 'RF-M03-01',
    `start_date`            DATE            NOT NULL,
    `start_time`            TIME            NOT NULL,
    `end_date`              DATE            NOT NULL,
    `end_time`              TIME            NOT NULL,
    `status`                ENUM('PROGRAMADA','ACTIVA','FINALIZADA') NOT NULL DEFAULT 'PROGRAMADA',
    `created_at`            DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updated_at`            DATETIME        NULL     DEFAULT NULL ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_ve_status`     (`status`),
    KEY `idx_ve_start`      (`start_date`, `start_time`),
    KEY `idx_ve_end`        (`end_date`, `end_time`),
    KEY `idx_ve_created_by` (`created_by_voter_id`),
    CONSTRAINT `fk_ve_created_by`
        FOREIGN KEY (`created_by_voter_id`) REFERENCES `voters` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE,
    CONSTRAINT `chk_ve_dates`
        CHECK (
            (`end_date` > `start_date`)
            OR (`end_date` = `start_date` AND `end_time` > `start_time`)
        )
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Procesos electorales configurados por el administrador';

-- ============================================================
-- 6. EVENT_GRADES — Grados habilitados por elección
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
-- 7. CANDIDATES — Candidatos postulados en una elección
-- ============================================================
CREATE TABLE `candidates` (
    `id`              INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `voting_event_id` INT UNSIGNED    NOT NULL,
    `voter_id`        INT UNSIGNED    NULL     DEFAULT NULL COMMENT 'NULL si es voto en blanco',
    `name`            VARCHAR(150)    NOT NULL COMMENT 'Nombre visible en el tarjetón',
    `slogan`          TEXT            NULL     DEFAULT NULL COMMENT 'Lema de campaña',
    `photo_url`       VARCHAR(500)    NULL     DEFAULT NULL COMMENT 'URL foto o avatar',
    `is_blank_vote`   TINYINT(1)      NOT NULL DEFAULT 0 COMMENT '1 = Voto en Blanco',
    `status`          ENUM('PENDIENTE','APROBADO','RECHAZADO') NOT NULL DEFAULT 'PENDIENTE',
    `enrolled_at`     DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_cand_voter_event` (`voter_id`, `voting_event_id`),
    KEY `idx_cand_voting_event_id` (`voting_event_id`),
    KEY `idx_cand_status`          (`status`),
    KEY `idx_cand_is_blank`        (`is_blank_vote`),
    CONSTRAINT `fk_cand_voting_event`
        FOREIGN KEY (`voting_event_id`) REFERENCES `voting_events` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_cand_voter`
        FOREIGN KEY (`voter_id`) REFERENCES `voters` (`id`)
        ON DELETE RESTRICT ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Candidatos postulados en cada elección, incluyendo el voto en blanco';

-- ============================================================
-- 8. CANDIDATE_PROPOSALS — Lista de propuestas del candidato (RF-M04-01)
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
  COMMENT='Propuestas de campaña de cada candidato, mostradas antes de confirmar el voto';

-- ============================================================
-- 9. VOTES — Registro inmutable de votos emitidos
--    *** SIN voter_id — secreto del voto garantizado ***
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
  COMMENT='Votos emitidos — inmutables y sin referencia directa al elector';

-- ============================================================
-- 10. VOTER_EVENT_PARTICIPATIONS — Control anti-duplicado
-- ============================================================
CREATE TABLE `voter_event_participations` (
    `id`              INT UNSIGNED    NOT NULL AUTO_INCREMENT,
    `voter_id`        INT UNSIGNED    NOT NULL,
    `voting_event_id` INT UNSIGNED    NOT NULL,
    `participated_at` DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    UNIQUE KEY `uq_vep_voter_event` (`voter_id`, `voting_event_id`),
    KEY `idx_vep_voting_event_id` (`voting_event_id`),
    CONSTRAINT `fk_vep_voter`
        FOREIGN KEY (`voter_id`) REFERENCES `voters` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE,
    CONSTRAINT `fk_vep_voting_event`
        FOREIGN KEY (`voting_event_id`) REFERENCES `voting_events` (`id`)
        ON DELETE CASCADE ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Control anti-duplicado: elector ya ejerció su voto en la elección';

-- ============================================================
-- 11. AUDIT_LOG — Trazabilidad de operaciones sensibles (RN-8)
-- ============================================================
CREATE TABLE `audit_log` (
    `id`            BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
    `voter_id`      INT UNSIGNED    NULL     DEFAULT NULL COMMENT 'NULL si fue el sistema (ej. promoción automática)',
    `action`        VARCHAR(100)    NOT NULL COMMENT 'LOGIN, VOTE_CAST, VOTER_CREATED, VOTER_UPDATED, VOTER_DELETED, VOTER_RESTORED, PROMOTION_RUN...',
    `target_entity` VARCHAR(200)    NULL     DEFAULT NULL COMMENT 'Tabla/entidad afectada',
    `target_id`     INT             NULL     DEFAULT NULL COMMENT 'ID del registro afectado',
    `field_name`    VARCHAR(100)    NULL     DEFAULT NULL COMMENT 'Campo modificado; NULL si no aplica',
    `old_value`     TEXT            NULL     DEFAULT NULL COMMENT 'Valor anterior del campo',
    `new_value`     TEXT            NULL     DEFAULT NULL COMMENT 'Valor nuevo del campo',
    `details`       TEXT            NULL     DEFAULT NULL COMMENT 'Contexto adicional en JSON (ej. resumen de promoción masiva)',
    `ip_address`    VARCHAR(45)     NULL     DEFAULT NULL COMMENT 'IP IPv4/IPv6 del cliente',
    `occurred_at`   DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_al_voter_id`    (`voter_id`),
    KEY `idx_al_action`      (`action`),
    KEY `idx_al_occurred_at` (`occurred_at`),
    CONSTRAINT `fk_al_voter`
        FOREIGN KEY (`voter_id`) REFERENCES `voters` (`id`)
        ON DELETE SET NULL ON UPDATE CASCADE
) ENGINE=InnoDB
  DEFAULT CHARSET=utf8mb4
  COLLATE=utf8mb4_unicode_ci
  COMMENT='Auditoría criptográfica de operaciones sensibles del sistema';

-- ============================================================
-- VISTA: Conteo de votos en tiempo real (para Dashboard)
-- ============================================================
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
GROUP BY ve.id, ve.title, ve.status, c.id, c.name, c.is_blank_vote
ORDER BY ve.id, total_votes DESC;

-- ============================================================
-- VISTA: Censo activo con grado legible (uso frecuente en M02)
-- ============================================================
CREATE OR REPLACE VIEW `vw_active_census` AS
SELECT
    v.id,
    v.full_name,
    g.name        AS grade,
    v.status,
    v.requiere_cambio_clave,
    v.excluir_de_promocion,
    v.registered_at,
    v.updated_at
FROM `voters` v
LEFT JOIN `grades` g ON g.id = v.grade_id
WHERE v.status IN ('ACTIVO','INACTIVO');

-- ============================================================
SET FOREIGN_KEY_CHECKS = 1;
-- FIN DEL SCRIPT DDL — wahl_mirai_db v2.2
-- ============================================================
