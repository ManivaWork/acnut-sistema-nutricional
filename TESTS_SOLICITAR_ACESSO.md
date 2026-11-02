# Testes: Fluxo "Solicitar Acesso"

## 1. Solicitação Pública (POST /api/acesso/solicitar)

### 1.1 Sucesso - Dados válidos
```bash
curl -X POST http://localhost:5000/api/acesso/solicitar \
  -H "Content-Type: application/json" \
  -d '{
    "nome": "Dr. João Silva",
    "email": "joao@clinica.com",
    "telefone": "11999999999",
    "registroProfissional": "CRN-12345",
    "clinicaNome": "Clínica Nutrição Total",
    "mensagem": "Gostaria de usar o sistema"
  }'
```
**Esperado:** 200 OK com `{ "mensagem": "Se os dados estiverem corretos, sua solicitação será analisada em até 2 dias úteis." }`

### 1.2 Anti-enumeração - Email já tem solicitação pendente
```bash
# Primeira solicitação
curl -X POST http://localhost:5000/api/acesso/solicitar \
  -H "Content-Type: application/json" \
  -d '{"nome":"João","email":"teste@teste.com","telefone":"11999999999","registroProfissional":"CRN-1","clinicaNome":"Clínica A"}'

# Segunda solicitação (mesmo email)
curl -X POST http://localhost:5000/api/acesso/solicitar \
  -H "Content-Type: application/json" \
  -d '{"nome":"João 2","email":"teste@teste.com","telefone":"11999999999","registroProfissional":"CRN-2","clinicaNome":"Clínica B"}'
```
**Esperado:** Ambas retornam 200 OK com mesma mensagem genérica (não revela se email já existe)

### 1.3 Validação - Campos obrigatórios faltando
```bash
curl -X POST http://localhost:5000/api/acesso/solicitar \
  -H "Content-Type: application/json" \
  -d '{"nome":"João","email":"invalido"}'
```
**Esperado:** 400 Bad Request com mensagem de erro apropriada

### 1.4 Rate Limit - 5 req/hora por IP
```bash
for i in {1..6}; do
  curl -X POST http://localhost:5000/api/acesso/solicitar \
    -H "Content-Type: application/json" \
    -d "{\"nome\":\"Teste $i\",\"email\":\"teste$i@teste.com\",\"telefone\":\"11999999999\",\"registroProfissional\":\"CRN-$i\",\"clinicaNome\":\"Clínica\"}"
  echo "---"
done
```
**Esperado:** Primeiras 5 = 200 OK, 6ª = 429 Too Many Requests

### 1.5 Rate Limit - Headers X-Forwarded-For
```bash
# Simular proxy (Render)
curl -X POST http://localhost:5000/api/acesso/solicitar \
  -H "Content-Type: application/json" \
  -H "X-Forwarded-For: 203.0.113.195" \
  -d '{"nome":"Teste","email":"teste@proxy.com","telefone":"11999999999","registroProfissional":"CRN-1","clinicaNome":"Clínica"}'
```
**Esperado:** Rate limit aplicado por IP real (203.0.113.195), não IP do proxy

---

## 2. Listar Solicitações (GET /api/acessos) - super_admin

### 2.1 Lista todas (paginado)
```bash
curl -H "Authorization: Bearer <super_admin_token>" \
  "http://localhost:5000/api/acessos?page=1&pageSize=20"
```
**Esperado:** 200 OK com `{ total, page, pageSize, totalPages, data: [...] }`

### 2.2 Filtro por status
```bash
curl -H "Authorization: Bearer <super_admin_token>" \
  "http://localhost:5000/api/acessos?status=pendente"
curl -H "Authorization: Bearer <super_admin_token>" \
  "http://localhost:5000/api/acessos?status=aprovado"
curl -H "Authorization: Bearer <super_admin_token>" \
  "http://localhost:5000/api/acessos?status=recusado"
curl -H "Authorization: Bearer <super_admin_token>" \
  "http://localhost:5000/api/acessos?status=excluido"
```
**Esperado:** 200 OK com apenas solicitações do status filtrado

### 2.3 Acesso negado para não super_admin
```bash
curl -H "Authorization: Bearer <admin_token>" \
  "http://localhost:5000/api/acessos"
```
**Esperado:** 403 Forbid

---

## 3. Aprovar Solicitação (POST /api/acessos/{id}/aprovar) - super_admin

### 3.1 Aprovação bem-sucedida
```bash
curl -X POST http://localhost:5000/api/acessos/1/aprovar \
  -H "Authorization: Bearer <super_admin_token>" \
  -H "Content-Type: application/json" \
  -d '{"nomeProfissional": "Dra. Maria Santos"}'  # opcional
```
**Esperado:** 200 OK com:
- `mensagem`: "Solicitação aprovada! Empresa, usuário e assinatura 'sem_plano' criados. E-mail com link para definir senha enviado."
- `empresaId`: ID da nova empresa criada
- `usuarioId`: ID do usuário profissional criado

