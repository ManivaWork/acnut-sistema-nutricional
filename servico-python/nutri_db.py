#!/usr/bin/env python3
"""
nutri_db.py - Biblioteca de cálculo nutricional para ACNUTRI

Funções:
  - buscar(nome_ou_id): busca alimento no JSON da TACO
  - calcular_porcao(alimento, target_kcal=None, target_proteina=None,
                    target_carbo=None, target_gordura=None): calcula gramas para atingir target
  - montar_item(alimento, gramas): monta item de plano alimentar com macros calculados
"""

import json
import os
from pathlib import Path
from typing import Optional, Union, Dict, List, Any

# Caminho do JSON gerado pelo importar_taco.py
PROJECT_ROOT = Path(__file__).parent.parent
JSON_PATH = PROJECT_ROOT / "database" / "dados" / "alimentos.json"

# Cache em memória
_alimentos_cache: Optional[List[Dict]] = None
_alimentos_by_id: Optional[Dict[str, Dict]] = None
_alimentos_by_nome: Optional[Dict[str, Dict]] = None

# Cache de medidas caseiras (carregado do banco PostgreSQL)
_medidas_cache: Optional[Dict[int, List[Dict]]] = None


def _carregar():
    """Carrega o JSON da TACO para memória (lazy loading)."""
    global _alimentos_cache, _alimentos_by_id, _alimentos_by_nome

    if _alimentos_cache is not None:
        return

    if not JSON_PATH.exists():
        raise FileNotFoundError(
            f"Arquivo de alimentos não encontrado: {JSON_PATH}. "
            f"Execute 'python importar_taco.py' primeiro."
        )

    with open(JSON_PATH, "r", encoding="utf-8") as f:
        _alimentos_cache = json.load(f)

    # Índices para busca rápida
    _alimentos_by_id = {a["id"]: a for a in _alimentos_cache}
    _alimentos_by_nome = {a["nome"].lower(): a for a in _alimentos_cache}


def _buscar_alimento_no_banco(alimento_id: Union[str, int]) -> Optional[Dict[str, Any]]:
    """Busca um alimento no banco PostgreSQL por ID."""
    conn_str = os.getenv("POSTGRES_CONN")
    if not conn_str:
        return None

    try:
        import psycopg2
        with psycopg2.connect(conn_str) as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id, nome, categoria, calorias_100g, proteinas_100g,
                           carboidratos_100g, gorduras_100g, fibras_100g
                    FROM alimentos WHERE id = %s
                """, (int(alimento_id),))
                row = cur.fetchone()
                if row:
                    return {
                        "id": str(row[0]),
                        "nome": row[1],
                        "categoria": row[2] or "",
                        "kcal_100g": float(row[3]) if row[3] is not None else None,
                        "proteina_100g": float(row[4]) if row[4] is not None else None,
                        "carbo_100g": float(row[5]) if row[5] is not None else None,
                        "gordura_100g": float(row[6]) if row[6] is not None else None,
                        "fibras_100g": float(row[7]) if row[7] is not None else None,
                    }
    except Exception:
        pass
    return None


def _carregar_medidas():
    """Carrega medidas caseiras do banco PostgreSQL (lazy loading)."""
    global _medidas_cache

    if _medidas_cache is not None:
        return

    _medidas_cache = {}

    # Tenta conectar no banco para carregar medidas caseiras
    conn_str = os.getenv("POSTGRES_CONN")
    if not conn_str:
        return  # Sem banco configurado, segue sem medidas caseiras

    try:
        import psycopg2
        with psycopg2.connect(conn_str) as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT alimento_id, medida, gramas_por_unidade, observacao
                    FROM alimentos_medidas_caseiras
                    ORDER BY alimento_id, medida
                """)
                for row in cur.fetchall():
                    alimento_id = row[0]
                    if alimento_id not in _medidas_cache:
                        _medidas_cache[alimento_id] = []
                    _medidas_cache[alimento_id].append({
                        "medida": row[1],
                        "gramas_por_unidade": float(row[2]),
                        "observacao": row[3] or ""
                    })
    except Exception:
        # Se falhar (banco indisponível, tabela não existe, etc.), segue sem medidas
        pass


