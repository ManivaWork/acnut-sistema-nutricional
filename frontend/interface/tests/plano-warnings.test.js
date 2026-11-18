import { describe, it, expect, vi, beforeEach } from 'vitest'
import { obterAvisoPlano, gerarChaveDismiss, lerDispensados, marcarDispensado, montarMensagemLimite } from '../js/plano-warnings.js'

describe('obterAvisoPlano - Lógica pura de avisos de plano', () => {
    const baseParams = {
        perfil: 'profissional',
        limite: 5,
        totalPacientes: 3,
        statusAssinatura: 'ativa',
        diasParaRenovar: null,
        dispensados: { limite: null, assinatura: null, renovacao: null }
    }

    beforeEach(() => {
        vi.clearAllMocks()
        localStorage.clear()
    })

    describe('Perfis que NÃO veem avisos', () => {
        it('retorna null para super_admin', () => {
            const result = obterAvisoPlano({ ...baseParams, perfil: 'super_admin' })
            expect(result).toBeNull()
        })

        it('retorna null para admin', () => {
            const result = obterAvisoPlano({ ...baseParams, perfil: 'admin' })
            expect(result).toBeNull()
        })

        it('retorna null para atendente', () => {
            const result = obterAvisoPlano({ ...baseParams, perfil: 'atendente' })
            expect(result).toBeNull()
        })

        it('retorna null para paciente', () => {
            const result = obterAvisoPlano({ ...baseParams, perfil: 'paciente' })
            expect(result).toBeNull()
        })

        it('retorna null para perfil desconhecido', () => {
            const result = obterAvisoPlano({ ...baseParams, perfil: 'desconhecido' })
            expect(result).toBeNull()
        })
    })

    describe('Aviso: LIMITE ATINGIDO', () => {
        it('retorna aviso erro nível 1 quando total == limite (5/5)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 5,
                limite: 5
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('limite')
            expect(result.severidade).toBe('erro')
            expect(result.mensagem).toContain('atingiu o limite de 5 pacientes')
            expect(result.mensagem).toContain('fale com o suporte')
            expect(result.assinaturaDoEstado).toEqual({ limite: 5, nivel: 1 })
        })

        it('retorna aviso erro nível 2 quando total > limite (6/5)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 6,
                limite: 5
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('limite')
            expect(result.severidade).toBe('erro')
            expect(result.mensagem).toContain('excedeu o limite')
            expect(result.mensagem).toContain('6/5')
            expect(result.assinaturaDoEstado).toEqual({ limite: 5, nivel: 2 })
        })

        it('NÃO retorna aviso quando total < limite (4/5)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 4,
                limite: 5
            })
            expect(result).toBeNull()
        })

        it('NÃO retorna aviso quando limite é null (ilimitado)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 100,
                limite: null
            })
            expect(result).toBeNull()
        })

        it('NÃO retorna aviso quando limite é 0 (ilimitado)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 100,
                limite: 0
            })
            expect(result).toBeNull()
        })

        it('NÃO retorna aviso se já dispensado para mesmo nível e mesmo limite (5/5 dispensado -> 5/5 não reaparece)', () => {
            const dispensados = { limite: { limite: 5, nivel: 1 }, assinatura: null, renovacao: null }
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 5,
                limite: 5,
                dispensados
            })
            expect(result).toBeNull()
        })

        it('REAPARECE se piorar: 5/5 dispensado -> 6/5 reaparece (nível 2 > nível 1)', () => {
            const dispensados = { limite: { limite: 5, nivel: 1 }, assinatura: null, renovacao: null }
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 6,
                limite: 5,
                dispensados
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('limite')
            expect(result.assinaturaDoEstado.nivel).toBe(2)
        })

        it('REAPARECE se limite mudou: 5/5 com limite 5 dispensado -> limite mudou para 3 e total 3 reaparece', () => {
            const dispensados = { limite: { limite: 5, nivel: 1 }, assinatura: null, renovacao: null }
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 3,
                limite: 3,
                dispensados
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('limite')
            expect(result.assinaturaDoEstado.limite).toBe(3)
        })

        it('NÃO reaparece se melhorar: 6/5 dispensado -> 5/5 (mesmo nível 1 ou 2, não piorou)', () => {
            // 6/5 = nível 2
            const dispensados = { limite: { limite: 5, nivel: 2 }, assinatura: null, renovacao: null }
            // Agora 5/5 = nível 1 (melhorou)
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 5,
                limite: 5,
                dispensados
            })
            expect(result).toBeNull()
        })

        it('NÃO reaparece se mesmo estado: 5/5 dispensado -> exclui 1 -> 4/5 -> cadastra -> 5/5', () => {
            const dispensados = { limite: { limite: 5, nivel: 1 }, assinatura: null, renovacao: null }

            // 5/5 de novo - mesmo estado, não reaparece
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 5,
                limite: 5,
                dispensados
            })
            expect(result).toBeNull()
        })
    })

    describe('Aviso: ASSINATURA PROBLEMÁTICA (apenas pendente, vencida, cancelada, past_due)', () => {
        it('retorna aviso para status "pendente" (gravidade 1)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'pendente'
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('assinatura')
            expect(result.severidade).toBe('aviso')
            expect(result.assinaturaDoEstado).toEqual({ status: 'pendente' })
        })

        it('retorna aviso para status "past_due" (gravidade 2)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'past_due'
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado).toEqual({ status: 'past_due' })
        })

        it('retorna aviso para status "vencida" (gravidade 2)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'vencida'
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado).toEqual({ status: 'vencida' })
        })

        it('retorna aviso para status "cancelada" (gravidade 3)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'cancelada'
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado).toEqual({ status: 'cancelada' })
        })

        it('NÃO retorna aviso para status "ativa"', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'ativa'
            })
            expect(result).toBeNull()
        })

        it('NÃO retorna aviso para status "trial"', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'trial'
            })
            expect(result).toBeNull()
        })

        it('NÃO retorna aviso para status "sem_plano" (sem_plano não gera aviso de assinatura)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'sem_plano'
            })
            expect(result).toBeNull()
        })

        it('NÃO retorna aviso se já dispensado para mesmo status (vencida dispensada -> vencida não reaparece)', () => {
            const dispensados = { limite: null, assinatura: { status: 'vencida' }, renovacao: null }
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'vencida',
                dispensados
            })
            expect(result).toBeNull()
        })

        it('vencida -> pendente NÃO reaparece (melhorou: gravidade 2 -> 1)', () => {
            const dispensados = { limite: null, assinatura: { status: 'vencida' }, renovacao: null }
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'pendente',
                dispensados
            })
            expect(result).toBeNull()
        })

        it('pendente -> vencida REAPARECE (piorou: gravidade 1 -> 2)', () => {
            const dispensados = { limite: null, assinatura: { status: 'pendente' }, renovacao: null }
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'vencida',
                dispensados
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('assinatura')
            expect(result.assinaturaDoEstado.status).toBe('vencida')
        })

        it('pendente -> cancelada REAPARECE (piorou: gravidade 1 -> 3)', () => {
            const dispensados = { limite: null, assinatura: { status: 'pendente' }, renovacao: null }
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'cancelada',
                dispensados
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado.status).toBe('cancelada')
        })

        it('past_due -> cancelada REAPARECE (piorou: gravidade 2 -> 3)', () => {
            const dispensados = { limite: null, assinatura: { status: 'past_due' }, renovacao: null }
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'cancelada',
                dispensados
            })
            expect(result).not.toBeNull()
        })
    })

    describe('Aviso: RENOVAÇÃO PRÓXIMA (≤ 7 dias)', () => {
        it('retorna aviso para 7 dias (faixa 7, gravidade 1)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 7
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('renovacao')
            expect(result.severidade).toBe('aviso')
            expect(result.mensagem).toContain('7 dia')
            expect(result.assinaturaDoEstado).toEqual({ faixaDias: 7 })
        })

        it('retorna aviso para 5 dias (faixa 7, gravidade 1)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 5
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado).toEqual({ faixaDias: 7 })
        })

        it('retorna aviso para 3 dias (faixa 3, gravidade 2)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 3
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado).toEqual({ faixaDias: 3 })
        })

        it('retorna aviso para 2 dias (faixa 3, gravidade 2)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 2
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado).toEqual({ faixaDias: 3 })
        })

        it('retorna aviso para 1 dia (faixa 1, gravidade 3)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 1
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado).toEqual({ faixaDias: 1 })
        })

        it('NÃO retorna aviso para 8 dias (> 7)', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 8
            })
            expect(result).toBeNull()
        })

        it('NÃO retorna aviso para diasParaRenovar null', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: null
            })
            expect(result).toBeNull()
        })

        it('NÃO retorna aviso para diasParaRenovar 0', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 0
            })
            expect(result).toBeNull()
        })

        it('NÃO retorna aviso se já dispensado para mesma faixa (7 dias dispensado -> 7 dias não reaparece)', () => {
            const dispensados = { limite: null, assinatura: null, renovacao: { faixaDias: 7 } }
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 7,
                dispensados
            })
            expect(result).toBeNull()
        })

        it('faixa 7 -> 3 REAPARECE (piorou: gravidade 1 -> 2)', () => {
            const dispensados = { limite: null, assinatura: null, renovacao: { faixaDias: 7 } }
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 3,
                dispensados
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado.faixaDias).toBe(3)
        })

        it('faixa 3 -> 1 REAPARECE (piorou: gravidade 2 -> 3)', () => {
            const dispensados = { limite: null, assinatura: null, renovacao: { faixaDias: 3 } }
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 1,
                dispensados
            })
            expect(result).not.toBeNull()
            expect(result.assinaturaDoEstado.faixaDias).toBe(1)
        })

        it('faixa 3 -> 7 NÃO reaparece (melhorou: gravidade 2 -> 1)', () => {
            const dispensados = { limite: null, assinatura: null, renovacao: { faixaDias: 3 } }
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 7,
                dispensados
            })
            expect(result).toBeNull()
        })

        it('faixa 1 -> 3 NÃO reaparece (melhorou: gravidade 3 -> 2)', () => {
            const dispensados = { limite: null, assinatura: null, renovacao: { faixaDias: 1 } }
            const result = obterAvisoPlano({
                ...baseParams,
                diasParaRenovar: 3,
                dispensados
            })
            expect(result).toBeNull()
        })
    })

    describe('Prioridade: limite > assinatura > renovacao', () => {
        it('limite tem prioridade sobre assinatura', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 5,
                limite: 5,
                statusAssinatura: 'vencida'
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('limite')
        })

        it('assinatura tem prioridade sobre renovacao', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'vencida',
                diasParaRenovar: 3
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('assinatura')
        })

        it('limite tem prioridade sobre renovacao', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                totalPacientes: 5,
                limite: 5,
                diasParaRenovar: 3
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('limite')
        })
    })

    describe('sem_plano só mostra aviso de LIMITE (não de assinatura)', () => {
        it('sem_plano, total < limite (3/5): null', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'sem_plano',
                totalPacientes: 3,
                limite: 5
            })
            expect(result).toBeNull()
        })

        it('sem_plano, total == limite (5/5): só aviso de limite nível 1', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'sem_plano',
                totalPacientes: 5,
                limite: 5
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('limite')
            expect(result.assinaturaDoEstado).toEqual({ limite: 5, nivel: 1 })
        })

        it('sem_plano, total > limite (6/5): só aviso de limite nível 2', () => {
            const result = obterAvisoPlano({
                ...baseParams,
                statusAssinatura: 'sem_plano',
                totalPacientes: 6,
                limite: 5
            })
            expect(result).not.toBeNull()
            expect(result.tipo).toBe('limite')
            expect(result.assinaturaDoEstado).toEqual({ limite: 5, nivel: 2 })
        })
    })
})

