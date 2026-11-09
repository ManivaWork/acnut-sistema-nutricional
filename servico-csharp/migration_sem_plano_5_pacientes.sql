-- Migration: Atualizar limites de sem_plano de 3 para 5 pacientes
-- Idempotente: só altera assinaturas sem_plano que ainda têm limite 3
-- Executa em transação e faz ROLLBACK no final (para revisão)

BEGIN;

-- 1. Verificar assinaturas sem_plano atuais
SELECT 'Antes da atualização:' as etapa, empresa_id, plano, status, limites
FROM assinaturas
WHERE status = 'sem_plano'
ORDER BY id;

-- 2. Atualizar limites de max_pacientes de 3 para 5 nas assinaturas sem_plano
-- Só atualiza se o limite atual for 3 (idempotente)
UPDATE assinaturas
SET limites = jsonb_set(
    COALESCE(limites, '{}'::jsonb),
    '{max_pacientes}',
    '5'::jsonb
)
WHERE status = 'sem_plano'
  AND (limites IS NULL OR (limites->>'max_pacientes')::int = 3);

-- 3. Verificar resultado
SELECT 'Após atualização:' as etapa, empresa_id, plano, status, limites
FROM assinaturas
WHERE status = 'sem_plano'
ORDER BY id;

-- 4. Contar quantas linhas foram alteradas
DO $$
DECLARE
    linhas_afetadas integer;
BEGIN
    GET DIAGNOSTICS linhas_afetadas = ROW_COUNT;
    RAISE NOTICE 'Assinaturas sem_plano atualizadas de 3 para 5 pacientes: %', linhas_afetadas;
END $$;

-- ROLLBACK para revisão (remova esta linha e use COMMIT para aplicar)
ROLLBACK;