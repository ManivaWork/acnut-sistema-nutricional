# Mapeamento de Dados Pessoais (LGPD)

| Tabela | Coluna | Tipo LGPD | Finalidade | Papéis que acessam | Armazenamento | Exclusão |
|--------|--------|-----------|------------|-------------------|---------------|----------|
| pacientes | nome, email, telefone | Comum | Identificação e contato | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente (CASCADE) |
| pacientes | cpf | Comum | Login paciente, identificação única | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente |
| pacientes | data_nascimento, sexo | Comum | Cálculo TMB/GET, anamnese | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente |
| pacientes | status | Comum | Controle de ativo/inativo | Profissional, Admin | Banco (Postgres) | Ao excluir paciente |
| usuarios | nome, email | Comum | Autenticação equipe | Admin, Super Admin | Banco (Postgres) | Ao excluir usuário |
| usuarios | senha_hash | Sensível (credencial) | Autenticação | Sistema (hash) | Banco (Postgres) | Ao excluir usuário |
| convites | email | Comum | Convidar profissional | Admin, Sistema (e-mail) | Banco (Postgres) | 7 dias (expira) ou ao usar |
| convites | token | Sensível (credencial) | Validação convite | Sistema | Banco (Postgres) | 7 dias ou ao usar |
| anamneses | historico_medico, alergias, intolerancias, rotina_diaria, historico_familiar | **Sensível (saúde)** | Anamnese clínica | Profissional, Admin, Paciente (leitura) | Banco (Postgres) | Ao excluir paciente |
| exames_paciente | titulo_exame, data_exame, observacao, enviado_por | **Sensível (saúde)** | Exames laboratoriais/imagem | Profissional, Admin, Paciente | Banco + arquivos (BYTEA) | Ao excluir paciente/exame |
| arquivos_paciente | nome, mime, tamanho, dados | **Sensível (saúde)** | Fotos evolução, exames, avaliações | Profissional, Admin, Paciente | Banco (BYTEA) | Ao excluir origem ou paciente |
| evolucao_registros | peso_kg, %gordura, circunferências | **Sensível (saúde)** | Evolução antropométrica | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente |
| avaliacoes_fisicas | peso, altura, %gordura, massa_magra, TMB, GET | **Sensível (saúde)** | Avaliação física completa | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente |
| checkins_semanais | peso_atual_kg, adesao, observacoes_paciente | **Sensível (saúde)** | Acompanhamento semanal | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente |
| metas_paciente | titulo, descricao, tipo, prazo, status | Comum | Metas comportamentais | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente |
| metas_acompanhamento | resultado, observacao | Comum | Registro de aderência | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir meta/paciente |
| observacoes_paciente | tipo, data_ref, texto | Comum/Sensível* | Obs. clínicas/semanais | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente |
| planos_alimentares/itens | nome_plano, alimentos, quantidades | Comum | Prescrição nutricional | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente |
| orientacoes_nutricionais | texto | Comum | Orientações do nutricionista | Profissional, Admin, Paciente | Banco (Postgres) | Ao excluir paciente |
| acessos_paciente | cpf_tentado, ip, sucesso | Comum | Auditoria login paciente | Admin, Super Admin | Banco (Postgres) | **Não há rotina de exclusão** |
| usuario_modulos | nivel | Comum | Controle de acesso módulos | Admin, Super Admin | Banco (Postgres) | Ao excluir usuário |
| empresas | nome | Comum | Identificação tenant | Super Admin | Banco (Postgres) | Ao excluir empresa (CASCADE) |

*Observações "semana" podem conter dados de saúde relatados pelo paciente.

## Pontos de Risco

1. **CPF e IP em URLs/logs**: `acessos_paciente.cpf_tentado` e `ip` guardados em texto plano; podem vazar em logs de erro ou URL de redirecionamento.
2. **Arquivos em BYTEA**: `arquivos_paciente.dados` armazena binários no banco — backup/restore expõe imagens/PDFs sensíveis; considerar S3 com criptografia.
3. **Ausência de retenção/exclusão automática**: `acessos_paciente`, `convites` (usados), logs de auditoria não têm job de limpeza (LGPD Art. 15).
4. **Token de convite em e-mail**: `convites.token` enviado por e-mail não criptografado — risco se e-mail interceptado.
5. **Dados de saúde em snapshots JSON**: `planos_versoes.snapshot` serializa anamnese/exames — cópias históricas persistem mesmo após exclusão do paciente se não cascatarem corretamente.
6. **Senha em texto plano no cadastro**: `usuarios.senha_hash` usa BCrypt (ok), mas endpoint de criação pode logar request body se não filtrado.