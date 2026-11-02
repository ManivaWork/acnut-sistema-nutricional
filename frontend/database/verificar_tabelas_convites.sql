-- Verificação das tabelas de convites (Gaps 1 e 2 da auditoria)
-- Execute este script no banco smart_nutri_db para confirmar existência das tabelas
-- Não faz alterações, apenas SELECT

BEGIN;

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

-- Verificar colunas da tabela convites (se existir)
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'convites'
ORDER BY ordinal_position;

-- Verificar colunas da tabela convites_paciente (se existir)
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'convites_paciente'
ORDER BY ordinal_position;

-- Verificar colunas da tabela acessos_paciente_nutri (se existir)
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'public'
  AND table_name = 'acessos_paciente_nutri'
ORDER BY ordinal_position;

ROLLBACK; -- Apenas leitura, sem commit