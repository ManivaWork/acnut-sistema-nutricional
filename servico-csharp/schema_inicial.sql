-- Schema Inicial - ACNUTRI / AC Hub
-- Idempotente, roda em transação, sem DROP/DELETE, termina em ROLLBACK
-- Aplicar: psql -d achubdb -f servico-csharp/schema_inicial.sql
-- Após revisar, remova a linha final "ROLLBACK;" e execute novamente

BEGIN;

-- ============================================================
-- 1. TABELAS BASE (sem empresa_id - globais ou de referência)
-- ============================================================

-- Perfis de usuário
CREATE TABLE IF NOT EXISTS perfis (
    chave   VARCHAR(50) PRIMARY KEY,
    nome    VARCHAR(100) NOT NULL
);

-- Módulos do sistema
CREATE TABLE IF NOT EXISTS modulos (
    chave   VARCHAR(50) PRIMARY KEY,
    nome    VARCHAR(100) NOT NULL
);

-- Empresas (multi-tenant root)
CREATE TABLE IF NOT EXISTS empresas (
    id          BIGSERIAL PRIMARY KEY,
    nome        VARCHAR(255) NOT NULL,
    criado_em   TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Módulos ativos por empresa
CREATE TABLE IF NOT EXISTS empresa_modulos (
    empresa_id  BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    modulo      VARCHAR(50) NOT NULL REFERENCES modulos(chave) ON DELETE CASCADE,
    PRIMARY KEY (empresa_id, modulo)
);

-- ============================================================
-- 2. USUÁRIOS E AUTENTICAÇÃO
-- ============================================================

CREATE TABLE IF NOT EXISTS usuarios (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    nome            VARCHAR(255) NOT NULL,
    email           VARCHAR(255) NOT NULL,
    senha_hash      VARCHAR(255) NOT NULL,
    perfil          VARCHAR(50) NOT NULL REFERENCES perfis(chave),
    ativo           BOOLEAN NOT NULL DEFAULT true,
    tentativas_falhas INT NOT NULL DEFAULT 0,
    bloqueado_ate   TIMESTAMPTZ,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (email, empresa_id)
);

CREATE INDEX IF NOT EXISTS idx_usuarios_empresa_email ON usuarios (empresa_id, email);
CREATE INDEX IF NOT EXISTS idx_usuarios_email ON usuarios (email);

-- Módulos por usuário (nível de acesso por módulo)
CREATE TABLE IF NOT EXISTS usuario_modulos (
    usuario_id  BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    modulo      VARCHAR(50) NOT NULL REFERENCES modulos(chave) ON DELETE CASCADE,
    nivel       VARCHAR(50) NOT NULL DEFAULT 'leitura', -- 'nenhum', 'leitura', 'escrita', 'total'
    PRIMARY KEY (usuario_id, modulo)
);

-- Convites para novos usuários
CREATE TABLE IF NOT EXISTS convites (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    email           VARCHAR(255) NOT NULL,
    perfil          VARCHAR(50) NOT NULL DEFAULT 'profissional' REFERENCES perfis(chave),
    nivel_nutricao  VARCHAR(50) NOT NULL DEFAULT 'leitura',
    token           VARCHAR(64) NOT NULL UNIQUE,
    expira_em       TIMESTAMPTZ NOT NULL,
    usado_em        TIMESTAMPTZ,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_convites_empresa_email
    ON convites (empresa_id, email) WHERE usado_em IS NULL;

CREATE INDEX IF NOT EXISTS idx_convites_token_valido
    ON convites (token) WHERE usado_em IS NULL;

-- ============================================================
-- 3. PACIENTES E DADOS CLÍNICOS (todos com empresa_id)
-- ============================================================

CREATE TABLE IF NOT EXISTS pacientes (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    nome            VARCHAR(255) NOT NULL,
    email           VARCHAR(255) NOT NULL,
    telefone        VARCHAR(50),
    data_nascimento DATE NOT NULL,
    sexo            VARCHAR(20) NOT NULL,
    status          VARCHAR(50) NOT NULL DEFAULT 'ativo',
    cpf             VARCHAR(14),
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (email, empresa_id),
    UNIQUE (cpf, empresa_id)
);

CREATE INDEX IF NOT EXISTS idx_pacientes_empresa ON pacientes (empresa_id);
CREATE INDEX IF NOT EXISTS idx_pacientes_nome ON pacientes (empresa_id, nome);

-- Avaliações físicas
CREATE TABLE IF NOT EXISTS avaliacoes_fisicas (
    id                  BIGSERIAL PRIMARY KEY,
    empresa_id          BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id         BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    peso_kg             NUMERIC(6,2),
    altura_cm           NUMERIC(6,2),
    percentual_gordura  NUMERIC(5,2),
    massa_magra_kg      NUMERIC(6,2),
    tmb_kcal            NUMERIC(8,2),
    get_kcal            NUMERIC(8,2),
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_avaliacoes_paciente ON avaliacoes_fisicas (paciente_id, criado_em DESC);

-- Alimentos (globais - sem empresa_id)
CREATE TABLE IF NOT EXISTS alimentos (
    id                  BIGSERIAL PRIMARY KEY,
    nome                VARCHAR(255) NOT NULL UNIQUE,
    categoria           VARCHAR(100),
    calorias_100g       NUMERIC(8,2),
    proteinas_100g      NUMERIC(8,2),
    carboidratos_100g   NUMERIC(8,2),
    gorduras_100g       NUMERIC(8,2),
    fibras_100g         NUMERIC(8,2),
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_alimentos_nome ON alimentos (nome);

-- Medidas caseiras por alimento
CREATE TABLE IF NOT EXISTS alimentos_medidas_caseiras (
    id                  BIGSERIAL PRIMARY KEY,
    alimento_id         BIGINT NOT NULL REFERENCES alimentos(id) ON DELETE CASCADE,
    medida              VARCHAR(100) NOT NULL,
    gramas_por_unidade  NUMERIC(10,2) NOT NULL,
    observacao          VARCHAR(255),
    created_at          TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (alimento_id, medida, observacao)
);

CREATE INDEX IF NOT EXISTS idx_alimentos_medidas_alimento ON alimentos_medidas_caseiras (alimento_id);

-- Check-ins semanais
CREATE TABLE IF NOT EXISTS checkins_semanais (
    id                      BIGSERIAL PRIMARY KEY,
    empresa_id              BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id             BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    peso_atual_kg           NUMERIC(6,2),
    adesao_plano_porcentagem INT,
    observacoes_paciente    TEXT,
    data_checkin            TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_checkins_paciente ON checkins_semanais (paciente_id, data_checkin DESC);

-- Anamneses
CREATE TABLE IF NOT EXISTS anamneses (
    id                  BIGSERIAL PRIMARY KEY,
    empresa_id          BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id         BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    historico_medico    TEXT,
    alergias            TEXT,
    intolerancias       TEXT,
    rotina_diaria       TEXT,
    historico_familiar  TEXT,
    atualizado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (paciente_id)
);

-- ============================================================
-- 4. PLANO ALIMENTAR
-- ============================================================

CREATE TABLE IF NOT EXISTS planos_alimentares (
    id                  BIGSERIAL PRIMARY KEY,
    empresa_id          BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id         BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    nome_plano          VARCHAR(255) NOT NULL DEFAULT 'Plano Personalizado',
    meta_calorica_kcal  INT NOT NULL DEFAULT 2000,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (paciente_id)
);

CREATE INDEX IF NOT EXISTS idx_planos_paciente ON planos_alimentares (paciente_id);

-- Refeições
CREATE TABLE IF NOT EXISTS refeicoes (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    plano_id        BIGINT NOT NULL REFERENCES planos_alimentares(id) ON DELETE CASCADE,
    nome_refeicao   VARCHAR(100) NOT NULL,
    horario         TIME,
    observacoes     TEXT,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (plano_id, nome_refeicao)
);

CREATE INDEX IF NOT EXISTS idx_refeicoes_plano ON refeicoes (plano_id, horario);

-- Itens da refeição (com opções e substituições)
CREATE TABLE IF NOT EXISTS itens_refeicao (
    id                      BIGSERIAL PRIMARY KEY,
    empresa_id              BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    refeicao_id             BIGINT NOT NULL REFERENCES refeicoes(id) ON DELETE CASCADE,
    opcao                   INT NOT NULL DEFAULT 1,
    substitui_item_id       BIGINT REFERENCES itens_refeicao(id) ON DELETE SET NULL,
    alimento_id             BIGINT REFERENCES alimentos(id) ON DELETE SET NULL,
    alimento_nome_livre     VARCHAR(255),
    quantidade_g            NUMERIC(10,2),
    medida_caseira          VARCHAR(100),
    kcal_manual             NUMERIC(8,2),
    proteinas_manual        NUMERIC(8,2),
    carbos_manual           NUMERIC(8,2),
    gorduras_manual         NUMERIC(8,2),
    e_substituicao          BOOLEAN NOT NULL DEFAULT false,
    criado_em               TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_itens_refeicao ON itens_refeicao (refeicao_id, opcao);
CREATE INDEX IF NOT EXISTS idx_itens_substituicao ON itens_refeicao (substitui_item_id) WHERE e_substituicao;

-- Versões do plano (snapshots)
CREATE TABLE IF NOT EXISTS planos_versoes (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id     BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    numero          INT NOT NULL,
    descricao       TEXT,
    snapshot        JSONB NOT NULL,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (paciente_id, numero)
);

CREATE INDEX IF NOT EXISTS idx_planos_versoes_paciente ON planos_versoes (paciente_id, numero DESC);

-- Orientações nutricionais
CREATE TABLE IF NOT EXISTS orientacoes_nutricionais (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id     BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    texto           TEXT NOT NULL DEFAULT '',
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (paciente_id)
);

-- ============================================================
-- 5. METAS E ACOMPANHAMENTO
-- ============================================================

CREATE TABLE IF NOT EXISTS metas_paciente (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id     BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    titulo          VARCHAR(255) NOT NULL,
    descricao       TEXT,
    tipo            VARCHAR(50) NOT NULL DEFAULT 'outro', -- 'agua', 'frutas', 'refeicoes', 'atividade', 'sono', 'outro'
    prazo           DATE,
    status          VARCHAR(50) NOT NULL DEFAULT 'ativa', -- 'ativa', 'concluida', 'arquivada'
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em   TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_metas_paciente ON metas_paciente (paciente_id, status);

CREATE TABLE IF NOT EXISTS metas_acompanhamento (
    id                      BIGSERIAL PRIMARY KEY,
    empresa_id              BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    meta_id                 BIGINT NOT NULL REFERENCES metas_paciente(id) ON DELETE CASCADE,
    data_acompanhamento     DATE NOT NULL DEFAULT CURRENT_DATE,
    resultado               VARCHAR(50) NOT NULL, -- 'mantida', 'parcial', 'nao_mantida'
    observacao              TEXT,
    criado_em               TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_metas_acomp_meta ON metas_acompanhamento (meta_id, data_acompanhamento DESC);

-- ============================================================
-- 6. ARQUIVOS, EVOLUÇÃO, EXAMES, OBSERVAÇÕES
-- ============================================================

CREATE TABLE IF NOT EXISTS arquivos_paciente (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id     BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    origem          VARCHAR(50) NOT NULL, -- 'evolucao', 'exame', 'avaliacao', etc.
    origem_id       BIGINT NOT NULL,
    nome            VARCHAR(255) NOT NULL,
    mime            VARCHAR(100) NOT NULL,
    tamanho         BIGINT NOT NULL,
    dados           BYTEA NOT NULL,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_arquivos_paciente ON arquivos_paciente (paciente_id, origem, origem_id);

-- Registros de evolução
CREATE TABLE IF NOT EXISTS evolucao_registros (
    id                  BIGSERIAL PRIMARY KEY,
    empresa_id          BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id         BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    data_registro       DATE NOT NULL DEFAULT CURRENT_DATE,
    peso_kg             NUMERIC(6,2),
    percentual_gordura  NUMERIC(5,2),
    cintura_cm          NUMERIC(6,2),
    quadril_cm          NUMERIC(6,2),
    abdomen_cm          NUMERIC(6,2),
    braco_cm            NUMERIC(6,2),
    coxa_cm             NUMERIC(6,2),
    observacao          TEXT,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_evolucao_paciente ON evolucao_registros (paciente_id, data_registro DESC);

-- Exames
CREATE TABLE IF NOT EXISTS exames_paciente (
    id                  BIGSERIAL PRIMARY KEY,
    empresa_id          BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id         BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    titulo_exame        VARCHAR(255) NOT NULL,
    data_exame          DATE NOT NULL,
    observacao          TEXT,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_exames_paciente ON exames_paciente (paciente_id, data_exame DESC);

-- Observações
CREATE TABLE IF NOT EXISTS observacoes_paciente (
    id              BIGSERIAL PRIMARY KEY,
    empresa_id      BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    paciente_id     BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    tipo            VARCHAR(50) NOT NULL DEFAULT 'geral', -- 'geral', 'semana'
    data_ref        DATE NOT NULL DEFAULT CURRENT_DATE,
    texto           TEXT NOT NULL,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_obs_paciente ON observacoes_paciente (paciente_id, data_ref DESC);

-- ============================================================
-- 7. ACESSOS PACIENTE (login paciente via CPF)
-- ============================================================

CREATE TABLE IF NOT EXISTS acessos_paciente (
    id              BIGSERIAL PRIMARY KEY,
    paciente_id     BIGINT REFERENCES pacientes(id) ON DELETE SET NULL,
    cpf_tentado     VARCHAR(14) NOT NULL,
    ip              INET,
    sucesso         BOOLEAN NOT NULL,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_acessos_cpf ON acessos_paciente (cpf_tentado, criado_em DESC);
CREATE INDEX IF NOT EXISTS idx_acessos_ip ON acessos_paciente (ip, criado_em DESC);

-- ============================================================
-- 8. DADOS INICIAIS (SEED) - perfis, modulos, empresa padrão
-- ============================================================

INSERT INTO perfis (chave, nome) VALUES
    ('super_admin', 'Super Administrador'),
    ('admin', 'Administrador'),
    ('profissional', 'Profissional'),
    ('atendente', 'Atendente')
ON CONFLICT (chave) DO NOTHING;

INSERT INTO modulos (chave, nome) VALUES
    ('nutricao', 'Nutrição')
ON CONFLICT (chave) DO NOTHING;

INSERT INTO empresas (nome)
SELECT 'Empresa Principal'
WHERE NOT EXISTS (SELECT 1 FROM empresas);

INSERT INTO empresa_modulos (empresa_id, modulo)
SELECT MIN(id), 'nutricao' FROM empresas
ON CONFLICT DO NOTHING;

-- ============================================================
-- 9. VERIFICAÇÃO RÁPIDA
-- ============================================================

SELECT schemaname, tablename, tableowner
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY tablename;

-- Contagem de linhas nas tabelas principais
SELECT 'perfis' AS tabela, COUNT(*) FROM perfis
UNION ALL SELECT 'modulos', COUNT(*) FROM modulos
UNION ALL SELECT 'empresas', COUNT(*) FROM empresas
UNION ALL SELECT 'empresa_modulos', COUNT(*) FROM empresa_modulos
UNION ALL SELECT 'usuarios', COUNT(*) FROM usuarios
UNION ALL SELECT 'pacientes', COUNT(*) FROM pacientes
UNION ALL SELECT 'alimentos', COUNT(*) FROM alimentos
UNION ALL SELECT 'convites', COUNT(*) FROM convites;

COMMIT;  -- REMOVA ESTA LINHA E EXECUTE NOVAMENTE PARA APLICAR DE VERDADE