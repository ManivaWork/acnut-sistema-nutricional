        const API_URL = (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1')
  ? 'http://localhost:5290/api'
  : 'https://acnut-sistema-nutricional.onrender.com/api';
        const TOKEN_KEY = "bybar_token";
const getToken = () => sessionStorage.getItem(TOKEN_KEY);
const urlArquivo = id => `${API_URL}/arquivos/${id}?t=${encodeURIComponent(getToken() || "")}`;

// Todo fetch para a API leva o token automaticamente
const _fetch = window.fetch.bind(window);
let sessaoEncerrando = false;
window.fetch = async (url, opts = {}) => {
    const ehApi = String(url).startsWith(API_URL) && !String(url).startsWith(`${API_URL}/auth/`);
    if (ehApi) opts = { ...opts, headers: { ...(opts.headers || {}), Authorization: `Bearer ${getToken() || ""}` } };
    const res = await _fetch(url, opts);
    if (ehApi && res.status === 401 && !sessaoEncerrando) {
        sessaoEncerrando = true;
        alert("Sua sessão expirou. Entre novamente.");
        fazerLogout();
        document.getElementById("area-paciente").style.display = "none";
        if (typeof modoLogin === "function") modoLogin("equipe");
        sessionStorage.removeItem(TOKEN_KEY);
        sessionStorage.removeItem("bybar_user");
        sessaoEncerrando = false;
    }
    return res;
};

function sessaoValida() {
    const t = getToken();
    if (!t) return false;
    try {
        const p = JSON.parse(atob(t.split(".")[1].replace(/-/g, "+").replace(/_/g, "/")));
        return p.exp * 1000 > Date.now();
    } catch { return false; }
}
let pacienteEmEdicaoId = null; // Se for null, está a criar. Se tiver um número, está a editar.
// ===== HELPERS =====
let listaPacientes = [];
function escaparHtml(valor) {
        return String(valor ?? "").replace(/[&<>"']/g, c => ({
        "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;"
        }[c]));
        }
function formatarData(iso) {
        if (!iso) return "-";
        const [a, m, d] = iso.split("-");
        return `${d}/${m}/${a}`;
        }
function badgeStatus(status) {
        const ativo = (status || "ativo").toLowerCase() === "ativo";
        return ativo
        ? `<span class="px-2.5 py-1 text-xs font-medium bg-teal-500/15 text-teal-300 rounded-full border border-teal-500/25">Ativo</span>`
        : `<span class="px-2.5 py-1 text-xs font-medium bg-rose-500/15 text-rose-300 rounded-full border border-rose-500/25">Inativo</span>`;
        }
function atualizarBotaoFormulario(editando) {
        const btn = document.querySelector('#form-paciente button[type="submit"]');
        const btnCancelar = document.getElementById('btn-cancelar');
        if (btn) {
        btn.innerHTML = editando
        ? '<i class="fa-solid fa-floppy-disk mr-2"></i>Atualizar Paciente'
        : '<i class="fa-solid fa-plus mr-2"></i>Cadastrar Paciente';
        ['from-emerald-600', 'to-teal-500', 'hover:from-emerald-500', 'hover:to-teal-400']
        .forEach(c => btn.classList.toggle(c, !editando));
        ['from-amber-600', 'to-orange-500', 'hover:from-amber-500', 'hover:to-orange-400']
        .forEach(c => btn.classList.toggle(c, editando));
        }
        if (btnCancelar) btnCancelar.classList.toggle('hidden', !editando);
        }
function navegar(secao) {
        document.querySelectorAll("main > section").forEach(s => s.classList.add("hidden"));
        const secaoAlvo = document.getElementById(`sec-${secao}`);
        if (secaoAlvo) {
        secaoAlvo.classList.remove("hidden");
        }
        if (secao === 'pacientes' || secao === 'dashboard') {
        carregarPacientes();
        }
        if (secao === 'prontuario') restaurarProntuarioAtivo();
        if (secao === 'empresas') carregarEmpresas();
        document.querySelectorAll(".acnut-rail-item[data-nav]").forEach(btn => {
        btn.classList.toggle("ativo", btn.dataset.nav === secao);
        });
        // Mostrar/ocultar item Empresas conforme perfil
        const navEmpresas = document.getElementById('nav-empresas');
        const user = JSON.parse(sessionStorage.getItem('bybar_user') || '{}');
        if (navEmpresas) {
            navEmpresas.style.display = (user.perfil === 'super_admin') ? 'flex' : 'none';
        }
        }
async function carregarPacientes() {
        try {
        const response = await fetch(`${API_URL}/pacientes`);
        if (!response.ok) throw new Error(`Erro HTTP: ${response.status}`);
        listaPacientes = await response.json();
        atualizarDashboard();
        const elementoTotal = document.getElementById("total-pacientes");
        if (elementoTotal) elementoTotal.innerText = listaPacientes.length;
        const tabela = document.getElementById("tabela-pacientes");
        if (!tabela) return;
        tabela.innerHTML = listaPacientes.map(p => `
        <tr class="border-b border-slate-700 hover:bg-slate-800/50">
        <td class="py-3 px-4 text-white">#${p.id}</td>
        <td class="py-3 px-4 font-medium text-emerald-400">${escaparHtml(p.nome)}</td>
        <td class="py-3 px-4 text-slate-300">${escaparHtml(p.email)}</td>
        <td class="py-3 px-4 text-slate-300">${formatarData(p.data_nascimento)}</td>
        <td class="py-3 px-4 text-slate-300">${escaparHtml(p.telefone) || '-'}</td>
        <td class="py-3 px-4">${badgeStatus(p.status)}</td>
        <td class="py-3 px-4 flex items-center gap-2">
        <button onclick="abrirProntuarioDireto(${p.id})" class="bg-emerald-600/20 text-emerald-400 hover:bg-emerald-600 hover:text-white px-3 py-1 rounded text-xs font-semibold transition">Prontuário</button>
        <button onclick="editarPaciente(${p.id})" class="bg-amber-500/20 text-amber-400 hover:bg-amber-500 hover:text-white px-3 py-1 rounded text-xs font-semibold transition">Editar</button>
        <button onclick="excluirPaciente(${p.id})" class="bg-rose-500/20 text-rose-400 hover:bg-rose-500 hover:text-white px-3 py-1 rounded text-xs font-semibold transition">Excluir</button>
        </td>
        </tr>`).join("");
        } catch (err) {
        console.error("Erro ao carregar pacientes:", err);
        }
        }
const CORES_AVATAR = ["#6c34eb", "#22b8a0", "#f0607a", "#e0a53a", "#4f8cff", "#b731b4"];
let dashFiltro = "";
 
function iniciais(nome) {
        const p = String(nome || "?").trim().split(/\s+/);
        const ini = (p[0]?.[0] || "") + (p.length > 1 ? p[p.length - 1][0] : "");
        return (ini || "?").toUpperCase();
        }
function corAvatar(nome) {
        let h = 0;
        for (const c of String(nome || "")) h = (h * 31 + c.charCodeAt(0)) >>> 0;
        return CORES_AVATAR[h % CORES_AVATAR.length];
        }
function filtrarDashboard(valor) {
        dashFiltro = (valor || "").trim().toLowerCase();
        renderRecentes();
        }
function renderRecentes() {
        const el = document.getElementById("dash-recentes");
        if (!el) return;
        const base = dashFiltro
        ? listaPacientes.filter(p => `${p.nome} ${p.email}`.toLowerCase().includes(dashFiltro))
        : listaPacientes;                                   // a API já devolve do mais novo ao mais antigo
        const lista = base.slice(0, dashFiltro ? 8 : 5);
        el.innerHTML = lista.length
        ? lista.map(p => `
        <div onclick="abrirProntuarioDireto(${p.id})" class="flex items-center gap-4 px-5 py-3.5">
        <div class="w-10 h-10 rounded-full flex items-center justify-center text-sm font-bold text-white shrink-0"
        style="background:${corAvatar(p.nome)}">${escaparHtml(iniciais(p.nome))}</div>
        <div class="min-w-0 flex-1">
        <p class="text-sm font-semibold text-white truncate">${escaparHtml(p.nome)}</p>
        <p class="text-xs text-slate-400 truncate">${escaparHtml(p.email)}</p>
        </div>
        <span class="hidden sm:inline-flex">${badgeStatus(p.status)}</span>
        <span class="text-sm text-slate-300 shrink-0">Abrir <i class="fa-solid fa-arrow-right text-xs ml-1"></i></span>
        </div>`).join("")
        : `<p class="px-5 py-4 text-sm text-slate-400">${dashFiltro ? "Nenhum paciente encontrado." : "Nenhum paciente cadastrado ainda."}</p>`;
        }
function atualizarDashboard() {
        const total = listaPacientes.length;
        const ativos = listaPacientes.filter(p => (p.status || "ativo").toLowerCase() === "ativo").length;
        const inativos = total - ativos;
        const pctAtivos = total ? Math.round((ativos / total) * 100) : 0;
        const pctInativos = total ? 100 - pctAtivos : 0;
        const set = (id, v) => { const el = document.getElementById(id); if (el) el.innerText = v; };
        set("total-pacientes", total);
        set("dash-ativos", ativos);
        set("dash-inativos", inativos);
        set("dash-ativos-pct", pctAtivos + "%");
        set("dash-inativos-pct", pctInativos + "%");
        set("dash-ring-pct", pctAtivos + "%");
        set("dash-leg-ativos", ativos);
        set("dash-leg-inativos", inativos);
        set("dash-data", new Date().toLocaleDateString("pt-BR",
        { weekday: "long", day: "numeric", month: "long" }));
        // Sincroniza título mobile do dashboard
        const mobileTitle = document.getElementById("dash-saudacao-mobile");
        if (mobileTitle) mobileTitle.textContent = document.getElementById("dash-saudacao").textContent;
        const barA = document.getElementById("dash-ativos-bar");
        const barI = document.getElementById("dash-inativos-bar");
        if (barA) barA.style.width = pctAtivos + "%";
        if (barI) barI.style.width = pctInativos + "%";
        const ring = document.getElementById("dash-ring");
        if (ring) ring.style.background = total
        ? `conic-gradient(#22b8a0 0 ${pctAtivos}%, #f0607a ${pctAtivos}% 100%)`
        : "conic-gradient(#2d3248 0 100%)";
        renderRecentes();
        }
function abrirProntuarioDireto(id) {
        document.getElementById("prontuario-paciente-id").value = id; // antes de navegar
        navegar('prontuario');  // o navegar já carrega se o paciente for outro
        }
function restaurarProntuarioAtivo() {
        const input = document.getElementById("prontuario-paciente-id");
        if (!input) return;
        if (!input.value) {
        try {
        const salvo = localStorage.getItem("acnut_paciente_ativo");
        if (salvo) input.value = salvo;
        } catch (e) {}
        }
        const id = parseInt(input.value);
        if (id && window.pacienteIdAtivo !== id) carregarProntuarioCompleto();
        }
async function cadastrarPaciente(event) {
        event.preventDefault();
        const dadosPaciente = {
        nome: document.getElementById('p-nome').value,
        email: document.getElementById('p-email').value,
        telefone: document.getElementById('p-telefone').value,
        data_nascimento: document.getElementById('p-data').value,
        sexo: document.getElementById('p-sexo').value,
        status: document.getElementById('p-status').value,
        cpf: document.getElementById('p-cpf').value,
        };
        const editando = pacienteEmEdicaoId !== null;
        const url = editando ? `${API_URL}/pacientes/${pacienteEmEdicaoId}` : `${API_URL}/pacientes`;
        try {
        const response = await fetch(url, {
        method: editando ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(dadosPaciente)
        });
        if (response.ok) {
        alert(editando ? "Paciente atualizado com sucesso!" : "Paciente cadastrado com sucesso!");
        cancelarEdicao();      // limpa o form e volta o botão ao normal
        carregarPacientes();
        } else {
        const erroTexto = await lerErro(response);
        console.error("Detalhe do erro do servidor:", erroTexto);
        alert("Erro ao guardar os dados do paciente: " + erroTexto);
        }
        } catch (error) {
        console.error("Erro na requisição:", error);
        alert("Erro de conexão com o servidor C#.");
        }
        }        
async function gerarAvaliacao(e) {
        e.preventDefault();
        const body = {
        pacienteId: parseInt(document.getElementById("a-paciente-id").value),
        idade: parseInt(document.getElementById("a-idade").value),
        sexo: document.getElementById("a-sexo").value,
        pesoKg: parseFloat(document.getElementById("a-peso").value),
        alturaCm: parseFloat(document.getElementById("a-altura").value),
        nivelAtividade: document.getElementById("a-atividade").value,
        objetivo: document.getElementById("a-objetivo").value,
        percentualGordura: parseFloat(document.getElementById("a-gordura").value || 0),
        massaMagraKg: parseFloat(document.getElementById("a-massa").value || 0)
        };
        try {
        const res = await fetch(`${API_URL}/avaliacoes`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
        });
        if (res.ok) {
        const data = await res.json();
        const calc = data.calculos_python;
        document.getElementById("resultado-avaliacao").classList.remove("hidden");
        document.getElementById("cards-resultado").innerHTML = `
        <div class="bg-slate-900 p-3 rounded border border-slate-700"><span class="text-xs text-slate-400">TMB</span><p class="text-xl font-bold text-emerald-400">${calc.tmb_kcal} kcal</p></div>
        <div class="bg-slate-900 p-3 rounded border border-slate-700"><span class="text-xs text-slate-400">GET</span><p class="text-xl font-bold text-emerald-400">${calc.get_kcal} kcal</p></div>
        <div class="bg-slate-900 p-3 rounded border border-slate-700"><span class="text-xs text-slate-400">Meta Calórica</span><p class="text-xl font-bold text-emerald-400">${calc.meta_calorias} kcal</p></div>
        <div class="bg-slate-900 p-3 rounded border border-slate-700"><span class="text-xs text-slate-400">Proteínas / Carb / Gord</span><p class="text-sm font-semibold text-emerald-400">${calc.macronutrientes.proteinas_g}g / ${calc.macronutrientes.carboidratos_g}g / ${calc.macronutrientes.gorduras_g}g</p></div>
        `;
        } else {
        const erroTexto = await res.text();
        console.error("Detalhe do erro do servidor:", erroTexto);
        alert("Erro ao processar avaliação: " + erroTexto);
        }
        } catch (error) {
        console.error("Erro na requisição:", error);
        alert("Erro de conexão com o servidor.");
        }
        }

// ===================== AVALIAÇÃO: AUTO-PREENCHER IDADE =====================
async function carregarDadosPacienteAvaliacao(pacienteId) {
        if (!pacienteId) return;
        const id = parseInt(pacienteId);
        if (isNaN(id)) return;

        console.log("[Avaliação] Buscando paciente ID:", id);

        try {
                const token = localStorage.getItem("token");
                console.log("[Avaliação] Token:", token ? "presente" : "AUSENTE");

                const res = await fetch(`${API_URL}/pacientes/${id}`, {
                        headers: { "Authorization": `Bearer ${token}` }
                });
                console.log("[Avaliação] Response status:", res.status);

                if (!res.ok) {
                        console.log("[Avaliação] Erro HTTP:", res.status);
                        return;
                }
                const paciente = await res.json();
                console.log("[Avaliação] Dados do paciente:", paciente);

                // Calcular idade a partir da data de nascimento
                if (paciente.data_nascimento) {
                        const nascimento = new Date(paciente.data_nascimento);
                        const hoje = new Date();
                        let idade = hoje.getFullYear() - nascimento.getFullYear();
                        const mes = hoje.getMonth() - nascimento.getMonth();
                        if (mes < 0 || (mes === 0 && hoje.getDate() < nascimento.getDate())) {
                                idade--;
                        }
                        document.getElementById("a-idade").value = idade;
                        // Feedback visual sutil
                        const idadeInput = document.getElementById("a-idade");
                        idadeInput.style.borderColor = "#22b8a0";
                        setTimeout(() => { idadeInput.style.borderColor = ""; }, 1500);
                        console.log("[Avaliação] Idade calculada:", idade);
                } else {
                        console.log("[Avaliação] data_nascimento NÃO encontrado no retorno");
                }

                // Opcional: preencher sexo se vier do paciente
                if (paciente.sexo) {
                        document.getElementById("a-sexo").value = paciente.sexo;
                }
        } catch (err) {
                console.error("[Avaliação] Erro ao buscar paciente:", err);
        }
}

