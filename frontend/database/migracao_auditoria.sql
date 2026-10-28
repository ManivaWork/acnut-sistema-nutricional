BEGIN;

CREATE TABLE IF NOT EXISTS auditoria (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    usuario_id      BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    acao            VARCHAR(30) NOT NULL, -- 'leitura', 'criacao', 'atualizacao', 'exclusao', 'exportacao'
    rota            VARCHAR(200) NOT NULL,
    registro_id     BIGINT,
    ip              INET,
    user_agent      TEXT,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_auditoria_empresa_data
    ON auditoria (empresa_id, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_auditoria_usuario_data
    ON auditoria (usuario_id, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_auditoria_registro
    ON auditoria (registro_id, criado_em DESC);

COMMIT;