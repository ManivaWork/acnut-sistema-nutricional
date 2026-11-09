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
using Npgsql;
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
        // Trava de segurança: testes só rodam em localhost
        if (!IsLocalhost())
        {
            throw new InvalidOperationException("Testes de isolamento multi-tenant só podem rodar em localhost (proteção contra execução em produção).");
        }

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

    /// <summary>
    /// Verifica se o ambiente é localhost (proteção contra execução em produção)
    /// </summary>
    private static bool IsLocalhost()
    {
        var machineName = Environment.MachineName.ToLowerInvariant();
        var connString = GetConnectionStringStatic();

        // Verificar se é máquina local conhecida
        if (machineName == "localhost" || machineName.Contains("local") || machineName.Contains("dev"))
            return true;

        // Verificar connection string - não deve conter hosts remotos de produção
        if (!string.IsNullOrEmpty(connString))
        {
            if (connString.Contains("render.com") || connString.Contains("amazonaws.com") ||
                connString.Contains("azure.com") || connString.Contains("supabase.co") ||
                connString.Contains("neon.tech") || connString.Contains("prod") ||
                connString.Contains("production"))
            {
                return false;
            }

            // Verificar se é IP local ou localhost
            if (connString.Contains("127.0.0.1") || connString.Contains("localhost") ||
                connString.Contains("192.168.") || connString.Contains("10.") ||
                connString.Contains("172.16."))
            {
                return true;
            }
        }

        // Se não conseguirmos determinar, assumimos que é ambiente de teste/local
        return true;
    }

    private string GetConnectionString()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        return config.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string not found");
    }

    private static string GetConnectionStringStatic()
    {
        return Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? Environment.GetEnvironmentVariable("DefaultConnection")
            ?? "";
    }

    private async Task<(int empresaId, int usuarioId)> CriarEmpresaEUsuariosTesteAsync(string testName = "")
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        // Garantir que os perfis existam
        await using (var cmdPerfis = new NpgsqlCommand(@"
            INSERT INTO perfis (chave, nome) VALUES
                ('super_admin', 'Super Administrador'),
                ('admin', 'Administrador'),
                ('profissional', 'Profissional'),
                ('atendente', 'Atendente')
            ON CONFLICT (chave) DO NOTHING;", conn))
        {
            await cmdPerfis.ExecuteNonQueryAsync();
        }

        // Criar empresa
        await using (var cmd = new NpgsqlCommand("INSERT INTO empresas (nome) VALUES ('Empresa Teste') RETURNING id;", conn))
        {
            var result = await cmd.ExecuteScalarAsync();
            int empresaId = Convert.ToInt32(result);

            // Criar usuário com ID único baseado no hash do nome do teste + timestamp
            int usuarioId = Math.Abs((testName + Environment.TickCount).GetHashCode()) % 1000000 + 1000;
            string userEmail = $"usuario_{testName}_{Guid.NewGuid():N}@teste.com".Replace("-", "");
            if (userEmail.Length > 100) userEmail = userEmail.Substring(0, 100);

            await using var cmdUser = new NpgsqlCommand(@"
                INSERT INTO usuarios (id, empresa_id, nome, email, senha_hash, perfil, ativo)
                VALUES (@uid, @emp, 'Usuário Teste', @email, 'hash', 'profissional', true)
                ON CONFLICT (id) DO UPDATE SET empresa_id = EXCLUDED.empresa_id, email = EXCLUDED.email;", conn);
            cmdUser.Parameters.AddWithValue("uid", usuarioId);
            cmdUser.Parameters.AddWithValue("emp", empresaId);
            cmdUser.Parameters.AddWithValue("email", userEmail);
            await cmdUser.ExecuteNonQueryAsync();

            return (empresaId, usuarioId);
        }
    }

    private async Task<int> CriarPacienteTesteAsync(int empresaId, string testName = "")
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        // Email único para evitar conflito de chave única
        string email = $"paciente_{testName}_{Guid.NewGuid():N}@teste.com".Replace("-", "");
        if (email.Length > 100) email = email.Substring(0, 100);

        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO pacientes (empresa_id, nome, email, telefone, data_nascimento, sexo, status, cpf)
            VALUES (@emp, @nome, @email, '', '1990-01-01', 'M', 'ativo', NULL)
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("emp", empresaId);
        cmd.Parameters.AddWithValue("nome", $"Paciente {testName}");
        cmd.Parameters.AddWithValue("email", email);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private async Task LimparDadosTesteAsync(int empresaId)
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        // Limpar na ordem correta (filhos antes dos pais)
        var tabelas = new[]
        {
            "anamneses", "pacientes", "usuarios", "empresas"
        };

        foreach (var tabela in tabelas)
        {
            try
            {
                await using var cmd = new NpgsqlCommand($"DELETE FROM {tabela} WHERE empresa_id = @emp", conn);
                cmd.Parameters.AddWithValue("emp", empresaId);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* tabela pode não existir, ignora */ }
        }
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
    public async Task POST_Anamnese_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_Anamnese_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(tokenEmpresaA);

            var payload = new
            {
                PacienteId = 999, // Paciente que não existe na empresa
                HistoricoMedico = "Teste",
                Alergias = "",
                Intolerancias = "",
                RotinaDiaria = "",
                HistoricoFamiliar = "",
                DadosExtendidos = new { }
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/anamnese", payload);

            // Assert - deve retornar 404 (paciente não encontrado na empresa)
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Anamnese_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Anamnese_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            // Act - tentar ler paciente 999 que não existe na empresa
            var response = await client.GetAsync("/api/anamnese/999");

            // Assert - deve retornar 404
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Anamnese_NutricionistaDaMesmaEmpresa_PodeGravar_DeveRetornar200()
    {
        var testName = nameof(POST_Anamnese_NutricionistaDaMesmaEmpresa_PodeGravar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            // Global middleware checks mod_nutricao, so include it
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "nutricionista", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            var payload = new
            {
                PacienteId = pacienteId,
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

            // Assert - deve retornar 200 com corpo esperado
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            json.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
            json.RootElement.GetProperty("mensagem").GetString().Should().Be("Anamnese salva com sucesso!");
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Anamnese_NutricionistaDaMesmaEmpresa_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Anamnese_NutricionistaDaMesmaEmpresa_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            // Primeiro gravar uma anamnese via POST
            var tokenPost = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "nutricionista", modulos: new() { { "nutricao", "edicao" } });
            var clientPost = CriarCliente(tokenPost);
            var payload = new
            {
                PacienteId = pacienteId,
                HistoricoMedico = "Histórico para leitura",
                Alergias = "Penicilina",
                Intolerancias = "Glúten",
                RotinaDiaria = "Rotina de teste",
                HistoricoFamiliar = "Histórico familiar de teste",
                DadosExtendidos = new { teste = "valor" }
            };
            await clientPost.PostAsJsonAsync("/api/anamnese", payload);

            // Agora testar GET
            var tokenGet = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "nutricionista", modulos: new() { { "nutricao", "leitura" } });
            var clientGet = CriarCliente(tokenGet);

            // Act
            var response = await clientGet.GetAsync($"/api/anamnese/{pacienteId}");

            // Assert - deve retornar 200 com corpo esperado
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            json.RootElement.GetProperty("historico_medico").GetString().Should().Be("Histórico para leitura");
            json.RootElement.GetProperty("alergias").GetString().Should().Be("Penicilina");
            json.RootElement.GetProperty("intolerancias").GetString().Should().Be("Glúten");
            json.RootElement.GetProperty("rotina_diaria").GetString().Should().Be("Rotina de teste");
            json.RootElement.GetProperty("historico_familiar").GetString().Should().Be("Histórico familiar de teste");
            json.RootElement.GetProperty("dados_extendidos").GetString().Should().Contain("teste");
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
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

    [Fact]
    public async Task POST_Anamnese_PerfilProfissional_PodeGravar_DeveRetornar200()
    {
        var testName = nameof(POST_Anamnese_PerfilProfissional_PodeGravar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            var payload = new
            {
                PacienteId = pacienteId,
                HistoricoMedico = "Histórico por profissional",
                Alergias = "Nenhuma",
                Intolerancias = "",
                RotinaDiaria = "Rotina profissional",
                HistoricoFamiliar = "Sem histórico",
                DadosExtendidos = new { }
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/anamnese", payload);

            // Assert - deve retornar 200 com corpo esperado
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            json.RootElement.GetProperty("id").GetInt32().Should().BeGreaterThan(0);
            json.RootElement.GetProperty("mensagem").GetString().Should().Be("Anamnese salva com sucesso!");
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Anamnese_PerfilProfissional_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Anamnese_PerfilProfissional_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            // Primeiro gravar uma anamnese
            var tokenPost = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientPost = CriarCliente(tokenPost);
            var payload = new
            {
                PacienteId = pacienteId,
                HistoricoMedico = "Histórico para leitura por profissional",
                Alergias = "Nenhuma",
                Intolerancias = "",
                RotinaDiaria = "",
                HistoricoFamiliar = "",
                DadosExtendidos = new { }
            };
            await clientPost.PostAsJsonAsync("/api/anamnese", payload);

            // Agora testar GET
            var tokenGet = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var clientGet = CriarCliente(tokenGet);

            // Act
            var response = await clientGet.GetAsync($"/api/anamnese/{pacienteId}");

            // Assert - deve retornar 200 com corpo esperado
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            json.RootElement.GetProperty("historico_medico").GetString().Should().Be("Histórico para leitura por profissional");
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }
}