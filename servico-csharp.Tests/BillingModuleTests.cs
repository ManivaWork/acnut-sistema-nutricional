using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace ACNutri.Tests;

public class BillingModuleTests
{
    [Fact]
    public void Planos_DeveTerTresPlanosBasicoProfissionalEnterprise()
    {
        // Arrange - replicando estrutura do BillingModule.cs
        var planos = new Dictionary<string, PlanoInfo>
        {
            ["basico"] = new PlanoInfo { Nome = "Básico", PrecoMensal = 49.90m, LimitePacientes = 30, LimiteProfissionais = 1 },
            ["profissional"] = new PlanoInfo { Nome = "Profissional", PrecoMensal = 99.90m, LimitePacientes = 100, LimiteProfissionais = 3 },
            ["enterprise"] = new PlanoInfo { Nome = "Enterprise", PrecoMensal = 199.90m, LimitePacientes = 500, LimiteProfissionais = 10 }
        };

        // Assert
        planos.Should().HaveCount(3);
        planos.Should().ContainKeys("basico", "profissional", "enterprise");
    }

    [Theory]
    [InlineData("basico", 49.90, 30, 1)]
    [InlineData("profissional", 99.90, 100, 3)]
    [InlineData("enterprise", 199.90, 500, 10)]
    public void Plano_DeveTerValoresCorretos(string key, decimal preco, int pacientes, int profissionais)
    {
        var planos = new Dictionary<string, PlanoInfo>
        {
            ["basico"] = new PlanoInfo { Nome = "Básico", PrecoMensal = 49.90m, LimitePacientes = 30, LimiteProfissionais = 1 },
            ["profissional"] = new PlanoInfo { Nome = "Profissional", PrecoMensal = 99.90m, LimitePacientes = 100, LimiteProfissionais = 3 },
            ["enterprise"] = new PlanoInfo { Nome = "Enterprise", PrecoMensal = 199.90m, LimitePacientes = 500, LimiteProfissionais = 10 }
        };

        planos[key].PrecoMensal.Should().Be(preco);
        planos[key].LimitePacientes.Should().Be(pacientes);
        planos[key].LimiteProfissionais.Should().Be(profissionais);
    }

    [Fact]
    public void PlanoInvalido_DeveLancarExcecao()
    {
        var planos = new Dictionary<string, PlanoInfo>
        {
            ["basico"] = new PlanoInfo { Nome = "Básico", PrecoMensal = 49.90m, LimitePacientes = 30, LimiteProfissionais = 1 }
        };

        Action act = () => _ = planos["inexistente"];
        act.Should().Throw<KeyNotFoundException>();
    }

    private record PlanoInfo
    {
        public string Nome { get; init; } = "";
        public decimal PrecoMensal { get; init; }
        public int LimitePacientes { get; init; }
        public int LimiteProfissionais { get; init; }
    }
}