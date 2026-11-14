// ============================================================
// billingApi.js - Camada única de acesso à API de Billing
// MODO_SIMULACAO lido de window.PLANOS_CONFIG.MODO_SIMULACAO
// TODO: Substituir mocks por fetch reais na fase backend
// ============================================================

(function() {
  'use strict';

  // ============================================================
  // Estado interno da simulação
  // ============================================================
  const simulacaoState = {
    preCadastros: new Map(),    // ref -> { dados, status, invoiceUrl, criarEmpresaToken }
    pollingCallbacks: new Map() // ref -> callback
  };

  // ============================================================
  // Utilitários
  // ============================================================
  function gerarRef() {
    // UUID v4 simples para simulação
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  function delay(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }

  function getModoSimulacao() {
    return window.PLANOS_CONFIG?.MODO_SIMULACAO === true;
  }

  function mostrarToastSimulacao(mensagem, tipo = 'info') {
    if (window.mostrarToast) {
      window.mostrarToast(`[SIMULAÇÃO] ${mensagem}`, tipo);
    } else {
      console.log(`[SIMULAÇÃO] ${mensagem}`);
    }
  }

  // ============================================================
  // MOCKS DE DADOS (Super Admin)
  // ============================================================
  const MOCK_ASSINATURAS = [
    {
      empresa_id: 1,
      empresa_nome: 'Clínica NutriVida',
      plano: 'profissional',
      ciclo: 'mensal',
      valor_centavos: 2790,
      status: 'ativa',
      inicio_vigencia: '2025-01-15T00:00:00Z',
      fim_vigencia: '2025-02-15T00:00:00Z',
      asaas_customer_id: 'cus_abc123',
      asaas_subscription_id: 'sub_xyz789'
    },
    {
      empresa_id: 2,
      empresa_nome: 'NutriClínica São Paulo',
      plano: 'basico',
      ciclo: 'anual',
      valor_centavos: 29990,
      status: 'ativa',
      inicio_vigencia: '2024-12-01T00:00:00Z',
      fim_vigencia: '2025-12-01T00:00:00Z',
      asaas_customer_id: 'cus_def456',
      asaas_subscription_id: 'sub_uvw012'
    },
    {
      empresa_id: 3,
      empresa_nome: 'Bem Estar Nutrição',
      plano: 'enterprise',
      ciclo: 'semestral',
      valor_centavos: 15990,
      status: 'past_due',
      inicio_vigencia: '2025-01-01T00:00:00Z',
      fim_vigencia: '2025-07-01T00:00:00Z',
      asaas_customer_id: 'cus_ghi789',
      asaas_subscription_id: 'sub_rst345'
    },
    {
      empresa_id: 4,
      empresa_nome: 'Vida Leve Clinics',
      plano: 'profissional',
      ciclo: 'mensal',
      valor_centavos: 2790,
      status: 'cancelada',
      inicio_vigencia: '2024-11-01T00:00:00Z',
      fim_vigencia: '2025-01-15T00:00:00Z',
      asaas_customer_id: 'cus_jkl012',
      asaas_subscription_id: 'sub_mno678'
    }
  ];

  const MOCK_WEBHOOK_EVENTS = [
    {
      id: 1,
      event_type: 'PAYMENT_RECEIVED',
      asaas_event_id: 'evt_abc123',
      payload_jsonb: { payment: { id: 'pay_001', value: 2790, status: 'RECEIVED' } },
      processado_em: '2025-01-15T10:30:00Z',
      erro: null,
      criado_em: '2025-01-15T10:29:45Z'
    },
    {
      id: 2,
      event_type: 'SUBSCRIPTION_CREATED',
      asaas_event_id: 'evt_def456',
      payload_jsonb: { subscription: { id: 'sub_xyz789', status: 'ACTIVE' } },
      processado_em: '2025-01-15T10:25:00Z',
      erro: null,
      criado_em: '2025-01-15T10:24:50Z'
    },
    {
      id: 3,
      event_type: 'PAYMENT_OVERDUE',
      asaas_event_id: 'evt_ghi789',
      payload_jsonb: { payment: { id: 'pay_002', value: 2790, status: 'OVERDUE' } },
      processado_em: '2025-01-20T08:00:00Z',
      erro: null,
      criado_em: '2025-01-20T07:59:55Z'
    },
    {
      id: 4,
      event_type: 'SUBSCRIPTION_CANCELLED',
      asaas_event_id: 'evt_jkl012',
      payload_jsonb: { subscription: { id: 'sub_mno678', status: 'CANCELLED' } },
      processado_em: '2025-01-15T15:00:00Z',
      erro: null,
      criado_em: '2025-01-15T14:59:50Z'
    }
  ];

  // ============================================================
  // API PÚBLICA
  // ============================================================
  window.billingApi = {
    // -----------------------------------------------------------------
    // (a) Criar pré-cadastro + iniciar assinatura Asaas
    // Input: { plano, ciclo, nome, email, cpfCnpj, telefone, clinica, termosAceitos }
    // Output: { ref, invoiceUrl, status: 'aguardando_pagamento' }
    // -----------------------------------------------------------------
    async criarPreCadastroEAssinatura(dados) {
      console.log('[billingApi] criarPreCadastroEAssinatura', dados);

      if (!getModoSimulacao()) {
        // TODO: Substituir por fetch real
        // const res = await fetch(`${API_URL}/public/pre-cadastros`, { ... });
        // return res.json();
        throw new Error('Modo real não implementado ainda');
      }

      // Validações básicas
      if (!dados.plano || !PLANOS_CONFIG.precos[dados.plano]) {
        throw new Error('Plano inválido');
      }
      if (!dados.ciclo || !PLANOS_CONFIG.ciclos.includes(dados.ciclo)) {
        throw new Error('Ciclo inválido');
      }
      if (!dados.termosAceitos) {
        throw new Error('É necessário aceitar os Termos e a LGPD');
      }

      // Simular latência de rede
      await delay(800);

      const ref = gerarRef();
      const plano = dados.plano;
      const ciclo = dados.ciclo;
      const valor = PLANOS_CONFIG.precos[plano][ciclo];

      // Simular invoiceUrl do Asaas (em produção vem da API)
      const invoiceUrl = `https://sandbox.asaas.com/i/${ref}/invoice`;

      // Salvar pré-cadastro
      simulacaoState.preCadastros.set(ref, {
        dados: { ...dados },
        status: 'aguardando_pagamento',
        invoiceUrl,
        criadoEm: Date.now(),
        // Token para "Definir Minha Senha" (gerado só após pagamento confirmado)
        criarEmpresaToken: null
      });

      mostrarToastSimulacao(`Pré-cadastro criado (ref: ${ref.slice(0,8)}...). Redirecionando para pagamento...`, 'sucesso');

      return {
        ref,
        invoiceUrl,
        status: 'aguardando_pagamento',
        valor,
        plano,
        ciclo
      };
    },

    // -----------------------------------------------------------------
    // (c) Polling de status do pré-cadastro
    // Input: ref (string)
    // Output: { status: 'aguardando_pagamento' | 'pago' | 'expirado' | 'recusado', definirSenhaToken? }
    // -----------------------------------------------------------------
    async consultarStatus(ref) {
      console.log('[billingApi] consultarStatus', ref);

      if (!getModoSimulacao()) {
        // TODO: Substituir por fetch real
        // const res = await fetch(`${API_URL}/public/pre-cadastros/${ref}/status`);
        // return res.json();
        throw new Error('Modo real não implementado ainda');
      }

      await delay(300);

      const preCadastro = simulacaoState.preCadastros.get(ref);
      if (!preCadastro) {
        return { status: 'expirado', mensagem: 'Pré-cadastro não encontrado ou expirado' };
      }

      // Verificar expiração (15 min para simulação)
      if (Date.now() - preCadastro.criadoEm > 15 * 60 * 1000) {
        preCadastro.status = 'expirado';
        return { status: 'expirado', mensagem: 'Tempo para pagamento expirou. Inicie novamente.' };
      }

      // Em simulação: se passou 3s desde criação, considerar "pago" para testar fluxo
      // TODO: Em produção, status vem do webhook real
      if (preCadastro.status === 'aguardando_pagamento' && Date.now() - preCadastro.criadoEm > 3000) {
        preCadastro.status = 'pago';
        // Gerar token único para "Definir Minha Senha"
        const token = `definir_senha_${gerarRef()}`;
        preCadastro.criarEmpresaToken = token;
        return {
          status: 'pago',
          definirSenhaToken: token,
          mensagem: 'Pagamento confirmado! Crie seu acesso.'
        };
      }

      return {
        status: preCadastro.status,
        invoiceUrl: preCadastro.invoiceUrl
      };
    },

    // -----------------------------------------------------------------
    // Simular pagamento confirmado (para testes manuais)
    // -----------------------------------------------------------------
    async simularPagamentoConfirmado(ref) {
      const preCadastro = simulacaoState.preCadastros.get(ref);
      if (!preCadastro) return { error: 'Não encontrado' };

      preCadastro.status = 'pago';
      const token = `definir_senha_${gerarRef()}`;
      preCadastro.criarEmpresaToken = token;

      return { status: 'pago', definirSenhaToken: token };
    },

    // -----------------------------------------------------------------
    // Simular pagamento recusado
    // -----------------------------------------------------------------
    async simularPagamentoRecusado(ref) {
      const preCadastro = simulacaoState.preCadastros.get(ref);
      if (!preCadastro) return { error: 'Não encontrado' };

      preCadastro.status = 'recusado';
      return { status: 'recusado', mensagem: 'Pagamento recusado. Tente outro método.' };
    },

    // -----------------------------------------------------------------
    // Super Admin: Listar todas assinaturas
    // TODO: Substituir por GET /api/billing/assinaturas/todas
    // -----------------------------------------------------------------
    async listarAssinaturas() {
      console.log('[billingApi] listarAssinaturas');

      if (!getModoSimulacao()) {
        // TODO: const res = await fetch(`${API_URL}/billing/assinaturas/todas`, { headers: { Authorization: `Bearer ${getToken()}` } });
        throw new Error('Modo real não implementado ainda');
      }

      await delay(400);
      return [...MOCK_ASSINATURAS];
    },

    // -----------------------------------------------------------------
    // Super Admin: Listar eventos webhook
    // TODO: Substituir por GET /api/billing/webhook-log
    // -----------------------------------------------------------------
    async listarWebhookEvents() {
      console.log('[billingApi] listarWebhookEvents');

      if (!getModoSimulacao()) {
        // TODO: const res = await fetch(`${API_URL}/billing/webhook-log`, { headers: { Authorization: `Bearer ${getToken()}` } });
        throw new Error('Modo real não implementado ainda');
      }

      await delay(300);
      return [...MOCK_WEBHOOK_EVENTS];
    },

    // -----------------------------------------------------------------
    // Helpers internos (expostos para debugging)
    // -----------------------------------------------------------------
    _simulacao: {
      getState: () => simulacaoState,
      reset: () => {
        simulacaoState.preCadastros.clear();
        simulacaoState.pollingCallbacks.clear();
      },
      // Para testes: forçar status
      setStatus: (ref, status, token = null) => {
        const pc = simulacaoState.preCadastros.get(ref);
        if (pc) {
          pc.status = status;
          if (token) pc.criarEmpresaToken = token;
        }
      }
    }
  };

  // ============================================================
  // Faixa visual "MODO SIMULAÇÃO" - FIXA no topo da viewport
  // ============================================================
  function criarFaixaSimulacao() {
    if (!getModoSimulacao()) return;

    // Evitar duplicata
    if (document.getElementById('faixa-simulacao')) return;

    const faixa = document.createElement('div');
    faixa.id = 'faixa-simulacao';
    faixa.innerHTML = '⚠ MODO SIMULAÇÃO ATIVO - Nenhum pagamento real é processado';

    // Inserir no body (fixed position, fora do layout)
    document.body.insertBefore(faixa, document.body.firstChild);
    document.body.classList.add('modo-simulacao');
  }

  // Auto-inicializar faixa quando DOM pronto
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', criarFaixaSimulacao);
  } else {
    criarFaixaSimulacao();
  }

  // Expor para debugging
  window.__billingApiSimulacao = window.billingApi._simulacao;

})();