describe('gerarChaveDismiss', () => {
    it('gera chave correta para limite', () => {
        const chave = gerarChaveDismiss('emp1', 'user1', 'limite')
        expect(chave).toBe('plano_dismiss_emp1_user1_limite')
    })

    it('gera chave correta para assinatura', () => {
        const chave = gerarChaveDismiss('emp1', 'user1', 'assinatura')
        expect(chave).toBe('plano_dismiss_emp1_user1_assinatura')
    })

    it('gera chave correta para renovacao', () => {
        const chave = gerarChaveDismiss('emp1', 'user1', 'renovacao')
        expect(chave).toBe('plano_dismiss_emp1_user1_renovacao')
    })

    it('retorna null se empresaId faltar', () => {
        const chave = gerarChaveDismiss('', 'user1', 'limite')
        expect(chave).toBeNull()
    })

    it('retorna null se userId faltar', () => {
        const chave = gerarChaveDismiss('emp1', '', 'limite')
        expect(chave).toBeNull()
    })

    it('retorna null se empresaId for "unknown"', () => {
        const chave = gerarChaveDismiss('unknown', 'user1', 'limite')
        expect(chave).toBeNull()
    })

    it('retorna null se userId for "unknown"', () => {
        const chave = gerarChaveDismiss('emp1', 'unknown', 'limite')
        expect(chave).toBeNull()
    })
})

