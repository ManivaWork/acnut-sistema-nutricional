#!/usr/bin/env python3
"""
Importa a TACO (Tabela Brasileira de Composição de Alimentos) de um arquivo XLSX
e gera database/dados/alimentos.json com:
  id, nome, categoria, kcal_100g, proteina_100g, carbo_100g, gordura_100g

Uso:
  python importar_taco.py

Requer: pandas, openpyxl
  pip install pandas openpyxl
"""

import sys
import json
import pandas as pd
from pathlib import Path

# Caminhos
PROJECT_ROOT = Path(__file__).parent.parent
XLSX_PATH = PROJECT_ROOT / "frontend" / "database" / "dados" / "taco_4_edicao_ampliada_e_revisada.xlsx"
OUT_DIR = PROJECT_ROOT / "database" / "dados"
OUT_JSON = OUT_DIR / "alimentos.json"

# Garante diretório de saída
OUT_DIR.mkdir(parents=True, exist_ok=True)


def to_float(val):
    """Converte valor para float, tratando vírgula, traços, células vazias, 'Tr' (traço), '*' (não determinado).

    Retorna:
    - float: valor numérico
    - None: se valor é '*' (não determinado na planilha) - NÃO inventamos valores
    - 0.0: se célula vazia/NaN (pode ser zero real, ex: sal)
    """
    if pd.isna(val):
        return 0.0
    s = str(val).strip()
    # '*' na TACO significa "não determinado" - não inventamos, retorna None
    if s == "*":
        return None
    s = s.replace(",", ".").replace("-", "0").replace("Tr", "0").strip()
    try:
        return float(s)
    except ValueError:
        return 0.0


def clean_str(val):
    """Converte para string limpa e lowercase."""
    if pd.isna(val):
        return ""
    return str(val).strip().lower()


def find_header_row(df, max_rows=5):
    """
    Encontra qual linha contém os headers com unidades (procura por '(kcal)').
    Retorna o índice da linha do header, ou None se não encontrar.
    """
    for i in range(min(max_rows, len(df))):
        row = df.iloc[i].tolist()
        row_clean = [clean_str(v) for v in row]
        if any("(kcal)" in h for h in row_clean):
            return i
    return None


def find_columns(df, header_row):
    """
    Usa a linha do header (com unidades) e a linha anterior (categorias)
    para encontrar os índices das colunas necessárias.
    """
    header = df.iloc[header_row].tolist()
    header_clean = [clean_str(v) for v in header]

    # Linha de categorias (uma acima do header)
    cat_row = header_row - 1 if header_row > 0 else None
    if cat_row is not None:
        cat = df.iloc[cat_row].tolist()
        cat_clean = [clean_str(v) for v in cat]
    else:
        cat_clean = [""] * len(header_clean)

    col_idx = {}

    for i, (h, c) in enumerate(zip(header_clean, cat_clean)):
        # ID: header tem "alimento" ou "número" E categoria tem "número"
        if ("alimento" in h or "número" in h or "numero" in h) and ("número" in c or "numero" in c):
            col_idx["id"] = i
        # Nome: header tem "descrição" ou "descri"
        elif "descri" in h:
            col_idx["nome"] = i
        # kcal: header tem "(kcal)"
        elif "(kcal)" in h:
            col_idx["kcal_100g"] = i
        # Proteína: header tem "(g)" E categoria tem "proteín" ou "prot"
        elif h == "(g)" and ("prote" in c):
            col_idx["proteina_100g"] = i
        # Lipídeos/Gordura: header tem "(g)" E categoria tem "lip" ou "gord"
        elif h == "(g)" and ("lip" in c or "gord" in c):
            col_idx["gordura_100g"] = i
        # Carboidrato: header tem "(g)" E categoria tem "carbo" ou "idrato" ou "cho"
        elif h == "(g)" and ("carbo" in c or "idrato" in c or "cho" in c):
            col_idx["carbo_100g"] = i

    # Verifica se achou todos
    required = ["id", "nome", "kcal_100g", "proteina_100g", "gordura_100g", "carbo_100g"]
    if all(k in col_idx for k in required):
        return col_idx

    # Debug: mostra o que achou
    found = {k: v for k, v in col_idx.items()}
    return None


