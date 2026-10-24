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
(591, 'unidade (agua)', 300.00, 'aproximado');
SELECT COUNT(*) FROM alimentos_medidas_caseiras;