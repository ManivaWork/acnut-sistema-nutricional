// plano-warnings.js - Lógica pura para avisos de plano/assinatura
// Função testável, sem efeitos colaterais (sem DOM, sem fetch, sem localStorage)

/**
 * Decodifica o payload de um JWT (base64url) e extrai empresaId e userId.
 * Claims esperadas: "empresa" (empresaId) e "uid" (userId).
 * @param {string|null|undefined} token - JWT token
 * @returns {{empresaId: string, userId: string}|null} - IDs extraídos ou null se token inválido/ausente/sem claims
 */
export function lerIdsDoToken(token) {
    if (!token || typeof token !== 'string') {
        return null;
    }

    try {
        // JWT tem 3 partes separadas por '.': header.payload.signature
        const parts = token.split('.');
        if (parts.length !== 3) {
            return null;
        }

        // Decodificar payload (parte do meio) - base64url
        let payload = parts[1];

        // Base64url: substituir - por +, _ por /, adicionar padding
        payload = payload.replace(/-/g, '+').replace(/_/g, '/');

        // Adicionar padding se necessário
        const padding = payload.length % 4;
        if (padding > 0) {
            payload += '='.repeat(4 - padding);
        }

        // Decodificar base64
        const decoded = atob(payload);

        // Parse JSON
        const claims = JSON.parse(decoded);

        // Extrair claims "empresa" e "uid"
        const empresaId = claims?.empresa;
        const userId = claims?.uid;

        if (!empresaId || !userId) {
            return null;
        }

        return { empresaId: String(empresaId), userId: String(userId) };
    } catch {
        // Qualquer erro (base64 inválido, JSON inválido, etc.) -> null
        return null;
    }
}

/**
 * Níveis de gravidade para comparação (maior = pior)
 */
const GRAVIDADE = {
    limite: { ok: 0, nivel1: 1, nivel2: 2 },
    assinatura: { pendente: 1, past_due: 2, vencida: 2, cancelada: 3 },
    renovacao: { faixa7: 1, faixa3: 2, faixa1: 3 }
};

/**
 * Calcula o nível de gravidade do limite
 * @param {number} totalPacientes
 * @param {number} limite
 * @returns {number} 0=ok, 1=total==limite, 2=total>limite
 */
function calcularNivelLimite(totalPacientes, limite) {
    if (totalPacientes > limite) return 2;
    if (totalPacientes === limite) return 1;
    return 0;
}

/**
 * Calcula a gravidade da assinatura
 * @param {string} status
 * @returns {number} 0=ok, 1-3=pior
 */
function calcularGravidadeAssinatura(status) {
    return GRAVIDADE.assinatura[status] ?? 0;
}

/**
 * Calcula a gravidade da renovação
 * @param {number|null} diasParaRenovar
 * @returns {number} 0=fora da faixa, 1=7, 2=3, 3=1
 */
function calcularGravidadeRenovacao(diasParaRenovar) {
    if (diasParaRenovar == null || diasParaRenovar <= 0 || diasParaRenovar > 7) return 0;
    if (diasParaRenovar <= 1) return 3;
    if (diasParaRenovar <= 3) return 2;
    return 1; // <= 7
}

/**
 * Converte gravidade numérica em label para assinaturaDoEstado
 */
function nivelParaLabelLimite(nivel) {
    return nivel === 2 ? 'nivel2' : nivel === 1 ? 'nivel1' : 'ok';
}

function gravidadeParaLabelAssinatura(gravidade) {
    if (gravidade >= 3) return 'cancelada';
    if (gravidade >= 2) return 'vencida'; // past_due e vencida
    if (gravidade >= 1) return 'pendente';
    return 'ok';
}

function gravidadeParaLabelRenovacao(gravidade) {
    if (gravidade >= 3) return 'faixa1';
    if (gravidade >= 2) return 'faixa3';
    if (gravidade >= 1) return 'faixa7';
    return 'ok';
}

function faixaDiasParaLabel(faixaDias) {
    if (faixaDias <= 1) return 'faixa1';
    if (faixaDias <= 3) return 'faixa3';
    return 'faixa7';
}

