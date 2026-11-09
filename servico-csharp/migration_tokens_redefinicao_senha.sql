CREATE TABLE IF NOT EXISTS tokens_redefinicao_senha (
    id          BIGSERIAL PRIMARY KEY,
    usuario_id  BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    token_hash  VARCHAR(255) NOT NULL,
    expira_em   TIMESTAMPTZ NOT NULL,
    usado_em    TIMESTAMPTZ,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_tokens_usuario_pendente
    ON tokens_redefinicao_senha (usuario_id)
    WHERE usado_em IS NULL;

CREATE INDEX IF NOT EXISTS idx_tokens_expira
    ON tokens_redefinicao_senha (expira_em);

CREATE UNIQUE INDEX IF NOT EXISTS uq_tokens_hash
    ON tokens_redefinicao_senha (token_hash);

COMMENT ON TABLE tokens_redefinicao_senha IS 'Tokens de definição/redefinição de senha (só hash, uso único, expira em 48h)';
COMMENT ON COLUMN tokens_redefinicao_senha.token_hash IS 'Hash do token enviado por e-mail';
COMMENT ON COLUMN tokens_redefinicao_senha.usado_em IS 'NULL = pendente';

SELECT to_regclass('tokens_redefinicao_senha');