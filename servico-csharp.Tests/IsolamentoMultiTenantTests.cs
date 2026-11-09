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
}