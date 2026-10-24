-- =====================================================================
-- MIGRAÇÃO ASSINATURAS (BILLING ASAAS): assinaturas + webhook events
-- Idempotente, transacional, sem DROP/DELETE/TRUNCATE.
--
-- ATENÇÃO: o último comando é ROLLBACK (modo de teste).
-- Depois de conferir os resultados, troque ROLLBACK por COMMIT
-- e rode o script de novo para gravar de verdade.
-- =====================================================================

BEGIN;

-- ---------------------------------------------------------------------
-- 1. TABELA ASSINATURAS (uma por empresa)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS assinaturas (
    id                      BIGSERIAL PRIMARY KEY,
    empresa_id              BIGINT NOT NULL UNIQUE REFERENCES empresas(id) ON DELETE CASCADE,
    asaas_customer_id       VARCHAR(100),                        -- ID do customer no Asaas
    asaas_subscription_id   VARCHAR(100) UNIQUE,                 -- ID da subscription no Asaas
    plano                   VARCHAR(50) NOT NULL DEFAULT 'basico'  -- basico, profissional, enterprise
        CHECK (plano IN ('basico','profissional','enterprise')),
    status                  VARCHAR(30) NOT NULL DEFAULT 'inativa' -- inativa, ativa, past_due, cancelada, trial
        CHECK (status IN ('inativa','ativa','past_due','cancelada','trial')),
    valor_centavos          INTEGER NOT NULL DEFAULT 0,           -- valor mensal em centavos
    ciclo                   VARCHAR(20) NOT NULL DEFAULT 'monthly' -- monthly, yearly
        CHECK (ciclo IN ('monthly','yearly')),
    trial_fim               TIMESTAMPTZ,                          -- fim do trial (se houver)
    inicio_vigencia         TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    fim_vigencia            TIMESTAMPTZ,                          -- NULL = indeterminado
    cancelada_em            TIMESTAMPTZ,
    criado_em               TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    atualizado_em           TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE assinaturas IS 'Assinaturas de planos por empresa (integração Asaas)';
COMMENT ON COLUMN assinaturas.asaas_customer_id IS 'ID do customer no Asaas';
COMMENT ON COLUMN assinaturas.asaas_subscription_id IS 'ID da subscription no Asaas';
COMMENT ON COLUMN assinaturas.plano IS 'basico | profissional | enterprise';
COMMENT ON COLUMN assinaturas.status IS 'inativa | ativa | past_due | cancelada | trial';
COMMENT ON COLUMN assinaturas.valor_centavos IS 'Valor mensal em centavos (ex: 9700 = R$ 97,00)';
COMMENT ON COLUMN assinaturas.trial_fim IS 'Data fim do trial gratuito (se aplicável)';

-- ---------------------------------------------------------------------
-- 2. ÍNDICES PARCIAIS (performance em lookups Asaas)
-- ---------------------------------------------------------------------
CREATE INDEX IF NOT EXISTS idx_assinaturas_asaas_customer
    ON assinaturas (asaas_customer_id) WHERE asaas_customer_id IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_assinaturas_asaas_subscription
    ON assinaturas (asaas_subscription_id) WHERE asaas_subscription_id IS NOT NULL;

-- ---------------------------------------------------------------------
-- 3. TRIGGER PARA atualizado_em
-- ---------------------------------------------------------------------
CREATE OR REPLACE FUNCTION set_updated_at()
RETURNS TRIGGER LANGUAGE plpgsql AS $$
BEGIN
    NEW.atualizado_em = CURRENT_TIMESTAMP;
    RETURN NEW;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_trigger WHERE tgname = 'trg_assinaturas_updated_at'
    ) THEN
        CREATE TRIGGER trg_assinaturas_updated_at
        BEFORE UPDATE ON assinaturas
        FOR EACH ROW EXECUTE FUNCTION set_updated_at();
    END IF;
END $$;

-- ---------------------------------------------------------------------
-- 4. TABELA DE EVENTOS DE WEBHOOK (auditoria / idempotência)
-- ---------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS asaas_webhook_events (
    id              BIGSERIAL PRIMARY KEY,
    event_type      VARCHAR(100) NOT NULL,
    asaas_event_id  VARCHAR(100) NOT NULL UNIQUE,  -- idempotência
    payload_jsonb   JSONB NOT NULL,
    processado_em   TIMESTAMPTZ,
    erro            TEXT,
    criado_em       TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
);

COMMENT ON TABLE asaas_webhook_events IS 'Log de eventos recebidos do webhook Asaas (idempotência + auditoria)';
COMMENT ON COLUMN asaas_webhook_events.asaas_event_id IS 'ID único do evento no Asaas (chave de idempotência)';
COMMENT ON COLUMN asaas_webhook_events.payload_jsonb IS 'Payload completo do evento (JSON)';
COMMENT ON COLUMN asaas_webhook_events.processado_em IS 'Timestamp de processamento bem-sucedido';
COMMENT ON COLUMN asaas_webhook_events.erro IS 'Mensagem de erro se processamento falhou';

CREATE INDEX IF NOT EXISTS idx_asaas_webhook_customer
    ON asaas_webhook_events ((payload_jsonb->>'customer'));

-- ---------------------------------------------------------------------
-- 5. VIEW CONVENIÊNCIA: assinaturas ativas com limites do plano
-- ---------------------------------------------------------------------
CREATE OR REPLACE VIEW v_assinaturas_ativas AS
SELECT
    a.empresa_id,
    a.plano,
    a.status,
    a.valor_centavos,
    a.ciclo,
    a.trial_fim,
    a.inicio_vigencia,
    a.fim_vigencia,
    CASE a.plano
        WHEN 'basico'       THEN jsonb_build_object('max_pacientes', 50, 'max_profissionais', 2, 'storage_mb', 500, 'max_versoes_plano', 5)
        WHEN 'profissional' THEN jsonb_build_object('max_pacientes', 200, 'max_profissionais', 10, 'storage_mb', 2000, 'max_versoes_plano', 20)
        WHEN 'enterprise'   THEN jsonb_build_object('max_pacientes', 1000, 'max_profissionais', 50, 'storage_mb', 10000, 'max_versoes_plano', 100)
    END AS limites
FROM assinaturas a
WHERE a.status IN ('ativa','trial')
  AND (a.fim_vigencia IS NULL OR a.fim_vigencia > CURRENT_TIMESTAMP);

COMMENT ON VIEW v_assinaturas_ativas IS 'Assinaturas ativas/trial com limites do plano para uso no frontend';

-- ---------------------------------------------------------------------
-- 6. VERIFICAÇÃO
-- ---------------------------------------------------------------------
SELECT 'assinaturas' AS tabela, COUNT(*) AS linhas FROM assinaturas
UNION ALL
SELECT 'asaas_webhook_events', COUNT(*) FROM asaas_webhook_events;

-- >>> MODO TESTE: nada é gravado. Troque por COMMIT; para aplicar de verdade. <<<
ROLLBACK;