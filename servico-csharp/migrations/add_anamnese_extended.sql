-- Migration: Add extended anamnese fields (JSONB column for 8-section form)
-- Run this AFTER the existing schema is applied
-- Idempotent: uses IF NOT EXISTS, no DROP/DELETE

BEGIN;

-- Add JSONB column for extended anamnese data (all 8 sections)
ALTER TABLE anamneses
ADD COLUMN IF NOT EXISTS dados_extendidos JSONB DEFAULT '{}'::jsonb;

-- Add index for JSONB queries if needed
CREATE INDEX IF NOT EXISTS idx_anamneses_dados_extendidos
ON anamneses USING GIN (dados_extendidos);

-- Update the atualizado_em timestamp on any change to dados_extendidos
-- (handled in application layer)

-- Verify the column exists
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'anamneses'
AND column_name = 'dados_extendidos';

COMMIT;