describe('lerDispensados', () => {
    beforeEach(() => {
        localStorage.clear()
    })

    it('retorna nulls se nada salvo', () => {
        const resultado = lerDispensados('emp1', 'user1')
        expect(resultado).toEqual({ limite: null, assinatura: null, renovacao: null })
    })

    it('lê corretamente estado salvo para limite', () => {
        const chave = 'plano_dismiss_emp1_user1_limite'
        localStorage.setItem(chave, JSON.stringify({ limite: 5, nivel: 1 }))
        const resultado = lerDispensados('emp1', 'user1')
        expect(resultado.limite).toEqual({ limite: 5, nivel: 1 })
    })

    it('lê corretamente estado salvo para assinatura', () => {
        const chave = 'plano_dismiss_emp1_user1_assinatura'
        localStorage.setItem(chave, JSON.stringify({ status: 'vencida' }))
        const resultado = lerDispensados('emp1', 'user1')
        expect(resultado.assinatura).toEqual({ status: 'vencida' })
    })

    it('lê corretamente estado salvo para renovacao', () => {
        const chave = 'plano_dismiss_emp1_user1_renovacao'
        localStorage.setItem(chave, JSON.stringify({ faixaDias: 7 }))
        const resultado = lerDispensados('emp1', 'user1')
        expect(resultado.renovacao).toEqual({ faixaDias: 7 })
    })

    it('ignora JSON inválido', () => {
        const chave = 'plano_dismiss_emp1_user1_limite'
        localStorage.setItem(chave, 'invalid json')
        const resultado = lerDispensados('emp1', 'user1')
        expect(resultado.limite).toBeNull()
    })

    it('retorna nulls se IDs inválidos', () => {
        const resultado = lerDispensados('', 'user1')
        expect(resultado).toEqual({ limite: null, assinatura: null, renovacao: null })
    })
})

