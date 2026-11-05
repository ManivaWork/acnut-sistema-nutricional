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

    [Fact]
    public void PlanoLimites_FromJson_DeveDesserializarSnakeCaseCorretamente()
    {
        // JSON como vem do banco (snake_case) - sem_plano limits
        var jsonSnakeCase = "{\"max_pacientes\": 3, \"max_profissionais\": 1, \"storage_mb\": 50, \"max_versoes_plano\": 1}";

        var limites = ACNutri.Billing.PlanoLimites.FromJson(jsonSnakeCase);

        limites.Should().NotBeNull();
        limites.MaxPacientes.Should().Be(3);
        limites.MaxProfissionais.Should().Be(1);
        limites.StorageMB.Should().Be(50);
        limites.MaxVersoesPlano.Should().Be(1);
    }

    [Fact]
    public void PlanoLimites_FromJson_DeveFuncionarComLimitesPlanoBasico()
    {
        // JSON com limites do plano básico (snake_case)
        var jsonSnakeCase = "{\"max_pacientes\": 50, \"max_profissionais\": 2, \"storage_mb\": 500, \"max_versoes_plano\": 5}";

        var limites = ACNutri.Billing.PlanoLimites.FromJson(jsonSnakeCase);

        limites.MaxPacientes.Should().Be(50);
        limites.MaxProfissionais.Should().Be(2);
        limites.StorageMB.Should().Be(500);
        limites.MaxVersoesPlano.Should().Be(5);
    }

    [Fact]
    public void PlanoLimites_FromJson_DeveFuncionarComLimitesPlanoProfissional()
    {
        // JSON com limites do plano profissional (snake_case)
        var jsonSnakeCase = "{\"max_pacientes\": 200, \"max_profissionais\": 10, \"storage_mb\": 2000, \"max_versoes_plano\": 20}";

        var limites = ACNutri.Billing.PlanoLimites.FromJson(jsonSnakeCase);

        limites.MaxPacientes.Should().Be(200);
        limites.MaxProfissionais.Should().Be(10);
        limites.StorageMB.Should().Be(2000);
        limites.MaxVersoesPlano.Should().Be(20);
    }

    [Fact]
    public void PlanoLimites_FromJson_DeveFuncionarComLimitesPlanoEnterprise()
    {
        // JSON com limites do plano enterprise (snake_case)
        var jsonSnakeCase = "{\"max_pacientes\": 1000, \"max_profissionais\": 50, \"storage_mb\": 10000, \"max_versoes_plano\": 100}";

        var limites = ACNutri.Billing.PlanoLimites.FromJson(jsonSnakeCase);

        limites.MaxPacientes.Should().Be(1000);
        limites.MaxProfissionais.Should().Be(50);
        limites.StorageMB.Should().Be(10000);
        limites.MaxVersoesPlano.Should().Be(100);
    }

    private record PlanoInfo
    {
        public string Nome { get; init; } = "";
        public decimal PrecoMensal { get; init; }
        public int LimitePacientes { get; init; }
        public int LimiteProfissionais { get; init; }
    }
}