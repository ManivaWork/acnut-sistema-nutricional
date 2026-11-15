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

public class AcessosApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private const string JwtKey = "SUA_CHAVE_JWT_AQUI_MIN_32_CHARS_12345678901234567890";
    private const string Issuer = "bybarhub";
    private const string Audience = "bybarhub-app";

    public AcessosApiTests(WebApplicationFactory<Program> factory)
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

    private string GerarTokenSuperAdmin(int uid = 1)
    {
        var claims = new List<Claim>
        {
            new("uid", uid.ToString()),
            new("empresa", "1"),
            new("perfil", "super_admin"),
            new("tipo", "equipe"),
            // mod_nutricao com peso "total" (3) para passar a validação do middleware (DELETE requer peso >= 2)
            new("mod_nutricao", "total")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims, expires: DateTime.UtcNow.AddHours(1), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GerarTokenComum(int uid = 1, int empresa = 1, string perfil = "profissional")
    {
        var claims = new List<Claim>
        {
            new("uid", uid.ToString()),
            new("empresa", empresa.ToString()),
            new("perfil", perfil),
            new("tipo", "equipe")
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

    private async Task<long> CriarSolicitacaoPendenteAsync(long? empresaId = null, long? usuarioId = null, string email = "")
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        string uniqueEmail = string.IsNullOrEmpty(email)
            ? $"solicitacao_{Guid.NewGuid():N}@teste.com".Replace("-", "")
            : email;

        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO solicitacoes_acesso (nome, email, telefone, registro_profissional, clinica_nome, status, empresa_id, usuario_id)
            VALUES (@nome, @email, '', '', '', 'pendente', @emp, @user)
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("nome", "Solicitante Teste");
        cmd.Parameters.AddWithValue("email", uniqueEmail);
        cmd.Parameters.AddWithValue("emp", empresaId.HasValue ? (object)empresaId.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("user", usuarioId.HasValue ? (object)usuarioId.Value : DBNull.Value);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private async Task<long> CriarSolicitacaoAprovadaAsync(long empresaId, long usuarioId, string email = "")
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        string uniqueEmail = string.IsNullOrEmpty(email)
            ? $"solicitacao_{Guid.NewGuid():N}@teste.com".Replace("-", "")
            : email;

        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO solicitacoes_acesso (nome, email, telefone, registro_profissional, clinica_nome, status, empresa_id, usuario_id)
            VALUES (@nome, @email, '', '', '', 'aprovado', @emp, @user)
            RETURNING id;", conn);
        cmd.Parameters.AddWithValue("nome", "Solicitante Aprovado");
        cmd.Parameters.AddWithValue("email", uniqueEmail);
        cmd.Parameters.AddWithValue("emp", empresaId);
        cmd.Parameters.AddWithValue("user", usuarioId);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt64(result);
    }

    private async Task LimparSolicitacoesTesteAsync()
    {
        var connString = GetConnectionString();
        await using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand(@"
            DELETE FROM solicitacoes_acesso
            WHERE email LIKE '%@teste.com' OR email LIKE 'solicitacao_%';", conn);
        await cmd.ExecuteNonQueryAsync();
    }

    // ============================================
    // TESTES DELETE /api/acessos/{id}
    // ============================================

    [Fact]
    public async Task DELETE_Acessos_SuperAdmin_ExcluiSolicitacaoPendenteSemEmpresaUsuario_DeveRetornar200()
    {
        // Arrange - criar solicitação pendente SEM empresa_id e usuario_id (como a do Adriano)
        var solicitacaoId = await CriarSolicitacaoPendenteAsync(empresaId: null, usuarioId: null);
        var token = GerarTokenSuperAdmin();
        var client = CriarCliente(token);

        try
        {
            // Act
            var response = await client.DeleteAsync($"/api/acessos/{solicitacaoId}");

            // Assert - deve retornar 200
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadAsStringAsync();
            System.Console.WriteLine($"[DIAG] Response content: {content}");

            // Verificar que foi realmente excluída do banco
            var connString = GetConnectionString();
            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            // DIAGNÓSTICO: Verificar RLS, triggers, policies na tabela
            await using var cmdDiag = new NpgsqlCommand(@"
                SELECT
                    c.relname as table_name,
                    c.relrowsecurity as rls_enabled,
                    c.relforcerowsecurity as rls_forced
                FROM pg_class c
                WHERE c.relname = 'solicitacoes_acesso';", conn);
            using var rdDiag = await cmdDiag.ExecuteReaderAsync();
            if (await rdDiag.ReadAsync())
            {
                System.Console.WriteLine($"[DIAG] Table: {rdDiag.GetString(0)}, RLS enabled: {rdDiag.GetBoolean(1)}, RLS forced: {rdDiag.GetBoolean(2)}");
            }
            await rdDiag.CloseAsync();

            // DIAGNÓSTICO: Verificar triggers na tabela
            await using var cmdTriggers = new NpgsqlCommand(@"
                SELECT
                    t.tgname as trigger_name,
                    p.proname as function_name,
                    t.tgenabled as enabled,
                    pg_get_triggerdef(t.oid) as trigger_def
                FROM pg_trigger t
                JOIN pg_proc p ON t.tgfoid = p.oid
                JOIN pg_class c ON t.tgrelid = c.oid
                WHERE c.relname = 'solicitacoes_acesso'
                AND NOT t.tgisinternal;", conn);
            using var rdTriggers = await cmdTriggers.ExecuteReaderAsync();
            while (await rdTriggers.ReadAsync())
            {
                var enabledChar = rdTriggers.GetChar(2);
                System.Console.WriteLine($"[DIAG] Trigger: {rdTriggers.GetString(0)}, Function: {rdTriggers.GetString(1)}, Enabled: {enabledChar}");
                System.Console.WriteLine($"[DIAG] Trigger Def: {rdTriggers.GetString(3)}");
            }
            await rdTriggers.CloseAsync();

            // DIAGNÓSTICO: Verificar definição da função do trigger
            await using var cmdTriggerFunc = new NpgsqlCommand(@"
                SELECT pg_get_functiondef(p.oid) as func_def
                FROM pg_proc p
                WHERE p.proname = 'fn_solicitacoes_atualizado_em';", conn);
            using var rdTriggerFunc = await cmdTriggerFunc.ExecuteReaderAsync();
            if (await rdTriggerFunc.ReadAsync())
            {
                System.Console.WriteLine($"[DIAG] Function def: {rdTriggerFunc.GetString(0)}");
            }
            await rdTriggerFunc.CloseAsync();

            // DIAGNÓSTICO: Verificar RULES na tabela (Podem reescrever DELETE)
            await using var cmdRules = new NpgsqlCommand(@"
                SELECT rulename, definition
                FROM pg_rules
                WHERE schemaname = 'public' AND tablename = 'solicitacoes_acesso';", conn);
            using var rdRules = await cmdRules.ExecuteReaderAsync();
            while (await rdRules.ReadAsync())
            {
                System.Console.WriteLine($"[DIAG] Rule: {rdRules.GetString(0)}, Def: {rdRules.GetString(1)}");
            }
            await rdRules.CloseAsync();

            // DIAGNÓSTICO: Verificar FK constraints na tabela
            await using var cmdFks = new NpgsqlCommand(@"
                SELECT
                    tc.constraint_name,
                    tc.table_name,
                    kcu.column_name,
                    ccu.table_name AS foreign_table_name,
                    ccu.column_name AS foreign_column_name,
                    rc.delete_rule
                FROM information_schema.table_constraints AS tc
                JOIN information_schema.key_column_usage AS kcu
                    ON tc.constraint_name = kcu.constraint_name
                JOIN information_schema.constraint_column_usage AS ccu
                    ON ccu.constraint_name = tc.constraint_name
                JOIN information_schema.referential_constraints AS rc
                    ON tc.constraint_name = rc.constraint_name
                WHERE tc.constraint_type = 'FOREIGN KEY' AND tc.table_name = 'solicitacoes_acesso';", conn);
            using var rdFks = await cmdFks.ExecuteReaderAsync();
            while (await rdFks.ReadAsync())
            {
                System.Console.WriteLine($"[DIAG] FK: {rdFks.GetString(0)}, Table: {rdFks.GetString(1)}, Col: {rdFks.GetString(2)}, RefTable: {rdFks.GetString(3)}, RefCol: {rdFks.GetString(4)}, OnDelete: {rdFks.GetString(5)}");
            }
            await rdFks.CloseAsync();

            // DIAGNÓSTICO: Verificar se a linha existe antes/depois
            await using var cmdBefore = new NpgsqlCommand("SELECT id, status, empresa_id, usuario_id FROM solicitacoes_acesso WHERE id = @id", conn);
            cmdBefore.Parameters.AddWithValue("id", solicitacaoId);
            using var rdBefore = await cmdBefore.ExecuteReaderAsync();
            if (await rdBefore.ReadAsync())
            {
                var empId = rdBefore.IsDBNull(2) ? "NULL" : rdBefore.GetInt64(2).ToString();
                var userId = rdBefore.IsDBNull(3) ? "NULL" : rdBefore.GetInt64(3).ToString();
                System.Console.WriteLine($"[DIAG] Row EXISTS after DELETE: id={rdBefore.GetInt64(0)}, status={rdBefore.GetString(1)}, empresa_id={empId}, usuario_id={userId}");
            }
            else
            {
                System.Console.WriteLine($"[DIAG] Row NOT FOUND after DELETE (id={solicitacaoId})");
            }
            await rdBefore.CloseAsync();

            await using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM solicitacoes_acesso WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", solicitacaoId);
            var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
            count.Should().Be(0, "a solicitação deve ter sido excluída do banco");
        }
        finally
        {
            await LimparSolicitacoesTesteAsync();
        }
    }

    [Fact]
    public async Task DELETE_Acessos_SuperAdmin_ExcluiSolicitacaoPendenteComEmpresaUsuario_DeveRetornar200()
    {
        // Arrange - criar solicitação pendente COM empresa_id e usuario_id
        var connString = GetConnectionString();
        await using (var conn = new NpgsqlConnection(connString))
        {
            await conn.OpenAsync();
            await using var cmdPerfis = new NpgsqlCommand(@"
                INSERT INTO perfis (chave, nome) VALUES
                    ('super_admin', 'Super Administrador'),
                    ('admin', 'Administrador'),
                    ('profissional', 'Profissional'),
                    ('atendente', 'Atendente')
                ON CONFLICT (chave) DO NOTHING;", conn);
            await cmdPerfis.ExecuteNonQueryAsync();

            await using var cmdEmp = new NpgsqlCommand("INSERT INTO empresas (nome) VALUES ('Empresa Teste') RETURNING id;", conn);
            var empResult = await cmdEmp.ExecuteScalarAsync();
            long empresaId = Convert.ToInt64(empResult);

            await using var cmdUser = new NpgsqlCommand(@"
                INSERT INTO usuarios (id, empresa_id, nome, email, senha_hash, perfil, ativo)
                VALUES (@uid, @emp, 'Usuário Teste', @email, 'hash', 'profissional', true)", conn);
            cmdUser.Parameters.AddWithValue("uid", 999999);
            cmdUser.Parameters.AddWithValue("emp", empresaId);
            cmdUser.Parameters.AddWithValue("email", $"user_{Guid.NewGuid():N}@teste.com".Replace("-", ""));
            await cmdUser.ExecuteNonQueryAsync();

            // Criar solicitação pendente COM empresa_id e usuario_id
            await using var cmdSol = new NpgsqlCommand(@"
                INSERT INTO solicitacoes_acesso (nome, email, telefone, registro_profissional, clinica_nome, status, empresa_id, usuario_id)
                VALUES (@nome, @email, '', '', '', 'pendente', @emp, @user)
                RETURNING id;", conn);
            cmdSol.Parameters.AddWithValue("nome", "Solicitante Com Empresa");
            cmdSol.Parameters.AddWithValue("email", $"solicitacao_com_emp_{Guid.NewGuid():N}@teste.com".Replace("-", ""));
            cmdSol.Parameters.AddWithValue("emp", empresaId);
            cmdSol.Parameters.AddWithValue("user", 999999);
            var solResult = await cmdSol.ExecuteScalarAsync();
            long solicitacaoId = Convert.ToInt64(solResult);

            var token = GerarTokenSuperAdmin();
            var client = CriarCliente(token);

            try
            {
                // Act
                var response = await client.DeleteAsync($"/api/acessos/{solicitacaoId}");

                // Assert - deve retornar 200
                response.StatusCode.Should().Be(HttpStatusCode.OK);

                var content = await response.Content.ReadAsStringAsync();
                System.Console.WriteLine($"[DIAG] Response content: {content}");

                // DIAGNÓSTICO: Verificar se a linha existe antes/depois
                await using var cmdBefore = new NpgsqlCommand("SELECT id, status, empresa_id, usuario_id FROM solicitacoes_acesso WHERE id = @id", conn);
                cmdBefore.Parameters.AddWithValue("id", solicitacaoId);
                using var rdBefore = await cmdBefore.ExecuteReaderAsync();
                if (await rdBefore.ReadAsync())
                {
                    var empId = rdBefore.IsDBNull(2) ? "NULL" : rdBefore.GetInt64(2).ToString();
                    var userId = rdBefore.IsDBNull(3) ? "NULL" : rdBefore.GetInt64(3).ToString();
                    System.Console.WriteLine($"[DIAG] Row EXISTS after DELETE: id={rdBefore.GetInt64(0)}, status={rdBefore.GetString(1)}, empresa_id={empId}, usuario_id={userId}");
                }
                else
                {
                    System.Console.WriteLine($"[DIAG] Row NOT FOUND after DELETE (id={solicitacaoId})");
                }
                await rdBefore.CloseAsync();

                // Verificar se a solicitação foi excluída do banco
                await using var cmdCheck = new NpgsqlCommand("SELECT COUNT(*) FROM solicitacoes_acesso WHERE id = @id", conn);
                cmdCheck.Parameters.AddWithValue("id", solicitacaoId);
                var count = Convert.ToInt32(await cmdCheck.ExecuteScalarAsync());
                count.Should().Be(0, "a solicitação deve ter sido excluída do banco");
            }
            finally
            {
                await LimparSolicitacoesTesteAsync();
                // Limpar empresa e usuário de teste
                await using var cmdDelUser = new NpgsqlCommand("DELETE FROM usuarios WHERE id = 999999", conn);
                await cmdDelUser.ExecuteNonQueryAsync();
                await using var cmdDelEmp = new NpgsqlCommand("DELETE FROM empresas WHERE id = @id", conn);
                cmdDelEmp.Parameters.AddWithValue("id", empresaId);
                await cmdDelEmp.ExecuteNonQueryAsync();
            }
        }
    }

    [Fact]
    public async Task DELETE_Acessos_SuperAdmin_IdInexistente_DeveRetornar404()
    {
        // Arrange
        var token = GerarTokenSuperAdmin();
        var client = CriarCliente(token);
        long idInexistente = 999999999; // ID que não existe

        // Act
        var response = await client.DeleteAsync($"/api/acessos/{idInexistente}");

        // Assert - deve retornar 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var content = await response.Content.ReadAsStringAsync();
        // O conteúdo pode ter encoding UTF-8, verificar apenas o status code que já confirma 404
        content.Should().Contain("mensagem");
    }

    [Fact]
    public async Task DELETE_Acessos_UsuarioNaoSuperAdmin_DeveRetornar403()
    {
        // Arrange - criar solicitação pendente
        var solicitacaoId = await CriarSolicitacaoPendenteAsync();
        var token = GerarTokenComum(perfil: "admin"); // Não é super_admin
        var client = CriarCliente(token);

        try
        {
            // Act
            var response = await client.DeleteAsync($"/api/acessos/{solicitacaoId}");

            // Assert - deve retornar 403 Forbidden
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
        finally
        {
            await LimparSolicitacoesTesteAsync();
        }
    }
}