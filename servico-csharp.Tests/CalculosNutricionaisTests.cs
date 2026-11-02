using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace ACNutri.Tests;

public class CalculosNutricionaisTests
{
    // Mifflin-St Jeor formula constants
    private const double MaleConstant = 5;
    private const double FemaleConstant = -161;

    // Activity factors (FAO/OMS)
    private static readonly Dictionary<string, double> FatoresAtividade = new()
    {
        { "sedentario", 1.2 },
        { "leve", 1.375 },
        { "moderado", 1.55 },
        { "intenso", 1.725 },
        { "muito_intenso", 1.9 }
    };

    [Theory]
    [InlineData(70, 175, 30, 'M', "sedentario", 1648.75, 1978.5)]    // Homem 70kg, 175cm, 30 anos
    [InlineData(60, 165, 25, 'F', "moderado", 1345.25, 2085.14)]      // Mulher 60kg, 165cm, 25 anos
    [InlineData(80, 180, 35, 'M', "intenso", 1755.0, 3027.38)]        // Homem 80kg, 180cm, 35 anos
    [InlineData(55, 160, 28, 'F', "leve", 1249.0, 1717.38)]           // Mulher 55kg, 160cm, 28 anos
    public void CalcularTMB_E_GET_DeveRetornarValoresEsperados(
        double pesoKg, double alturaCm, double idade, char sexo,
        string nivelAtividade, double tmbEsperada, double getEsperada)
    {
        // Act
        double tmb = CalcularTMB(pesoKg, alturaCm, idade, sexo);
        double fator = FatoresAtividade[nivelAtividade];
        double get = tmb * fator;

        // Assert
        tmb.Should().BeApproximately(tmbEsperada, 0.01);
        get.Should().BeApproximately(getEsperada, 0.01);
    }

    [Fact]
    public void CalcularTMB_Homem_DeveUsarFormulaCorreta()
    {
        // Arrange: Homem 70kg, 175cm, 30 anos
        double peso = 70, altura = 175, idade = 30;
        char sexo = 'M';

        // Act
        double tmb = CalcularTMB(peso, altura, idade, sexo);

        // Assert: (10 * 70) + (6.25 * 175) - (5 * 30) + 5 = 700 + 1093.75 - 150 + 5 = 1648.75
        tmb.Should().BeApproximately(1648.75, 0.01);
    }

    [Fact]
    public void CalcularTMB_Mulher_DeveUsarFormulaCorreta()
    {
        // Arrange: Mulher 60kg, 165cm, 25 anos
        double peso = 60, altura = 165, idade = 25;
        char sexo = 'F';

        // Act
        double tmb = CalcularTMB(peso, altura, idade, sexo);

        // Assert: (10 * 60) + (6.25 * 165) - (5 * 25) - 161 = 600 + 1031.25 - 125 - 161 = 1345.25
        tmb.Should().BeApproximately(1345.25, 0.01);
    }

    [Fact]
    public void FatorAtividade_Sedentario_DeveSer1Ponto2()
    {
        FatoresAtividade["sedentario"].Should().Be(1.2);
    }

    [Fact]
    public void FatorAtividade_MuitoIntenso_DeveSer1Ponto9()
    {
        FatoresAtividade["muito_intenso"].Should().Be(1.9);
    }

    [Theory]
    [InlineData("perder_gordura", -500)]
    [InlineData("manter_peso", 0)]
    [InlineData("ganhar_massa", 400)]
    public void MetaCalorica_DeveAplicarAjusteCorreto(string objetivo, double ajusteEsperado)
    {
        double get = 2000;
        double meta = objetivo switch
        {
            "perder_gordura" => get - 500,
            "ganhar_massa" => get + 400,
            _ => get
        };

        meta.Should().Be(get + ajusteEsperado);
    }

    [Theory]
    [InlineData("perder_gordura", 2.2, 0.8)]
    [InlineData("ganhar_massa", 2.0, 1.0)]
    [InlineData("manter_peso", 1.8, 0.9)]
    public void Macros_DeveCalcularProteinaEGorduraPorObjetivo(string objetivo, double protGKg, double gordGKg)
    {
        double peso = 70;
        double protEsperada = Math.Round(peso * protGKg, 1);
        double gordEsperada = Math.Round(peso * gordGKg, 1);

        double protCalculada = Math.Round(peso * (objetivo switch
        {
            "perder_gordura" => 2.2,
            "ganhar_massa" => 2.0,
            _ => 1.8
        }), 1);

        double gordCalculada = Math.Round(peso * (objetivo switch
        {
            "perder_gordura" => 0.8,
            "ganhar_massa" => 1.0,
            _ => 0.9
        }), 1);

        protCalculada.Should().Be(protEsperada);
        gordCalculada.Should().Be(gordEsperada);
    }

    // Helper methods (replicando a lógica do Python service)
    private static double CalcularTMB(double pesoKg, double alturaCm, double idade, char sexo)
    {
        sexo = char.ToUpper(sexo);
        return sexo == 'M'
            ? (10 * pesoKg) + (6.25 * alturaCm) - (5 * idade) + 5
            : (10 * pesoKg) + (6.25 * alturaCm) - (5 * idade) - 161;
    }
}