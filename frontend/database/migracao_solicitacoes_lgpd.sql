BEGIN;

CREATE TABLE IF NOT EXISTS solicitacoes_lgpd (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id     BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    tipo            VARCHAR(30) NOT NULL CHECK (tipo IN ('correcao', 'exclusao', 'informacao')),
    descricao       TEXT NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'pendente' CHECK (status IN ('pendente', 'em_analise', 'atendida', 'recusada')),
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    resolvido_em    TIMESTAMPTZ
);

CREATE INDEX IF NOT EXISTS idx_solicitacoes_paciente
    ON solicitacoes_lgpd (paciente_id, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_solicitacoes_empresa_status
    ON solicitacoes_lgpd (empresa_id, status, criado_em DESC);

COMMIT;