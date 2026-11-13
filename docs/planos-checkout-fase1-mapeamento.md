# Fase 1 - Mapeamento Completo: Venda de Planos (Planos-Checkout)

## Contexto
Implementar a venda de planos de assinatura com dois pontos de entrada:
1. **Tela de Login** (público) - 3 cards de planos visíveis para novos usuários
2. **Menu "Plano & Assinatura"** (super_admin) - Painel de controle só leitura

**ESTÁGIO ATUAL**: FRONTEND + SIMULAÇÃO + CONTRATO DE API (docs)  
**NÃO IMPLEMENTAR**: Backend real do checkout, NÃO chamar Asaas agora  
**SEM PUSH** e **SEM MERGE** na main

---

## 1. Análise do Estado Atual

### 1.1 Tela de Login (`index.html` linhas 917-1043)
- **Dois formulários**: "Sou da equipe" (email/senha) e "Sou paciente" (CPF/nascimento)
- **Tabs** para alternar entre eles (`modoLogin()`)
- **Links existentes**: "Esqueci minha senha", "Solicitar acesso" (modal)
- **Container**: `#login-container` com classe `.lg-screen`
- **Layout**: SVG ilustração à esquerda, formulário à direita (desktop)

### 1.2 Tela "Definir Minha Senha" (`index.html` linhas 1245-1306)
- **Container**: `#definir-senha-container` (id inferido do JS)
- **Estados**: carregando → válido (formulário) → inválido → sucesso
- **Função JS**: `definirSenha(event)` - define senha via token na URL
- **Navegação de volta**: `modoLogin('equipe')` + troca de containers

### 1.3 Backend - Endpoints de Billing (`BillingModule.cs`)
| Endpoint | Método | Auth | Descrição |
|---|---|---|---|
| `/api/assinatura/status` | GET | Qualquer | Status da assinatura da empresa logada (retorna 200 com `sem_plano` se não há assinatura) |
| `/api/billing/assinatura` | GET | Admin/SuperAdmin | Detalhes da assinatura |
| `/api/billing/assinatura/iniciar` | POST | Admin/SuperAdmin | **Cria customer + subscription no Asaas** |
| `/api/billing/assinatura/cancelar` | POST | Admin/SuperAdmin | Cancela assinatura |
| `/api/billing/limites` | GET | Qualquer | Limites do plano atual |
| `/api/webhook/asaas` | POST | Público (HMAC) | Webhook Asaas |
| `/api/billing/webhook-log` | GET | SuperAdmin | Log de webhooks |
| `/api/billing/bypass-limites` | POST | SuperAdmin | Toggle bypass |

**⚠️ VERIFICAR**: Código retorna 200 (`sem_plano`) mas em produção console mostra 404. Procurar todos `NotFound`/404 no BillingModule e caso super_admin sem empresa_id.

### 1.4 Frontend - Tela Billing (`billing.html` - `sec-billing`)
- **Catálogo**: `PLANOS_CATALOGO` com 3 planos (Básico, Profissional, Enterprise) + Sem Plano
- **Ciclos**: Mensal, Semestral, Anual com preços
- **Função**: `carregarBilling()` - exige admin (`exigirAdmin()`)
- **Ação**: `confirmarMudancaPlano()` → POST `/api/billing/assinatura/iniciar`

### 1.5 Verificação de Plano no App (`app.js`)
- `verificarBannerPlano()` - só para perfil `profissional`
- Busca `/api/assinatura/status` → mostra banner se `sem_plano` com limite finito
- Banner tem botão "Ver Planos" → `navegar('billing')` → **REMOVER**

### 1.6 Fluxo de Login Atual
```javascript
// fazerLogin() - equipe
POST /api/auth/login → { token, nome, perfil }
sessionStorage.setItem('bybar_token', token)
sessionStorage.setItem('bybar_user', JSON.stringify({ nome, tipo: 'equipe', perfil }))
entrarNoSistema() → navegar('dashboard') → verificarBannerPlano()

// fazerLoginPaciente() - paciente
POST /api/auth/paciente → { token, nome }
sessionStorage.setItem(...)
entrarComoPaciente()
```

---

## 2. DECISÕES FINAIS (Vinculantes)

