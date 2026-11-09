-- 1. Tabela
CREATE TABLE IF NOT EXISTS solicitacoes_acesso (
    id                    BIGSERIAL PRIMARY KEY,
    nome                  VARCHAR(255) NOT NULL,
    email                 VARCHAR(255) NOT NULL,
    telefone              VARCHAR(50),
    registro_profissional VARCHAR(100),
    clinica_nome          VARCHAR(255),
    mensagem              TEXT,
    status                VARCHAR(20) NOT NULL DEFAULT 'pendente'
        CHECK (status IN ('pendente', 'aprovado', 'recusado', 'excluido')),
    empresa_id            BIGINT REFERENCES empresas(id) ON DELETE SET NULL,
    usuario_id            BIGINT REFERENCES usuarios(id) ON DELETE SET NULL,
    ip                    INET,
    user_agent            TEXT,
    criado_em             TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em         TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- 2. Limpa versões antigas da unicidade
ALTER TABLE solicitacoes_acesso
    DROP CONSTRAINT IF EXISTS uq_solicitacoes_email_pendente;
DROP INDEX IF EXISTS idx_solicitacoes_email_pendente;

-- 3. Índices
CREATE INDEX IF NOT EXISTS idx_solicitacoes_status_criado
    ON solicitacoes_acesso (status, criado_em DESC);

CREATE UNIQUE INDEX IF NOT EXISTS uq_solicitacoes_email_pendente
    ON solicitacoes_acesso (lower(email))
    WHERE status = 'pendente';

CREATE INDEX IF NOT EXISTS idx_solicitacoes_empresa
    ON solicitacoes_acesso (empresa_id)
    WHERE empresa_id IS NOT NULL;

-- 4. Trigger de atualizado_em
CREATE OR REPLACE FUNCTION fn_solicitacoes_atualizado_em()
RETURNS TRIGGER AS $fn$
BEGIN
    NEW.atualizado_em = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$fn$ LANGUAGE plpgsql;

DROP TRIGGER IF EXISTS trg_solicitacoes_atualizado_em ON solicitacoes_acesso;
CREATE TRIGGER trg_solicitacoes_atualizado_em
    BEFORE UPDATE ON solicitacoes_acesso
    FOR EACH ROW
    EXECUTE FUNCTION fn_solicitacoes_atualizado_em();

-- 5. Comentários
COMMENT ON TABLE solicitacoes_acesso IS 'Solicitações de acesso (fluxo público "Solicitar acesso")';
COMMENT ON COLUMN solicitacoes_acesso.status IS 'pendente/aprovado/recusado/excluido';
COMMENT ON COLUMN solicitacoes_acesso.ip IS 'IP do solicitante (rate limiting)';
COMMENT ON COLUMN solicitacoes_acesso.empresa_id IS 'Preenchido ao aprovar';
COMMENT ON COLUMN solicitacoes_acesso.usuario_id IS 'Preenchido ao aprovar';

-- 6. Verificação
SELECT 'solicitacoes_acesso' AS tabela, COUNT(*) AS linhas FROM solicitacoes_acesso;

