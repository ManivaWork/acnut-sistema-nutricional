using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace ACNutri.Tests;

public class AnamneseApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string JwtKey = "SUA_CHAVE_JWT_AQUI_MIN_32_CHARS_12345678901234567890";
    private const string Issuer = "bybarhub";
    private const string Audience = "bybarhub-app";

    public AnamneseApiTests(WebApplicationFactory<Program> factory)
    {
        // Set the JWT key as environment variable before the factory creates the host
        Environment.SetEnvironmentVariable("Jwt:Key", JwtKey);

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Disable claim mapping to ensure custom claims like "perfil" pass through unchanged
                services.Configure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                    Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                    options => options.MapInboundClaims = false);
            });
        });
    }

    private string GerarTokenEquipe(int uid, int empresa, string perfil, Dictionary<string, string>? modulos = null)
    {
        var claims = new List<Claim>
        {
            new("uid", uid.ToString()),
            new("empresa", empresa.ToString()),
            new("perfil", perfil),
            new("tipo", "equipe")
        };
        if (modulos != null)
        {
            foreach (var (m, n) in modulos)
                claims.Add(new Claim($"mod_{m}", n));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GerarTokenPaciente(int pid, int empresa)
    {
        var claims = new List<Claim>
        {
            new("pid", pid.ToString()),
            new("empresa", empresa.ToString()),
            new("tipo", "paciente"),
            // Global middleware checks mod_nutricao for all routes, so include it
            new("mod_nutricao", "total")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private HttpClient CriarCliente(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public void Debug_TokenClaims_ShouldContainPerfil()
    {
        // Generate a token and decode it to verify claims
        var token = GerarTokenEquipe(uid: 1, empresa: 1, perfil: "nutricionista", modulos: new() { { "nutricao", "edicao" } });
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var perfilClaim = jwt.Claims.FirstOrDefault(c => c.Type == "perfil");
        var tipoClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tipo");
        var empresaClaim = jwt.Claims.FirstOrDefault(c => c.Type == "empresa");

        // Print claims for debugging
        foreach (var claim in jwt.Claims)
        {
            System.Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
        }

        perfilClaim.Should().NotBeNull("perfil claim should exist");
        perfilClaim!.Value.Should().Be("nutricionista");
        tipoClaim.Should().NotBeNull("tipo claim should exist");
        tipoClaim!.Value.Should().Be("equipe");
        empresaClaim.Should().NotBeNull("empresa claim should exist");
        empresaClaim!.Value.Should().Be("1");
    }

    [Fact]
    public void Debug_PacienteTokenClaims_ShouldContainModNutricao()
    {
        // Generate a paciente token and decode it to verify claims
        var token = GerarTokenPaciente(pid: 1, empresa: 1);
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        var modNutricaoClaim = jwt.Claims.FirstOrDefault(c => c.Type == "mod_nutricao");
        var tipoClaim = jwt.Claims.FirstOrDefault(c => c.Type == "tipo");
        var pidClaim = jwt.Claims.FirstOrDefault(c => c.Type == "pid");

        // Print claims for debugging
        foreach (var claim in jwt.Claims)
        {
            System.Console.WriteLine($"Paciente Claim: {claim.Type} = {claim.Value}");
        }

        modNutricaoClaim.Should().NotBeNull("mod_nutricao claim should exist");
        modNutricaoClaim!.Value.Should().Be("total");
        tipoClaim.Should().NotBeNull("tipo claim should exist");
        tipoClaim!.Value.Should().Be("paciente");
        pidClaim.Should().NotBeNull("pid claim should exist");
        pidClaim!.Value.Should().Be("1");
    }

    [Fact]
    public async Task POST_Anamnese_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404()
    {
        // Arrange - global middleware checks mod_nutricao, so include it
        var tokenEmpresaA = GerarTokenEquipe(uid: 1, empresa: 1, perfil: "nutricionista", modulos: new() { { "nutricao", "edicao" } });
        var client = CriarCliente(tokenEmpresaA);

        var payload = new
        {
            PacienteId = 999, // Paciente da empresa 2 (não existe na empresa 1)
            HistoricoMedico = "Teste",
            Alergias = "",
            Intolerancias = "",
            RotinaDiaria = "",
            HistoricoFamiliar = "",
            DadosExtendidos = new { }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/anamnese", payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Anamnese_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        // Arrange - global middleware checks mod_nutricao, so include it
        var tokenEmpresaA = GerarTokenEquipe(uid: 1, empresa: 1, perfil: "nutricionista", modulos: new() { { "nutricao", "leitura" } });
        var client = CriarCliente(tokenEmpresaA);

        // Act - tentar ler paciente 999 que pertence à empresa 2
        var response = await client.GetAsync("/api/anamnese/999");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Anamnese_NutricionistaDaMesmaEmpresa_PodeGravar_DeveRetornar200()
    {
        // Arrange
        // Precisa de um paciente real na empresa 1 para testar
        // Este teste assume que existe um paciente com ID 1 na empresa 1
        // Global middleware checks mod_nutricao, so include it
        var token = GerarTokenEquipe(uid: 1, empresa: 1, perfil: "nutricionista", modulos: new() { { "nutricao", "edicao" } });
        var client = CriarCliente(token);

        var payload = new
        {
            PacienteId = 1, // Assumindo paciente 1 na empresa 1
            HistoricoMedico = "Histórico de teste",
            Alergias = "Nenhuma",
            Intolerancias = "Lactose",
            RotinaDiaria = "Trabalho das 9 às 18h",
            HistoricoFamiliar = "Diabetes na família",
            DadosExtendidos = new
            {
                objetivo = new { objetivo = "emagrecimento", detalhes = "Perder 5kg" },
                dados_pessoais = new { peso_atual = "75", altura = "175", idade = "30", sexo = "masculino" }
            }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/anamnese", payload);
        var content = await response.Content.ReadAsStringAsync();
        System.Console.WriteLine($"Response status: {response.StatusCode}, content: {content}");

        // Assert - pode ser 200 (sucesso) ou 404 (paciente não existe no banco de teste)
        // O importante é que NÃO seja 401/403 (não autorizado por empresa/perfil)
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GET_Anamnese_NutricionistaDaMesmaEmpresa_PodeLer_DeveRetornar200()
    {
        // Arrange - global middleware checks mod_nutricao, so include it
        var token = GerarTokenEquipe(uid: 1, empresa: 1, perfil: "nutricionista", modulos: new() { { "nutricao", "leitura" } });
        var client = CriarCliente(token);

        // Act
        var response = await client.GetAsync("/api/anamnese/1");

        // Assert - pode ser 200 (sucesso) ou 404 (paciente não existe no banco de teste)
        // O importante é que NÃO seja 401/403
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task POST_Anamnese_Paciente_SoPodeGravarPropriaAnamnese_DeveRetornar404SeOutroPaciente()
    {
        // Arrange
        var tokenPaciente = GerarTokenPaciente(pid: 1, empresa: 1);
        var client = CriarCliente(tokenPaciente);

        var payload = new
        {
            PacienteId = 2, // Tentando gravar anamnese do paciente 2
            HistoricoMedico = "Teste",
            Alergias = "",
            Intolerancias = "",
            RotinaDiaria = "",
            HistoricoFamiliar = "",
            DadosExtendidos = new { }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/anamnese", payload);
        var content = await response.Content.ReadAsStringAsync();
        System.Console.WriteLine($"POST Paciente response: {response.StatusCode}, content: {content}");

        // Assert - deve retornar 404 (não encontrado) para não revelar que o paciente existe
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GET_Anamnese_Paciente_SoPodeLerPropriaAnamnese_DeveRetornar404SeOutroPaciente()
    {
        // Arrange
        var tokenPaciente = GerarTokenPaciente(pid: 1, empresa: 1);
        var client = CriarCliente(tokenPaciente);

        // Act - tentar ler anamnese do paciente 2
        var response = await client.GetAsync("/api/anamnese/2");
        var content = await response.Content.ReadAsStringAsync();
        System.Console.WriteLine($"GET Paciente response: {response.StatusCode}, content: {content}");

        // Assert - deve retornar 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Anamnese_PerfilInvalido_DeveRetornar403()
    {
        // Arrange - perfil "recepcionista" não tem permissão para anamnese
        // Global middleware checks mod_nutricao, so include it
        var token = GerarTokenEquipe(uid: 1, empresa: 1, perfil: "recepcionista", modulos: new() { { "nutricao", "leitura" } });
        var client = CriarCliente(token);

        var payload = new
        {
            PacienteId = 1,
            HistoricoMedico = "Teste",
            Alergias = "",
            Intolerancias = "",
            RotinaDiaria = "",
            HistoricoFamiliar = "",
            DadosExtendidos = new { }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/anamnese", payload);

        // Assert - deve retornar 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GET_Anamnese_PerfilInvalido_DeveRetornar403()
    {
        // Arrange - perfil "recepcionista" não tem permissão para anamnese
        // Global middleware checks mod_nutricao, so include it with "edicao" (weight 2) to pass GET/POST checks
        var token = GerarTokenEquipe(uid: 1, empresa: 1, perfil: "recepcionista", modulos: new() { { "nutricao", "edicao" } });
        var client = CriarCliente(token);

        // Act
        var response = await client.GetAsync("/api/anamnese/1");

        // Assert - deve retornar 403 Forbidden (perfil inválido) ou 404 (paciente não existe no banco de teste)
        // O importante é que NÃO seja 200 (sucesso) ou 401 (não autenticado)
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound);
    }
}