// ===================== CHECK-IN =====================
async function salvarCheckin(e) {
        e.preventDefault();
        const form = e.target;
        const body = {
        pacienteId: parseInt(document.getElementById("c-paciente-id").value),
        pesoAtualKg: parseFloat(document.getElementById("c-peso").value),
        adesaoPlanoPorcentagem: parseInt(document.getElementById("c-adesao").value),
        observacoes: document.getElementById("c-obs").value
        };
        try {
        const res = await fetch(`${API_URL}/checkins`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
        });
        if (res.ok) {
        alert("Check-in semanal registrado!");
        form.reset();
        if (body.observacoes.trim()) {
        await fetch(`${API_URL}/observacoes`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ pacienteId: body.pacienteId, tipo: "semana", data: hojeISO(), texto: body.observacoes })
        });
        if (body.pacienteId === window.pacienteIdAtivo) carregarObservacoes(body.pacienteId);
        }
        // Se o paciente tiver metas ativas, pergunta como foram
        await abrirModalAcompanhamento(body.pacienteId, null, true);
        } else {
        alert("Erro ao registrar check-in: " + await lerErro(res));
        }
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }
function calcularMassaMagraAutomatica() {
        const pesoInput = document.getElementById("a-peso");
        const gorduraInput = document.getElementById("a-gordura");
        const massaInput = document.getElementById("a-massa");
        if (!pesoInput || !gorduraInput || !massaInput) return;
        const peso = parseFloat(pesoInput.value) || 0;
        const percentualGordura = parseFloat(gorduraInput.value) || 0;
        if (peso > 0 && percentualGordura > 0) {
        const massaMagra = peso * (1 - percentualGordura / 100);
        massaInput.value = massaMagra.toFixed(1);
        }
        }
        document.addEventListener("DOMContentLoaded", () => {
        const pesoInput = document.getElementById("a-peso");
        const gorduraInput = document.getElementById("a-gordura");
        if (pesoInput) pesoInput.addEventListener("input", calcularMassaMagraAutomatica);
        if (gorduraInput) gorduraInput.addEventListener("input", calcularMassaMagraAutomatica);

        // Avaliação: auto-buscar dados do paciente ao digitar ID (com debounce)
        const pacienteIdInput = document.getElementById("a-paciente-id");
        if (pacienteIdInput) {
                let timeoutId;
                pacienteIdInput.addEventListener("input", (e) => {
                        clearTimeout(timeoutId);
                        timeoutId = setTimeout(() => {
                                carregarDadosPacienteAvaliacao(e.target.value);
                        }, 500);
                });
        }
});
async function salvarAnamneseProntuario() {
        const pacienteId = parseInt(document.getElementById("prontuario-paciente-id").value) || window.pacienteIdAtivo;
        if (!pacienteId) {
        alert("Abra o prontuário de um paciente primeiro.");
        return;
        }
        const body = {
        pacienteId: pacienteId,
        historicoMedico: document.getElementById("p-an-historico").value,
        alergias: document.getElementById("p-an-alergias").value,
        intolerancias: document.getElementById("p-an-intolerancias").value,
        rotinaDiaria: document.getElementById("p-an-rotina").value,
        historicoFamiliar: document.getElementById("p-an-familiar").value
        };
        try {
        const res = await fetch(`${API_URL}/anamnese`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
        });
        if (res.ok) {
        alert("Anamnese salva com sucesso!");
        } else {
        alert("Erro ao salvar anamnese: " + await res.text());
        }
        } catch (err) {
        console.error("Erro ao salvar anamnese:", err);
        alert("Erro de conexão com o servidor.");
        }
        }
function trocarAbaProntuario(nomeAba) {
        // Esconde todos os conteúdos das abas
        document.querySelectorAll(".aba-conteudo").forEach(el => el.classList.add("hidden"));
        // Reseta a cor de todas as abas
        document.querySelectorAll(".aba-item").forEach(el => {
        el.classList.remove("bg-emerald-600", "text-white");
        el.classList.add("bg-slate-800", "text-slate-400");
        });
        // Mostra o conteúdo da aba selecionada
        const abaAlvo = document.getElementById(`sub-${nomeAba}`);
        if (abaAlvo) abaAlvo.classList.remove("hidden");
        // Destaca o botão da aba selecionada
        const btnTab = document.getElementById(`tab-${nomeAba}`);
        if (btnTab) {
        btnTab.classList.remove("bg-slate-800", "text-slate-400");
        btnTab.classList.add("bg-emerald-600", "text-white");
        }
        if (nomeAba === "evolucao" && graficoEvolucaoDetalhe) graficoEvolucaoDetalhe.resize();
                if (nomeAba === "historico" && window.pacienteIdAtivo) carregarHistorico(window.pacienteIdAtivo);
        }
async function carregarProntuarioCompleto() {
        const pacienteId = parseInt(document.getElementById("prontuario-paciente-id").value);
        if (!pacienteId) {
        alert("Por favor, introduza o ID do paciente.");
        return;
        }
        try {
        const resPacientes = await fetch(`${API_URL}/pacientes`);
        const pacientes = await resPacientes.json();
        const paciente = pacientes.find(p => p.id === pacienteId);
        if (!paciente) {
        try { localStorage.removeItem("acnut_paciente_ativo"); } catch (e) {}
        alert("Paciente não encontrado!");
        return;
        }
        window.pacienteIdAtivo = pacienteId;
        try { localStorage.setItem("acnut_paciente_ativo", String(pacienteId)); } catch (e) {}
        // Cabeçalho
        document.getElementById("prontuario-cabecalho-paciente").classList.remove("hidden");
        document.getElementById("p-cab-nome").innerText = `${paciente.nome} (#${paciente.id})`;
        document.getElementById("p-cab-email").innerText = paciente.email;
        // Perfil
        const campo = (rotulo, valor) =>
        `<div class="bg-slate-900 p-3 rounded border border-slate-700"><strong>${rotulo}:</strong> ${escaparHtml(valor)}</div>`;
        document.getElementById("conteudo-perfil").innerHTML =
        campo("Nome", paciente.nome) +
        campo("E-mail", paciente.email) +
        campo("Telefone", paciente.telefone || "Não informado") +
        campo("Data de Nascimento", formatarData(paciente.data_nascimento)) +
        campo("Sexo", paciente.sexo) +
        campo("Estado", paciente.status);
        try {
    const ra = await fetch(`${API_URL}/acessos/${pacienteId}`);
    const ac = ra.ok ? await ra.json() : [];
    document.getElementById("conteudo-perfil").innerHTML +=
        campo("CPF", paciente.cpf ? formatarCpf(paciente.cpf) : "Não cadastrado") +
        campo("Último acesso à área do paciente", ac.length ? new Date(ac[0].em).toLocaleString("pt-BR") : "Nunca acessou");
} catch {}
        // Anamnese (o backend devolve campos vazios se não existir)
        const resAnamnese = await fetch(`${API_URL}/anamnese/${pacienteId}`);
        const an = resAnamnese.ok ? await resAnamnese.json() : {};
        document.getElementById("p-an-historico").value = an.historico_medico || "";
        document.getElementById("p-an-alergias").value = an.alergias || "";
        document.getElementById("p-an-intolerancias").value = an.intolerancias || "";
        document.getElementById("p-an-rotina").value = an.rotina_diaria || "";
        document.getElementById("p-an-familiar").value = an.historico_familiar || "";
        // Plano, gráfico e orientações em paralelo
        await Promise.all([
        carregarPlanoAlimentarProntuario(pacienteId),
        carregarOrientacoes(pacienteId),
        carregarMetas(pacienteId),
        carregarEvolucao(pacienteId),
        carregarExames(pacienteId),
        carregarObservacoes(pacienteId)
        ]);
        } catch (err) {
        console.error("Erro ao carregar prontuário:", err);
        }
        }
// ========================== PLANO ALIMENTAR (refeições, opções, substituições, versões) ===============================
let planoAtual = { planoId: null, refeicoes: [] };
let formItem = {};
let formRefeicao = { id: null };
const BTN_MINI = "px-2.5 py-1 rounded-lg text-[11px] font-semibold transition";
function abrirModal(id) {
        const el = document.getElementById(id);
        el.classList.remove("hidden");
        el.classList.add("flex");
        }
function fecharModal(id) {
        const el = document.getElementById(id);
        el.classList.add("hidden");
        el.classList.remove("flex");
        }
async function lerErro(res) {
        try {
        const j = await res.clone().json();
        return j.mensagem || j.title || JSON.stringify(j);
        } catch {
        return await res.text();
        }
        }
function exigirPaciente() {
        if (!window.pacienteIdAtivo) {
        alert("Abra o prontuário de um paciente primeiro.");
        return false;
        }
        return true;
        }
// ---------- Agrupamento e totais ----------
function agruparOpcoes(refeicao) {
        const principais = refeicao.itens.filter(i => !i.substituiItemId);
        const mapa = {};
        principais.forEach(i => {
        (mapa[i.opcao] = mapa[i.opcao] || []).push({
        ...i,
        substituicoes: refeicao.itens.filter(s => s.substituiItemId === i.id)
        });
        });
        return Object.keys(mapa).map(Number).sort((a, b) => a - b)
        .map(n => ({ numero: n, itens: mapa[n] }));
        }
function totaisPlano(plano) {
        const t = { kcal: 0, prot: 0, carbo: 0, gord: 0 };
        (plano.refeicoes || []).forEach(r => {
        const primeira = agruparOpcoes(r)[0];   // Opção 1 de cada refeição
        if (!primeira) return;
        primeira.itens.forEach(i => {
        t.kcal += Number(i.kcal);
        t.prot += Number(i.proteinas);
        t.carbo += Number(i.carbos);
        t.gord += Number(i.gorduras);
        });
        });
        return t;
        }