### 2.1 Regras Gerais (Invioláveis)
| Regra | Descrição |
|---|---|
| **R1** | Etapa = Frontend + Simulação + Contrato API. **NÃO** backend real, **NÃO** Asaas. |
| **R2** | Empresa e usuário **SÓ** criados APÓS webhook confirmar pagamento. **NUNCA** JWT/login no início do checkout. |
| **R3** | Preço e ciclo **SEMPRE** vêm do backend (BillingModule). Front envia só `plano` + `ciclo`; backend **NUNCA** confia em valor do navegador. |
| **R4** | `git add` **apenas** em arquivos criados/alterados. **NUNCA** `git add .` (existem `temp-superadmin/`, `.vscode/` soltos). |
| **R5** | **NENHUM MERGE** até backend funcionar no sandbox (cards no login público não vendem nada sem backend). |

### 2.2 Decisões de Produto (Lista 1)
| # | Decisão | Definição |
|---|---|---|
| **D1** | Planos no login | **3 cards visíveis** na própria tela (direita desktop, abaixo formulário mobile). Atalho "Quero assinar" rola até cards. **NÃO** aba/tab. |
| **D2** | Pré-cadastro | **NÃO** criar empresa no início. Criar **pré-cadastro pendente** (`status: aguardando_pagamento`) com **UUID imprevisível** (ref). Empresa/usuário nascem **só no webhook**. Pré-cadastros não pagos **expiram**. |
| **D3** | Trial | **NÃO**. Acesso só após pagamento confirmado. Se backend suportar trial, deixar desativado. |
| **D4** | Pagamento | **Asaas Subscription API**: `billingType: UNDEFINED` (cliente escolhe Pix/cartão/boleto), `cycle: MONTHLY/SEMIANNUALLY/YEARLY`, `externalReference = ref do pré-cadastro`, redirect para `invoiceUrl` do Asaas. **NUNCA** receber dados de cartão. **Confirmar no sandbox** se `UNDEFINED` aceito em assinaturas; se não, usar link de pagamento recorrente. |
| **D5** | Primeiro usuário | Dono da clínica, **mesmo perfil dos clientes atuais** (hoje `profissional`? `admin`?). **NUNCA** `super_admin`, **SEM** acesso a "Plano & Assinatura". |
| **D6** | "Definir Minha Senha" | Reaproveitar **SIM**, mas **só após confirmação de pagamento**, via **link único, uso único, com validade**, enviado por e-mail (também liberado na tela "Pagamento confirmado"). |
| **D7** | Campos formulário | Nome, E-mail, CPF/CNPJ (validação dígitos verificadores), Telefone, Nome da clínica, Aceite Termos/LGPD. **Senha SÓ depois** no "Definir Minha Senha". |

### 2.3 Decisões Técnicas (Lista 2 + Q1-Q7)
| # | Item | Definição |
|---|---|---|
| **T1** | Config preços | `frontend/interface/config/planos.js` → **script clássico** (`window.PLANOS_CONFIG`). App usa scripts globais. Backend mantém própria fonte (`BillingModule.cs`) com **mesmos valores**. |
| **T2** | Painel super_admin | **Só leitura**: listar assinaturas + eventos webhook. Ações depois. |
| **T3** | MODO_SIMULACAO | Constante **no próprio `planos.js`** (`true` por enquanto). Faixa visível "MODO SIMULAÇÃO" nas telas. **NÃO** `VITE_` a menos que projeto já use Vite (verificar). |
| **T4** | Validação CPF/CNPJ | **Função própria** com dígitos verificadores, **sem biblioteca**. |
| **T5** | Termos/LGPD | Linkar documentos existentes se houver; senão **placeholder com TODO claro** (pendência obrigatória antes de produção). |
| **T6** | Asaas Integration | Subscription API + redirect para fatura (conforme D4). |
| **T7** | Preços antigos (49,90/99,90/199,90) | **Procurar** em `*.sql`, seeds, JSON, scripts; podem estar no banco (tabela planos/assinaturas). Dizer qual query rodar no DBeaver. **Nesta etapa apenas LISTAR** correção no `BillingModule.cs`: Mensal 27,90, Semestral **159,90** (hoje 160), Anual **299,90** (hoje 300). Aplicar só na fase backend. |