/**
 * Monta a mensagem de limite de pacientes.
 * Função pura para facilitar testes.
 *
 * @param {Object} params
 * @param {number|null} [params.limite] - Limite de pacientes do plano (null/undefined/0 = omitir número)
 * @param {number} [params.totalPacientes] - Total de pacientes cadastrados (opcional, para formato "excedeu")
 * @param {string} [params.suporteContato] - Contato do suporte (opcional)
 * @returns {string} Mensagem formatada
 */
export function montarMensagemLimite({ limite, totalPacientes, suporteContato }) {
    const temLimite = limite != null && limite > 0;
    const excedeu = temLimite && totalPacientes != null && totalPacientes > limite;

    let mensagem;
    if (excedeu) {
        // "Você excedeu o limite de X pacientes do seu plano (Y/X). Para cadastrar mais, fale com o suporte"
        mensagem = `Você excedeu o limite de ${limite} pacientes do seu plano (${totalPacientes}/${limite}). Para cadastrar mais, fale com o suporte`;
    } else if (temLimite) {
        // "Você atingiu o limite de X pacientes do seu plano. Para cadastrar mais, fale com o suporte"
        mensagem = `Você atingiu o limite de ${limite} pacientes do seu plano. Para cadastrar mais, fale com o suporte`;
    } else {
        // "Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte"
        mensagem = `Você atingiu o limite de pacientes do seu plano. Para cadastrar mais, fale com o suporte`;
    }
    if (suporteContato) {
        mensagem += ` (${suporteContato})`;
    }
    return mensagem + '.';
}

/**
 * Determina qual aviso de plano/assinatura mostrar, se algum.
 *
 * @param {Object} params - Parâmetros de entrada
 * @param {string} params.perfil - Perfil do usuário: 'super_admin', 'admin', 'profissional', 'atendente', 'paciente'
 * @param {number|null} params.limite - Limite de pacientes do plano (null/0 = ilimitado)
 * @param {number} params.totalPacientes - Total de pacientes cadastrados
 * @param {string} params.statusAssinatura - Status da assinatura: 'ativa', 'trial', 'past_due', 'cancelada', 'pendente', 'vencida'
 * @param {number|null} params.diasParaRenovar - Dias até renovação (null se não aplicável/dado indisponível)
 * @param {Object} params.dispensados - Objeto com estado visto por tipo: { limite: assinaturaVista|null, assinatura: statusVisto|null, renovacao: faixaVista|null }
 *   Cada assinaturaVista = { limite, nivel } ou null
 *   statusVisto = string do status ou null
 *   faixaVista = string 'faixa1'|'faixa3'|'faixa7' ou null
 * @param {string} [params.suporteContato] - Contato do suporte para incluir na mensagem de limite (opcional)
 * @returns {Object|null} - null se nenhum aviso, ou { tipo, mensagem, severidade, assinaturaDoEstado }
 */
