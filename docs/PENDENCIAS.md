# PENDÊNCIAS ACNUTRI — Reavaliação (2026-11-11)

> **Regra**: este arquivo é a fonte única de verdade. Atualize a cada commit que alterar o estado de algum item. Não gere listas novas em conversas.

---

## Legenda
- **FEITO** — commit hash onde foi resolvido
- **ALARME FALSO** — item não era real (evidência)
- **PENDENTE** — ainda aberto; menor passo para resolver

---

## 1. Multi-tenant / Isolamento

| # | Item | Status | Evidência / Próximo passo |
|---|------|--------|---------------------------|
| 1 | `empresa_id` NOT NULL nas 14 tabelas clínicas | **FEITO** `dfef4df` | `schema_inicial.sql` já tem `empresa_id` NOT NULL + FK em `pacientes`, `anamneses`, `checkins_semanais`, `orientacoes_nutricionais`, `metas_acompanhamento`, `metas_paciente`, `planos_versoes`, `evolucao_registros`, `exames_paciente`, `observacoes_paciente`, `avaliacoes_fisicas`, `arquivos_paciente`, `consentimentos`, `acessos_paciente`. `Program.cs` grava `empresa_id` via `Auth.Empresa(user)`. |
| 2 | Validação de tenant em POST /api/avaliacoes | **FEITO** `24dd60e` | `Program.cs:791` chama `TenantValidation.ValidarPacienteEmpresaAsync` antes de chamar Python. Testes em `IsolamentoMultiTenantTests.cs:391-433`. |
| 3 | Validação de tenant em GET /api/avaliacoes/{id} | **PENDENTE** | Endpoint **não existe** no backend (ver item 22 abaixo). |
| 4 | Isolamento em convites / convites_paciente | **FEITO** `dfef4df` | Tabelas `convites` e `convites_paciente` já existem no banco (`migration_convites.sql`). Login e convite de equipe corrigidos em `dfef4df`. |
| 5 | Testes de isolamento multi-tenant | **FEITO** `0ee2a60` | `IsolamentoMultiTenantTests.cs` tem 193 testes cobrindo checkins, orientações, metas, planos, evolução, exames, observações, anamnese, acessos, histórico, avaliações, portal do paciente, limites de plano. |

---

## 2. Autenticação / Autorização

| # | Item | Status | Evidência / Próximo passo |
|---|------|--------|---------------------------|
| 6 | Rate limiting login (IP + identificador) | **FEITO** `99cef9d` | `Program.cs:85-118` política `ip-auth` (30 req/15min por IP) + bloqueio por hash(identificador+IP) no banco. Reset em login bem-sucedido (`660f607`). |
| 7 | Two-tier rate limiting com reset por sucesso | **FEITO** `660f607` | Mesmo commit acima. |
| 8 | Perfil `super_admin` bypass de limites | **FEITO** `8d3408d` | `Program.cs` valida `perfil == "super_admin"` antes de checar limites. Testes `IsolamentoMultiTenantTests.cs:1201-1234`. |
| 9 | Flag `bypass_limites` na assinatura | **FEITO** `8d3408d` | Coluna `bypass_limites` em `assinaturas`. Testes `IsolamentoMultiTenantTests.cs:1240-1292`. |
| 10 | Plano gratuito (`sem_plano`) = 5 pacientes | **FEITO** `f10b0e5` | `BillingModule.cs` catálogo inclui `sem_plano` (5 pacientes, 1 profissional). Migrações `migration_sem_plano_5_pacientes.sql`, `migration_add_limites_to_assinaturas.sql`. Testes `BillingModuleTests.cs:28-44`, `PlanoEnforcementTests.cs:214-271`. |

---

## 3. Billing / Assinaturas / Asaas

| # | Item | Status | Evidência / Próximo passo |
|---|------|--------|---------------------------|
| 11 | Preços mensal 27,90 / semestral 160 / anual 300 | **FEITO** `671e5c7` | `BillingModule.cs` valores atualizados. |
| 12 | Limites em snake_case na escrita e upgrade | **FEITO** `c0a08ee` | `PlanoLimites.ToJson/FromJson` serializa `max_pacientes`, `max_profissionais`, `storage_mb`, `max_versoes_plano`. Testes `BillingModuleTests.cs:79-163`. |
| 13 | Toggle ilimitado + excluir empresa no painel admin | **FEITO** `504d6c4` | `Program.cs` endpoints admin para toggle `bypass_limites` e DELETE empresa. |
| 14 | Webhook Asaas funcional | **FEITO** `dfef4df` | `BillingModule.cs` mapeia eventos Asaas → atualiza `assinaturas`. `Program.cs:198` mapeia endpoints. |

---

## 4. Frontend / Portal do Paciente

