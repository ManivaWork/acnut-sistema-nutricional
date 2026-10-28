# Política de Privacidade — AC Hub / ACNUTRI

**Versão:** 1.0  
**Data:** 2025-01-15  
**Status:** [REVISAR COM ADVOGADO]

---

## 1. Controlador dos Dados
AC Hub / ACNUTRI — Sistema Clínico de Nutrição  
[REVISAR COM ADVOGADO: Inserir razão social, CNPJ, endereço, DPO, e-mail de contato]

## 2. Dados Coletados
### 2.1 Dados de Identificação (Art. 5º, I, LGPD)
- Nome completo, e-mail, telefone, CPF, data de nascimento, sexo
- Finalidade: cadastro, login (CPF), comunicação, emissão de documentos
- Base legal: execução de contrato (Art. 7º, V) e obrigação legal (Art. 7º, II)

### 2.2 Dados de Saúde — **Sensíveis** (Art. 5º, II, LGPD)
- Anamnese: histórico médico, alergias, intolerâncias, rotina, histórico familiar
- Exames laboratoriais e de imagem (arquivos e metadados)
- Avaliações físicas: peso, altura, % gordura, circunferências, TMB, GET
- Evolução: registros antropométricos, fotos, check-ins semanais
- Planos alimentares, orientações, metas, observações clínicas
- Finalidade: atendimento nutricional, prescrição, acompanhamento clínico
- Base legal: tutela da saúde (Art. 11, II, a) + consentimento específico (Art. 11, I)

### 2.3 Dados Técnicos e de Auditoria
- IP, timestamps de acesso, logs de login (tabela `acessos_paciente`)
- Tokens de convite, consentimentos registrados
- Finalidade: segurança, auditoria, prevenção de fraude
- Base legal: legítimo interesse (Art. 7º, IX) e obrigação legal

## 3. Compartilhamento
- **Equipe da mesma empresa** (multi-tenant): profissionais, admins — acesso aos dados do paciente vinculado à empresa
- **Paciente**: acesso aos próprios dados via área do paciente
- **Terceiros**: não há compartilhamento, exceto por ordem judicial
- [REVISAR COM ADVOGADO: listar subprocessadores, nuvem, backup, e-mail transacional]

## 4. Armazenamento e Segurança
- Banco PostgreSQL criptografado em repouso (TDE) e em trânsito (TLS 1.2+)
- Senhas com BCrypt (cost 12)
- Arquivos (exames, fotos) armazenados como BYTEA no banco
- [REVISAR COM ADVOGADO: política de backup, retenção de logs, criptografia de chaves]

## 5. Retenção e Exclusão
| Dado | Prazo | Base |
|------|-------|------|
| Dados cadastrais e clínicos | Durante o vínculo + 10 anos | Resolução CFN, Art. 7º, II LGPD |
| Logs de acesso (`acessos_paciente`) | 2 anos | Segurança, Art. 7º, IX |
| Consentimentos | Permanente (histórico) | Art. 8º, §1º LGPD |
| Convites não usados | 7 dias (auto-expiram) | — |

[REVISAR COM ADVOGADO: confirmar prazos com legislação setorial (CFN, ANVISA, CFM)]

## 6. Direitos do Titular (Art. 18 LGPD)
- Confirmação, acesso, correção, anonimização, bloqueio, eliminação
- Portabilidade (JSON via área do paciente)
- Revogação do consentimento (exceto onde base legal independente)
- Oposição ao tratamento
- Solicitações: via área do paciente ou e-mail [REVISAR COM ADVOGADO: canal oficial]

## 7. Cookies e Tecnologias Similares
- Apenas cookies essenciais: sessão (JWT em HttpOnly), preferência de sidebar
- Sem analytics de terceiros, sem pixel de marketing
- [REVISAR COM ADVOGADO: se houver Google Analytics, Hotjar, etc.]

## 8. Transferência Internacional
- Não há transferência para fora do Brasil
- [REVISAR COM ADVOGADO: se usar nuvem com datacenter no exterior]

## 9. Alterações nesta Política
- Notificação por e-mail e banner no sistema 30 dias antes
- Versão anterior arquivada para auditoria

## 10. Contato do DPO
[REVISAR COM ADVOGADO: nome, e-mail, telefone do Encarregado]

---

**Ao marcar os checkboxes abaixo, você declara ter lido e compreendido esta Política e consente livremente com o tratamento descrito.**