// ---------- Carregar e desenhar ----------
async function carregarPlanoAlimentarProntuario(pacienteId) {
        const el = document.getElementById("prontuario-plano-resultado");
        if (!el) return;
        try {
        const res = await fetch(`${API_URL}/planos/${pacienteId}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        planoAtual = await res.json();
        renderizarPlano();
        renderizarSubstituicoes();
        } catch (err) {
        console.error("Erro ao carregar plano alimentar:", err);
        el.innerHTML = `<p class="text-rose-400 text-sm">Não foi possível carregar o plano.</p>`;
        }
        }
function linhaItem(i, ehSub) {
        const qtd = Number(i.quantidadeG) > 0 ? `${i.quantidadeG}g` : "";
        const medida = i.medidaCaseira ? ` • ${escaparHtml(i.medidaCaseira)}` : "";
        const classeLinha = ehSub
        ? "ml-6 mt-1.5 pl-3 border-l-2 border-amber-500/40"
        : "py-2.5 border-b border-slate-800";
        const rotuloOu = ehSub
        ? `<span class="text-[10px] font-bold uppercase text-amber-400 mr-1.5">ou</span>`
        : "";
        const btnSubstituir = ehSub ? "" : `
        <button title="Adicionar substituição" onclick="abrirModalItem(${i.refeicaoId}, ${i.opcao}, ${i.id})"
        class="${BTN_MINI} bg-amber-500/15 text-amber-400 hover:bg-amber-500 hover:text-white"><i class="fa-solid fa-right-left"></i></button>`;
        return `
        <div class="flex flex-wrap items-center justify-between gap-3 ${classeLinha}">
        <div class="min-w-0">
        ${rotuloOu}<span class="text-sm text-white font-medium">${escaparHtml(i.alimento)}</span>
        <span class="text-xs text-slate-400 ml-1">${qtd}${medida}</span>
        </div>
        <div class="flex items-center gap-3">
        <span class="text-[11px] text-slate-400 font-mono">${i.kcal} kcal · P${i.proteinas} C${i.carbos} G${i.gorduras}</span>
        <div class="flex gap-1.5">
        ${btnSubstituir}
        <button title="Editar" onclick="abrirModalItem(${i.refeicaoId}, ${i.opcao}, ${i.substituiItemId || null}, ${i.id})"
        class="${BTN_MINI} bg-blue-500/15 text-blue-400 hover:bg-blue-500 hover:text-white"><i class="fa-solid fa-pen"></i></button>
        <button title="Excluir" onclick="excluirItem(${i.id})"
        class="${BTN_MINI} bg-rose-500/15 text-rose-400 hover:bg-rose-500 hover:text-white"><i class="fa-solid fa-trash"></i></button>
        </div>
        </div>
        </div>`;
        }
function htmlOpcao(r, o, idx) {
        const itens = o.itens.map(i =>
        linhaItem(i, false) + i.substituicoes.map(s => linhaItem(s, true)).join("")
        ).join("");
        return `
        <div>
        <div class="flex items-center justify-between mb-1">
        <span class="text-xs font-bold uppercase tracking-wider text-emerald-400">Opção ${idx + 1}</span>
        <div class="flex gap-1.5">
        <button onclick="abrirModalItem(${r.id}, ${o.numero})"
        class="${BTN_MINI} bg-emerald-500/15 text-emerald-400 hover:bg-emerald-500 hover:text-white">+ Alimento</button>
        <button onclick="duplicarOpcao(${r.id}, ${o.numero})"
        class="${BTN_MINI} bg-blue-500/15 text-blue-400 hover:bg-blue-500 hover:text-white"><i class="fa-solid fa-copy mr-1"></i>Duplicar</button>
        <button onclick="excluirOpcao(${r.id}, ${o.numero})"
        class="${BTN_MINI} bg-slate-700 text-slate-300 hover:bg-rose-500 hover:text-white">Excluir opção</button>
        </div>
        </div>
        ${itens}
        </div>`;
        }
function htmlRefeicao(r) {
        const opcoes = agruparOpcoes(r);
        const proxima = opcoes.length ? Math.max(...opcoes.map(o => o.numero)) + 1 : 1;
        const obs = r.observacoes
        ? `<p class="text-xs text-slate-400 mt-1 whitespace-pre-wrap">${escaparHtml(r.observacoes)}</p>`
        : "";
        const corpo = opcoes.length
        ? opcoes.map((o, idx) => htmlOpcao(r, o, idx)).join("")
        : `<p class="text-sm text-slate-500">Nenhum alimento nesta refeição ainda.</p>`;
        return `
        <div class="bg-slate-900/60 border border-slate-700/70 rounded-2xl overflow-hidden">
        <div class="flex flex-wrap items-start justify-between gap-3 px-5 py-4 border-b border-slate-700/60">
        <div class="min-w-0">
        <h4 class="font-bold text-white">${escaparHtml(r.nome)}
        <span class="text-emerald-400 text-sm font-semibold ml-2">${escaparHtml(r.horario)}</span>
        </h4>
        ${obs}
        </div>
        <div class="flex gap-1.5">
        ${opcoes.length ? `<button onclick="abrirModalItem(${r.id}, ${proxima})"
        class="${BTN_MINI} bg-emerald-500/15 text-emerald-400 hover:bg-emerald-500 hover:text-white"><i class="fa-solid fa-layer-group mr-1"></i>Nova opção</button>` : ""}
        <button onclick="abrirModalRefeicao(${r.id})"
        class="${BTN_MINI} bg-blue-500/15 text-blue-400 hover:bg-blue-500 hover:text-white"><i class="fa-solid fa-pen mr-1"></i>Editar</button>
        <button onclick="excluirRefeicao(${r.id})"
        class="${BTN_MINI} bg-rose-500/15 text-rose-400 hover:bg-rose-500 hover:text-white"><i class="fa-solid fa-trash mr-1"></i>Excluir</button>
        </div>
        </div>
        <div class="p-5 space-y-5">
        ${corpo}
        <button onclick="abrirModalItem(${r.id}, ${proxima})"
        class="${BTN_MINI} border border-dashed border-slate-600 text-slate-300 hover:border-emerald-500 hover:text-emerald-400">
        <i class="fa-solid fa-plus mr-1"></i>${opcoes.length ? "Nova opção" : "Adicionar alimento"}
        </button>
        </div>
        </div>`;
        }
function renderizarPlano() {
        const el = document.getElementById("prontuario-plano-resultado");
        if (!el) return;
        const refeicoes = planoAtual.refeicoes || [];
        if (!refeicoes.length) {
        el.innerHTML = `
        <div class="bg-slate-900/50 border border-slate-700/60 rounded-xl p-5">
        <p class="text-sm text-slate-400">Este paciente ainda não tem plano. Clique em <strong>Nova refeição</strong> para começar.</p>
        </div>`;
        return;
        }
        const t = totaisPlano(planoAtual);
        const card = (rotulo, valor) => `
        <div class="bg-emerald-950/40 border border-emerald-500/20 rounded-xl p-3">
        <p class="text-[10px] uppercase tracking-wider text-slate-500">${rotulo}</p>
        <p class="text-lg font-extrabold text-emerald-400">${valor}</p>
        </div>`;
        el.innerHTML = `
        <div>
        <p class="text-[11px] text-slate-500 mb-2">Total do dia (Opção 1 de cada refeição)</p>
        <div class="grid grid-cols-2 md:grid-cols-4 gap-3">
        ${card("Calorias", t.kcal.toFixed(0) + " kcal")}
        ${card("Proteínas", t.prot.toFixed(1) + " g")}
        ${card("Carboidratos", t.carbo.toFixed(1) + " g")}
        ${card("Gorduras", t.gord.toFixed(1) + " g")}
        </div>
        </div>
        ${refeicoes.map(htmlRefeicao).join("")}`;
        }
function renderizarSubstituicoes() {
        const el = document.getElementById("substituicoes-resultado");
        if (!el) return;
        const blocos = (planoAtual.refeicoes || []).map(r => {
        const comSub = r.itens
        .filter(i => !i.substituiItemId)
        .map(i => ({ i, subs: r.itens.filter(s => s.substituiItemId === i.id) }))
        .filter(x => x.subs.length);
        if (!comSub.length) return "";
        const detalhe = x => {
        const qtd = Number(x.quantidadeG) > 0 ? ` — ${x.quantidadeG}g` : "";
        const medida = x.medidaCaseira ? ` (${escaparHtml(x.medidaCaseira)})` : "";
        return `${escaparHtml(x.alimento)}${qtd}${medida}`;
        };
        return `
        <div class="bg-slate-900/60 border border-slate-700/70 rounded-2xl p-5 space-y-3">
        <h4 class="font-bold text-white">${escaparHtml(r.nome)}</h4>
        ${comSub.map(x => `
        <div class="bg-slate-800/60 rounded-xl p-3">
        <p class="text-sm text-white font-medium">${detalhe(x.i)}</p>
        <ul class="mt-1.5 space-y-1">
        ${x.subs.map(s => `<li class="text-sm text-amber-300"><span class="text-slate-500">↳ pode trocar por:</span> ${detalhe(s)}</li>`).join("")}
        </ul>
        </div>`).join("")}
        </div>`;
        }).join("");
        el.innerHTML = blocos || `
        <div class="bg-slate-900/50 border border-slate-700/60 rounded-xl p-5">
        <p class="text-sm text-slate-400">Nenhuma substituição cadastrada ainda.</p>
        </div>`;
        }
// ---------- Refeição (criar / editar / excluir) ----------
const normalizar = s => (s || "").normalize("NFD").replace(/[\u0300-\u036f]/g, "").trim().toLowerCase();
function abrirModalRefeicao(id) {
        if (!exigirPaciente()) return;
        const r = id ? planoAtual.refeicoes.find(x => x.id === id) : null;
        formRefeicao = { id: r ? r.id : null };
        document.getElementById("mr-titulo").innerText = r ? "Editar refeição" : "Nova refeição";
        document.getElementById("mr-nome").value = r ? r.nome : "";
        document.getElementById("mr-horario").value = r ? r.horario : "";
        document.getElementById("mr-obs").value = r ? r.observacoes : "";
        abrirModal("modal-refeicao");
        }
async function salvarRefeicao(e) {
        e.preventDefault();
        if (!exigirPaciente()) return;
        const editando = formRefeicao.id !== null;
        const body = {
        pacienteId: window.pacienteIdAtivo,
        nomeRefeicao: document.getElementById("mr-nome").value,
        horario: document.getElementById("mr-horario").value,
        observacoes: document.getElementById("mr-obs").value
        };
        const existente = (planoAtual.refeicoes || []).find(
        r => normalizar(r.nome) === normalizar(body.nomeRefeicao) && r.id !== formRefeicao.id
        );
        if (existente) {
        alert(`Já existe "${existente.nome}" neste plano.\n\nPara criar outra opção dessa refeição, use o botão "Nova opção" dentro dela.`);
        return;
        }
        try {
        const res = await fetch(
        editando ? `${API_URL}/planos/refeicao/${formRefeicao.id}` : `${API_URL}/planos/refeicao`,
        {
        method: editando ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
        });
        if (res.ok) {
        fecharModal("modal-refeicao");
        await carregarPlanoAlimentarProntuario(window.pacienteIdAtivo);
        } else {
        alert(await lerErro(res));
        }
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }
async function excluirRefeicao(id) {
        if (!confirm("Excluir esta refeição e todos os alimentos dela?")) return;
        const res = await fetch(`${API_URL}/planos/refeicao/${id}`, { method: "DELETE" });
        if (res.ok) await carregarPlanoAlimentarProntuario(window.pacienteIdAtivo);
        else alert(await lerErro(res));
        }
async function excluirOpcao(refeicaoId, opcao) {
        if (!confirm("Excluir esta opção inteira (alimentos e substituições)?")) return;
        const res = await fetch(`${API_URL}/planos/refeicao/${refeicaoId}/opcao/${opcao}`, { method: "DELETE" });
        if (res.ok) await carregarPlanoAlimentarProntuario(window.pacienteIdAtivo);
        else alert(await lerErro(res));
        }
async function duplicarOpcao(refeicaoId, opcao) {
        try {
        const res = await fetch(`${API_URL}/planos/refeicao/${refeicaoId}/opcao/${opcao}/duplicar`, { method: "POST" });
        if (res.ok) await carregarPlanoAlimentarProntuario(window.pacienteIdAtivo);
        else alert(await lerErro(res));
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }
function todosItens() {
        return (planoAtual.refeicoes || []).flatMap(r => r.itens);
        }
function abrirModalItem(refeicaoId, opcao, substituiId = null, itemId = null) {
        if (!exigirPaciente()) return;
        formItem = { refeicaoId, opcao, substituiId, itemId, alimentoId: null };
        const item = itemId ? todosItens().find(i => i.id === itemId) : null;
        const original = substituiId ? todosItens().find(i => i.id === substituiId) : null;
let titulo = "Adicionar alimento";
        if (itemId) titulo = substituiId ? "Editar substituição" : "Editar alimento";
        else if (substituiId) titulo = "Adicionar substituição";
        document.getElementById("mi-titulo").innerText = titulo;
        const ctx = document.getElementById("mi-contexto");
        if (original) {
        ctx.innerText = `Substituindo: ${original.alimento}`;
        ctx.classList.remove("hidden");
        } else {
        ctx.classList.add("hidden");
        }
        document.getElementById("mi-nome").value = item ? item.alimento : "";
        document.getElementById("mi-qtd").value = item && item.quantidadeG ? item.quantidadeG : "";
        document.getElementById("mi-medida").value = item ? item.medidaCaseira : "";
        document.getElementById("mi-kcal").value = item && item.kcal ? item.kcal : "";
        document.getElementById("mi-prot").value = item && item.proteinas ? item.proteinas : "";
        document.getElementById("mi-carbo").value = item && item.carbos ? item.carbos : "";
        document.getElementById("mi-gord").value = item && item.gorduras ? item.gorduras : "";

        // Reset novos campos de medida caseira
        document.getElementById("mi-medida-caseira").value = "";
        document.getElementById("mi-qtd-medida").value = "";
        document.getElementById("mi-obs-medida").value = "";
        document.getElementById("mi-gramas-calculadas").classList.add("hidden");
        mostrarContainerMedidaCaseira(false);

        setupAlimentoAutocomplete();
        abrirModal("modal-item");
        }

// ===================== SUBSTITUIÇÃO RÁPIDA (do prontuário) =====================
function abrirModalSubstituicaoRapida() {
        if (!exigirPaciente()) return;
        // Limpar formulário
        document.getElementById("msr-refeicao").innerHTML = '<option value="">Selecione uma refeição...</option>';
        document.getElementById("msr-opcao-select").innerHTML = '<option value="">Selecione uma opção...</option>';
        document.getElementById("msr-item-original").innerHTML = '<option value="">Selecione um alimento...</option>';
        document.getElementById("msr-refeicao-id").value = "";
        document.getElementById("msr-opcao").value = "";
        document.getElementById("msr-item-id-original").value = "";
        document.getElementById("msr-busca-taco").value = "";
        document.getElementById("msr-alimento-manual").value = "";
        document.getElementById("msr-kcal").value = "";
        document.getElementById("msr-prot").value = "";
        document.getElementById("msr-carbo").value = "";
        document.getElementById("msr-gord").value = "";
        document.getElementById("msr-qtd").value = "100";
        document.getElementById("msr-medida").value = "";
        document.getElementById("msr-resultados-taco").classList.add("hidden");
        document.getElementById("msr-resultados-taco").innerHTML = "";

        // Popular refeições
        if (planoAtual.refeicoes && planoAtual.refeicoes.length) {
                planoAtual.refeicoes.forEach(r => {
                        const opt = document.createElement("option");
                        opt.value = r.id;
                        opt.textContent = r.nome + (r.horario ? ` (${r.horario})` : "");
                        document.getElementById("msr-refeicao").appendChild(opt);
                });
        } else {
                alert("Nenhuma refeição cadastrada. Crie uma na aba 'Plano alimentar' primeiro.");
                return;
        }

        abrirModal("modal-substituicao-rapida");
}

function atualizarOpcoesSubstituicaoRapida() {
        const refeicaoId = Number(document.getElementById("msr-refeicao").value);
        const opcaoSelect = document.getElementById("msr-opcao-select");
        const itemSelect = document.getElementById("msr-item-original");

        opcaoSelect.innerHTML = '<option value="">Selecione uma opção...</option>';
        itemSelect.innerHTML = '<option value="">Selecione um alimento...</option>';
        document.getElementById("msr-opcao").value = "";
        document.getElementById("msr-item-id-original").value = "";

        if (!refeicaoId) return;

        const refeicao = planoAtual.refeicoes.find(r => r.id === refeicaoId);
        if (!refeicao) return;

        document.getElementById("msr-refeicao-id").value = refeicaoId;

        // Opções únicas
        const opcoes = [...new Set(refeicao.itens.map(i => i.opcao))].sort((a, b) => a - b);
        opcoes.forEach(op => {
                const opt = document.createElement("option");
                opt.value = op;
                opt.textContent = `Opção ${op}`;
                opcaoSelect.appendChild(opt);
        });
}

function atualizarItensSubstituicaoRapida() {
        const refeicaoId = Number(document.getElementById("msr-refeicao").value);
        const opcao = Number(document.getElementById("msr-opcao-select").value);
        const itemSelect = document.getElementById("msr-item-original");

        itemSelect.innerHTML = '<option value="">Selecione um alimento...</option>';
        document.getElementById("msr-item-id-original").value = "";

        if (!refeicaoId || !opcao) return;

        const refeicao = planoAtual.refeicoes.find(r => r.id === refeicaoId);
        if (!refeicao) return;

        document.getElementById("msr-opcao").value = opcao;

        // Apenas itens principais (não substituições)
        const itensPrincipais = refeicao.itens.filter(i => i.opcao === opcao && !i.substituiItemId);
        itensPrincipais.forEach(item => {
                const opt = document.createElement("option");
                opt.value = item.id;
                const medida = item.medidaCaseira ? ` (${item.medidaCaseira})` : "";
                const qtd = item.quantidadeG ? ` — ${item.quantidadeG}g` : "";
                opt.textContent = `${item.alimento}${qtd}${medida}`;
                itemSelect.appendChild(opt);
        });
}

async function buscarTacoSubstituicao(query) {
        const resultadosDiv = document.getElementById("msr-resultados-taco");
        if (query.length < 2) {
                resultadosDiv.classList.add("hidden");
                resultadosDiv.innerHTML = "";
                return;
        }

        try {
                const res = await fetch(`${API_URL}/alimentos/buscar?q=${encodeURIComponent(query)}&limite=10`, {
                        headers: { "Authorization": `Bearer ${localStorage.getItem("token")}` }
                });
                if (!res.ok) throw new Error("Erro na busca");
                const alimentos = await res.json();

                if (alimentos.length === 0) {
                        resultadosDiv.innerHTML = '<p class="text-xs text-slate-500 px-2 py-1">Nenhum alimento encontrado</p>';
                        resultadosDiv.classList.remove("hidden");
                        return;
                }

                resultadosDiv.innerHTML = alimentos.map(a => `
                        <button type="button" onclick="selecionarTacoSubstituicao(${JSON.stringify(a).replace(/"/g, '"')})"
                                class="w-full text-left px-2 py-1.5 rounded-lg hover:bg-slate-700 text-sm transition flex items-center justify-between">
                                <span>${a.nome} <span class="text-slate-500">(${a.quantidade}g)</span></span>
                                <span class="text-xs text-amber-400">${a.calorias}kcal</span>
                        </button>
                `).join("");
                resultadosDiv.classList.remove("hidden");
        } catch (err) {
                console.error("Erro busca TACO:", err);
        }
}

function selecionarTacoSubstituicao(alimento) {
        document.getElementById("msr-alimento-manual").value = alimento.nome;
        document.getElementById("msr-kcal").value = alimento.calorias || "";
        document.getElementById("msr-prot").value = alimento.proteinas || "";
        document.getElementById("msr-carbo").value = alimento.carbos || "";
        document.getElementById("msr-gord").value = alimento.gorduras || "";
        document.getElementById("msr-qtd").value = alimento.quantidade || 100;
        document.getElementById("msr-resultados-taco").classList.add("hidden");
        document.getElementById("msr-busca-taco").value = "";
}

async function salvarSubstituicaoRapida(e) {
        e.preventDefault();
        if (!exigirPaciente()) return;

        const itemIdOriginal = Number(document.getElementById("msr-item-id-original").value);
        const refeicaoId = Number(document.getElementById("msr-refeicao-id").value);
        const opcao = Number(document.getElementById("msr-opcao").value);
        const alimentoSubstituto = document.getElementById("msr-alimento-manual").value.trim();
        const qtd = Number(document.getElementById("msr-qtd").value);
        const medida = document.getElementById("msr-medida").value.trim();
        const kcal = Number(document.getElementById("msr-kcal").value) || null;
        const prot = Number(document.getElementById("msr-prot").value) || null;
        const carbo = Number(document.getElementById("msr-carbo").value) || null;
        const gord = Number(document.getElementById("msr-gord").value) || null;

        if (!itemIdOriginal || !refeicaoId || !opcao) {
                alert("Selecione a refeição, opção e o alimento original.");
                return;
        }
        if (!alimentoSubstituto) {
                alert("Informe o alimento substituto (busque no TACO ou digite manualmente).");
                return;
        }
        if (!qtd || qtd <= 0) {
                alert("Informe a quantidade em gramas/ml.");
                return;
        }

        const btn = e.target.querySelector('button[type="submit"]');
        const txtOriginal = btn.innerHTML;
        btn.disabled = true;
        btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-1"></i> Salvando...';

        try {
                const body = {
                        pacienteId: window.pacienteIdAtivo,
                        refeicaoId,
                        opcao,
                        substituiItemId: itemIdOriginal,
                        alimento: alimentoSubstituto,
                        quantidadeG: qtd,
                        medidaCaseira: medida || null,
                        kcal,
                        proteinas: prot,
                        carbos: carbo,
                        gorduras: gord
                };

                const res = await fetch(`${API_URL}/planos/item`, {
                        method: "POST",
                        headers: { "Content-Type": "application/json" },
                        body: JSON.stringify(body)
                });

                if (res.ok) {
                        fecharModal("modal-substituicao-rapida");
                        await carregarPlanoAlimentarProntuario(window.pacienteIdAtivo);
                        // Atualizar a aba de substituições
                        renderizarSubstituicoes();
                } else {
                        alert(await lerErro(res));
                }
        } catch (err) {
                console.error(err);
                alert("Erro de conexão com o servidor.");
        } finally {
                btn.disabled = false;
                btn.innerHTML = txtOriginal;
        }
}

let alimentoAutocompleteTimer = null;
let alimentoAutocompleteAbort = null;

function setupAlimentoAutocomplete() {
        const input = document.getElementById("mi-nome");
        const dropdown = document.getElementById("mi-sugestoes");
        if (!input || !dropdown) return;

        // Remove listeners antigos
        input.removeEventListener("input", onAlimentoInput);
        input.removeEventListener("blur", onAlimentoBlur);
        input.removeEventListener("focus", onAlimentoFocus);
        document.removeEventListener("click", onAlimentoClickOutside);

        // Adiciona novos
        input.addEventListener("input", onAlimentoInput);
        input.addEventListener("blur", onAlimentoBlur);
        input.addEventListener("focus", onAlimentoFocus);
        document.addEventListener("click", onAlimentoClickOutside);
}

async function onAlimentoInput(e) {
        const input = e.target;
        const dropdown = document.getElementById("mi-sugestoes");
        const query = input.value.trim();

        // Cancela busca anterior
        if (alimentoAutocompleteTimer) clearTimeout(alimentoAutocompleteTimer);
        if (alimentoAutocompleteAbort) alimentoAutocompleteAbort.abort();

        if (query.length < 2) {
        dropdown.classList.add("hidden");
        return;
        }

        // Debounce 300ms
        alimentoAutocompleteTimer = setTimeout(async () => {
        alimentoAutocompleteAbort = new AbortController();
        try {
                const res = await fetch(`${API_URL}/alimentos/buscar?q=${encodeURIComponent(query)}&limite=10`, {
                signal: alimentoAutocompleteAbort.signal,
                headers: { "Authorization": `Bearer ${localStorage.getItem("token")}` }
                });
                if (!res.ok) throw new Error("Erro na busca");
                const alimentos = await res.json();
                renderAlimentoSugestoes(alimentos);
        } catch (err) {
                if (err.name !== "AbortError") console.error("Erro autocomplete:", err);
        }
        }, 300);
}

function onAlimentoBlur() {
        // Delay para permitir clique no dropdown
        setTimeout(() => {
        const dropdown = document.getElementById("mi-sugestoes");
        if (dropdown) dropdown.classList.add("hidden");
        }, 200);
}

function onAlimentoFocus(e) {
        const input = e.target;
        if (input.value.trim().length >= 2) {
        onAlimentoInput(e);
        }
}

function onAlimentoClickOutside(e) {
        const dropdown = document.getElementById("mi-sugestoes");
        const input = document.getElementById("mi-nome");
        if (dropdown && input && !dropdown.contains(e.target) && e.target !== input) {
        dropdown.classList.add("hidden");
        }
}

function renderAlimentoSugestoes(alimentos) {
        const dropdown = document.getElementById("mi-sugestoes");
        if (!dropdown) return;

        if (!alimentos.length) {
        dropdown.classList.add("hidden");
        return;
        }

        dropdown.innerHTML = alimentos.map(a => `
        <button type="button" class="w-full text-left px-3 py-2 hover:bg-slate-700 transition-colors flex items-center gap-2"
                onclick="selecionarAlimento(${a.id})">
            <span class="font-medium text-white">${escaparHtml(a.nome)}</span>
            <span class="text-xs text-slate-400">${escaparHtml(a.categoria)}</span>
            <span class="text-xs text-emerald-400 ml-auto">${a.calorias_100g} kcal/100g</span>
        </button>
        `).join("");
        dropdown.classList.remove("hidden");
}

async function selecionarAlimento(id) {
        const dropdown = document.getElementById("mi-sugestoes");
        dropdown.classList.add("hidden");

        // Busca detalhes completos do alimento
        try {
        const res = await fetch(`${API_URL}/alimentos/${id}`, {
                headers: { "Authorization": `Bearer ${localStorage.getItem("token")}` }
        });
        if (!res.ok) throw new Error("Erro ao buscar detalhes");
        const a = await res.json();

        // Preenche nome e campos nutricionais (por 100g)
        document.getElementById("mi-nome").value = a.nome;
        document.getElementById("mi-kcal").value = a.calorias_100g ?? "";
        document.getElementById("mi-prot").value = a.proteinas_100g ?? "";
        document.getElementById("mi-carbo").value = a.carboidratos_100g ?? "";
        document.getElementById("mi-gord").value = a.gorduras_100g ?? "";
        } catch (err) {
        console.error("Erro ao carregar detalhes do alimento:", err);
        alert("Não foi possível carregar os detalhes do alimento.");
        }
}

// ======================== MEDIDA CASEIRA ========================
let medidaCaseiraCache = new Map(); // cache: alimento_id -> medidas[]

async function carregarMedidasCaseira(alimentoId) {
        if (medidaCaseiraCache.has(alimentoId)) {
                return medidaCaseiraCache.get(alimentoId);
        }
        try {
                const res = await fetch(`${API_URL}/alimentos/${alimentoId}/medidas`, {
                        headers: { "Authorization": `Bearer ${getToken()}` }
                });
                if (!res.ok) throw new Error("Erro ao buscar medidas");
                const medidas = await res.json();
                medidaCaseiraCache.set(alimentoId, medidas);
                return medidas;
        } catch (err) {
                console.error("Erro ao carregar medidas caseiras:", err);
                return [];
        }
}

function popularDropdownMedidas(medidas) {
        const select = document.getElementById("mi-medida-caseira");
        if (!select) return;
        const valorAtual = select.value;
        select.innerHTML = '<option value="">Selecione uma medida...</option>' +
                medidas.map(m => {
                        const label = m.observacao ? `${m.medida} (${m.observacao})` : m.medida;
                        return `<option value="${escaparHtml(m.medida)}" data-obs="${escaparHtml(m.observacao || "")}" data-gramas="${m.gramas_por_unidade}">${escaparHtml(label)}</option>`;
                }).join("");
        // Restaura valor se ainda existe
        if (valorAtual && [...select.options].some(o => o.value === valorAtual)) {
                select.value = valorAtual;
        }
}

function atualizarGramasPorMedida() {
        const select = document.getElementById("mi-medida-caseira");
        const qtdInput = document.getElementById("mi-qtd-medida");
        const gramasDisplay = document.getElementById("mi-gramas-calculadas");
        const miQtd = document.getElementById("mi-qtd");

        if (!select || !qtdInput) return;

        const option = select.options[select.selectedIndex];
        const quantidade = parseFloat(qtdInput.value) || 0;

        if (option && option.dataset.gramas && quantidade > 0) {
                const gramasPorUnidade = parseFloat(option.dataset.gramas);
                const gramasTotais = gramasPorUnidade * quantidade;
                miQtd.value = gramasTotais.toFixed(1);
                gramasDisplay.textContent = `→ ${gramasTotais.toFixed(1)}g (${quantidade} × ${gramasPorUnidade}g)`;
                gramasDisplay.classList.remove("hidden");
        } else {
                gramasDisplay.classList.add("hidden");
        }
}

function mostrarContainerMedidaCaseira(mostrar) {
        const container = document.getElementById("mi-medida-container");
        if (container) {
                if (mostrar) container.classList.remove("hidden");
                else container.classList.add("hidden");
        }
}

async function selecionarAlimento(id) {
        const dropdown = document.getElementById("mi-sugestoes");
        dropdown.classList.add("hidden");

        // Busca detalhes completos do alimento
        try {
        const res = await fetch(`${API_URL}/alimentos/${id}`, {
                headers: { "Authorization": `Bearer ${getToken()}` }
        });
        if (!res.ok) throw new Error("Erro ao buscar detalhes");
        const a = await res.json();

        // Preenche nome e campos nutricionais (por 100g)
        document.getElementById("mi-nome").value = a.nome;
        document.getElementById("mi-kcal").value = a.calorias_100g ?? "";
        document.getElementById("mi-prot").value = a.proteinas_100g ?? "";
        document.getElementById("mi-carbo").value = a.carboidratos_100g ?? "";
        document.getElementById("mi-gord").value = a.gorduras_100g ?? "";

        // Armazena o ID do alimento para uso no endpoint de cálculo
        formItem.alimentoId = String(a.id);

        // Carrega medidas caseiras para este alimento
        const medidas = await carregarMedidasCaseira(a.id);
        popularDropdownMedidas(medidas);
        mostrarContainerMedidaCaseira(medidas.length > 0);
        } catch (err) {
        console.error("Erro ao carregar detalhes do alimento:", err);
        alert("Não foi possível carregar os detalhes do alimento.");
        }
}
async function salvarItem(e) {
        e.preventDefault();
        const num = id => parseFloat(document.getElementById(id).value) || 0;
        const editando = !!formItem.itemId;
        const medidaCaseira = document.getElementById("mi-medida-caseira").value;
        const quantidadeMedida = num("mi-qtd-medida");
        const observacaoMedida = document.getElementById("mi-obs-medida").value;

        // Se tem medida caseira + quantidade, usa endpoint de cálculo automático
        const usaCalculo = medidaCaseira && quantidadeMedida > 0;

        let body, url, method;

        if (usaCalculo) {
            // Endpoint /calcular espera propriedades em PascalCase (C# record ItemCalcularInput)
            body = {
                RefeicaoId: formItem.refeicaoId,
                Opcao: formItem.opcao,
                SubstituiItemId: formItem.substituiId,
                AlimentoId: formItem.alimentoId,  // ID do alimento na TACO (string)
                Refeicao: "lanche",  // padrão
                Observacao: "",
                TargetKcal: null,
                TargetProteina: null,
                TargetCarbo: null,
                TargetGordura: null,
                Gramas: null,
                MedidaCaseira: medidaCaseira,
                QuantidadeMedida: quantidadeMedida,
                ObservacaoMedida: observacaoMedida
            };
            url = `${API_URL}/planos/item/calcular`;
            method = "POST";
        } else {
            // Endpoint regular espera propriedades em camelCase (C# record ItemInput)
            body = {
                refeicaoId: formItem.refeicaoId,
                opcao: formItem.opcao,
                substituiItemId: formItem.substituiId,
                alimentoNome: document.getElementById("mi-nome").value,
                quantidadeG: num("mi-qtd"),
                medidaCaseira: medidaCaseira || document.getElementById("mi-medida").value,
                quantidadeMedida: quantidadeMedida || null,
                observacaoMedida: observacaoMedida || "",
                kcal: num("mi-kcal"),
                proteinas: num("mi-prot"),
                carbos: num("mi-carbo"),
                gorduras: num("mi-gord")
            };
            url = editando ? `${API_URL}/planos/item/${formItem.itemId}` : `${API_URL}/planos/item`;
            method = editando ? "PUT" : "POST";
        }

        try {
        const res = await fetch(url, {
            method,
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(body)
        });
        if (res.ok) {
        fecharModal("modal-item");
        await carregarPlanoAlimentarProntuario(window.pacienteIdAtivo);
        } else {
        alert(await lerErro(res));
        }
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }
async function excluirItem(id) {
        if (!confirm("Excluir este alimento? As substituições ligadas a ele também serão excluídas.")) return;
        const res = await fetch(`${API_URL}/planos/item/${id}`, { method: "DELETE" });
        if (res.ok) await carregarPlanoAlimentarProntuario(window.pacienteIdAtivo);
        else alert(await lerErro(res));
        }
function htmlPlanoLeitura(plano) {
        const refeicoes = plano.refeicoes || [];
        return refeicoes.map(r => {
        const opcoes = agruparOpcoes(r);
        const linha = (i, sub) => {
        const qtd = Number(i.quantidadeG) > 0 ? ` — ${i.quantidadeG}g` : "";
        const medida = i.medidaCaseira ? ` (${escaparHtml(i.medidaCaseira)})` : "";
        return `<div class="${sub ? "ml-6 text-amber-300" : "text-slate-200"} text-sm py-1">
        ${sub ? "↳ ou " : ""}${escaparHtml(i.alimento)}${qtd}${medida}
        <span class="text-slate-500 text-xs ml-2">${i.kcal} kcal</span></div>`;
        };
        return `
        <div class="bg-slate-800/60 border border-slate-700/60 rounded-xl p-4">
        <h5 class="font-bold text-white">${escaparHtml(r.nome)} <span class="text-emerald-400 text-sm ml-1">${escaparHtml(r.horario)}</span></h5>
        ${r.observacoes ? `<p class="text-xs text-slate-400 mt-1 whitespace-pre-wrap">${escaparHtml(r.observacoes)}</p>` : ""}
        ${opcoes.map((o, idx) => `
        <p class="text-[11px] font-bold uppercase text-emerald-400 mt-3">Opção ${idx + 1}</p>
        ${o.itens.map(i => linha(i, false) + i.substituicoes.map(s => linha(s, true)).join("")).join("")}
        `).join("")}
        </div>`;
        }).join("");
        }
async function salvarVersaoPlano() {
        if (!exigirPaciente()) return;
        const descricao = prompt("O que mudou nesta versão? (opcional)\nEx: reduzi carboidratos no jantar");
        if (descricao === null) return;
        try {
        const res = await fetch(`${API_URL}/planos/${window.pacienteIdAtivo}/versoes`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ descricao })
        });
        if (res.ok) {
        const data = await res.json();
        alert(data.mensagem);
        } else {
        alert(await lerErro(res));
        }
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }
async function abrirHistoricoVersoes() {
        if (!exigirPaciente()) return;
        abrirModal("modal-versoes");
        const lista = document.getElementById("versoes-lista");
        document.getElementById("versoes-detalhe").classList.add("hidden");
        lista.classList.remove("hidden");
        lista.innerHTML = `<p class="text-sm text-slate-400">Carregando...</p>`;
        try {
        const res = await fetch(`${API_URL}/planos/${window.pacienteIdAtivo}/versoes`);
        const versoes = await res.json();
        if (!versoes.length) {
        lista.innerHTML = `<p class="text-sm text-slate-400">Nenhuma versão salva ainda. Use <strong>Salvar versão</strong> para guardar o estado atual do plano.</p>`;
        return;
        }
        lista.innerHTML = versoes.map(v => `
        <button onclick="verVersaoPlano(${v.id})"
        class="w-full text-left bg-slate-800/70 hover:bg-slate-800 border border-slate-700/70 hover:border-emerald-500/40 rounded-xl p-4 transition">
        <div class="flex items-center justify-between gap-3">
        <span class="font-bold text-white">Versão ${v.numero}</span>
        <span class="text-xs text-slate-400">${new Date(v.criado_em).toLocaleString("pt-BR")}</span>
        </div>
        <p class="text-sm text-slate-400 mt-1">${escaparHtml(v.descricao) || "<em>Sem descrição</em>"}</p>
        </button>`).join("");
        } catch (err) {
        console.error(err);
        lista.innerHTML = `<p class="text-rose-400 text-sm">Erro ao carregar versões.</p>`;
        }
        }
async function verVersaoPlano(id) {
        const res = await fetch(`${API_URL}/planos/versao/${id}`);
        if (!res.ok) { alert(await lerErro(res)); return; }
        const v = await res.json();
        document.getElementById("versoes-lista").classList.add("hidden");
        const det = document.getElementById("versoes-detalhe");
        det.classList.remove("hidden");
        const orient = v.snapshot.orientacoes
        ? `<div class="bg-slate-800/60 border border-slate-700/60 rounded-xl p-4">
        <p class="text-[11px] font-bold uppercase text-amber-400 mb-1">Orientações</p>
        <p class="text-sm text-slate-300 whitespace-pre-wrap">${escaparHtml(v.snapshot.orientacoes)}</p>
        </div>` : "";
        det.innerHTML = `
        <div class="flex items-center justify-between">
        <button onclick="abrirHistoricoVersoes()" class="text-sm text-emerald-400 hover:text-emerald-300"><i class="fa-solid fa-arrow-left mr-1"></i>Voltar</button>
        <button onclick="restaurarVersaoPlano(${id})" class="flex items-center gap-2 bg-amber-600 hover:bg-amber-500 px-4 py-2 rounded-xl text-sm font-bold text-white transition">
        <i class="fa-solid fa-rotate-left"></i> Restaurar esta versão
        </button>
        </div>
        <div>
        <h4 class="text-base font-bold text-white">Versão ${v.numero}
        <span class="text-xs text-slate-400 font-normal ml-2">${new Date(v.criado_em).toLocaleString("pt-BR")}</span></h4>
        <p class="text-sm text-slate-400 mt-1">${escaparHtml(v.descricao) || "<em>Sem descrição</em>"}</p>
        </div>
        ${htmlPlanoLeitura(v.snapshot.plano)}
        ${orient}`;
        }

function htmlPlanoPDF(plano) {
        const linha = (i, sub) => {
        const qtd = Number(i.quantidadeG) > 0 ? ` — ${i.quantidadeG}g` : "";
        const medida = i.medidaCaseira ? ` (${escaparHtml(i.medidaCaseira)})` : "";
        const prefixo = sub ? `<span style="color:#b45309;font-weight:bold;">ou </span>` : "";
        return `<div style="padding:4px 0 4px ${sub ? 18 : 0}px;font-size:12px;color:#334155;">
        ${prefixo}${escaparHtml(i.alimento)}${qtd}${medida}
        <span style="float:right;color:#059669;">${i.kcal} kcal</span></div>`;
        };
        const blocos = (plano.refeicoes || []).map(r => {
        const opcoes = agruparOpcoes(r);
        const obs = r.observacoes
        ? `<p style="margin:4px 0 8px;font-size:11px;color:#64748b;white-space:pre-wrap;">${escaparHtml(r.observacoes)}</p>` : "";
        const corpo = opcoes.map((o, idx) => `
        <p style="margin:8px 0 2px;font-size:11px;font-weight:bold;color:#047857;text-transform:uppercase;">Opção ${idx + 1}</p>
        ${o.itens.map(i => linha(i, false) + i.substituicoes.map(s => linha(s, true)).join("")).join("")}`
        ).join("");
        return `<div style="page-break-inside:avoid;border:1px solid #e2e8f0;border-radius:6px;padding:10px 14px;margin-bottom:12px;">
        <strong style="color:#047857;font-size:14px;">${escaparHtml(r.nome)} (${escaparHtml(r.horario)})</strong>
        ${obs}${corpo}</div>`;
        }).join("");
        const t = totaisPlano(plano);
        return blocos + `
        <div style="margin-top:15px;background:#ecfdf5;border:1px solid #6d4de0;padding:12px 15px;border-radius:6px;display:flex;justify-content:space-between;">
        <strong style="color:#065f46;font-size:13px;">Total diário (Opção 1)</strong>
        <span style="color:#047857;font-weight:bold;">${t.kcal.toFixed(0)} kcal · P ${t.prot.toFixed(1)}g · C ${t.carbo.toFixed(1)}g · G ${t.gord.toFixed(1)}g</span>
        </div>`;
        }
async function gerarPDFPlanoAlimentar() {
        if (!exigirPaciente()) return;
        const pacienteId = window.pacienteIdAtivo;
        try {
        const resPlano = await fetch(`${API_URL}/planos/${pacienteId}`);
        const plano = await resPlano.json();
        if (!plano.refeicoes || !plano.refeicoes.length) {
        alert("Não há refeições registradas para exportar.");
        return;
        }
        let textoOrientacoes = "Nenhuma orientação registrada.";
        try {
        const resO = await fetch(`${API_URL}/orientacoes/${pacienteId}`);
        if (resO.ok) {
        const d = await resO.json();
        textoOrientacoes = d.texto || textoOrientacoes;
        }
        } catch (err) {
        console.error("Erro ao buscar orientações:", err);
        }
        const paciente = listaPacientes.find(p => p.id === pacienteId);
        const nome = paciente ? paciente.nome : `Paciente #${pacienteId}`;
        const email = paciente && paciente.email ? paciente.email : "Não informado";
        const data = new Date().toLocaleDateString("pt-BR");
        const html = `<!DOCTYPE html>
        <html lang="pt-BR">
        <head>
        <meta charset="UTF-8">
        <title>Plano alimentar - ${escaparHtml(nome)}</title>
        <style>
        @page { size: A4; margin: 14mm; }
        * { box-sizing: border-box; }
        body { font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif; color: #1e293b; margin: 0; }
        .topo { display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #6d4de0; padding-bottom: 14px; margin-bottom: 18px; }
        .topo h1 { color: #047857; margin: 0; font-size: 26px; }
        .topo p { margin: 4px 0 0; color: #64748b; font-size: 12px; }
        .paciente { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 12px 16px; margin-bottom: 18px; font-size: 14px; }
        .paciente h3 { margin: 0 0 6px; font-size: 14px; text-transform: uppercase; }
        .paciente p { margin: 3px 0; }
        h2 { font-size: 16px; margin: 0 0 12px; border-left: 4px solid #6d4de0; padding-left: 8px; }
        .orient { font-size: 13px; line-height: 1.5; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 6px; padding: 12px; white-space: pre-wrap; }
        .rodape { margin-top: 28px; border-top: 1px solid #cbd5e1; padding-top: 10px; text-align: center; font-size: 11px; color: #827d9c; }
        </style>
        </head>
        <body>
        <div class="topo">
        <div><h1>ACNUTRI</h1><p>Sistema Clínico de Nutrição e Dietética Avançada</p></div>
        <div style="font-size:12px;color:#64748b;">Emitido em: ${data}</div>
        </div>
        <div class="paciente">
        <h3>Identificação do Paciente</h3>
        <p><strong>Nome:</strong> ${escaparHtml(nome)}</p>
        <p><strong>E-mail:</strong> ${escaparHtml(email)}</p>
        </div>
        <h2>Prescrição do Plano Alimentar</h2>
        ${htmlPlanoPDF(plano)}
        <h2 style="margin-top:22px;">Orientações Nutricionais &amp; Recomendações</h2>
        <div class="orient">${escaparHtml(textoOrientacoes)}</div>
        <div class="rodape">ACNUTRI • Documento clínico confidencial</div>
        </body>
        </html>`;
        const janela = window.open("", "_blank");
        if (!janela) {
        alert("O navegador bloqueou a nova janela. Permita pop-ups para este site e tente de novo.");
        return;
        }
        janela.document.open();
        janela.document.write(html);
        janela.document.close();
        janela.focus();
        // Espera renderizar e abre o diálogo de impressão (escolha "Salvar como PDF")
        setTimeout(() => janela.print(), 400);
        } catch (err) {
        console.error("Erro ao gerar PDF:", err);
        alert("Ocorreu um erro ao gerar o PDF. Veja o console (F12).");
        }
        }
