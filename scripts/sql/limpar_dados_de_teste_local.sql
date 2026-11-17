-- Script de limpeza de dados de teste do banco LOCAL
-- EXECUTAR MANUALMENTE APENAS NO BANCO DE DESENVOLVIMENTO/LOCAL
-- NÃO EXECUTAR EM PRODUÇÃO!
-- Este script é ignorado pelo Git via .git/info/exclude

-- Ordem de exclusão: filhos antes dos pais (respeitando FKs)
-- Limpa apenas dados de teste identificados por padrões nos nomes

BEGIN;

-- 1. Rate limiting tables
DELETE FROM tentativas_login
WHERE ip = '::1'::inet
  AND identificador_hash IN (
    SELECT identificador_hash FROM tentativas_login
    WHERE tipo IN ('equipe', 'paciente')
  );

-- 2. Tabelas clínicas (filhos de pacientes)
DELETE FROM checkins_semanais
WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM orientacoes_nutricionais
WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM metas_acompanhamento
WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM metas_paciente
WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM planos_versoes
WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %'));

DELETE FROM evolucao_registros
WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %'));

DELETE FROM exames_paciente
WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %'));

DELETE FROM observacoes_paciente
WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM avaliacoes_fisicas
WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM anamneses
WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM arquivos_paciente
WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM pacientes
WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

-- 3. Planos (refeicoes e itens)
DELETE FROM plano_itens
WHERE refeicao_id IN (
  SELECT id FROM plano_refeicoes
  WHERE plano_versao_id IN (
    SELECT id FROM planos_versoes
    WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %'))
  )
);

DELETE FROM plano_refeicoes
WHERE plano_versao_id IN (
  SELECT id FROM planos_versoes
  WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %'))
);

-- 4. Acessos e convites
DELETE FROM acessos_paciente
WHERE paciente_id IN (SELECT id FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %'))

DELETE FROM solicitacoes_acesso
WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM convites_paciente
WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

-- 5. Usuários e assinaturas
DELETE FROM usuarios
WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

DELETE FROM assinaturas
WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %');

-- 6. Empresas de teste (por último - pai)
DELETE FROM empresas
WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %';

-- Verificação final
SELECT 'Empresas RL restantes:' AS check_type, COUNT(*) AS count FROM empresas WHERE nome LIKE 'Empresa RL %'
UNION ALL
SELECT 'Empresas Teste restantes:', COUNT(*) FROM empresas WHERE nome LIKE 'Empresa Teste %'
UNION ALL
SELECT 'Pacientes restantes:', COUNT(*) FROM pacientes WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa RL %' OR nome LIKE 'Empresa Teste %')
UNION ALL
SELECT 'Rate limit tentativas restantes:', COUNT(*) FROM tentativas_login WHERE ip = '::1'::inet;

ROLLBACK;
-- Para confirmar a limpeza, mude ROLLBACK para COMMIT e execute novamente