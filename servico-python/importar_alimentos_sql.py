#!/usr/bin/env python3
"""
importar_alimentos_sql.py - Gera SQL de UPSERT para importar alimentos.json no PostgreSQL
Uso: python importar_alimentos_sql.py
Saída: frontend/database/import_alimentos.sql
"""

import json
from pathlib import Path

JSON_PATH = Path(__file__).parent.parent / "database" / "dados" / "alimentos.json"
OUT_SQL = Path(__file__).parent.parent / "frontend" / "database" / "import_alimentos.sql"

def nz(v):
    """None -> 0 para macros faltantes (tabela tem NOT NULL)"""
    return v if v is not None else 0

def escape_sql(s: str) -> str:
    """Escapa aspas simples para SQL"""
    return s.replace("'", "''")

def gerar_sql():
    if not JSON_PATH.exists():
        print(f"[ERRO] Arquivo não encontrado: {JSON_PATH}")
        print("   Rode antes: python importar_taco.py")
        return

    with open(JSON_PATH, "r", encoding="utf-8") as f:
        alimentos = json.load(f)

    print(f"Carregados {len(alimentos)} alimentos do JSON")

    lines = []
    lines.append("-- =====================================================================")
    lines.append("-- IMPORTAÇÃO TACO → TABELA alimentos (UPSERT idempotente)")
    lines.append(f"-- Gerado automaticamente - {len(alimentos)} alimentos")
    lines.append("-- =====================================================================")
    lines.append("")
    lines.append("BEGIN;")
    lines.append("")

    # Verifica/Cria tabela (idempotente)
    lines.append("-- Garante que a tabela existe")
    lines.append("""
CREATE TABLE IF NOT EXISTS alimentos (
    id BIGSERIAL PRIMARY KEY,
    nome VARCHAR(255) NOT NULL,
    categoria VARCHAR(100),
    calorias_100g NUMERIC(10,2),
    proteinas_100g NUMERIC(10,2),
    carboidratos_100g NUMERIC(10,2),
    gorduras_100g NUMERIC(10,2),
    fibras_100g NUMERIC(10,2) DEFAULT 0
);
""")
    lines.append("")

    inserted = 0
    skipped = 0

    for a in alimentos:
        raw_id = a["id"]
        try:
            food_id = int(str(raw_id).strip().split("\n")[-1])
        except (ValueError, TypeError):
            print(f"  ⚠️  ID inválido '{raw_id}' — pulando '{a.get('nome', 'sem nome')}'")
            skipped += 1
            continue

        nome = escape_sql(a["nome"])
        categoria = escape_sql(a.get("categoria", ""))

        kcal = nz(a.get("kcal_100g"))
        prot = nz(a.get("proteina_100g"))
        carbo = nz(a.get("carbo_100g"))
        gord = nz(a.get("gordura_100g"))
        fibra = nz(a.get("fibra_100g", 0))

        sql = f"""INSERT INTO alimentos
    (id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g)
VALUES
    ({food_id}, '{nome}', '{categoria}', {kcal}, {prot}, {carbo}, {gord}, {fibra})
ON CONFLICT (id) DO UPDATE SET
    nome = EXCLUDED.nome,
    categoria = EXCLUDED.categoria,
    calorias_100g = EXCLUDED.calorias_100g,
    proteinas_100g = EXCLUDED.proteinas_100g,
    carboidratos_100g = EXCLUDED.carboidratos_100g,
    gorduras_100g = EXCLUDED.gorduras_100g,
    fibras_100g = EXCLUDED.fibras_100g;"""

        lines.append(sql)
        lines.append("")
        inserted += 1

    lines.append("-- Verificação")
    lines.append("SELECT COUNT(*) AS total_alimentos FROM alimentos;")
    lines.append("")
    lines.append("COMMIT;")

    OUT_SQL.parent.mkdir(parents=True, exist_ok=True)
    with open(OUT_SQL, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))

    print(f"[OK] SQL gerado: {OUT_SQL}")
    print(f"     {inserted} UPSERTs, {skipped} pulados")
    print(f"\nPara aplicar: abra no DBeaver e execute (Ctrl+Enter)")

if __name__ == "__main__":
    gerar_sql()