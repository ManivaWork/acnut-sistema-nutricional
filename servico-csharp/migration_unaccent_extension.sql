-- Habilitar extensão unaccent para busca sem acentos (ILIKE unaccent)
-- Idempotente, roda em transação, sem DROP/DELETE, termina em ROLLBACK
-- Aplicar: psql -d achubdb -f servico-csharp/migration_unaccent_extension.sql
-- Após revisar, remova a linha final "ROLLBACK;" e execute novamente

BEGIN;

-- Criar extensão unaccent se não existir
CREATE EXTENSION IF NOT EXISTS unaccent;

-- Verificação
SELECT extname, extversion FROM pg_extension WHERE extname = 'unaccent';

COMMIT;