-- Migration: Tabelas para convites/vínculo paciente ↔ nutricionista
-- Idempotente, roda em transação, sem DROP/DELETE, termina em ROLLBACK
-- Aplicar: psql -d smart_nutri_db -f migration_convites_paciente.sql

BEGIN;

-- ============================================================
-- 1. Tabela convites_paciente
-- ============================================================
CREATE TABLE IF NOT EXISTS convites_paciente (
    id                  BIGSERIAL PRIMARY KEY,
    empresa_id          BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    nutricionista_id    BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    paciente_id         BIGINT REFERENCES pacientes(id) ON DELETE SET NULL,
    email_paciente      VARCHAR(255) NOT NULL,
    token               VARCHAR(64) NOT NULL UNIQUE,
    status              VARCHAR(20) NOT NULL DEFAULT 'pendente',  -- pendente, aceito, expirado, cancelado
    expira_em           TIMESTAMPTZ NOT NULL,
    aceito_em           TIMESTAMPTZ,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Índices de performance
CREATE INDEX IF NOT EXISTS idx_convites_pac_empresa_nutri
    ON convites_paciente (empresa_id, nutricionista_id);

CREATE INDEX IF NOT EXISTS idx_convites_pac_token_valido
    ON convites_paciente (token) WHERE status = 'pendente';

CREATE INDEX IF NOT EXISTS idx_convites_pac_email_ativo
    ON convites_paciente (empresa_id, email_paciente) WHERE status = 'pendente';

-- Constraint: um convite ativo por email por nutricionista
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_convite_pac_nutri_email_ativo'
    ) THEN
        ALTER TABLE convites_paciente
        ADD CONSTRAINT uq_convite_pac_nutri_email_ativo
        UNIQUE (nutricionista_id, email_paciente)
        DEFERRABLE INITIALLY IMMEDIATE;
    END IF;
END $$;

-- Comentários
COMMENT ON TABLE convites_paciente IS 'Convites de nutricionista para pacientes (expiram em 7 dias)';
COMMENT ON COLUMN convites_paciente.token IS 'Token único (UUID v4 sem hífens) enviado por e-mail';
COMMENT ON COLUMN convites_paciente.expira_em IS 'Expiração do convite (padrão 7 dias)';
COMMENT ON COLUMN convites_paciente.aceito_em IS 'Preenchido quando o convite é aceito; NULL = pendente';
COMMENT ON COLUMN convites_paciente.status IS 'pendente, aceito, expirado, cancelado';

-- ============================================================
-- 2. Tabela acessos_paciente_nutri (vínculo nutricionista ↔ paciente)
-- ============================================================
CREATE TABLE IF NOT EXISTS acessos_paciente_nutri (
    id                  BIGSERIAL PRIMARY KEY,
    empresa_id          BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    nutricionista_id    BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    paciente_id         BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (nutricionista_id, paciente_id)
);

CREATE INDEX IF NOT EXISTS idx_acesso_pac_nutri_nutri
    ON acessos_paciente_nutri (nutricionista_id);

CREATE INDEX IF NOT EXISTS idx_acesso_pac_nutri_pac
    ON acessos_paciente_nutri (paciente_id);

COMMENT ON TABLE acessos_paciente_nutri IS 'Vínculo nutricionista ↔ paciente (nutricionista vê apenas seus pacientes)';
COMMENT ON COLUMN acessos_paciente_nutri.nutricionista_id IS 'Profissional que tem acesso ao paciente';
COMMENT ON COLUMN acessos_paciente_nutri.paciente_id IS 'Paciente vinculado ao nutricionista';

-- ============================================================
-- 3. Verificação rápida
-- ============================================================
SELECT 'convites_paciente' AS tabela, COUNT(*) AS linhas FROM convites_paciente;
SELECT 'acessos_paciente_nutri' AS tabela, COUNT(*) AS linhas FROM acessos_paciente_nutri;

ROLLBACK;  -- remova esta linha e execute novamente para aplicar de verdade