-- Migration: criar tabela tokens_redefinicao_senha
-- Idempotente, roda em transação, sem DROP/DELETE, termina em ROLLBACK
-- Aplicar manualmente quando quiser: psql -d smart_nutri_db -f migration_tokens_redefinicao_senha.sql

BEGIN;

-- 1. Tabela tokens_redefinicao_senha
CREATE TABLE IF NOT EXISTS tokens_redefinicao_senha (
    id              BIGSERIAL PRIMARY KEY,
    usuario_id      BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    token_hash      VARCHAR(255) NOT NULL,
    expira_em       TIMESTAMPTZ NOT NULL,
    usado_em        TIMESTAMPTZ,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 2. Índices de performance
CREATE INDEX IF NOT EXISTS idx_tokens_usuario_valido
    ON tokens_redefinicao_senha (usuario_id)
    WHERE usado_em IS NULL AND expira_em > CURRENT_TIMESTAMP;

-- 3. Constraint de unicidade: um token válido por usuário por vez
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_tokens_usuario_valido'
    ) THEN
        ALTER TABLE tokens_redefinicao_senha
        ADD CONSTRAINT uq_tokens_usuario_valido
        UNIQUE (usuario_id)
        DEFERRABLE INITIALLY IMMEDIATE;
    END IF;
END $$;

-- 4. Comentários para documentação
COMMENT ON TABLE tokens_redefinicao_senha IS 'Tokens para definição/redefinição de senha (hash no banco, uso único, expira em 48h)';
COMMENT ON COLUMN tokens_redefinicao_senha.token_hash IS 'BCrypt hash do token (token raw enviado por e-mail)';
COMMENT ON COLUMN tokens_redefinicao_senha.expira_em IS 'Expiração do token (padrão 48 horas)';
COMMENT ON COLUMN tokens_redefinicao_senha.usado_em IS 'Preenchido quando o token é usado; NULL = pendente';

-- 5. Verificação rápida
SELECT 'tokens_redefinicao_senha' AS tabela, COUNT(*) AS linhas FROM tokens_redefinicao_senha;

ROLLBACK;  -- remova esta linha e execute novamente para aplicar de verdade


-- =====================================================================
-- VERSÃO COM COMMIT (para aplicar em produção)
-- =====================================================================
-- BEGIN;
--
-- -- 1. Tabela tokens_redefinicao_senha
-- CREATE TABLE IF NOT EXISTS tokens_redefinicao_senha (
--     id              BIGSERIAL PRIMARY KEY,
--     usuario_id      BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
--     token_hash      VARCHAR(255) NOT NULL,
--     expira_em       TIMESTAMPTZ NOT NULL,
--     usado_em        TIMESTAMPTZ,
--     criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
-- );
--
-- -- 2. Índices de performance
-- CREATE INDEX IF NOT EXISTS idx_tokens_usuario_valido
--     ON tokens_redefinicao_senha (usuario_id)
--     WHERE usado_em IS NULL AND expira_em > CURRENT_TIMESTAMP;
--
-- -- 3. Constraint de unicidade: um token válido por usuário por vez
-- DO $$
-- BEGIN
--     IF NOT EXISTS (
--         SELECT 1 FROM pg_constraint
--         WHERE conname = 'uq_tokens_usuario_valido'
--     ) THEN
--         ALTER TABLE tokens_redefinicao_senha
--         ADD CONSTRAINT uq_tokens_usuario_valido
--         UNIQUE (usuario_id)
--         DEFERRABLE INITIALLY IMMEDIATE;
--     END IF;
-- END $$;
--
-- -- 4. Comentários para documentação
-- COMMENT ON TABLE tokens_redefinicao_senha IS 'Tokens para definição/redefinição de senha (hash no banco, uso único, expira em 48h)';
-- COMMENT ON COLUMN tokens_redefinicao_senha.token_hash IS 'BCrypt hash do token (token raw enviado por e-mail)';
-- COMMENT ON COLUMN tokens_redefinicao_senha.expira_em IS 'Expiração do token (padrão 48 horas)';
-- COMMENT ON COLUMN tokens_redefinicao_senha.usado_em IS 'Preenchido quando o token é usado; NULL = pendente';
--
-- -- 5. Verificação rápida
-- SELECT 'tokens_redefinicao_senha' AS tabela, COUNT(*) AS linhas FROM tokens_redefinicao_senha;
--
-- COMMIT;