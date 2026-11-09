-- Migration: Adicionar coluna bypass_limites na tabela assinaturas
-- Idempotente: só adiciona se não existir
-- Executa em transação e faz ROLLBACK no final (para revisão)

BEGIN;

-- 1. Verificar estrutura atual
SELECT 'Antes da migração:' as etapa, column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'assinaturas' AND column_name = 'bypass_limites';

-- 2. Adicionar coluna bypass_limites (boolean, default false, not null)
ALTER TABLE assinaturas
ADD COLUMN IF NOT EXISTS bypass_limites boolean NOT NULL DEFAULT false;

-- 3. Verificar resultado
SELECT 'Após migração:' as etapa, column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_name = 'assinaturas' AND column_name = 'bypass_limites';

-- 4. Comentário na coluna
COMMENT ON COLUMN assinaturas.bypass_limites IS 'Se true, ignora limites do plano (acesso ilimitado). Apenas Super Admin pode alterar.';

-- ROLLBACK para revisão (remova esta linha e use COMMIT para aplicar)
ROLLBACK;