// ===================================================== // METAS DO PACIENTE// =====================================================
        const TIPOS_META = {
        agua:      { rotulo: "Água",               icone: "fa-droplet",        cor: "bg-blue-500/10 text-blue-400" },
        frutas:    { rotulo: "Frutas e vegetais",  icone: "fa-apple-whole",    cor: "bg-rose-500/10 text-rose-400" },
        refeicoes: { rotulo: "Organizar refeições", icone: "fa-utensils",      cor: "bg-amber-500/10 text-amber-400" },
        atividade: { rotulo: "Atividade física",   icone: "fa-person-running", cor: "bg-emerald-500/10 text-emerald-400" },
        sono:      { rotulo: "Sono",               icone: "fa-moon",           cor: "bg-indigo-500/10 text-indigo-400" },
        outro:     { rotulo: "Outra",              icone: "fa-bullseye",       cor: "bg-purple-500/10 text-purple-400" }
        };
        const RESULTADOS_META = {
        mantida:        { rotulo: "Mantida", ponto: "bg-green-400", chip: "bg-green-500/15 text-green-400 border-green-500/30" },
        parcial:     { rotulo: "Parcial",     ponto: "bg-amber-400",   chip: "bg-amber-500/15 text-amber-400 border-amber-500/30" },
        nao_mantida: { rotulo: "Não mantida", ponto: "bg-rose-400",    chip: "bg-rose-500/15 text-rose-400 border-rose-500/30" }
        };
