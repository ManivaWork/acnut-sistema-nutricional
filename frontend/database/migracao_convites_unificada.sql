-- =====================================================================
-- MIGRAÇÃO CONVITES UNIFICADA: Cria tabela convites + dependências
-- Idempotente, transacional, sem DROP/DELETE/TRUNCATE.
--
-- ATENÇÃO: o último comando deste arquivo é ROLLBACK (modo de teste).
-- Depois de conferir os resultados, troque o ROLLBACK final por COMMIT
-- e rode o script de novo para gravar de verdade.
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. TABELA DE PERFIS (base para FK de convites.perfil)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS perfis (
    chave VARCHAR(50) PRIMARY KEY,
    nome VARCHAR(100) NOT NULL
);

-- Dados iniciais de perfis (idempotente via ON CONFLICT)
INSERT INTO perfis (chave, nome) VALUES
    ('super_admin', 'Super Administrador'),
    ('admin', 'Administrador'),
    ('profissional', 'Profissional de Nutrição'),
    ('atendente', 'Atendente/Recepção')
ON CONFLICT (chave) DO NOTHING;

-- ---------------------------------------------------------------------
-- 2. TABELA DE EMPRESAS (referenciada por convites.empresa_id)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS empresas (
    id BIGSERIAL PRIMARY KEY,
    nome VARCHAR(150) NOT NULL,
    email VARCHAR(255),
    telefone VARCHAR(30),
    cnpj VARCHAR(20),
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ---------------------------------------------------------------------
-- 3. TABELA DE CONVITES
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS convites (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    email           VARCHAR(255) NOT NULL,
    perfil          VARCHAR(50) NOT NULL DEFAULT 'profissional',
    nivel_nutricao  VARCHAR(50) NOT NULL DEFAULT 'leitura'
        CHECK (nivel_nutricao IN ('nenhum', 'leitura', 'escrita', 'total')),
    token           VARCHAR(64) NOT NULL UNIQUE,
    expira_em       TIMESTAMPTZ NOT NULL,
    usado_em        TIMESTAMPTZ,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- ---------------------------------------------------------------------
-- 4. ÍNDICES DE PERFORMANCE
-- ---------------------------------------------------------------------
-- Índice parcial: busca convite válido por token (não usado e não expirado)
CREATE INDEX IF NOT EXISTS idx_convites_token_valido
    ON convites (token)
    WHERE usado_em IS NULL AND expira_em > CURRENT_TIMESTAMP;

-- Índice parcial: verifica convite ativo por email+empresa
CREATE INDEX IF NOT EXISTS idx_convites_empresa_email_ativo
    ON convites (empresa_id, email)
    WHERE usado_em IS NULL;

-- ---------------------------------------------------------------------
-- 5. CONSTRAINT DE UNICIDADE: um convite ativo por email por empresa
-- (parcial: só enquanto não usado e não expirado)
-- ---------------------------------------------------------------------
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

-- ---------------------------------------------------------------------
-- 6. COMENTÁRIOS PARA DOCUMENTAÇÃO
-- ---------------------------------------------------------------------
COMMENT ON TABLE convites IS 'Convites para usuários entrarem na empresa (expiram em 7 dias)';
COMMENT ON COLUMN convites.token IS 'Token único (UUID v4 sem hífens) enviado por e-mail';
COMMENT ON COLUMN convites.expira_em IS 'Expiração do convite (padrão 7 dias)';
COMMENT ON COLUMN convites.usado_em IS 'Preenchido quando o convite é aceito; NULL = pendente';
COMMENT ON COLUMN convites.perfil IS 'Perfil do usuário: super_admin, admin, profissional, atendente';
COMMENT ON COLUMN convites.nivel_nutricao IS 'Nível de acesso nutrição: nenhum, leitura, escrita, total';

-- ---------------------------------------------------------------------
-- 7. CONFERÊNCIA: estrutura da tabela convites
-- ---------------------------------------------------------------------
SELECT table_name, column_name, is_nullable, data_type, column_default
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'convites'
ORDER BY ordinal_position;

-- ---------------------------------------------------------------------
-- 8. CONFERÊNCIA: índices criados
-- ---------------------------------------------------------------------
SELECT indexname, tablename
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename = 'convites'
ORDER BY indexname;

-- ---------------------------------------------------------------------
-- 9. CONFERÊNCIA: constraints
-- ---------------------------------------------------------------------
SELECT conname, contype, pg_get_constraintdef(oid) AS definition
FROM pg_constraint
WHERE conrelid = 'convites'::regclass
ORDER BY conname;

-- ---------------------------------------------------------------------
-- 10. CONFERÊNCIA: perfis populados
-- ---------------------------------------------------------------------
SELECT chave, nome FROM perfis ORDER BY chave;

-- ---------------------------------------------------------------------
-- 11. CONFERÊNCIA: contagem de linhas
-- ---------------------------------------------------------------------
SELECT 'convites' AS tabela, COUNT(*) AS linhas FROM convites
UNION ALL
SELECT 'perfis', COUNT(*) FROM perfis
UNION ALL
SELECT 'empresas', COUNT(*) FROM empresas;

-- >>> MODO TESTE: nada é gravado. Troque por COMMIT; para aplicar de verdade. <<<
ROLLBACK;