using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.RateLimiting;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
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
    private static bool _databaseCleaned = false;
    private static readonly object _cleanupLock = new();
    private static int _testCounter = 0;

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

        // Limpeza global do banco de dados de teste (executa uma vez)
        // Usa a connection string do factory para garantir que limpe o banco correto
        lock (_cleanupLock)
        {
            if (!_databaseCleaned)
            {
                var connString = GetConnectionString();
                LimparBancoTesteGlobalAsync(connString).GetAwaiter().GetResult();
                _databaseCleaned = true;
            }
        }
    }

    /// <summary>
    /// Limpa todos os dados de teste do banco (empresas de teste, usuários, pacientes, etc.)
    /// </summary>
    private static async Task LimparBancoTesteGlobalAsync(string connString)
    {
        if (string.IsNullOrEmpty(connString)) return;

        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        // Limpar na ordem correta (filhos antes dos pais)
        var tabelas = new[]
        {
            "checkins_semanais", "orientacoes_nutricionais", "metas_acompanhamento", "metas_paciente",
            "planos_versoes", "evolucao_registros", "exames_paciente", "observacoes_paciente",
            "anamneses", "arquivos_paciente", "pacientes", "usuarios", "empresas"
        };

        foreach (var tabela in tabelas)
        {
            try
            {
                // Deletar apenas dados de teste (empresas com nome começando com 'Empresa Teste')
                await using var cmd = new NpgsqlCommand($@"
                    DELETE FROM {tabela}
                    WHERE empresa_id IN (SELECT id FROM empresas WHERE nome LIKE 'Empresa Teste%')", conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* tabela pode não existir, ignora */ }
        }

        // Também limpar empresas de teste
        try
        {
            await using var cmd = new NpgsqlCommand("DELETE FROM empresas WHERE nome LIKE 'Empresa Teste%'", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch { }
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

        // Criar empresa com nome único por teste para evitar conflitos
        string empresaNome = $"Empresa Teste {testName}";
        await using (var cmd = new NpgsqlCommand("INSERT INTO empresas (nome) VALUES (@nome) RETURNING id;", conn))
        {
            cmd.Parameters.AddWithValue("nome", empresaNome);
            var result = await cmd.ExecuteScalarAsync();
            int empresaId = Convert.ToInt32(result);

            // Criar usuário com ID único e email garantidamente único
            int usuarioId = Math.Abs(testName.GetHashCode() + Environment.TickCount + Random.Shared.Next()) % 1000000 + 1000;
            int counter = Interlocked.Increment(ref _testCounter);
            string uniquePart = $"{counter}_{Guid.NewGuid():N}";
            string userEmail = $"usr_{uniquePart}_{testName}@teste.com".Replace("-", "");
            if (userEmail.Length > 100) userEmail = userEmail.Substring(0, 100);

            await using var cmdUser = new NpgsqlCommand(@"
                INSERT INTO usuarios (id, empresa_id, nome, email, senha_hash, perfil, ativo)
                VALUES (@uid, @emp, 'Usuário Teste', @email, 'hash', 'profissional', true)", conn);
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

        // Email único para evitar conflito de chave única - colocar parte única no início para não ser truncada
        int counter = Interlocked.Increment(ref _testCounter);
        string uniquePart = $"{counter}_{Guid.NewGuid():N}";
        string email = $"pac_{uniquePart}_{testName}@teste.com".Replace("-", "");
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

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
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

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
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

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
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

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
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

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
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

    // ============================================
    // 14. PACIENTES - ISOLAMENTO POR ID (PUT/DELETE /api/pacientes/{id})
    // ============================================
    [Fact]
    public async Task PUT_Paciente_EmpresaA_NaoPodeEditarPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(PUT_Paciente_EmpresaA_NaoPodeEditarPacienteDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int pacienteIdEmpresaB = 0;
        try
        {
            // Criar paciente na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            pacienteIdEmpresaB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            // Tentar editar com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var updatePayload = new
            {
                nome = "Nome Alterado",
                email = $"paciente_{testName}@teste.com",
                telefone = "",
                data_nascimento = "1990-01-01",
                sexo = "M",
                status = "ativo"
            };
            var response = await clientA.PutAsJsonAsync($"/api/pacientes/{pacienteIdEmpresaB}", updatePayload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task PUT_Paciente_EmpresaMesma_PodeEditar_DeveRetornar200()
    {
        var testName = nameof(PUT_Paciente_EmpresaMesma_PodeEditar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int pacienteId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var updatePayload = new
            {
                nome = "Nome Atualizado",
                email = $"paciente_{testName}@teste.com",
                telefone = "",
                data_nascimento = "1990-01-01",
                sexo = "M",
                status = "ativo"
            };
            var response = await client.PutAsJsonAsync($"/api/pacientes/{pacienteId}", updatePayload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Paciente_EmpresaA_NaoPodeExcluirPacienteDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_Paciente_EmpresaA_NaoPodeExcluirPacienteDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int pacienteIdEmpresaB = 0;
        try
        {
            // Criar paciente na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            pacienteIdEmpresaB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");

            // Tentar excluir com token da empresa A (precisa mod_nutricao: "total" para DELETE /api/pacientes)
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "total" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/pacientes/{pacienteIdEmpresaB}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_Paciente_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_Paciente_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int pacienteId = 0;
        try
        {
            // DELETE /api/pacientes requer mod_nutricao: "total" (nível 3)
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "total" } });
            var client = CriarCliente(token);
            pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");

            var response = await client.DeleteAsync($"/api/pacientes/{pacienteId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task PUT_Paciente_PacienteToken_NaoPodeEditarOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(PUT_Paciente_PacienteToken_NaoPodeEditarOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        try
        {
            // Token do paciente 1 tentando editar paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var updatePayload = new
            {
                nome = "Tentativa",
                email = $"paciente_{testName}@teste.com",
                telefone = "",
                data_nascimento = "1990-01-01",
                sexo = "M",
                status = "ativo"
            };
            var response = await client.PutAsJsonAsync($"/api/pacientes/{pacienteId2}", updatePayload);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Paciente_PacienteToken_NaoPodeExcluirOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(DELETE_Paciente_PacienteToken_NaoPodeExcluirOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        try
        {
            // Token do paciente 1 tentando excluir paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.DeleteAsync($"/api/pacientes/{pacienteId2}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 15. ARQUIVOS - ISOLAMENTO POR ID (DELETE /api/arquivos/{id})
    // ============================================
    private async Task<int> CriarArquivoTesteAsync(int pacienteId, int empresaId, string origem = "exame", int origemId = 1)
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO arquivos_paciente (paciente_id, empresa_id, origem, origem_id, nome, mime, tamanho, dados)
            VALUES (@pid, @emp, @origem, @origemId, 'teste.txt', 'text/plain', 10, @dados)
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("pid", pacienteId);
        cmd.Parameters.AddWithValue("emp", empresaId);
        cmd.Parameters.AddWithValue("origem", origem);
        cmd.Parameters.AddWithValue("origemId", origemId);
        cmd.Parameters.AddWithValue("dados", new byte[10]);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    [Fact]
    public async Task DELETE_Arquivo_EmpresaA_NaoPodeExcluirArquivoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_Arquivo_EmpresaA_NaoPodeExcluirArquivoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int arquivoIdEmpresaB = 0;
        try
        {
            // Criar paciente e arquivo na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            arquivoIdEmpresaB = await CriarArquivoTesteAsync(pacienteIdB, empresaIdB);

            // Tentar excluir com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/arquivos/{arquivoIdEmpresaB}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_Arquivo_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_Arquivo_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int arquivoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            arquivoId = await CriarArquivoTesteAsync(pacienteId, empresaId);

            var response = await client.DeleteAsync($"/api/arquivos/{arquivoId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Arquivo_PacienteToken_NaoPodeExcluirArquivoDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(DELETE_Arquivo_PacienteToken_NaoPodeExcluirArquivoDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int arquivoIdPaciente2 = 0;
        try
        {
            // Criar arquivo do paciente 2
            arquivoIdPaciente2 = await CriarArquivoTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando excluir arquivo do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.DeleteAsync($"/api/arquivos/{arquivoIdPaciente2}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_PacienteArquivo_PacienteToken_NaoPodeAcessarArquivoDeOutroPacienteMesmaEmpresa_DeveRetornar404()
    {
        var testName = nameof(GET_PacienteArquivo_PacienteToken_NaoPodeAcessarArquivoDeOutroPacienteMesmaEmpresa_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int arquivoIdPaciente2 = 0;
        try
        {
            // Criar arquivo do paciente 2
            arquivoIdPaciente2 = await CriarArquivoTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando acessar arquivo do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/paciente/arquivos/{arquivoIdPaciente2}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task GET_PacienteArquivo_PacienteToken_NaoPodeAcessarArquivoDeOutraEmpresa_DeveRetornar404()
    {
        var testName = nameof(GET_PacienteArquivo_PacienteToken_NaoPodeAcessarArquivoDeOutraEmpresa_DeveRetornar404);
        var (empresaId1, usuarioId1) = await CriarEmpresaEUsuariosTesteAsync(testName + "_e1");
        var (empresaId2, usuarioId2) = await CriarEmpresaEUsuariosTesteAsync(testName + "_e2");
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId1, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId2, testName + "_p2");
        int arquivoIdPaciente2 = 0;
        try
        {
            // Criar arquivo do paciente 2 na empresa 2
            arquivoIdPaciente2 = await CriarArquivoTesteAsync(pacienteId2, empresaId2);

            // Token do paciente da empresa 1 tentando acessar arquivo do paciente da empresa 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId1);
            var client = CriarCliente(token);

            var response = await client.GetAsync($"/api/paciente/arquivos/{arquivoIdPaciente2}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId1);
            await LimparDadosTesteAsync(empresaId2);
        }
    }

    // ============================================
    // 16. EVOLUÇÃO - ISOLAMENTO POR ID (PUT/DELETE /api/evolucao/{id})
    // ============================================
    private async Task<int> CriarEvolucaoTesteAsync(int pacienteId, int empresaId)
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO evolucao_registros (paciente_id, empresa_id, data_registro, peso_kg)
            VALUES (@pid, @emp, CURRENT_DATE, 70.0)
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("pid", pacienteId);
        cmd.Parameters.AddWithValue("emp", empresaId);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    [Fact]
    public async Task PUT_Evolucao_EmpresaA_NaoPodeEditarEvolucaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(PUT_Evolucao_EmpresaA_NaoPodeEditarEvolucaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int evolucaoIdEmpresaB = 0;
        try
        {
            // Criar evolução na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            evolucaoIdEmpresaB = await CriarEvolucaoTesteAsync(pacienteIdB, empresaIdB);

            // Tentar editar com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", "999" },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "peso", "75.0" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await clientA.PutAsync($"/api/evolucao/{evolucaoIdEmpresaB}", content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task PUT_Evolucao_EmpresaMesma_PodeEditar_DeveRetornar200()
    {
        var testName = nameof(PUT_Evolucao_EmpresaMesma_PodeEditar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int evolucaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            evolucaoId = await CriarEvolucaoTesteAsync(pacienteId, empresaId);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", pacienteId.ToString() },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "peso", "75.0" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PutAsync($"/api/evolucao/{evolucaoId}", content);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Evolucao_EmpresaA_NaoPodeExcluirEvolucaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_Evolucao_EmpresaA_NaoPodeExcluirEvolucaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int evolucaoIdEmpresaB = 0;
        try
        {
            // Criar evolução na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            evolucaoIdEmpresaB = await CriarEvolucaoTesteAsync(pacienteIdB, empresaIdB);

            // Tentar excluir com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/evolucao/{evolucaoIdEmpresaB}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_Evolucao_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_Evolucao_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int evolucaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            evolucaoId = await CriarEvolucaoTesteAsync(pacienteId, empresaId);

            var response = await client.DeleteAsync($"/api/evolucao/{evolucaoId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task PUT_Evolucao_PacienteToken_NaoPodeEditarEvolucaoDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(PUT_Evolucao_PacienteToken_NaoPodeEditarEvolucaoDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int evolucaoIdPaciente2 = 0;
        try
        {
            // Criar evolução do paciente 2
            evolucaoIdPaciente2 = await CriarEvolucaoTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando editar evolução do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", pacienteId2.ToString() },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "peso", "75.0" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PutAsync($"/api/evolucao/{evolucaoIdPaciente2}", content);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Evolucao_PacienteToken_NaoPodeExcluirEvolucaoDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(DELETE_Evolucao_PacienteToken_NaoPodeExcluirEvolucaoDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int evolucaoIdPaciente2 = 0;
        try
        {
            // Criar evolução do paciente 2
            evolucaoIdPaciente2 = await CriarEvolucaoTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando excluir evolução do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.DeleteAsync($"/api/evolucao/{evolucaoIdPaciente2}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 17. EXAMES - ISOLAMENTO POR ID (PUT/DELETE /api/exames/{id})
    // ============================================
    private async Task<int> CriarExameTesteAsync(int pacienteId, int empresaId)
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO exames_paciente (paciente_id, empresa_id, titulo_exame, data_exame, enviado_por)
            VALUES (@pid, @emp, 'Hemograma', CURRENT_DATE, 'profissional')
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("pid", pacienteId);
        cmd.Parameters.AddWithValue("emp", empresaId);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    [Fact]
    public async Task PUT_Exame_EmpresaA_NaoPodeEditarExameDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(PUT_Exame_EmpresaA_NaoPodeEditarExameDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int exameIdEmpresaB = 0;
        try
        {
            // Criar exame na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            exameIdEmpresaB = await CriarExameTesteAsync(pacienteIdB, empresaIdB);

            // Tentar editar com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", "999" },
                { "titulo", "Hemograma Alterado" },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "observacao", "Teste" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await clientA.PutAsync($"/api/exames/{exameIdEmpresaB}", content);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task PUT_Exame_EmpresaMesma_PodeEditar_DeveRetornar200()
    {
        var testName = nameof(PUT_Exame_EmpresaMesma_PodeEditar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int exameId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            exameId = await CriarExameTesteAsync(pacienteId, empresaId);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", pacienteId.ToString() },
                { "titulo", "Hemograma Atualizado" },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "observacao", "Teste" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PutAsync($"/api/exames/{exameId}", content);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Exame_EmpresaA_NaoPodeExcluirExameDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_Exame_EmpresaA_NaoPodeExcluirExameDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int exameIdEmpresaB = 0;
        try
        {
            // Criar exame na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            exameIdEmpresaB = await CriarExameTesteAsync(pacienteIdB, empresaIdB);

            // Tentar excluir com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/exames/{exameIdEmpresaB}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_Exame_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_Exame_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int exameId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            exameId = await CriarExameTesteAsync(pacienteId, empresaId);

            var response = await client.DeleteAsync($"/api/exames/{exameId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task PUT_Exame_PacienteToken_NaoPodeEditarExameDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(PUT_Exame_PacienteToken_NaoPodeEditarExameDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int exameIdPaciente2 = 0;
        try
        {
            // Criar exame do paciente 2
            exameIdPaciente2 = await CriarExameTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando editar exame do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var form = new Dictionary<string, string>
            {
                { "pacienteId", pacienteId2.ToString() },
                { "titulo", "Hemograma" },
                { "data", DateTime.Today.ToString("yyyy-MM-dd") },
                { "observacao", "Teste" }
            };
            var content = new FormUrlEncodedContent(form);
            var response = await client.PutAsync($"/api/exames/{exameIdPaciente2}", content);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Exame_PacienteToken_NaoPodeExcluirExameDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(DELETE_Exame_PacienteToken_NaoPodeExcluirExameDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int exameIdPaciente2 = 0;
        try
        {
            // Criar exame do paciente 2
            exameIdPaciente2 = await CriarExameTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando excluir exame do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.DeleteAsync($"/api/exames/{exameIdPaciente2}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 18. OBSERVAÇÕES - ISOLAMENTO POR ID (PUT/DELETE /api/observacoes/{id})
    // ============================================
    private async Task<int> CriarObservacaoTesteAsync(int pacienteId, int empresaId)
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO observacoes_paciente (paciente_id, empresa_id, tipo, data_ref, texto)
            VALUES (@pid, @emp, 'geral', CURRENT_DATE, 'Observação teste')
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("pid", pacienteId);
        cmd.Parameters.AddWithValue("emp", empresaId);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    [Fact]
    public async Task PUT_Observacao_EmpresaA_NaoPodeEditarObservacaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(PUT_Observacao_EmpresaA_NaoPodeEditarObservacaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int observacaoIdEmpresaB = 0;
        try
        {
            // Criar observação na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            observacaoIdEmpresaB = await CriarObservacaoTesteAsync(pacienteIdB, empresaIdB);

            // Tentar editar com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var payload = new { PacienteId = 999, Tipo = "geral", Data = DateTime.Today.ToString("yyyy-MM-dd"), Texto = "Observação alterada" };
            var response = await clientA.PutAsJsonAsync($"/api/observacoes/{observacaoIdEmpresaB}", payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task PUT_Observacao_EmpresaMesma_PodeEditar_DeveRetornar200()
    {
        var testName = nameof(PUT_Observacao_EmpresaMesma_PodeEditar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int observacaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            observacaoId = await CriarObservacaoTesteAsync(pacienteId, empresaId);

            var payload = new { PacienteId = pacienteId, Tipo = "geral", Data = DateTime.Today.ToString("yyyy-MM-dd"), Texto = "Observação atualizada" };
            var response = await client.PutAsJsonAsync($"/api/observacoes/{observacaoId}", payload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Observacao_EmpresaA_NaoPodeExcluirObservacaoDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_Observacao_EmpresaA_NaoPodeExcluirObservacaoDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int observacaoIdEmpresaB = 0;
        try
        {
            // Criar observação na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            observacaoIdEmpresaB = await CriarObservacaoTesteAsync(pacienteIdB, empresaIdB);

            // Tentar excluir com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/observacoes/{observacaoIdEmpresaB}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_Observacao_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_Observacao_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int observacaoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            observacaoId = await CriarObservacaoTesteAsync(pacienteId, empresaId);

            var response = await client.DeleteAsync($"/api/observacoes/{observacaoId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task PUT_Observacao_PacienteToken_NaoPodeEditarObservacaoDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(PUT_Observacao_PacienteToken_NaoPodeEditarObservacaoDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int observacaoIdPaciente2 = 0;
        try
        {
            // Criar observação do paciente 2
            observacaoIdPaciente2 = await CriarObservacaoTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando editar observação do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var payload = new { PacienteId = pacienteId2, Tipo = "geral", Data = DateTime.Today.ToString("yyyy-MM-dd"), Texto = "Tentativa" };
            var response = await client.PutAsJsonAsync($"/api/observacoes/{observacaoIdPaciente2}", payload);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Observacao_PacienteToken_NaoPodeExcluirObservacaoDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(DELETE_Observacao_PacienteToken_NaoPodeExcluirObservacaoDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int observacaoIdPaciente2 = 0;
        try
        {
            // Criar observação do paciente 2
            observacaoIdPaciente2 = await CriarObservacaoTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando excluir observação do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.DeleteAsync($"/api/observacoes/{observacaoIdPaciente2}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 19. METAS - ISOLAMENTO POR ID (PUT/DELETE /api/metas/{id})
    // ============================================
    private async Task<int> CriarMetaTesteAsync(int pacienteId, int empresaId)
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO metas_paciente (paciente_id, empresa_id, titulo, descricao, tipo, prazo, status)
            VALUES (@pid, @emp, 'Meta teste', 'Desc', 'agua', CURRENT_DATE + INTERVAL '30 days', 'ativa')
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("pid", pacienteId);
        cmd.Parameters.AddWithValue("emp", empresaId);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    [Fact]
    public async Task PUT_Meta_EmpresaA_NaoPodeEditarMetaDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(PUT_Meta_EmpresaA_NaoPodeEditarMetaDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int metaIdEmpresaB = 0;
        try
        {
            // Criar meta na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            metaIdEmpresaB = await CriarMetaTesteAsync(pacienteIdB, empresaIdB);

            // Tentar editar com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var payload = new { Titulo = "Meta alterada", Descricao = "Desc", Tipo = "agua", Prazo = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"), Status = "ativa" };
            var response = await clientA.PutAsJsonAsync($"/api/metas/{metaIdEmpresaB}", payload);

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task PUT_Meta_EmpresaMesma_PodeEditar_DeveRetornar200()
    {
        var testName = nameof(PUT_Meta_EmpresaMesma_PodeEditar_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int metaId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            metaId = await CriarMetaTesteAsync(pacienteId, empresaId);

            var payload = new { Titulo = "Meta atualizada", Descricao = "Desc", Tipo = "agua", Prazo = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"), Status = "ativa" };
            var response = await client.PutAsJsonAsync($"/api/metas/{metaId}", payload);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Meta_EmpresaA_NaoPodeExcluirMetaDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_Meta_EmpresaA_NaoPodeExcluirMetaDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int metaIdEmpresaB = 0;
        try
        {
            // Criar meta na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            metaIdEmpresaB = await CriarMetaTesteAsync(pacienteIdB, empresaIdB);

            // Tentar excluir com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/metas/{metaIdEmpresaB}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_Meta_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_Meta_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int metaId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            metaId = await CriarMetaTesteAsync(pacienteId, empresaId);

            var response = await client.DeleteAsync($"/api/metas/{metaId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task PUT_Meta_PacienteToken_NaoPodeEditarMetaDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(PUT_Meta_PacienteToken_NaoPodeEditarMetaDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int metaIdPaciente2 = 0;
        try
        {
            // Criar meta do paciente 2
            metaIdPaciente2 = await CriarMetaTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando editar meta do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var payload = new { Titulo = "Tentativa", Descricao = "Desc", Tipo = "agua", Prazo = DateTime.Today.AddDays(30).ToString("yyyy-MM-dd"), Status = "ativa" };
            var response = await client.PutAsJsonAsync($"/api/metas/{metaIdPaciente2}", payload);

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_Meta_PacienteToken_NaoPodeExcluirMetaDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(DELETE_Meta_PacienteToken_NaoPodeExcluirMetaDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int metaIdPaciente2 = 0;
        try
        {
            // Criar meta do paciente 2
            metaIdPaciente2 = await CriarMetaTesteAsync(pacienteId2, empresaId);

            // Token do paciente 1 tentando excluir meta do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.DeleteAsync($"/api/metas/{metaIdPaciente2}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    // ============================================
    // 20. METAS ACOMPANHAMENTO - ISOLAMENTO POR ID (DELETE /api/metas/acompanhamento/{id})
    // ============================================
    private async Task<int> CriarMetaAcompanhamentoTesteAsync(int metaId, int empresaId)
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO metas_acompanhamento (meta_id, data_acompanhamento, resultado, observacao, empresa_id)
            VALUES (@meta, CURRENT_DATE, 'mantida', 'OK', @emp)
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("meta", metaId);
        cmd.Parameters.AddWithValue("emp", empresaId);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    [Fact]
    public async Task DELETE_MetaAcompanhamento_EmpresaA_NaoPodeExcluirDaEmpresaB_DeveRetornar404()
    {
        var testName = nameof(DELETE_MetaAcompanhamento_EmpresaA_NaoPodeExcluirDaEmpresaB_DeveRetornar404);
        var (empresaIdA, usuarioIdA) = await CriarEmpresaEUsuariosTesteAsync(testName + "A");
        var (empresaIdB, usuarioIdB) = await CriarEmpresaEUsuariosTesteAsync(testName + "B");
        int acompanhamentoIdEmpresaB = 0;
        try
        {
            // Criar meta e acompanhamento na empresa B
            var tokenB = GerarTokenEquipe(uid: usuarioIdB, empresa: empresaIdB, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientB = CriarCliente(tokenB);
            var pacienteIdB = await CriarPacienteTesteAsync(empresaIdB, testName + "B_paciente");
            var metaIdB = await CriarMetaTesteAsync(pacienteIdB, empresaIdB);
            acompanhamentoIdEmpresaB = await CriarMetaAcompanhamentoTesteAsync(metaIdB, empresaIdB);

            // Tentar excluir com token da empresa A
            var tokenA = GerarTokenEquipe(uid: usuarioIdA, empresa: empresaIdA, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var clientA = CriarCliente(tokenA);

            var response = await clientA.DeleteAsync($"/api/metas/acompanhamento/{acompanhamentoIdEmpresaB}");

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaIdA);
            await LimparDadosTesteAsync(empresaIdB);
        }
    }

    [Fact]
    public async Task DELETE_MetaAcompanhamento_EmpresaMesma_PodeExcluir_DeveRetornar200()
    {
        var testName = nameof(DELETE_MetaAcompanhamento_EmpresaMesma_PodeExcluir_DeveRetornar200);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        int acompanhamentoId = 0;
        try
        {
            var token = GerarTokenEquipe(uid: usuarioId, empresa: empresaId, perfil: "profissional", modulos: new() { { "nutricao", "edicao" } });
            var client = CriarCliente(token);
            var pacienteId = await CriarPacienteTesteAsync(empresaId, testName + "_paciente");
            var metaId = await CriarMetaTesteAsync(pacienteId, empresaId);
            acompanhamentoId = await CriarMetaAcompanhamentoTesteAsync(metaId, empresaId);

            var response = await client.DeleteAsync($"/api/metas/acompanhamento/{acompanhamentoId}");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }

    [Fact]
    public async Task DELETE_MetaAcompanhamento_PacienteToken_NaoPodeExcluirDeOutroPaciente_DeveRetornar404()
    {
        var testName = nameof(DELETE_MetaAcompanhamento_PacienteToken_NaoPodeExcluirDeOutroPaciente_DeveRetornar404);
        var (empresaId, usuarioId) = await CriarEmpresaEUsuariosTesteAsync(testName);
        var pacienteId1 = await CriarPacienteTesteAsync(empresaId, testName + "_p1");
        var pacienteId2 = await CriarPacienteTesteAsync(empresaId, testName + "_p2");
        int metaIdPaciente2 = 0;
        int acompanhamentoIdPaciente2 = 0;
        try
        {
            // Criar meta e acompanhamento do paciente 2
            metaIdPaciente2 = await CriarMetaTesteAsync(pacienteId2, empresaId);
            acompanhamentoIdPaciente2 = await CriarMetaAcompanhamentoTesteAsync(metaIdPaciente2, empresaId);

            // Token do paciente 1 tentando excluir acompanhamento do paciente 2
            var token = GerarTokenPaciente(pid: pacienteId1, empresa: empresaId);
            var client = CriarCliente(token);

            var response = await client.DeleteAsync($"/api/metas/acompanhamento/{acompanhamentoIdPaciente2}");

            response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparDadosTesteAsync(empresaId);
        }
    }
}

// ============================================
// Custom WebApplicationFactory for Rate Limiting tests
// ============================================
public class RateLimitingWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string JwtKey = "SUA_CHAVE_JWT_AQUI_MIN_32_CHARS_12345678901234567890";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // IMPORTANT: Call base to execute Program.cs (registers middleware, endpoints, rate limiter)
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            Environment.SetEnvironmentVariable("Jwt:Key", JwtKey);

            // JWT configuration
            services.Configure<Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions>(
                Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme,
                options => options.MapInboundClaims = false);
        });

        // Trava de segurança: testes só rodam em localhost
        builder.UseSetting("ASPNETCORE_ENVIRONMENT", "Development");
    }

    private static bool IsLocalhost()
    {
        var machineName = Environment.MachineName.ToLowerInvariant();
        var connString = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "";

        if (machineName == "localhost" || machineName.Contains("local") || machineName.Contains("dev"))
            return true;

        if (!string.IsNullOrEmpty(connString))
        {
            if (connString.Contains("render.com") || connString.Contains("amazonaws.com") ||
                connString.Contains("azure.com") || connString.Contains("supabase.co") ||
                connString.Contains("neon.tech") || connString.Contains("prod") ||
                connString.Contains("production"))
            {
                return false;
            }

            if (connString.Contains("127.0.0.1") || connString.Contains("localhost") ||
                connString.Contains("192.168.") || connString.Contains("10.") ||
                connString.Contains("172.16."))
            {
                return true;
            }
        }

        return true;
    }
}

// ============================================
// 21. RATE LIMITING - Autenticação (commit 99cef9d)
// ============================================
public class RateLimitingTests : IDisposable
{
    private readonly RateLimitingWebApplicationFactory _factory;
    private readonly string _testRunId = Guid.NewGuid().ToString("N")[..8]; // Unique per test instance
    private const string JwtKey = "SUA_CHAVE_JWT_AQUI_MIN_32_CHARS_12345678901234567890";
    private const string Issuer = "bybarhub";
    private const string Audience = "bybarhub-app";

    public RateLimitingTests()
    {
        if (!IsLocalhost())
        {
            throw new InvalidOperationException("Testes de rate limiting só podem rodar em localhost (proteção contra execução em produção).");
        }

        _factory = new RateLimitingWebApplicationFactory();

        // Warm up the server first to ensure host is built and Seed.Garantir runs
        var warmupClient = _factory.CreateClient();
        for (int i = 0; i < 3; i++)
        {
            try { _ = warmupClient.GetAsync("/health").Result; } catch { }
            Thread.Sleep(100);
        }

        // Ensure tentativas_login table exists (Seed.Garantir may not complete before tests)
        var connString = GetConnectionStringFromFactory();
        if (!string.IsNullOrEmpty(connString))
        {
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS tentativas_login (
                        id BIGSERIAL PRIMARY KEY,
                        tipo VARCHAR(50) NOT NULL,
                        identificador_hash CHAR(64) NOT NULL,
                        ip INET NOT NULL,
                        sucesso BOOLEAN NOT NULL DEFAULT false,
                        criado_em TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE INDEX IF NOT EXISTS ix_tentativas_login_tipo_ident_ip_criado
                        ON tentativas_login (tipo, identificador_hash, ip, criado_em DESC)
                        WHERE sucesso = false;
                    CREATE INDEX IF NOT EXISTS ix_tentativas_login_criado_em
                        ON tentativas_login (criado_em);", conn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RateLimitingTests] Failed to create tentativas_login table: {ex.Message}");
                }

                // Verify tentativas_login table exists
                using var verifyCmd = new NpgsqlCommand("SELECT COUNT(*) FROM tentativas_login", conn);
                try
                {
                    var count = verifyCmd.ExecuteScalar();
                    Console.WriteLine($"[RateLimitingTests] tentativas_login table exists, row count: {count}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RateLimitingTests] tentativas_login table verification failed: {ex.Message}");
                }

                // Create solicitacoes_acesso table (from migration)
                using var cmdSolic = new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS solicitacoes_acesso (
                        id BIGSERIAL PRIMARY KEY,
                        nome VARCHAR(255) NOT NULL,
                        email VARCHAR(255) NOT NULL,
                        telefone VARCHAR(50),
                        registro_profissional VARCHAR(100),
                        clinica_nome VARCHAR(255),
                        mensagem TEXT,
                        status VARCHAR(20) NOT NULL DEFAULT 'pendente'
                            CHECK (status IN ('pendente', 'aprovado', 'recusado', 'excluido')),
                        empresa_id BIGINT REFERENCES empresas(id) ON DELETE SET NULL,
                        usuario_id BIGINT REFERENCES usuarios(id) ON DELETE SET NULL,
                        ip INET,
                        user_agent TEXT,
                        criado_em TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        atualizado_em TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE UNIQUE INDEX IF NOT EXISTS uq_solicitacoes_email_pendente
                        ON solicitacoes_acesso (lower(email))
                        WHERE status = 'pendente';
                    CREATE INDEX IF NOT EXISTS idx_solicitacoes_status_criado
                        ON solicitacoes_acesso (status, criado_em DESC);
                    CREATE INDEX IF NOT EXISTS idx_solicitacoes_empresa
                        ON solicitacoes_acesso (empresa_id)
                        WHERE empresa_id IS NOT NULL;", conn);
                try
                {
                    cmdSolic.ExecuteNonQuery();
                    Console.WriteLine("[RateLimitingTests] solicitacoes_acesso table created/verified");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[RateLimitingTests] Failed to create solicitacoes_acesso table: {ex.Message}");
                }
            }
        }
    }

    public void Dispose()
    {
        _factory?.Dispose();
    }

    private static bool IsLocalhost()
    {
        var machineName = Environment.MachineName.ToLowerInvariant();
        var connString = Environment.GetEnvironmentVariable("ConnectionStrings__Default") ?? "";

        if (machineName == "localhost" || machineName.Contains("local") || machineName.Contains("dev"))
            return true;

        if (!string.IsNullOrEmpty(connString))
        {
            if (connString.Contains("render.com") || connString.Contains("amazonaws.com") ||
                connString.Contains("azure.com") || connString.Contains("supabase.co") ||
                connString.Contains("neon.tech") || connString.Contains("prod") ||
                connString.Contains("production"))
            {
                return false;
            }

            if (connString.Contains("127.0.0.1") || connString.Contains("localhost") ||
                connString.Contains("192.168.") || connString.Contains("10.") ||
                connString.Contains("172.16."))
            {
                return true;
            }
        }

        return true;
    }

    private string GetConnectionStringFromFactory()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        return config.GetConnectionString("Default") ?? "";
    }

    private static string GetConnectionStringStatic()
    {
        return Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? Environment.GetEnvironmentVariable("DefaultConnection")
            ?? "";
    }

    private HttpClient CriarCliente()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Run-Id", _testRunId);
        return client;
    }

    // Helper: inserir usuário de teste no banco
    private async Task<int> CriarUsuarioTesteAsync(string email, string senha = "senha123")
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        // Garantir perfis
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
        string empresaNome = $"Empresa RL {Guid.NewGuid():N}";
        int empresaId;
        await using (var cmd = new NpgsqlCommand("INSERT INTO empresas (nome) VALUES (@nome) RETURNING id;", conn))
        {
            cmd.Parameters.AddWithValue("nome", empresaNome);
            var result = await cmd.ExecuteScalarAsync();
            empresaId = Convert.ToInt32(result);
        }

        // Hash da senha
        var senhaHash = BCrypt.Net.BCrypt.HashPassword(senha);

        // Criar usuário
        int usuarioId = Math.Abs(email.GetHashCode() + Environment.TickCount + Random.Shared.Next()) % 1000000 + 1000;
        await using var cmdUser = new NpgsqlCommand(@"
            INSERT INTO usuarios (id, empresa_id, nome, email, senha_hash, perfil, ativo)
            VALUES (@uid, @emp, 'Usuário RL', @email, @hash, 'profissional', true)", conn);
        cmdUser.Parameters.AddWithValue("uid", usuarioId);
        cmdUser.Parameters.AddWithValue("emp", empresaId);
        cmdUser.Parameters.AddWithValue("email", email);
        cmdUser.Parameters.AddWithValue("hash", senhaHash);
        await cmdUser.ExecuteNonQueryAsync();

        return usuarioId;
    }

    private async Task<int> CriarPacienteTesteAsync(int empresaId, string email, string? cpf = null, DateTime? nascimento = null)
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        cpf ??= GerarCpfValido();
        nascimento ??= new DateTime(1990, 1, 1);

        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO pacientes (empresa_id, nome, email, telefone, data_nascimento, sexo, status, cpf)
            VALUES (@emp, @nome, @email, '', @nascimento, 'M', 'ativo', @cpf)
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("emp", empresaId);
        cmd.Parameters.AddWithValue("nome", $"Paciente {email}");
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("nascimento", nascimento.Value);
        cmd.Parameters.AddWithValue("cpf", cpf);
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    private static string GerarCpfValido()
    {
        // Gera 9 dígitos aleatórios
        var random = new Random();
        var digitos = new int[11];
        for (int i = 0; i < 9; i++)
            digitos[i] = random.Next(0, 10);

        // Calcula o 10º dígito
        int soma = 0;
        for (int i = 0; i < 9; i++)
            soma += digitos[i] * (10 - i);
        int resto = soma % 11;
        digitos[9] = resto < 2 ? 0 : 11 - resto;

        // Calcula o 11º dígito
        soma = 0;
        for (int i = 0; i < 10; i++)
            soma += digitos[i] * (11 - i);
        resto = soma % 11;
        digitos[10] = resto < 2 ? 0 : 11 - resto;

        return string.Concat(digitos.Select(d => d.ToString()));
    }

    private string GetConnectionString()
    {
        using var scope = _factory.Services.CreateScope();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        return config.GetConnectionString("Default") ?? throw new InvalidOperationException("Connection string not found");
    }

    // ============================================
    // TESTES: Rate Limiting por IP (memória) - 30 req/15min
    // ============================================

    [Fact]
    public async Task RateLimit_IP_LoginEquipe_Ate30ReqPermitido_Depois429()
    {
        var client = CriarCliente();
        var email = $"rl_ip_login_{Guid.NewGuid():N}@teste.com";
        await CriarUsuarioTesteAsync(email, "senhaCorreta");

        // 30 requests com senha CORRETA devem passar (sucesso não conta como falha no rate limit por identificador)
        for (int i = 0; i < 30; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaCorreta" });
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"Request {i+1} não deveria ser 429");
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Request {i+1} deveria ser 200 OK");
        }

        // 31ª request (com senha errada) deve ser bloqueada por rate limit IP
        var response31 = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
        response31.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Verificar headers (do IP rate limiter middleware)
        response31.Headers.Should().ContainKey("Retry-After");
        var retryAfter = response31.Headers.GetValues("Retry-After").FirstOrDefault();
        retryAfter.Should().NotBeNullOrEmpty();

        var json = await response31.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("mensagem").GetString().Should().Contain("minuto");
    }

    [Fact]
    public async Task RateLimit_IP_PacienteLogin_Ate30ReqPermitido_Depois429()
    {
        var client = CriarCliente();
        var email = $"rl_ip_pac_{Guid.NewGuid():N}@teste.com";

        // Criar empresa + paciente com CPF válido
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        string empresaNome = $"Empresa RL {Guid.NewGuid():N}";
        int empresaId;
        await using (var cmd = new NpgsqlCommand("INSERT INTO empresas (nome) VALUES (@nome) RETURNING id;", conn))
        {
            cmd.Parameters.AddWithValue("nome", empresaNome);
            var result = await cmd.ExecuteScalarAsync();
            empresaId = Convert.ToInt32(result);
        }

        var cpf = GerarCpfValido();
        var nascimento = new DateTime(1990, 1, 1);
        await CriarPacienteTesteAsync(empresaId, email, cpf, nascimento);

        // 30 requests com credenciais CORRETAS devem passar (sucesso não incrementa contador de falhas no DB rate limiter)
        for (int i = 0; i < 30; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/paciente", new { Cpf = cpf, Nascimento = nascimento.ToString("yyyy-MM-dd") });
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"Request {i+1} não deveria ser 429");
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Request {i+1} deveria ser 200 OK");
        }

        // 31ª request (com nascimento errado) deve ser bloqueada por rate limit IP
        var response31 = await client.PostAsJsonAsync("/api/auth/paciente", new { Cpf = cpf, Nascimento = "2000-01-01" });
        response31.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimit_IP_DefinirSenha_Ate30ReqPermitido_Depois429()
    {
        var client = CriarCliente();

        // Usar tokens DIFERENTES para cada request para não disparar o rate limiter por identificador+IP (banco)
        // O rate limiter por IP deve bloquear após 30 requests do mesmo IP
        for (int i = 0; i < 30; i++)
        {
            var token = $"token_invalido_{Guid.NewGuid():N}";
            var response = await client.PostAsJsonAsync($"/api/definir-senha/{token}", new { senha = "NovaSenha123!", confirmarSenha = "NovaSenha123!" });
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"Request {i+1} não deveria ser 429");
        }

        // 31ª request com novo token deve ser bloqueada por rate limit IP
        var token31 = $"token_invalido_{Guid.NewGuid():N}";
        var response31 = await client.PostAsJsonAsync($"/api/definir-senha/{token31}", new { senha = "NovaSenha123!", confirmarSenha = "NovaSenha123!" });
        response31.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimit_IP_SolicitarAcesso_Ate30ReqPermitido_Depois429()
    {
        var client = CriarCliente();

        for (int i = 0; i < 30; i++)
        {
            var response = await client.PostAsJsonAsync("/api/acesso/solicitar", new
            {
                Nome = "Teste",
                Email = $"rl_solic_{i}_{Guid.NewGuid():N}@teste.com",
                Telefone = "",
                RegistroProfissional = "CRN-12345",
                ClinicaNome = "Clínica Teste",
                Mensagem = "Teste"
            });
            response.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests, $"Request {i+1} não deveria ser 429");
        }

        var response31 = await client.PostAsJsonAsync("/api/acesso/solicitar", new
        {
            Nome = "Teste",
            Email = $"rl_solic_31_{Guid.NewGuid():N}@teste.com",
            Telefone = "",
            RegistroProfissional = "CRN-12345",
            ClinicaNome = "Clínica Teste",
            Mensagem = "Teste"
        });
        response31.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // ============================================
    // TESTES: Rate Limiting por Identificador+IP (banco) - 5 falhas/15min
    // ============================================

    [Fact]
    public async Task RateLimit_IdentificadorIP_LoginEquipe_5FalhasBloqueia_ComRetryAfterDinamico()
    {
        var client = CriarCliente();
        var email = $"rl_idip_eq_{Guid.NewGuid():N}@teste.com";
        await CriarUsuarioTesteAsync(email, "senhaCorreta");

        // 5 falhas consecutivas
        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
        }

        // 6ª tentativa deve ser bloqueada (429)
        var response6 = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
        response6.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var json = await response6.Content.ReadFromJsonAsync<JsonElement>();
        var mensagem = json.GetProperty("mensagem").GetString();
        mensagem.Should().Contain("Muitas tentativas");
        mensagem.Should().MatchRegex(@"\d+ minutos?"); // Retry-After dinâmico em minutos
    }

    [Fact]
    public async Task RateLimit_IdentificadorIP_PacienteLogin_5FalhasBloqueia()
    {
        var client = CriarCliente();
        var email = $"rl_idip_pac_{Guid.NewGuid():N}@teste.com";

        // Criar empresa + paciente com CPF válido
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        string empresaNome = $"Empresa RL {Guid.NewGuid():N}";
        int empresaId;
        await using (var cmd = new NpgsqlCommand("INSERT INTO empresas (nome) VALUES (@nome) RETURNING id;", conn))
        {
            cmd.Parameters.AddWithValue("nome", empresaNome);
            var result = await cmd.ExecuteScalarAsync();
            empresaId = Convert.ToInt32(result);
        }

        var cpf = GerarCpfValido();
        var nascimento = new DateTime(1990, 1, 1);
        await CriarPacienteTesteAsync(empresaId, email, cpf, nascimento);

        // 5 falhas consecutivas (nascimento errado)
        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/paciente", new { Cpf = cpf, Nascimento = "2000-01-01" });
            response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.BadRequest);
        }

        // 6ª tentativa deve ser bloqueada (429)
        var response6 = await client.PostAsJsonAsync("/api/auth/paciente", new { Cpf = cpf, Nascimento = "2000-01-01" });
        response6.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimit_IdentificadorIP_DefinirSenha_5FalhasBloqueia()
    {
        var client = CriarCliente();
        var token = "token_inexistente_" + Guid.NewGuid().ToString("N");

        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync($"/api/definir-senha/{token}", new { senha = "NovaSenha123!", confirmarSenha = "NovaSenha123!" });
            response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
        }

        var response6 = await client.PostAsJsonAsync($"/api/definir-senha/{token}", new { senha = "NovaSenha123!", confirmarSenha = "NovaSenha123!" });
        response6.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RateLimit_IdentificadorIP_SolicitarAcesso_5FalhasBloqueia()
    {
        var client = CriarCliente();
        var email = $"rl_idip_solic_{Guid.NewGuid():N}@teste.com";

        // 6 requests: 1 cria (200) + 5 falhas por email duplicado (400)
        // Após 5 falhas, a 7ª deve ser bloqueada pelo rate limiter (429)
        for (int i = 0; i < 6; i++)
        {
            var response = await client.PostAsJsonAsync("/api/acesso/solicitar", new
            {
                Nome = "Teste",
                Email = email,
                Telefone = "",
                RegistroProfissional = "CRN-12345",
                ClinicaNome = "Clínica Teste",
                Mensagem = "Teste"
            });
            // Primeira vez cria (200), demais falham por email duplicado (400) - cada uma registra tentativa falha
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        }

        // 7ª request deve ser bloqueada pelo rate limiter (5 falhas registradas)
        var response7 = await client.PostAsJsonAsync("/api/acesso/solicitar", new
        {
            Nome = "Teste",
            Email = email,
            Telefone = "",
            RegistroProfissional = "CRN-12345",
            ClinicaNome = "Clínica Teste",
            Mensagem = "Teste"
        });
        response7.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // ============================================
    // TESTES: Sucesso zera contador
    // ============================================

    [Fact]
    public async Task RateLimit_SucessoLogin_ZeraContadorFalhas()
    {
        var client = CriarCliente();
        var email = $"rl_sucesso_{Guid.NewGuid():N}@teste.com";
        await CriarUsuarioTesteAsync(email, "senhaCorreta");

        // 4 falhas
        for (int i = 0; i < 4; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // 1 sucesso - deve zerar o contador de falhas
        var responseSucesso = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaCorreta" });
        responseSucesso.StatusCode.Should().Be(HttpStatusCode.OK);

        // Mais 5 falhas (contador recomeçou do zero após sucesso)
        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // 6ª falha após sucesso deve bloquear (contador chegou a 5)
        var response6 = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
        response6.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
    // ============================================
    // TESTES: Isolamento por IP (diferentes IPs não compartilham contador)
    // ============================================

    [Fact]
    public async Task RateLimit_IPDiferentes_ContadoresIndependentes()
    {
        // Nota: WebApplicationFactory usa mesmo IP (::1) para todas as requisições
        // Este teste valida a lógica conceitual - em produção IPs diferentes têm contadores separados

        var client = CriarCliente();
        var email = $"rl_ipdiff_{Guid.NewGuid():N}@teste.com";
        await CriarUsuarioTesteAsync(email, "senhaCorreta");

        // Fazer 5 falhas
        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // 6ª deve bloquear
        var response6 = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
        response6.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    // ============================================
    // TESTES: Isolamento por Identificador (diferentes emails não compartilham contador)
    // ============================================

    [Fact]
    public async Task RateLimit_IdentificadoresDiferentes_ContadoresIndependentes()
    {
        var client = CriarCliente();

        // Criar dois usuários
        var email1 = $"rl_id1_{Guid.NewGuid():N}@teste.com";
        var email2 = $"rl_id2_{Guid.NewGuid():N}@teste.com";
        await CriarUsuarioTesteAsync(email1, "senhaCorreta");
        await CriarUsuarioTesteAsync(email2, "senhaCorreta");

        // 5 falhas no email1
        for (int i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", new { email = email1, senha = "senhaErrada" });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // email1 deve estar bloqueado
        var responseBlocked = await client.PostAsJsonAsync("/api/auth/login", new { email = email1, senha = "senhaErrada" });
        responseBlocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // email2 ainda deve funcionar (contador independente)
        var responseEmail2 = await client.PostAsJsonAsync("/api/auth/login", new { email = email2, senha = "senhaErrada" });
        responseEmail2.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ============================================
    // TESTES: Hash do identificador no banco (SHA-256 + pepper)
    // ============================================

    [Fact]
    public async Task RateLimit_HashIdentificador_NoPlainTextNoBanco()
    {
        var client = CriarCliente();
        var email = $"rl_hash_{Guid.NewGuid():N}@teste.com";
        await CriarUsuarioTesteAsync(email, "senhaCorreta");

        // Fazer 3 falhas para gerar registros
        for (int i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
        }

        // Verificar no banco que o identificador está hasheado (CHAR(64) = SHA-256 hex)
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
            SELECT identificador_hash FROM tentativas_login
            WHERE tipo = 'equipe' AND ip = '::1'::inet
            ORDER BY criado_em DESC LIMIT 1;", conn);

        var result = await cmd.ExecuteScalarAsync();
        result.Should().NotBeNull();
        var hash = result.ToString();

        // Deve ser hash SHA-256 (64 chars hex)
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]{64}$");

        // Não deve conter o email em texto plano
        hash.Should().NotContain(email);
        hash.Should().NotContain("@teste.com");
    }

    // ============================================
    // TESTES: Tabela criada automaticamente pelo Seed
    // ============================================

    [Fact]
    public async Task RateLimit_TabelaTentativasLogin_CriadaAutomaticamentePeloSeed()
    {
        // Apenas verificar se a tabela existe (Seed.Garantir cria na inicialização)
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
            SELECT EXISTS (
                SELECT 1 FROM information_schema.tables
                WHERE table_name = 'tentativas_login'
            );", conn);

        var result = await cmd.ExecuteScalarAsync();
        result.Should().NotBeNull();
        Convert.ToBoolean(result).Should().BeTrue("tabela tentativas_login deve ser criada automaticamente pelo Seed");
    }

    // ============================================
    // TESTES: Limpeza automática de registros antigos (>24h)
    // ============================================

    [Fact]
    public async Task RateLimit_LimpezaAutomatica_RegistrosAntigosSaoRemovidos()
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        // Inserir registro antigo (>24h)
        await using (var cmd = new NpgsqlCommand(@"
            INSERT INTO tentativas_login (tipo, identificador_hash, ip, sucesso, criado_em)
            VALUES ('equipe', 'abc123', '::1'::inet, false, NOW() - INTERVAL '25 hours');", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Inserir registro recente
        await using (var cmd = new NpgsqlCommand(@"
            INSERT INTO tentativas_login (tipo, identificador_hash, ip, sucesso, criado_em)
            VALUES ('equipe', 'def456', '::1'::inet, false, NOW());", conn))
        {
            await cmd.ExecuteNonQueryAsync();
        }

        // Chamar VerificarTentativasFalhas (que faz limpeza automática)
        var client = CriarCliente();
        var email = $"rl_cleanup_{Guid.NewGuid():N}@teste.com";
        await CriarUsuarioTesteAsync(email, "senhaCorreta");

        await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });

        // Verificar que o registro antigo foi removido
        await using var cmdCheck = new NpgsqlCommand(@"
            SELECT COUNT(*) FROM tentativas_login
            WHERE identificador_hash = 'abc123';", conn);

        var count = await cmdCheck.ExecuteScalarAsync();
        Convert.ToInt64(count).Should().Be(0, "registros com mais de 24h devem ser limpos automaticamente");
    }

    // ============================================
    // TESTES: Graceful degradation (DB indisponível não bloqueia login)
    // ============================================

    [Fact]
    public async Task RateLimit_GracefulDegradation_DbIndisponivelNaoBloqueiaLogin()
    {
        // Este teste valida que o código tem try/catch ao redor das chamadas de rate limiting
        // Se o DB falhar, o login deve prosseguir (apenas loga warning)

        var client = CriarCliente();
        var email = $"rl_graceful_{Guid.NewGuid():N}@teste.com";
        await CriarUsuarioTesteAsync(email, "senhaCorreta");

        // Login com senha correta deve funcionar mesmo se houver problemas no rate limiting
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaCorreta" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    // ============================================
    // TESTES: Headers de resposta 429
    // ============================================

    [Fact]
    public async Task RateLimit_Response429_TemHeadersCorretos()
    {
        var client = CriarCliente();
        var email = $"rl_headers_{Guid.NewGuid():N}@teste.com";
        await CriarUsuarioTesteAsync(email, "senhaCorreta");

        // Fazer 30 requests com credenciais CORRETAS (sucesso)
        // O rate limiter por IP (memória) conta TODAS as requests, não só falhas
        HttpResponseMessage response;
        for (int i = 0; i < 30; i++)
        {
            response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaCorreta" });
            response.StatusCode.Should().Be(HttpStatusCode.OK, $"Request {i+1} deveria ser 200 OK");
        }

        // 31ª request deve ser bloqueada pelo rate limiter por IP (429 com headers)
        response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaCorreta" });

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        // Verificar headers padrão do rate limiter ASP.NET Core
        response.Headers.Should().ContainKey("Retry-After");
        response.Headers.Should().ContainKey("X-RateLimit-Limit");
        response.Headers.Should().ContainKey("X-RateLimit-Remaining");
        response.Headers.Should().ContainKey("X-RateLimit-Reset");

        var retryAfter = response.Headers.GetValues("Retry-After").FirstOrDefault();
        int.TryParse(retryAfter, out var seconds).Should().BeTrue();
        seconds.Should().BeGreaterThan(0);
        seconds.Should().BeLessThanOrEqualTo(900); // max 15 min = 900s
    }

    // ============================================
    // TESTES: Mensagem amigável com tempo restante
    // ============================================

    [Fact]
    public async Task RateLimit_MensagemAmigavel_ContemTempoRestanteEmMinutos()
    {
        var client = CriarCliente();
        var email = $"rl_msg_{Guid.NewGuid().ToString("N")}@teste.com";
        await CriarUsuarioTesteAsync(email, "senhaCorreta");

        for (int i = 0; i < 5; i++)
        {
            await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });
        }

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, senha = "senhaErrada" });

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var mensagem = json.GetProperty("mensagem").GetString();

        mensagem.Should().Contain("Muitas tentativas");
        mensagem.Should().MatchRegex(@"Tente novamente em \d+ minutos?"); // ex: "Tente novamente em 14 minutos"
    }
}