let metasAtuais = [];
let formMeta = { id: null };
let acompanhamentoCtx = { pacienteId: null };
        const hojeISO = () => new Date().toLocaleDateString("en-CA"); // yyyy-mm-dd no fuso local
async function carregarMetas(pacienteId) {
        const el = document.getElementById("metas-lista");
        if (!el) return;
        try {
        const res = await fetch(`${API_URL}/metas/${pacienteId}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        metasAtuais = await res.json();
        renderizarMetas();
        } catch (err) {
        console.error("Erro ao carregar metas:", err);
        el.innerHTML = `<p class="text-rose-400 text-sm">Não foi possível carregar as metas.</p>`;
        }
        }
function htmlMeta(m) {
        const t = TIPOS_META[m.tipo] || TIPOS_META.outro;
        const acs = m.acompanhamentos || [];
        const total = acs.length;
        const mantidas = acs.filter(a => a.resultado === "mantida").length;
        const taxa = total ? Math.round((mantidas / total) * 100) : null;
        const ativa = m.status === "ativa";
        const vencida = ativa && m.prazo && m.prazo < hojeISO();
        const pontos = acs.slice(0, 8).reverse().map(a => {
        const r = RESULTADOS_META[a.resultado];
        return `<span title="${formatarData(a.data)} • ${r.rotulo}" class="w-3 h-3 rounded-full ${r.ponto}"></span>`;
        }).join("");
        const badgeStatus = m.status === "concluida"
        ? `<span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500/15 text-emerald-400 border border-emerald-500/30">CONCLUÍDA</span>`
        : m.status === "arquivada"
        ? `<span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-slate-500/15 text-slate-400 border border-slate-500/30">ARQUIVADA</span>`
        : "";
        const prazo = m.prazo
        ? `<span class="text-xs ${vencida ? "text-rose-400" : "text-slate-400"}"><i class="fa-solid fa-calendar-day mr-1"></i>${vencida ? "Venceu em " : "Até "}${formatarData(m.prazo)}</span>`
        : "";
        const historico = acs.length
        ? acs.map(a => {
        const r = RESULTADOS_META[a.resultado];
        return `
        <div class="flex items-start justify-between gap-3 py-2 border-b border-slate-800 last:border-0">
        <div class="min-w-0">
        <span class="text-xs text-slate-400">${formatarData(a.data)}</span>
        <span class="ml-2 px-2 py-0.5 rounded-full text-[10px] font-bold border ${r.chip}">${r.rotulo}</span>
        ${a.observacao ? `<p class="text-xs text-slate-300 mt-1 whitespace-pre-wrap">${escaparHtml(a.observacao)}</p>` : ""}
        </div>
        <button title="Excluir" onclick="excluirAcompanhamento(${a.id})" class="${BTN_MINI} text-slate-500 hover:text-rose-400"><i class="fa-solid fa-trash"></i></button>
        </div>`;
        }).join("")
        : `<p class="text-xs text-slate-500 py-2">Nenhum acompanhamento registrado ainda.</p>`;
        return `
        <div class="bg-slate-900/60 border ${vencida ? "border-rose-500/30" : "border-slate-700/70"} rounded-2xl p-5 ${ativa ? "" : "opacity-70"}">
        <div class="flex flex-wrap items-start justify-between gap-3">
        <div class="flex items-start gap-3 min-w-0">
        <div class="w-10 h-10 shrink-0 rounded-xl ${t.cor} flex items-center justify-center">
        <i class="fa-solid ${t.icone}"></i>
        </div>
        <div class="min-w-0">
        <div class="flex flex-wrap items-center gap-2">
        <h4 class="font-bold text-white">${escaparHtml(m.titulo)}</h4>
        ${badgeStatus}
        </div>
        <div class="flex flex-wrap items-center gap-x-3 gap-y-1 mt-1">
        <span class="text-xs text-slate-500">${t.rotulo}</span>
        ${prazo}
        </div>
        ${m.descricao ? `<p class="text-sm text-slate-400 mt-2 whitespace-pre-wrap">${escaparHtml(m.descricao)}</p>` : ""}
        </div>
        </div>
        <div class="flex flex-wrap gap-1.5">
        ${ativa ? `<button onclick="abrirModalAcompanhamento(window.pacienteIdAtivo, ${m.id})" class="${BTN_MINI} bg-emerald-500/15 text-emerald-400 hover:bg-emerald-500 hover:text-white"><i class="fa-solid fa-check mr-1"></i>Acompanhar</button>` : ""}
        <button onclick="abrirModalMeta(${m.id})" class="${BTN_MINI} bg-blue-500/15 text-blue-400 hover:bg-blue-500 hover:text-white"><i class="fa-solid fa-pen mr-1"></i>Editar</button>
        <button onclick="alterarStatusMeta(${m.id}, '${ativa ? "concluida" : "ativa"}')" class="${BTN_MINI} bg-slate-700 text-slate-300 hover:bg-slate-600">${ativa ? "Concluir" : "Reativar"}</button>
        <button onclick="excluirMeta(${m.id})" class="${BTN_MINI} bg-rose-500/15 text-rose-400 hover:bg-rose-500 hover:text-white"><i class="fa-solid fa-trash mr-1"></i>Excluir</button>
        </div>
        </div>
        <div class="flex flex-wrap items-center gap-4 mt-4 pt-3 border-t border-slate-800">
        <div class="flex items-center gap-1.5">${pontos || `<span class="text-xs text-slate-500">Sem acompanhamentos</span>`}</div>
        ${taxa !== null ? `<span class="text-xs text-slate-400">Mantida em <strong class="text-white">${mantidas} de ${total}</strong> check-ins (${taxa}%)</span>` : ""}
        </div>
        <details class="mt-3">
        <summary class="text-xs text-emerald-400 cursor-pointer hover:text-emerald-300">Ver histórico (${total})</summary>
        <div class="mt-2">${historico}</div>
        </details>
        </div>`;
        }
function renderizarMetas() {
        const lista = document.getElementById("metas-lista");
        const resumo = document.getElementById("metas-resumo");
        if (!lista) return;
        const ativas = metasAtuais.filter(m => m.status === "ativa").length;
        const concluidas = metasAtuais.filter(m => m.status === "concluida").length;
        const ultimosMantidos = metasAtuais.filter(m => m.status === "ativa" && m.acompanhamentos[0]?.resultado === "mantida").length;
        const card = (rotulo, valor, cor) => `
        <div class="bg-slate-900/60 border border-slate-700/70 rounded-xl p-3">
        <p class="text-[10px] uppercase tracking-wider text-slate-500">${rotulo}</p>
        <p class="text-xl font-extrabold ${cor}">${valor}</p>
        </div>`;
        if (resumo) {
        resumo.innerHTML = metasAtuais.length
        ? card("Metas ativas", ativas, "text-emerald-400") +
        card("Mantidas no último check-in", `${ultimosMantidos}/${ativas}`, "text-blue-400") +
        card("Concluídas", concluidas, "text-purple-400")
        : "";
        }
        lista.innerHTML = metasAtuais.length
        ? metasAtuais.map(htmlMeta).join("")
        : `<div class="bg-slate-900/50 border border-slate-700/60 rounded-xl p-5">
        <p class="text-sm text-slate-400">Nenhuma meta criada. Clique em <strong>Nova meta</strong> (ex.: beber água, aumentar frutas, atividade física).</p>
        </div>`;
        }
function abrirModalMeta(id) {
        if (!exigirPaciente()) return;
        const m = id ? metasAtuais.find(x => x.id === id) : null;
        formMeta = { id: m ? m.id : null, status: m ? m.status : "ativa" };
        document.getElementById("mm-heading").innerText = m ? "Editar meta" : "Nova meta";
        document.getElementById("mm-tipo").innerHTML = Object.entries(TIPOS_META)
        .map(([k, v]) => `<option value="${k}">${v.rotulo}</option>`).join("");
        document.getElementById("mm-titulo").value = m ? m.titulo : "";
        document.getElementById("mm-tipo").value = m ? m.tipo : "agua";
        document.getElementById("mm-prazo").value = m && m.prazo ? m.prazo : "";
        document.getElementById("mm-desc").value = m ? m.descricao : "";
        abrirModal("modal-meta");
        }
async function enviarMeta(id, dados) {
        const res = await fetch(id ? `${API_URL}/metas/${id}` : `${API_URL}/metas`, {
        method: id ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dados)
        });
        return res;
        }
async function salvarMeta(e) {
        e.preventDefault();
        if (!exigirPaciente()) return;
        const dados = {
        pacienteId: window.pacienteIdAtivo,
        titulo: document.getElementById("mm-titulo").value,
        tipo: document.getElementById("mm-tipo").value,
        prazo: document.getElementById("mm-prazo").value,
        descricao: document.getElementById("mm-desc").value,
        status: formMeta.status || "ativa"
        };
        try {
        const res = await enviarMeta(formMeta.id, dados);
        if (res.ok) {
        fecharModal("modal-meta");
        await carregarMetas(window.pacienteIdAtivo);
        } else {
        alert(await lerErro(res));
        }
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }
async function alterarStatusMeta(id, status) {
        const m = metasAtuais.find(x => x.id === id);
        if (!m) return;
        const res = await enviarMeta(id, {
        pacienteId: window.pacienteIdAtivo,
        titulo: m.titulo, tipo: m.tipo, prazo: m.prazo || "", descricao: m.descricao, status
        });
        if (res.ok) await carregarMetas(window.pacienteIdAtivo);
        else alert(await lerErro(res));
        }
async function excluirMeta(id) {
        if (!confirm("Excluir esta meta e todo o histórico de acompanhamento dela?")) return;
        const res = await fetch(`${API_URL}/metas/${id}`, { method: "DELETE" });
        if (res.ok) await carregarMetas(window.pacienteIdAtivo);
        else alert(await lerErro(res));
        }
async function excluirAcompanhamento(id) {
        if (!confirm("Excluir este registro de acompanhamento?")) return;
        const res = await fetch(`${API_URL}/metas/acompanhamento/${id}`, { method: "DELETE" });
        if (res.ok) await carregarMetas(window.pacienteIdAtivo);
        else alert(await lerErro(res));
        }
async function abrirModalAcompanhamento(pacienteId, metaId = null, aposCheckin = false) {
        if (!pacienteId) {
        alert("Abra o prontuário de um paciente primeiro.");
        return;
        }
let metas = [];
        try {
        const res = await fetch(`${API_URL}/metas/${pacienteId}`);
        if (res.ok) metas = await res.json();
        } catch (err) {
        console.error(err);
        }
        metas = metas.filter(m => m.status === "ativa" && (!metaId || m.id === metaId));
        if (!metas.length) {
        if (!aposCheckin) alert("Este paciente não tem metas ativas. Crie uma em Nova meta.");
        return;
        }
        acompanhamentoCtx = { pacienteId, metas };
        document.getElementById("ma-data").value = hojeISO();
        document.getElementById("ma-lista").innerHTML = metas.map(m => {
        const t = TIPOS_META[m.tipo] || TIPOS_META.outro;
        const ultimo = m.acompanhamentos[0];
        const dica = ultimo
        ? `Último (${formatarData(ultimo.data)}): ${RESULTADOS_META[ultimo.resultado].rotulo}`
        : "Ainda sem acompanhamento";
        return `
        <div class="bg-slate-800/60 border border-slate-700/60 rounded-xl p-4 space-y-2">
        <div class="flex items-center gap-3">
        <div class="w-8 h-8 shrink-0 rounded-lg ${t.cor} flex items-center justify-center text-sm"><i class="fa-solid ${t.icone}"></i></div>
        <div class="min-w-0">
        <p class="text-sm font-semibold text-white">${escaparHtml(m.titulo)}</p>
        <p class="text-[11px] text-slate-500">${dica}</p>
        </div>
        </div>
        <div class="grid grid-cols-1 md:grid-cols-3 gap-2">
        <select id="ma-res-${m.id}" class="acnut-input md:col-span-1">
        <option value="">Não avaliar</option>
        <option value="mantida">Mantida</option>
        <option value="parcial">Parcial</option>
        <option value="nao_mantida">Não mantida</option>
        </select>
        <input id="ma-obs-${m.id}" placeholder="Observação (opcional)" class="acnut-input md:col-span-2">
        </div>
        </div>`;
        }).join("");
        abrirModal("modal-acompanhamento");
        }
async function salvarAcompanhamento(e) {
        e.preventDefault();
        const { pacienteId, metas } = acompanhamentoCtx;
        const data = document.getElementById("ma-data").value || hojeISO();
        const registros = metas
        .map(m => ({
        metaId: m.id,
        resultado: document.getElementById(`ma-res-${m.id}`).value,
        observacao: document.getElementById(`ma-obs-${m.id}`).value,
        data
        }))
        .filter(r => r.resultado);
        if (!registros.length) {
        alert("Marque o resultado de pelo menos uma meta.");
        return;
        }
        try {
        const respostas = await Promise.all(registros.map(r =>
        fetch(`${API_URL}/metas/acompanhamento`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(r)
        })));
        const falha = respostas.find(r => !r.ok);
        if (falha) {
        alert(await lerErro(falha));
        return;
        }
        fecharModal("modal-acompanhamento");
        if (pacienteId === window.pacienteIdAtivo) await carregarMetas(pacienteId);
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }

// ===================================================== ANEXOS (compartilhado entre Evolução e Exames) =====================================================
const LIMITE_ARQUIVO = 10 * 1024 * 1024;
function ehImagem(a) { return (a.mime || "").startsWith("image/"); }
function validarArquivos(input, somenteImagem) {
        const permitidos = somenteImagem
        ? ["image/jpeg", "image/png", "image/webp"]
        : ["image/jpeg", "image/png", "image/webp", "application/pdf"];
        for (const f of input.files) {
        if (f.size > LIMITE_ARQUIVO) { alert(`"${f.name}" passa de 10 MB.`); return false; }
        if (!permitidos.includes(f.type)) {
        alert(`"${f.name}": tipo não permitido.`);
        return false;
        }
        }
        return true;
        }
        // Visualização no card (clique abre em nova aba)
function htmlAnexos(arquivos) {
        if (!arquivos || !arquivos.length) return "";
        return `<div class="flex flex-wrap gap-2 mt-3">` + arquivos.map(a => {
        const url = urlArquivo(a.id);
        return ehImagem(a)
        ? `<a href="${url}" target="_blank" title="${escaparHtml(a.nome)}"><img src="${url}" loading="lazy" class="w-24 h-24 object-cover rounded-lg border border-slate-700 hover:border-emerald-500 transition"></a>`
        : `<a href="${url}" target="_blank" class="flex items-center gap-2 px-3 py-2 rounded-lg bg-slate-800 border border-slate-700 hover:border-emerald-500 text-xs text-slate-200 transition max-w-[220px]">
        <i class="fa-solid fa-file-pdf text-red-400"></i><span class="truncate">${escaparHtml(a.nome)}</span>
        </a>`;
        }).join("") + `</div>`;
        }
        // Arquivos já salvos dentro do modal de edição (com botão de remover)
function renderAnexosExistentes(containerId, arquivos, tipo) {
        const el = document.getElementById(containerId);
        if (!el) return;
        el.innerHTML = (arquivos || []).map(a => {
        const url = urlArquivo(a.id);
        const mini = ehImagem(a)
        ? `<img src="${url}" class="w-20 h-20 object-cover rounded-lg border border-slate-700">`
        : `<div class="w-20 h-20 rounded-lg bg-slate-800 border border-slate-700 flex flex-col items-center justify-center gap-1 px-1">
        <i class="fa-solid fa-file-pdf text-red-400 text-lg"></i>
        <span class="text-[9px] text-slate-400 truncate w-full text-center">${escaparHtml(a.nome)}</span>
        </div>`;
        return `<div class="relative">${mini}
        <button type="button" title="Excluir arquivo" onclick="removerAnexo(${a.id}, '${tipo}')"
        class="absolute -top-1.5 -right-1.5 w-5 h-5 rounded-full bg-rose-600 hover:bg-rose-500 text-white text-[10px] flex items-center justify-center">
        <i class="fa-solid fa-xmark"></i></button></div>`;
        }).join("");
        }
async function removerAnexo(id, tipo) {
        if (!confirm("Excluir este arquivo definitivamente?")) return;
        const res = await fetch(`${API_URL}/arquivos/${id}`, { method: "DELETE" });
        if (!res.ok) { alert(await lerErro(res)); return; }
        if (tipo === "evolucao") {
        formEvolucao.fotos = formEvolucao.fotos.filter(a => a.id !== id);
        renderAnexosExistentes("mev-existentes", formEvolucao.fotos, "evolucao");
        await carregarEvolucao(window.pacienteIdAtivo);
        } else {
        formExame.arquivos = formExame.arquivos.filter(a => a.id !== id);
        renderAnexosExistentes("mex-existentes", formExame.arquivos, "exame");
        await carregarExames(window.pacienteIdAtivo);
        }
        }
// =====================================================EVOLUÇÃO =====================================================
        const METRICAS_EVOLUCAO = {
        peso:    { rotulo: "Peso",     un: "kg" },
        gordura: { rotulo: "Gordura",  un: "%" },
        cintura: { rotulo: "Cintura",  un: "cm" },
        quadril: { rotulo: "Quadril",  un: "cm" },
        abdomen: { rotulo: "Abdômen",  un: "cm" },
        braco:   { rotulo: "Braço",    un: "cm" },
        coxa:    { rotulo: "Coxa",     un: "cm" }
        };
let evolucaoAtual = [];
let formEvolucao = { id: null, fotos: [] };
let graficoEvolucaoDetalhe = null;
async function carregarEvolucao(pacienteId) {
        const el = document.getElementById("evolucao-lista");
        if (!el) return;
        try {
        const res = await fetch(`${API_URL}/evolucao/${pacienteId}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        evolucaoAtual = await res.json();
        renderizarEvolucao();
        renderizarGraficoEvolucao();
        await carregarGraficoEvolucao(pacienteId); // gráfico do topo (check-ins + evolução)
        } catch (err) {
        console.error("Erro ao carregar evolução:", err);
        el.innerHTML = `<p class="text-rose-400 text-sm">Não foi possível carregar a evolução.</p>`;
        }
        }
function fmtNum(v, casas = 1) {
        return Number(v).toLocaleString("pt-BR", { minimumFractionDigits: casas, maximumFractionDigits: casas });
        }
// ---------- Filtro de período ----------
function evolucaoFiltrada() {
        const dias = parseInt(document.getElementById("evolucao-periodo")?.value || "0");
        if (!dias) return evolucaoAtual;
        const corte = new Date();
        corte.setDate(corte.getDate() - dias);
        const limite = corte.toLocaleDateString("en-CA");
        return evolucaoAtual.filter(r => r.data >= limite);
}
function aplicarPeriodoEvolucao() {
        renderizarEvolucao();
        renderizarGraficoEvolucao();
}
function renderizarEvolucao() {
        const lista = document.getElementById("evolucao-lista");
        const resumo = document.getElementById("evolucao-resumo");
        if (!lista) return;
        const card = (rotulo, valor, cor) => `
        <div class="bg-slate-900/60 border border-slate-700/70 rounded-xl p-3">
        <p class="text-[10px] uppercase tracking-wider text-slate-500">${rotulo}</p>
        <p class="text-xl font-extrabold ${cor}">${valor}</p>
        </div>`;
        // Resumo: sempre sobre todo o histórico
        const comPeso = evolucaoAtual.filter(r => r.peso != null);
        if (resumo) {
        if (comPeso.length) {
        const atual = comPeso[0].peso;
        const inicial = comPeso[comPeso.length - 1].peso;
        const dif = atual - inicial;
        resumo.innerHTML =
        card("Peso inicial", `${fmtNum(inicial, 1)} kg`, "text-slate-200") +
        card("Peso atual", `${fmtNum(atual, 1)} kg`, "text-emerald-400") +
        card("Variação total", `${dif > 0 ? "+" : ""}${fmtNum(dif, 1)} kg`, "text-blue-400") +
        card("Registros", evolucaoAtual.length, "text-purple-400");
        } else {
        resumo.innerHTML = evolucaoAtual.length ? card("Registros", evolucaoAtual.length, "text-purple-400") : "";
        }
        }
        if (!evolucaoAtual.length) {
        lista.innerHTML = `<div class="bg-slate-900/50 border border-slate-700/60 rounded-xl p-5">
        <p class="text-sm text-slate-400">Nenhum registro ainda. Clique em <strong>Novo registro</strong> para lançar peso, medidas ou fotos.</p></div>`;
        return;
        }
        const visiveis = evolucaoFiltrada();
        if (!visiveis.length) {
        lista.innerHTML = `<div class="bg-slate-900/50 border border-slate-700/60 rounded-xl p-5">
        <p class="text-sm text-slate-400">Nenhum registro neste período. Escolha "Todo o período" para ver tudo.</p></div>`;
        return;
        }
        lista.innerHTML = visiveis.map(r => {
        // Variação em relação ao registro anterior, mesmo que ele esteja fora do filtro
        let delta = "";
        if (r.peso != null) {
        const i = evolucaoAtual.indexOf(r);
        const anterior = evolucaoAtual.slice(i + 1).find(x => x.peso != null);
        if (anterior) {
        const d = r.peso - anterior.peso;
        const cor = d === 0 ? "text-slate-400" : d < 0 ? "text-emerald-400" : "text-amber-400";
        delta = `<span class="text-xs ${cor}">${d > 0 ? "+" : ""}${fmtNum(d, 1)} kg</span>`;
        }
        }
        const chips = ["peso", "gordura", "cintura", "quadril", "abdomen", "braco", "coxa"]
        .filter(k => r[k] != null)
        .map(k => `<span class="px-2.5 py-1 rounded-lg bg-slate-800 border border-slate-700 text-xs text-slate-200">
        <span class="text-slate-500">${METRICAS_EVOLUCAO[k].rotulo}</span>
        <strong class="ml-1">${fmtNum(r[k], k === "peso" ? 2 : 1)}</strong> ${METRICAS_EVOLUCAO[k].un}</span>`)
        .join("");
        return `
        <div class="bg-slate-900/60 border border-slate-700/70 rounded-2xl p-5">
        <div class="flex flex-wrap items-start justify-between gap-3">
        <div class="flex items-center gap-3">
        <h4 class="font-bold text-white"><i class="fa-solid fa-calendar-day text-slate-500 mr-2"></i>${formatarData(r.data)}</h4>
        ${delta}
        </div>
        <div class="flex gap-1.5">
        <button onclick="abrirModalEvolucao(${r.id})" class="${BTN_MINI} bg-blue-500/15 text-blue-400 hover:bg-blue-500 hover:text-white"><i class="fa-solid fa-pen mr-1"></i>Editar</button>
        <button onclick="excluirEvolucao(${r.id})" class="${BTN_MINI} bg-rose-500/15 text-rose-400 hover:bg-rose-500 hover:text-white"><i class="fa-solid fa-trash mr-1"></i>Excluir</button>
        </div>
        </div>
        ${chips ? `<div class="flex flex-wrap gap-2 mt-3">${chips}</div>` : ""}
        ${r.observacao ? `<p class="text-sm text-slate-300 mt-3 whitespace-pre-wrap">${escaparHtml(r.observacao)}</p>` : ""}
        ${htmlAnexos(r.fotos)}
        </div>`;
        }).join("");
        }
function renderizarGraficoEvolucao() {
        const canvas = document.getElementById("graficoEvolucaoDetalhe");
        if (!canvas) return;
        if (graficoEvolucaoDetalhe) {
        graficoEvolucaoDetalhe.destroy();
        graficoEvolucaoDetalhe = null;
        }
        const chave = document.getElementById("evolucao-metrica").value;
        const m = METRICAS_EVOLUCAO[chave];
        const pontos = evolucaoFiltrada().filter(r => r[chave] != null).slice().reverse(); // do mais antigo ao mais novo
        if (!pontos.length) return;
        graficoEvolucaoDetalhe = new Chart(canvas, {
        type: "line",
        data: {
        labels: pontos.map(p => formatarData(p.data)),
        datasets: [{
        label: `${m.rotulo} (${m.un})`,
        data: pontos.map(p => Number(p[chave])),
        borderColor: "#6d4de0",
        backgroundColor: "rgba(109, 77, 224, 0.10)",
        borderWidth: 2,
        tension: 0.3,
        fill: true,
        pointBackgroundColor: "#6d4de0"
        }]
        },
        options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { labels: { color: "#827d9c" } } },
        scales: {
        x: { ticks: { color: "#827d9c" }, grid: { color: "rgba(0, 0, 0, 0.06)" } },
        y: { ticks: { color: "#827d9c" }, grid: { color: "rgba(0, 0, 0, 0.06)" } }
        }
        }
        });
        }
function abrirModalEvolucao(id) {
        if (!exigirPaciente()) return;
        const r = id ? evolucaoAtual.find(x => x.id === id) : null;
        formEvolucao = { id: r ? r.id : null, fotos: r ? [...r.fotos] : [] };
        const v = x => (x == null ? "" : x);
        document.getElementById("mev-heading").innerText = r ? "Editar registro de evolução" : "Novo registro de evolução";
        document.getElementById("mev-data").value = r ? r.data : hojeISO();
        document.getElementById("mev-peso").value = r ? v(r.peso) : "";
        document.getElementById("mev-gordura").value = r ? v(r.gordura) : "";
        document.getElementById("mev-cintura").value = r ? v(r.cintura) : "";
        document.getElementById("mev-quadril").value = r ? v(r.quadril) : "";
        document.getElementById("mev-abdomen").value = r ? v(r.abdomen) : "";
        document.getElementById("mev-braco").value = r ? v(r.braco) : "";
        document.getElementById("mev-coxa").value = r ? v(r.coxa) : "";
        document.getElementById("mev-obs").value = r ? r.observacao : "";
        document.getElementById("mev-fotos").value = "";
        renderAnexosExistentes("mev-existentes", formEvolucao.fotos, "evolucao");
        abrirModal("modal-evolucao");
        }
async function salvarEvolucao(e) {
        e.preventDefault();
        if (!exigirPaciente()) return;
        const inputFotos = document.getElementById("mev-fotos");
        if (!validarArquivos(inputFotos, true)) return;
        const fd = new FormData();
        fd.append("pacienteId", window.pacienteIdAtivo);
        fd.append("data", document.getElementById("mev-data").value);
        ["peso", "gordura", "cintura", "quadril", "abdomen", "braco", "coxa"].forEach(k =>
        fd.append(k, document.getElementById(`mev-${k}`).value));
        fd.append("observacao", document.getElementById("mev-obs").value);
        for (const f of inputFotos.files) fd.append("fotos", f);
        const editando = formEvolucao.id !== null;
        try {
        // Não definir Content-Type: o navegador monta o multipart sozinho
        const res = await fetch(editando ? `${API_URL}/evolucao/${formEvolucao.id}` : `${API_URL}/evolucao`, {
        method: editando ? "PUT" : "POST",
        body: fd
        });
        if (res.ok) {
        fecharModal("modal-evolucao");
        await carregarEvolucao(window.pacienteIdAtivo);
        } else {
        alert(await lerErro(res));
        }
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }
async function excluirEvolucao(id) {
        if (!confirm("Excluir este registro de evolução e as fotos dele?")) return;
        const res = await fetch(`${API_URL}/evolucao/${id}`, { method: "DELETE" });
        if (res.ok) await carregarEvolucao(window.pacienteIdAtivo);
        else alert(await lerErro(res));
        }
// ---------- Comparar fotos ----------
function abrirComparacaoFotos() {
        if (!exigirPaciente()) return;
        const comFotos = evolucaoAtual.filter(r => r.fotos && r.fotos.length);
        if (comFotos.length < 2) {
        alert("Para comparar, são necessários pelo menos 2 registros com fotos.");
        return;
        }
        const opcoes = comFotos.map(r =>
        `<option value="${r.id}">${formatarData(r.data)}${r.peso != null ? " • " + fmtNum(r.peso, 1) + " kg" : ""}</option>`
        ).join("");
        const a = document.getElementById("cmp-a");
        const b = document.getElementById("cmp-b");
        a.innerHTML = opcoes;
        b.innerHTML = opcoes;
        a.value = comFotos[comFotos.length - 1].id; // mais antigo
        b.value = comFotos[0].id;                   // mais recente
        renderizarComparacaoFotos();
        abrirModal("modal-comparar");
        }
function renderizarComparacaoFotos() {
        let r1 = evolucaoAtual.find(r => r.id === parseInt(document.getElementById("cmp-a").value));
        let r2 = evolucaoAtual.find(r => r.id === parseInt(document.getElementById("cmp-b").value));
        if (!r1 || !r2) return;
        // Antes = data menor, Depois = data maior
        if (r1.data > r2.data) [r1, r2] = [r2, r1];
        const dias = Math.round((new Date(r2.data) - new Date(r1.data)) / 86400000);
        let resumo = `<strong>${dias}</strong> dia${dias === 1 ? "" : "s"} entre os registros`;
        if (r1.peso != null && r2.peso != null) {
        const d = r2.peso - r1.peso;
        resumo += ` • peso <strong class="${d <= 0 ? "text-emerald-400" : "text-amber-400"}">${d > 0 ? "+" : ""}${fmtNum(d, 1)} kg</strong>`;
        }
        document.getElementById("cmp-resumo").innerHTML = resumo;
        const coluna = (titulo, r) => `
        <div class="bg-slate-800/60 border border-slate-700/60 rounded-xl p-4">
        <p class="text-[10px] uppercase tracking-wider text-emerald-400 font-bold">${titulo}</p>
        <p class="text-white font-bold">${formatarData(r.data)}
        ${r.peso != null ? `<span class="text-slate-400 text-sm font-normal ml-2">${fmtNum(r.peso, 1)} kg</span>` : ""}</p>
        <div class="grid grid-cols-2 gap-2 mt-3">
        ${r.fotos.filter(ehImagem).map(f => {
        const url = urlArquivo(f.id);
        return `<a href="${url}" target="_blank"><img src="${url}" class="w-full aspect-[3/4] object-cover rounded-lg border border-slate-700 hover:border-emerald-500 transition"></a>`;
        }).join("")}
        </div>
        </div>`;
        document.getElementById("cmp-colunas").innerHTML = coluna("Antes", r1) + coluna("Depois", r2);
}
// ===================================================== // EXAMES =====================================================
let examesAtuais = [];
let formExame = { id: null, arquivos: [] };
async function carregarExames(pacienteId) {
        const el = document.getElementById("exames-lista");
        if (!el) return;
        try {
        const res = await fetch(`${API_URL}/exames/${pacienteId}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        examesAtuais = await res.json();
        renderizarExames();
        } catch (err) {
        console.error("Erro ao carregar exames:", err);
        el.innerHTML = `<p class="text-rose-400 text-sm">Não foi possível carregar os exames.</p>`;
        }
        }
function renderizarExames() {
        const el = document.getElementById("exames-lista");
        if (!el) return;
        if (!examesAtuais.length) {
        el.innerHTML = `<div class="bg-slate-900/50 border border-slate-700/60 rounded-xl p-5">
        <p class="text-sm text-slate-400">Nenhum exame anexado. Clique em <strong>Novo exame</strong>.</p></div>`;
        return;
        }
        el.innerHTML = examesAtuais.map(x => `
        <div class="bg-slate-900/60 border border-slate-700/70 rounded-2xl p-5">
        <div class="flex flex-wrap items-start justify-between gap-3">
        <div class="min-w-0">
        <h4 class="font-bold text-white">${escaparHtml(x.titulo)}</h4>
        <p class="text-xs text-slate-400 mt-1"><i class="fa-solid fa-calendar-day mr-1"></i>Exame de ${formatarData(x.data)}</p>
        </div>
        <div class="flex gap-1.5">
        <button onclick="abrirModalExame(${x.id})" class="${BTN_MINI} bg-blue-500/15 text-blue-400 hover:bg-blue-500 hover:text-white"><i class="fa-solid fa-pen mr-1"></i>Editar</button>
        <button onclick="excluirExame(${x.id})" class="${BTN_MINI} bg-rose-500/15 text-rose-400 hover:bg-rose-500 hover:text-white"><i class="fa-solid fa-trash mr-1"></i>Excluir</button>
        </div>
        </div>
        ${x.observacao ? `<div class="mt-3 bg-slate-800/60 border border-slate-700/60 rounded-xl p-3">
        <p class="text-[10px] uppercase tracking-wider text-amber-400 font-bold mb-1">Observação</p>
        <p class="text-sm text-slate-300 whitespace-pre-wrap">${escaparHtml(x.observacao)}</p></div>` : ""}
        ${htmlAnexos(x.arquivos)}
        </div>`).join("");
        }
function abrirModalExame(id) {
        if (!exigirPaciente()) return;
        const x = id ? examesAtuais.find(e => e.id === id) : null;
        formExame = { id: x ? x.id : null, arquivos: x ? [...x.arquivos] : [] };
        document.getElementById("mex-heading").innerText = x ? "Editar exame" : "Novo exame";
        document.getElementById("mex-titulo").value = x ? x.titulo : "";
        document.getElementById("mex-data").value = x ? x.data : hojeISO();
        document.getElementById("mex-obs").value = x ? x.observacao : "";
        document.getElementById("mex-arquivos").value = "";
        renderAnexosExistentes("mex-existentes", formExame.arquivos, "exame");
        abrirModal("modal-exame");
        }
async function salvarExame(e) {
        e.preventDefault();
        if (!exigirPaciente()) return;
        const input = document.getElementById("mex-arquivos");
        if (!validarArquivos(input, false)) return;
        const fd = new FormData();
        fd.append("pacienteId", window.pacienteIdAtivo);
        fd.append("titulo", document.getElementById("mex-titulo").value);
        fd.append("data", document.getElementById("mex-data").value);
        fd.append("observacao", document.getElementById("mex-obs").value);
        for (const f of input.files) fd.append("arquivos", f);
        const editando = formExame.id !== null;
        try {
        const res = await fetch(editando ? `${API_URL}/exames/${formExame.id}` : `${API_URL}/exames`, {
        method: editando ? "PUT" : "POST",
        body: fd
        });
        if (res.ok) {
        fecharModal("modal-exame");
        await carregarExames(window.pacienteIdAtivo);
        } else {
        alert(await lerErro(res));
        }
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }
async function excluirExame(id) {
    if (!confirm("Excluir este exame e os arquivos anexados?")) return;
    const res = await fetch(`${API_URL}/exames/${id}`, { method: "DELETE" });
    if (res.ok) await carregarExames(window.pacienteIdAtivo);
    else alert(await lerErro(res));
}  

// ===================================== OBSERVAÇÕES (semana do check-in e gerais)=====================================
// Cole aqui o link do seu Google Forms (deixe "" para esconder o botão)
const URL_GOOGLE_FORMS = "https://docs.google.com/forms/d/e/1FAIpQLScgrgewyyy_6sdsgpUccisp9WlMDW7RVgyhlhPhVfyt_vPYoQ/viewform?usp=send_form";
let obsAtuais = { semana: [], geral: [] };
let formObs = { id: null, tipo: "semana" };

async function carregarObservacoes(pacienteId) {
    const btn = document.getElementById("btn-forms");
    if (btn && URL_GOOGLE_FORMS) {
        btn.href = URL_GOOGLE_FORMS;
        btn.classList.remove("hidden");
        btn.classList.add("flex");
        }
    try {
        const res = await fetch(`${API_URL}/observacoes/${pacienteId}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const todas = await res.json();
        obsAtuais = {
        semana: todas.filter(o => o.tipo === "semana"),
        geral: todas.filter(o => o.tipo === "geral")
        };
        renderizarObs("semana");
        renderizarObs("geral");
        } catch (err) {
        console.error("Erro ao carregar observações:", err);
        ["semana", "geral"].forEach(t => {
        const el = document.getElementById(`obs-lista-${t}`);
        if (el) el.innerHTML = `<p class="text-rose-400 text-sm">Não foi possível carregar as observações.</p>`;
        });
        }
        }
function renderizarObs(tipo) {
        const el = document.getElementById(`obs-lista-${tipo}`);
        if (!el) return;
        const lista = obsAtuais[tipo];
        const rotulo = tipo === "semana" ? "Semana de " : "";
        if (!lista.length) {
        el.innerHTML = `<div class="bg-slate-900/50 border border-slate-700/60 rounded-xl p-5">
        <p class="text-sm text-slate-400">Nenhuma observação ainda. Clique em <strong>Nova observação</strong>.</p></div>`;
        return;
         }
        el.innerHTML = lista.map(o => `
        <div class="bg-slate-900/60 border border-slate-700/70 rounded-2xl p-5">
        <div class="flex flex-wrap items-start justify-between gap-3">
        <h4 class="font-bold text-white"><i class="fa-solid fa-calendar-day text-slate-500 mr-2"></i>${rotulo}${formatarData(o.data)}</h4>
        <div class="flex gap-1.5">
        <button onclick="abrirModalObs('${tipo}', ${o.id})" class="${BTN_MINI} bg-blue-500/15 text-blue-400 hover:bg-blue-500 hover:text-white"><i class="fa-solid fa-pen mr-1"></i>Editar</button>
        <button onclick="excluirObs(${o.id})" class="${BTN_MINI} bg-rose-500/15 text-rose-400 hover:bg-rose-500 hover:text-white"><i class="fa-solid fa-trash mr-1"></i>Excluir</button>
        </div>
        </div>
        <p class="text-sm text-slate-300 mt-3 whitespace-pre-wrap">${escaparHtml(o.texto)}</p>
        </div>`).join("");
        }
function abrirModalObs(tipo, id) {
        if (!exigirPaciente()) return;
        const o = id ? obsAtuais[tipo].find(x => x.id === id) : null;
        formObs = { id: o ? o.id : null, tipo };
        document.getElementById("mob-heading").innerText =
        (o ? "Editar " : "Nova ") + (tipo === "semana" ? "observação da semana" : "observação");
        document.getElementById("mob-rotulo-data").innerText = tipo === "semana" ? "Semana de" : "Data";
        document.getElementById("mob-data").value = o ? o.data : hojeISO();
        document.getElementById("mob-texto").value = o ? o.texto : "";
        abrirModal("modal-obs");
        }
async function salvarObs(e) {
        e.preventDefault();
        if (!exigirPaciente()) return;
        const editando = formObs.id !== null;
        const body = {
        pacienteId: window.pacienteIdAtivo,
        tipo: formObs.tipo,
        data: document.getElementById("mob-data").value,
        texto: document.getElementById("mob-texto").value
        };
    try {
        const res = await fetch(editando ? `${API_URL}/observacoes/${formObs.id}` : `${API_URL}/observacoes`, {
        method: editando ? "PUT" : "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
        });
        if (res.ok) {
        fecharModal("modal-obs");
        await carregarObservacoes(window.pacienteIdAtivo);
        } else {
        alert(await lerErro(res));
        }
        } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
        }
        }
async function excluirObs(id) {
        if (!confirm("Excluir esta observação?")) return;
        const res = await fetch(`${API_URL}/observacoes/${id}`, { method: "DELETE" });
        if (res.ok) await carregarObservacoes(window.pacienteIdAtivo);
        else alert(await lerErro(res));
        }
        // =====================================================
// HISTÓRICO UNIFICADO
// =====================================================
const TIPOS_HIST = {
    plano:      { rotulo: "Plano",       icone: "fa-bowl-food",     cor: "#a78bfa", aba: "planos" },
    evolucao:   { rotulo: "Evolução",    icone: "fa-chart-line",    cor: "#60a5fa", aba: "evolucao" },
    checkin:    { rotulo: "Check-in",    icone: "fa-calendar-check", cor: "#f472b6", aba: "checkin" },
    obs_semana: { rotulo: "Obs. semana", icone: "fa-note-sticky",   cor: "#fbbf24", aba: "checkin" },
    obs_geral:  { rotulo: "Observação",  icone: "fa-note-sticky",   cor: "#fbbf24", aba: "observacoes" },
    meta_acomp: { rotulo: "Meta",        icone: "fa-bullseye",      cor: "#4ade80", aba: "metas" },
    meta_nova:  { rotulo: "Meta",        icone: "fa-bullseye",      cor: "#4ade80", aba: "metas" },
    exame:      { rotulo: "Exame",       icone: "fa-flask",         cor: "#f87171", aba: "exames" }
};
const FILTROS_HIST = [
    ["todos", "Tudo", null],
    ["plano", "Plano", ["plano"]],
    ["evolucao", "Evolução", ["evolucao"]],
    ["checkin", "Check-ins", ["checkin", "obs_semana"]],
    ["obs", "Observações", ["obs_geral", "obs_semana"]],
    ["metas", "Metas", ["meta_acomp", "meta_nova"]],
    ["exame", "Exames", ["exame"]]
];

let historicoAtual = [];
let filtroHist = "todos";

async function carregarHistorico(pacienteId) {
    const el = document.getElementById("hist-lista");
    if (!el) return;
    el.innerHTML = `<p class="text-sm text-slate-400">Carregando...</p>`;
    try {
        const res = await fetch(`${API_URL}/historico/${pacienteId}`);
        if (!res.ok) throw new Error(res.status);
        historicoAtual = await res.json();
        renderizarHistorico();
    } catch (err) {
        console.error("Erro ao carregar histórico:", err);
        el.innerHTML = `<p class="text-rose-400 text-sm">Não foi possível carregar o histórico.</p>`;
    }
}

function definirFiltroHist(chave) {
    filtroHist = chave;
    renderizarHistorico();
}

function renderizarHistorico() {
    const fil = document.getElementById("hist-filtros");
    const el = document.getElementById("hist-lista");
    if (!fil || !el) return;

    fil.innerHTML = FILTROS_HIST.map(([chave, rotulo]) => {
        const ativo = chave === filtroHist;
        return `<button onclick="definirFiltroHist('${chave}')"
            class="px-3 py-1.5 rounded-lg text-xs font-semibold transition ${ativo ? "text-white" : "text-slate-400 hover:text-white"}"
            style="background:${ativo ? "linear-gradient(90deg,#4c1d95,#be185d)" : "#181430"}">${rotulo}</button>`;
    }).join("");

    const tipos = FILTROS_HIST.find(f => f[0] === filtroHist)[2];
    const itens = tipos ? historicoAtual.filter(h => tipos.includes(h.tipo)) : historicoAtual;

    if (!itens.length) {
        el.innerHTML = `<div class="bg-slate-900/50 border border-slate-700/60 rounded-xl p-5">
            <p class="text-sm text-slate-400">Nada registrado${tipos ? " neste filtro" : " ainda"}.</p></div>`;
        return;
    }

    let mesAtual = "";
    el.innerHTML = `<div class="relative pl-6" style="border-left:2px solid rgba(255,255,255,.08)">` +
        itens.map(h => {
            const t = TIPOS_HIST[h.tipo] || TIPOS_HIST.obs_geral;
            const [a, m] = h.data.split("-");
            const mes = new Date(+a, +m - 1, 1).toLocaleDateString("pt-BR", { month: "long", year: "numeric" });
            let cab = "";
            if (mes !== mesAtual) {
                mesAtual = mes;
                cab = `<p class="text-xs font-bold uppercase tracking-wider text-slate-500 mt-6 mb-3 first:mt-0 -ml-6 pl-6">${mes}</p>`;
            }

            let extra = escaparHtml(h.extra);
            let corExtra = "#c3bedd";
            if (h.tipo === "meta_acomp" && RESULTADOS_META[h.extra]) {
                extra = RESULTADOS_META[h.extra].rotulo;
                corExtra = { mantida: "#4ade80", parcial: "#fbbf24", nao_mantida: "#fb7185" }[h.extra];
            }

            return `${cab}
            <div class="relative mb-3">
                <span class="absolute -left-[31px] top-4 w-3 h-3 rounded-full" style="background:${t.cor};box-shadow:0 0 0 4px #181430"></span>
                <button onclick="trocarAbaProntuario('${t.aba}')"
                    class="w-full text-left bg-slate-900/60 border border-slate-700/60 hover:border-slate-500 rounded-xl p-4 transition">
                    <div class="flex flex-wrap items-center justify-between gap-2">
                        <div class="flex items-center gap-2 min-w-0">
                            <i class="fa-solid ${t.icone} text-xs" style="color:${t.cor}"></i>
                            <span class="text-sm font-semibold text-white truncate">${escaparHtml(h.titulo)}</span>
                        </div>
                        <span class="text-xs text-slate-500 shrink-0">${formatarData(h.data)}</span>
                    </div>
                    ${extra ? `<p class="text-xs font-semibold mt-1" style="color:${corExtra}">${extra}</p>` : ""}
                    ${h.detalhe ? `<p class="text-sm text-slate-400 mt-1 whitespace-pre-wrap line-clamp-3">${escaparHtml(h.detalhe)}</p>` : ""}
                </button>
            </div>`;
        }).join("") + `</div>`;
}
// 18. GRÁFICO DE EVOLUÇÃO DE PESO
let meuGraficoEvolucao = null;
async function carregarGraficoEvolucao(pacienteId) {
    const canvas = document.getElementById('graficoEvolucao');
        if (!canvas) return;
        if (meuGraficoEvolucao) {
        meuGraficoEvolucao.destroy();
        meuGraficoEvolucao = null;
        }
let checkins = [];
        try {
        const res = await fetch(`${API_URL}/checkins/${pacienteId}`);
        if (res.ok) checkins = await res.json();
        } catch (err) {
        console.error("Erro ao buscar check-ins para o gráfico:", err);
        }
// Junta check-ins e registros de evolução; na mesma data vale o da Evolução
        const mapa = new Map();
        checkins.forEach(c => mapa.set(c.data_checkin, Number(c.peso_atual_kg)));
        evolucaoAtual.forEach(r => { if (r.peso != null) mapa.set(r.data, Number(r.peso)); });
        const pontos = [...mapa.entries()].sort((a, b) => a[0].localeCompare(b[0]));
        if (!pontos.length) return;
        meuGraficoEvolucao = new Chart(canvas, {
        type: 'line',
        data: {
        labels: pontos.map(p => formatarData(p[0])),
        datasets: [{
        label: 'Peso (kg)',
        data: pontos.map(p => p[1]),
        borderColor: '#6d4de0',
        backgroundColor: 'rgba(109, 77, 224, 0.10)',
        borderWidth: 2,
        tension: 0.3,
        fill: true,
        pointBackgroundColor: '#6d4de0'
        }]
        },
        options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { labels: { color: '#827d9c' } } },
        scales: {
        x: { ticks: { color: '#827d9c' }, grid: { color: 'rgba(0, 0, 0, 0.06)' } },
        y: { ticks: { color: '#827d9c' }, grid: { color: 'rgba(0, 0, 0, 0.06)' } }
        }
        }
        });
        }
async function carregarOrientacoes(pacienteId) {
        try {
        const res = await fetch(`${API_URL}/orientacoes/${pacienteId}`);
        if (res.ok) {
        const data = await res.json();
        // Coloca o texto guardado dentro da textarea do HTML
        const textarea = document.getElementById("input-orientacoes"); // Substitui pelo ID correto da tua textarea
        if (textarea) {
        textarea.value = data.texto || "";
        }
        }
        } catch (err) {
        console.error("Erro ao carregar orientações:", err);
        }
        }
async function salvarOrientacoes(pacienteId) {
        const textarea = document.getElementById("input-orientacoes");        if (!textarea) return;
        const texto = textarea.value;
        try {
        const res = await fetch(`${API_URL}/orientacoes`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ pacienteId: parseInt(pacienteId), texto: texto })
        });
        if (res.ok) {
        alert("Orientações salvas com sucesso!");
        } else {
        alert("Erro ao salvar as orientações.");
        }
        } catch (err) {
        console.error("Erro de conexão:", err);
        alert("Erro de conexão com o servidor.");
        }
        }    