---

## 3. Arquitetura do Frontend (Simulação)

### 3.1 Config Única: `frontend/interface/config/planos.js`
```javascript
// window.PLANOS_CONFIG - fonte única do frontend
window.PLANOS_CONFIG = {
  // VALORES DEVEM ESPERLHAR BillingModule.cs EXATAMENTE
  precos: {
    basico: { mensal: 27.90, semestral: 159.90, anual: 299.90 },
    profissional: { mensal: 27.90, semestral: 159.90, anual: 299.90 },
    enterprise: { mensal: 27.90, semestral: 159.90, anual: 299.90 }
  },
  limites: {
    basico: { max_pacientes: 50, max_profissionais: 2, storage_mb: 500, max_versoes_plano: 5 },
    profissional: { max_pacientes: 200, max_profissionais: 10, storage_mb: 2000, max_versoes_plano: 20 },
    enterprise: { max_pacientes: 1000, max_profissionais: 50, storage_mb: 10000, max_versoes_plano: 100 }
  },
  destaque: {
    semestral: { economia: "15%", label: "Economia de 15%" },
    anual: { economia: "25%", label: "Melhor custo-benefício" }
  },
  planosOrdem: ['basico', 'profissional', 'enterprise'],
  MODO_SIMULACAO: true  // ← CONSTANTE AQUI
};
```

### 3.2 Camada de API: `frontend/interface/js/billingApi.js`
```javascript
// Simulação completa - substitui chamadas reais
const billingApi = {
  // (a) Criar pré-cadastro + iniciar assinatura Asaas
  async criarPreCadastroEAssinatura(dados) {
    // dados: { plano, ciclo, nome, email, cpfCnpj, telefone, clinica, termosAceitos }
    // retorna: { ref, invoiceUrl, status: 'aguardando_pagamento' }
  },

  // (c) Polling de status
  async consultarStatus(ref) {
    // retorna: { status: 'aguardando_pagamento' | 'pago' | 'expirado' | 'recusado', definirSenhaToken? }
  },

  // Super admin
  async listarAssinaturas() { /* mock */ },
  async listarWebhookEvents() { /* mock */ },
};
```

### 3.3 Fluxo de Telas (Estados no Login)
```
ESTADO 0: Login normal (formulários equipe/paciente) + 3 CARDS VISÍVEIS
    ↓ Clica "Assinar" em um card
ESTADO 1: Modal/Seção "Dados Cadastrais" (nome, email, cpf/cnpj, telefone, clínica, termos)
    ↓ "Continuar para Pagamento"
ESTADO 2: "Finalizar Pagamento" - explica Asaas + botão "Pagar com Asaas" → redirect invoiceUrl
    ↓ (usuário paga no Asaas, volta para o app)
ESTADO 3: "Aguardando Confirmação" - polling consultarStatus(ref) a cada 5s
    - Aviso: "Boleto pode levar dias úteis"
    - Botão "Já paguei, verificar novamente"
    ↓ Webhook confirma / polling retorna 'pago'
ESTADO 4: "Pagamento Confirmado" - botão "Criar meu acesso" → abre "Definir Minha Senha" com token
    ↓ Define senha
ESTADO 5: Login normal (agora com empresa ativa)
```
**Estados de erro**: Recusado, Expirado, Erro rede - mensagens claras + "Tentar novamente"

---

## 4. Mudanças no Frontend (Escopo Fase 2)

### 4.1 `frontend/interface/config/planos.js` **(NOVO)**
- Script clássico (`window.PLANOS_CONFIG`)
- Valores espelhando `BillingModule.cs` (Mensal 27.90, Semestral 159.90, Anual 299.90)
- `MODO_SIMULACAO: true`
- Faixa "MODO SIMULAÇÃO" visível quando true

### 4.2 `frontend/interface/index.html`
**No `#login-container`:**
- **Incluir** `<script src="/interface/config/planos.js"></script>`
- **Adicionar 3 cards** de planos (lado direito desktop, abaixo formulário mobile)
- **Atalho** "Quero assinar" rola até cards (`scrollIntoView`)
- **Modal/Seção** para fluxo Estados 1-4 (sobrescreve ou ao lado dos formulários)
- **Faixa** "MODO SIMULAÇÃO" no topo se `PLANOS_CONFIG.MODO_SIMULACAO`

