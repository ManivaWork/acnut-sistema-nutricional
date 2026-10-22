-- =====================================================================
-- MIGRAÇÃO MULTI-TENANT: empresa_id nas tabelas clínicas (VERSÃO CORRIGIDA)
-- Idempotente, transacional, sem DROP/DELETE/TRUNCATE.
--
-- ATENÇÃO: o último comando deste arquivo é ROLLBACK (modo de teste).
-- Depois de conferir os resultados, troque o ROLLBACK final por COMMIT
-- e rode o script de novo para gravar de verdade.
-- =====================================================================

BEGIN;

DO $$
DECLARE
    t text;
    n bigint;
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
    -- 1.a Adicionar coluna empresa_id onde não existe (FK -> empresas)
    FOREACH t IN ARRAY cols LOOP
        IF NOT EXISTS (
            SELECT 1 FROM information_schema.columns
            WHERE table_schema = 'public' AND table_name = t AND column_name = 'empresa_id'
        ) THEN
            EXECUTE format('ALTER TABLE %I ADD COLUMN empresa_id INT REFERENCES empresas(id)', t);
            RAISE NOTICE 'Coluna empresa_id adicionada em %', t;
        END IF;
    END LOOP;

    -- 1.b Índices
    CREATE INDEX IF NOT EXISTS idx_planos_empresa_paciente ON planos_alimentares(empresa_id, paciente_id);
    CREATE INDEX IF NOT EXISTS idx_refeicoes_empresa ON refeicoes(empresa_id);
    CREATE INDEX IF NOT EXISTS idx_itens_empresa ON itens_refeicao(empresa_id);
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

    -- 1.c BACKFILL (pai -> filho), só onde empresa_id IS NULL

    UPDATE planos_alimentares pa
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE pa.paciente_id = p.id AND pa.empresa_id IS NULL;

    UPDATE refeicoes r
    SET empresa_id = p.empresa_id
    FROM planos_alimentares pa
    JOIN pacientes p ON p.id = pa.paciente_id
    WHERE r.plano_id = pa.id AND r.empresa_id IS NULL;

    UPDATE itens_refeicao i
    SET empresa_id = p.empresa_id
    FROM refeicoes r
    JOIN planos_alimentares pa ON pa.id = r.plano_id
    JOIN pacientes p ON p.id = pa.paciente_id
    WHERE i.refeicao_id = r.id AND i.empresa_id IS NULL;

    UPDATE avaliacoes_fisicas a
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE a.paciente_id = p.id AND a.empresa_id IS NULL;

    UPDATE checkins_semanais c
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE c.paciente_id = p.id AND c.empresa_id IS NULL;

    UPDATE anamneses a
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE a.paciente_id = p.id AND a.empresa_id IS NULL;

    UPDATE metas_paciente m
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE m.paciente_id = p.id AND m.empresa_id IS NULL;

    UPDATE metas_acompanhamento ma
    SET empresa_id = p.empresa_id
    FROM metas_paciente m
    JOIN pacientes p ON p.id = m.paciente_id
    WHERE ma.meta_id = m.id AND ma.empresa_id IS NULL;

    UPDATE evolucao_registros e
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE e.paciente_id = p.id AND e.empresa_id IS NULL;

    UPDATE exames_paciente e
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE e.paciente_id = p.id AND e.empresa_id IS NULL;

    UPDATE observacoes_paciente o
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE o.paciente_id = p.id AND o.empresa_id IS NULL;

    UPDATE arquivos_paciente a
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE a.paciente_id = p.id AND a.empresa_id IS NULL;

    UPDATE planos_versoes v
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE v.paciente_id = p.id AND v.empresa_id IS NULL;

    UPDATE orientacoes_nutricionais o
    SET empresa_id = p.empresa_id
    FROM pacientes p
    WHERE o.paciente_id = p.id AND o.empresa_id IS NULL;

    RAISE NOTICE 'Backfill concluido para todas as tabelas';

    -- 1.d NOT NULL somente onde nao restou nenhuma linha com empresa_id NULL
    -- (CORRIGIDO: sem DO aninhado; usa contagem + EXECUTE direto)
    FOREACH t IN ARRAY cols LOOP
        EXECUTE format('SELECT count(*) FROM %I WHERE empresa_id IS NULL', t) INTO n;
        IF n = 0 THEN
            EXECUTE format('ALTER TABLE %I ALTER COLUMN empresa_id SET NOT NULL', t);
            RAISE NOTICE 'empresa_id NOT NULL aplicado em %', t;
        ELSE
            RAISE WARNING 'Tabela % ainda tem % linha(s) com empresa_id NULL; NOT NULL NAO aplicado', t, n;
        END IF;
    END LOOP;
END $$;

-- ---------------------------------------------------------------------
-- 2. CONFERENCIA: estrutura (esperado: 14 linhas, is_nullable = NO)
-- ---------------------------------------------------------------------
SELECT table_name, column_name, is_nullable, data_type
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

-- ---------------------------------------------------------------------
-- 3. CONFERENCIA: linhas com empresa_id NULL (esperado: 0 em todas)
-- ---------------------------------------------------------------------
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

-- ---------------------------------------------------------------------
-- 4. CONFERENCIA: indices criados
-- ---------------------------------------------------------------------
SELECT indexname, tablename
FROM pg_indexes
WHERE schemaname = 'public'
  AND indexname LIKE 'idx_%empresa%'
ORDER BY tablename, indexname;

-- ---------------------------------------------------------------------
-- 5. CONFERENCIA: a tabela de convites existe? (vazio = NAO existe)
-- ---------------------------------------------------------------------
SELECT to_regclass('public.convites') AS tabela_convites;

-- >>> MODO TESTE: nada e gravado. Troque por COMMIT; para aplicar de verdade. <<<
ROLLBACK;