| # | Item | Status | Evidência / Próximo passo |
|---|------|--------|---------------------------|
| 15 | SyntaxError em app.js | **FEITO** `5b4c84b` | Corrigido. CI roda `node --check frontend/interface/app.js frontend/interface/build.js` (`1ac4461`). |
| 16 | Portal do paciente: consentimento destrava abas e anamnese | **FEITO** `3fb268b` | `app.js` fluxo de consentimento → libera abas. |
| 17 | Anamnese em página separada (full-screen) | **FEITO** `14ab0a9` | `app.js` navegação para `sec-prontuario` com aba `anamnese`. |
| 18 | Remove dashboard e corrige abas do portal | **FEITO** `ff8fffb` | `app.js` refatorado. |
| 19 | **Modal de consentimento aparecendo para a equipe (403 em /api/paciente/consentimentos)** | **FEITO** `c33baae` | `app.js:4663-4675` `verificarConsentimento()` agora lê `bybar_user.tipo` do `sessionStorage` e retorna `true` imediatamente para `tipo !== 'paciente'`. Equipe vai direto ao painel sem chamar `/api/paciente/consentimentos`. |
| 20 | **"Carregando..." infinito no portal do paciente** | **PENDENTE** | **Problema novo**: Em `app.js`, `carregarProntuarioCompleto()` ou `navegar('prontuario')` pode falhar silenciosamente. `restaurarProntuarioAtivo()` (linha 520) chama `carregarProntuarioCompleto()` se `id` mudou, mas não há tratamento de erro visível. **Menor passo**: Adicionar `catch` com toast em `carregarProntuarioCompleto()` e log no console; verificar se `pacienteId` está definido antes de chamar. |
| 21 | **400 em POST /api/avaliacoes** | **PENDENTE** | **Problema novo**: `Program.cs:786-903` valida tenant (linha 791) e chama Python `/calcular-plano`. Retorna 400 se Python retorna 4xx (linha 865). **Menor passo**: Verificar payload enviado pelo front (`app.js:583-595` `gerarAvaliacao`) — campos `percentualGordura` e `massaMagraKg` podem vir `NaN` se inputs vazios (`|| 0` só protege string vazia, não `parseFloat("")` → `NaN`). Corrigir: `parseFloat(...) || 0`. |
| 22 | **GET /api/avaliacoes/{id} inexistente no backend** | **PENDENTE** | **Problema novo**: Não há `app.MapGet("/api/avaliacoes/{id}")` em `Program.cs`. Front espera buscar avaliação individual (ex.: `abrirAnamneseProntuario` ou histórico). **Menor passo**: Adicionar endpoint `GET /api/avaliacoes/{id}` com validação de tenant (copiar padrão de `/api/checkins/{id}` linha ~1100). |
| 23 | **Peso do repositório (tamanho excessivo)** | **PENDENTE** | **Problema novo**: `servico-csharp/obj/`, `bin/`, `.venv/`, `frontend/interface/node_modules/` não estão no `.gitignore` ou estão commitados. **Menor passo**: Adicionar ao `.gitignore`: `**/obj/`, `**/bin/`, `.venv/`, `frontend/interface/node_modules/`, `*.log`. Rodar `git rm -r --cached` nas pastas e commit. |

---

## 5. Testes / CI

| # | Item | Status | Evidência / Próximo passo |
|---|------|--------|---------------------------|
| 24 | CI com validação sintaxe frontend + testes .NET | **FEITO** `1ac4461` | `.github/workflows/ci.yml` roda `node --check` e `dotnet test`. |
| 25 | 193 testes passando | **FEITO** `0ee2a60` | `IsolamentoMultiTenantTests` (≈150), `AnamneseApiTests` (≈20), `BillingModuleTests` (≈15), `CalculosNutricionaisTests` (≈10), `EmailServiceTests`, `UnitTest1`. |

---

## 6. LGPD / Consentimentos

| # | Item | Status | Evidência / Próximo passo |
|---|------|--------|---------------------------|
| 26 | Tabela `consentimentos` com unique constraint | **FEITO** | `migracao_lgpd_consentimentos.sql`, `migracao_consentimentos_unique.sql`. Upsert em `Program.cs:5180-5183` (`ON CONFLICT`). |
| 27 | Endpoints paciente: POST/GET `/api/paciente/consentimentos` | **FEITO** | `Program.cs:5160-5223`. |
| 28 | Exportação dados paciente (Art. 18 LGPD) | **FEITO** | `Program.cs:5238+` `GET /api/paciente/meus-dados`. |

---

## 7. Python Service

| # | Item | Status | Evidência / Próximo passo |
|---|------|--------|---------------------------|
| 29 | `/calculo/tmb` e `/calcular-plano` funcionando | **FEITO** | `servico-python/main.py` + testes `test_api.py` (16 testes). |
| 30 | Timeout 60s no HttpClient dedicado | **FEITO** | `Program.cs:66-69` `AddHttpClient("Python", c => c.Timeout = TimeSpan.FromSeconds(60))`. |
| 31 | Tratamento de erro 503/400 do Python no C# | **FEITO** `24dd60e` | `Program.cs:807-870` mapeia status Python → 503 (indisponível) ou 400 (dados inválidos). |

---

## Resumo por categoria

| Categoria | Feito | Alarme Falso | Pendente |
|-----------|-------|--------------|----------|
| Multi-tenant / Isolamento | 4 | 0 | 1 |
| Auth / Autorização | 5 | 0 | 0 |
| Billing / Assinaturas | 4 | 0 | 0 |
| Frontend / Portal | 5 | 0 | **3** |
| Testes / CI | 2 | 0 | 0 |
| LGPD | 3 | 0 | 0 |
| Python Service | 3 | 0 | 0 |
| **Total** | **26** | **0** | **4** |

---

## Próximos passos prioritários (ordem sugerida)

1. **P22** — `GET /api/avaliacoes/{id}` (backend) — desbloqueia histórico/prontuário
2. **P21** — Fix `parseFloat` NaN em `gerarAvaliacao` (frontend) — resolve 400
3. **P20** — Try/catch em `carregarProntuarioCompleto` (frontend) — "Carregando..." infinito
4. **P23** — `.gitignore` + limpeza repo — peso do repositório

---

## Histórico de atualizações deste arquivo

| Data | Commit | Alteração |
|------|--------|-----------|
| 2026-11-11 | `c33baae` | P19 → FEITO: consent modal só para pacientes (guarda `bybar_user.tipo` em `verificarConsentimento`) |
| 2026-11-11 | — | Criação inicial com reavaliação completa (25 feitos, 5 pendentes) |