describe('marcarDispensado', () => {
    beforeEach(() => {
        localStorage.clear()
    })

    it('salva assinaturaDoEstado para limite', () => {
        marcarDispensado('emp1', 'user1', 'limite', { limite: 5, nivel: 1 })
        const chave = 'plano_dismiss_emp1_user1_limite'
        expect(JSON.parse(localStorage.getItem(chave))).toEqual({ limite: 5, nivel: 1 })
    })

    it('salva assinaturaDoEstado para assinatura', () => {
        marcarDispensado('emp1', 'user1', 'assinatura', { status: 'vencida' })
        const chave = 'plano_dismiss_emp1_user1_assinatura'
        expect(JSON.parse(localStorage.getItem(chave))).toEqual({ status: 'vencida' })
    })

    it('salva assinaturaDoEstado para renovacao', () => {
        marcarDispensado('emp1', 'user1', 'renovacao', { faixaDias: 7 })
        const chave = 'plano_dismiss_emp1_user1_renovacao'
        expect(JSON.parse(localStorage.getItem(chave))).toEqual({ faixaDias: 7 })
    })

    it('NÃO salva se empresaId faltar', () => {
        marcarDispensado('', 'user1', 'limite', { limite: 5, nivel: 1 })
        expect(localStorage.getItem('plano_dismiss__user1_limite')).toBeNull()
    })

    it('NÃO salva se userId faltar', () => {
        marcarDispensado('emp1', '', 'limite', { limite: 5, nivel: 1 })
        expect(localStorage.getItem('plano_dismiss_emp1__limite')).toBeNull()
    })

    it('NÃO salva se empresaId for "unknown"', () => {
        marcarDispensado('unknown', 'user1', 'limite', { limite: 5, nivel: 1 })
        expect(localStorage.getItem('plano_dismiss_unknown_user1_limite')).toBeNull()
    })

    it('NÃO salva se userId for "unknown"', () => {
        marcarDispensado('emp1', 'unknown', 'limite', { limite: 5, nivel: 1 })
        expect(localStorage.getItem('plano_dismiss_emp1_unknown_limite')).toBeNull()
    })
})

