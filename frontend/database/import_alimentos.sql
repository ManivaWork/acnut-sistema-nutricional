-- =====================================================================
-- IMPORTAÇÃO TACO → TABELA alimentos (UPSERT idempotente)
-- Gerado automaticamente - 546 alimentos
-- =====================================================================

BEGIN;

-- Garante que a tabela existe

CREATE TABLE IF NOT EXISTS alimentos (
    id BIGSERIAL PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    categoria VARCHAR(100),
    calorias_100g NUMERIC(10,2),
    proteinas_100g NUMERIC(10,2),
    carboidratos_100g NUMERIC(10,2),
    gorduras_100g NUMERIC(10,2),
    fibras_100g NUMERIC(10,2) DEFAULT 0
);


INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (32, 'Farinha, de centeio, integral', 'Grupo 13', 336.0, 12.5, 73.3, 1.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (33, 'Farinha, de milho, amarela', 'Grupo 13', 351.0, 7.2, 79.1, 1.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (34, 'Farinha, de rosca', 'Grupo 13', 371.0, 11.4, 75.8, 1.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (35, 'Farinha, de trigo', 'Grupo 13', 360.0, 9.8, 75.1, 1.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (36, 'Farinha, láctea, de cereais', 'Grupo 13', 415.0, 11.9, 77.8, 5.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (37, 'Lasanha, massa fresca, cozida', 'Grupo 13', 164.0, 5.8, 32.5, 1.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (38, 'Lasanha, massa fresca, crua', 'Grupo 13', 220.0, 7.0, 45.1, 1.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (39, 'Macarrão, instantâneo', 'Grupo 13', 436.0, 8.8, 62.4, 17.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (40, 'Macarrão, trigo, cru', 'Grupo 13', 371.0, 10.0, 77.9, 1.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (41, 'Macarrão, trigo, cru, com ovos', 'Grupo 13', 371.0, 10.3, 76.6, 2.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (42, 'Milho, amido, cru', 'Grupo 13', 361.0, 0.6, 87.1, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (43, 'Milho, fubá, cru', 'Grupo 13', 353.0, 7.2, 78.9, 1.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (44, 'Milho, verde, cru', 'Grupo 13', 138.0, 6.6, 28.6, 0.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (45, 'Milho, verde, enlatado, drenado', 'Grupo 13', 98.0, 3.2, 17.1, 2.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (46, 'Mingau tradicional, pó', 'Grupo 13', 373.0, 0.6, 89.3, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (47, 'Pamonha, barra para cozimento, pré-cozida', 'Grupo 13', 171.0, 2.6, 30.7, 4.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (48, 'Pão, aveia, forma', 'Grupo 13', 343.0, 12.4, 59.6, 5.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (49, 'Pão, de soja', 'Grupo 13', 309.0, 11.3, 56.5, 3.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (50, 'Pão, glúten, forma', 'Grupo 13', 253.0, 12.0, 44.1, 2.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (51, 'Pão, milho, forma', 'Grupo 13', 292.0, 8.3, 56.4, 3.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (52, 'Pão, trigo, forma, integral', 'Grupo 13', 253.0, 9.4, 49.9, 3.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (53, 'Pão, trigo, francês', 'Grupo 13', 300.0, 8.0, 58.6, 3.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (54, 'Pão, trigo, sovado', 'Grupo 13', 311.0, 8.4, 61.5, 2.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (55, 'Pastel, de carne, cru', 'Grupo 13', 289.0, 10.7, 42.0, 8.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (56, 'Pastel, de carne, frito', 'Grupo 13', 388.0, 10.1, 43.8, 20.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (57, 'Pastel, de queijo, cru', 'Grupo 13', 308.0, 9.9, 45.9, 9.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (58, 'Pastel, de queijo, frito', 'Grupo 13', 422.0, 8.7, 48.1, 22.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (59, 'Pastel, massa, crua', 'Grupo 13', 310.0, 6.9, 57.4, 5.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (60, 'Pastel, massa, frita', 'Grupo 13', 570.0, 6.0, 49.3, 40.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (61, 'Pipoca, com óleo de soja, sem sal', 'Grupo 13', 448.0, 9.9, 70.3, 15.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (62, 'Polenta, pré-cozida', 'Grupo 13', 103.0, 2.3, 23.3, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (63, 'Torrada, pão francês', 'Grupo 13', 377.0, 10.5, 74.6, 3.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (64, 'Abóbora, cabotian, cozida', 'Grupo 15', 48.0, 1.4, 10.8, 0.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (65, 'Abóbora, cabotian, crua', 'Grupo 15', 39.0, 1.7, 8.4, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (66, 'Abóbora, menina brasileira, crua', 'Grupo 15', 14.0, 0.6, 3.3, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (67, 'Abóbora, moranga, crua', 'Grupo 15', 12.0, 1.0, 2.7, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (68, 'Abóbora, moranga, refogada', 'Grupo 15', 29.0, 0.4, 6.0, 0.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (69, 'Abobora, pescoço, crua', 'Grupo 15', 24.0, 0.7, 6.1, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (70, 'Abobrinha, italiana, cozida', 'Grupo 15', 15.0, 1.1, 3.0, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (71, 'Abobrinha, italiana, crua', 'Grupo 15', 19.0, 1.1, 4.3, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (72, 'Abobrinha, italiana, refogada', 'Grupo 15', 24.0, 1.1, 4.2, 0.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (73, 'Abobrinha, paulista, crua', 'Grupo 15', 31.0, 0.6, 7.9, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (74, 'Acelga, crua', 'Grupo 15', 21.0, 1.4, 4.6, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (75, 'Agrião, cru', 'Grupo 15', 17.0, 2.7, 2.3, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (76, 'Aipo, cru', 'Grupo 15', 19.0, 0.8, 4.3, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (77, 'Alface, americana, crua', 'Grupo 15', 9.0, 0.6, 1.7, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (78, 'Alface, crespa, crua', 'Grupo 15', 11.0, 1.3, 1.7, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (79, 'Alface, lisa, crua', 'Grupo 15', 14.0, 1.7, 2.4, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (80, 'Alface, roxa, crua', 'Grupo 15', 13.0, 0.9, 2.5, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (81, 'Alfavaca, crua', 'Grupo 15', 29.0, 2.7, 5.2, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (82, 'Alho, cru', 'Grupo 15', 113.0, 7.0, 23.9, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (83, 'Alho-poró, cru', 'Grupo 15', 32.0, 1.4, 6.9, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (84, 'Almeirão, cru', 'Grupo 15', 18.0, 1.8, 3.3, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (85, 'Almeirão, refogado', 'Grupo 15', 65.0, 1.7, 5.7, 4.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (86, 'Batata, baroa, cozida', 'Grupo 15', 80.0, 0.9, 18.9, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (87, 'Batata, baroa, crua', 'Grupo 15', 101.0, 1.0, 24.0, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (88, 'Batata, doce, cozida', 'Grupo 15', 77.0, 0.6, 18.4, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (89, 'Batata, doce, crua', 'Grupo 15', 118.0, 1.3, 28.2, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (90, 'Batata, frita, tipo chips, industrializada', 'Grupo 15', 543.0, 5.6, 51.2, 36.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (91, 'Batata, inglesa, cozida', 'Grupo 15', 52.0, 1.2, 11.9, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (92, 'Batata, inglesa, crua', 'Grupo 15', 64.0, 1.8, 14.7, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (93, 'Batata, inglesa, frita', 'Grupo 15', 267.0, 5.0, 35.6, 13.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (94, 'Batata, inglesa, sauté', 'Grupo 17', 68.0, 1.3, 14.1, 0.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (95, 'Berinjela, cozida', 'Grupo 17', 19.0, 0.7, 4.5, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (96, 'Berinjela, crua', 'Grupo 17', 20.0, 1.2, 4.4, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (97, 'Beterraba, cozida', 'Grupo 17', 32.0, 1.3, 7.2, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (98, 'Beterraba, crua', 'Grupo 17', 49.0, 1.9, 11.1, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (99, 'Biscoito, polvilho doce', 'Grupo 17', 438.0, 1.3, 80.5, 12.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (100, 'Brócolis, cozido', 'Grupo 17', 25.0, 2.1, 4.4, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (101, 'Brócolis, cru', 'Grupo 17', 25.0, 3.6, 4.0, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (102, 'Cará, cozido', 'Grupo 17', 78.0, 1.5, 18.9, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (103, 'Cará, cru', 'Grupo 17', 96.0, 2.3, 23.0, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (104, 'Caruru, cru', 'Grupo 17', 34.0, 3.2, 6.0, 0.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (105, 'Catalonha, crua', 'Grupo 17', 24.0, 1.9, 4.8, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (106, 'Catalonha, refogada', 'Grupo 17', 63.0, 2.0, 4.8, 4.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (107, 'Cebola, crua', 'Grupo 17', 39.0, 1.7, 8.9, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (108, 'Cebolinha, crua', 'Grupo 17', 20.0, 1.9, 3.4, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (109, 'Cenoura, cozida', 'Grupo 17', 30.0, 0.8, 6.7, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (110, 'Cenoura, crua', 'Grupo 17', 34.0, 1.3, 7.7, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (111, 'Chicória, crua', 'Grupo 17', 14.0, 1.1, 2.9, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (112, 'Chuchu, cozido', 'Grupo 17', 19.0, 0.4, 4.8, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (113, 'Chuchu, cru', 'Grupo 17', 17.0, 0.7, 4.1, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (114, 'Coentro, folhas desidratadas', 'Grupo 17', 309.0, 20.9, 48.0, 10.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (115, 'Couve, manteiga, crua', 'Grupo 17', 27.0, 2.9, 4.3, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (116, 'Couve, manteiga, refogada', 'Grupo 17', 90.0, 1.7, 8.7, 6.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (117, 'Couve-flor, crua', 'Grupo 17', 23.0, 1.9, 4.5, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (118, 'Couve-flor, cozida', 'Grupo 17', 19.0, 1.2, 3.9, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (119, 'Espinafre,  Nova Zelândia, cru', 'Grupo 17', 16.0, 2.0, 2.6, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (120, 'Espinafre, Nova Zelândia, refogado', 'Grupo 17', 67.0, 2.7, 4.2, 5.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (121, 'Farinha, de mandioca, crua', 'Grupo 17', 361.0, 1.6, 87.9, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (122, 'Farinha, de mandioca, torrada', 'Grupo 17', 365.0, 1.2, 89.2, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (123, 'Farinha, de puba', 'Grupo 17', 360.0, 1.6, 87.3, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (124, 'Fécula, de mandioca', 'Grupo 17', 331.0, 0.5, 81.1, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (125, 'Feijão, broto, cru', 'Grupo 17', 39.0, 4.2, 7.8, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (126, 'Inhame, cru', 'Grupo 19', 97.0, 2.1, 23.2, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (127, 'Jiló, cru', 'Grupo 19', 27.0, 1.4, 6.2, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (128, 'Jurubeba, crua', 'Grupo 19', 126.0, 4.4, 23.1, 3.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (129, 'Mandioca, cozida', 'Grupo 19', 125.0, 0.6, 30.1, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (130, 'Mandioca, crua', 'Grupo 19', 151.0, 1.1, 36.2, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (131, 'Mandioca, farofa, temperada', 'Grupo 19', 406.0, 2.1, 80.3, 9.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (132, 'Mandioca, frita', 'Grupo 19', 300.0, 1.4, 50.3, 11.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (133, 'Manjericão, cru', 'Grupo 19', 21.0, 2.0, 3.6, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (134, 'Maxixe, cru', 'Grupo 19', 14.0, 1.4, 2.7, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (135, 'Mostarda, folha, crua', 'Grupo 19', 18.0, 2.1, 3.2, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (136, 'Nhoque, batata, cozido', 'Grupo 19', 181.0, 5.9, 36.8, 1.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (137, 'Nabo, cru', 'Grupo 19', 18.0, 1.2, 4.1, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (138, 'Palmito, juçara, em conserva', 'Grupo 19', 23.0, 1.8, 4.3, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (139, 'Palmito, pupunha, em conserva', 'Grupo 19', 29.0, 2.5, 5.5, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (140, 'Pão, de queijo, assado', 'Grupo 19', 363.0, 5.1, 34.2, 24.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (141, 'Pão, de queijo, cru', 'Grupo 19', 295.0, 3.6, 38.5, 14.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (142, 'Pepino, cru', 'Grupo 19', 10.0, 0.9, 2.0, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (143, 'Pimentão, amarelo, cru', 'Grupo 19', 28.0, 1.2, 6.0, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (144, 'Pimentão, verde, cru', 'Grupo 19', 21.0, 1.1, 4.9, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (145, 'Pimentão, vermelho, cru', 'Grupo 19', 23.0, 1.0, 5.5, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (146, 'Polvilho, doce', 'Grupo 19', 351.0, 0.4, 86.8, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (147, 'Quiabo, cru', 'Grupo 19', 30.0, 1.9, 6.4, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (148, 'Rabanete, cru', 'Grupo 19', 14.0, 1.4, 2.7, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (149, 'Repolho, branco, cru', 'Grupo 19', 17.0, 0.9, 3.9, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (150, 'Repolho, roxo, cru', 'Grupo 19', 31.0, 1.9, 7.2, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (151, 'Repolho, roxo, refogado', 'Grupo 19', 42.0, 1.8, 7.6, 1.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (152, 'Rúcula, crua', 'Grupo 19', 13.0, 1.8, 2.2, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (153, 'Salsa, crua', 'Grupo 19', 33.0, 3.3, 5.7, 0.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (154, 'Seleta de legumes, enlatada', 'Grupo 19', 57.0, 3.4, 12.7, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (155, 'Serralha, crua', 'Grupo 19', 30.0, 2.7, 4.9, 0.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (156, 'Taioba, crua', 'Grupo 19', 34.0, 2.9, 5.4, 0.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (157, 'Tomate, com semente, cru', 'Grupo 19', 15.0, 1.1, 3.1, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (158, 'Tomate, extrato', 'Grupo 21', 61.0, 2.4, 15.0, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (159, 'Tomate, molho industrializado', 'Grupo 21', 38.0, 1.4, 7.7, 0.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (160, 'Tomate, purê', 'Grupo 21', 28.0, 1.4, 6.9, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (161, 'Tomate, salada', 'Grupo 21', 21.0, 0.8, 5.1, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (162, 'Vagem, crua', 'Grupo 21', 25.0, 1.8, 5.3, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (163, 'Abacate, cru', 'Grupo 21', 96.0, 1.2, 6.0, 8.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (164, 'Abacaxi, cru', 'Grupo 21', 48.0, 0.9, 12.3, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (165, 'Abacaxi, polpa, congelada', 'Grupo 21', 31.0, 0.5, 7.8, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (166, 'Abiu, cru', 'Grupo 21', 62.0, 0.8, 14.9, 0.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (167, 'Açaí, polpa, com xarope de guaraná e glucose', 'Grupo 21', 110.0, 0.7, 21.5, 3.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (168, 'Açaí, polpa, congelada', 'Grupo 21', 58.0, 0.8, 6.2, 3.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (169, 'Acerola, crua', 'Grupo 21', 33.0, 0.9, 8.0, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (170, 'Acerola, polpa, congelada', 'Grupo 21', 22.0, 0.6, 5.5, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (171, 'Ameixa, calda, enlatada', 'Grupo 21', 183.0, 0.4, 46.9, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (172, 'Ameixa, crua', 'Grupo 21', 53.0, 0.8, 13.9, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (173, 'Ameixa, em calda, enlatada, drenada', 'Grupo 21', 177.0, 1.0, 47.7, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (174, 'Atemóia, crua', 'Grupo 21', 97.0, 1.0, 25.3, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (175, 'Banana, da terra, crua', 'Grupo 21', 128.0, 1.4, 33.7, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (176, 'Banana, doce em barra', 'Grupo 21', 280.0, 2.2, 75.7, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (177, 'Banana, figo, crua', 'Grupo 21', 105.0, 1.1, 27.8, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (178, 'Banana, maçã, crua', 'Grupo 21', 87.0, 1.8, 22.3, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (179, 'Banana, nanica, crua', 'Grupo 21', 92.0, 1.4, 23.8, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (180, 'Banana, ouro, crua', 'Grupo 21', 112.0, 1.5, 29.3, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (181, 'Banana, pacova, crua', 'Grupo 21', 78.0, 1.2, 20.3, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (182, 'Banana, prata, crua', 'Grupo 21', 98.0, 1.3, 26.0, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (183, 'Cacau, cru', 'Grupo 21', 74.0, 1.0, 19.4, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (184, 'Cajá-Manga, cru', 'Grupo 21', 46.0, 1.3, 11.4, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (185, 'Cajá, polpa, congelada', 'Grupo 21', 26.0, 0.6, 6.4, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (186, 'Caju, cru', 'Grupo 21', 43.0, 1.0, 10.3, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (187, 'Caju, polpa, congelada', 'Grupo 21', 37.0, 0.5, 9.4, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (188, 'Caju, suco concentrado, envasado', 'Grupo 23', 45.0, 0.4, 10.7, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (189, 'Caqui, chocolate, cru', 'Grupo 23', 71.0, 0.4, 19.3, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (190, 'Carambola, crua', 'Grupo 23', 46.0, 0.9, 11.5, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (191, 'Ciriguela, crua', 'Grupo 23', 76.0, 1.4, 18.9, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (192, 'Cupuaçu, cru', 'Grupo 23', 49.0, 1.2, 10.4, 1.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (193, 'Cupuaçu, polpa, congelada', 'Grupo 23', 49.0, 0.8, 11.4, 0.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (194, 'Figo, cru', 'Grupo 23', 41.0, 1.0, 10.2, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (195, 'Figo, enlatado, em calda', 'Grupo 23', 184.0, 0.6, 50.3, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (196, 'Fruta-pão, crua', 'Grupo 23', 67.0, 1.1, 17.2, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (197, 'Goiaba, branca, com casca, crua', 'Grupo 23', 52.0, 0.9, 12.4, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (198, 'Goiaba, doce em pasta', 'Grupo 23', 269.0, 0.6, 74.1, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (199, 'Goiaba, doce, cascão', 'Grupo 23', 286.0, 0.4, 78.7, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (200, 'Goiaba, vermelha, com casca, crua', 'Grupo 23', 54.0, 1.1, 13.0, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (201, 'Graviola, crua', 'Grupo 23', 62.0, 0.8, 15.8, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (202, 'Graviola, polpa, congelada', 'Grupo 23', 38.0, 0.6, 9.8, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (203, 'Jabuticaba, crua', 'Grupo 23', 58.0, 0.6, 15.3, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (204, 'Jaca, crua', 'Grupo 23', 88.0, 1.4, 22.5, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (205, 'Jambo, cru', 'Grupo 23', 27.0, 0.9, 6.5, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (206, 'Jamelão, cru', 'Grupo 23', 41.0, 0.5, 10.6, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (207, 'Kiwi, cru', 'Grupo 23', 51.0, 1.3, 11.5, 0.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (208, 'Laranja, baía, crua', 'Grupo 23', 45.0, 1.0, 11.5, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (209, 'Laranja, baía, suco', 'Grupo 23', 37.0, 0.7, 8.7, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (210, 'Laranja, da terra, crua', 'Grupo 23', 51.0, 1.1, 12.9, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (211, 'Laranja, da terra, suco', 'Grupo 23', 41.0, 0.7, 9.6, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (212, 'Laranja, lima, crua', 'Grupo 23', 46.0, 1.1, 11.5, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (213, 'Laranja, lima, suco', 'Grupo 23', 39.0, 0.7, 9.2, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (214, 'Laranja, pêra, crua', 'Grupo 23', 37.0, 1.0, 8.9, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (215, 'Laranja, pêra, suco', 'Grupo 23', 33.0, 0.7, 7.6, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (216, 'Laranja, valência, crua', 'Grupo 23', 46.0, 0.8, 11.7, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (217, 'Laranja, valência, suco', 'Grupo 23', 36.0, 0.5, 8.6, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (218, 'Limão, cravo, suco', 'Grupo 23', 14.0, 0.3, 5.2, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (219, 'Limão, galego, suco', 'Grupo 23', 22.0, 0.6, 7.3, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (220, 'Limão, tahiti, cru', 'Grupo 25', 32.0, 0.9, 11.1, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (221, 'Maçã, Argentina, com casca, crua', 'Grupo 25', 63.0, 0.2, 16.6, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (222, 'Maçã, Fuji, com casca, crua', 'Grupo 25', 56.0, 0.3, 15.2, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (223, 'Macaúba, crua', 'Grupo 25', 404.0, 2.1, 13.9, 40.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (224, 'Mamão, doce em calda, drenado', 'Grupo 25', 196.0, 0.2, 54.0, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (225, 'Mamão, Formosa, cru', 'Grupo 25', 45.0, 0.8, 11.6, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (226, 'Mamão, Papaia, cru', 'Grupo 25', 40.0, 0.5, 10.4, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (227, 'Mamão verde, doce em calda, drenado', 'Grupo 25', 209.0, 0.3, 57.6, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (228, 'Manga, Haden, crua', 'Grupo 25', 64.0, 0.4, 16.7, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (229, 'Manga, Palmer, crua', 'Grupo 25', 72.0, 0.4, 19.4, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (230, 'Manga, polpa, congelada', 'Grupo 25', 48.0, 0.4, 12.5, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (231, 'Manga, Tommy Atkins, crua', 'Grupo 25', 51.0, 0.9, 12.8, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (232, 'Maracujá, cru', 'Grupo 25', 68.0, 2.0, 12.3, 2.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (233, 'Maracujá, polpa, congelada', 'Grupo 25', 39.0, 0.8, 9.6, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (234, 'Maracujá, suco concentrado, envasado', 'Grupo 25', 42.0, 0.8, 9.6, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (235, 'Melancia, crua', 'Grupo 25', 33.0, 0.9, 8.1, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (236, 'Melão, cru', 'Grupo 25', 29.0, 0.7, 7.5, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (237, 'Mexerica, Murcote, crua', 'Grupo 25', 58.0, 0.9, 14.9, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (238, 'Mexerica, Rio, crua', 'Grupo 25', 37.0, 0.7, 9.3, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (239, 'Morango, cru', 'Grupo 25', 30.0, 0.9, 6.8, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (240, 'Nêspera, crua', 'Grupo 25', 43.0, 0.3, 11.5, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (241, 'Pequi, cru', 'Grupo 25', 205.0, 2.3, 13.0, 18.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (242, 'Pêra, Park, crua', 'Grupo 25', 61.0, 0.2, 16.1, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (243, 'Pêra, Williams, crua', 'Grupo 25', 53.0, 0.6, 14.0, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (244, 'Pêssego, Aurora, cru', 'Grupo 25', 36.0, 0.8, 9.3, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (245, 'Pêssego, enlatado, em calda', 'Grupo 25', 63.0, 0.7, 16.9, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (246, 'Pinha, crua', 'Grupo 25', 88.0, 1.5, 22.4, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (247, 'Pitanga, crua', 'Grupo 25', 41.0, 0.9, 10.2, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (248, 'Pitanga, polpa, congelada', 'Grupo 25', 19.0, 0.3, 4.8, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (249, 'Romã, crua', 'Grupo 25', 56.0, 0.4, 15.1, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (250, 'Tamarindo, cru', 'Grupo 25', 276.0, 3.2, 72.5, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (251, 'Tangerina, Poncã, crua', 'Grupo 25', 38.0, 0.8, 9.6, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (252, 'Tangerina, Poncã, suco', 'Grupo 27', 36.0, 0.5, 8.8, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (253, 'Tucumã, cru', 'Grupo 27', 262.0, 2.1, 26.5, 19.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (254, 'Umbu, cru', 'Grupo 27', 37.0, 0.8, 9.4, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (255, 'Umbu, polpa, congelada', 'Grupo 27', 34.0, 0.5, 8.8, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (256, 'Uva, Itália, crua', 'Grupo 27', 53.0, 0.7, 13.6, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (257, 'Uva, Rubi, crua', 'Grupo 27', 49.0, 0.6, 12.7, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (258, 'Uva, suco concentrado, envasado', 'Grupo 27', 58.0, 0.0, 14.7, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (279, 'Bacalhau, salgado, cru', 'Grupo 29', 136.0, 29.0, 0.0, 1.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (280, 'Bacalhau, salgado, refogado', 'Grupo 29', 140.0, 24.0, 1.2, 3.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (281, 'Cação, posta, com farinha de trigo, frita', 'Grupo 29', 208.0, 25.0, 3.1, 10.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (282, 'Cação, posta, cozida', 'Grupo 29', 116.0, 25.6, 0.0, 0.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (283, 'Cação, posta, crua', 'Grupo 29', 83.0, 17.9, 0.0, 0.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (284, 'Camarão, Rio Grande, grande, cozido', 'Grupo 29', 90.0, 19.0, 0.0, 1.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (285, 'Camarão, Rio Grande, grande, cru', 'Grupo 29', 47.0, 10.0, 0.0, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (286, 'Camarão, Sete Barbas, sem cabeça, com casca, frito', 'Grupo 29', 231.0, 18.4, 2.9, 15.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (287, 'Caranguejo, cozido', 'Grupo 29', 83.0, 18.5, 0.0, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (288, 'Corimba, cru', 'Grupo 29', 128.0, 17.4, 0.0, 6.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (289, 'Corimbatá, assado', 'Grupo 29', 261.0, 19.9, 0.0, 19.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (290, 'Corimbatá, cozido', 'Grupo 29', 239.0, 20.1, 0.0, 16.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (291, 'Corvina de água doce, crua', 'Grupo 29', 101.0, 18.9, 0.0, 2.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (292, 'Corvina do mar, crua', 'Grupo 29', 94.0, 18.6, 0.0, 1.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (293, 'Corvina grande, assada', 'Grupo 29', 147.0, 26.8, 0.0, 3.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (294, 'Corvina grande, cozida', 'Grupo 29', 100.0, 23.4, 0.0, 2.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (295, 'Dourada de água doce, fresca', 'Grupo 29', 131.0, 18.8, 0.0, 5.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (296, 'Lambari, congelado, cru', 'Grupo 29', 131.0, 16.8, 0.0, 6.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (297, 'Lambari, congelado, frito', 'Grupo 29', 327.0, 28.4, 0.0, 22.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (298, 'Lambari, fresco, cru', 'Grupo 29', 152.0, 15.7, 0.0, 9.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (299, 'Manjuba, com farinha de trigo, frita', 'Grupo 29', 344.0, 23.5, 10.2, 22.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (300, 'Manjuba, frita', 'Grupo 29', 349.0, 30.1, 0.0, 24.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (301, 'Merluza, filé, assado', 'Grupo 29', 122.0, 26.6, 0.0, 0.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (302, 'Merluza, filé, cru', 'Grupo 29', 89.0, 16.6, 0.0, 2.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (303, 'Merluza, filé, frito', 'Grupo 29', 192.0, 26.9, 0.0, 8.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (304, 'Pescada, branca, crua', 'Grupo 29', 111.0, 16.3, 0.0, 4.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (305, 'Pescada, branca, frita', 'Grupo 29', 223.0, 27.4, 0.0, 11.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (306, 'Pescada, filé, com farinha de trigo, frito', 'Grupo 29', 283.0, 21.4, 5.0, 19.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (307, 'Pescada, filé, cru', 'Grupo 29', 107.0, 16.7, 0.0, 4.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (308, 'Pescada, filé, frito', 'Grupo 29', 154.0, 28.6, 0.0, 3.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (309, 'Pescada, filé, molho escabeche', 'Grupo 29', 142.0, 11.8, 5.0, 8.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (310, 'Pescadinha, crua', 'Grupo 29', 76.0, 15.5, 0.0, 1.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (311, 'Pintado, assado', 'Grupo 31', 192.0, 36.5, 0.0, 4.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (312, 'Pintado, cru', 'Grupo 31', 91.0, 18.6, 0.0, 1.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (313, 'Pintado, grelhado', 'Grupo 31', 152.0, 30.8, 0.0, 2.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (314, 'Porquinho, cru', 'Grupo 31', 93.0, 20.5, 0.0, 0.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (315, 'Salmão, filé, com pele, fresco,  grelhado', 'Grupo 31', 229.0, 23.9, 0.0, 14.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (316, 'Salmão, sem pele, fresco, cru', 'Grupo 31', 170.0, 19.3, 0.0, 9.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (317, 'Salmão, sem pele, fresco, grelhado', 'Grupo 31', 243.0, 26.1, 0.0, 14.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (318, 'Sardinha, assada', 'Grupo 31', 164.0, 32.2, 0.0, 3.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (319, 'Sardinha, conserva em óleo', 'Grupo 31', 285.0, 15.9, 0.0, 24.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (320, 'Sardinha, frita', 'Grupo 31', 257.0, 33.4, 0.0, 12.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (321, 'Sardinha, inteira, crua', 'Grupo 31', 114.0, 21.1, 0.0, 2.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (322, 'Tucunaré, filé, congelado, cru', 'Grupo 31', 88.0, 18.0, 0.0, 1.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (323, 'Apresuntado', 'Grupo 31', 129.0, 13.5, 2.9, 6.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (324, 'Caldo de carne, tablete', 'Grupo 31', 241.0, 7.8, 15.1, 16.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (325, 'Caldo de galinha, tablete', 'Grupo 31', 251.0, 6.3, 10.6, 20.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (326, 'Carne, bovina, acém, moído, cozido', 'Grupo 31', 212.0, 26.7, 0.0, 10.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (327, 'Carne, bovina, acém, moído, cru', 'Grupo 31', 137.0, 19.4, 0.0, 5.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (328, 'Carne, bovina, acém, sem gordura, cozido', 'Grupo 31', 215.0, 27.3, 0.0, 10.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (329, 'Carne, bovina, acém, sem gordura, cru', 'Grupo 31', 144.0, 20.8, 0.0, 6.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (330, 'Carne, bovina, almôndegas, cruas', 'Grupo 31', 189.0, 12.3, 9.8, 11.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (331, 'Carne, bovina, almôndegas, fritas', 'Grupo 31', 272.0, 18.2, 14.3, 15.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (332, 'Carne, bovina, bucho, cozido', 'Grupo 31', 133.0, 21.6, 0.0, 4.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (333, 'Carne, bovina, bucho, cru', 'Grupo 31', 137.0, 20.5, 0.0, 5.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (334, 'Carne, bovina, capa de contra-filé, com gordura, crua', 'Grupo 31', 217.0, 19.2, 0.0, 15.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (335, 'Carne, bovina, capa de contra-filé, com gordura, grelhada', 'Grupo 31', 312.0, 30.7, 0.0, 20.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (336, 'Carne, bovina, capa de contra-filé, sem gordura, crua', 'Grupo 31', 131.0, 21.5, 0.0, 4.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (337, 'Carne, bovina, capa de contra-filé, sem gordura, grelhada', 'Grupo 31', 239.0, 35.1, 0.0, 10.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (338, 'Carne, bovina, charque, cozido', 'Grupo 31', 263.0, 36.4, 0.0, 11.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (339, 'Carne, bovina, charque, cru', 'Grupo 31', 249.0, 22.7, 0.0, 16.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (340, 'Carne, bovina, contra-filé, à milanesa', 'Grupo 31', 352.0, 20.6, 12.2, 24.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (341, 'Carne, bovina, contra-filé de costela, cru', 'Grupo 33', 202.0, 19.8, 0.0, 13.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (342, 'Carne, bovina, contra-filé de costela, grelhado', 'Grupo 33', 275.0, 29.9, 0.0, 16.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (343, 'Carne, bovina, contra-filé, com gordura, cru', 'Grupo 33', 206.0, 21.2, 0.0, 12.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (344, 'Carne, bovina, contra-filé, com gordura, grelhado', 'Grupo 33', 278.0, 32.4, 0.0, 15.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (345, 'Carne, bovina, contra-filé, sem gordura, cru', 'Grupo 33', 157.0, 24.0, 0.0, 6.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (346, 'Carne, bovina, contra-filé, sem gordura, grelhado', 'Grupo 33', 194.0, 35.9, 0.0, 4.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (347, 'Carne, bovina, costela, assada', 'Grupo 33', 373.0, 28.8, 0.0, 27.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (348, 'Carne, bovina, costela, crua', 'Grupo 33', 358.0, 16.7, 0.0, 31.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (349, 'Carne, bovina, coxão duro, sem gordura, cozido', 'Grupo 33', 217.0, 31.9, 0.0, 8.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (350, 'Carne, bovina, coxão duro, sem gordura, cru', 'Grupo 33', 148.0, 21.5, 0.0, 6.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (351, 'Carne, bovina, coxão mole, sem gordura, cozido', 'Grupo 33', 219.0, 32.4, 0.0, 8.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (352, 'Carne, bovina, coxão mole, sem gordura, cru', 'Grupo 33', 169.0, 21.2, 0.0, 8.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (353, 'Carne, bovina, cupim, assado', 'Grupo 33', 330.0, 28.6, 0.0, 23.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (354, 'Carne, bovina, cupim, cru', 'Grupo 33', 221.0, 19.5, 0.0, 15.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (355, 'Carne, bovina, fígado, cru', 'Grupo 33', 141.0, 20.7, 1.1, 5.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (356, 'Carne, bovina, fígado, grelhado', 'Grupo 33', 225.0, 29.9, 4.2, 9.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (357, 'Carne, bovina, filé mingnon, sem gordura, cru', 'Grupo 33', 143.0, 21.6, 0.0, 5.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (358, 'Carne, bovina, filé mingnon, sem gordura, grelhado', 'Grupo 33', 220.0, 32.8, 0.0, 8.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (359, 'Carne, bovina, flanco, sem gordura, cozido', 'Grupo 33', 196.0, 29.4, 0.0, 7.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (360, 'Carne, bovina, flanco, sem gordura, cru', 'Grupo 33', 141.0, 20.0, 0.0, 6.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (361, 'Carne, bovina, fraldinha, com gordura, cozida', 'Grupo 33', 338.0, 24.2, 0.0, 26.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (362, 'Carne, bovina, fraldinha, com gordura, crua', 'Grupo 33', 221.0, 17.6, 0.0, 16.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (363, 'Carne, bovina, lagarto, cozido', 'Grupo 33', 222.0, 32.9, 0.0, 9.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (364, 'Carne, bovina, lagarto, cru', 'Grupo 33', 135.0, 20.5, 0.0, 5.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (365, 'Carne, bovina, língua, cozida', 'Grupo 33', 315.0, 21.4, 0.0, 24.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (366, 'Carne, bovina, língua, crua', 'Grupo 33', 215.0, 17.1, 0.0, 15.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (367, 'Carne, bovina, maminha, crua', 'Grupo 33', 153.0, 20.9, 0.0, 7.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (368, 'Carne, bovina, maminha, grelhada', 'Grupo 33', 153.0, 30.7, 0.0, 2.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (369, 'Carne, bovina, miolo de alcatra, sem gordura, cru', 'Grupo 33', 163.0, 21.6, 0.0, 7.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (370, 'Carne, bovina, miolo de alcatra, sem gordura, grelhado', 'Grupo 33', 241.0, 31.9, 0.0, 11.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (371, 'Carne, bovina, músculo, sem gordura, cozido', 'Grupo 33', 194.0, 31.2, 0.0, 6.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (372, 'Carne, bovina, músculo, sem gordura, cru', 'Grupo 33', 142.0, 21.6, 0.0, 5.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (373, 'Carne, bovina, paleta, com gordura, crua', 'Grupo 35', 159.0, 21.4, 0.0, 7.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (374, 'Carne, bovina, paleta, sem gordura, cozida', 'Grupo 35', 194.0, 29.7, 0.0, 7.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (375, 'Carne, bovina, paleta, sem gordura, crua', 'Grupo 35', 141.0, 21.0, 0.0, 5.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (376, 'Carne, bovina, patinho, sem gordura, cru', 'Grupo 35', 133.0, 21.7, 0.0, 4.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (377, 'Carne, bovina, patinho, sem gordura, grelhado', 'Grupo 35', 219.0, 35.9, 0.0, 7.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (378, 'Carne, bovina, peito, sem gordura, cozido', 'Grupo 35', 338.0, 22.2, 0.0, 27.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (379, 'Carne, bovina, peito, sem gordura, cru', 'Grupo 35', 259.0, 17.6, 0.0, 20.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (380, 'Carne, bovina, picanha, com gordura, crua', 'Grupo 35', 213.0, 18.8, 0.0, 14.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (381, 'Carne, bovina, picanha, com gordura, grelhada', 'Grupo 35', 289.0, 26.4, 0.0, 19.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (382, 'Carne, bovina, picanha, sem gordura, crua', 'Grupo 35', 134.0, 21.3, 0.0, 4.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (383, 'Carne, bovina, picanha, sem gordura, grelhada', 'Grupo 35', 238.0, 31.9, 0.0, 11.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (384, 'Carne, bovina, seca, cozida', 'Grupo 35', 313.0, 26.9, 0.0, 21.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (385, 'Carne, bovina, seca, crua', 'Grupo 35', 313.0, 19.7, 0.0, 25.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (386, 'Coxinha de frango, frita', 'Grupo 35', 283.0, 9.6, 34.5, 11.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (387, 'Croquete, de carne, cru', 'Grupo 35', 246.0, 12.0, 13.9, 15.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (388, 'Croquete, de carne, frito', 'Grupo 35', 347.0, 16.9, 18.1, 22.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (389, 'Empada de frango, pré-cozida, assada', 'Grupo 35', 358.0, 6.9, 47.5, 15.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (390, 'Empada, de frango, pré-cozida', 'Grupo 35', 377.0, 7.3, 35.5, 22.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (391, 'Frango, asa, com pele, crua', 'Grupo 35', 213.0, 18.1, 0.0, 15.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (392, 'Frango, caipira, inteiro, com pele, cozido', 'Grupo 35', 243.0, 23.9, 0.0, 15.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (393, 'Frango, caipira, inteiro, sem pele, cozido', 'Grupo 35', 196.0, 29.6, 0.0, 7.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (394, 'Frango, coração, cru', 'Grupo 35', 222.0, 12.6, 0.0, 18.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (395, 'Frango, coração, grelhado', 'Grupo 35', 207.0, 22.4, 0.6, 12.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (396, 'Frango, coxa, com pele, assada', 'Grupo 35', 215.0, 28.5, 0.1, 10.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (397, 'Frango, coxa, com pele, crua', 'Grupo 35', 161.0, 17.1, 0.0, 9.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (398, 'Frango, coxa, sem pele, cozida', 'Grupo 35', 167.0, 26.9, 0.0, 5.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (399, 'Frango, coxa, sem pele, crua', 'Grupo 35', 120.0, 17.8, 0.0, 4.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (400, 'Frango, fígado, cru', 'Grupo 35', 106.0, 17.6, 0.0, 3.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (401, 'Frango, filé, à milanesa', 'Grupo 35', 221.0, 28.5, 7.5, 7.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (402, 'Frango, inteiro, com pele, cru', 'Grupo 35', 226.0, 16.4, 0.0, 17.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (403, 'Frango, inteiro, sem pele, assado', 'Grupo 35', 187.0, 28.0, 0.0, 7.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (404, 'Frango, inteiro, sem pele, cozido', 'Grupo 35', 170.0, 25.0, 0.0, 7.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (405, 'Frango, inteiro, sem pele, cru', 'Grupo 37', 129.0, 20.6, 0.0, 4.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (406, 'Frango, peito, com pele, assado', 'Grupo 37', 212.0, 33.4, 0.0, 7.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (407, 'Frango, peito, com pele, cru', 'Grupo 37', 149.0, 20.8, 0.0, 6.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (408, 'Frango, peito, sem pele, cozido', 'Grupo 37', 163.0, 31.5, 0.0, 3.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (409, 'Frango, peito, sem pele, cru', 'Grupo 37', 119.0, 21.5, 0.0, 3.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (410, 'Frango, peito, sem pele, grelhado', 'Grupo 37', 159.0, 32.0, 0.0, 2.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (411, 'Frango, sobrecoxa, com pele, assada', 'Grupo 37', 260.0, 28.7, 0.0, 15.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (412, 'Frango, sobrecoxa, com pele, crua', 'Grupo 37', 255.0, 15.5, 0.0, 20.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (413, 'Frango, sobrecoxa, sem pele, assada', 'Grupo 37', 233.0, 29.2, 0.0, 12.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (414, 'Frango, sobrecoxa, sem pele, crua', 'Grupo 37', 162.0, 17.6, 0.0, 9.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (415, 'Hambúrguer, bovino, cru', 'Grupo 37', 215.0, 13.2, 4.2, 16.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (416, 'Hambúrguer, bovino, frito', 'Grupo 37', 258.0, 20.0, 6.3, 17.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (417, 'Hambúrguer, bovino, grelhado', 'Grupo 37', 210.0, 13.2, 11.3, 12.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (418, 'Lingüiça, frango, crua', 'Grupo 37', 218.0, 14.2, 0.0, 17.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (419, 'Lingüiça, frango, frita', 'Grupo 37', 245.0, 18.3, 0.0, 18.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (420, 'Lingüiça, frango, grelhada', 'Grupo 37', 244.0, 18.2, 0.0, 18.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (421, 'Lingüiça, porco, crua', 'Grupo 37', 227.0, 16.1, 0.0, 17.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (422, 'Lingüiça, porco, frita', 'Grupo 37', 280.0, 20.5, 0.0, 21.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (423, 'Lingüiça, porco, grelhada', 'Grupo 37', 296.0, 23.2, 0.0, 21.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (424, 'Mortadela', 'Grupo 37', 269.0, 12.0, 5.8, 21.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (425, 'Peru, congelado, assado', 'Grupo 37', 163.0, 26.2, 0.0, 5.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (426, 'Peru, congelado, cru', 'Grupo 37', 94.0, 18.1, 0.0, 1.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (427, 'Porco, bisteca, crua', 'Grupo 37', 164.0, 21.5, 0.0, 8.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (428, 'Porco, bisteca, frita', 'Grupo 37', 311.0, 33.7, 0.0, 18.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (429, 'Porco, bisteca, grelhada', 'Grupo 37', 280.0, 28.9, 0.0, 17.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (430, 'Porco, costela, assada', 'Grupo 37', 402.0, 30.2, 0.0, 30.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (431, 'Porco, costela, crua', 'Grupo 37', 256.0, 18.0, 0.0, 19.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (432, 'Porco, lombo, assado', 'Grupo 37', 210.0, 35.7, 0.0, 6.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (433, 'Porco, lombo, cru', 'Grupo 37', 176.0, 22.6, 0.0, 8.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (434, 'Porco, orelha, salgada, crua', 'Grupo 37', 258.0, 18.5, 0.0, 19.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (435, 'Porco, pernil, assado', 'Grupo 37', 262.0, 32.1, 0.0, 13.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (436, 'Porco, pernil, cru', 'Grupo 37', 186.0, 20.1, 0.0, 11.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (437, 'Porco, rabo, salgado, cru', 'Grupo 39', 377.0, 15.6, 0.0, 34.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (438, 'Presunto, com capa de gordura', 'Grupo 39', 128.0, 14.4, 1.4, 6.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (439, 'Presunto, sem capa de gordura', 'Grupo 39', 94.0, 14.3, 2.1, 2.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (440, 'Quibe, assado', 'Grupo 39', 136.0, 14.6, 12.9, 2.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (441, 'Quibe, cru', 'Grupo 39', 109.0, 12.4, 10.8, 1.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (442, 'Quibe, frito', 'Grupo 39', 254.0, 14.9, 12.3, 15.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (443, 'Salame', 'Grupo 39', 398.0, 25.8, 2.9, 30.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (444, 'Toucinho, cru', 'Grupo 39', 593.0, 11.5, 0.0, 60.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (445, 'Toucinho, frito', 'Grupo 39', 697.0, 27.3, 0.0, 64.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (446, 'Bebida láctea, pêssego', 'Grupo 39', 55.0, 2.1, 7.6, 1.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (447, 'Creme de Leite', 'Grupo 39', 221.0, 1.5, 4.5, 22.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (448, 'Iogurte, natural', 'Grupo 39', 51.0, 4.1, 1.9, 3.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (449, 'Iogurte, natural, desnatado', 'Grupo 39', 41.0, 3.8, 5.8, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (450, 'Iogurte, sabor abacaxi', 'Grupo 39', 0, 0, 0, 0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (451, 'Iogurte, sabor morango', 'Grupo 39', 70.0, 2.7, 9.7, 2.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (452, 'Iogurte, sabor pêssego', 'Grupo 39', 68.0, 2.5, 9.4, 2.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (453, 'Leite, condensado', 'Grupo 39', 313.0, 7.7, 57.0, 6.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (454, 'Leite, de cabra', 'Grupo 39', 66.0, 3.1, 5.2, 3.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (455, 'Leite, de vaca, achocolatado', 'Grupo 39', 83.0, 2.1, 14.2, 2.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (456, 'Leite, de vaca, desnatado, pó', 'Grupo 39', 362.0, 34.7, 53.0, 0.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (457, 'Leite, de vaca, desnatado, UHT', 'Grupo 39', 0, 0, 0, 0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (458, 'Leite, de vaca, integral', 'Grupo 39', 0, 0, 0, 0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (459, 'Leite, de vaca, integral, pó', 'Grupo 39', 497.0, 25.4, 39.2, 26.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (460, 'Leite, fermentado', 'Grupo 39', 70.0, 1.9, 15.7, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (461, 'Queijo, minas, frescal', 'Grupo 39', 264.0, 17.4, 3.2, 20.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (462, 'Queijo, minas, meia cura', 'Grupo 39', 321.0, 21.2, 3.6, 24.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (463, 'Queijo, mozarela', 'Grupo 39', 330.0, 22.6, 3.0, 25.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (464, 'Queijo, parmesão', 'Grupo 39', 453.0, 35.6, 1.7, 33.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (465, 'Queijo, pasteurizado', 'Grupo 39', 303.0, 9.4, 5.7, 27.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (466, 'Queijo, petit suisse, morango', 'Grupo 41', 121.0, 5.8, 18.5, 2.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (467, 'Queijo, prato', 'Grupo 41', 360.0, 22.7, 1.9, 29.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (468, 'Queijo, requeijão, cremoso', 'Grupo 41', 257.0, 9.6, 2.4, 23.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (469, 'Queijo, ricota', 'Grupo 41', 140.0, 12.6, 3.8, 8.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (470, 'Bebida isotônica, sabores variados', 'Grupo 41', 26.0, 0.0, 6.4, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (471, 'Café, infusão 10%', 'Grupo 41', 9.0, 0.7, 1.5, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (472, 'Cana, aguardente 1', 'Grupo 41', 216.0, 0.0, 0.0, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (473, 'Cana, caldo de', 'Grupo 41', 65.0, 0.0, 18.2, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (474, 'Cerveja, pilsen 2', 'Grupo 41', 41.0, 0.6, 3.3, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (475, 'Chá, erva-doce, infusão 5%', 'Grupo 41', 1.0, 0.0, 0.4, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (476, 'Chá, mate, infusão 5%', 'Grupo 41', 3.0, 0.0, 0.6, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (477, 'Chá, preto, infusão 5%', 'Grupo 41', 2.0, 0.0, 0.6, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (478, 'Coco, água de', 'Grupo 41', 22.0, 0.0, 5.3, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (479, 'Refrigerante, tipo água tônica', 'Grupo 41', 31.0, 0.0, 8.0, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (480, 'Refrigerante, tipo cola', 'Grupo 41', 34.0, 0.0, 8.7, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (481, 'Refrigerante, tipo guaraná', 'Grupo 41', 39.0, 0.0, 10.0, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (482, 'Refrigerante, tipo laranja', 'Grupo 41', 46.0, 0.0, 11.8, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (483, 'Refrigerante, tipo limão', 'Grupo 41', 40.0, 0.0, 10.3, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (484, 'Omelete, de queijo', 'Grupo 41', 268.0, 15.6, 0.4, 22.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (485, 'Ovo, de codorna, inteiro, cru', 'Grupo 41', 177.0, 13.7, 0.8, 12.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (486, 'Ovo, de galinha, clara, cozida/10minutos', 'Grupo 41', 59.0, 13.4, 0.0, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (487, 'Ovo, de galinha, gema, cozida/10minutos', 'Grupo 41', 353.0, 15.9, 1.6, 30.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (488, 'Ovo, de galinha, inteiro, cozido/10minutos', 'Grupo 41', 146.0, 13.3, 0.6, 9.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (489, 'Ovo, de galinha, inteiro, cru', 'Grupo 41', 143.0, 13.0, 1.6, 8.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (490, 'Ovo, de galinha, inteiro, frito', 'Grupo 41', 240.0, 15.6, 1.2, 18.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (491, 'Achocolatado, pó', 'Grupo 41', 401.0, 4.2, 91.2, 2.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (492, 'Açúcar, cristal', 'Grupo 41', 387.0, 0.3, 99.6, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (493, 'Açúcar, mascavo', 'Grupo 43', 369.0, 0.8, 94.5, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (494, 'Açúcar, refinado', 'Grupo 43', 387.0, 0.3, 99.5, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (495, 'Chocolate, ao leite', 'Grupo 43', 540.0, 7.2, 59.6, 30.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (496, 'Chocolate, ao leite, com castanha do Pará', 'Grupo 43', 559.0, 7.4, 55.4, 34.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (497, 'Chocolate, ao leite, dietético', 'Grupo 43', 557.0, 6.9, 56.3, 33.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (498, 'Chocolate, meio amargo', 'Grupo 43', 475.0, 4.9, 62.4, 29.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (499, 'Cocada branca', 'Grupo 43', 449.0, 1.1, 81.4, 13.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (500, 'Doce, de abóbora, cremoso', 'Grupo 43', 199.0, 0.9, 54.6, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (501, 'Doce, de leite, cremoso', 'Grupo 43', 306.0, 5.5, 59.5, 6.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (502, 'Geléia, mocotó, natural', 'Grupo 43', 106.0, 2.1, 24.2, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (503, 'Glicose de milho', 'Grupo 43', 292.0, 0.0, 79.4, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (504, 'Maria mole', 'Grupo 43', 301.0, 3.8, 73.6, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (505, 'Maria mole, coco queimado', 'Grupo 43', 307.0, 3.9, 75.1, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (506, 'Marmelada', 'Grupo 43', 257.0, 0.4, 70.8, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (507, 'Mel, de abelha', 'Grupo 43', 309.0, 0.0, 84.0, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (508, 'Melado', 'Grupo 43', 297.0, 0.0, 76.6, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (509, 'Quindim', 'Grupo 43', 411.0, 4.7, 46.3, 24.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (510, 'Rapadura', 'Grupo 43', 352.0, 1.0, 90.8, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (511, 'Café, pó, torrado', 'Grupo 43', 419.0, 14.7, 65.8, 11.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (512, 'Capuccino, pó', 'Grupo 43', 417.0, 11.3, 73.6, 8.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (513, 'Fermento em pó, químico', 'Grupo 43', 90.0, 0.5, 43.9, 0.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (514, 'Fermento, biológico, levedura, tablete', 'Grupo 43', 90.0, 17.0, 7.7, 1.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (515, 'Gelatina, sabores variados, pó', 'Grupo 43', 380.0, 8.9, 89.2, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (516, 'Sal, dietético', 'Grupo 43', 0.0, 0.0, 0.0, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (517, 'Sal, grosso', 'Grupo 43', 0.0, 0.0, 0.0, 0.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (518, 'Shoyu', 'Grupo 43', 61.0, 3.3, 11.6, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (519, 'Tempero a base de sal', 'Grupo 43', 21.0, 2.7, 2.1, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (520, 'Azeitona, preta, conserva', 'Grupo 43', 194.0, 1.2, 5.5, 20.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (521, 'Azeitona, verde, conserva', 'Grupo 45', 137.0, 0.9, 4.1, 14.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (522, 'Chantilly, spray, com gordura vegetal', 'Grupo 45', 315.0, 0.5, 16.9, 27.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (523, 'Leite, de coco', 'Grupo 45', 166.0, 1.0, 2.2, 18.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (524, 'Maionese, tradicional com ovos', 'Grupo 45', 302.0, 0.6, 7.9, 30.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (525, 'Acarajé', 'Grupo 45', 289.0, 8.3, 19.1, 19.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (526, 'Arroz carreteiro', 'Grupo 45', 154.0, 10.8, 11.6, 7.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (527, 'Baião de dois, arroz e feijão-de-corda', 'Grupo 45', 136.0, 6.2, 20.4, 3.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (528, 'Barreado', 'Grupo 45', 165.0, 18.3, 0.2, 9.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (529, 'Bife à cavalo, com contra filé', 'Grupo 45', 291.0, 23.7, 0.0, 21.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (530, 'Bolinho de arroz', 'Grupo 45', 274.0, 8.0, 41.7, 8.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (531, 'Camarão à baiana', 'Grupo 45', 101.0, 7.9, 3.2, 6.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (532, 'Charuto, de repolho', 'Grupo 45', 78.0, 6.8, 10.1, 1.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (533, 'Cuscuz, de milho, cozido com sal', 'Grupo 45', 113.0, 2.2, 25.3, 0.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (534, 'Cuscuz, paulista', 'Grupo 45', 142.0, 2.6, 22.5, 4.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (535, 'Cuxá, molho', 'Grupo 45', 80.0, 5.6, 5.7, 3.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (536, 'Dobradinha', 'Grupo 45', 125.0, 19.8, 0.0, 4.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (537, 'Estrogonofe de carne', 'Grupo 45', 173.0, 15.0, 3.0, 10.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (538, 'Estrogonofe de frango', 'Grupo 45', 157.0, 17.6, 2.6, 8.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (539, 'Feijão tropeiro mineiro', 'Grupo 45', 152.0, 10.2, 19.6, 6.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (540, 'Feijoada', 'Grupo 45', 117.0, 8.7, 11.6, 6.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (541, 'Frango, com açafrão', 'Grupo 45', 113.0, 9.7, 4.1, 6.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (542, 'Macarrão, molho bolognesa', 'Grupo 45', 120.0, 4.9, 22.5, 0.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (543, 'Maniçoba', 'Grupo 45', 134.0, 10.0, 3.4, 8.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (544, 'Quibebe', 'Grupo 45', 86.0, 8.6, 6.6, 2.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (545, 'Salada, de legumes, com maionese', 'Grupo 45', 96.0, 1.1, 8.9, 7.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (546, 'Salada, de legumes, cozida no vapor', 'Grupo 45', 35.0, 2.0, 7.1, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (547, 'Salpicão, de frango', 'Grupo 45', 148.0, 13.9, 4.6, 7.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (548, 'Sarapatel', 'Grupo 45', 123.0, 18.5, 1.1, 4.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (549, 'Tabule', 'Grupo 45', 57.0, 2.0, 10.6, 1.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (550, 'Tacacá', 'Grupo 47', 47.0, 7.0, 3.4, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (551, 'Tapioca, com manteiga', 'Grupo 47', 348.0, 0.1, 63.6, 10.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (552, 'Tucupi, com pimenta-de-cheiro', 'Grupo 47', 27.0, 2.1, 4.7, 0.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (553, 'Vaca atolada', 'Grupo 47', 145.0, 5.1, 10.1, 9.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (554, 'Vatapá', 'Grupo 47', 255.0, 6.0, 9.7, 23.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (555, 'Virado à paulista', 'Grupo 47', 307.0, 10.2, 14.1, 25.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (556, 'Yakisoba', 'Grupo 47', 113.0, 7.5, 18.3, 2.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (557, 'Amendoim, grão, cru', 'Grupo 47', 544.0, 27.2, 20.3, 43.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (558, 'Amendoim, torrado, salgado', 'Grupo 47', 606.0, 22.5, 18.7, 54.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (559, 'Ervilha, em vagem', 'Grupo 47', 88.0, 7.5, 14.2, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (560, 'Ervilha, enlatada, drenada', 'Grupo 47', 74.0, 4.6, 13.4, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (561, 'Feijão, carioca, cozido', 'Grupo 47', 76.0, 4.8, 13.6, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (562, 'Feijão, carioca, cru', 'Grupo 47', 329.0, 20.0, 61.2, 1.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (563, 'Feijão, fradinho, cozido', 'Grupo 47', 78.0, 5.1, 13.5, 0.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (564, 'Feijão, fradinho, cru', 'Grupo 47', 339.0, 20.2, 61.2, 2.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (565, 'Feijão, jalo, cozido', 'Grupo 47', 93.0, 6.1, 16.5, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (566, 'Feijão, jalo, cru', 'Grupo 47', 328.0, 20.1, 61.5, 0.9, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (567, 'Feijão, preto, cozido', 'Grupo 47', 77.0, 4.5, 14.0, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (568, 'Feijão, preto, cru', 'Grupo 47', 324.0, 21.3, 58.8, 1.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (569, 'Feijão, rajado, cozido', 'Grupo 47', 85.0, 5.5, 15.3, 0.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (570, 'Feijão, rajado, cru', 'Grupo 47', 326.0, 17.3, 62.9, 1.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (571, 'Feijão, rosinha, cozido', 'Grupo 47', 68.0, 4.5, 11.8, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (572, 'Feijão, rosinha, cru', 'Grupo 47', 337.0, 20.9, 62.2, 1.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (573, 'Feijão, roxo, cozido', 'Grupo 47', 77.0, 5.7, 12.9, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (574, 'Feijão, roxo, cru', 'Grupo 47', 331.0, 22.2, 60.0, 1.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (575, 'Grão-de-bico, cru', 'Grupo 47', 355.0, 21.2, 57.9, 5.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (576, 'Guandu, cru', 'Grupo 47', 344.0, 19.0, 64.0, 2.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (577, 'Lentilha, cozida', 'Grupo 47', 93.0, 6.3, 16.3, 0.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (578, 'Lentilha, crua', 'Grupo 49', 339.0, 23.2, 62.0, 0.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (579, 'Paçoca, amendoim', 'Grupo 49', 487.0, 16.0, 52.4, 26.1, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (580, 'Pé-de-moleque, amendoim', 'Grupo 49', 503.0, 13.2, 54.7, 28.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (581, 'Soja, farinha', 'Grupo 49', 404.0, 36.0, 38.4, 14.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (582, 'Soja, extrato solúvel, natural, fluido', 'Grupo 49', 39.0, 2.4, 4.3, 1.6, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (583, 'Soja, extrato solúvel, pó', 'Grupo 49', 459.0, 35.7, 28.5, 26.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (584, 'Soja, queijo (tofu)', 'Grupo 49', 64.0, 6.6, 2.1, 4.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (585, 'Tremoço, cru', 'Grupo 49', 381.0, 33.6, 43.8, 10.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (586, 'Tremoço, em conserva', 'Grupo 49', 121.0, 11.1, 12.4, 3.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (587, 'Amêndoa, torrada, salgada', 'Grupo 49', 581.0, 18.6, 29.5, 47.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (588, 'Castanha-de-caju, torrada, salgada', 'Grupo 49', 570.0, 18.5, 29.1, 46.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (589, 'Castanha-do-Brasil, crua', 'Grupo 49', 643.0, 14.5, 15.1, 63.5, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (590, 'Coco, cru', 'Grupo 49', 406.0, 3.7, 10.4, 42.0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (591, 'Coco,  verde, cru', 'Grupo 49', 0, 0, 0, 0, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (592, 'Farinha, de mesocarpo de babaçu, crua', 'Grupo 49', 329.0, 1.4, 79.2, 0.2, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (593, 'Gergelim, semente', 'Grupo 49', 584.0, 21.2, 21.6, 50.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (594, 'Linhaça, semente', 'Grupo 49', 495.0, 14.1, 43.3, 32.3, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (595, 'Pinhão, cozido', 'Grupo 49', 174.0, 3.0, 43.9, 0.7, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (596, 'Pupunha, cozida', 'Grupo 49', 219.0, 2.5, 29.6, 12.8, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    (597, 'Noz, crua', 'Grupo 49', 620.0, 14.0, 18.4, 59.4, 0)
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;

-- Verificação
SELECT COUNT(*) AS total_alimentos FROM alimentos;

COMMIT;