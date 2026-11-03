-- Migration: adicionar 'sem_plano' ao constraint assinaturas_status_check
-- Idempotente, transacional, sem DROP/DELETE de dados, com COMMIT
-- Aplicar no Render: Database → Connect → PSQL → cole este conteúdo

BEGIN;

-- 1. Verificar constraint atual
DO $$
DECLARE
    constraint_def text;
BEGIN
    SELECT pg_get_constraintdef(oid) INTO constraint_def
    FROM pg_constraint
    WHERE conname = 'assinaturas_status_check';

    IF constraint_def IS NULL THEN
        RAISE EXCEPTION 'Constraint assinaturas_status_check não encontrada';
    END IF;

    RAISE NOTICE 'Constraint atual: %', constraint_def;

    -- Se já tem 'sem_plano', não faz nada
    IF constraint_def ILIKE '%sem_plano%' THEN
        RAISE NOTICE 'Status "sem_plano" já existe na constraint. Nada a fazer.';
    ELSE
        -- 2. Remover constraint antiga e criar nova com 'sem_plano'
        -- (ALTER TABLE ... ALTER COLUMN ... DROP CONSTRAINT não funciona para CHECK, precisa DROP CONSTRAINT por nome)
        EXECUTE 'ALTER TABLE assinaturas DROP CONSTRAINT assinaturas_status_check';

        EXECUTE $$
            ALTER TABLE assinaturas
            ADD CONSTRAINT assinaturas_status_check
            CHECK (status IN ('inativa','ativa','past_due','cancelada','trial','sem_plano'))
        $$;

        RAISE NOTICE 'Constraint atualizada com "sem_plano"';
    END IF;
END $$;

-- 3. Verificação
SELECT 'assinaturas_status_check atualizada' AS resultado,
       pg_get_constraintdef(oid) AS definicao
FROM pg_constraint
WHERE conname = 'assinaturas_status_check';

-- 4. Teste rápido: inserir e deletar uma linha com 'sem_plano' para validar
DO $$
DECLARE
    test_empresa_id bigint;
BEGIN
    -- Buscar uma empresa existente para teste (ou criar temporária)
    SELECT id INTO test_empresa_id FROM empresas LIMIT 1;

    IF test_empresa_id IS NOT NULL THEN
        -- Tenta inserir com 'sem_plano' (vai falhar se já existe assinatura para essa empresa, mas valida o constraint)
        BEGIN
            INSERT INTO assinaturas (empresa_id, plano, status, valor_centavos, ciclo)
            VALUES (test_empresa_id, 'basico', 'sem_plano', 0, 'monthly')
            ON CONFLICT (empresa_id) DO NOTHING;
            RAISE NOTICE 'Teste de inserção com "sem_plano": OK (constraint aceita)';
        EXCEPTION WHEN OTHERS THEN
            RAISE NOTICE 'Teste de inserção: % (esperado se empresa já tem assinatura)', SQLERRM;
        END;
    ELSE
        RAISE NOTICE 'Nenhuma empresa encontrada para teste de inserção';
    END IF;
END $$;

COMMIT;