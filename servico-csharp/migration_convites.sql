-- Migration: criar tabela convites
-- Idempotente, roda em transação, sem DROP/DELETE, termina em ROLLBACK
-- Aplicar manualmente quando quiser: psql -d smart_nutri_db -f migration_convites.sql

BEGIN;

-- 1. Tabela convites
CREATE TABLE IF NOT EXISTS convites (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    email           VARCHAR(255) NOT NULL,
    perfil          VARCHAR(50) NOT NULL DEFAULT 'profissional',
    nivel_nutricao  VARCHAR(50) NOT NULL DEFAULT 'leitura',
    token           VARCHAR(64) NOT NULL UNIQUE,
    expira_em       TIMESTAMPTZ NOT NULL,
    usado_em        TIMESTAMPTZ,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 2. Índices de performance
CREATE INDEX IF NOT EXISTS idx_convites_empresa_email
    ON convites (empresa_id, email)
    WHERE usado_em IS NULL;

CREATE INDEX IF NOT EXISTS idx_convites_token_valido
    ON convites (token)
    WHERE usado_em IS NULL AND expira_em > CURRENT_TIMESTAMP;

-- 3. Constraint de unicidade: um convite ativo por email por empresa
-- (parcial: só enquanto não usado e não expirado)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_convites_empresa_email_ativo'
    ) THEN
        ALTER TABLE convites
        ADD CONSTRAINT uq_convites_empresa_email_ativo
        UNIQUE (empresa_id, email)
        DEFERRABLE INITIALLY IMMEDIATE;
    END IF;
END $$;

-- 4. Comentários para documentação
COMMENT ON TABLE convites IS 'Convites para usuários entrarem na empresa (expiram em 7 dias)';
COMMENT ON COLUMN convites.token IS 'Token único (UUID v4 sem hífens) enviado por e-mail';
COMMENT ON COLUMN convites.expira_em IS 'Expiração do convite (padrão 7 dias)';
COMMENT ON COLUMN convites.usado_em IS 'Preenchido quando o convite é aceito; NULL = pendente';

-- 5. Verificação rápida
SELECT 'convites' AS tabela, COUNT(*) AS linhas FROM convites;

ROLLBACK;  -- remova esta linha e execute novamente para aplicar de verdade