**Na tela "Definir Minha Senha" (`#definir-senha-container`):**
- Aceitar token via URL (`?token=...`) para fluxo pós-pagamento
- Link único, uso único, com validade

### 4.3 `frontend/interface/app.js`
- **Remover** botão "Ver Planos" do `mostrarBannerPlano()` e bloqueio de cadastro
- **Nova mensagem**: "Assinatura inativa. Regularize pelo link de pagamento enviado por e-mail ou fale com o suporte."
- **Funções novas**: `iniciarContratacao(plano)`, `mostrarDadosCadastrais()`, `mostrarPagamento()`, `mostrarAguardando()`, `mostrarConfirmado()`, `pollingStatus(ref)`
- **Validação CPF/CNPJ** própria (dígitos verificadores)
- **Integração** `billingApi` (simulação)

### 4.4 `frontend/interface/js/billingApi.js` **(NOVO)**
- Camada única de acesso à API
- `MODO_SIMULACAO` lido de `window.PLANOS_CONFIG.MODO_SIMULACAO`
- Mocks realistas para todos endpoints
- Comentários `// TODO: substituir por fetch real` onde plugar backend

### 4.5 `frontend/interface/billing.html` (Painel Super Admin)
- **Visibilidade**: Item `nav-billing` no sidebar **só se `user.perfil === 'super_admin'`**
- **Rota**: `navegar('billing')` bloqueia se não super_admin
- **Tabela 1**: Assinaturas (Empresa, Plano, Período, Valor, Status, Próximo Vencimento) - **dados mockados**
- **Tabela 2**: Eventos Webhook (já existe `/api/billing/webhook-log`) - **dados mockados**
- **Comentários** `// TODO: plugar API real - GET /api/billing/assinaturas/todas` etc.

### 4.6 `frontend/interface/assets/styles/*.css`
- Estilos para cards de planos (responsivos)
- Estilos para modais/seções do fluxo
- Estilos para tabelas do painel super_admin
- Faixa "MODO SIMULAÇÃO"

---

## 5. Backend - Contrato de API (Para Fase Futura)

### 5.1 Endpoints Públicos Novos
| Endpoint | Método | Descrição |
|---|---|---|
| `/api/public/pre-cadastros` | POST | Criar pré-cadastro (`aguardando_pagamento`) + retornar `ref` (UUID) |
| `/api/public/pre-cadastros/{ref}/iniciar-assinatura` | POST | Iniciar assinatura Asaas (recebe `plano`, `ciclo`), retorna `invoiceUrl` |
| `/api/public/pre-cadastros/{ref}/status` | GET | Polling: `aguardando_pagamento` | `pago` | `expirado` | `recusado` + `definirSenhaToken` se pago |

### 5.2 Endpoints Super Admin Novos
| Endpoint | Método | Descrição |
|---|---|---|
| `/api/billing/assinaturas/todas` | GET | Lista paginada de todas assinaturas (empresa, plano, período, valor, status, próximo vencimento) |
| `/api/billing/webhook-log` | GET | **Já existe** (linha 503-530) |

### 5.3 Webhook Asaas (Já Existe - `/api/webhook/asaas`)
- Processa `PAYMENT_RECEIVED`, `SUBSCRIPTION_CREATED`, etc.
- **Ao confirmar pagamento**: Cria empresa + usuário (perfil = cliente atual) + assinatura ativa
- Gera token "Definir Minha Senha" (único, uso único, validade) → envia por e-mail

### 5.4 Correções no `BillingModule.cs` (Listar - Aplicar Só Fase Backend)
- **Semestral**: 160.00 → **159.90**
- **Anual**: 300.00 → **299.90**
- **Mensal**: 27.90 (mantém)
- **Procurar** 49.90/99.90/199.90 em: `*.sql`, seeds, JSON, scripts, banco (tabela `planos`/`assinaturas`)
- **Query DBeaver**: `SELECT * FROM planos; SELECT * FROM assinaturas LIMIT 10;`

---

## 6. Plano de Implementação (Fase 2 - Frontend Only)

