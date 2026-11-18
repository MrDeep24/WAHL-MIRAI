-- SPDX-License-Identifier: AGPL-3.0-or-later
-- Copyright (c) 2026 Camilo Camelo, Sneider Luligo, Kevin Santiago Rodríguez Oliveros
-- ============================================================
-- WAHL MIRAI — Diagnóstico y Ajuste de Contigüidad de Etapas Electorales
-- Requerimientos: RN-12, RF-M03-01 (ERS v3.2)
-- Base de datos: wahl_mirai_db
-- ============================================================
-- Este script es seguro: no altera el esquema relacional DDL v3.0.
-- Proporciona:
-- 1. Consulta de diagnóstico para detectar brechas (huecos o traslapes)
--    entre Inscripción -> Propuestas y Propuestas -> Votación.
-- 2. Sentencia controlada/comentada para cerrar huecos en datos de prueba.
-- ============================================================

USE `wahl_mirai_db`;

-- ------------------------------------------------------------
-- 1. CONSULTA DE DIAGNÓSTICO
-- ------------------------------------------------------------
SELECT 
    id,
    title,
    status,
    election_type,
    registration_start_date,
    registration_start_time,
    registration_end_date,
    registration_end_time,
    proposals_start_date,
    proposals_start_time,
    proposals_end_date,
    proposals_end_time,
    voting_start_date,
    voting_start_time,
    voting_end_date,
    voting_end_time,
    CASE 
        WHEN (registration_end_date != proposals_start_date OR registration_end_time != proposals_start_time)
             AND (proposals_end_date != voting_start_date OR proposals_end_time != voting_start_time)
            THEN 'Discontinuidad en ambas transiciones (Reg->Prop y Prop->Vot)'
        WHEN (registration_end_date != proposals_start_date OR registration_end_time != proposals_start_time)
            THEN 'Discontinuidad entre Inscripción y Propuestas'
        WHEN (proposals_end_date != voting_start_date OR proposals_end_time != voting_start_time)
            THEN 'Discontinuidad entre Propuestas y Votación'
        ELSE 'Contiguo'
    END AS diagnostico_contiguidad
FROM voting_events
WHERE status != 'ELIMINADO'
  AND (
      (registration_end_date != proposals_start_date OR registration_end_time != proposals_start_time)
      OR
      (proposals_end_date != voting_start_date OR proposals_end_time != voting_start_time)
  )
ORDER BY id ASC;

-- ------------------------------------------------------------
-- 2. SENTENCIA DE AJUSTE OPCIONAL / CONTROLADA
-- Descomentar y ejecutar solo si se desea sincronizar datos previos.
-- ------------------------------------------------------------
/*
START TRANSACTION;

UPDATE voting_events
SET 
    proposals_start_date = registration_end_date,
    proposals_start_time = registration_end_time,
    voting_start_date = proposals_end_date,
    voting_start_time = proposals_end_time
WHERE status != 'ELIMINADO'
  AND (
      (registration_end_date != proposals_start_date OR registration_end_time != proposals_start_time)
      OR
      (proposals_end_date != voting_start_date OR proposals_end_time != voting_start_time)
  );

-- Verificación posterior
SELECT id, title, status,
       registration_end_date, registration_end_time,
       proposals_start_date, proposals_start_time,
       proposals_end_date, proposals_end_time,
       voting_start_date, voting_start_time
FROM voting_events
WHERE status != 'ELIMINADO';

-- COMMIT;
-- ROLLBACK;
*/