describe('montarMensagemLimite', () => {
    it('monta mensagem sem suporteContato', () => {
        const msg = montarMensagemLimite({ limite: 5 })
        expect(msg).toBe('Você atingiu o limite de 5 pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('monta mensagem com suporteContato', () => {
        const msg = montarMensagemLimite({ limite: 5, suporteContato: 'suporte@exemplo.com' })
        expect(msg).toBe('Você atingiu o limite de 5 pacientes do seu plano. Para cadastrar mais, fale com o suporte (suporte@exemplo.com).')
    })

    it('monta mensagem com suporteContato telefone', () => {
        const msg = montarMensagemLimite({ limite: 10, suporteContato: '(11) 99999-9999' })
        expect(msg).toBe('Você atingiu o limite de 10 pacientes do seu plano. Para cadastrar mais, fale com o suporte ((11) 99999-9999).')
    })

    it('monta mensagem com suporteContato string vazia (não adiciona)', () => {
        const msg = montarMensagemLimite({ limite: 5, suporteContato: '' })
        expect(msg).toBe('Você atingiu o limite de 5 pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })
})

describe('obterAvisoPlano com suporteContato', () => {
    const baseParams = {
        perfil: 'profissional',
        limite: 5,
        totalPacientes: 5,
        statusAssinatura: 'ativa',
        diasParaRenovar: null,
        dispensados: { limite: null, assinatura: null, renovacao: null }
    }

    it('inclui suporteContato na mensagem de limite nível 1', () => {
        const result = obterAvisoPlano({
            ...baseParams,
            suporteContato: 'suporte@teste.com'
        })
        expect(result).not.toBeNull()
        expect(result.mensagem).toContain('(suporte@teste.com)')
    })

    it('inclui suporteContato na mensagem de limite nível 2', () => {
        const result = obterAvisoPlano({
            ...baseParams,
            totalPacientes: 6,
            suporteContato: 'suporte@teste.com'
        })
        expect(result).not.toBeNull()
        expect(result.mensagem).toContain('(suporte@teste.com)')
    })

    it('sem suporteContato mantém mensagem original', () => {
        const result = obterAvisoPlano({
            ...baseParams,
            suporteContato: undefined
        })
        expect(result).not.toBeNull()
        expect(result.mensagem).not.toContain('()')
        expect(result.mensagem).toContain('fale com o suporte.')
    })
})

describe('montarMensagemLimite - sem limite (null, undefined, 0)', () => {
    it('monta mensagem sem limite (null)', () => {
        const msg = montarMensagemLimite({ limite: null })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('monta mensagem sem limite (undefined)', () => {
        const msg = montarMensagemLimite({ limite: undefined })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('monta mensagem sem limite (0)', () => {
        const msg = montarMensagemLimite({ limite: 0 })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('monta mensagem sem limite com suporteContato (null)', () => {
        const msg = montarMensagemLimite({ limite: null, suporteContato: 'suporte@teste.com' })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte (suporte@teste.com).')
    })

    it('monta mensagem sem limite com suporteContato (undefined)', () => {
        const msg = montarMensagemLimite({ limite: undefined, suporteContato: 'suporte@teste.com' })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte (suporte@teste.com).')
    })

    it('monta mensagem sem limite com suporteContato (0)', () => {
        const msg = montarMensagemLimite({ limite: 0, suporteContato: 'suporte@teste.com' })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte (suporte@teste.com).')
    })
})

describe('obterAvisoPlano - nível 2 com e sem suporteContato', () => {
    const baseParams = {
        perfil: 'profissional',
        limite: 5,
        totalPacientes: 6,
        statusAssinatura: 'ativa',
        diasParaRenovar: null,
        dispensados: { limite: null, assinatura: null, renovacao: null }
    }

    it('nível 2 sem suporteContato inclui total (6/5)', () => {
        const result = obterAvisoPlano({
            ...baseParams,
            suporteContato: undefined
        })
        expect(result).not.toBeNull()
        expect(result.mensagem).toContain('excedeu o limite')
        expect(result.mensagem).toContain('6/5')
        expect(result.mensagem).not.toContain('()')
    })

    it('nível 2 com suporteContato inclui total e contato', () => {
        const result = obterAvisoPlano({
            ...baseParams,
            suporteContato: 'suporte@teste.com'
        })
        expect(result).not.toBeNull()
        expect(result.mensagem).toContain('excedeu o limite')
        expect(result.mensagem).toContain('6/5')
        expect(result.mensagem).toContain('(suporte@teste.com)')
    })

    it('nível 2 sem limite (null) com suporteContato', () => {
        const result = obterAvisoPlano({
            ...baseParams,
            limite: null,
            suporteContato: 'suporte@teste.com'
        })
        // sem_plano não deve mostrar aviso de limite quando total < limite (mas limite é null, então sem limite finito)
        // Como limite é null, temLimiteFinito é false, não deve retornar aviso de limite
        expect(result).toBeNull()
    })
})

describe('montarMensagemLimite - formato exato nível 1 e nível 2', () => {
    // Nível 1: totalPacientes == limite OU totalPacientes não passado OU total <= limite
    it('nível 1 com limite e sem totalPacientes -> "atingiu"', () => {
        const msg = montarMensagemLimite({ limite: 5 })
        expect(msg).toBe('Você atingiu o limite de 5 pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('nível 1 com limite, totalPacientes == limite -> "atingiu"', () => {
        const msg = montarMensagemLimite({ limite: 5, totalPacientes: 5 })
        expect(msg).toBe('Você atingiu o limite de 5 pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('nível 1 com limite, totalPacientes < limite -> "atingiu"', () => {
        const msg = montarMensagemLimite({ limite: 5, totalPacientes: 3 })
        expect(msg).toBe('Você atingiu o limite de 5 pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('nível 1 com limite e contato -> "atingiu (contato)"', () => {
        const msg = montarMensagemLimite({ limite: 5, suporteContato: 'suporte@teste.com' })
        expect(msg).toBe('Você atingiu o limite de 5 pacientes do seu plano. Para cadastrar mais, fale com o suporte (suporte@teste.com).')
    })

    // Nível 2: totalPacientes > limite
    it('nível 2 com limite e totalPacientes > limite -> "excedeu (Y/X)"', () => {
        const msg = montarMensagemLimite({ limite: 5, totalPacientes: 6 })
        expect(msg).toBe('Você excedeu o limite de 5 pacientes do seu plano (6/5). Para cadastrar mais, fale com o suporte.')
    })

    it('nível 2 com limite, totalPacientes > limite e contato -> "excedeu (Y/X) (contato)"', () => {
        const msg = montarMensagemLimite({ limite: 5, totalPacientes: 6, suporteContato: 'suporte@teste.com' })
        expect(msg).toBe('Você excedeu o limite de 5 pacientes do seu plano (6/5). Para cadastrar mais, fale com o suporte (suporte@teste.com).')
    })

    // Sem limite (null/0): sem número, sem "(Y/X)"
    it('sem limite (null) -> sem número, sem (Y/X)', () => {
        const msg = montarMensagemLimite({ limite: null })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('sem limite (0) -> sem número, sem (Y/X)', () => {
        const msg = montarMensagemLimite({ limite: 0 })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('sem limite (null) com totalPacientes > 0 -> ainda "atingiu" (sem número)', () => {
        const msg = montarMensagemLimite({ limite: null, totalPacientes: 10 })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('sem limite (null) com totalPacientes > 0 e contato -> "atingiu (contato)"', () => {
        const msg = montarMensagemLimite({ limite: null, totalPacientes: 10, suporteContato: 'suporte@teste.com' })
        expect(msg).toBe('Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte (suporte@teste.com).')
    })
})

describe('obterAvisoPlano - strings exatas nível 1 e nível 2', () => {
    it('nível 1 (5/5) mensagem exata sem contato', () => {
        const result = obterAvisoPlano({
            perfil: 'profissional',
            limite: 5,
            totalPacientes: 5,
            statusAssinatura: 'ativa',
            diasParaRenovar: null,
            dispensados: { limite: null, assinatura: null, renovacao: null }
        })
        expect(result).not.toBeNull()
        expect(result.mensagem).toBe('Você atingiu o limite de 5 pacientes do seu plano. Para cadastrar mais, fale com o suporte.')
    })

    it('nível 1 (5/5) mensagem exata com contato', () => {
        const result = obterAvisoPlano({
            perfil: 'profissional',
            limite: 5,
            totalPacientes: 5,
            statusAssinatura: 'ativa',
            diasParaRenovar: null,
            dispensados: { limite: null, assinatura: null, renovacao: null },
            suporteContato: 'suporte@teste.com'
        })
        expect(result).not.toBeNull()
        expect(result.mensagem).toBe('Você atingiu o limite de 5 pacientes do seu plano. Para cadastrar mais, fale com o suporte (suporte@teste.com).')
    })

    it('nível 2 (6/5) mensagem exata sem contato', () => {
        const result = obterAvisoPlano({
            perfil: 'profissional',
            limite: 5,
            totalPacientes: 6,
            statusAssinatura: 'ativa',
            diasParaRenovar: null,
            dispensados: { limite: null, assinatura: null, renovacao: null }
        })
        expect(result).not.toBeNull()
        expect(result.mensagem).toBe('Você excedeu o limite de 5 pacientes do seu plano (6/5). Para cadastrar mais, fale com o suporte.')
    })

    it('nível 2 (6/5) mensagem exata com contato', () => {
        const result = obterAvisoPlano({
            perfil: 'profissional',
            limite: 5,
            totalPacientes: 6,
            statusAssinatura: 'ativa',
            diasParaRenovar: null,
            dispensados: { limite: null, assinatura: null, renovacao: null },
            suporteContato: 'suporte@teste.com'
        })
        expect(result).not.toBeNull()
        expect(result.mensagem).toBe('Você excedeu o limite de 5 pacientes do seu plano (6/5). Para cadastrar mais, fale com o suporte (suporte@teste.com).')
    })
})