async function excluirPaciente(id) {
        if (!confirm("Tens a certeza de que pretendes excluir este paciente?")) {
        return;
        }
        try {
        // Usa a variável API_URL para garantir que vai para a porta correta (5001)
        const response = await fetch(`${API_URL}/pacientes/${id}`, {
        method: 'DELETE'
        });
        if (response.ok) {
        alert("Paciente excluído com sucesso!");
        carregarPacientes();
        } else {
        alert("Erro ao tentar excluir o paciente.");
        }
        } catch (err) {
        console.error("Erro de conexão:", err);
        alert("Erro de conexão com o servidor ao tentar excluir.");
        }
        }
function editarPaciente(id) {
        const p = listaPacientes.find(x => x.id === id);
        if (!p) {
        alert("Paciente não encontrado na lista. Recarregue a página.");
        return;
        }
        pacienteEmEdicaoId = p.id;
        document.getElementById('p-nome').value = p.nome;
        document.getElementById('p-email').value = p.email;
        document.getElementById('p-telefone').value = p.telefone || "";
        document.getElementById('p-data').value = p.data_nascimento;
        document.getElementById('p-sexo').value = (p.sexo || "M").trim();
        document.getElementById('p-status').value = (p.status || "ativo").toLowerCase();
        atualizarBotaoFormulario(true);
        document.getElementById('form-paciente').scrollIntoView({ behavior: 'smooth', block: 'center' });
        document.getElementById('p-cpf').value = p.cpf ? formatarCpf(p.cpf) : "";
        }

