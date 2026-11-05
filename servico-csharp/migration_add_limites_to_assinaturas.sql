-- =====================================================================
-- MIGRAÇÃO: Adicionar coluna 'limites' (JSONB) na tabela assinaturas
-- Idempotente, transacional, sem DROP/DELETE de dados
-- Aplicar no Render: Database → Connect → PSQL → cole este conteúdo
-- =====================================================================

-- 1. Adicionar coluna limites se não existir
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public' AND table_name = 'assinaturas' AND column_name = 'limites'
    ) THEN
        ALTER TABLE assinaturas ADD COLUMN limites JSONB;
        RAISE NOTICE 'Coluna limites adicionada em assinaturas';
    ELSE
        RAISE NOTICE 'Coluna limites já existe em assinaturas';
    END IF;
END $$;

-- 2. Backfill: popular limites para linhas existentes baseando-se no plano
-- Para assinaturas com status 'sem_plano', usar limites restritivos (max 3 pacientes)
-- Para outras, usar os limites do catálogo conforme o plano
DO $$
DECLARE
    lim_basico jsonb := '{"max_pacientes": 50, "max_profissionais": 2, "storage_mb": 500, "max_versoes_plano": 5}';
    lim_profissional jsonb := '{"max_pacientes": 200, "max_profissionais": 10, "storage_mb": 2000, "max_versoes_plano": 20}';
    lim_enterprise jsonb := '{"max_pacientes": 1000, "max_profissionais": 50, "storage_mb": 10000, "max_versoes_plano": 100}';
    lim_sem_plano jsonb := '{"max_pacientes": 3, "max_profissionais": 1, "storage_mb": 50, "max_versoes_plano": 1}';
    linhas integer;
BEGIN
    -- Atualizar linhas onde limites IS NULL
    UPDATE assinaturas
    SET limites = CASE
        WHEN status = 'sem_plano' THEN lim_sem_plano
        WHEN plano = 'basico' THEN lim_basico
        WHEN plano = 'profissional' THEN lim_profissional
        WHEN plano = 'enterprise' THEN lim_enterprise
        ELSE lim_basico
    END
    WHERE limites IS NULL;

    GET DIAGNOSTICS linhas = ROW_COUNT;
    RAISE NOTICE 'Backfill de limites concluído: % linhas atualizadas', linhas;
END $$;

-- 3. Verificação rápida
SELECT
    'assinaturas' AS tabela,
    COUNT(*) AS total_linhas,
    COUNT(limites) AS com_limites,
    COUNT(*) - COUNT(limites) AS sem_limites
FROM assinaturas;

SELECT
    plano,
    status,
    limites
FROM assinaturas
ORDER BY id;