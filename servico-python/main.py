from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from typing import Optional

# Importa a biblioteca nutricional
import sys
from pathlib import Path
sys.path.insert(0, str(Path(__file__).parent))
from nutri_db import buscar, calcular_porcao, montar_item, converter_medida_caseira, listar_medidas_caseiras

app = FastAPI(
    title="Acnut - Calculadora Clínica Nutricional",
    version="1.0.0"
)

# Modelo de entrada de dados
class DadosAvaliacao(BaseModel):
    idade: int
    sexo: str  # 'M' ou 'F'
    peso_kg: float
    altura_cm: float
    nivel_atividade: str  # 'sedentario', 'leve', 'moderado', 'intenso', 'muito_intenso'
    objetivo: str  # 'perder_gordura', 'manter_peso', 'ganhar_massa'

# Modelo simplificado para cálculo de TMB/GET apenas
class DadosTMB(BaseModel):
    idade: int
    sexo: str  # 'M' ou 'F'
    peso_kg: float
    altura_cm: float
    nivel_atividade: str  # 'sedentario', 'leve', 'moderado', 'intenso', 'muito_intenso'

# Fatores de Atividade Física (Estatuto FAO/OMS)
FATORES_ATIVIDADE = {
    "sedentario": 1.2,
    "leve": 1.375,
    "moderado": 1.55,
    "intenso": 1.725,
    "muito_intenso": 1.9
}

@app.get("/")
def status():
    return {"servico": "Smart Nutri Python API", "status": "Ativo"}

@app.post("/calculo/tmb")
def calcular_tmb(dados: DadosTMB):
    """
    Calcula Taxa Metabólica Basal (TMB) e Gasto Energético Total (GET).

    Entrada: peso_kg, altura_cm, idade, sexo (M/F), nivel_atividade
    Saída: tmb_kcal, get_kcal, fator_atividade

    Fórmula: Mifflin-St Jeor
    - Homens: TMB = (10 × peso) + (6.25 × altura) - (5 × idade) + 5
    - Mulheres: TMB = (10 × peso) + (6.25 × altura) - (5 × idade) - 161

    GET = TMB × Fator de Atividade (FAO/OMS)
    """
    # Validação de sexo
    sexo = dados.sexo.upper()
    if sexo not in ('M', 'F'):
        raise HTTPException(status_code=400, detail="Sexo deve ser 'M' ou 'F'")

    # 1. Cálculo da TMB (Fórmula Mifflin-St Jeor)
    if sexo == 'M':
        tmb = (10 * dados.peso_kg) + (6.25 * dados.altura_cm) - (5 * dados.idade) + 5
    else:
        tmb = (10 * dados.peso_kg) + (6.25 * dados.altura_cm) - (5 * dados.idade) - 161

    # 2. Cálculo do GET (Gasto Energético Total)
    fator = FATORES_ATIVIDADE.get(dados.nivel_atividade.lower(), 1.2)
    get_kcal = tmb * fator

    return {
        "tmb_kcal": round(tmb, 2),
        "get_kcal": round(get_kcal, 2),
        "fator_atividade": fator,
        "nivel_atividade": dados.nivel_atividade.lower()
    }


@app.post("/calcular-plano")
def calcular_plano(dados: DadosAvaliacao):
    # 1. Cálculo da TMB (Fórmula Mifflin-St Jeor)
    if dados.sexo.upper() == 'M':
        tmb = (10 * dados.peso_kg) + (6.25 * dados.altura_cm) - (5 * dados.idade) + 5
    else:
        tmb = (10 * dados.peso_kg) + (6.25 * dados.altura_cm) - (5 * dados.idade) - 161

    # 2. Cálculo do GET (Gasto Energético Total)
    fator = FATORES_ATIVIDADE.get(dados.nivel_atividade.lower(), 1.2)
    get_kcal = tmb * fator

    # 3. Definição da Meta Calórica conforme o Objetivo
    if dados.objetivo == "perder_gordura":
        meta_calorias = get_kcal - 500  # Déficit calórico
    elif dados.objetivo == "ganhar_massa":
        meta_calorias = get_kcal + 400  # Superávit calórico
    else:
        meta_calorias = get_kcal  # Manutenção

    # 4. Distribuição de Macronutrientes (g/kg de peso corporal)
    if dados.objetivo == "perder_gordura":
        proteina_g_kg = 2.2
        gordura_g_kg = 0.8
    elif dados.objetivo == "ganhar_massa":
        proteina_g_kg = 2.0
        gordura_g_kg = 1.0
    else:
        proteina_g_kg = 1.8
        gordura_g_kg = 0.9

    proteinas_g = round(dados.peso_kg * proteina_g_kg, 1)
    gorduras_g = round(dados.peso_kg * gordura_g_kg, 1)

    # 1g Proteína = 4 kcal | 1g Gordura = 9 kcal | 1g Carboidrato = 4 kcal
    calorias_proteinas = proteinas_g * 4
    calorias_gorduras = gorduras_g * 9
    calorias_restantes = meta_calorias - (calorias_proteinas + calorias_gorduras)

    carboidratos_g = round(max(calorias_restantes / 4, 0), 1)

    return {
        "tmb_kcal": round(tmb, 2),
        "get_kcal": round(get_kcal, 2),
        "meta_calorias": round(meta_calorias, 2),
        "macronutrientes": {
            "proteinas_g": proteinas_g,
            "carboidratos_g": carboidratos_g,
            "gorduras_g": gorduras_g
        }
    }