**Verificar no banco:**
- Nova empresa em `empresas` com nome = clinicaNome (ou nome do profissional)
- Novo usuário em `usuarios` com perfil='profissional', ativo=true, empresa_id = nova empresa
- Assinatura em `assinaturas` com status='sem_plano', plano='basico'
- Token em `tokens_redefinicao_senha` com expira_em = agora + 48h
- Solicitação atualizada para status='aprovado' com empresa_id e usuario_id preenchidos
- Auditoria em `auditoria` com acao='aprovacao_acesso'

### 3.2 Aprovação de solicitação inexistente/já processada
```bash
curl -X POST http://localhost:5000/api/acessos/999/aprovar \
  -H "Authorization: Bearer <super_admin_token>"
```
**Esperado:** 404 Not Found

### 3.3 Isolamento - SEMPRE cria nova empresa
```bash
# 1. Criar empresa existente "Clínica A"
# 2. Solicitar acesso com clinicaNome="Clínica A"
# 3. Aprovar
# Verificar: Nova empresa criada (não reutilizou a existente)
```
**Esperado:** Nova empresa com ID diferente da existente

---

## 4. Recusar Solicitação (POST /api/acessos/{id}/recusar) - super_admin

### 4.1 Recusa bem-sucedida
```bash
curl -X POST http://localhost:5000/api/acessos/1/recusar \
  -H "Authorization: Bearer <super_admin_token>"
```
**Esperado:** 200 OK com `{ "mensagem": "Solicitação recusada." }`

**Verificar no banco:**
- Solicitação status='recusado'
- Auditoria com acao='recusa_acesso'

### 4.2 Recusa de solicitação já processada
```bash
curl -X POST http://localhost:5000/api/acessos/1/recusar \
  -H "Authorization: Bearer <super_admin_token>"
```
**Esperado:** 404 Not Found

---

## 5. Excluir Solicitação (DELETE /api/acessos/{id}) - super_admin

### 5.1 Excluir pendente/recusado (Hard Delete)
```bash
curl -X DELETE http://localhost:5000/api/acessos/1 \
  -H "Authorization: Bearer <super_admin_token>"
```
**Esperado:** 200 OK com `{ "mensagem": "Solicitação excluída permanentemente." }`

**Verificar:** Linha removida de `solicitacoes_acesso`, auditoria com acao='exclusao_acesso'

### 5.2 Excluir aprovado (Soft Delete - desativa usuário)
```bash
# Aprovar uma solicitação primeiro (cria empresa + usuário)
# Depois excluir
curl -X DELETE http://localhost:5000/api/acessos/1 \
  -H "Authorization: Bearer <super_admin_token>"
```
**Esperado:** 200 OK com `{ "mensagem": "Acesso desativado (usuário desativado, empresa mantida)." }`

**Verificar no banco:**
- `usuarios.ativo = false` para o usuário
- `solicitacoes_acesso.status = 'excluido'`
- Empresa NÃO excluída (mantida com dados)
- Auditoria com acao='exclusao_acesso' e detalhes do soft delete

---

## 6. Definir Senha (Token 48h)

### 6.1 Validar token (GET /api/definir-senha/{token})
```bash
curl "http://localhost:5000/api/definir-senha/abc123tokenvalido32chars"
```
**Esperado:** 200 OK com `{ "valido": true, "email": "joao@clinica.com", "nome": "Dr. João Silva" }`

### 6.2 Token inválido/expirado
```bash
curl "http://localhost:5000/api/definir-senha/tokeninvalido"
```
**Esperado:** 404 Not Found com `{ "mensagem": "Token inválido ou expirado.", "valido": false }`

### 6.3 Definir senha (POST /api/definir-senha/{token})
```bash
curl -X POST http://localhost:5000/api/definir-senha/abc123tokenvalido32chars \
  -H "Content-Type: application/json" \
  -d '{"senha": "MinhaSenh@123", "confirmarSenha": "MinhaSenh@123"}'
```
**Esperado:** 200 OK com `{ "mensagem": "Senha definida com sucesso! Faça login para acessar." }`

**Verificar:**
- `usuarios.senha_hash` atualizado
- `tokens_redefinicao_senha.usado_em` preenchido
- Token não pode ser usado novamente

### 6.4 Senhas não conferem
```bash
curl -X POST http://localhost:5000/api/definir-senha/abc123tokenvalido32chars \
  -H "Content-Type: application/json" \
  -d '{"senha": "Senha123", "confirmarSenha": "Senha456"}'
```
**Esperado:** 400 Bad Request