function cancelarEdicao() {
        pacienteEmEdicaoId = null;
        document.getElementById('form-paciente').reset();
        atualizarBotaoFormulario(false);
        }
function alternarSenha() {
        const senha = document.getElementById("login-senha");
        const icone = document.getElementById("icone-senha");
        if (senha.type === "password") {
        senha.type = "text";
        icone.classList.remove("fa-eye");
        icone.classList.add("fa-eye-slash");
        } else {
        senha.type = "password";
        icone.classList.remove("fa-eye-slash");
        icone.classList.add("fa-eye");
        }
        }
function abrirMenuMobile() {
    const sidebar = document.getElementById("sidebar-acnut");
    const overlay = document.getElementById("acnut-menu-overlay");
    if (!sidebar || !overlay) return;

    sidebar.classList.add("aberto-mobile");
    overlay.classList.remove("hidden");
    document.body.style.overflow = "hidden";
    // Troca ☰ por X em todos os botões de menu mobile
    document.querySelectorAll(".btn-menu-mobile i").forEach(icone => {
        icone.classList.replace("fa-bars", "fa-xmark");
    });
}
function fecharMenuMobile() {
    const sidebar = document.getElementById("sidebar-acnut");
    const overlay = document.getElementById("acnut-menu-overlay");
    if (!sidebar || !overlay) return;

    sidebar.classList.remove("aberto-mobile");
    overlay.classList.add("hidden");
    document.body.style.overflow = "";
    // Força reflow no iOS Safari para restaurar scroll corretamente
    document.body.offsetHeight;
    // Volta X para ☰
    document.querySelectorAll(".btn-menu-mobile i").forEach(icone => {
        icone.classList.replace("fa-xmark", "fa-bars");
    });
}
function alternarSidebar() {
        const sidebar = document.getElementById("sidebar-acnut");
        if (!sidebar) return;
        const expandida = sidebar.classList.toggle("expandida");
        try {
        localStorage.setItem("acnut_sidebar_expandida", expandida ? "1" : "0");
        } catch (e) {}
        }
