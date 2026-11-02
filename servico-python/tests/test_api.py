import pytest
from fastapi.testclient import TestClient

class TestTMBEndpoint:
    """Testes para /calculo/tmb"""

    def test_calcular_tmb_homem_sedentario(self, client):
        response = client.post("/calculo/tmb", json={
            "idade": 30,
            "sexo": "M",
            "peso_kg": 70,
            "altura_cm": 175,
            "nivel_atividade": "sedentario"
        })
        assert response.status_code == 200
        data = response.json()
        assert "tmb_kcal" in data
        assert "get_kcal" in data
        assert "fator_atividade" in data
        assert data["fator_atividade"] == 1.2
        # TMB = (10*70) + (6.25*175) - (5*30) + 5 = 1648.75
        assert abs(data["tmb_kcal"] - 1648.75) < 0.01
        # GET = 1648.75 * 1.2 = 1978.5
        assert abs(data["get_kcal"] - 1978.5) < 0.01

    def test_calcular_tmb_mulher_moderado(self, client):
        response = client.post("/calculo/tmb", json={
            "idade": 25,
            "sexo": "F",
            "peso_kg": 60,
            "altura_cm": 165,
            "nivel_atividade": "moderado"
        })
        assert response.status_code == 200
        data = response.json()
        assert data["fator_atividade"] == 1.55
        # TMB = (10*60) + (6.25*165) - (5*25) - 161 = 1345.25
        assert abs(data["tmb_kcal"] - 1345.25) < 0.01
        # GET = 1345.25 * 1.55 = 2085.1375
        assert abs(data["get_kcal"] - 2085.14) < 0.01

    def test_calcular_tmb_sexo_invalido(self, client):
        response = client.post("/calculo/tmb", json={
            "idade": 30,
            "sexo": "X",
            "peso_kg": 70,
            "altura_cm": 175,
            "nivel_atividade": "sedentario"
        })
        assert response.status_code == 400
        assert "Sexo deve ser" in response.json()["detail"]

    def test_calcular_tmb_todos_niveis_atividade(self, client):
        for nivel, fator in [("sedentario", 1.2), ("leve", 1.375), ("moderado", 1.55), ("intenso", 1.725), ("muito_intenso", 1.9)]:
            response = client.post("/calculo/tmb", json={
                "idade": 30, "sexo": "M", "peso_kg": 70, "altura_cm": 175, "nivel_atividade": nivel
            })
            assert response.status_code == 200
            assert response.json()["fator_atividade"] == fator


class TestCalcularPlanoEndpoint:
    """Testes para /calcular-plano"""

    def test_calcular_plano_perder_gordura(self, client):
        response = client.post("/calcular-plano", json={
            "idade": 30, "sexo": "M", "peso_kg": 70, "altura_cm": 175,
            "nivel_atividade": "sedentario", "objetivo": "perder_gordura"
        })
        assert response.status_code == 200
        data = response.json()
        assert "tmb_kcal" in data
        assert "get_kcal" in data
        assert "meta_calorias" in data
        assert "macronutrientes" in data
        # Meta = GET - 500
        assert data["meta_calorias"] == data["get_kcal"] - 500
        # Proteína = 2.2 * 70 = 154g
        assert abs(data["macronutrientes"]["proteinas_g"] - 154.0) < 0.1

    def test_calcular_plano_ganhar_massa(self, client):
        response = client.post("/calcular-plano", json={
            "idade": 30, "sexo": "M", "peso_kg": 70, "altura_cm": 175,
            "nivel_atividade": "sedentario", "objetivo": "ganhar_massa"
        })
        assert response.status_code == 200
        data = response.json()
        # Meta = GET + 400
        assert data["meta_calorias"] == data["get_kcal"] + 400
        # Proteína = 2.0 * 70 = 140g
        assert abs(data["macronutrientes"]["proteinas_g"] - 140.0) < 0.1

    def test_calcular_plano_manter_peso(self, client):
        response = client.post("/calcular-plano", json={
            "idade": 30, "sexo": "M", "peso_kg": 70, "altura_cm": 175,
            "nivel_atividade": "sedentario", "objetivo": "manter_peso"
        })
        assert response.status_code == 200
        data = response.json()
        # Meta = GET
        assert data["meta_calorias"] == data["get_kcal"]
        # Proteína = 1.8 * 70 = 126g
        assert abs(data["macronutrientes"]["proteinas_g"] - 126.0) < 0.1

    def test_calcular_plano_objetivo_invalido(self, client):
        response = client.post("/calcular-plano", json={
            "idade": 30, "sexo": "M", "peso_kg": 70, "altura_cm": 175,
            "nivel_atividade": "sedentario", "objetivo": "invalido"
        })
        # Deve default para manter_peso (sem erro)
        assert response.status_code == 200


class TestHealthEndpoint:
    def test_status(self, client):
        response = client.get("/")
        assert response.status_code == 200
        assert response.json()["status"] == "Ativo"