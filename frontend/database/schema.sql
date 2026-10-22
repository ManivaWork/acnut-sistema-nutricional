BEGIN;

-- ---------------------------------------------------------------------
-- 1. TABELAS BASE
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS pacientes (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(150) NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    telefone VARCHAR(20),
    data_nascimento DATE NOT NULL,
    sexo CHAR(1) CHECK (sexo IN ('M', 'F')),
    foto_url TEXT,
    status VARCHAR(20) DEFAULT 'ativo',
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE pacientes ADD COLUMN IF NOT EXISTS status VARCHAR(20) DEFAULT 'ativo';

CREATE TABLE IF NOT EXISTS anamneses (
    id SERIAL PRIMARY KEY,
    paciente_id INT REFERENCES pacientes(id) ON DELETE CASCADE,
    historico_medico TEXT,
    alergias TEXT,
    intolerancias TEXT,
    rotina_diaria TEXT,
    historico_familiar TEXT,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE anamneses ADD COLUMN IF NOT EXISTS historico_medico TEXT;
ALTER TABLE anamneses ADD COLUMN IF NOT EXISTS alergias TEXT;
ALTER TABLE anamneses ADD COLUMN IF NOT EXISTS intolerancias TEXT;
ALTER TABLE anamneses ADD COLUMN IF NOT EXISTS rotina_diaria TEXT;
ALTER TABLE anamneses ADD COLUMN IF NOT EXISTS historico_familiar TEXT;
ALTER TABLE anamneses ADD COLUMN IF NOT EXISTS atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP;

CREATE TABLE IF NOT EXISTS avaliacoes_fisicas (
    id SERIAL PRIMARY KEY,
    paciente_id INT REFERENCES pacientes(id) ON DELETE CASCADE,
    data_avaliacao DATE DEFAULT CURRENT_DATE,
    peso_kg DECIMAL(5,2) NOT NULL,
    altura_cm DECIMAL(5,2) NOT NULL,
    percentual_gordura DECIMAL(4,1),
    massa_magra_kg DECIMAL(5,2),
    circunferencia_cintura_cm DECIMAL(5,2),
    circunferencia_quadril_cm DECIMAL(5,2),
    tmb_kcal DECIMAL(6,2),
    get_kcal DECIMAL(6,2),
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS alimentos (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(100) NOT NULL,
    categoria VARCHAR(50),
    calorias_100g DECIMAL(6,2) NOT NULL,
    proteinas_100g DECIMAL(5,2) NOT NULL,
    carboidratos_100g DECIMAL(5,2) NOT NULL,
    gorduras_100g DECIMAL(5,2) NOT NULL,
    fibras_100g DECIMAL(5,2) DEFAULT 0.00
);

CREATE TABLE IF NOT EXISTS checkins_semanais (
    id SERIAL PRIMARY KEY,
    paciente_id INT REFERENCES pacientes(id) ON DELETE CASCADE,
    data_checkin DATE DEFAULT CURRENT_DATE,
    peso_atual_kg DECIMAL(5,2) NOT NULL,
    adesao_plano_porcentagem INT CHECK (adesao_plano_porcentagem BETWEEN 0 AND 100),
    observacoes_paciente TEXT,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
-- Garante as colunas que o C# usa, mesmo se a tabela antiga era outra versão
ALTER TABLE checkins_semanais ADD COLUMN IF NOT EXISTS data_checkin DATE DEFAULT CURRENT_DATE;
ALTER TABLE checkins_semanais ADD COLUMN IF NOT EXISTS peso_atual_kg DECIMAL(5,2);
ALTER TABLE checkins_semanais ADD COLUMN IF NOT EXISTS adesao_plano_porcentagem INT;
ALTER TABLE checkins_semanais ADD COLUMN IF NOT EXISTS observacoes_paciente TEXT;

CREATE TABLE IF NOT EXISTS exames_paciente (
    id SERIAL PRIMARY KEY,
    paciente_id INT REFERENCES pacientes(id) ON DELETE CASCADE,
    titulo_exame VARCHAR(100) NOT NULL,
    data_exame DATE NOT NULL,
    observacao TEXT,
    arquivo_url TEXT,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS orientacoes_nutricionais (
    id SERIAL PRIMARY KEY,
    paciente_id INT UNIQUE REFERENCES pacientes(id) ON DELETE CASCADE,
    texto TEXT,
    atualizado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- ---------------------------------------------------------------------
-- 2. PLANO ALIMENTAR
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS planos_alimentares (
    id SERIAL PRIMARY KEY,
    paciente_id INT REFERENCES pacientes(id) ON DELETE CASCADE,
    titulo VARCHAR(100) DEFAULT 'Plano Alimentar',
    nome_plano VARCHAR(255),
    meta_calorias DECIMAL(6,2) DEFAULT 2000,
    meta_calorica_kcal NUMERIC,
    meta_proteinas_g DECIMAL(6,2) DEFAULT 100,
    meta_carboidratos_g DECIMAL(6,2) DEFAULT 250,
    meta_gorduras_g DECIMAL(6,2) DEFAULT 60,
    ativo BOOLEAN DEFAULT TRUE,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
ALTER TABLE planos_alimentares ADD COLUMN IF NOT EXISTS nome_plano VARCHAR(255);
ALTER TABLE planos_alimentares ADD COLUMN IF NOT EXISTS meta_calorica_kcal NUMERIC;
ALTER TABLE planos_alimentares ALTER COLUMN titulo DROP NOT NULL;
ALTER TABLE planos_alimentares ALTER COLUMN titulo SET DEFAULT 'Plano Alimentar';
ALTER TABLE planos_alimentares ALTER COLUMN meta_calorias DROP NOT NULL;
ALTER TABLE planos_alimentares ALTER COLUMN meta_calorias SET DEFAULT 2000;
ALTER TABLE planos_alimentares ALTER COLUMN meta_proteinas_g DROP NOT NULL;
ALTER TABLE planos_alimentares ALTER COLUMN meta_proteinas_g SET DEFAULT 100;
ALTER TABLE planos_alimentares ALTER COLUMN meta_carboidratos_g DROP NOT NULL;
ALTER TABLE planos_alimentares ALTER COLUMN meta_carboidratos_g SET DEFAULT 250;
ALTER TABLE planos_alimentares ALTER COLUMN meta_gorduras_g DROP NOT NULL;
ALTER TABLE planos_alimentares ALTER COLUMN meta_gorduras_g SET DEFAULT 60;

CREATE TABLE IF NOT EXISTS refeicoes (
    id SERIAL PRIMARY KEY,
    plano_id INT REFERENCES planos_alimentares(id) ON DELETE CASCADE,
    nome VARCHAR(50) DEFAULT 'Refeição',
    nome_refeicao VARCHAR(255),
    horario TIME,
    observacoes TEXT
);
ALTER TABLE refeicoes ADD COLUMN IF NOT EXISTS nome_refeicao VARCHAR(255);
ALTER TABLE refeicoes ADD COLUMN IF NOT EXISTS observacoes TEXT;
ALTER TABLE refeicoes ALTER COLUMN nome DROP NOT NULL;
ALTER TABLE refeicoes ALTER COLUMN nome SET DEFAULT 'Refeição';

-- Se "horario" tiver virado texto em algum momento, volta para TIME
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'public' AND table_name = 'refeicoes'
                 AND column_name = 'horario' AND data_type <> 'time without time zone') THEN
        ALTER TABLE refeicoes ALTER COLUMN horario TYPE TIME
            USING NULLIF(horario::text, '')::time;
    END IF;
END $$;

-- Refeições antigas: copia o nome antigo para a coluna que o C# usa
UPDATE refeicoes SET nome_refeicao = nome WHERE nome_refeicao IS NULL;

CREATE TABLE IF NOT EXISTS itens_refeicao (
    id SERIAL PRIMARY KEY,
    refeicao_id INT REFERENCES refeicoes(id) ON DELETE CASCADE,
    alimento_id INT REFERENCES alimentos(id),
    alimento_nome_livre VARCHAR(255),
    quantidade_g DECIMAL(8,2),
    medida_caseira VARCHAR(255),
    kcal_manual NUMERIC(10,2) DEFAULT 0,
    proteinas_manual NUMERIC(10,2) DEFAULT 0,
    carbos_manual NUMERIC(10,2) DEFAULT 0,
    gorduras_manual NUMERIC(10,2) DEFAULT 0,
    e_substituicao BOOLEAN DEFAULT FALSE,
    opcao INT NOT NULL DEFAULT 1,
    substitui_item_id INT REFERENCES itens_refeicao(id) ON DELETE CASCADE
);
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS alimento_nome_livre VARCHAR(255);
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS quantidade_g DECIMAL(8,2);
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS medida_caseira VARCHAR(255);
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS kcal_manual NUMERIC(10,2) DEFAULT 0;
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS proteinas_manual NUMERIC(10,2) DEFAULT 0;
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS carbos_manual NUMERIC(10,2) DEFAULT 0;
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS gorduras_manual NUMERIC(10,2) DEFAULT 0;
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS e_substituicao BOOLEAN DEFAULT FALSE;
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS opcao INT NOT NULL DEFAULT 1;
ALTER TABLE itens_refeicao ADD COLUMN IF NOT EXISTS substitui_item_id INT
    REFERENCES itens_refeicao(id) ON DELETE CASCADE;
ALTER TABLE itens_refeicao ALTER COLUMN alimento_id DROP NOT NULL;

-- Colunas da estrutura ANTIGA: não podem bloquear o INSERT novo
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns
               WHERE table_schema = 'public' AND table_name = 'itens_refeicao'
                 AND column_name = 'quantidade_gramas') THEN
        ALTER TABLE itens_refeicao ALTER COLUMN quantidade_gramas DROP NOT NULL;
        ALTER TABLE itens_refeicao ALTER COLUMN quantidade_gramas SET DEFAULT 0;
    END IF;
END $$;

-- Itens antigos marcados como "substituição" sem item de origem viram Opção 2
UPDATE itens_refeicao
SET opcao = 2, e_substituicao = FALSE
WHERE e_substituicao = TRUE AND substitui_item_id IS NULL;

CREATE TABLE IF NOT EXISTS planos_versoes (
    id SERIAL PRIMARY KEY,
    paciente_id INT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    numero INT NOT NULL,
    descricao TEXT,
    snapshot JSONB NOT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (paciente_id, numero)
);

-- ---------------------------------------------------------------------
-- 3. LIMPEZA DE DUPLICADOS (só faz algo se existirem duplicados)
-- ---------------------------------------------------------------------
-- Anamnese: mantém a mais recente por paciente
DELETE FROM anamneses a
USING anamneses b
WHERE a.paciente_id = b.paciente_id AND a.id < b.id;

-- Planos: une planos duplicados no de menor id
UPDATE refeicoes r
SET plano_id = k.keep_id
FROM planos_alimentares p
JOIN (SELECT paciente_id, MIN(id) AS keep_id
      FROM planos_alimentares GROUP BY paciente_id) k
  ON k.paciente_id = p.paciente_id
WHERE r.plano_id = p.id AND p.id <> k.keep_id;

DELETE FROM planos_alimentares p
USING (SELECT paciente_id, MIN(id) AS keep_id
       FROM planos_alimentares GROUP BY paciente_id) k
WHERE p.paciente_id = k.paciente_id AND p.id <> k.keep_id;

-- Refeições: une refeições duplicadas dentro do mesmo plano
UPDATE itens_refeicao i
SET refeicao_id = k.keep_id
FROM refeicoes r
JOIN (SELECT plano_id, nome_refeicao, MIN(id) AS keep_id
      FROM refeicoes GROUP BY plano_id, nome_refeicao) k
  ON k.plano_id = r.plano_id AND k.nome_refeicao = r.nome_refeicao
WHERE i.refeicao_id = r.id AND r.id <> k.keep_id;

DELETE FROM refeicoes r
USING (SELECT plano_id, nome_refeicao, MIN(id) AS keep_id
       FROM refeicoes GROUP BY plano_id, nome_refeicao) k
WHERE r.plano_id = k.plano_id
  AND r.nome_refeicao = k.nome_refeicao
  AND r.id <> k.keep_id;

-- ---------------------------------------------------------------------
-- 4. CONSTRAINTS (só cria se ainda não existirem)
-- ---------------------------------------------------------------------
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_anamneses_paciente') THEN
        ALTER TABLE anamneses ADD CONSTRAINT uq_anamneses_paciente UNIQUE (paciente_id);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_plano_paciente') THEN
        ALTER TABLE planos_alimentares ADD CONSTRAINT uq_plano_paciente UNIQUE (paciente_id);
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'uq_refeicao_plano_nome') THEN
        ALTER TABLE refeicoes ADD CONSTRAINT uq_refeicao_plano_nome UNIQUE (plano_id, nome_refeicao);
    END IF;
END $$;

-- Coluna redundante (o C# usa plano_id)
ALTER TABLE refeicoes DROP COLUMN IF EXISTS plano_alimentar_id;

-- ---------------------------------------------------------------------
-- 5. DADOS INICIAIS (não duplicam se rodar de novo)
-- ---------------------------------------------------------------------
INSERT INTO alimentos (nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
SELECT * FROM (VALUES
    ('Peito de Frango Grelhado',      'Proteínas',           165.00, 31.00,  0.00,   3.60, 0.00),
    ('Arroz Integral Cozido',         'Carboidratos',        111.00,  2.60, 23.00,   0.90, 1.80),
    ('Ovo Cozido',                    'Proteínas/Gorduras',  155.00, 13.00,  1.10,  11.00, 0.00),
    ('Aveia em Flocos',               'Carboidratos/Fibras', 394.00, 13.90, 66.60,   8.50, 9.10),
    ('Banana Prata',                  'Frutas',               98.00,  1.30, 26.00,   0.10, 2.00),
    ('Azeite de Oliva Extra Virgem',  'Gorduras',            884.00,  0.00,  0.00, 100.00, 0.00),
    ('Feijão Preto Cozido',           'Leguminosas',          77.00,  4.50, 14.00,   0.50, 8.40)
) AS v(nome, categoria, cal, prot, carb, gord, fib)
WHERE NOT EXISTS (SELECT 1 FROM alimentos);

INSERT INTO pacientes (nome, email, telefone, data_nascimento, sexo)
VALUES ('Maria Silva', 'maria.silva@email.com', '(11) 98765-4321', '1995-06-15', 'F')
ON CONFLICT (email) DO NOTHING;

COMMIT;

-- ---------------------------------------------------------------------
-- 6. CONFERÊNCIA (rode depois; deve listar opcao e substitui_item_id)
-- ---------------------------------------------------------------------
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'itens_refeicao'
ORDER BY ordinal_position;

BEGIN;

CREATE TABLE IF NOT EXISTS metas_paciente (
    id SERIAL PRIMARY KEY,
    paciente_id INT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    titulo VARCHAR(150) NOT NULL,
    descricao TEXT,
    tipo VARCHAR(30) NOT NULL DEFAULT 'outro',
    prazo DATE,
    status VARCHAR(20) NOT NULL DEFAULT 'ativa'
        CHECK (status IN ('ativa', 'concluida', 'arquivada')),
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS metas_acompanhamento (
    id SERIAL PRIMARY KEY,
    meta_id INT NOT NULL REFERENCES metas_paciente(id) ON DELETE CASCADE,
    data_acompanhamento DATE NOT NULL DEFAULT CURRENT_DATE,
    resultado VARCHAR(20) NOT NULL
        CHECK (resultado IN ('mantida', 'parcial', 'nao_mantida')),
    observacao TEXT,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_metas_paciente ON metas_paciente (paciente_id);
CREATE INDEX IF NOT EXISTS idx_metas_acomp_meta ON metas_acompanhamento (meta_id, data_acompanhamento DESC);

COMMIT;

-- Conferência: deve listar as duas tabelas
SELECT table_name FROM information_schema.tables
WHERE table_name IN ('metas_paciente', 'metas_acompanhamento');

CREATE TABLE IF NOT EXISTS evolucao_registros (
    id SERIAL PRIMARY KEY,
    paciente_id INT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    data_registro DATE NOT NULL DEFAULT CURRENT_DATE,
    peso_kg DECIMAL(5,2),
    percentual_gordura DECIMAL(4,1),
    cintura_cm DECIMAL(5,1),
    quadril_cm DECIMAL(5,1),
    abdomen_cm DECIMAL(5,1),
    braco_cm DECIMAL(5,1),
    coxa_cm DECIMAL(5,1),
    observacao TEXT,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS arquivos_paciente (
    id SERIAL PRIMARY KEY,
    paciente_id INT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    origem VARCHAR(20) NOT NULL CHECK (origem IN ('evolucao', 'exame')),
    origem_id INT NOT NULL,
    nome VARCHAR(255) NOT NULL,
    mime VARCHAR(100) NOT NULL,
    tamanho INT NOT NULL,
    dados BYTEA NOT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS exames_paciente (
    id SERIAL PRIMARY KEY,
    paciente_id INT REFERENCES pacientes(id) ON DELETE CASCADE,
    titulo_exame VARCHAR(100) NOT NULL,
    data_exame DATE NOT NULL,
    observacao TEXT,
    arquivo_url TEXT,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS observacoes_paciente (
    id SERIAL PRIMARY KEY,
    paciente_id INT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    tipo VARCHAR(10) NOT NULL CHECK (tipo IN ('semana', 'geral')),
    data_ref DATE NOT NULL DEFAULT CURRENT_DATE,
    texto TEXT NOT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX IF NOT EXISTS idx_obs_paciente ON observacoes_paciente (paciente_id, tipo, data_ref DESC);
CREATE INDEX IF NOT EXISTS idx_evolucao_paciente ON evolucao_registros (paciente_id, data_registro DESC);
CREATE INDEX IF NOT EXISTS idx_arquivos_origem ON arquivos_paciente (paciente_id, origem, origem_id);

SELECT table_name FROM information_schema.tables
WHERE table_name IN ('evolucao_registros', 'arquivos_paciente', 'exames_paciente');

-- =====================================================================
-- TABELAS MULTI-TENANT (base)
-- =====================================================================
CREATE TABLE IF NOT EXISTS perfis (
    chave VARCHAR(30) PRIMARY KEY,
    nome VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS modulos (
    chave VARCHAR(30) PRIMARY KEY,
    nome VARCHAR(50) NOT NULL
);

CREATE TABLE IF NOT EXISTS empresas (
    id SERIAL PRIMARY KEY,
    nome VARCHAR(150) NOT NULL,
    criado_em TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS empresa_modulos (
    empresa_id INT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    modulo VARCHAR(30) NOT NULL REFERENCES modulos(chave) ON DELETE CASCADE,
    PRIMARY KEY (empresa_id, modulo)
);

CREATE TABLE IF NOT EXISTS usuario_modulos (
    usuario_id INT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    modulo VARCHAR(30) NOT NULL REFERENCES modulos(chave) ON DELETE CASCADE,
    nivel VARCHAR(20) NOT NULL DEFAULT 'leitura'
        CHECK (nivel IN ('nenhum', 'leitura', 'edicao', 'total')),
    PRIMARY KEY (usuario_id, modulo)
);

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
CREATE INDEX IF NOT EXISTS idx_convites_token ON convites (token);
CREATE INDEX IF NOT EXISTS idx_convites_empresa ON convites (empresa_id);

-- =====================================================================
-- MIGRAÇÃO MULTI-TENANT: Adicionar empresa_id nas tabelas clínicas
-- =====================================================================
-- Este script é idempotente - pode ser executado múltiplas vezes
-- Fase 1: Adicionar coluna empresa_id + FK + Índices + Backfill
-- =====================================================================

DO $$
DECLARE
    t text;
    cols text[] := ARRAY[
        'planos_alimentares',
        'refeicoes',
        'itens_refeicao',
        'avaliacoes_fisicas',
        'checkins_semanais',
        'anamneses',
        'metas_paciente',
        'metas_acompanhamento',
        'evolucao_registros',
        'exames_paciente',
        'observacoes_paciente',
        'arquivos_paciente',
        'planos_versoes',
        'orientacoes_nutricionais'
    ];
BEGIN
    -- 1. Adicionar coluna empresa_id onde não existe
    FOREACH t IN ARRAY cols LOOP
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = t AND column_name = 'empresa_id'
        ) THEN
            EXECUTE format('ALTER TABLE %I ADD COLUMN empresa_id INT REFERENCES empresas(id)', t);
            RAISE NOTICE 'Coluna empresa_id adicionada em %', t;
        END IF;
    END LOOP;

    -- 2. Criar índices compostos para performance (empresa_id, paciente_id)
    -- Índices parciais: só cria se a tabela tiver paciente_id
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'planos_alimentares' AND column_name = 'paciente_id') THEN
        CREATE INDEX IF NOT EXISTS idx_planos_empresa_paciente ON planos_alimentares(empresa_id, paciente_id);
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'refeicoes' AND column_name = 'paciente_id') THEN
        CREATE INDEX IF NOT EXISTS idx_refeicoes_empresa_paciente ON refeicoes(empresa_id, paciente_id);
    ELSIF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'refeicoes' AND column_name = 'plano_id') THEN
        -- refeicoes não tem paciente_id direto, mas tem plano_id -> plano -> paciente
        CREATE INDEX IF NOT EXISTS idx_refeicoes_empresa ON refeicoes(empresa_id);
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'itens_refeicao' AND column_name = 'paciente_id') THEN
        CREATE INDEX IF NOT EXISTS idx_itens_empresa_paciente ON itens_refeicao(empresa_id, paciente_id);
    ELSIF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name = 'itens_refeicao' AND column_name = 'refeicao_id') THEN
        CREATE INDEX IF NOT EXISTS idx_itens_empresa ON itens_refeicao(empresa_id);
    END IF;
    CREATE INDEX IF NOT EXISTS idx_avaliacoes_empresa_paciente ON avaliacoes_fisicas(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_checkins_empresa_paciente ON checkins_semanais(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_anamneses_empresa_paciente ON anamneses(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_metas_empresa_paciente ON metas_paciente(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_metas_acomp_empresa ON metas_acompanhamento(empresa_id);
    CREATE INDEX IF NOT EXISTS idx_evolucao_empresa_paciente ON evolucao_registros(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_exames_empresa_paciente ON exames_paciente(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_obs_empresa_paciente ON observacoes_paciente(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_arquivos_empresa_paciente ON arquivos_paciente(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_versoes_empresa_paciente ON planos_versoes(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_orientacoes_empresa_paciente ON orientacoes_nutricionais(empresa_id, paciente_id);

    -- 3. BACKFILL: Popular empresa_id via JOIN com pacientes
    -- Cada tabela clínica pertence a um paciente, que já tem empresa_id

    -- planos_alimentares: JOIN direto com pacientes
    UPDATE planos_alimentares pa
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE pa.paciente_id = p.id AND pa.empresa_id IS NULL;

    -- refeicoes: JOIN via planos_alimentares -> pacientes
    UPDATE refeicoes r
    SET empresa_id = p.empresa_id
    FROM planos_alimentares pa
    JOIN pacientes p ON p.id = pa.paciente_id
    WHERE r.plano_id = pa.id AND r.empresa_id IS NULL;

    -- itens_refeicao: JOIN via refeicoes -> planos -> pacientes
    UPDATE itens_refeicao i
    SET empresa_id = p.empresa_id
    FROM refeicoes r
    JOIN planos_alimentares pa ON pa.id = r.plano_id
    JOIN pacientes p ON p.id = pa.paciente_id
    WHERE i.refeicao_id = r.id AND i.empresa_id IS NULL;

    -- avaliacoes_fisicas: JOIN direto com pacientes
    UPDATE avaliacoes_fisicas a
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE a.paciente_id = p.id AND a.empresa_id IS NULL;

    -- checkins_semanais: JOIN direto com pacientes
    UPDATE checkins_semanais c
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE c.paciente_id = p.id AND c.empresa_id IS NULL;

    -- anamneses: JOIN direto com pacientes
    UPDATE anamneses a
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE a.paciente_id = p.id AND a.empresa_id IS NULL;

    -- metas_paciente: JOIN direto com pacientes
    UPDATE metas_paciente m
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE m.paciente_id = p.id AND m.empresa_id IS NULL;

    -- metas_acompanhamento: JOIN via metas_paciente -> pacientes
    UPDATE metas_acompanhamento ma
    SET empresa_id = p.empresa_id
    FROM metas_paciente m
    JOIN pacientes p ON p.id = m.paciente_id
    WHERE ma.meta_id = m.id AND ma.empresa_id IS NULL;

    -- evolucao_registros: JOIN direto com pacientes
    UPDATE evolucao_registros e
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE e.paciente_id = p.id AND e.empresa_id IS NULL;

    -- exames_paciente: JOIN direto com pacientes
    UPDATE exames_paciente e
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE e.paciente_id = p.id AND e.empresa_id IS NULL;

    -- observacoes_paciente: JOIN direto com pacientes
    UPDATE observacoes_paciente o
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE o.paciente_id = p.id AND o.empresa_id IS NULL;

    -- arquivos_paciente: JOIN direto com pacientes
    UPDATE arquivos_paciente a
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE a.paciente_id = p.id AND a.empresa_id IS NULL;

    -- planos_versoes: JOIN direto com pacientes
    UPDATE planos_versoes v
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE v.paciente_id = p.id AND v.empresa_id IS NULL;

    -- orientacoes_nutricionais: JOIN direto com pacientes
    UPDATE orientacoes_nutricionais o
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE o.paciente_id = p.id AND o.empresa_id IS NULL;

    RAISE NOTICE 'Backfill concluído para todas as tabelas';

    -- 4. Tornar empresa_id NOT NULL após backfill (apenas onde preenchido)
    FOREACH t IN ARRAY cols LOOP
        EXECUTE format($f$
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM %I WHERE empresa_id IS NULL
                ) THEN
                    EXECUTE format('ALTER TABLE %I ALTER COLUMN empresa_id SET NOT NULL', '%I');
                    RAISE NOTICE 'Coluna empresa_id tornada NOT NULL em %', '%I';
                ELSE
                    RAISE NOTICE 'Ainda existem linhas NULL em %, pulando NOT NULL', '%I';
                END IF;
            END $$;
        $f$, t, t, t);
    END LOOP;
END $$;

-- =====================================================================
-- CONFERÊNCIA: Verificar se todas as tabelas têm empresa_id NOT NULL
-- =====================================================================
SELECT
    table_name,
    column_name,
    is_nullable,
    data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name = 'empresa_id'
  AND table_name IN (
    'planos_alimentares', 'refeicoes', 'itens_refeicao',
    'avaliacoes_fisicas', 'checkins_semanais', 'anamneses',
    'metas_paciente', 'metas_acompanhamento', 'evolucao_registros',
    'exames_paciente', 'observacoes_paciente', 'arquivos_paciente',
    'planos_versoes', 'orientacoes_nutricionais'
  )
ORDER BY table_name;

-- Verificar contagem de linhas com empresa_id NULL (deve ser 0)
SELECT 'planos_alimentares' AS tabela, COUNT(*) AS null_count FROM planos_alimentares WHERE empresa_id IS NULL
UNION ALL SELECT 'refeicoes', COUNT(*) FROM refeicoes WHERE empresa_id IS NULL
UNION ALL SELECT 'itens_refeicao', COUNT(*) FROM itens_refeicao WHERE empresa_id IS NULL
UNION ALL SELECT 'avaliacoes_fisicas', COUNT(*) FROM avaliacoes_fisicas WHERE empresa_id IS NULL
UNION ALL SELECT 'checkins_semanais', COUNT(*) FROM checkins_semanais WHERE empresa_id IS NULL
UNION ALL SELECT 'anamneses', COUNT(*) FROM anamneses WHERE empresa_id IS NULL
UNION ALL SELECT 'metas_paciente', COUNT(*) FROM metas_paciente WHERE empresa_id IS NULL
UNION ALL SELECT 'metas_acompanhamento', COUNT(*) FROM metas_acompanhamento WHERE empresa_id IS NULL
UNION ALL SELECT 'evolucao_registros', COUNT(*) FROM evolucao_registros WHERE empresa_id IS NULL
UNION ALL SELECT 'exames_paciente', COUNT(*) FROM exames_paciente WHERE empresa_id IS NULL
UNION ALL SELECT 'observacoes_paciente', COUNT(*) FROM observacoes_paciente WHERE empresa_id IS NULL
UNION ALL SELECT 'arquivos_paciente', COUNT(*) FROM arquivos_paciente WHERE empresa_id IS NULL
UNION ALL SELECT 'planos_versoes', COUNT(*) FROM planos_versoes WHERE empresa_id IS NULL
UNION ALL SELECT 'orientacoes_nutricionais', COUNT(*) FROM orientacoes_nutricionais WHERE empresa_id IS NULL;