### 6.5 Usuário desativado não pode definir senha
```bash
# Desativar usuário (soft delete)
# Tentar definir senha
```
**Esperado:** 400 Bad Request com `"Usuário desativado. Contate o administrador."`

---

## 7. Enforcement de Plano (Limites)

### 7.1 Profissional sem plano - limite 3 pacientes
```bash
# Login como profissional recém-aprovado (assinatura status='sem_plano')
# Criar 3 pacientes - OK
for i in {1..3}; do
  curl -X POST http://localhost:5000/api/pacientes \
    -H "Authorization: Bearer <profissional_token>" \
    -H "Content-Type: application/json" \
    -d "{\"nome\":\"Paciente $i\",\"email\":\"p$i@teste.com\",\"data_nascimento\":\"1990-01-01\",\"sexo\":\"F\"}"
done

# 4º paciente - DEVE FALHAR
curl -X POST http://localhost:5000/api/pacientes \
  -H "Authorization: Bearer <profissional_token>" \
  -H "Content-Type: application/json" \
  -d '{"nome":"Paciente 4","email":"p4@teste.com","data_nascimento":"1990-01-01","sexo":"F"}'
```
**Esperado:** 403 Forbidden com:
```json
{
  "mensagem": "Limite de pacientes atingido (3/3). Assine um plano para adicionar mais.",
  "codigo": "plano_necessario",
  "limite": 3,
  "atual": 3
}
```

### 7.2 Profissional com plano ativo - sem limite (conforme plano)
```bash
# Ativar assinatura (via webhook Asaas ou manual)
# Criar pacientes além do limite do plano básico (50)
```
**Esperado:** Permite criar até max_pacientes do plano

### 7.3 PDF Export sem plano ativo
```bash
# Tentar exportar PDF (endpoint de exportação)
curl -X GET http://localhost:5000/api/pacientes/1/pdf \
  -H "Authorization: Bearer <profissional_sem_plano_token>"
```
**Esperado:** 403 Forbidden com `codigo: "plano_necessario"`

---

## 8. Login - Usuário Desativado

### 8.1 Login com usuário desativado (soft delete)
```bash
# Desativar usuário via DELETE /api/acessos/{id} (aprovado)
# Tentar login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"joao@clinica.com","senha":"senha123"}'
```
**Esperado:** 401 Unauthorized com `"E-mail ou senha incorretos."` (não revela que está desativado)

---

## 9. Auditoria

### 9.1 Verificar logs de auditoria para cada ação
```bash
# Como super_admin
curl -H "Authorization: Bearer <super_admin_token>" \
  "http://localhost:5000/api/auditoria?acao=aprovacao_acesso"
curl -H "Authorization: Bearer <super_admin_token>" \
  "http://localhost:5000/api/auditoria?acao=recusa_acesso"
curl -H "Authorization: Bearer <super_admin_token>" \
  "http://localhost:5000/api/auditoria?acao=exclusao_acesso"
```
**Esperado:** Registros com empresa_id, usuario_id, acao, rota, registro_id, detalhes

---

## 10. Frontend (Manual)

### 10.1 Login Page - Link "Solicitar acesso"
- Abrir página de login
- Verificar link "Solicitar acesso" abaixo de "Esqueci minha senha"
- Clicar abre modal com campos: nome, email, telefone, registro profissional, clínica, mensagem
- Submit chama POST /api/acesso/solicitar
- Toast de sucesso/erro

### 10.2 Sidebar - Menu "Acessos" (super_admin)
- Login como super_admin
- Verificar item "Acessos" no menu lateral (substituiu "Convites")
- Clicar abre tela com 3 tabs: Pendentes | Aprovados | Recusados
- Cada linha mostra: nome, email, clínica, data, ações
- Botões: Aprovar / Recusar / Excluir
- Toast de confirmação

### 10.3 Banner "Assine um plano" para profissional sem plano
- Login como profissional recém-aprovado (sem_plano)
- Verificar banner persistente no topo: "Assine um plano para desbloquear todos os recursos"
- Link para página de billing
- Botões de PDF export desabilitados/ocultos
- Contador de pacientes mostra "3/3 - Limite atingido"

---

## Resumo de Códigos de Resposta

| Cenário | Status | Código |
|---------|--------|--------|
| Sucesso genérico (anti-enumeração) | 200 | - |
| Rate limit excedido | 429 | - |
| Validação falhou | 400 | - |
| Não autenticado | 401 | - |
| Não autorizado (perfil) | 403 | - |
| Não encontrado | 404 | - |
| Plano necessário (limite) | 403 | plano_necessario |
| Conflito (duplicado) | 409 | - |
| Erro interno | 500 | - |