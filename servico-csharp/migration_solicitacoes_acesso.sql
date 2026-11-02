-- Migration: criar tabela solicitacoes_acesso
-- Idempotente, roda em transação, sem DROP/DELETE, termina em ROLLBACK
-- Aplicar manualmente quando quiser: psql -d smart_nutri_db -f migration_solicitacoes_acesso.sql

BEGIN;

-- 1. Tabela solicitacoes_acesso
CREATE TABLE IF NOT EXISTS solicitacoes_acesso (
    id              BIGSERIAL PRIMARY KEY,
    nome            VARCHAR(255) NOT NULL,
    email           VARCHAR(255) NOT NULL,
    telefone        VARCHAR(50),
    registro_profissional VARCHAR(100),
    clinica_nome    VARCHAR(255),
    mensagem        TEXT,
    status          VARCHAR(20) NOT NULL DEFAULT 'pendente'
        CHECK (status IN ('pendente', 'aprovado', 'recusado', 'excluido')),
    empresa_id      BIGINT REFERENCES empresas(id) ON DELETE SET NULL,
    usuario_id      BIGINT REFERENCES usuarios(id) ON DELETE SET NULL,
    ip              INET,
    user_agent      TEXT,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 2. Índices de performance
CREATE INDEX IF NOT EXISTS idx_solicitacoes_status_criado
    ON solicitacoes_acesso (status, criado_em DESC);

CREATE INDEX IF NOT EXISTS idx_solicitacoes_email_pendente
    ON solicitacoes_acesso (email)
    WHERE status = 'pendente';

CREATE INDEX IF NOT EXISTS idx_solicitacoes_empresa
    ON solicitacoes_acesso (empresa_id)
    WHERE empresa_id IS NOT NULL;

-- 3. Constraint de unicidade: uma solicitação pendente por email
-- (parcial: só enquanto pendente)
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_solicitacoes_email_pendente'
    ) THEN
        ALTER TABLE solicitacoes_acesso
        ADD CONSTRAINT uq_solicitacoes_email_pendente
        UNIQUE (email)
        DEFERRABLE INITIALLY IMMEDIATE;
    END IF;
END $$;

-- 4. Trigger para atualizar atualizado_em
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_trigger
        WHERE tgname = 'trg_solicitacoes_atualizado_em'
    ) THEN
        CREATE OR REPLACE FUNCTION atualizar_updated_em()
        RETURNS TRIGGER AS $$
        BEGIN
            NEW.atualizado_em = CURRENT_TIMESTAMP;
            RETURN NEW;
        END;
        $$ LANGUAGE plpgsql;

        CREATE TRIGGER trg_solicitacoes_atualizado_em
        BEFORE UPDATE ON solicitacoes_acesso
        FOR EACH ROW
        EXECUTE FUNCTION atualizar_updated_em();
    END IF;
END $$;

-- 5. Comentários para documentação
COMMENT ON TABLE solicitacoes_acesso IS 'Solicitações de acesso de visitantes (fluxo público "Solicitar acesso")';
COMMENT ON COLUMN solicitacoes_acesso.status IS 'pendente/aprovado/recusado/excluido';
COMMENT ON COLUMN solicitacoes_acesso.ip IS 'IP do solicitante (para rate limiting)';
COMMENT ON COLUMN solicitacoes_acesso.empresa_id IS 'Preenchido ao aprovar (nova empresa criada)';
COMMENT ON COLUMN solicitacoes_acesso.usuario_id IS 'Preenchido ao aprovar (usuário profissional criado)';

-- 6. Verificação rápida
SELECT 'solicitacoes_acesso' AS tabela, COUNT(*) AS linhas FROM solicitacoes_acesso;

ROLLBACK;  -- remova esta linha e execute novamente para aplicar de verdade


-- =====================================================================
-- VERSÃO COM COMMIT (para aplicar em produção)
-- =====================================================================
-- BEGIN;
--
-- -- 1. Tabela solicitacoes_acesso
-- CREATE TABLE IF NOT EXISTS solicitacoes_acesso (
--     id              BIGSERIAL PRIMARY KEY,
--     nome            VARCHAR(255) NOT NULL,
--     email           VARCHAR(255) NOT NULL,
--     telefone        VARCHAR(50),
--     registro_profissional VARCHAR(100),
--     clinica_nome    VARCHAR(255),
--     mensagem        TEXT,
--     status          VARCHAR(20) NOT NULL DEFAULT 'pendente'
--         CHECK (status IN ('pendente', 'aprovado', 'recusado', 'excluido')),
--     empresa_id      BIGINT REFERENCES empresas(id) ON DELETE SET NULL,
--     usuario_id      BIGINT REFERENCES usuarios(id) ON DELETE SET NULL,
--     ip              INET,
--     user_agent      TEXT,
--     criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
--     atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );
--
-- -- 2. Índices de performance
-- CREATE INDEX IF NOT EXISTS idx_solicitacoes_status_criado
--     ON solicitacoes_acesso (status, criado_em DESC);
--
-- CREATE INDEX IF NOT EXISTS idx_solicitacoes_email_pendente
--     ON solicitacoes_acesso (email)
--     WHERE status = 'pendente';
--
-- CREATE INDEX IF NOT EXISTS idx_solicitacoes_empresa
--     ON solicitacoes_acesso (empresa_id)
--     WHERE empresa_id IS NOT NULL;
--
-- -- 3. Constraint de unicidade: uma solicitação pendente por email
-- DO $$
-- BEGIN
--     IF NOT EXISTS (
--         SELECT 1 FROM pg_constraint
--         WHERE conname = 'uq_solicitacoes_email_pendente'
--     ) THEN
--         ALTER TABLE solicitacoes_acesso
--         ADD CONSTRAINT uq_solicitacoes_email_pendente
--         UNIQUE (email)
--         DEFERRABLE INITIALLY IMMEDIATE;
--     END IF;
-- END $$;
--
-- -- 4. Trigger para atualizar atualizado_em
-- DO $$
-- BEGIN
--     IF NOT EXISTS (
--         SELECT 1 FROM pg_trigger
--         WHERE tgname = 'trg_solicitacoes_atualizado_em'
--     ) THEN
--         CREATE OR REPLACE FUNCTION atualizar_updated_em()
--         RETURNS TRIGGER AS $$
--         BEGIN
--             NEW.atualizado_em = CURRENT_TIMESTAMP;
--             RETURN NEW;
--         END;
--         $$ LANGUAGE plpgsql;
--
--         CREATE TRIGGER trg_solicitacoes_atualizado_em
--         BEFORE UPDATE ON solicitacoes_acesso
--         FOR EACH ROW
--         EXECUTE FUNCTION atualizar_updated_em();
--     END IF;
-- END $$;
--
-- -- 5. Comentários para documentação
-- COMMENT ON TABLE solicitacoes_acesso IS 'Solicitações de acesso de visitantes (fluxo público "Solicitar acesso")';
-- COMMENT ON COLUMN solicitacoes_acesso.status IS 'pendente/aprovado/recusado/excluido';
-- COMMENT ON COLUMN solicitacoes_acesso.ip IS 'IP do solicitante (para rate limiting)';
-- COMMENT ON COLUMN solicitacoes_acesso.empresa_id IS 'Preenchido ao aprovar (nova empresa criada)';
-- COMMENT ON COLUMN solicitacoes_acesso.usuario_id IS 'Preenchido ao aprovar (usuário profissional criado)';
--
-- -- 6. Verificação rápida
-- SELECT 'solicitacoes_acesso' AS tabela, COUNT(*) AS linhas FROM solicitacoes_acesso;
--
-- COMMIT;