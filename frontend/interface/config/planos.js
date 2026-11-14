// ============================================================
// PLANOS_CONFIG - Fonte única de verdade para preços, limites e ciclos
// MODELO NOVO: limites POR PERÍODO (não por plano)
// Mensal (MONTHLY): R$ 27,90 - 100 pacientes, 1 acesso
// Semestral (SEMIANNUALLY): R$ 159,90 - 300 pacientes, 2 acessos
// Anual (YEARLY): R$ 299,90 - Pacientes ilimitado (uso justo), 5 acessos
// ============================================================

window.PLANOS_CONFIG = {
  // Preços por ciclo (em reais) - mesmo valor para todos os "planos" internos
  // O backend ainda usa chaves basico/profissional/enterprise, mas o preço é por ciclo
  precos: {
    mensal: 27.90,
    semestral: 159.90,
    anual: 299.90
  },

  // Limites POR PERÍODO (novo modelo)
  limites: {
    mensal: {
      max_pacientes: 100,
      max_acessos: 1,
      label_pacientes: "100 pacientes",
      label_acessos: "1 acesso"
    },
    semestral: {
      max_pacientes: 300,
      max_acessos: 2,
      label_pacientes: "300 pacientes",
      label_acessos: "2 acessos"
    },
    anual: {
      max_pacientes: -1, // -1 = ilimitado (uso justo)
      max_acessos: 5,
      label_pacientes: "Pacientes ilimitados (uso justo)",
      label_acessos: "5 acessos"
    }
  },

  // Mapeamento ciclo -> label para Asaas
  cicloAsaas: {
    mensal: "MONTHLY",
    semestral: "SEMIANNUALLY",
    anual: "YEARLY"
  },

  // Mapeamento ciclo -> label amigável
  cicloLabels: {
    mensal: "Mensal",
    semestral: "Semestral",
    anual: "Anual"
  },

  // Mapeamento ciclo -> sufixo período
  cicloPeriodo: {
    mensal: "mês",
    semestral: "semestre",
    anual: "ano"
  },

  // ============================================================
  // Helpers de cálculo de economia (dinâmico, sem valores hardcoded)
  // ============================================================
  // Preço base de referência = mensal (R$ 27,90)
  _precoBaseMensal: 27.90,

  /**
   * Calcula porcentagem de economia do ciclo vs plano mensal
   * Compara o equivalente mensal do ciclo com o preço do plano mensal
   * Arredonda PARA BAIXO (floor): 4.5% -> 4%, 10.4% -> 10%
   * @returns {number} porcentagem inteira (ex: 4 para 4%)
   */
  getEconomiaPercentual: function(ciclo) {
    const precoMensalPlano = PLANOS_CONFIG.precos.mensal ?? 27.90;
    const equivMensal = PLANOS_CONFIG.getPrecoMensalEquivalente(ciclo);
    if (precoMensalPlano <= 0 || equivMensal <= 0) return 0;
    const economia = ((precoMensalPlano - equivMensal) / precoMensalPlano) * 100;
    return Math.floor(economia); // Arredonda para baixo (floor)
  },

  /**
   * Retorna label de economia formatado
   * @returns {string} ex: "Economia de 4%"
   */
  getEconomiaLabel: function(ciclo) {
    const pct = PLANOS_CONFIG.getEconomiaPercentual(ciclo);
    if (pct <= 0) return '';
    return `Economia de ${pct}%`;
  },

  // Planos internos (mantidos para compatibilidade com backend)
  // Estes NÃO são mostrados ao usuário final - apenas enviados ao backend
  planosInternos: ['basico', 'profissional', 'enterprise'],
  nomesInternos: {
    basico: 'Básico',
    profissional: 'Profissional',
    enterprise: 'Enterprise'
  },

  // ============================================================
  // MODO SIMULAÇÃO - Frontend apenas, sem backend real
  // true = usa mocks em billingApi.js
  // false = chamadas reais para API (fase backend futura)
  // ============================================================
  MODO_SIMULACAO: true
};

// Tornar PLANOS_CONFIG disponível como variável global (para compatibilidade browser/Node)
var PLANOS_CONFIG = window.PLANOS_CONFIG;

// ============================================================
// Helpers expostos globalmente E métodos do PLANOS_CONFIG
// ============================================================

/**
 * Retorna preço mensal equivalente para comparação
 * Disponível como PLANOS_CONFIG.getPrecoMensalEquivalente() e window.getPrecoMensalEquivalente()
 */
PLANOS_CONFIG.getPrecoMensalEquivalente = function(ciclo) {
  const valor = this.precos[ciclo] ?? 0;
  switch (ciclo) {
    case 'mensal': return valor;
    case 'semestral': return valor / 6;
    case 'anual': return valor / 12;
    default: return valor;
  }
};
window.getPrecoMensalEquivalente = PLANOS_CONFIG.getPrecoMensalEquivalente.bind(PLANOS_CONFIG);

/**
 * Retorna preço formatado (ex: "R$ 27,90/mês")
 */
window.formatarPreco = function(ciclo) {
  const valor = PLANOS_CONFIG.precos[ciclo] ?? 0;
  const periodo = PLANOS_CONFIG.cicloPeriodo[ciclo] || 'mês';
  return `R$ ${valor.toFixed(2).replace('.', ',')}/${periodo}`;
};

/**
 * Retorna limites do ciclo
 */
window.getLimitesCiclo = function(ciclo) {
  return PLANOS_CONFIG.limites[ciclo] || PLANOS_CONFIG.limites.mensal;
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
 * Retorna destaque do ciclo (economia, label) - calculado dinamicamente
 */
window.getDestaqueCiclo = function(ciclo) {
  const economiaLabel = PLANOS_CONFIG.getEconomiaLabel(ciclo);
  if (!economiaLabel) return null;
  return {
    label: economiaLabel
  };
};

/**
 * Retorna label de pacientes (trata ilimitado)
 */
window.getLabelPacientes = function(ciclo) {
  const limites = PLANOS_CONFIG.limites[ciclo];
  return limites?.label_pacientes || `${limites?.max_pacientes || 0} pacientes`;
};

/**
 * Retorna label de acessos
 */
window.getLabelAcessos = function(ciclo) {
  const limites = PLANOS_CONFIG.limites[ciclo];
  return limites?.label_acessos || `${limites?.max_acessos || 0} acessos`;
};