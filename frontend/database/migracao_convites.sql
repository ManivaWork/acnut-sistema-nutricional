-- =====================================================================
-- MIGRAÇÃO CONVITES: Cria a tabela de convites para onboarding multi-tenant
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
    chave VARCHAR(30) PRIMARY KEY,
    nome VARCHAR(50) NOT NULL
);

-- Dados iniciais de perfis (idempotente via ON CONFLICT)
INSERT INTO perfis (chave, nome) VALUES
    ('super_admin', 'Super Administrador'),
    ('admin', 'Administrador'),
    ('profissional', 'Profissional'),
    ('atendente', 'Atendente')
ON CONFLICT (chave) DO NOTHING;

-- ---------------------------------------------------------------------
-- 2. TABELA DE EMPRESAS (referenciada por convites.empresa_id)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS empresas (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(150) NOT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ---------------------------------------------------------------------
-- 3. TABELA DE CONVITES
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS convites (
    id SERIAL PRIMARY KEY,
    empresa_id INT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    email VARCHAR(100) NOT NULL,
    perfil VARCHAR(30) NOT NULL REFERENCES perfis(chave) ON DELETE CASCADE,
    nivel_nutricao VARCHAR(20) NOT NULL DEFAULT 'leitura'
        CHECK (nivel_nutricao IN ('nenhum', 'leitura', 'edicao', 'total')),
    token VARCHAR(32) NOT NULL UNIQUE,
    expira_em TIMESTAMP NOT NULL,
    usado_em TIMESTAMP,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Índices para performance nas consultas mais frequentes
CREATE INDEX IF NOT EXISTS idx_convites_token ON convites (token);
CREATE INDEX IF NOT EXISTS idx_convites_empresa ON convites (empresa_id);
CREATE INDEX IF NOT EXISTS idx_convites_email_empresa ON convites (email, empresa_id);

-- ---------------------------------------------------------------------
-- 4. CONFERÊNCIA: estrutura da tabela convites
-- ---------------------------------------------------------------------
SELECT table_name, column_name, is_nullable, data_type, column_default
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'convites'
ORDER BY ordinal_position;

-- ---------------------------------------------------------------------
-- 5. CONFERÊNCIA: índices criados
-- ---------------------------------------------------------------------
SELECT indexname, tablename
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename = 'convites'
ORDER BY indexname;

-- ---------------------------------------------------------------------
-- 6. CONFERÊNCIA: perfis populados
-- ---------------------------------------------------------------------
SELECT chave, nome FROM perfis ORDER BY chave;

-- >>> MODO TESTE: nada é gravado. Troque por COMMIT; para aplicar de verdade. <<<
ROLLBACK;