def process_sheet(df, sheet_name):
    """Processa uma aba de dados da TACO."""
    if len(df) < 3:
        return [], "Aba com menos de 3 linhas"

    # Encontra a linha do header (onde tem "(kcal)")
    header_row = find_header_row(df)
    if header_row is None:
        return [], "Não tem coluna '(kcal)' - tabela de micronutrientes"

    # Encontra colunas usando header + categoria
    col_idx = find_columns(df, header_row)
    if col_idx is None:
        return [], f"Colunas necessárias não encontradas (header na linha {header_row})"

    alimentos = []
    # Dados começam na linha após o header
    data_start = header_row + 1

    for idx in range(data_start, len(df)):
        row = df.iloc[idx]
        try:
            id_val = str(row.iloc[col_idx["id"]]).strip()
            if not id_val or id_val.lower() in ["nan", "none"]:
                continue

            nome = str(row.iloc[col_idx["nome"]]).strip()
            if not nome or nome.lower() in ["nan", "none", "alimento", "descrição", "descricao"]:
                continue

            alimento = {
                "id": id_val,
                "nome": nome,
                "categoria": sheet_name.replace("Table ", "Grupo "),
                "kcal_100g": to_float(row.iloc[col_idx["kcal_100g"]]),
                "proteina_100g": to_float(row.iloc[col_idx["proteina_100g"]]),
                "gordura_100g": to_float(row.iloc[col_idx["gordura_100g"]]),
                "carbo_100g": to_float(row.iloc[col_idx["carbo_100g"]]),
            }
            alimentos.append(alimento)
        except Exception as e:
            continue

    return alimentos, None


def main():
    # 1. Verifica se o arquivo XLSX existe
    if not XLSX_PATH.exists():
        print(f"[ERRO] Arquivo TACO não encontrado em: {XLSX_PATH}")
        print("   Coloque o arquivo 'taco_4_edicao_ampliada_e_revisada.xlsx' em frontend/database/dados/")
        sys.exit(1)

    print(f"Lendo: {XLSX_PATH}")

    # 2. Lê todas as abas "Table X" e combina
    try:
        xls = pd.ExcelFile(XLSX_PATH)
        print(f"   Total de abas: {len(xls.sheet_names)}")

        table_sheets = [s for s in xls.sheet_names if s.lower().startswith("table ")]
        print(f"   Abas de dados (Table X): {len(table_sheets)}")

        if not table_sheets:
            print("[ERRO] Nenhuma aba 'Table X' encontrada.")
            sys.exit(1)

        todos_alimentos = []
        for sheet_name in table_sheets:
            try:
                df = pd.read_excel(XLSX_PATH, sheet_name=sheet_name, header=None)
                alimentos, erro = process_sheet(df, sheet_name)
                if erro:
                    if "micronutrientes" in erro or "Não tem coluna" in erro:
                        pass  # silencioso para tabelas de micronutrientes
                    else:
                        print(f"   [AVISO] Aba '{sheet_name}': {erro}")
                    continue
                if alimentos:
                    todos_alimentos.extend(alimentos)
                    print(f"   [OK] '{sheet_name}': {len(alimentos)} alimentos")
            except Exception as e:
                print(f"   [AVISO] Erro na aba '{sheet_name}': {e}")
                continue

    except Exception as e:
        print(f"[ERRO] ao ler XLSX: {e}")
        sys.exit(1)

    if not todos_alimentos:
        print("[ERRO] Nenhum alimento extraído. Verifique a estrutura das abas.")
        sys.exit(1)

    # Remove duplicatas por ID (mantém o primeiro)
    vistos = set()
    unicos = []
    for a in todos_alimentos:
        if a["id"] not in vistos:
            vistos.add(a["id"])
            unicos.append(a)

    print(f"\n[OK] Total de alimentos únicos: {len(unicos)}")

    # 3. Salva JSON
    with open(OUT_JSON, "w", encoding="utf-8") as f:
        json.dump(unicos, f, ensure_ascii=False, indent=2)

    print(f"[SALVO] JSON salvo em: {OUT_JSON}")

    # 4. Mostra amostra
    print("\n[AMOSTRA] Amostra (primeiros 20):")
    for a in unicos[:20]:
        print(f"  {a['id']:>6} | {a['nome'][:50]:50s} | {a['categoria'][:20]:20s} | "
              f"{a['kcal_100g']:>6.1f} kcal | P:{a['proteina_100g']:>5.1f} | C:{a['carbo_100g']:>5.1f} | G:{a['gordura_100g']:>5.1f}")


if __name__ == "__main__":
    main()