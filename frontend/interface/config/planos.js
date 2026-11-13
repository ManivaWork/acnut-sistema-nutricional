// ============================================================
// PLANOS_CONFIG - Fonte única de verdade para preços, limites e ciclos
// VALORES DEVEM ESPELHAR EXATAMENTE BillingModule.cs (Planos.Precos + Planos.Limites)
// Mensal: 27,90 | Semestral: 159,90 | Anual: 299,90
// ============================================================

window.PLANOS_CONFIG = {
  // Preços por plano e ciclo (em reais)
  precos: {
    basico: {
      mensal: 27.90,
      semestral: 159.90,
      anual: 299.90
    },
    profissional: {
      mensal: 27.90,
      semestral: 159.90,
      anual: 299.90
    },
    enterprise: {
      mensal: 27.90,
      semestral: 159.90,
      anual: 299.90
    }
  },

  // Limites por plano (compatível com snake_case do backend)
  limites: {
    basico: {
      max_pacientes: 50,
      max_profissionais: 2,
      storage_mb: 500,
      max_versoes_plano: 5
    },
    profissional: {
      max_pacientes: 200,
      max_profissionais: 10,
      storage_mb: 2000,
      max_versoes_plano: 20
    },
    enterprise: {
      max_pacientes: 1000,
      max_profissionais: 50,
      storage_mb: 10000,
      max_versoes_plano: 100
    }
  },

  // Destaques visuais para ciclos anuais/semestrais
  destaque: {
    semestral: {
      economia: "15%",
      label: "Economia de 15%"
    },
    anual: {
      economia: "25%",
      label: "Melhor custo-benefício"
    }
  },

  // Ordem de exibição dos cards
  planosOrdem: ['basico', 'profissional', 'enterprise'],

  // Nomes amigáveis
  nomes: {
    basico: 'Básico',
    profissional: 'Profissional',
    enterprise: 'Enterprise'
  },

  // Descrições curtas
  descricoes: {
    basico: 'Ideal para nutricionistas individuais começando',
    profissional: 'Para clínicas em crescimento com equipe',
    enterprise: 'Para grandes clínicas e redes de atendimento'
  },

  // Ciclos válidos
  ciclos: ['mensal', 'semestral', 'anual'],

  // Mapeamento ciclo -> label
  cicloLabels: {
    mensal: 'Mensal',
    semestral: 'Semestral',
    anual: 'Anual'
  },

  // Mapeamento ciclo -> sufixo período
  cicloPeriodo: {
    mensal: 'mês',
    semestral: 'semestre',
    anual: 'ano'
  },

  // ============================================================
  // MODO SIMULAÇÃO - Frontend apenas, sem backend real
  // true = usa mocks em billingApi.js
  // false = chamadas reais para API (fase backend futura)
  // ============================================================
  MODO_SIMULACAO: true
};

// ============================================================
// Helpers expostos globalmente
// ============================================================

/**
 * Retorna preço formatado (ex: "R$ 27,90/mês")
 */
window.formatarPreco = function(plano, ciclo) {
  const valor = PLANOS_CONFIG.precos[plano]?.[ciclo] ?? 0;
  const periodo = PLANOS_CONFIG.cicloPeriodo[ciclo] || 'mês';
  return `R$ ${valor.toFixed(2).replace('.', ',')}/${periodo}`;
};

/**
 * Retorna preço mensal equivalente para comparação
 */
window.getPrecoMensalEquivalente = function(plano, ciclo) {
  const valor = PLANOS_CONFIG.precos[plano]?.[ciclo] ?? 0;
  switch (ciclo) {
    case 'mensal': return valor;
    case 'semestral': return valor / 6;
    case 'anual': return valor / 12;
    default: return valor;
  }
};

/**
 * Retorna limites do plano
 */
window.getLimitesPlano = function(plano) {
  return PLANOS_CONFIG.limites[plano] || PLANOS_CONFIG.limites.basico;
};

/**
 * Verifica se está em modo simulação
 */
window.isModoSimulacao = function() {
  return PLANOS_CONFIG.MODO_SIMULACAO === true;
};

/**
 * Retorna label do ciclo
 */
window.getCicloLabel = function(ciclo) {
  return PLANOS_CONFIG.cicloLabels[ciclo] || ciclo;
};

/**
 * Retorna destaque do ciclo (economia, label)
 */
window.getDestaqueCiclo = function(ciclo) {
  return PLANOS_CONFIG.destaque[ciclo] || null;
};