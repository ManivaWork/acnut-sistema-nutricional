-- Seed: Admin Bybar (admin@bybar.com / @Admin122)
-- Idempotente: só insere se não existir
-- Execute no banco smart_nutri_db
-- Revisar e rodar com COMMIT se quiser persistir (padrão é ROLLBACK)

BEGIN;

-- 1. Verifica/cria empresa base
DO $$
DECLARE
    v_empresa_id INT;
BEGIN
    SELECT id INTO v_empresa_id FROM empresas WHERE nome = 'ACNUTRI Local' LIMIT 1;

    IF v_empresa_id IS NULL THEN
        INSERT INTO empresas (nome) VALUES ('ACNUTRI Local') RETURNING id INTO v_empresa_id;
        INSERT INTO empresa_modulos (empresa_id, modulo) VALUES (v_empresa_id, 'nutricao') ON CONFLICT DO NOTHING;
    END IF;

    -- 2. Insere admin se não existir
    IF NOT EXISTS (SELECT 1 FROM usuarios WHERE lower(email) = 'admin@bybar.com') THEN
        INSERT INTO usuarios (empresa_id, nome, email, senha_hash, perfil, ativo)
        VALUES (v_empresa_id, 'Admin Bybar', 'admin@bybar.com', '$2b$12$5sHtBoLD/bhiJlCg5.8AAuvn0kTRYuYRvmpFiPmXweNHdvnJNuHJi', 'super_admin', true);

        -- 3. Permissão total no módulo nutrição
        INSERT INTO usuario_modulos (usuario_id, modulo, nivel)
        SELECT id, 'nutricao', 'total' FROM usuarios WHERE lower(email) = 'admin@bybar.com'
        ON CONFLICT DO NOTHING;
    END IF;
END $$;

-- Verificação (apenas leitura)
SELECT 'Empresa' as tipo, id, nome FROM empresas WHERE nome = 'ACNUTRI Local'
UNION ALL
SELECT 'Usuario', id, email FROM usuarios WHERE lower(email) = 'admin@bybar.com';

ROLLBACK; -- Altere para COMMIT para persistir