def listar_medidas_caseiras(alimento_id: Union[str, int]) -> List[Dict[str, Any]]:
    """
    Retorna lista de medidas caseiras disponíveis para um alimento.

    Args:
        alimento_id: ID do alimento

    Returns:
        Lista de dicts com: medida, gramas_por_unidade, observacao
        Ex: [{"medida": "colher de sopa", "gramas_por_unidade": 15.0, "observacao": "rasa"}, ...]
    """
    _carregar_medidas()
    key = int(alimento_id)
    return _medidas_cache.get(key, [])


def converter_medida_caseira(
    alimento_id: Union[str, int],
    medida: str,
    quantidade: float,
    observacao: str = ""
) -> Optional[float]:
    """
    Converte quantidade em medida caseira para gramas.

    Args:
        alimento_id: ID do alimento
        medida: Nome da medida (ex: "colher de sopa", "xícara", "fatia")
        quantidade: Quantidade desta medida (ex: 2 para "2 colheres")
        observacao: Detalhe opcional para diferenciar (ex: "rasa", "cheia", "200ml")

    Returns:
        Gramas calculadas ou None se medida não encontrada

    Exemplo:
        >>> converter_medida_caseira(175, "unidade média", 2)
        240.0  # 2 bananas médias de 120g cada
    """
    _carregar_medidas()
    key = int(alimento_id)
    medidas = _medidas_cache.get(key, [])

    # Busca exata por medida + observacao
    for m in medidas:
        if m["medida"].lower() == medida.lower() and m["observacao"].lower() == observacao.lower():
            return round(m["gramas_por_unidade"] * quantidade, 1)

    # Busca apenas por medida (ignora observacao)
    for m in medidas:
        if m["medida"].lower() == medida.lower():
            return round(m["gramas_por_unidade"] * quantidade, 1)

    return None


def buscar(nome_ou_id: Union[str, int]) -> Optional[Dict[str, Any]]:
    """
    Busca um alimento por ID (string/numérico) ou por nome (busca exata case-insensitive).
    Primeiro busca no JSON da TACO, depois no banco PostgreSQL (para alimentos customizados).

    Args:
        nome_ou_id: ID do alimento (ex: "341", 341) ou nome (ex: "Banana, prata, crua")

    Returns:
        Dict com o alimento ou None se não encontrado

    Exemplo:
        >>> buscar("341")
        {'id': '341', 'nome': 'Carne, bovina, contra-filé...', 'kcal_100g': 202, ...}
        >>> buscar("banana, prata, crua")
        {'id': '123', 'nome': 'Banana, prata, crua', 'kcal_100g': 98, ...}
    """
    _carregar()

    # Tenta por ID primeiro no JSON da TACO
    key = str(nome_ou_id).strip()
    if key in _alimentos_by_id:
        return _alimentos_by_id[key]

    # Tenta por nome (case-insensitive) no JSON da TACO
    key_lower = key.lower()
    if key_lower in _alimentos_by_nome:
        return _alimentos_by_nome[key_lower]

    # Fallback: busca no banco PostgreSQL (para alimentos customizados não presentes na TACO)
    # Só tenta por ID numérico - fazer ANTES da busca parcial para evitar matches incorretos
    try:
        alimento_id = int(key)
        return _buscar_alimento_no_banco(alimento_id)
    except ValueError:
        pass

    # Busca parcial por nome (contém) no JSON da TACO
    for nome, alimento in _alimentos_by_nome.items():
        if key_lower in nome:
            return alimento

    return None


