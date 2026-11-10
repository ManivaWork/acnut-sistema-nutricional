-- Migration: Create tentativas_login table for rate limiting by identifier+IP
-- This table tracks failed login attempts by identifier (email/CPF) + IP combination
-- Used for: /api/auth/login (equipe), /api/auth/paciente, /api/definir-senha/{token}, /api/acesso/solicitar

BEGIN;

CREATE TABLE IF NOT EXISTS tentativas_login (
    id BIGSERIAL PRIMARY KEY,
    tipo VARCHAR(50) NOT NULL,           -- 'equipe', 'paciente', 'definir_senha', 'solicitar_acesso'
    identificador VARCHAR(255) NOT NULL, -- email ou CPF limpo
    ip INET NOT NULL,                    -- IP do cliente
    sucesso BOOLEAN NOT NULL DEFAULT false,
    criado_em TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Índice para consultas rápidas de contagem de falhas recentes
CREATE INDEX IF NOT EXISTS ix_tentativas_login_tipo_ident_ip_criado
    ON tentativas_login (tipo, identificador, ip, criado_em DESC)
    WHERE sucesso = false;

-- Índice para limpeza periódica de registros antigos
CREATE INDEX IF NOT EXISTS ix_tentativas_login_criado_em
    ON tentativas_login (criado_em);

COMMIT;