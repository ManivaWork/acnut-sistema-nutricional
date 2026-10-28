BEGIN;

-- Adicionar constraint única para permitir ON CONFLICT (upsert) em consentimentos
-- Isso permite atualizar o consentimento (aceito=true/false) sem duplicatas
ALTER TABLE consentimentos
    ADD CONSTRAINT uq_consentimentos_empresa_paciente_tipo_versao
    UNIQUE (empresa_id, paciente_id, tipo, versao_texto);

COMMIT;