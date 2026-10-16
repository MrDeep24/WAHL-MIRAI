-- ============================================================
-- WAHL MIRAI — Migración / Parche DDL para auto-registro OTP (v3.0)
-- Agrega columna `contact_email` en `census_whitelist`
-- y actualiza la vista `vw_pending_whitelist`.
--
-- Uso: Ejecutar sobre una base de datos existente sin destruir datos.
-- ============================================================

USE `wahl_mirai_db`;

-- 1. Agregar columna contact_email a census_whitelist
ALTER TABLE `census_whitelist`
    ADD COLUMN `contact_email` VARCHAR(150) NULL DEFAULT NULL
    COMMENT 'Correo de contacto preautorizado para verificación OTP en auto-registro (v3.0)'
    AFTER `full_name`;

-- 2. Actualizar la vista vw_pending_whitelist para proyectar contact_email
CREATE OR REPLACE VIEW `vw_pending_whitelist` AS
SELECT
    w.id,
    w.document_hash,
    w.encrypted_document,
    w.full_name,
    w.contact_email,
    w.grade_id,
    g.name AS grade,
    w.excluir_de_promocion,
    w.created_at
FROM `census_whitelist` w
LEFT JOIN `grades` g ON g.id = w.grade_id
WHERE w.claimed_at IS NULL
ORDER BY w.created_at ASC;

-- 3. (Opcional para pruebas locales) Asignar correo preautorizado a Sofía Ramírez Torres
UPDATE `census_whitelist`
SET `contact_email` = 'sofia.ramirez@colegio.edu.co'
WHERE `document_hash` = '16ad24eeedca3411b3ab6e848d6789bdb0f6c864edb0b2b60e361a7277f69fbd'
  AND `contact_email` IS NULL;