export function obterAvisoPlano({ perfil, limite, totalPacientes, statusAssinatura, diasParaRenovar, dispensados = {}, suporteContato }) {
    // super_admin, admin, atendente, paciente nunca veem avisos de plano
    if (['super_admin', 'admin', 'atendente', 'paciente'].includes(perfil)) {
        return null;
    }

    // Apenas profissional vê avisos
    if (perfil !== 'profissional') {
        return null;
    }

    const temLimiteFinito = limite != null && limite > 0;

    // 1. Aviso: LIMITE ATINGIDO (prioridade alta - erro)
    if (temLimiteFinito) {
        const nivelAtual = calcularNivelLimite(totalPacientes, limite);
        if (nivelAtual > 0) {
            const assinaturaDoEstado = { limite, nivel: nivelAtual };
            const visto = dispensados.limite;

            // Mostra se não há visto, ou se gravidade atual > vista, ou se limite mudou
            const deveMostrar = !visto
                || nivelAtual > (visto.nivel ?? 0)
                || visto.limite !== limite;

            if (deveMostrar) {
                const nivelLabel = nivelParaLabelLimite(nivelAtual);
                let mensagem;
                if (nivelLabel === 'nivel2') {
                    // Para nivel2: passa totalPacientes para montarMensagemLimite gerar formato "excedeu (Y/X)"
                    mensagem = montarMensagemLimite({ limite, totalPacientes, suporteContato });
                } else {
                    // nível 1: sem totalPacientes, usa formato "atingiu"
                    mensagem = montarMensagemLimite({ limite, suporteContato });
                }
                return {
                    tipo: 'limite',
                    mensagem,
                    severidade: 'erro',
                    assinaturaDoEstado: assinaturaDoEstado
                };
            }
        }
    }

    // 2. Aviso: ASSINATURA PROBLEMÁTICA (pendente, vencida, cancelada, past_due)
    const gravidadeAssinatura = calcularGravidadeAssinatura(statusAssinatura);
    if (gravidadeAssinatura > 0) {
        const assinaturaDoEstado = { status: statusAssinatura };
        const visto = dispensados.assinatura;

        // visto é { status: 'vencida' } ou null
        const gravidadeVista = visto ? (GRAVIDADE.assinatura[visto.status] ?? 0) : 0;
        const deveMostrar = !visto || gravidadeAssinatura > gravidadeVista;

        if (deveMostrar) {
            const mensagens = {
                'pendente': 'Sua assinatura está pendente. Regularize para manter o acesso.',
                'vencida': 'Sua assinatura venceu. Renove para continuar usando o sistema.',
                'cancelada': 'Sua assinatura foi cancelada. Reative para acessar todos os recursos.',
                'past_due': 'Pagamento em atraso. Regularize para evitar interrupção do serviço.'
            };

            return {
                tipo: 'assinatura',
                mensagem: mensagens[statusAssinatura] || 'Problema com sua assinatura. Fale com o suporte.',
                severidade: 'aviso',
                assinaturaDoEstado: assinaturaDoEstado
            };
        }
    }

    // 3. Aviso: RENOVAÇÃO PRÓXIMA (≤ 7 dias) - apenas se dado existir
    const gravidadeRenovacao = calcularGravidadeRenovacao(diasParaRenovar);
    if (gravidadeRenovacao > 0) {
        let faixaDias;
        if (diasParaRenovar <= 1) faixaDias = 1;
        else if (diasParaRenovar <= 3) faixaDias = 3;
        else faixaDias = 7;

        const assinaturaDoEstado = { faixaDias };
        const faixaLabel = gravidadeParaLabelRenovacao(gravidadeRenovacao);
        const visto = dispensados.renovacao;

        // visto é { faixaDias: 7 } ou null
        const gravidadeVista = visto ? (GRAVIDADE.renovacao[faixaDiasParaLabel(visto.faixaDias)] ?? 0) : 0;
        const deveMostrar = !visto || gravidadeRenovacao > gravidadeVista;

        if (deveMostrar) {
            return {
                tipo: 'renovacao',
                mensagem: `Sua assinatura renova em ${diasParaRenovar} dia${diasParaRenovar > 1 ? 's' : ''}. Verifique se o pagamento está em dia.`,
                severidade: 'aviso',
                assinaturaDoEstado: assinaturaDoEstado
            };
        }
    }

    // Nenhum aviso aplicável
    return null;
}

/**
 * Gera a chave de localStorage para o dismiss de um tipo de aviso.
 * Uma chave por tipo (não por estado): plano_dismiss_{empresa}_{user}_{tipo}
 * Valor = JSON da assinaturaDoEstado vista.
 * Se empresaId ou userId faltar: retorna null (não grava dismiss).
 *
 * @param {string} empresaId - ID da empresa
 * @param {string} userId - ID do usuário
 * @param {string} tipo - Tipo do aviso: 'limite', 'assinatura', 'renovacao'
 * @returns {string|null} - Chave do localStorage ou null se IDs inválidos
 */
export function gerarChaveDismiss(empresaId, userId, tipo) {
    if (!empresaId || !userId || empresaId === 'unknown' || userId === 'unknown') {
        return null;
    }
    return `plano_dismiss_${empresaId}_${userId}_${tipo}`;
}

/**
 * Lê todos os estados dispensados do localStorage para uma empresa/usuario.
 * Retorna { limite, assinatura, renovacao } com a assinaturaDoEstado vista ou null.
 *
 * @param {string} empresaId - ID da empresa
 * @param {string} userId - ID do usuário
 * @returns {Object} - { limite, assinatura, renovacao }
 */