# ============================================================
# Modelos para endpoint /calcular-item
# ============================================================
class CalcularItemInput(BaseModel):
    alimento_id: str
    # Modo 1: target nutricional (prioridade: kcal > proteina > carbo > gordura)
    target_kcal: Optional[float] = None
    target_proteina: Optional[float] = None
    target_carbo: Optional[float] = None
    target_gordura: Optional[float] = None
    # Modo 2: gramas diretas
    gramas: Optional[float] = None
    # Modo 3: medida caseira (ex: "2 colheres de sopa")
    medida_caseira: Optional[str] = None
    quantidade_medida: Optional[float] = None
    observacao_medida: Optional[str] = ""  # ex: "rasa", "cheia", "200ml"
    refeicao: Optional[str] = "lanche"
    observacao: Optional[str] = ""


class CalcularItemOutput(BaseModel):
    alimento_id: str
    alimento_nome: str
    gramas: float
    kcal: Optional[float] = None
    proteina: Optional[float] = None
    carbo: Optional[float] = None
    gordura: Optional[float] = None
    refeicao: str
    observacao: str
    base_calculo: str  # 'kcal', 'proteina', 'carbo', 'gordura', 'gramas', 'padrao', 'dados_insuficientes'


@app.post("/calcular-item", response_model=CalcularItemOutput)
def calcular_item(input_data: CalcularItemInput):
    """
    Calcula item de plano alimentar.

    Modo 1 - Por target nutricional: forneça alimento_id + um de (target_kcal, target_proteina, target_carbo, target_gordura)
      Ex: {"alimento_id": "175", "target_kcal": 130} -> calcula gramas para 130 kcal
      Ex: {"alimento_id": "341", "target_proteina": 20} -> calcula gramas para 20g proteína

    Modo 2 - Por gramas fixas: forneça alimento_id + gramas
      Ex: {"alimento_id": "175", "gramas": 100} -> calcula macros para 100g

    Modo 3 - Por medida caseira: forneça alimento_id + medida_caseira + quantidade_medida
      Ex: {"alimento_id": "175", "medida_caseira": "unidade média", "quantidade_medida": 2} -> 2 bananas médias

    Retorna item pronto para inserir em plano_alimentar_itens.
    """
    # Busca alimento
    alimento = buscar(input_data.alimento_id)
    if not alimento:
        raise HTTPException(status_code=404, detail=f"Alimento não encontrado: {input_data.alimento_id}")

    # Modo 3: medida caseira (prioridade sobre gramas e targets)
    if input_data.medida_caseira and input_data.quantidade_medida and input_data.quantidade_medida > 0:
        gramas = converter_medida_caseira(
            input_data.alimento_id,
            input_data.medida_caseira,
            input_data.quantidade_medida,
            input_data.observacao_medida or ""
        )
        if gramas is None:
            raise HTTPException(
                status_code=400,
                detail=f"Medida caseira '{input_data.medida_caseira}' não encontrada para este alimento. "
                       f"Medidas disponíveis: {[m['medida'] for m in listar_medidas_caseiras(input_data.alimento_id)]}"
            )
        item = montar_item(alimento, gramas, input_data.refeicao, input_data.observacao)
        item["base_calculo"] = f"medida_caseira ({input_data.medida_caseira})"
        return item

    # Modo 2: gramas fixas
    if input_data.gramas is not None and input_data.gramas > 0:
        item = montar_item(alimento, input_data.gramas, input_data.refeicao, input_data.observacao)
        item["base_calculo"] = "gramas"
        return item

    # Modo 1: target nutricional
    # Verifica se pelo menos um target foi fornecido
    targets = [input_data.target_kcal, input_data.target_proteina, input_data.target_carbo, input_data.target_gordura]
    if all(t is None for t in targets):
        raise HTTPException(status_code=400, detail="Forneça 'gramas', 'medida_caseira'+'quantidade_medida', ou pelo menos um target (target_kcal, target_proteina, target_carbo, target_gordura)")

    resultado = calcular_porcao(
        alimento,
        target_kcal=input_data.target_kcal,
        target_proteina=input_data.target_proteina,
        target_carbo=input_data.target_carbo,
        target_gordura=input_data.target_gordura
    )

    # Monta item final
    item = montar_item(alimento, resultado["gramas"], input_data.refeicao, input_data.observacao)
    item["base_calculo"] = resultado["base"]
    return item


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=5002)