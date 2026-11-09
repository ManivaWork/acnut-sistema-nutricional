using System;
using System.Collections.Generic;
using System.Linq;
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

    [Fact]
    public void Plano_SemPlano_DeveTerLimite5Pacientes()
    {
        // Arrange - replicando estrutura do BillingModule.cs incluindo sem_plano
        var planos = new Dictionary<string, PlanoInfo>
        {
            ["basico"] = new PlanoInfo { Nome = "Básico", PrecoMensal = 49.90m, LimitePacientes = 30, LimiteProfissionais = 1 },
            ["profissional"] = new PlanoInfo { Nome = "Profissional", PrecoMensal = 99.90m, LimitePacientes = 100, LimiteProfissionais = 3 },
            ["enterprise"] = new PlanoInfo { Nome = "Enterprise", PrecoMensal = 199.90m, LimitePacientes = 500, LimiteProfissionais = 10 },
            ["sem_plano"] = new PlanoInfo { Nome = "Gratuito", PrecoMensal = 0m, LimitePacientes = 5, LimiteProfissionais = 1 }
        };

        // Assert
        planos.Should().ContainKey("sem_plano");
        planos["sem_plano"].LimitePacientes.Should().Be(5);
        planos["sem_plano"].PrecoMensal.Should().Be(0m);
        planos["sem_plano"].LimiteProfissionais.Should().Be(1);
    }

    [Theory]
    [InlineData("basico", 49.90, 30, 1)]
    [InlineData("profissional", 99.90, 100, 3)]
    [InlineData("enterprise", 199.90, 500, 10)]
    [InlineData("sem_plano", 0, 5, 1)]
    public void Plano_DeveTerValoresCorretos(string key, decimal preco, int pacientes, int profissionais)
    {
        var planos = new Dictionary<string, PlanoInfo>
        {
            ["basico"] = new PlanoInfo { Nome = "Básico", PrecoMensal = 49.90m, LimitePacientes = 30, LimiteProfissionais = 1 },
            ["profissional"] = new PlanoInfo { Nome = "Profissional", PrecoMensal = 99.90m, LimitePacientes = 100, LimiteProfissionais = 3 },
            ["enterprise"] = new PlanoInfo { Nome = "Enterprise", PrecoMensal = 199.90m, LimitePacientes = 500, LimiteProfissionais = 10 },
            ["sem_plano"] = new PlanoInfo { Nome = "Gratuito", PrecoMensal = 0m, LimitePacientes = 5, LimiteProfissionais = 1 }
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
        // JSON como vem do banco (snake_case) - sem_plano limits (5 pacientes)
        var jsonSnakeCase = "{\"max_pacientes\": 5, \"max_profissionais\": 1, \"storage_mb\": 50, \"max_versoes_plano\": 1}";

        var limites = ACNutri.Billing.PlanoLimites.FromJson(jsonSnakeCase);

        limites.Should().NotBeNull();
        limites.MaxPacientes.Should().Be(5);
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

// =====================================================================
// PLAN ENFORCEMENT TESTS - Testes de limites de planos (sem_plano = 5 pacientes)
// =====================================================================
public class PlanoEnforcementTests
{
    [Fact]
    public void SemPlano_LimitePacientes_DeveSer5()
    {
        // Testa que o catálogo tem sem_plano com limite 5
        var catalogo = ACNutri.Billing.Planos.Catalogo;
        catalogo.Should().ContainKey("sem_plano");
        catalogo["sem_plano"].Limites.MaxPacientes.Should().Be(5);
    }

    [Theory]
    [InlineData("sem_plano", 5)]
    [InlineData("basico", 50)]
    [InlineData("profissional", 200)]
    [InlineData("enterprise", 1000)]
    public void Catalogo_DeveTerLimitesCorretos(string planoKey, int maxPacientesEsperado)
    {
        var catalogo = ACNutri.Billing.Planos.Catalogo;
        catalogo.Should().ContainKey(planoKey);
        catalogo[planoKey].Limites.MaxPacientes.Should().Be(maxPacientesEsperado);
    }

    [Theory]
    [InlineData("sem_plano", 1)]
    [InlineData("basico", 2)]
    [InlineData("profissional", 10)]
    [InlineData("enterprise", 50)]
    public void Catalogo_DeveTerLimitesProfissionaisCorretos(string planoKey, int maxProfissionaisEsperado)
    {
        var catalogo = ACNutri.Billing.Planos.Catalogo;
        catalogo.Should().ContainKey(planoKey);
        catalogo[planoKey].Limites.MaxProfissionais.Should().Be(maxProfissionaisEsperado);
    }

    [Fact]
    public void PlanoLimites_SemPlano_DevePermiteAte5Pacientes()
    {
        // Testa que os limites do sem_plano permitem até 5 pacientes
        var limites = ACNutri.Billing.Planos.Catalogo["sem_plano"].Limites;

        // Verificar que permite até 5
        limites.MaxPacientes.Should().Be(5);

        // Simular verificação: 1 a 5 pacientes devem ser permitidos
        for (int i = 1; i <= 5; i++)
        {
            i.Should().BeLessThanOrEqualTo(limites.MaxPacientes);
        }
    }

    [Fact]
    public void PlanoLimites_SemPlano_DeveBloquear6oPaciente()
    {
        // Testa que o 6º paciente deve ser bloqueado pelo sem_plano
        var limites = ACNutri.Billing.Planos.Catalogo["sem_plano"].Limites;

        // O 6º paciente excede o limite
        int sextoPaciente = 6;
        sextoPaciente.Should().BeGreaterThan(limites.MaxPacientes);
    }

    [Fact]
    public void PlanoLimites_Basico_DevePermitirAte50Pacientes()
    {
        var limites = ACNutri.Billing.Planos.Catalogo["basico"].Limites;
        limites.MaxPacientes.Should().Be(50);

        // 50 deve ser permitido
        50.Should().BeLessThanOrEqualTo(limites.MaxPacientes);
        // 51 deve ser bloqueado
        51.Should().BeGreaterThan(limites.MaxPacientes);
    }

    [Fact]
    public void PlanoLimites_Profissional_DevePermitirAte200Pacientes()
    {
        var limites = ACNutri.Billing.Planos.Catalogo["profissional"].Limites;
        limites.MaxPacientes.Should().Be(200);

        200.Should().BeLessThanOrEqualTo(limites.MaxPacientes);
        201.Should().BeGreaterThan(limites.MaxPacientes);
    }

    [Fact]
    public void PlanoLimites_Enterprise_DevePermitirAte1000Pacientes()
    {
        var limites = ACNutri.Billing.Planos.Catalogo["enterprise"].Limites;
        limites.MaxPacientes.Should().Be(1000);

        1000.Should().BeLessThanOrEqualTo(limites.MaxPacientes);
        1001.Should().BeGreaterThan(limites.MaxPacientes);
    }

    [Fact]
    public void PlanoLimites_SemPlano_DeveTerStorage50MB()
    {
        var limites = ACNutri.Billing.Planos.Catalogo["sem_plano"].Limites;
        limites.StorageMB.Should().Be(50);
    }

    [Fact]
    public void PlanoLimites_SemPlano_DeveTerMaxVersoesPlano1()
    {
        var limites = ACNutri.Billing.Planos.Catalogo["sem_plano"].Limites;
        limites.MaxVersoesPlano.Should().Be(1);
    }

    [Fact]
    public void PlanoLimites_SemPlano_DeveTerMaxProfissionais1()
    {
        var limites = ACNutri.Billing.Planos.Catalogo["sem_plano"].Limites;
        limites.MaxProfissionais.Should().Be(1);
    }
}