export function lerDispensados(empresaId, userId) {
    const resultado = { limite: null, assinatura: null, renovacao: null };

    const chaveLimite = gerarChaveDismiss(empresaId, userId, 'limite');
    if (chaveLimite) {
        const val = localStorage.getItem(chaveLimite);
        if (val) {
            try { resultado.limite = JSON.parse(val); } catch { }
        }
    }

    const chaveAssinatura = gerarChaveDismiss(empresaId, userId, 'assinatura');
    if (chaveAssinatura) {
        const val = localStorage.getItem(chaveAssinatura);
        if (val) {
            try { resultado.assinatura = JSON.parse(val); } catch { }
        }
    }

    const chaveRenovacao = gerarChaveDismiss(empresaId, userId, 'renovacao');
    if (chaveRenovacao) {
        const val = localStorage.getItem(chaveRenovacao);
        if (val) {
            try { resultado.renovacao = JSON.parse(val); } catch { }
        }
    }

    return resultado;
}

/**
 * Marca um aviso como dispensado salvando a assinaturaDoEstado no localStorage.
 * Se empresaId ou userId faltar: não grava (silenciosamente).
 *
 * @param {string} empresaId - ID da empresa
 * @param {string} userId - ID do usuário
 * @param {string} tipo - Tipo do aviso: 'limite', 'assinatura', 'renovacao'
 * @param {Object} assinaturaDoEstado - Assinatura do estado atual a ser salva
 */
export function marcarDispensado(empresaId, userId, tipo, assinaturaDoEstado) {
    const chave = gerarChaveDismiss(empresaId, userId, tipo);
    if (!chave) return;
    localStorage.setItem(chave, JSON.stringify(assinaturaDoEstado));
}

/**
 * Cria um elemento toast para exibir um aviso com botão "Dispensar".
 * Usa textContent para a mensagem (nunca innerHTML) e addEventListener no botão.
 *
 * @param {Object} aviso - Objeto do aviso retornado por obterAvisoPlano
 *   { tipo, mensagem, severidade, assinaturaDoEstado }
 * @param {Object} opts - Opções
 * @param {string} opts.empresaId - ID da empresa
 * @param {string} opts.userId - ID do usuário
 * @param {Function} opts.onFechar - Callback chamado ao fechar (dispensar ou timeout)
 * @returns {HTMLElement} - Elemento toast pronto para ser anexado ao DOM
 */
export function criarToastAviso(aviso, { empresaId, userId, onFechar }) {
    // Mapear severidade para classes CSS
    const cores = {
        erro: 'bg-rose-600 text-white',
        aviso: 'bg-amber-600 text-white'
    };
    const icones = {
        erro: 'fa-circle-xmark',
        aviso: 'fa-triangle-exclamation'
    };

    const toast = document.createElement('div');
    toast.className = `flex items-center gap-3 px-4 py-3 rounded-xl shadow-lg ${cores[aviso.severidade] || cores.aviso} animate-slide-in`;
    toast.dataset.avisoTipo = aviso.tipo;

    // Ícone
    const icone = document.createElement('i');
    icone.className = `fa-solid ${icones[aviso.severidade] || icones.aviso}`;
    toast.appendChild(icone);

    // Mensagem - usando textContent (nunca innerHTML)
    const spanMensagem = document.createElement('span');
    spanMensagem.className = 'text-sm font-medium flex-1 min-w-0';
    spanMensagem.textContent = aviso.mensagem;
    toast.appendChild(spanMensagem);

    // Botão "Dispensar"
    const btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'px-3 py-1.5 text-xs font-semibold rounded-lg bg-white/20 hover:bg-white/30 transition whitespace-nowrap flex-shrink-0';
    btn.textContent = 'Dispensar';

    // Event listener - sem JSON em atributo HTML
    btn.addEventListener('click', () => {
        // Marcar como dispensado
        marcarDispensado(empresaId, userId, aviso.tipo, aviso.assinaturaDoEstado);
        // Remover toast do DOM
        toast.remove();
        // Chamar callback de fechamento
        if (typeof onFechar === 'function') {
            onFechar();
        }
    });

    toast.appendChild(btn);

    return toast;
}

// Expor no window para uso no app.js (testes usam os exports nomeados)
if (typeof window !== 'undefined') {
    Object.assign(window, {
        obterAvisoPlano,
        montarMensagemLimite,
        gerarChaveDismiss,
        lerDispensados,
        marcarDispensado,
        lerIdsDoToken,
        criarToastAviso
    });
}