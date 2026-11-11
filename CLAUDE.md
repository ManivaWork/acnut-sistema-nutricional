# AC Hub / ACNUTRI
Stack: API C# (servico-csharp), servico Python (servico-python/main.py), Postgres smart_nutri_db, front HTML/JS (frontend/interface).
Estado: multi-tenant APLICADO no banco (empresa_id NOT NULL nas 14 tabelas clinicas). Program.cs ja grava empresa_id. Tabelas convites e convites_paciente JÁ existem no banco (achubdb e local). NAO rode migracao_multitenant.sql de novo.
Regras: uma tarefa por vez; mostre o plano antes de alterar; nunca execute SQL no banco (so gere .sql idempotente, em transacao, sem DROP/DELETE, terminando em ROLLBACK); sem commit sem eu pedir; leia so os arquivos necessarios.
CI: antes de todo commit que alterar o frontend, rode `node --check frontend/interface/app.js frontend/interface/build.js`.