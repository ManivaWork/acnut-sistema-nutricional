BEGIN;

CREATE TABLE IF NOT EXISTS consentimentos (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id     BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    tipo            VARCHAR(50) NOT NULL, -- 'lgpd', 'termos', 'tratamento_dados', etc.
    versao_texto    VARCHAR(50) NOT NULL, -- versão do texto aceito (ex: '1.0', '2024-01')
    aceito          BOOLEAN NOT NULL DEFAULT true,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    ip              INET
);

CREATE INDEX IF NOT EXISTS idx_consentimentos_paciente
    ON consentimentos (paciente_id, tipo, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_consentimentos_empresa_tipo
    ON consentimentos (empresa_id, tipo, versao_texto);

COMMIT;