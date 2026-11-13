-- =====================================================================
-- VERIFICAÇÃO COMPLETA DO BANCO - ACNUTRI
-- Execute este script ANTES de qualquer migração para ver o estado atual
-- Não faz alterações, apenas SELECTs
-- =====================================================================

BEGIN;

-- 1. LISTAR TODAS AS TABELAS DO SCHEMA PUBLIC
SELECT '=== TABELAS EXISTENTES ===' AS secao;
SELECT schemaname, tablename, tableowner
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY tablename;

-- 2. VERIFICAR TABELAS DE CONVITES (GAPS 1 E 2)
SELECT '=== VERIFICAÇÃO GAPS 1 E 2 ===' AS secao;
SELECT
    table_name,
    CASE
        WHEN table_name IS NOT NULL THEN 'EXISTE ✅'
        ELSE 'NÃO EXISTE ❌'
    END as status
FROM information_schema.tables
WHERE table_schema = 'public'
  AND table_name IN ('convites', 'convites_paciente', 'acessos_paciente_nutri')
ORDER BY table_name;

-- 3. ESTRUTURA DA TABELA CONVITES
SELECT '=== ESTRUTURA CONVITES ===' AS secao;
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'convites'
ORDER BY ordinal_position;

-- 4. ESTRUTURA CONVITES_PACIENTE
SELECT '=== ESTRUTURA CONVITES_PACIENTE ===' AS secao;
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'convites_paciente'
ORDER BY ordinal_position;

-- 5. ESTRUTURA ACESSOS_PACIENTE_NUTRI
SELECT '=== ESTRUTURA ACESSOS_PACIENTE_NUTRI ===' AS secao;
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'acessos_paciente_nutri'
ORDER BY ordinal_position;

-- 6. VERIFICAR EMPRESA_ID NOT NULL NAS 14 TABELAS CLÍNICAS + CONVITES
SELECT '=== EMPRESA_ID NOT NULL (14 tabelas + convites) ===' AS secao;
SELECT
    table_name,
    column_name,
    is_nullable,
    CASE WHEN is_nullable = 'NO' THEN 'NOT NULL ✅' ELSE 'NULL ⚠️' END as status
FROM information_schema.columns
WHERE table_schema = 'public'
  AND column_name = 'empresa_id'
  AND table_name IN (
    'planos_alimentares', 'refeicoes', 'itens_refeicao',
    'avaliacoes_fisicas', 'checkins_semanais', 'anamneses',
    'metas_paciente', 'metas_acompanhamento', 'evolucao_registros',
    'exames_paciente', 'observacoes_paciente', 'arquivos_paciente',
    'planos_versoes', 'orientacoes_nutricionais',
    'convites'
  )
ORDER BY table_name;

-- 7. CONTAGEM DE LINHAS COM EMPRESA_ID NULL (DEVE SER 0)
SELECT '=== LINHAS COM EMPRESA_ID NULL (DEVE SER 0) ===' AS secao;
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
UNION ALL SELECT 'orientacoes_nutricionais', COUNT(*) FROM orientacoes_nutricionais WHERE empresa_id IS NULL
UNION ALL SELECT 'convites', COUNT(*) FROM convites WHERE empresa_id IS NULL
UNION ALL SELECT 'convites_paciente', COUNT(*) FROM convites_paciente WHERE empresa_id IS NULL
UNION ALL SELECT 'acessos_paciente_nutri', COUNT(*) FROM acessos_paciente_nutri WHERE empresa_id IS NULL;

-- 8. CONSTRAINTS DAS TABELAS DE CONVITE
SELECT '=== CONSTRAINTS CONVITES_PACIENTE ===' AS secao;
SELECT conname, contype, pg_get_constraintdef(oid) AS definition
FROM pg_constraint
WHERE conrelid = 'convites_paciente'::regclass
ORDER BY conname;

SELECT '=== CONSTRAINTS ACESSOS_PACIENTE_NUTRI ===' AS secao;
SELECT conname, contype, pg_get_constraintdef(oid) AS definition
FROM pg_constraint
WHERE conrelid = 'acessos_paciente_nutri'::regclass
ORDER BY conname;

-- 9. ÍNDICES CRIADOS
SELECT '=== ÍNDICES DAS TABELAS DE CONVITE ===' AS secao;
SELECT indexname, tablename, indexdef
FROM pg_indexes
WHERE schemaname = 'public'
  AND tablename IN ('convites', 'convites_paciente', 'acessos_paciente_nutri')
  AND indexname LIKE 'idx_%'
ORDER BY tablename, indexname;

-- 10. CONTAGEM GERAL DE LINHAS
SELECT '=== CONTAGEM DE LINHAS ===' AS secao;
SELECT 'perfis' AS tabela, COUNT(*) FROM perfis
UNION ALL SELECT 'modulos', COUNT(*) FROM modulos
UNION ALL SELECT 'empresas', COUNT(*) FROM empresas
UNION ALL SELECT 'empresa_modulos', COUNT(*) FROM empresa_modulos
UNION ALL SELECT 'usuarios', COUNT(*) FROM usuarios
UNION ALL SELECT 'pacientes', COUNT(*) FROM pacientes
UNION ALL SELECT 'alimentos', COUNT(*) FROM alimentos
UNION ALL SELECT 'convites', COUNT(*) FROM convites
UNION ALL SELECT 'convites_paciente', COUNT(*) FROM convites_paciente
UNION ALL SELECT 'acessos_paciente_nutri', COUNT(*) FROM acessos_paciente_nutri
UNION ALL SELECT 'avaliacoes_fisicas', COUNT(*) FROM avaliacoes_fisicas
UNION ALL SELECT 'anamneses', COUNT(*) FROM anamneses
UNION ALL SELECT 'planos_alimentares', COUNT(*) FROM planos_alimentares
UNION ALL SELECT 'refeicoes', COUNT(*) FROM refeicoes
UNION ALL SELECT 'itens_refeicao', COUNT(*) FROM itens_refeicao
UNION ALL SELECT 'checkins_semanais', COUNT(*) FROM checkins_semanais
UNION ALL SELECT 'metas_paciente', COUNT(*) FROM metas_paciente
UNION ALL SELECT 'evolucao_registros', COUNT(*) FROM evolucao_registros
UNION ALL SELECT 'exames_paciente', COUNT(*) FROM exames_paciente
UNION ALL SELECT 'observacoes_paciente', COUNT(*) FROM observacoes_paciente
UNION ALL SELECT 'arquivos_paciente', COUNT(*) FROM arquivos_paciente
UNION ALL SELECT 'planos_versoes', COUNT(*) FROM planos_versoes
UNION ALL SELECT 'orientacoes_nutricionais', COUNT(*) FROM orientacoes_nutricionais;

-- >>> APENAS LEITURA: ROLLBACK não faz nada, só encerra a transação <<<
ROLLBACK;