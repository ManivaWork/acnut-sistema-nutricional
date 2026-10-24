-- =====================================================================
-- MIGRAÇÃO MEDIDAS CASEIRAS: alimentos_medidas_caseiras + dados iniciais
-- Idempotente, transacional, sem DROP/DELETE/TRUNCATE.
--
-- ATENÇÃO: o último comando é ROLLBACK (modo de teste).
-- Depois de conferir os resultados, troque ROLLBACK por COMMIT
-- e rode o script de novo para gravar de verdade.
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. TABELA DE MEDIDAS CASEIRAS POR ALIMENTO
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS alimentos_medidas_caseiras (
    id                    BIGSERIAL PRIMARY KEY,
    alimento_id           BIGINT NOT NULL REFERENCES alimentos(id) ON DELETE CASCADE,
    medida                VARCHAR(100) NOT NULL,           -- ex: 'colher de sopa', 'xícara', 'fatia', 'unidade média', 'copo'
    gramas_por_unidade    NUMERIC(10,2) NOT NULL,         -- ex: 15.00, 200.00, 30.00, 120.00
    observacao            VARCHAR(255),                    -- ex: 'rasa', 'cheia', '200ml'
    created_at            TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (alimento_id, medida, observacao)
);

COMMENT ON TABLE alimentos_medidas_caseiras IS 'Medidas caseiras por alimento para conversão em gramas';
COMMENT ON COLUMN alimentos_medidas_caseiras.medida IS 'Nome da medida (colher de sopa, xícara, fatia, unidade, copo, etc.)';
COMMENT ON COLUMN alimentos_medidas_caseiras.gramas_por_unidade IS 'Peso em gramas de 1 unidade desta medida';
COMMENT ON COLUMN alimentos_medidas_caseiras.observacao IS 'Detalhe opcional (rasa, cheia, 200ml, média, grande)';

-- ---------------------------------------------------------------------
-- 2. ÍNDICES
-- ---------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_alimentos_medidas_alimento
    ON alimentos_medidas_caseiras (alimento_id);

-- ---------------------------------------------------------------------
-- 3. POPULAÇÃO INICIAL (idempotente via ON CONFLICT DO NOTHING)
-- ---------------------------------------------------------------------
-- Alimentos do banco local (ids 1-7) + TACO (ids 175, 341, 458, 591)
INSERT INTO alimentos_medidas_caseiras (alimento_id, medida, gramas_por_unidade, observacao) VALUES
-- Peito de Frango Grelhado (id=1)
(1, 'fatia media', 50.00, ''),
(1, 'fatia grossa', 80.00, ''),
(1, 'unidade (peito pequeno)', 120.00, ''),
(1, 'xicara (desfiado)', 140.00, ''),
-- Arroz Integral Cozido (id=2)
(2, 'colher de sopa', 25.00, 'rasa'),
(2, 'colher de sopa', 35.00, 'cheia'),
(2, 'xicara', 160.00, 'cozido'),
(2, 'prato raso', 200.00, 'porcao media'),
-- Ovo Cozido (id=3)
(3, 'unidade pequena', 45.00, ''),
(3, 'unidade media', 55.00, ''),
(3, 'unidade grande', 65.00, ''),
-- Aveia em Flocos (id=4)
(4, 'colher de sopa', 10.00, 'rasa'),
(4, 'colher de sopa', 15.00, 'cheia'),
(4, 'xicara', 90.00, ''),
-- Banana Prata (id=5)
(5, 'unidade pequena', 80.00, ''),
(5, 'unidade media', 100.00, ''),
(5, 'unidade grande', 130.00, ''),
-- Azeite de Oliva Extra Virgem (id=6)
(6, 'colher de sopa', 13.50, ''),
(6, 'colher de cha', 4.50, ''),
(6, 'fio', 5.00, 'aproximado'),
-- Feijao Preto Cozido (id=7)
(7, 'colher de sopa', 30.00, 'rasa'),
(7, 'colher de sopa', 45.00, 'cheia'),
(7, 'concha', 120.00, ''),
(7, 'xicara', 180.00, 'cozido'),
-- TACO: Banana, da terra, crua (id=175)
(175, 'unidade pequena', 80.00, ''),
(175, 'unidade media', 120.00, ''),
(175, 'unidade grande', 150.00, ''),
-- TACO: Carne, bovina, contra-file de costela, cru (id=341)
(341, 'fatia fina', 30.00, ''),
(341, 'fatia media', 50.00, ''),
(341, 'bife medio', 120.00, ''),
(341, 'bife grande', 180.00, ''),
-- TACO: Leite, de vaca, integral (id=458)
(458, 'copo', 200.00, '200ml'),
(458, 'xicara', 240.00, '240ml'),
(458, 'colher de sopa', 15.00, ''),
-- TACO: Coco, verde, cru (id=591)
(591, 'colher de sopa', 15.00, 'rasa'),
(591, 'colher de sopa', 20.00, 'cheia'),
(591, 'xicara', 80.00, 'ralado'),
(591, 'unidade (agua)', 300.00, 'aproximado')
ON CONFLICT (alimento_id, medida, observacao) DO NOTHING;

-- ---------------------------------------------------------------------
-- 4. VERIFICAÇÃO
-- ---------------------------------------------------------------------
SELECT 'alimentos_medidas_caseiras' AS tabela, COUNT(*) AS linhas FROM alimentos_medidas_caseiras;

-- Amostra dos dados inseridos
SELECT amc.alimento_id, a.nome AS alimento, amc.medida, amc.gramas_por_unidade, amc.observacao
FROM alimentos_medidas_caseiras amc
JOIN alimentos a ON a.id = amc.alimento_id
ORDER BY amc.alimento_id, amc.medida, amc.observacao
LIMIT 20;

-- >>> MODO TESTE: nada é gravado. Troque por COMMIT; para aplicar de verdade. <<<
ROLLBACK;