def calcular_porcao(
    alimento: Dict[str, Any],
    target_kcal: Optional[float] = None,
    target_proteina: Optional[float] = None,
    target_carbo: Optional[float] = None,
    target_gordura: Optional[float] = None
) -> Dict[str, Any]:
    """
    Calcula a quantidade em gramas necessária para atingir um target nutricional.

    Prioridade de cálculo (usa o primeiro target não-None e com dado disponível):
    1. target_kcal -> gramas = target_kcal / (kcal_100g / 100)
    2. target_proteina -> gramas = target_proteina / (proteina_100g / 100)
    3. target_carbo -> gramas = target_carbo / (carbo_100g / 100)
    4. target_gordura -> gramas = target_gordura / (gordura_100g / 100)

    Se todos forem None ou não tiverem dados disponíveis, retorna 100g (porção de referência).

    Args:
        alimento: Dict retornado por buscar()
        target_kcal: Calorias desejadas (ex: 130)
        target_proteina: Gramas de proteína desejadas (ex: 10)
        target_carbo: Gramas de carboidrato desejadas (ex: 20)
        target_gordura: Gramas de gordura desejadas (ex: 5)

    Returns:
        Dict com:
        - gramas: quantidade calculada
        - kcal: calorias na porção calculada (None se kcal_100g não disponível)
        - proteina: g de proteína na porção (None se proteina_100g não disponível)
        - carbo: g de carboidrato na porção (None se carbo_100g não disponível)
        - gordura: g de gordura na porção (None se gordura_100g não disponível)
        - base: qual target foi usado ('kcal', 'proteina', 'carbo', 'gordura', 'padrao', 'dados_insuficientes')

    Exemplo:
        >>> banana = buscar("banana, prata, crua")
        >>> calcular_porcao(banana, target_kcal=130)
        {'gramas': 132.7, 'kcal': 130.0, 'proteina': 1.4, 'carbo': 33.5, 'gordura': 0.4, 'base': 'kcal'}
        >>> calcular_porcao(banana, target_proteina=10)
        {'gramas': 952.4, 'kcal': 933.3, 'proteina': 10.0, 'carbo': 240.7, 'gordura': 3.6, 'base': 'proteina'}
    """
    # Não converte None para 0 - None significa "dado não disponível na planilha"
    kcal_100g = alimento.get("kcal_100g")
    prot_100g = alimento.get("proteina_100g")
    carbo_100g = alimento.get("carbo_100g")
    gord_100g = alimento.get("gordura_100g")

    # Determina qual target usar (prioridade: kcal > proteina > carbo > gordura)
    # Só usa o target se o macro correspondente estiver disponível (não None)
    gramas = 100.0
    base = "padrao"

    if target_kcal is not None and kcal_100g is not None and kcal_100g > 0:
        gramas = (target_kcal / kcal_100g) * 100
        base = "kcal"
    elif target_proteina is not None and prot_100g is not None and prot_100g > 0:
        gramas = (target_proteina / prot_100g) * 100
        base = "proteina"
    elif target_carbo is not None and carbo_100g is not None and carbo_100g > 0:
        gramas = (target_carbo / carbo_100g) * 100
        base = "carbo"
    elif target_gordura is not None and gord_100g is not None and gord_100g > 0:
        gramas = (target_gordura / gord_100g) * 100
        base = "gordura"
    elif any(t is not None for t in [target_kcal, target_proteina, target_carbo, target_gordura]):
        # Target foi pedido mas macro não está disponível
        base = "dados_insuficientes"

    # Calcula macros na porção calculada (None se macro não disponível)
    fator = gramas / 100.0

    def calc(macro_100g):
        if macro_100g is None:
            return None
        return round(macro_100g * fator, 1)

    return {
        "gramas": round(gramas, 1),
        "kcal": calc(kcal_100g),
        "proteina": calc(prot_100g),
        "carbo": calc(carbo_100g),
        "gordura": calc(gord_100g),
        "base": base
    }