### Sprint 1 - Config + API Simulada
1. [ ] Criar `frontend/interface/config/planos.js` (script clássico, `window.PLANOS_CONFIG`)
2. [ ] Criar `frontend/interface/js/billingApi.js` (simulação completa + mocks)
3. [ ] Incluir `planos.js` no `index.html`
4. [ ] Adicionar faixa "MODO SIMULAÇÃO" condicional

### Sprint 2 - Cards no Login + Fluxo Estados 1-4
1. [ ] Adicionar 3 cards responsivos no `#login-container` (usando `PLANOS_CONFIG`)
2. [ ] Atalho "Quero assinar" → `scrollIntoView` cards
3. [ ] Implementar Estado 1: Modal "Dados Cadastrais" (validação CPF/CNPJ própria, termos placeholder TODO)
4. [ ] Implementar Estado 2: "Finalizar Pagamento" - botão redirect `invoiceUrl` (mock)
5. [ ] Implementar Estado 3: "Aguardando Confirmação" - polling 5s + aviso boleto + botão re-verificar
6. [ ] Implementar Estado 4: "Pagamento Confirmado" - botão → "Definir Minha Senha" com token mock
7. [ ] Estados de erro (recusado, expirado, rede)

### Sprint 3 - Painel Super Admin + Correções
1. [ ] Adicionar `nav-billing` no sidebar (só `super_admin`)
2. [ ] Bloquear `navegar('billing')` se não `super_admin`
3. [ ] Tela billing: tabela assinaturas mock + tabela webhook mock + TODOs
4. [ ] **Corrigir `verificarBannerPlano()`**: remover "Ver Planos", nova mensagem suporte/e-mail
5. [ ] **Corrigir bloqueio cadastro paciente**: mesma mensagem, sem link

### Sprint 4 - Validação (Fase C)
1. [ ] `node --check frontend/interface/app.js frontend/interface/build.js`
2. [ ] `dotnet build` + `dotnet test` (todos passando - sem mudanças backend)
3. [ ] `git grep` confirmação:
   - Sem referências a preços antigos (97/297/797, 49.90/99.90/199.90)
   - Sem "Ver Planos" no frontend
4. [ ] Testes manuais documentados:
   - Login super_admin vs nutricionista (menu billing visível/invisível)
   - Cards login desktop + mobile (responsivo)
   - Fluxo simulado completo até "Pagamento confirmado" → "Definir Minha Senha"
5. [ ] **Commit** na branch `feature/planos-checkout` - **APENAS arquivos criados/alterados** - **SEM PUSH**

---

## 7. Arquivos a Criar/Alterar (Frontend Only)

### Novos
- `frontend/interface/config/planos.js`
- `frontend/interface/js/billingApi.js`

### Alterados
- `frontend/interface/index.html` (incluir planos.js, cards, modais fluxo, faixa simulação)
- `frontend/interface/app.js` (fluxo contratação, banner/bloqueio nova msg, validação CPF/CNPJ)
- `frontend/interface/billing.html` (painel super_admin só leitura + mocks + TODOs)
- `frontend/interface/assets/styles/*.css` (estilos novos)

### NÃO TOCAR (Fase Backend Futura)
- `servico-csharp/BillingModule.cs` - apenas LISTAR correções de preço
- `servico-csharp/Program.cs` - não alterar agora

---

## 8. Verificações Pendentes (Antes de Codificar)

1. **Vite?** Verificar se projeto usa Vite (`package.json` tem `vite`?) → define se `MODO_SIMULACAO` pode ser `import.meta.env.VITE_` ou deve ficar constante no `planos.js`.
2. **404 vs 200** no `/api/billing/assinatura`: Procurar `NotFound`/`404` no `BillingModule.cs` e explicar diferença (super_admin sem empresa_id?).
3. **Termos/LGPD URLs**: Verificar se existem documentos existentes no repo (`docs/termos.md`, `docs/lgpd.md`, etc.) ou placeholders.
4. **Perfil cliente atual**: Confirmar qual perfil têm os donos de clínica hoje (`profissional`? `admin`? `nutricionista`?).

---

## Próximo Passo

**Iniciar Sprint 1** - Criar `config/planos.js` e `js/billingApi.js`, depois integrar no `index.html` e `app.js`.

Vou começar agora.