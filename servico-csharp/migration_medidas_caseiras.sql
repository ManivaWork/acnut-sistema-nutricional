-- Migration: tabela de medidas caseiras por alimento
-- Idempotente, roda em transação, sem DROP/DELETE, termina em ROLLBACK
-- Aplicar: psql -d smart_nutri_db -f servico-csharp/migration_medidas_caseiras.sql

BEGIN;

-- 1. Tabela de medidas caseiras
CREATE TABLE IF NOT EXISTS alimentos_medidas_caseiras (
    id              BIGSERIAL PRIMARY KEY,
    alimento_id     BIGINT NOT NULL REFERENCES alimentos(id) ON DELETE CASCADE,
    medida          VARCHAR(100) NOT NULL,           -- ex: 'colher de sopa', 'xícara', 'fatia', 'unidade média', 'copo'
    gramas_por_unidade NUMERIC(10,2) NOT NULL,      -- ex: 15.00, 200.00, 30.00, 120.00
    observacao      VARCHAR(255),                    -- ex: 'rasa', 'cheia', '200ml'
    created_at      TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (alimento_id, medida, observacao)
);

-- 2. Índices
CREATE INDEX IF NOT EXISTS idx_alimentos_medidas_alimento
    ON alimentos_medidas_caseiras (alimento_id);

-- 3. Comentários
COMMENT ON TABLE alimentos_medidas_caseiras IS 'Medidas caseiras por alimento para conversão em gramas';
COMMENT ON COLUMN alimentos_medidas_caseiras.medida IS 'Nome da medida (colher de sopa, xícara, fatia, unidade, copo, etc.)';
COMMENT ON COLUMN alimentos_medidas_caseiras.gramas_por_unidade IS 'Peso em gramas de 1 unidade desta medida';
COMMENT ON COLUMN alimentos_medidas_caseiras.observacao IS 'Detalhe opcional (rasa, cheia, 200ml, média, grande)';

-- 4. Exemplos iniciais (opcional - descomente para popular)
-- INSERT INTO alimentos_medidas_caseiras (alimento_id, medida, gramas_por_unidade, observacao) VALUES
-- (175, 'unidade média', 120.00, ''),           -- Banana
-- (175, 'unidade pequena', 80.00, ''),
-- (175, 'unidade grande', 150.00, ''),
-- (341, 'fatia fina', 30.00, ''),                -- Carne bovina
-- (341, 'fatia média', 50.00, ''),
-- (341, 'bife médio', 120.00, ''),
-- (458, 'copo', 200.00, '200ml'),                -- Leite integral
-- (458, 'xícara', 240.00, '240ml'),
-- (591, 'colher de sopa', 15.00, ''),            -- Coco ralado
-- (591, 'xícara', 80.00, 'ralado');

-- 5. Verificação
SELECT 'alimentos_medidas_caseiras' AS tabela, COUNT(*) AS linhas FROM alimentos_medidas_caseiras;

ROLLBACK;  -- remova esta linha e execute novamente para aplicar de verdade