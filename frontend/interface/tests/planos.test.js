import { describe, it, expect, beforeEach, vi } from 'vitest'

// Helper to load planos.js in jsdom
async function loadPlanosConfig() {
  // Reset modules
  vi.resetModules()

  // Read the planos.js file
  const fs = await import('fs')
  const path = await import('path')
  const filePath = path.resolve('config/planos.js')
  const content = fs.readFileSync(filePath, 'utf-8')

  // Execute in global context to set up window.PLANOS_CONFIG
  const script = new Function(content + '; return window.PLANOS_CONFIG;')
  return script.call(globalThis)
}

describe('PLANOS_CONFIG - Configuração de Planos', () => {
  let PLANOS_CONFIG

  beforeEach(async () => {
    PLANOS_CONFIG = await loadPlanosConfig()
  })

  describe('Preços por ciclo', () => {
    it('deve ter preço mensal R$ 27,90', () => {
      expect(PLANOS_CONFIG.precos.mensal).toBe(27.90)
    })

    it('deve ter preço semestral R$ 159,90', () => {
      expect(PLANOS_CONFIG.precos.semestral).toBe(159.90)
    })

    it('deve ter preço anual R$ 299,90', () => {
      expect(PLANOS_CONFIG.precos.anual).toBe(299.90)
    })
  })

  describe('getPrecoMensalEquivalente', () => {
    it('mensal: equivalente mensal = 27,90', () => {
      const result = PLANOS_CONFIG.getPrecoMensalEquivalente('mensal')
      expect(result).toBe(27.90)
    })

    it('semestral: equivalente mensal = 159,90 / 6 = 26,65', () => {
      const result = PLANOS_CONFIG.getPrecoMensalEquivalente('semestral')
      expect(result).toBeCloseTo(26.65, 2)
    })

    it('anual: equivalente mensal = 299,90 / 12 = 24,99', () => {
      const result = PLANOS_CONFIG.getPrecoMensalEquivalente('anual')
      expect(result).toBeCloseTo(24.9916, 2)
    })

    it('ciclo inválido: retorna 0', () => {
      const result = PLANOS_CONFIG.getPrecoMensalEquivalente('invalido')
      expect(result).toBe(0)
    })
  })

  describe('getEconomiaPercentual', () => {
    it('mensal: economia = 0% (base de comparação)', () => {
      const result = PLANOS_CONFIG.getEconomiaPercentual('mensal')
      expect(result).toBe(0)
    })

    it('semestral: economia = 4% (floor de 4.48%)', () => {
      const result = PLANOS_CONFIG.getEconomiaPercentual('semestral')
      // (27.90 - 26.65) / 27.90 * 100 = 4.48% -> floor = 4%
      expect(result).toBe(4)
    })

    it('anual: economia = 10% (floor de 10.43%)', () => {
      const result = PLANOS_CONFIG.getEconomiaPercentual('anual')
      // (27.90 - 24.99) / 27.90 * 100 = 10.43% -> floor = 10%
      expect(result).toBe(10)
    })

    it('ciclo inválido: retorna 0', () => {
      const result = PLANOS_CONFIG.getEconomiaPercentual('invalido')
      expect(result).toBe(0)
    })
  })

  describe('getEconomiaLabel', () => {
    it('mensal: retorna string vazia (sem economia)', () => {
      const result = PLANOS_CONFIG.getEconomiaLabel('mensal')
      expect(result).toBe('')
    })

    it('semestral: retorna "Economia de 4%"', () => {
      const result = PLANOS_CONFIG.getEconomiaLabel('semestral')
      expect(result).toBe('Economia de 4%')
    })

    it('anual: retorna "Economia de 10%"', () => {
      const result = PLANOS_CONFIG.getEconomiaLabel('anual')
      expect(result).toBe('Economia de 10%')
    })
  })

  describe('Limites por período', () => {
    it('mensal: 100 pacientes, 1 acesso', () => {
      const limites = PLANOS_CONFIG.limites.mensal
      expect(limites.max_pacientes).toBe(100)
      expect(limites.max_acessos).toBe(1)
      expect(limites.label_pacientes).toBe('100 pacientes')
      expect(limites.label_acessos).toBe('1 acesso')
    })

    it('semestral: 300 pacientes, 2 acessos', () => {
      const limites = PLANOS_CONFIG.limites.semestral
      expect(limites.max_pacientes).toBe(300)
      expect(limites.max_acessos).toBe(2)
      expect(limites.label_pacientes).toBe('300 pacientes')
      expect(limites.label_acessos).toBe('2 acessos')
    })

    it('anual: pacientes ilimitado (-1), 5 acessos', () => {
      const limites = PLANOS_CONFIG.limites.anual
      expect(limites.max_pacientes).toBe(-1)
      expect(limites.max_acessos).toBe(5)
      expect(limites.label_pacientes).toBe('Pacientes ilimitados (uso justo)')
      expect(limites.label_acessos).toBe('5 acessos')
    })
  })

  describe('Helpers globais (window)', () => {
    it('window.getPrecoMensalEquivalente deve existir e funcionar', () => {
      expect(typeof window.getPrecoMensalEquivalente).toBe('function')
      expect(window.getPrecoMensalEquivalente('mensal')).toBe(27.90)
      expect(window.getPrecoMensalEquivalente('semestral')).toBeCloseTo(26.65, 2)
      expect(window.getPrecoMensalEquivalente('anual')).toBeCloseTo(24.99, 2)
    })

    it('window.formatarPreco deve formatar corretamente', () => {
      expect(typeof window.formatarPreco).toBe('function')
      expect(window.formatarPreco('mensal')).toBe('R$ 27,90/mês')
      expect(window.formatarPreco('semestral')).toBe('R$ 159,90/semestre')
      expect(window.formatarPreco('anual')).toBe('R$ 299,90/ano')
    })

    it('window.getLimitesCiclo deve retornar limites corretos', () => {
      expect(typeof window.getLimitesCiclo).toBe('function')
      expect(window.getLimitesCiclo('mensal').max_pacientes).toBe(100)
      expect(window.getLimitesCiclo('semestral').max_pacientes).toBe(300)
      expect(window.getLimitesCiclo('anual').max_pacientes).toBe(-1)
    })

    it('window.getLabelPacientes deve formatar labels', () => {
      expect(typeof window.getLabelPacientes).toBe('function')
      expect(window.getLabelPacientes('mensal')).toBe('100 pacientes')
      expect(window.getLabelPacientes('semestral')).toBe('300 pacientes')
      expect(window.getLabelPacientes('anual')).toBe('Pacientes ilimitados (uso justo)')
    })

    it('window.getLabelAcessos deve formatar labels', () => {
      expect(typeof window.getLabelAcessos).toBe('function')
      expect(window.getLabelAcessos('mensal')).toBe('1 acesso')
      expect(window.getLabelAcessos('semestral')).toBe('2 acessos')
      expect(window.getLabelAcessos('anual')).toBe('5 acessos')
    })

    it('window.getCicloLabel deve retornar labels amigáveis', () => {
      expect(typeof window.getCicloLabel).toBe('function')
      expect(window.getCicloLabel('mensal')).toBe('Mensal')
      expect(window.getCicloLabel('semestral')).toBe('Semestral')
      expect(window.getCicloLabel('anual')).toBe('Anual')
    })

    it('window.getDestaqueCiclo deve retornar economia para semestral/anual', () => {
      expect(typeof window.getDestaqueCiclo).toBe('function')
      expect(window.getDestaqueCiclo('mensal')).toBeNull()
      expect(window.getDestaqueCiclo('semestral')).toEqual({ label: 'Economia de 4%' })
      expect(window.getDestaqueCiclo('anual')).toEqual({ label: 'Economia de 10%' })
    })

    it('window.isModoSimulacao deve retornar true', () => {
      expect(typeof window.isModoSimulacao).toBe('function')
      expect(window.isModoSimulacao()).toBe(true)
    })
  })
})