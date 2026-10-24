#!/usr/bin/env python3
"""
importar_alimentos_db.py - Importa alimentos.json para PostgreSQL (tabela alimentos)

Uso:
    python importar_alimentos_db.py

Requer variáveis de ambiente ou appsettings para conexão com o banco.
"""

import json
import os
import sys
from pathlib import Path

# Tenta ler connection string do appsettings do C# ou variável de ambiente
def get_conn_string():
    # 1. Variável de ambiente
    if conn := os.getenv("POSTGRES_CONN"):
        return conn

    # 2. appsettings.json do projeto C#
    appsettings_path = Path(__file__).parent.parent / "servico-csharp" / "appsettings.json"
    if appsettings_path.exists():
        import json as json_mod
        with open(appsettings_path, "r", encoding="utf-8") as f:
            config = json_mod.load(f)
            if conn := config.get("ConnectionStrings", {}).get("Default"):
                return conn

    # 3. user-secrets (não acessível facilmente via script)
    raise RuntimeError(
        "Connection string não encontrada. Defina POSTGRES_CONN ou configure appsettings.json"
    )

try:
    import psycopg2
except ImportError:
    print("Instale psycopg2: pip install psycopg2-binary")
    sys.exit(1)

JSON_PATH = Path(__file__).parent.parent / "database" / "dados" / "alimentos.json"

def importar():
    if not JSON_PATH.exists():
        print(f"Arquivo não encontrado: {JSON_PATH}")
        sys.exit(1)

    with open(JSON_PATH, "r", encoding="utf-8") as f:
        alimentos = json.load(f)

    print(f"Carregados {len(alimentos)} alimentos do JSON")

    conn_str = get_conn_string()
    print(f"Conectando ao banco...")

    with psycopg2.connect(conn_str) as conn:
        with conn.cursor() as cur:
            # Verifica se tabela existe
            cur.execute("""
                SELECT EXISTS (
                    SELECT 1 FROM information_schema.tables
                    WHERE table_name = 'alimentos'
                )
            """)
            if not cur.fetchone()[0]:
                print("Tabela 'alimentos' não existe. Criando...")
                cur.execute("""
                    CREATE TABLE alimentos (
                        id BIGSERIAL PRIMARY KEY,
                        nome VARCHAR(255) NOT NULL,
                        categoria VARCHAR(100),
                        calorias_100g NUMERIC(10,2),
                        proteinas_100g NUMERIC(10,2),
                        carboidratos_100g NUMERIC(10,2),
                        gorduras_100g NUMERIC(10,2),
                        fibras_100g NUMERIC(10,2) DEFAULT 0
                    )
                """)
                conn.commit()
                print("Tabela criada.")

            # UPSERT - atualiza se existe, insere se não existe (evita TRUNCATE com FK)
            # Usa 0 para macros faltantes (tabela tem NOT NULL; Python service usa JSON que tem None)
            inserted = 0
            for a in alimentos:
                def nz(v):
                    return v if v is not None else 0

                cur.execute("""
                    INSERT INTO alimentos
                    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
                    ON CONFLICT (id) DO UPDATE SET
                        nome = EXCLUDED.nome,
                        categoria = EXCLUDED.categoria,
                        calorias_100g = EXCLUDED.calorias_100g,
                        proteinas_100g = EXCLUDED.proteinas_100g,
                        carboidratos_100g = EXCLUDED.carboidratos_100g,
                        gorduras_100g = EXCLUDED.gorduras_100g,
                        fibras_100g = EXCLUDED.fibras_100g
                """, (
                    a["id"],
                    a["nome"],
                    a.get("categoria", ""),
                    nz(a.get("kcal_100g")),
                    nz(a.get("proteina_100g")),
                    nz(a.get("carbo_100g")),
                    nz(a.get("gordura_100g")),
                    nz(a.get("fibra_100g", 0))
                ))
                if cur.rowcount > 0:
                    inserted += 1

            conn.commit()
            print(f"Processados {inserted} alimentos (insert/update)")

            # Verificação rápida
            cur.execute("SELECT COUNT(*) FROM alimentos")
            count = cur.fetchone()[0]
            print(f"Total na tabela: {count}")

if __name__ == "__main__":
    importar()