def montar_item(
    alimento: Dict[str, Any],
    gramas: float,
    refeicao: str = "lanche",
    observacao: str = ""
) -> Dict[str, Any]:
    """
    Monta um item de plano alimentar com macros calculados para a quantidade em gramas.

    Args:
        alimento: Dict retornado por buscar()
        gramas: Quantidade em gramas
        refeicao: Tipo de refeição ('cafe_manha', 'almoco', 'jantar', 'lanche', 'pre_treino', 'pos_treino', 'ceia')
        observacao: Observação livre

    Returns:
        Dict pronto para inserir no banco (tabela plano_alimentar_itens)
        Valores None indicam que o dado não está disponível na planilha TACO.

    Exemplo:
        >>> banana = buscar("banana, prata, crua")
        >>> montar_item(banana, 130, refeicao="lanche", observacao="pós-treino")
        {
            'alimento_id': '123',
            'alimento_nome': 'Banana, prata, crua',
            'gramas': 130,
            'kcal': 127.4,
            'proteina': 1.4,
            'carbo': 32.8,
            'gordura': 0.4,
            'refeicao': 'lanche',
            'observacao': 'pós-treino'
        }
    """
    # Não converte None para 0 - None significa "dado não disponível na planilha"
    kcal_100g = alimento.get("kcal_100g")
    prot_100g = alimento.get("proteina_100g")
    carbo_100g = alimento.get("carbo_100g")
    gord_100g = alimento.get("gordura_100g")

    fator = gramas / 100.0

    def calc(macro_100g):
        if macro_100g is None:
            return None
        return round(macro_100g * fator, 1)

    return {
        "alimento_id": alimento["id"],
        "alimento_nome": alimento["nome"],
        "gramas": round(gramas, 1),
        "kcal": calc(kcal_100g),
        "proteina": calc(prot_100g),
        "carbo": calc(carbo_100g),
        "gordura": calc(gord_100g),
        "refeicao": refeicao,
        "observacao": observacao
    }


def listar_todos() -> List[Dict[str, Any]]:
    """Retorna todos os alimentos carregados."""
    _carregar()
    return _alimentos_cache


def buscar_por_categoria(categoria: str) -> List[Dict[str, Any]]:
    """Busca alimentos por categoria (match parcial case-insensitive)."""
    _carregar()
    cat_lower = categoria.lower()
    return [a for a in _alimentos_cache if cat_lower in a.get("categoria", "").lower()]


# Teste rápido se executado diretamente
if __name__ == "__main__":
    print("=== Teste nutri_db ===\n")

    # Teste buscar por ID
    print("1. Buscar por ID '341':")
    a = buscar("341")
    print(f"   {a['nome']} | {a['kcal_100g']} kcal/100g\n")

    # Teste buscar por nome
    print("2. Buscar por nome 'banana':")
    a = buscar("banana")
    if a:
        print(f"   {a['nome']} | {a['kcal_100g']} kcal/100g\n")
    else:
        print("   Não encontrado\n")

    # Teste calcular_porcao por kcal
    print("3. Calcular porção para 130 kcal (banana):")
    banana = buscar("banana")
    if banana:
        r = calcular_porcao(banana, target_kcal=130)
        print(f"   {r['gramas']}g -> {r['kcal']} kcal, P:{r['proteina']}g C:{r['carbo']}g G:{r['gordura']}g (base: {r['base']})\n")

    # Teste calcular_porcao por proteína
    print("4. Calcular porção para 10g proteína (carne bovina):")
    carne = buscar("341")
    if carne:
        r = calcular_porcao(carne, target_proteina=10)
        print(f"   {r['gramas']}g -> {r['kcal']} kcal, P:{r['proteina']}g C:{r['carbo']}g G:{r['gordura']}g (base: {r['base']})\n")

    # Teste montar_item
    print("5. Montar item (100g banana no lanche):")
    if banana:
        item = montar_item(banana, 100, refeicao="lanche", observacao="teste")
        print(f"   {item}\n")

    print("=== Fim do teste ===")