function restaurarSidebar() {
        const sidebar = document.getElementById("sidebar-acnut");
        if (!sidebar) return;
        try {
        if (localStorage.getItem("acnut_sidebar_expandida") === "1") {
        sidebar.classList.add("expandida");
        }
        } catch (e) {}
        }
async function restaurarVersaoPlano(id) {
    if (!confirm("Restaurar esta versão? O plano atual será guardado automaticamente como uma nova versão antes da troca.")) return;
    try {
        const res = await fetch(`${API_URL}/planos/versao/${id}/restaurar`, { method: "POST" });
        if (res.ok) {
            const data = await res.json();
            alert(data.mensagem);
            fecharModal("modal-versoes");
            await carregarPlanoAlimentarProntuario(window.pacienteIdAtivo);
            await carregarOrientacoes(window.pacienteIdAtivo);
        } else {
            alert(await lerErro(res));
        }
    } catch (err) {
        console.error(err);
        alert("Erro de conexão com o servidor.");
    }
}

// ---------- CPF ----------
function mascararCpf(el) {
    const d = el.value.replace(/\D/g, "").slice(0, 11);
    el.value = d.replace(/(\d{3})(\d)/, "$1.$2").replace(/(\d{3})(\d)/, "$1.$2").replace(/(\d{3})(\d{1,2})$/, "$1-$2");
}
function formatarCpf(c) {
    const d = String(c || "").replace(/\D/g, "");
    return d.length === 11 ? d.replace(/(\d{3})(\d{3})(\d{3})(\d{2})/, "$1.$2.$3-$4") : d;
}

// ---------- Telefone ----------
function mascararTelefone(el) {
    let d = el.value.replace(/\D/g, "").slice(0, 11);
    // Formato: (11) 98765-4321 ou (11) 8765-4321
    if (d.length <= 2) {
        el.value = d.replace(/(\d{0,2})/, "($1");
    } else if (d.length <= 6) {
        el.value = d.replace(/(\d{2})(\d{0,4})/, "($1) $2");
    } else if (d.length <= 10) {
        el.value = d.replace(/(\d{2})(\d{4,5})(\d{0,4})/, "($1) $2-$3");
    } else {
        el.value = d.replace(/(\d{2})(\d{5})(\d{4})/, "($1) $2-$3");
    }
}
function formatarTelefone(t) {
    const d = String(t || "").replace(/\D/g, "");
    if (d.length === 10) return d.replace(/(\d{2})(\d{4})(\d{4})/, "($1) $2-$3");
    if (d.length === 11) return d.replace(/(\d{2})(\d{5})(\d{4})/, "($1) $2-$3");
    return t;
}

// ---------- Login: alternar equipe / paciente -->
function modoLogin(modo) {
    const pac = modo === "paciente";
    document.getElementById("form-login-equipe").style.display = pac ? "none" : "";
    document.getElementById("form-login-paciente").style.display = pac ? "" : "none";
    document.getElementById("tab-equipe").classList.toggle("ativo", !pac);
    document.getElementById("tab-paciente").classList.toggle("ativo", pac);
    document.querySelector(".lg-title").firstChild.textContent = pac ? "Área do paciente" : "Faça seu login";
    document.querySelector(".lg-foot").style.display = pac ? "none" : "";
}

async function fazerLoginPaciente(e) {
    e.preventDefault();
    const erro = document.getElementById("login-erro-pac");
    const msg = erro.querySelector("span");
    try {
        const res = await fetch(`${API_URL}/auth/paciente`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                cpf: document.getElementById("pac-cpf").value,
                nascimento: document.getElementById("pac-nasc").value
            })
        });
        const data = await res.json().catch(() => ({}));
        if (!res.ok) { msg.innerText = data.mensagem || "CPF ou data de nascimento incorretos."; erro.style.display = "flex"; return; }
        sessionStorage.setItem(TOKEN_KEY, data.token);
        sessionStorage.setItem("bybar_user", JSON.stringify({ nome: data.nome, tipo: "paciente" }));
        erro.style.display = "none";
        entrarComoPaciente(data.nome);
    } catch {
        msg.innerText = "Não foi possível conectar ao servidor.";
        erro.style.display = "flex";
    }
}

// ---------- Área do paciente (somente leitura) ----------
let dadosPaciente = null;

function entrarComoPaciente(nome) {
    document.getElementById("login-container").style.display = "none";
    document.getElementById("main-app").style.display = "none";
    document.getElementById("area-paciente").style.display = "block";
    document.getElementById("pac-nome").innerText = nome || "";
    carregarAreaPaciente();
}

async function carregarAreaPaciente() {
    const el = document.getElementById("pac-conteudo");
    el.innerHTML = `<p class="text-slate-400 text-sm">Carregando...</p>`;
    try {
        const res = await fetch(`${API_URL}/paciente/tudo`);
        if (!res.ok) throw new Error(res.status);
        dadosPaciente = await res.json();
        document.getElementById("pac-nome").innerText = dadosPaciente.nome.split(" ")[0];
        abaPaciente("plano");
    } catch {
        el.innerHTML = `<p class="text-rose-400 text-sm">Não foi possível carregar seus dados. Tente novamente.</p>`;
    }
}

function abaPaciente(aba) {
    document.querySelectorAll(".pac-aba").forEach(b => {
        const ativa = b.dataset.aba === aba;
        b.style.background = ativa ? "linear-gradient(90deg,#4c1d95,#be185d)" : "#181430";
        b.style.color = ativa ? "#fff" : "#a99bd0";
    });
    const d = dadosPaciente;
    if (!d) return;
    const el = document.getElementById("pac-conteudo");
    const vazio = t => `<div class="bg-slate-800/50 border border-white/5 rounded-2xl p-5 text-sm text-slate-400">${t}</div>`;
    const alimento = i => {
        const q = Number(i.quantidadeG) > 0 ? ` — ${i.quantidadeG}g` : "";
        const m = i.medidaCaseira ? ` (${escaparHtml(i.medidaCaseira)})` : "";
        return `${escaparHtml(i.alimento)}${q}${m}`;
    };

    if (aba === "plano") {
        const refs = d.plano.refeicoes || [];
        el.innerHTML = refs.length ? refs.map(r => {
            const opcoes = agruparOpcoes(r);
            return `
            <div class="bg-slate-800/50 border border-white/5 rounded-2xl p-5">
                <h3 class="font-bold text-white text-lg">${escaparHtml(r.nome)}
                    <span class="text-sm font-semibold ml-2" style="color:#d8b4fe">${escaparHtml(r.horario)}</span></h3>
                ${r.observacoes ? `<p class="text-sm text-slate-400 mt-1 whitespace-pre-wrap">${escaparHtml(r.observacoes)}</p>` : ""}
                ${opcoes.map((o, idx) => `
                    <p class="text-xs font-bold uppercase tracking-wider mt-4 mb-1" style="color:#d8b4fe">${opcoes.length > 1 ? `Opção ${idx + 1}` : "Refeição"}</p>
                    ${o.itens.map(i => `
                        <p class="text-sm text-white py-1">${alimento(i)}</p>
                        ${i.substituicoes.map(s => `<p class="text-sm text-amber-300 ml-5 pb-1">↳ ou ${alimento(s)}</p>`).join("")}
                    `).join("")}
                `).join("")}
            </div>`;
        }).join("") : vazio("Seu plano alimentar ainda não foi cadastrado.");
    }

    else if (aba === "subs") {
        const blocos = (d.plano.refeicoes || []).map(r => {
            const comSub = r.itens.filter(i => !i.substituiItemId)
                .map(i => ({ i, subs: r.itens.filter(s => s.substituiItemId === i.id) }))
                .filter(x => x.subs.length);
            if (!comSub.length) return "";
            return `
            <div class="bg-slate-800/50 border border-white/5 rounded-2xl p-5 space-y-3">
                <h3 class="font-bold text-white">${escaparHtml(r.nome)}</h3>
                ${comSub.map(x => `
                    <div>
                        <p class="text-sm text-white font-medium">${alimento(x.i)}</p>
                        ${x.subs.map(s => `<p class="text-sm text-amber-300 ml-4">↳ pode trocar por: ${alimento(s)}</p>`).join("")}
                    </div>`).join("")}
            </div>`;
        }).join("");
        el.innerHTML = blocos || vazio("Nenhuma substituição cadastrada.");
    }

    else if (aba === "orient") {
        el.innerHTML = d.orientacoes
            ? `<div class="bg-slate-800/50 border border-white/5 rounded-2xl p-5 text-sm text-slate-200 whitespace-pre-wrap">${escaparHtml(d.orientacoes)}</div>`
            : vazio("Nenhuma orientação cadastrada.");
    }

    else if (aba === "metas") {
        el.innerHTML = d.metas.length ? d.metas.map(m => {
            const t = TIPOS_META[m.tipo] || TIPOS_META.outro;
            const pontos = (m.acompanhamentos || []).slice(0, 8).reverse().map(a => {
                const r = RESULTADOS_META[a.resultado];
                return `<span title="${formatarData(a.data)} • ${r.rotulo}" class="w-3 h-3 rounded-full ${r.ponto}"></span>`;
            }).join("");
            return `
            <div class="bg-slate-800/50 border border-white/5 rounded-2xl p-5 ${m.status === "concluida" ? "opacity-70" : ""}">
                <div class="flex items-center gap-3">
                    <div class="w-10 h-10 rounded-xl ${t.cor} flex items-center justify-center"><i class="fa-solid ${t.icone}"></i></div>
                    <div>
                        <h3 class="font-bold text-white">${escaparHtml(m.titulo)}${m.status === "concluida" ? " <span class='text-xs text-green-400'>(concluída)</span>" : ""}</h3>
                        <p class="text-xs text-slate-400">${t.rotulo}${m.prazo ? ` • até ${formatarData(m.prazo)}` : ""}</p>
                    </div>
                </div>
                ${m.descricao ? `<p class="text-sm text-slate-300 mt-3 whitespace-pre-wrap">${escaparHtml(m.descricao)}</p>` : ""}
                ${pontos ? `<div class="flex items-center gap-1.5 mt-3">${pontos}</div>` : ""}
            </div>`;
        }).join("") : vazio("Nenhuma meta combinada ainda.");
    }
}
// ===== EMPRESAS (super_admin) =====
async function carregarEmpresas() {
    // Verificar se é super_admin
    const user = JSON.parse(sessionStorage.getItem('bybar_user') || '{}');
    if (user.perfil !== 'super_admin') {
        alert('Acesso restrito a super administradores.');
        navegar('dashboard');
        return;
    }

    try {
        const res = await fetch(`${API_URL}/empresas`);
        if (!res.ok) {
            if (res.status === 403) {
                alert('Acesso negado. Apenas super administradores podem ver esta tela.');
                navegar('dashboard');
                return;
            }
            throw new Error(`Erro HTTP: ${res.status}`);
        }
        const empresas = await res.json();
        renderEmpresas(empresas);
    } catch (err) {
        console.error("Erro ao carregar empresas:", err);
        document.getElementById('tabela-empresas').innerHTML = '<tr><td colspan="3" class="py-6 text-center text-rose-400">Erro ao carregar empresas</td></tr>';
    }
}

function renderEmpresas(empresas) {
    const tbody = document.getElementById('tabela-empresas');
    if (!tbody) return;

    if (!empresas || empresas.length === 0) {
        tbody.innerHTML = '<tr><td colspan="3" class="py-6 text-center text-slate-400">Nenhuma empresa cadastrada</td></tr>';
        return;
    }

    tbody.innerHTML = empresas.map(emp => `
        <tr class="hover:bg-slate-800/50">
            <td class="py-3 px-4 text-white font-mono">#${emp.id}</td>
            <td class="py-3 px-4 font-medium text-white">${escaparHtml(emp.nome)}</td>
            <td class="py-3 px-4 text-slate-300">${new Date(emp.criadoEm).toLocaleDateString('pt-BR')}</td>
        </tr>
    `).join('');
}

async function cadastrarEmpresa(event) {
    event.preventDefault();

    const user = JSON.parse(sessionStorage.getItem('bybar_user') || '{}');
    if (user.perfil !== 'super_admin') {
        alert('Acesso restrito a super administradores.');
        return;
    }

    const nome = document.getElementById('emp-nome').value.trim();
    if (!nome) {
        alert('Informe o nome da empresa.');
        return;
    }

    const btn = event.target.querySelector('button[type="submit"]');
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<i class="fa-solid fa-spinner fa-spin mr-2"></i>Cadastrando…';

    try {
        const res = await fetch(`${API_URL}/empresas`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ nome })
        });

        if (!res.ok) {
            const err = await res.json();
            throw new Error(err.mensagem || 'Erro ao cadastrar empresa');
        }

        const data = await res.json();
        alert(`Empresa "${data.nome}" criada com sucesso!`);
        document.getElementById('form-empresa').reset();
        carregarEmpresas();

        // Atualizar contador se necessário
        atualizarDashboard();
    } catch (err) {
        console.error("Erro ao cadastrar empresa:", err);
        alert(err.message);
    } finally {
        btn.disabled = false;
        btn.innerHTML = originalText;
    }
}

// ===== INICIALIZAÇÃO =====
document.addEventListener("DOMContentLoaded", async () => {
    restaurarSidebar();

    // 1. Verificar se há token de convite na URL
    if (typeof verificarConviteURL === "function" && verificarConviteURL()) {
        return; // Para aqui, mostra tela de convite
    }

    // 2. Verificar se há sessão válida
    if (sessaoValida()) {
        let u = {};
        try { u = JSON.parse(sessionStorage.getItem("bybar_user") || "{}"); } catch {}
        if (u.tipo === "paciente") entrarComoPaciente(u.nome);
        else entrarNoSistema(u.nome || "");
        return;
    }

    // 3. Sem sessão - verificar se precisa de onboarding (sem empresas)
    // Só verifica onboarding se não houver token de convite
    try {
        const res = await fetch(`${API_URL}/empresas/count`);
        if (res.ok) {
            const data = await res.json();
            if (data.count === 0) {
                // Sem empresas - mostrar onboarding
                if (typeof mostrarOnboarding === "function") mostrarOnboarding();
                return;
            }
        }
    } catch (e) {
        console.warn("Não foi possível verificar empresas:", e);
    }

    // 4. Caso padrão - mostrar login
    sessionStorage.removeItem(TOKEN_KEY);
    if (typeof mostrarLogin === "function") mostrarLogin();
});