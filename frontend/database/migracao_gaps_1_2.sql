-- =====================================================================
-- MIGRAÇÃO GAPS 1-2: Ajustes multi-tenant + tabelas convite-paciente
-- Idempotente, transacional, sem DROP/DELETE/TRUNCATE.
--
-- ATENÇÃO: o último comando é ROLLBACK (modo de teste).
-- Depois de conferir os resultados, troque por COMMIT; e rode de novo.
-- =====================================================================

BEGIN;

-- =====================================================================
-- GAP 1: Adicionar 'convites' à lista de tabelas com empresa_id NOT NULL
-- =====================================================================
DO $$
DECLARE
    t text;
    n bigint;
    -- Lista das 14 tabelas clínicas + convites (gap 1)
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
        'orientacoes_nutricionais',
        'convites'  -- GAP 1: adicionada aqui
    ];
BEGIN
    -- 1.a Adicionar coluna empresa_id onde não existe (FK -> empresas)
    FOREACH t IN ARRAY cols LOOP
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = t AND column_name = 'empresa_id'
        ) THEN
            EXECUTE format('ALTER TABLE %I ADD COLUMN empresa_id BIGINT REFERENCES empresas(id)', t);
            RAISE NOTICE 'Coluna empresa_id adicionada em %', t;
        END IF;
    END LOOP;

    -- 1.b Índices compostos (empresa_id, paciente_id) onde aplicável
    -- convites não tem paciente_id, só empresa_id
    CREATE INDEX IF NOT EXISTS idx_convites_empresa ON convites(empresa_id);

    -- 1.c BACKFILL: popular empresa_id via JOIN com pacientes (onde paciente_id existe)
    -- Para convites: backfill via empresas (já tem empresa_id na criação)
    -- Se houver linhas órfãs, ficam NULL e NOT NULL não será aplicado (aviso no final)

    -- 1.d NOT NULL somente onde não restou nenhuma linha com empresa_id NULL
    FOREACH t IN ARRAY cols LOOP
        EXECUTE format('SELECT count(*) FROM %I WHERE empresa_id IS NULL', t) INTO n;
        IF n = 0 THEN
            EXECUTE format('ALTER TABLE %I ALTER COLUMN empresa_id SET NOT NULL', t);
            RAISE NOTICE 'empresa_id NOT NULL aplicado em %', t;
        ELSE
            RAISE WARNING 'Tabela % ainda tem % linha(s) com empresa_id NULL; NOT NULL NÃO aplicado', t, n;
        END IF;
    END LOOP;
END $$;

-- =====================================================================
-- GAP 2: Criar tabelas convites_paciente + acessos_paciente_nutri
-- (se não existirem no schema_inicial.sql)
-- =====================================================================

-- 2.a Tabela convites_paciente
CREATE TABLE IF NOT EXISTS convites_paciente (
    id                  BIGSERIAL PRIMARY KEY,
    empresa_id          BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    nutricionista_id    BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    paciente_id         BIGINT REFERENCES pacientes(id) ON DELETE SET NULL,
    email_paciente      VARCHAR(255) NOT NULL,
    token               VARCHAR(64) NOT NULL UNIQUE,
    status              VARCHAR(20) NOT NULL DEFAULT 'pendente',
    expira_em           TIMESTAMPTZ NOT NULL,
    aceito_em           TIMESTAMPTZ,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Índices convites_paciente
CREATE INDEX IF NOT EXISTS idx_convites_pac_empresa_nutri
    ON convites_paciente (empresa_id, nutricionista_id);

CREATE INDEX IF NOT EXISTS idx_convites_pac_token_valido
    ON convites_paciente (token) WHERE status = 'pendente';

CREATE INDEX IF NOT EXISTS idx_convites_pac_email_ativo
    ON convites_paciente (empresa_id, email_paciente) WHERE status = 'pendente';

-- Constraint: um convite ativo por email por nutricionista
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint
        WHERE conname = 'uq_convite_pac_nutri_email_ativo'
    ) THEN
        ALTER TABLE convites_paciente
        ADD CONSTRAINT uq_convite_pac_nutri_email_ativo
        UNIQUE (nutricionista_id, email_paciente)
        DEFERRABLE INITIALLY IMMEDIATE;
    END IF;
END $$;

-- Comentários
COMMENT ON TABLE convites_paciente IS 'Convites de nutricionista para pacientes (expiram em 7 dias)';
COMMENT ON COLUMN convites_paciente.token IS 'Token único (UUID v4 sem hífens) enviado por e-mail';
COMMENT ON COLUMN convites_paciente.expira_em IS 'Expiração do convite (padrão 7 dias)';
COMMENT ON COLUMN convites_paciente.aceito_em IS 'Preenchido quando o convite é aceito; NULL = pendente';
COMMENT ON COLUMN convites_paciente.status IS 'pendente, aceito, expirado, cancelado';

-- 2.b Tabela acessos_paciente_nutri (vínculo nutricionista ↔ paciente)
CREATE TABLE IF NOT EXISTS acessos_paciente_nutri (
    id                  BIGSERIAL PRIMARY KEY,
    empresa_id          BIGINT NOT NULL REFERENCES empresas(id) ON DELETE CASCADE,
    nutricionista_id    BIGINT NOT NULL REFERENCES usuarios(id) ON DELETE CASCADE,
    paciente_id         BIGINT NOT NULL REFERENCES pacientes(id) ON DELETE CASCADE,
    criado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (nutricionista_id, paciente_id)
);

CREATE INDEX IF NOT EXISTS idx_acesso_pac_nutri_nutri
    ON acessos_paciente_nutri (nutricionista_id);

CREATE INDEX IF NOT EXISTS idx_acesso_pac_nutri_pac
    ON acessos_paciente_nutri (paciente_id);

COMMENT ON TABLE acessos_paciente_nutri IS 'Vínculo nutricionista ↔ paciente (nutricionista vê apenas seus pacientes)';
COMMENT ON COLUMN acessos_paciente_nutri.nutricionista_id IS 'Profissional que tem acesso ao paciente';
COMMENT ON COLUMN acessos_paciente_nutri.paciente_id IS 'Paciente vinculado ao nutricionista';

-- 2.c Adicionar empresa_id NOT NULL nas novas tabelas (já criadas com NOT NULL)
-- Índices já criados acima

-- =====================================================================
-- CONFERÊNCIA: Verificar estrutura final
-- =====================================================================

-- 1. Verificar se convites tem empresa_id NOT NULL
SELECT table_name, column_name, is_nullable, data_type
FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name = 'empresa_id'
  AND table_name = 'convites';

-- 2. Verificar tabelas novas existem
SELECT table_name FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('convites_paciente', 'acessos_paciente_nutri');

-- 3. Verificar constraints das novas tabelas
SELECT conname, contype, pg_get_constraintdef(oid) AS definition
FROM pg_constraint
WHERE conrelid IN ('convites_paciente'::regclass, 'acessos_paciente_nutri'::regclass)
ORDER BY conname;

-- 4. Verificar índices criados
SELECT indexname, tablename
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename IN ('convites', 'convites_paciente', 'acessos_paciente_nutri')
  AND indexname LIKE 'idx_%'
ORDER BY tablename, indexname;

-- >>> MODO TESTE: nada é gravado. Troque ROLLBACK por COMMIT; para aplicar de verdade. <<<
ROLLBACK;