import { describe, it, expect } from 'vitest'
import fs from 'fs'
import path from 'path'

describe('plano-warnings.js carregamento no index.html', () => {
    const indexPath = path.resolve(__dirname, '..', 'index.html')
    const indexHtml = fs.readFileSync(indexPath, 'utf-8')

    it('deve ter script tag para plano-warnings.js com type="module"', () => {
        // Procura a tag script que carrega plano-warnings.js
        const scriptTagRegex = /<script\s+type="module"\s+src="\/interface\/js\/plano-warnings\.js"><\/script>/
        expect(scriptTagRegex.test(indexHtml)).toBe(true)
    })

    it('deve carregar plano-warnings.js antes do app.js', () => {
        const warningsIndex = indexHtml.indexOf('/interface/js/plano-warnings.js')
        const appIndex = indexHtml.indexOf('/interface/app.js')

        expect(warningsIndex).toBeGreaterThan(-1)
        expect(appIndex).toBeGreaterThan(-1)
        expect(warningsIndex).toBeLessThan(appIndex)
    })

    it('não deve chamar funções de plano-warnings durante o carregamento', () => {
        // Verifica que não há chamadas inline para obterAvisoPlano, etc. no index.html
        // (as chamadas devem acontecer apenas via app.js após carregamento)
        const inlineCalls = [
            'obterAvisoPlano(',
            'montarMensagemLimite(',
            'gerarChaveDismiss(',
            'lerDispensados(',
            'marcarDispensado('
        ]

        inlineCalls.forEach(call => {
            // Não deve haver chamadas diretas no HTML (fora de tags script de módulo)
            const scriptContentMatches = indexHtml.match(/<script[^>]*>([\s\S]*?)<\/script>/g) || []
            const inlineScripts = scriptContentMatches.filter(s => !s.includes('type="module"') && !s.includes('src='))

            inlineScripts.forEach(script => {
                expect(script).not.toContain(call)
            })
        })
    })
})