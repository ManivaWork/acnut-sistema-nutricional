BEGIN;

ALTER TABLE pacientes
    ADD COLUMN IF NOT EXISTS anonimizado_em TIMESTAMPTZ;

COMMENT ON COLUMN pacientes.anonimizado_em IS 'Timestamp da anonimização do paciente (LGPD). Quando preenchido, o paciente foi anonimizado.';

COMMIT;