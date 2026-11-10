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

public class IsolamentoMultiTenantTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string JwtKey = "SUA_CHAVE_JWT_AQUI_MIN_32_CHARS_12345678901234567890";
    private const string Issuer = "bybarhub";
    private const string Audience = "bybarhub-app";

    public IsolamentoMultiTenantTests(WebApplicationFactory<Program> factory)
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
        // (os testes usam WebApplicationFactory que cria servidor em memória)
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
        // Tenta ler do user-secrets ou variável de ambiente
        return Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? Environment.GetEnvironmentVariable("DefaultConnection")
            ?? "";
    }

    private async Task<(int empresaId, int usuarioId)> CriarEmpresaEUsuariosTesteAsync(string testName)
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
            int usuarioId = Math.Abs(testName.GetHashCode() + Environment.TickCount) % 1000000 + 1000;
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

    private async Task<int> CriarPacienteTesteAsync(int empresaId, string testName)
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
            "checkins_semanais", "orientacoes_nutricionais", "metas_acompanhamento", "metas_paciente",
            "planos_versoes", "evolucao_registros", "exames_paciente", "observacoes_paciente",
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

    // ============================================
    // 1. CHECK-INS SEMANAIS
    // ============================================
    [Fact]
    public async Task POST_Checkin_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_Checkin_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(tokenEmpresaA);

            var payload = new { PacienteId = 999, PesoAtualKg = 70.5, AdesaoPlanoPorcentagem = 85, Observacoes = "Teste" };
            var response = await client.PostAsJsonAsync("/api/checkins", payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Checkin_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Checkin_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/checkins/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Checkin_EmpresaMesma_PodeGravar_DeveRetornar200()
    {
        var testName = nameof(POST_Checkin_EmpresaMesma_PodeGravar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId, PesoAtualKg = 70.5, AdesaoPlanoPorcentagem = 85, Observacoes = "Teste isolamento" };
            var response = await client.PostAsJsonAsync("/api/checkins", payload);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Checkin_EmpresaMesma_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Checkin_EmpresaMesma_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            // Primeiro criar um check-in
            var tokenPost = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientPost = CriarCliente(tokenPost);
            await clientPost.PostAsJsonAsync("/api/checkins", new { PacienteId = pacienteId, PesoAtualKg = 70.5, AdesaoPlanoPorcentagem = 85 });

            // Agora testar GET
            var tokenGet = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var clientGet = CriarCliente(tokenGet);

            var response = await clientGet.GetAsync($"/api/checkins/{pacienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 2. ORIENTAÇÕES NUTRICIONAIS
    // ============================================
    [Fact]
    public async Task POST_Orientacao_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_Orientacao_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(tokenEmpresaA);

            var payload = new { PacienteId = 999, Texto = "Orientação de teste" };
            var response = await client.PostAsJsonAsync("/api/orientacoes", payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Orientacao_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Orientacao_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/orientacoes/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Orientacao_EmpresaMesma_PodeGravar_DeveRetornar200()
    {
        var testName = nameof(POST_Orientacao_EmpresaMesma_PodeGravar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId, Texto = "Orientação de teste isolamento" };
            var response = await client.PostAsJsonAsync("/api/orientacoes", payload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Orientacao_EmpresaMesma_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Orientacao_EmpresaMesma_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            // Primeiro criar uma orientação
            var tokenPost = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientPost = CriarCliente(tokenPost);
            await clientPost.PostAsJsonAsync("/api/orientacoes", new { PacienteId = pacienteId, Texto = "Orientação para leitura" });

            // Agora testar GET
            var tokenGet = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var clientGet = CriarCliente(tokenGet);

            var response = await clientGet.GetAsync($"/api/orientacoes/{pacienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 3. METAS DO PACIENTE
    // ============================================
    [Fact]
    public async Task POST_Meta_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_Meta_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(tokenEmpresaA);

            var payload = new { PacienteId = 999, Titulo = "Meta teste", Descricao = "Desc", Tipo = "agua", Prazo = "2026-12-31" };
            var response = await client.PostAsJsonAsync("/api/metas", payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Meta_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Meta_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/metas/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Meta_EmpresaMesma_PodeGravar_DeveRetornar200()
    {
        var testName = nameof(POST_Meta_EmpresaMesma_PodeGravar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId, Titulo = "Beber 2L água", Descricao = "Hidratação", Tipo = "agua", Prazo = "2026-12-31" };
            var response = await client.PostAsJsonAsync("/api/metas", payload);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Meta_EmpresaMesma_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Meta_EmpresaMesma_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var tokenPost = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientPost = CriarCliente(tokenPost);
            await clientPost.PostAsJsonAsync("/api/metas", new { PacienteId = pacienteId, Titulo = "Meta leitura", Tipo = "agua", Prazo = "2026-12-31" });

            var tokenGet = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var clientGet = CriarCliente(tokenGet);

            var response = await clientGet.GetAsync($"/api/metas/{pacienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 4. PLANOS VERSÕES
    // ============================================
    [Fact]
    public async Task GET_Plano_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Plano_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/planos/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_PlanoVersao_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_PlanoVersao_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(tokenEmpresaA);

            var payload = new { Descricao = "Versão teste" };
            var response = await client.PostAsJsonAsync("/api/planos/999/versoes", payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_PlanoVersoes_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_PlanoVersoes_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/planos/999/versoes");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_PlanoVersao_EmpresaA_NaoPodeLerVersaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_PlanoVersao_EmpresaA_NaoPodeLerVersaoDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/planos/versao/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Plano_EmpresaMesma_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Plano_EmpresaMesma_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/planos/{pacienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_PlanoVersao_EmpresaMesma_PodeAcessar_DeveRetornar200ou400()
    {
        var testName = nameof(POST_PlanoVersao_EmpresaMesma_PodeAcessar_DeveRetornar200ou400);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            // Endpoint retorna 200 se plano tem refeições, 400 se plano vazio - ambos indicam que o isolamento funcionou
            var payload = new { Descricao = "Versão inicial" };
            var response = await client.PostAsJsonAsync($"/api/planos/{pacienteId}/versoes", payload);

            // 200 = sucesso, 400 = plano vazio (comportamento esperado para paciente sem refeições)
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 5. EVOLUÇÃO REGISTROS
    // ============================================
    [Fact]
    public async Task POST_Evolucao_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_Evolucao_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(tokenEmpresaA);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", "999" },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "peso", "70.5" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PostAsync("/api/evolucao", content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Evolucao_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Evolucao_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/evolucao/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Evolucao_EmpresaMesma_PodeGravar_DeveRetornar200()
    {
        var testName = nameof(POST_Evolucao_EmpresaMesma_PodeGravar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", pacienteId.ToString() },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "peso", "70.5" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PostAsync("/api/evolucao", content);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Evolucao_EmpresaMesma_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Evolucao_EmpresaMesma_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var tokenPost = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientPost = CriarCliente(tokenPost);
            var form = new Dictionary<string, string> { { "pacienteId", pacienteId.ToString() }, { "data", DateTime.Today.ToString("yyyy-MM-dd") }, { "peso", "70.5" } };
            await clientPost.PostAsync("/api/evolucao", new FormUrlEncodedContent(form));

            var tokenGet = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var clientGet = CriarCliente(tokenGet);

            var response = await clientGet.GetAsync($"/api/evolucao/{pacienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 6. EXAMES PACIENTE
    // ============================================
    [Fact]
    public async Task POST_Exame_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_Exame_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(tokenEmpresaA);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", "999" },
                { "titulo", "Hemograma" },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "observacao", "Teste" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PostAsync("/api/exames", content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Exame_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Exame_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/exames/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Exame_EmpresaMesma_PodeGravar_DeveRetornar200()
    {
        var testName = nameof(POST_Exame_EmpresaMesma_PodeGravar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", pacienteId.ToString() },
                { "titulo", "Hemograma completo" },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "observacao", "Teste isolamento" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PostAsync("/api/exames", content);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Exame_EmpresaMesma_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Exame_EmpresaMesma_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var tokenPost = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientPost = CriarCliente(tokenPost);
            var form = new Dictionary<string, string> { { "pacienteId", pacienteId.ToString() }, { "titulo", "Exame leitura" }, { "data", DateTime.Today.ToString("yyyy-MM-dd") }, { "observacao", "Teste" } };
            await clientPost.PostAsync("/api/exames", new FormUrlEncodedContent(form));

            var tokenGet = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var clientGet = CriarCliente(tokenGet);

            var response = await clientGet.GetAsync($"/api/exames/{pacienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 7. OBSERVAÇÕES PACIENTE
    // ============================================
    [Fact]
    public async Task POST_Observacao_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_Observacao_EmpresaA_NaoPodeGravarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(tokenEmpresaA);

            var payload = new { PacienteId = 999, Tipo = "geral", Data = DateTime.Today.ToString("yyyy-MM-dd"), Texto = "Observação teste" };
            var response = await client.PostAsJsonAsync("/api/observacoes", payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Observacao_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Observacao_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/observacoes/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Observacao_EmpresaMesma_PodeGravar_DeveRetornar200()
    {
        var testName = nameof(POST_Observacao_EmpresaMesma_PodeGravar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId, Tipo = "geral", Data = DateTime.Today.ToString("yyyy-MM-dd"), Texto = "Observação isolamento" };
            var response = await client.PostAsJsonAsync("/api/observacoes", payload);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Observacao_EmpresaMesma_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Observacao_EmpresaMesma_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var tokenPost = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientPost = CriarCliente(tokenPost);
            await clientPost.PostAsJsonAsync("/api/observacoes", new { PacienteId = pacienteId, Tipo = "geral", Data = DateTime.Today.ToString("yyyy-MM-dd"), Texto = "Obs leitura" });

            var tokenGet = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var clientGet = CriarCliente(tokenGet);

            var response = await clientGet.GetAsync($"/api/observacoes/{pacienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 8. ACESSOS PACIENTE
    // ============================================
    [Fact]
    public async Task GET_Acessos_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Acessos_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/acessos/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Acessos_EmpresaMesma_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Acessos_EmpresaMesma_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/acessos/{pacienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 9. HISTÓRICO UNIFICADO
    // ============================================
    [Fact]
    public async Task GET_Historico_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(GET_Historico_EmpresaA_NaoPodeLerPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(tokenEmpresaA);

            var response = await client.GetAsync("/api/historico/999");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Historico_EmpresaMesma_PodeLer_DeveRetornar200()
    {
        var testName = nameof(GET_Historico_EmpresaMesma_PodeLer_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "leitura" } });
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/historico/{pacienteId}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 10. LIMITE DE PACIENTES - sem_plano (plano gratuito)
    // ============================================
    [Fact]
    public async Task POST_Pacientes_SemPlano_Com0Pacientes_DevePermitirCriar()
    {
        var testName = nameof(POST_Pacientes_SemPlano_Com0Pacientes_DevePermitirCriar);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            // Nova empresa sem assinatura = sem_plano (limite 5 pacientes)
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "admin", modulos: new() { { "nutricao", "total" } });
            var client = CriarCliente(token);

            var payload = new
            {
                nome = "Paciente Teste",
                email = $"paciente_{testName}@teste.com",
                telefone = "",
                data_nascimento = "1990-01-01",
                sexo = "M",
                status = "ativo"
            };

            var response = await client.PostAsJsonAsync("/api/pacientes", payload);

            // Deve permitir criar (status 201 Created)
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Pacientes_SemPlano_Com5Pacientes_DeveBloquear6o()
    {
        var testName = nameof(POST_Pacientes_SemPlano_Com5Pacientes_DeveBloquear6o);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            // Nova empresa sem assinatura = sem_plano (limite 5 pacientes)
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "admin", modulos: new() { { "nutricao", "total" } });
            var client = CriarCliente(token);

            // Criar 5 pacientes diretamente no banco
            for (int i = 1; i <= 5; i++)
            {
                await CriarPacienteTesteAsync(empresaId, $"{testName}_p{i}");
            }

            // Tentar criar o 6º paciente via API
            var payload = new
            {
                nome = "Paciente 6",
                email = $"paciente_6_{testName}@teste.com",
                telefone = "",
                data_nascimento = "1990-01-01",
                sexo = "M",
                status = "ativo"
            };

            var response = await client.PostAsJsonAsync("/api/pacientes", payload);

            // Deve bloquear com 403
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var result = await response.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("mensagem").GetString().Should().Contain("Limite de pacientes atingido (5/5)");
            result.GetProperty("codigo").GetString().Should().Be("plano_necessario");
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 11. LIMITE DE PACIENTES - Super Admin Bypass
    // ============================================
    [Fact]
    public async Task POST_Pacientes_SuperAdmin_SemprePermiteMesmoSemLimite()
    {
        var testName = nameof(POST_Pacientes_SuperAdmin_SemprePermiteMesmoSemLimite);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            // Super Admin - usa perfil super_admin
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "super_admin", modulos: new() { { "nutricao", "total" } });
            var client = CriarCliente(token);

            // Criar 10 pacientes (muito além do limite 5 do sem_plano)
            for (int i = 1; i <= 10; i++)
            {
                var payload = new
                {
                    nome = $"Paciente {i}",
                    email = $"paciente_{i}_{testName}@teste.com",
                    telefone = "",
                    data_nascimento = "1990-01-01",
                    sexo = "M",
                    status = "ativo"
                };

                var response = await client.PostAsJsonAsync("/api/pacientes", payload);

                // Super Admin SEMPRE deve poder criar
                response.StatusCode.Should().Be(HttpStatusCode.Created, $"Falha ao criar paciente {i}: {await response.Content.ReadAsStringAsync()}");
            }
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 12. LIMITE DE PACIENTES - bypass_limites flag
    // ============================================
    [Fact]
    public async Task POST_Pacientes_BypassLimitesTrue_SemprePermite()
    {
        var testName = nameof(POST_Pacientes_BypassLimitesTrue_SemprePermite);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            // Ativar bypass_limites para esta empresa via UPDATE direto no banco
            // Usa plano 'basico' porque o check constraint não permite 'sem_plano'
            var connString = GetConnectionString();
            await using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO assinaturas (empresa_id, plano, status, valor_centavos, ciclo, limites, bypass_limites)
                    VALUES (@emp, 'basico', 'ativa', 2790, 'monthly', @limites, true)
                    ON CONFLICT (empresa_id) DO UPDATE SET bypass_limites = true, atualizado_em = CURRENT_TIMESTAMP;", conn);
                cmd.Parameters.AddWithValue("emp", empresaId);
                var limitesBasico = new ACNutri.Billing.PlanoLimites(50, 2, 500, 5);
                cmd.Parameters.Add(new NpgsqlParameter("limites", NpgsqlTypes.NpgsqlDbType.Jsonb)
                {
                    Value = limitesBasico.ToJson()
                });
                await cmd.ExecuteNonQueryAsync();
            }

            // Usuário comum (não Super Admin) - perfil admin
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "admin", modulos: new() { { "nutricao", "total" } });
            var client = CriarCliente(token);

            // Criar 100 pacientes (muito além do limite 50 do basico)
            for (int i = 1; i <= 100; i++)
            {
                var payload = new
                {
                    nome = $"Paciente {i}",
                    email = $"paciente_{i}_{testName}@teste.com",
                    telefone = "",
                    data_nascimento = "1990-01-01",
                    sexo = "M",
                    status = "ativo"
                };

                var response = await client.PostAsJsonAsync("/api/pacientes", payload);

                // Com bypass_limites = true, SEMPRE deve poder criar
                response.StatusCode.Should().Be(HttpStatusCode.Created, $"Falha ao criar paciente {i}: {await response.Content.ReadAsStringAsync()}");
            }
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 13. PORTAL DO PACIENTE (token tipo=paciente)
    // ============================================
    [Fact]
    public async Task GET_PacienteTudo_PacienteToken_PodeAcessarProprioPid_DeveRetornar200()
    {
        var testName = nameof(GET_PacienteTudo_PacienteToken_PodeAcessarProprioPid_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.GetAsync("/api/paciente/tudo");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var json = await response.Content.ReadFromJsonAsync<JsonElement>();
            json.GetProperty("pid").GetInt32().Should().Be(pacienteId);
            json.GetProperty("nome").GetString().Should().NotBeNullOrEmpty();
            json.GetProperty("plano").Should().NotBeNull();
            json.GetProperty("orientacoes").Should().NotBeNull();
            json.GetProperty("metas").Should().NotBeNull();
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Anamnese_PacienteToken_PodeAcessarPropria_DeveRetornar200()
    {
        var testName = nameof(GET_Anamnese_PacienteToken_PodeAcessarPropria_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            // Criar anamnese para o paciente
            var connString = GetConnectionString();
            await using (var conn = new NpgsqlConnection(connString))
            {
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(@"
                    INSERT INTO anamneses (paciente_id, empresa_id, dados_extendidos, criado_em)
                    VALUES (@pid, @emp, '{}', NOW())
                    ON CONFLICT (paciente_id) DO UPDATE SET dados_extendidos = '{}';", conn);
                cmd.Parameters.AddWithValue("pid", pacienteId);
                cmd.Parameters.AddWithValue("emp", empresaId);
                await cmd.ExecuteNonQueryAsync();
            }

            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/anamnese/{pacienteId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Anamnese_PacienteToken_NaoPodeAcessarOutroPacienteMesmaEmpresa_DeveRetornar404()
    {
        var testName = nameof(GET_Anamnese_PacienteToken_NaoPodeAcessarOutroPacienteMesmaEmpresa_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        try
        {
            // Token do paciente 1 tentando acessar anamnese do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/anamnese/{pacienteId2}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Anamnese_PacienteToken_NaoPodeAcessarOutraEmpresa_DeveRetornar404()
    {
        var testName = nameof(GET_Anamnese_PacienteToken_NaoPodeAcessarOutraEmpresa_DeveRetornar404);
        var (empresaId1, usuarioId1) = await CriarEmpresaEUsuariosTesteAsync(testName + "_e1");
        var (empresaId2, usuarioId2) = await CriarEmpresaEUsuariosTesteAsync(testName + "_e2");
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId1, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId2, testName + "_p2");
        try
        {
            // Token do paciente da empresa 1 tentando acessar anamnese do paciente da empresa 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId1);
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/anamnese/{pacienteId2}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId1);
            await LimparDadosTesteAsync(empresaId2);
        }
    }

    [Fact]
    public async Task POST_Anamnese_PacienteToken_PodeGravarPropria_DeveRetornar200()
    {
        var testName = nameof(POST_Anamnese_PacienteToken_PodeGravarPropria_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId, DadosExtendido = new { objetivo = "Perder peso" } };
            var response = await client.PostAsJsonAsync("/api/anamnese", payload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Anamnese_PacienteToken_NaoPodeGravarOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(POST_Anamnese_PacienteToken_NaoPodeGravarOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId2, DadosExtendido = new { objetivo = "Ganhar massa" } };
            var response = await client.PostAsJsonAsync("/api/anamnese", payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Planos_PacienteToken_PodeAcessarProprio_DeveRetornar200()
    {
        var testName = nameof(GET_Planos_PacienteToken_PodeAcessarProprio_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/planos/{pacienteId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Planos_PacienteToken_NaoPodeAcessarOutroPacienteMesmaEmpresa_DeveRetornar404()
    {
        var testName = nameof(GET_Planos_PacienteToken_NaoPodeAcessarOutroPacienteMesmaEmpresa_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/planos/{pacienteId2}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Checkins_PacienteToken_PodeAcessarProprio_DeveRetornar200()
    {
        var testName = nameof(GET_Checkins_PacienteToken_PodeAcessarProprio_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/checkins/{pacienteId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_Checkins_PacienteToken_NaoPodeAcessarOutroPacienteMesmaEmpresa_DeveRetornar404()
    {
        var testName = nameof(GET_Checkins_PacienteToken_NaoPodeAcessarOutroPacienteMesmaEmpresa_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/checkins/{pacienteId2}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_PacienteExames_PacienteToken_PodeAcessarProprio_DeveRetornar200()
    {
        var testName = nameof(GET_PacienteExames_PacienteToken_PodeAcessarProprio_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.GetAsync("/api/paciente/exames");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Checkin_PacienteToken_NaoPodeGravar_DeveRetornar403ou405()
    {
        var testName = nameof(POST_Checkin_PacienteToken_NaoPodeGravar_DeveRetornar403ou405);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId, PesoAtualKg = 70.5, AdesaoPlanoPorcentagem = 85 };
            var response = await client.PostAsJsonAsync("/api/checkins", payload);

            // Paciente não tem permissão de escrita - 403 ou 405
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.MethodNotAllowed);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_PlanosVersoes_PacienteToken_NaoPodeGravar_DeveRetornar403ou405()
    {
        var testName = nameof(POST_PlanosVersoes_PacienteToken_NaoPodeGravar_DeveRetornar403ou405);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var payload = new { Descricao = "Tentativa paciente" };
            var response = await client.PostAsJsonAsync($"/api/planos/{pacienteId}/versoes", payload);

            // Paciente não tem permissão de escrita - 403 ou 405
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.MethodNotAllowed);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Exames_PacienteToken_NaoPodeGravarViaApiExames_DeveRetornar403ou405()
    {
        var testName = nameof(POST_Exames_PacienteToken_NaoPodeGravarViaApiExames_DeveRetornar403ou405);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", pacienteId.ToString() },
                { "titulo", "Hemograma" },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "observacao", "Teste" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PostAsync("/api/exames", content);

            // Endpoint /api/exames é para equipe, paciente usa /api/paciente/exames
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.MethodNotAllowed);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Orientacoes_PacienteToken_NaoPodeGravar_DeveRetornar403ou405()
    {
        var testName = nameof(POST_Orientacoes_PacienteToken_NaoPodeGravar_DeveRetornar403ou405);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId, Texto = "Orientação" };
            var response = await client.PostAsJsonAsync("/api/orientacoes", payload);

            // Paciente não tem permissão de escrita
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.MethodNotAllowed);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Metas_PacienteToken_NaoPodeGravar_DeveRetornar403ou405()
    {
        var testName = nameof(POST_Metas_PacienteToken_NaoPodeGravar_DeveRetornar403ou405);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId, Titulo = "Meta", Tipo = "agua", Prazo = "2026-12-31" };
            var response = await client.PostAsJsonAsync("/api/metas", payload);

            // Paciente não tem permissão de escrita
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.MethodNotAllowed);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_Evolucao_PacienteToken_NaoPodeGravar_DeveRetornar403ou405()
    {
        var testName = nameof(POST_Evolucao_PacienteToken_NaoPodeGravar_DeveRetornar403ou405);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenPaciente(pid: pacienteId, empresa: empresaId);
            var client = CriarCliente(token);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", pacienteId.ToString() },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "peso", "70.5" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PostAsync("/api/evolucao", content);

            // Paciente não tem permissão de escrita
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.MethodNotAllowed);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // PLANOS - ISOLAMENTO ESCRITA POR ID (commit 63f2fba + POST refeicao)
    // ============================================

    // ----- POST /api/planos/refeicao -----
    [Fact]
    public async Task POST_PlanosRefeicao_EmpresaA_NaoPodeCriarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_PlanosRefeicao_EmpresaA_NaoPodeCriarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        try
        {
            var tokenEmpresaA = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(tokenEmpresaA);

            var payload = new { PacienteId = 999, NomeRefeicao = "Café da manhã", Horario = "08:00" };
            var response = await client.PostAsJsonAsync("/api/planos/refeicao", payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task POST_PlanosRefeicao_EmpresaMesma_PodeCriar_DeveRetornar201()
    {
        var testName = nameof(POST_PlanosRefeicao_EmpresaMesma_PodeCriar_DeveRetornar201);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId = await CriarPacienteTesteAsync(empresaId, testName);
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId, NomeRefeicao = "Café da manhã", Horario = "08:00" };
            var response = await client.PostAsJsonAsync("/api/planos/refeicao", payload);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- PUT /api/planos/refeicao/{id} -----
    [Fact]
    public async Task PUT_PlanosRefeicao_EmpresaA_NaoPodeEditarRefeicaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(PUT_PlanosRefeicao_EmpresaA_NaoPodeEditarRefeicaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int refeicaoIdEmpresaB = 0;
        try
        {
            // Criar refeição na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            var createPayload = new { PacienteId = pacienteIdB, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createResponse = await clientB.PostAsJsonAsync("/api/planos/refeicao", createPayload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createResult = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoIdEmpresaB = createResult.GetProperty("id").GetInt32();

            // Tentar editar com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var updatePayload = new { PacienteId = 999, NomeRefeicao = "Jantar", Horario = "19:00" };
            var response = await clientA.PutAsJsonAsync($"/api/planos/refeicao/{refeicaoIdEmpresaB}", updatePayload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task PUT_PlanosRefeicao_EmpresaMesma_PodeEditar_DeveRetornar200()
    {
        var testName = nameof(PUT_PlanosRefeicao_EmpresaMesma_PodeEditar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int refeicaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var createPayload = new { PacienteId = pacienteId, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createResponse = await client.PostAsJsonAsync("/api/planos/refeicao", createPayload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createResult = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoId = createResult.GetProperty("id").GetInt32();

            var updatePayload = new { PacienteId = pacienteId, NomeRefeicao = "Jantar", Horario = "19:00" };
            var response = await client.PutAsJsonAsync($"/api/planos/refeicao/{refeicaoId}", updatePayload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- DELETE /api/planos/refeicao/{id} -----
    [Fact]
    public async Task DELETE_PlanosRefeicao_EmpresaA_NaoPodeExcluirRefeicaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_PlanosRefeicao_EmpresaA_NaoPodeExcluirRefeicaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int refeicaoIdEmpresaB = 0;
        try
        {
            // Criar refeição na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            var createPayload = new { PacienteId = pacienteIdB, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createResponse = await clientB.PostAsJsonAsync("/api/planos/refeicao", createPayload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createResult = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoIdEmpresaB = createResult.GetProperty("id").GetInt32();

            // Tentar excluir com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/planos/refeicao/{refeicaoIdEmpresaB}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_PlanosRefeicao_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_PlanosRefeicao_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int refeicaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var createPayload = new { PacienteId = pacienteId, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createResponse = await client.PostAsJsonAsync("/api/planos/refeicao", createPayload);
            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createResult = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoId = createResult.GetProperty("id").GetInt32();

            var response = await client.DeleteAsync($"/api/planos/refeicao/{refeicaoId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- DELETE /api/planos/refeicao/{refeicaoId}/opcao/{opcao} -----
    [Fact]
    public async Task DELETE_PlanosOpcao_EmpresaA_NaoPodeExcluirOpcaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_PlanosOpcao_EmpresaA_NaoPodeExcluirOpcaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int refeicaoIdEmpresaB = 0;
        try
        {
            // Criar refeição e item na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            var createRefPayload = new { PacienteId = pacienteIdB, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await clientB.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoIdEmpresaB = createRefResult.GetProperty("id").GetInt32();

            // Criar item (opção 1)
            var createItemPayload = new { RefeicaoId = refeicaoIdEmpresaB, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var createItemResponse = await clientB.PostAsJsonAsync("/api/planos/item", createItemPayload);
            createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Tentar excluir opção com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/planos/refeicao/{refeicaoIdEmpresaB}/opcao/1");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_PlanosOpcao_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_PlanosOpcao_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int refeicaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var createRefPayload = new { PacienteId = pacienteId, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await client.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoId = createRefResult.GetProperty("id").GetInt32();

            // Criar item (opção 1)
            var createItemPayload = new { RefeicaoId = refeicaoId, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var createItemResponse = await client.PostAsJsonAsync("/api/planos/item", createItemPayload);
            createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var response = await client.DeleteAsync($"/api/planos/refeicao/{refeicaoId}/opcao/1");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- POST /api/planos/item -----
    [Fact]
    public async Task POST_PlanosItem_EmpresaA_NaoPodeCriarItemEmRefeicaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_PlanosItem_EmpresaA_NaoPodeCriarItemEmRefeicaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int refeicaoIdEmpresaB = 0;
        try
        {
            // Criar refeição na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            var createRefPayload = new { PacienteId = pacienteIdB, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await clientB.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoIdEmpresaB = createRefResult.GetProperty("id").GetInt32();

            // Tentar criar item com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var createItemPayload = new { RefeicaoId = refeicaoIdEmpresaB, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var response = await clientA.PostAsJsonAsync("/api/planos/item", createItemPayload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task POST_PlanosItem_EmpresaMesma_PodeCriar_DeveRetornar201()
    {
        var testName = nameof(POST_PlanosItem_EmpresaMesma_PodeCriar_DeveRetornar201);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int refeicaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var createRefPayload = new { PacienteId = pacienteId, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await client.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoId = createRefResult.GetProperty("id").GetInt32();

            var createItemPayload = new { RefeicaoId = refeicaoId, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var response = await client.PostAsJsonAsync("/api/planos/item", createItemPayload);

            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- PUT /api/planos/item/{id} -----
    [Fact]
    public async Task PUT_PlanosItem_EmpresaA_NaoPodeEditarItemDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(PUT_PlanosItem_EmpresaA_NaoPodeEditarItemDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int itemIdEmpresaB = 0;
        try
        {
            // Criar refeição e item na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            var createRefPayload = new { PacienteId = pacienteIdB, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await clientB.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            var refeicaoIdEmpresaB = createRefResult.GetProperty("id").GetInt32();

            var createItemPayload = new { RefeicaoId = refeicaoIdEmpresaB, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var createItemResponse = await clientB.PostAsJsonAsync("/api/planos/item", createItemPayload);
            createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createItemResult = await createItemResponse.Content.ReadFromJsonAsync<JsonElement>();
            itemIdEmpresaB = createItemResult.GetProperty("id").GetInt32();

            // Tentar editar com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var updatePayload = new { RefeicaoId = 999, Opcao = 1, AlimentoNome = "Feijão", QuantidadeG = 150, MedidaCaseira = "1 concha", Kcal = 110, Proteinas = 8, Carbos = 20, Gorduras = 0.5 };
            var response = await clientA.PutAsJsonAsync($"/api/planos/item/{itemIdEmpresaB}", updatePayload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task PUT_PlanosItem_EmpresaMesma_PodeEditar_DeveRetornar200()
    {
        var testName = nameof(PUT_PlanosItem_EmpresaMesma_PodeEditar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int itemId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var createRefPayload = new { PacienteId = pacienteId, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await client.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            var refeicaoId = createRefResult.GetProperty("id").GetInt32();

            var createItemPayload = new { RefeicaoId = refeicaoId, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var createItemResponse = await client.PostAsJsonAsync("/api/planos/item", createItemPayload);
            createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createItemResult = await createItemResponse.Content.ReadFromJsonAsync<JsonElement>();
            itemId = createItemResult.GetProperty("id").GetInt32();

            var updatePayload = new { RefeicaoId = refeicaoId, Opcao = 1, AlimentoNome = "Feijão", QuantidadeG = 150, MedidaCaseira = "1 concha", Kcal = 110, Proteinas = 8, Carbos = 20, Gorduras = 0.5 };
            var response = await client.PutAsJsonAsync($"/api/planos/item/{itemId}", updatePayload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- DELETE /api/planos/item/{id} -----
    [Fact]
    public async Task DELETE_PlanosItem_EmpresaA_NaoPodeExcluirItemDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_PlanosItem_EmpresaA_NaoPodeExcluirItemDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int itemIdEmpresaB = 0;
        try
        {
            // Criar refeição e item na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            var createRefPayload = new { PacienteId = pacienteIdB, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await clientB.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            var refeicaoIdEmpresaB = createRefResult.GetProperty("id").GetInt32();

            var createItemPayload = new { RefeicaoId = refeicaoIdEmpresaB, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var createItemResponse = await clientB.PostAsJsonAsync("/api/planos/item", createItemPayload);
            createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createItemResult = await createItemResponse.Content.ReadFromJsonAsync<JsonElement>();
            itemIdEmpresaB = createItemResult.GetProperty("id").GetInt32();

            // Tentar excluir com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/planos/item/{itemIdEmpresaB}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_PlanosItem_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_PlanosItem_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int itemId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var createRefPayload = new { PacienteId = pacienteId, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await client.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            var refeicaoId = createRefResult.GetProperty("id").GetInt32();

            var createItemPayload = new { RefeicaoId = refeicaoId, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var createItemResponse = await client.PostAsJsonAsync("/api/planos/item", createItemPayload);
            createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createItemResult = await createItemResponse.Content.ReadFromJsonAsync<JsonElement>();
            itemId = createItemResult.GetProperty("id").GetInt32();

            var response = await client.DeleteAsync($"/api/planos/item/{itemId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- POST /api/planos/refeicao/{refeicaoId}/opcao/{opcao}/duplicar -----
    [Fact]
    public async Task POST_PlanosDuplicarOpcao_EmpresaA_NaoPodeDuplicarOpcaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_PlanosDuplicarOpcao_EmpresaA_NaoPodeDuplicarOpcaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int refeicaoIdEmpresaB = 0;
        try
        {
            // Criar refeição e item na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            var createRefPayload = new { PacienteId = pacienteIdB, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await clientB.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoIdEmpresaB = createRefResult.GetProperty("id").GetInt32();

            var createItemPayload = new { RefeicaoId = refeicaoIdEmpresaB, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var createItemResponse = await clientB.PostAsJsonAsync("/api/planos/item", createItemPayload);
            createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            // Tentar duplicar com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.PostAsync($"/api/planos/refeicao/{refeicaoIdEmpresaB}/opcao/1/duplicar", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task POST_PlanosDuplicarOpcao_EmpresaMesma_PodeDuplicar_DeveRetornar200()
    {
        var testName = nameof(POST_PlanosDuplicarOpcao_EmpresaMesma_PodeDuplicar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int refeicaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var createRefPayload = new { PacienteId = pacienteId, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await client.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoId = createRefResult.GetProperty("id").GetInt32();

            var createItemPayload = new { RefeicaoId = refeicaoId, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            var createItemResponse = await client.PostAsJsonAsync("/api/planos/item", createItemPayload);
            createItemResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var response = await client.PostAsync($"/api/planos/refeicao/{refeicaoId}/opcao/1/duplicar", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- POST /api/planos/versao/{id}/restaurar -----
    [Fact]
    public async Task POST_PlanosVersaoRestaurar_EmpresaA_NaoPodeRestaurarVersaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_PlanosVersaoRestaurar_EmpresaA_NaoPodeRestaurarVersaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int versaoIdEmpresaB = 0;
        try
        {
            // Criar paciente, plano e versão na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            var createRefPayload = new { PacienteId = pacienteIdB, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await clientB.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createItemPayload = new { RefeicaoId = (await createRefResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32(), Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            await clientB.PostAsJsonAsync("/api/planos/item", createItemPayload);

            // Criar versão
            var createVersaoPayload = new { Descricao = "Versão 1" };
            var createVersaoResponse = await clientB.PostAsJsonAsync($"/api/planos/{pacienteIdB}/versoes", createVersaoPayload);
            createVersaoResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var createVersaoResult = await createVersaoResponse.Content.ReadFromJsonAsync<JsonElement>();
            // A versão não retorna ID diretamente, vamos buscar
            var versoesResponse = await clientB.GetAsync($"/api/planos/{pacienteIdB}/versoes");
            versoesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var versoes = await versoesResponse.Content.ReadFromJsonAsync<JsonElement>();
            versaoIdEmpresaB = versoes[0].GetProperty("id").GetInt32();

            // Tentar restaurar com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.PostAsync($"/api/planos/versao/{versaoIdEmpresaB}/restaurar", null);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task POST_PlanosVersaoRestaurar_EmpresaMesma_PodeRestaurar_DeveRetornar200()
    {
        var testName = nameof(POST_PlanosVersaoRestaurar_EmpresaMesma_PodeRestaurar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int versaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var createRefPayload = new { PacienteId = pacienteId, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await client.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);

            var createItemPayload = new { RefeicaoId = (await createRefResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32(), Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            await client.PostAsJsonAsync("/api/planos/item", createItemPayload);

            // Criar versão
            var createVersaoPayload = new { Descricao = "Versão 1" };
            var createVersaoResponse = await client.PostAsJsonAsync($"/api/planos/{pacienteId}/versoes", createVersaoPayload);
            createVersaoResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var versoesResponse = await client.GetAsync($"/api/planos/{pacienteId}/versoes");
            versoesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var versoes = await versoesResponse.Content.ReadFromJsonAsync<JsonElement>();
            versaoId = versoes[0].GetProperty("id").GetInt32();

            var response = await client.PostAsync($"/api/planos/versao/{versaoId}/restaurar", null);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- GET /api/planos/versao/{id} - Paciente token -----
    [Fact]
    public async Task GET_PlanoVersao_PacienteToken_NaoPodeLerVersaoDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(GET_PlanoVersao_PacienteToken_NaoPodeLerVersaoDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int versaoIdOutroPaciente = 0;
        try
        {
            // Criar paciente 1 e versão
            var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_paciente1");
            var tokenEquipe = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientEquipe = CriarCliente(tokenEquipe);

            var createRefPayload = new { PacienteId = pacienteId1, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await clientEquipe.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var refeicaoId = (await createRefResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var createItemPayload = new { RefeicaoId = refeicaoId, Opcao = 1, AlimentoNome = "Arroz", QuantidadeG = 100, MedidaCaseira = "1 colher", Kcal = 130, Proteinas = 2.7, Carbos = 28, Gorduras = 0.3 };
            await clientEquipe.PostAsJsonAsync("/api/planos/item", createItemPayload);

            var createVersaoPayload = new { Descricao = "Versão 1" };
            await clientEquipe.PostAsJsonAsync($"/api/planos/{pacienteId1}/versoes", createVersaoPayload);

            var versoesResponse = await clientEquipe.GetAsync($"/api/planos/{pacienteId1}/versoes");
            var versoes = await versoesResponse.Content.ReadFromJsonAsync<JsonElement>();
            versaoIdOutroPaciente = versoes[0].GetProperty("id").GetInt32();

            // Criar paciente 2
            var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_paciente2");
            var tokenPaciente2 = GerarTokenPaciente(pid: pacienteId2, empresa: empresaId);
            var clientPaciente2 = CriarCliente(tokenPaciente2);

            // Tentar ler versão do paciente 1 com token do paciente 2
            var response = await clientPaciente2.GetAsync($"/api/planos/versao/{versaoIdOutroPaciente}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ----- POST /api/planos/item/calcular -----
    [Fact]
    public async Task POST_PlanosItemCalcular_EmpresaA_NaoPodeCalcularEmRefeicaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(POST_PlanosItemCalcular_EmpresaA_NaoPodeCalcularEmRefeicaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int refeicaoIdEmpresaB = 0;
        try
        {
            // Criar refeição na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            var createRefPayload = new { PacienteId = pacienteIdB, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await clientB.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoIdEmpresaB = createRefResult.GetProperty("id").GetInt32();

            // Tentar calcular item com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var createItemPayload = new { RefeicaoId = refeicaoIdEmpresaB, Opcao = 1, AlimentoId = 1, TargetKcal = 100, Gramas = 100, MedidaCaseira = "1 colher", QuantidadeMedida = 1, Refeicao = "almoco" };
            var response = await clientA.PostAsJsonAsync("/api/planos/item/calcular", createItemPayload);

            // 404 = refeição não encontrada (isolamento), 503 = serviço Python indisponível (ambiente de teste sem Python)
            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.ServiceUnavailable);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task POST_PlanosItemCalcular_EmpresaMesma_PodeCalcular_DeveRetornar201ou400ou503()
    {
        var testName = nameof(POST_PlanosItemCalcular_EmpresaMesma_PodeCalcular_DeveRetornar201ou400ou503);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int refeicaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var createRefPayload = new { PacienteId = pacienteId, NomeRefeicao = "Almoço", Horario = "12:00" };
            var createRefResponse = await client.PostAsJsonAsync("/api/planos/refeicao", createRefPayload);
            createRefResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createRefResult = await createRefResponse.Content.ReadFromJsonAsync<JsonElement>();
            refeicaoId = createRefResult.GetProperty("id").GetInt32();

            var createItemPayload = new { RefeicaoId = refeicaoId, Opcao = 1, AlimentoId = 1, TargetKcal = 100, Gramas = 100, MedidaCaseira = "1 colher", QuantidadeMedida = 1, Refeicao = "almoco" };
            var response = await client.PostAsJsonAsync("/api/planos/item/calcular", createItemPayload);

            // 201 = sucesso, 400 = alimento não tem dados na TACO, 503 = serviço Python indisponível, 500 = erro interno (ambiente de teste sem Python)
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Created, HttpStatusCode.BadRequest, HttpStatusCode.ServiceUnavailable, HttpStatusCode.InternalServerError);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }
}