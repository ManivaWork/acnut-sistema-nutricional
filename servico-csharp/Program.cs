using Npgsql;
using System.Text.Json;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Net;
using ACNutri.Billing;
using ACNutri.Email;

var builder = WebApplication.CreateBuilder(args);
// === Asaas / Billing ===
var asaasSection = builder.Configuration.GetSection("Asaas");
var asaasCfg = new AsaasConfig(
    asaasSection["BaseUrl"] ?? "https://sandbox.asaas.com/api/v3",
    asaasSection["ApiKey"] ?? throw new InvalidOperationException("Defina Asaas:ApiKey nos user-secrets ou variÃ¡veis de ambiente."),
    asaasSection["WebhookSecret"] ?? throw new InvalidOperationException("Defina Asaas:WebhookSecret nos user-secrets ou variÃ¡veis de ambiente.")
);

builder.Services.AddSingleton(asaasCfg);
builder.Services.AddHttpClient<AsaasClient>();

    string connString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Defina ConnectionStrings:Default nos user-secrets.");
    string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Defina Jwt:Key nos user-secrets (mÃ­nimo 32 caracteres).");
    string pythonBaseUrl = builder.Configuration["Python:BaseUrl"] ?? "http://127.0.0.1:5002";
var origens = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "null" };
// "null" = pÃ¡gina aberta direto do disco (file://). Com Live Server, adicione http://127.0.0.1:5500
// Em produÃ§Ã£o (Render), defina Cors__AllowedOrigins como JSON array: ["https://achubhome.vercel.app","https://seu-outro-dominio.com"]

    builder.Services.AddCors(o => o.AddPolicy("Front", p =>
        p.WithOrigins(origens)
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials())); // necessÃ¡rio se frontend envia cookies/Authorization header

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = "bybarhub",
            ValidateAudience = true, ValidAudience = "bybarhub-app",
            ValidateLifetime = true, ClockSkew = TimeSpan.FromMinutes(1),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        // Token via header Authorization obrigatÃ³rio (nÃ£o aceita ?t=)
    });
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));
builder.Services.AddHttpClient<IEmailService, EmailService>(c => c.Timeout = TimeSpan.FromSeconds(10));

// HttpClient dedicado para o microserviÃ§o Python com timeout de 60s
builder.Services.AddHttpClient("Python", c =>
{
    c.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddHttpContextAccessor();

// Registrar NpgsqlDataSource para injeÃ§Ã£o de dependÃªncia (usado no BillingModule)
builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connString));

// === Asaas / Billing ===
builder.Services.Configure<AsaasConfig>(builder.Configuration.GetSection("Asaas"));
builder.Services.AddHttpClient<AsaasClient>();
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<AsaasConfig>>().Value);

// === // === // === // === // === // === Rate Limiting para endpoints de autenticação ===
// Limite por IP mais folgado (30 req/15min por IP) + bloqueio de tentativas falhas por identificador+IP no banco
// Em testes, o header X-Test-Run-Id pode ser usado para isolar o rate limiter por execução de teste
builder.Services.AddRateLimiter(options =>
{
    // Política "ip-auth" - 30 req/15min particionado por IP do cliente
    // Em testes, usa header X-Test-Run-Id como prefixo se presente (para isolar entre testes paralelos)
    options.AddPolicy("ip-auth", httpContext =>
    {
        var testRunPrefix = httpContext.Request.Headers["X-Test-Run-Id"].FirstOrDefault();
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionKey = string.IsNullOrEmpty(testRunPrefix) ? ip : $"{testRunPrefix}:{ip}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(15),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });

    // Rejeição personalizada com Retry-After e headers de rate limit
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        var retryAfter = 900; // fallback 15 min
        context.HttpContext.Response.Headers["Retry-After"] = retryAfter.ToString();
        context.HttpContext.Response.Headers["X-RateLimit-Limit"] = "30";
        context.HttpContext.Response.Headers["X-RateLimit-Remaining"] = "0";
        context.HttpContext.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfter).ToUnixTimeSeconds().ToString();
        await context.HttpContext.Response.WriteAsJsonAsync(new { mensagem = $"Muitas tentativas. Tente novamente em {retryAfter / 60} minutos." }, token);
    };
});

var app = builder.Build();

// Log da configuraÃ§Ã£o do Python Service na inicializaÃ§Ã£o
var pythonBaseUrlLog = builder.Configuration["Python:BaseUrl"] ?? "http://127.0.0.1:5002 (padrÃ£o)";
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var startupLogger = loggerFactory.CreateLogger("Startup");
startupLogger.LogInformation("Python Service BaseUrl: {BaseUrl}", pythonBaseUrlLog);
if (app.Environment.IsProduction() && (pythonBaseUrlLog.Contains("127.0.0.1") || pythonBaseUrlLog.Contains("localhost")))
{
    startupLogger.LogWarning("Python:BaseUrl aponta para localhost/127.0.0.1 em ambiente Production. Configure a URL do serviÃ§o Python em produÃ§Ã£o (ex.: https://seu-servico.onrender.com).");
}

// Verificar configuraÃ§Ã£o do pepper para rate limiting (hash do identificador+IP)
var pepper = builder.Configuration["RateLimit:Pepper"];
if (string.IsNullOrEmpty(pepper))
{
    startupLogger.LogWarning("RateLimit:Pepper nÃ£o configurado! Usando valor padrÃ£o INSEGURO 'default-pepper-change-in-production'. Defina RateLimit:Pepper nos user-secrets/variÃ¡veis de ambiente com uma string aleatÃ³ria longa (ex.: 64 chars) para produÃ§Ã£o.");
}
else if (pepper == "default-pepper-change-in-production")
{
    startupLogger.LogWarning("RateLimit:Pepper estÃ¡ usando o valor padrÃ£o INSEGURO 'default-pepper-change-in-production'. Altere para uma string aleatÃ³ria longa em produÃ§Ã£o.");
}

// Forwarded headers - apenas em Production (no Render hÃ¡ proxy reverso)
// Em Development/Test nÃ£o hÃ¡ proxy, entÃ£o nÃ£o precisamos e evitamos problemas com IP
if (app.Environment.IsProduction())
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1, // Pega apenas o Ãºltimo IP da cadeia (o do cliente real apÃ³s o proxy do Render)
        KnownNetworks = { }, // Vazio = confia em todos (necessÃ¡rio no Render onde proxy nÃ£o Ã© loopback/known)
        KnownProxies = { }
    });
}

// VerificaÃ§Ã£o de IP do cliente em desenvolvimento (log para debug)
if (app.Environment.IsDevelopment())
{
    app.Use(async (ctx, next) =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var forwardedFor = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "none";
        var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogDebug("Client IP: {Ip}, X-Forwarded-For: {ForwardedFor}", ip, forwardedFor);
        await next();
    });
}

app.UseDefaultFiles();
app.UseStaticFiles();

// CORS e Auth devem vir ANTES do UseRouting para AllowAnonymous funcionar
app.UseCors("Front");
app.UseAuthentication();

app.UseRouting();

// Rate Limiting middleware (aplica a política "ip-auth" por endpoint via .RequireRateLimiting("ip-auth"))
app.UseRateLimiter();

// Health endpoint for UptimeRobot (no auth, no DB)
app.MapGet("/health", () => Results.Ok("ok")).AllowAnonymous();

app.UseAuthorization();

// Swagger UI apenas em Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ACNUTRI API v1");
        c.RoutePrefix = "swagger";
    });
}

if (!string.IsNullOrEmpty(asaasCfg.ApiKey))
    app.MapBillingEndpoints(connString, asaasCfg);

await Seed.Garantir(connString, builder.Configuration);

        // ===== HEADERS DE SEGURANÃ‡A =====
        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            // HSTS sÃ³ em HTTPS (produÃ§Ã£o)
            if (ctx.Request.IsHttps)
                ctx.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
            await next();
        });

        // ===== AUDITORIA HELPER =====
        async Task RegistrarAuditoria(HttpContext http, string acao, long? registroId = null)
        {
            try
            {
                var user = http.User;
                if (user.FindFirst("tipo")?.Value != "equipe") return;
                var uidClaim = user.FindFirst("uid");
                var empClaim = user.FindFirst("empresa");
                if (uidClaim == null || empClaim == null) return;

                int uid = int.Parse(uidClaim.Value);
                int emp = int.Parse(empClaim.Value);
                var ip = http.Connection.RemoteIpAddress;
                var ua = http.Request.Headers["User-Agent"].ToString();

                using var conn = new NpgsqlConnection(connString);
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO auditoria (empresa_id, usuario_id, acao, rota, registro_id, ip, user_agent)
                    VALUES (@emp, @uid, @acao, @rota, @reg, @ip, @ua)", conn);
                cmd.Parameters.AddWithValue("emp", emp);
                cmd.Parameters.AddWithValue("uid", uid);
                cmd.Parameters.AddWithValue("acao", acao);
                cmd.Parameters.AddWithValue("rota", http.Request.Path.Value ?? "");
                cmd.Parameters.AddWithValue("reg", (object?)registroId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("ip", (object?)ip ?? DBNull.Value);
                cmd.Parameters.AddWithValue("ua", string.IsNullOrEmpty(ua) ? DBNull.Value : (object)ua);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* auditoria nÃ£o deve quebrar a request */ }
        }

        // VersÃ£o para uso dentro de transaÃ§Ã£o existente
        async Task RegistrarAuditoriaDetalhada(NpgsqlConnection conn, NpgsqlTransaction tx, int emp, int uid, string acao, string rota, long? registroId, string detalhes)
        {
            try
            {
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO auditoria (empresa_id, usuario_id, acao, rota, registro_id, ip, user_agent)
                    VALUES (@emp, @uid, @acao, @rota, @reg, NULL, @detalhes)", conn, tx);
                cmd.Parameters.AddWithValue("emp", emp);
                cmd.Parameters.AddWithValue("uid", uid);
                cmd.Parameters.AddWithValue("acao", acao);
                cmd.Parameters.AddWithValue("rota", rota);
                cmd.Parameters.AddWithValue("reg", (object?)registroId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("detalhes", detalhes);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* auditoria nÃ£o deve quebrar a request */ }
        }

        // Qualquer erro vira JSON { mensagem } (o front mostra no alert) - SEM DADOS PESSOAIS NOS LOGS
app.Use(async (context, next) =>
        {
        try
        {
        await next();
        }
        catch (Exception ex)
        {
        // Log sem dados sensÃ­veis: apenas mÃ©todo, rota, tipo de erro
        Console.WriteLine($"=== ERRO em {context.Request.Method} {context.Request.Path}: {ex.GetType().Name} ===");
        if (!context.Response.HasStarted)
        {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { mensagem = "Erro interno do servidor." }));
        }
        }
        });
        // PORTEIRO: toda rota /api exige token de equipe e nÃ­vel no mÃ³dulo nutriÃ§Ã£o
app.Use(async (ctx, next) =>
{
    var path = ctx.Request.Path.Value ?? "";
    var m = ctx.Request.Method;
    if (!path.StartsWith("/api/")
    || path.StartsWith("/api/auth/")
    || path.StartsWith("/api/webhook/")
    || path == "/api/empresas/count"
    || path.StartsWith("/api/convites/info/")
    || path == "/api/onboarding"
    || path.StartsWith("/api/definir-senha/")
    || path == "/api/acesso/solicitar"   // POST pÃºblico - solicitar acesso
    || HttpMethods.IsOptions(m))
{ await next(); return; }
    async Task Negar(int status, string msg)
    {
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { mensagem = msg });
    }
    var u = ctx.User;
        if (u.Identity?.IsAuthenticated != true) { await Negar(401, "SessÃ£o expirada. Entre novamente."); return; }
    // Token de paciente: GET em /api/paciente/* + POST/DELETE em /api/paciente/exames + POST/GET em /api/paciente/consentimentos
    // + GET/POST em /api/anamnese + GET em /api/planos
    // metas/orientacoes carregados via /api/paciente/tudo; exames via /api/paciente/exames; evolucao nÃ£o usado no portal
    if (u.FindFirst("tipo")?.Value == "paciente")
    {
        bool isPacientePath = path.StartsWith("/api/paciente/")
            || path.StartsWith("/api/anamnese")
            || path.StartsWith("/api/planos/");
        if (!isPacientePath)
        { await Negar(403, "Acesso nÃ£o permitido."); return; }
        if (!HttpMethods.IsGet(m))
        {
            bool permitido = (path == "/api/paciente/exames" && HttpMethods.IsPost(m))
                          || (path.StartsWith("/api/paciente/exames/") && HttpMethods.IsDelete(m))
                          || (path == "/api/paciente/consentimentos" && (HttpMethods.IsPost(m) || HttpMethods.IsGet(m)))
                          || (path == "/api/anamnese" && HttpMethods.IsPost(m))
                          || (path.StartsWith("/api/anamnese/") && HttpMethods.IsGet(m));
            if (!permitido) { await Negar(403, "Acesso nÃ£o permitido."); return; }
        }
        await next(); return;
    }
    if (u.FindFirst("tipo")?.Value != "equipe" || path.StartsWith("/api/paciente/"))
    { await Negar(403, "Acesso nÃ£o permitido."); return; }
    if (path.StartsWith("/api/usuarios") && u.FindFirst("perfil")?.Value is not ("admin" or "super_admin"))
    { await Negar(403, "Apenas administradores."); return; }

    // ROTAS EXCLUSIVAS SUPER_ADMIN
    var perfil = u.FindFirst("perfil")?.Value;
    if (perfil != "super_admin")
    {
        bool isSuperAdminAcesso =
            // GET /api/acessos (listar solicitaÃ§Ãµes)
            (HttpMethods.IsGet(m) && path == "/api/acessos")
            // POST /api/acessos/{id}/aprovar
            || (HttpMethods.IsPost(m) && System.Text.RegularExpressions.Regex.IsMatch(path, @"^/api/acessos/\d+/aprovar$"))
            // POST /api/acessos/{id}/recusar
            || (HttpMethods.IsPost(m) && System.Text.RegularExpressions.Regex.IsMatch(path, @"^/api/acessos/\d+/recusar$"))
            // POST /api/acessos/{id}/reenviar-email
            || (HttpMethods.IsPost(m) && System.Text.RegularExpressions.Regex.IsMatch(path, @"^/api/acessos/\d+/reenviar-email$"))
            // DELETE /api/acessos/{id}
            || (HttpMethods.IsDelete(m) && System.Text.RegularExpressions.Regex.IsMatch(path, @"^/api/acessos/\d+$"));

        if (isSuperAdminAcesso
            || path.StartsWith("/api/empresas")
            || path.StartsWith("/api/lgpd")
            || path.StartsWith("/api/nutricionista/convites-paciente")
            || (path.StartsWith("/api/empresas/") && path.Contains("/convite")))
        {
            await Negar(403, "Acesso restrito ao Super Admin.");
            return;
        }
    }

    int nivel = Auth.Peso(u.FindFirst("mod_nutricao")?.Value);
    int exigido = HttpMethods.IsGet(m) ? 1
                : (HttpMethods.IsDelete(m) && path.StartsWith("/api/pacientes")) ? 3
                : 2;
    if (nivel < exigido) { await Negar(403, "Seu perfil nÃ£o tem permissÃ£o para esta aÃ§Ã£o."); return; }

    // VALIDAÃ‡ÃƒO: usuÃ¡rio pertence Ã  empresa do token (exceto super_admin)

    // Bloquear perfil "profissional" no mÃ³dulo LGPD (redundante, mas mantido para clareza)
    if (perfil == "profissional" && path.StartsWith("/api/lgpd"))
    {
        await Negar(403, "Perfil profissional nÃ£o tem acesso ao mÃ³dulo LGPD.");
        return;
    }

    if (perfil != "super_admin")
    {
        int uid = int.Parse(u.FindFirst("uid")!.Value);
        int tokenEmpresa = Auth.Empresa(u);
        int userEmpresa = 0;
        using (var conn = new NpgsqlConnection(connString))
        {
            await conn.OpenAsync();
            using var cmd = new NpgsqlCommand("SELECT empresa_id FROM usuarios WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("id", uid);
            var r = await cmd.ExecuteScalarAsync();
            if (r != null) userEmpresa = Convert.ToInt32(r);
        }
        if (userEmpresa != tokenEmpresa)
        {
            await Negar(403, "UsuÃ¡rio nÃ£o pertence Ã  empresa do token.");
            return;
        }
    }

    // ISOLAMENTO POR EMPRESA: o registro precisa pertencer Ã  empresa do token
    var alvos = await Escopo.Alvos(ctx);

    if (alvos.Count > 0 && !await Escopo.Pertence(connString, alvos, Auth.Empresa(u)))
    { await Negar(404, "Registro nÃ£o encontrado."); return; }
    await next();
});
// ==================================================== PACIENTES  ========================================
// 1. LISTAR PACIENTES
app.MapGet("/api/pacientes", async (ClaimsPrincipal user) =>
{
    var pacientes = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT id, nome, email, telefone, data_nascimento, sexo, status, cpf FROM pacientes WHERE empresa_id = @emp ORDER BY id DESC;", conn);
    cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        pacientes.Add(new
        {
            id = Convert.ToInt32(reader.GetValue(0)),
            nome = reader.GetString(1),
            email = reader.GetString(2),
            telefone = reader.IsDBNull(3) ? "" : reader.GetString(3),
            data_nascimento = reader.GetDateTime(4).ToString("yyyy-MM-dd"),
            sexo = reader.GetString(5),
            status = reader.IsDBNull(6) ? "ativo" : reader.GetString(6),
            cpf = reader.IsDBNull(7) ? "" : reader.GetString(7)
        });
    }
    return Results.Ok(pacientes);
});

// 1a. OBTER PACIENTE POR ID
app.MapGet("/api/pacientes/{id:int}", async (int id, ClaimsPrincipal user) =>
{
    var empresaId = Auth.Empresa(user);
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT id, nome, email, telefone, data_nascimento, sexo, status, cpf FROM pacientes WHERE id = @id AND empresa_id = @emp", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", empresaId);
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    return Results.Ok(new
    {
        id = Convert.ToInt32(reader.GetValue(0)),
        nome = reader.GetString(1),
        email = reader.GetString(2),
        telefone = reader.IsDBNull(3) ? "" : reader.GetString(3),
        data_nascimento = reader.GetDateTime(4).ToString("yyyy-MM-dd"),
        sexo = reader.GetString(5),
        status = reader.IsDBNull(6) ? "ativo" : reader.GetString(6),
        cpf = reader.IsDBNull(7) ? "" : reader.GetString(7)
    });
}).RequireAuthorization();

    // 1b. BUSCAR PACIENTES POR NOME
    app.MapGet("/api/pacientes/buscar", async (string q, ClaimsPrincipal user) =>
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Results.Ok(new List<object>());

        var empresaId = Auth.Empresa(user);
        var resultados = new List<object>();
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        // unaccent + ILIKE para busca sem acento (requer extensÃ£o unaccent no Postgres)
        using var cmd = new NpgsqlCommand(@"
            SELECT id, nome, data_nascimento
            FROM pacientes
            WHERE empresa_id = @emp
              AND unaccent(nome) ILIKE unaccent(@q)
            ORDER BY nome
            LIMIT 10;", conn);
        cmd.Parameters.AddWithValue("emp", empresaId);
        cmd.Parameters.AddWithValue("q", $"%{q}%");
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var dataNasc = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2);
            resultados.Add(new
            {
                id = Convert.ToInt32(reader.GetValue(0)),
                nome = reader.GetString(1),
                data_nascimento = dataNasc?.ToString("yyyy-MM-dd")
            });
        }
        return Results.Ok(resultados);
    })
    .RequireAuthorization(); // garante que sÃ³ usuÃ¡rios autenticados acessem

// 2. CADASTRAR PACIENTE
app.MapPost("/api/pacientes", async (HttpRequest request, ClaimsPrincipal user, NpgsqlDataSource db, ILogger<Program> logger) =>
{
    // Gerar traceId (codigo_erro) para rastreamento de erros
    var traceId = Guid.NewGuid().ToString("N")[..12].ToUpper();
    var empresaIdLog = 0;

    try
    {
        var data = await JsonSerializer.DeserializeAsync<JsonElement>(request.Body);

        string nome = data.GetProperty("nome").GetString()!;
        string email = data.GetProperty("email").GetString()!;
        string telefone = data.TryGetProperty("telefone", out var tel) ? tel.GetString() ?? "" : "";
        string dataNascimento = data.GetProperty("data_nascimento").GetString()!;
        string sexo = data.GetProperty("sexo").GetString()!;
        string status = data.TryGetProperty("status", out var st) ? st.GetString() ?? "ativo" : "ativo";
        var cpf = Cpf.Limpar(data.TryGetProperty("cpf", out var cp) ? cp.GetString() : "");
        if (cpf != "" && !Cpf.Valido(cpf)) return Results.BadRequest(new { mensagem = "CPF invÃ¡lido." });

        int empresaId = Auth.Empresa(user);
        empresaIdLog = empresaId;

        // Verificar limite de pacientes conforme plano (com bypass para Super Admin)
        var (podeCriar, maxPacientes, atual) = await PlanoEnforcement.VerificarLimitePacientesAsync(db, empresaId, user);
        if (!podeCriar)
        {
            return Results.Json(new
            {
                mensagem = $"Limite de pacientes atingido ({atual}/{maxPacientes}). Assine um plano para adicionar mais.",
                codigo = "plano_necessario",
                limite = maxPacientes,
                atual
            }, statusCode: 403);
        }

        DateTime dtNascimento;
        try
        {
            dtNascimento = DateTime.Parse(dataNascimento);
        }
        catch
        {
            return Results.BadRequest(new { mensagem = "Data de nascimento invÃ¡lida. Use formato ISO (YYYY-MM-DD)." });
        }

        await using var conn = await db.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(@"
            INSERT INTO pacientes (nome, email, telefone, data_nascimento, sexo, status, empresa_id, cpf)
            VALUES (@nome, @email, @telefone, @data_nascimento, @sexo, @status, @emp, @cpf) RETURNING id;", conn);

        cmd.Parameters.AddWithValue("nome", nome);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("telefone", telefone);
        cmd.Parameters.AddWithValue("data_nascimento", dtNascimento);
        cmd.Parameters.AddWithValue("sexo", sexo);
        cmd.Parameters.AddWithValue("status", status);
        cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
        cmd.Parameters.Add(new NpgsqlParameter("cpf", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = cpf == "" ? DBNull.Value : cpf });

        var newId = await cmd.ExecuteScalarAsync();
        return Results.Created($"/api/pacientes/{newId}", new { id = newId, mensagem = "Paciente cadastrado com sucesso!" });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        logger.LogWarning(ex, "Conflito de unicidade ao cadastrar paciente: traceId={TraceId} empresa={EmpresaId}", traceId, empresaIdLog);
        return Results.Conflict(new { mensagem = "JÃ¡ existe um paciente com esse e-mail ou CPF nesta empresa.", codigo = "duplicado", codigo_erro = traceId });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Erro inesperado ao cadastrar paciente: traceId={TraceId} empresa={EmpresaId}", traceId, empresaIdLog);
        return Results.Json(new { title = "Erro interno ao cadastrar paciente.", detail = "Ocorreu um erro inesperado. Contate o suporte informando o cÃ³digo: " + traceId, codigo_erro = traceId }, statusCode: 500);
    }
});// 3. ATUALIZAR PACIENTE
app.MapPut("/api/pacientes/{id}", async (int id, HttpRequest request, ClaimsPrincipal user) =>
{
    var data = await JsonSerializer.DeserializeAsync<JsonElement>(request.Body);

    string nome = data.GetProperty("nome").GetString()!;
    string email = data.GetProperty("email").GetString()!;
    string telefone = data.TryGetProperty("telefone", out var tel) ? tel.GetString() ?? "" : "";
    string dataNascimento = data.GetProperty("data_nascimento").GetString()!;
    string sexo = data.GetProperty("sexo").GetString()!;
    string status = data.TryGetProperty("status", out var st) ? st.GetString() ?? "ativo" : "ativo";
    var cpf = Cpf.Limpar(data.TryGetProperty("cpf", out var cp) ? cp.GetString() : "");
    if (cpf != "" && !Cpf.Valido(cpf)) return Results.BadRequest(new { mensagem = "CPF invÃ¡lido." });

    int empresaId = Auth.Empresa(user);

    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    await using var cmd = new NpgsqlCommand(@"
        UPDATE pacientes
        SET nome = @nome, email = @email, telefone = @telefone,
            data_nascimento = @data, sexo = @sexo, status = @status, cpf = @cpf
        WHERE id = @id AND empresa_id = @emp", conn);

    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("nome", nome);
    cmd.Parameters.AddWithValue("email", email);
    cmd.Parameters.AddWithValue("telefone", telefone);
    cmd.Parameters.AddWithValue("data", DateTime.Parse(dataNascimento));
    cmd.Parameters.AddWithValue("sexo", sexo);
    cmd.Parameters.AddWithValue("status", status);
    cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
    cmd.Parameters.Add(new NpgsqlParameter("cpf", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = cpf == "" ? DBNull.Value : cpf });

    try
    {
        var rowsAffected = await cmd.ExecuteNonQueryAsync();
        return rowsAffected > 0
            ? Results.Ok(new { mensagem = "Paciente atualizado com sucesso!" })
            : Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        return Results.Conflict(new { mensagem = "JÃ¡ existe um paciente com esse e-mail ou CPF." });
    }
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// 4. EXCLUIR PACIENTE
app.MapDelete("/api/pacientes/{id}", async (int id, ClaimsPrincipal user) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM pacientes WHERE id = @id AND empresa_id = @emp", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));

    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    return rowsAffected > 0
        ? Results.Ok(new { mensagem = "Paciente excluÃ­do com sucesso!" })
        : Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// 5. ANONIMIZAR PACIENTE (LGPD) - apenas admin e super_admin
app.MapPost("/api/pacientes/{id}/anonimizar", async (int id, ClaimsPrincipal user) =>
{
    var perfil = user.FindFirst("perfil")?.Value;
    if (perfil != "super_admin" && perfil != "admin")
        return Results.Forbid();

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Verifica se o paciente existe e pertence Ã  empresa
    using (var cmd = new NpgsqlCommand("SELECT id, nome FROM pacientes WHERE id = @id AND empresa_id = @emp", conn))
    {
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });
    }

    // Usar transaÃ§Ã£o para garantir atomicidade
    using var tx = await conn.BeginTransactionAsync();
    try
    {
        var agora = DateTime.UtcNow;
        var cpfAnon = $"ANON-{id:D8}";
        var emailAnon = $"anon-{id:D8}@anonimizado.local";
        var nomeAnon = $"Paciente Anonimizado {id}";
        var telefoneAnon = "";
        var dataNascAnon = new DateTime(1900, 1, 1);

        // 1. Apagar dados relacionados
        // Exames e arquivos de exames
        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM arquivos_paciente
            WHERE paciente_id = @id AND empresa_id = @emp
            AND tipo IN ('exame', 'evolucao');", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM exames WHERE paciente_id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        // Anamnese
        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM anamneses WHERE paciente_id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        // EvoluÃ§Ã£o (inclui fotos)
        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM evolucao WHERE paciente_id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        // Check-ins semanais
        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM checkins_semanais WHERE paciente_id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        // AvaliaÃ§Ãµes fÃ­sicas
        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM avaliacoes_fisicas WHERE paciente_id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        // Planos alimentares e versÃµes
        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM planos_versoes WHERE paciente_id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM planos_alimentares WHERE paciente_id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        // OrientaÃ§Ãµes nutricionais
        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM orientacoes_nutricionais WHERE paciente_id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        // Metas do paciente
        using (var cmd = new NpgsqlCommand(@"
            DELETE FROM metas_paciente WHERE paciente_id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            await cmd.ExecuteNonQueryAsync();
        }

        // 2. Anonimizar dados do paciente
        using (var cmd = new NpgsqlCommand(@"
            UPDATE pacientes SET
                nome = @nome,
                email = @email,
                telefone = @telefone,
                data_nascimento = @data_nasc,
                cpf = @cpf,
                foto_url = NULL,
                anonimizado_em = @agora
            WHERE id = @id AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", id);
            cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
            cmd.Parameters.AddWithValue("nome", nomeAnon);
            cmd.Parameters.AddWithValue("email", emailAnon);
            cmd.Parameters.AddWithValue("telefone", telefoneAnon);
            cmd.Parameters.AddWithValue("data_nasc", dataNascAnon);
            cmd.Parameters.AddWithValue("cpf", cpfAnon);
            cmd.Parameters.AddWithValue("agora", agora);
            await cmd.ExecuteNonQueryAsync();
        }

        // 3. Registrar na auditoria
        await RegistrarAuditoriaDetalhada(conn, tx, Auth.Empresa(user), Auth.UsuarioId(user),
            "anonimizacao", $"/api/pacientes/{id}/anonimizar", id,
            $"Paciente anonimizado por {Auth.Nome(user) ?? "usuÃ¡rio"}. Dados clÃ­nicos removidos, PII substituÃ­da por valores anÃ´nimos.");

        await tx.CommitAsync();
        return Results.Ok(new { mensagem = "Paciente anonimizado com sucesso. Dados clÃ­nicos removidos e PII substituÃ­da.", anonimizado_em = agora });
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
});

//==================================== AVALIAÃ‡ÃƒO FÃSICA (integra com o microserviÃ§o Python)======================================
app.MapPost("/api/avaliacoes", async (AvaliacaoInput input, IHttpClientFactory clientFactory, ILogger<Program> logger) =>
{
    var httpClient = clientFactory.CreateClient("Python");
    var payloadPython = new
    {
        idade = input.Idade,
        sexo = input.Sexo,
        peso_kg = input.PesoKg,
        altura_cm = input.AlturaCm,
        nivel_atividade = input.NivelAtividade,
        objetivo = input.Objetivo
    };

    var content = new StringContent(JsonSerializer.Serialize(payloadPython), Encoding.UTF8, "application/json");

    HttpResponseMessage responsePython;
    try
    {
        responsePython = await httpClient.PostAsync($"{pythonBaseUrl}/calcular-plano", content);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Falha de rede/ligaÃ§Ã£o ao chamar Python Service /calcular-plano");
        return Results.Json(new { mensagem = "O serviÃ§o de cÃ¡lculos nutricionais estÃ¡ temporariamente indisponÃ­vel. Tente novamente em alguns instantes." }, statusCode: 503);
    }
    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.CancellationToken.IsCancellationRequested)
    {
        logger.LogError(ex, "Timeout ao chamar Python Service /calcular-plano (60s)");
        return Results.Json(new { mensagem = "O serviÃ§o de cÃ¡lculos nutricionais estÃ¡ temporariamente indisponÃ­vel. Tente novamente em alguns instantes." }, statusCode: 503);
    }

    if (!responsePython.IsSuccessStatusCode)
    {
        var errorBody = await responsePython.Content.ReadAsStringAsync();
        var logBody = errorBody.Length > 500 ? errorBody[..500] + "..." : errorBody;

        if ((int)responsePython.StatusCode >= 500 || responsePython.StatusCode == System.Net.HttpStatusCode.BadGateway || responsePython.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || responsePython.StatusCode == System.Net.HttpStatusCode.GatewayTimeout || responsePython.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Python Service /calcular-plano retornou {StatusCode}: {Body}", responsePython.StatusCode, logBody);
            return Results.Json(new { mensagem = "O serviÃ§o de cÃ¡lculos nutricionais estÃ¡ temporariamente indisponÃ­vel. Tente novamente em alguns instantes." }, statusCode: 503);
        }
        else if ((int)responsePython.StatusCode >= 400)
        {
            logger.LogWarning("Python Service /calcular-plano retornou erro 4xx {StatusCode}: {Body}", responsePython.StatusCode, logBody);
            return Results.Json(new { mensagem = "Dados invÃ¡lidos para cÃ¡lculo nutricional. Verifique os campos informados." }, statusCode: 400);
        }
        else
        {
            logger.LogWarning("Python Service /calcular-plano retornou {StatusCode}: {Body}", responsePython.StatusCode, logBody);
            return Results.Json(new { mensagem = "Erro inesperado no serviÃ§o de cÃ¡lculos nutricionais." }, statusCode: 502);
        }
    }

    var jsonResp = await responsePython.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(jsonResp);
    var root = doc.RootElement;

    double tmb = root.GetProperty("tmb_kcal").GetDouble();
    double get = root.GetProperty("get_kcal").GetDouble();

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO avaliacoes_fisicas (paciente_id, empresa_id, peso_kg, altura_cm, percentual_gordura, massa_magra_kg, tmb_kcal, get_kcal)
        VALUES (@paciente_id, (SELECT empresa_id FROM pacientes WHERE id = @paciente_id), @peso, @altura, @gordura, @massa_magra, @tmb, @get) RETURNING id;", conn);

    cmd.Parameters.AddWithValue("paciente_id", input.PacienteId);
    cmd.Parameters.AddWithValue("peso", input.PesoKg);
    cmd.Parameters.AddWithValue("altura", input.AlturaCm);
    cmd.Parameters.AddWithValue("gordura", input.PercentualGordura);
    cmd.Parameters.AddWithValue("massa_magra", input.MassaMagraKg);
    cmd.Parameters.AddWithValue("tmb", tmb);
    cmd.Parameters.AddWithValue("get", get);

    var avaliacaoId = await cmd.ExecuteScalarAsync();

    return Results.Ok(new
    {
        avaliacao_id = avaliacaoId,
        mensagem = "AvaliaÃ§Ã£o registrada com sucesso!",
        calculos_python = JsonSerializer.Deserialize<object>(jsonResp)
    });
});
// ================================================= ALIMENTOS =================================================================
app.MapGet("/api/alimentos", async () =>
{
    var alimentos = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g FROM alimentos ORDER BY nome;", conn);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        alimentos.Add(new
        {
            id = Convert.ToInt32(reader.GetValue(0)),
            nome = reader.GetString(1),
            categoria = reader.IsDBNull(2) ? "" : reader.GetString(2),
            calorias_100g = reader.GetDecimal(3),
            proteinas_100g = reader.GetDecimal(4),
            carboidratos_100g = reader.GetDecimal(5),
            gorduras_100g = reader.GetDecimal(6),
            fibras_100g = reader.IsDBNull(7) ? 0m : reader.GetDecimal(7)
        });
    }
    return Results.Ok(alimentos);
});

// Busca alimentos por nome (para autocomplete/preenchimento automÃ¡tico)
app.MapGet("/api/alimentos/buscar", async (string q, int limite = 20) =>
{
    if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
        return Results.Ok(new List<object>());

    var alimentos = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g
        FROM alimentos
        WHERE nome ILIKE @q
        ORDER BY
            CASE WHEN nome ILIKE @exato THEN 0 ELSE 1 END,
            nome
        LIMIT @lim;", conn);
    cmd.Parameters.AddWithValue("q", $"%{q}%");
    cmd.Parameters.AddWithValue("exato", $"{q}%");
    cmd.Parameters.AddWithValue("lim", limite);

    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        alimentos.Add(new
        {
            id = Convert.ToInt32(reader.GetValue(0)),
            nome = reader.GetString(1),
            categoria = reader.IsDBNull(2) ? "" : reader.GetString(2),
            calorias_100g = reader.GetDecimal(3),
            proteinas_100g = reader.GetDecimal(4),
            carboidratos_100g = reader.GetDecimal(5),
            gorduras_100g = reader.GetDecimal(6),
            fibras_100g = reader.IsDBNull(7) ? 0m : reader.GetDecimal(7)
        });
    }
    return Results.Ok(alimentos);
});

// Busca alimento exato por ID (para preencher formulÃ¡rio ao selecionar)
app.MapGet("/api/alimentos/{id:int}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g FROM alimentos WHERE id = @id;", conn);
    cmd.Parameters.AddWithValue("id", id);
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.NotFound(new { mensagem = "Alimento nÃ£o encontrado." });

    return Results.Ok(new
    {
        id = Convert.ToInt32(reader.GetValue(0)),
        nome = reader.GetString(1),
        categoria = reader.IsDBNull(2) ? "" : reader.GetString(2),
        calorias_100g = reader.GetDecimal(3),
        proteinas_100g = reader.GetDecimal(4),
        carboidratos_100g = reader.GetDecimal(5),
        gorduras_100g = reader.GetDecimal(6),
        fibras_100g = reader.IsDBNull(7) ? 0m : reader.GetDecimal(7)
    });
});

// Busca medidas caseiras de um alimento
app.MapGet("/api/alimentos/{id:int}/medidas", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT id, medida, gramas_por_unidade, observacao FROM alimentos_medidas_caseiras WHERE alimento_id = @id ORDER BY medida;", conn);
    cmd.Parameters.AddWithValue("id", id);
    using var reader = await cmd.ExecuteReaderAsync();
    var medidas = new List<object>();
    while (await reader.ReadAsync())
    {
        medidas.Add(new
        {
            id = reader.GetInt64(0),
            medida = reader.GetString(1),
            gramas_por_unidade = reader.GetDecimal(2),
            observacao = reader.IsDBNull(3) ? "" : reader.GetString(3)
        });
    }
    return Results.Ok(medidas);
});

// POST /api/alimentos/{id}/medidas - Criar medida caseira (admin/profissional)
app.MapPost("/api/alimentos/{alimentoId:int}/medidas", async (int alimentoId, MedidaCaseiraInput input, ClaimsPrincipal user) =>
{
    var perfil = user.FindFirst("perfil")?.Value;
    if (perfil != "super_admin" && perfil != "admin" && perfil != "profissional")
        return Results.Forbid();

    int emp = Auth.Empresa(user);
    // Verificar se alimento pertence Ã  empresa (alimentos globais nÃ£o tÃªm empresa_id, ou verificar se Ã© da empresa)
    // Para simplificar, permitimos adicionar medidas a qualquer alimento

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO alimentos_medidas_caseiras (alimento_id, medida, gramas_por_unidade, observacao)
        VALUES (@alimento_id, @medida, @gramas, @obs) RETURNING id;", conn);
    cmd.Parameters.AddWithValue("alimento_id", alimentoId);
    cmd.Parameters.AddWithValue("medida", input.Medida ?? "");
    cmd.Parameters.AddWithValue("gramas", input.GramasPorUnidade);
    cmd.Parameters.AddWithValue("obs", (object?)input.Observacao ?? DBNull.Value);
    var newId = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/alimentos/{alimentoId}/medidas/{newId}", new { id = newId, mensagem = "Medida caseira criada!" });
})
.RequireAuthorization();

// PUT /api/alimentos/{id}/medidas/{medidaId} - Atualizar medida caseira
app.MapPut("/api/alimentos/{alimentoId:int}/medidas/{medidaId:int}", async (int alimentoId, int medidaId, MedidaCaseiraInput input, ClaimsPrincipal user) =>
{
    var perfil = user.FindFirst("perfil")?.Value;
    if (perfil != "super_admin" && perfil != "admin" && perfil != "profissional")
        return Results.Forbid();

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        UPDATE alimentos_medidas_caseiras
        SET medida = @medida, gramas_por_unidade = @gramas, observacao = @obs
        WHERE id = @id AND alimento_id = @alimento_id;", conn);
    cmd.Parameters.AddWithValue("id", medidaId);
    cmd.Parameters.AddWithValue("alimento_id", alimentoId);
    cmd.Parameters.AddWithValue("medida", input.Medida ?? "");
    cmd.Parameters.AddWithValue("gramas", input.GramasPorUnidade);
    cmd.Parameters.AddWithValue("obs", (object?)input.Observacao ?? DBNull.Value);
    int rows = await cmd.ExecuteNonQueryAsync();
    if (rows == 0) return Results.NotFound(new { mensagem = "Medida nÃ£o encontrada." });
    return Results.Ok(new { mensagem = "Medida caseira atualizada!" });
})
.RequireAuthorization();

// DELETE /api/alimentos/{id}/medidas/{medidaId} - Excluir medida caseira
app.MapDelete("/api/alimentos/{alimentoId:int}/medidas/{medidaId:int}", async (int alimentoId, int medidaId, ClaimsPrincipal user) =>
{
    var perfil = user.FindFirst("perfil")?.Value;
    if (perfil != "super_admin" && perfil != "admin" && perfil != "profissional")
        return Results.Forbid();

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM alimentos_medidas_caseiras WHERE id = @id AND alimento_id = @alimento_id;", conn);
    cmd.Parameters.AddWithValue("id", medidaId);
    cmd.Parameters.AddWithValue("alimento_id", alimentoId);
    int rows = await cmd.ExecuteNonQueryAsync();
    if (rows == 0) return Results.NotFound(new { mensagem = "Medida nÃ£o encontrada." });
    return Results.Ok(new { mensagem = "Medida caseira excluÃ­da!" });
})
.RequireAuthorization();

// ====================================================== CHECK-INS============================================================
app.MapPost("/api/checkins", async (CheckinInput input, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, input.PacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO checkins_semanais (paciente_id, empresa_id, peso_atual_kg, adesao_plano_porcentagem, observacoes_paciente)
        VALUES (@paciente_id, @emp, @peso, @adesao, @obs) RETURNING id;", conn);

    cmd.Parameters.AddWithValue("paciente_id", input.PacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("peso", input.PesoAtualKg);
    cmd.Parameters.AddWithValue("adesao", input.AdesaoPlanoPorcentagem);
    cmd.Parameters.AddWithValue("obs", input.Observacoes ?? "");

    var checkinId = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/checkins/{checkinId}", new { id = checkinId, mensagem = "Check-in registrado com sucesso!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));
app.MapGet("/api/checkins/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    var checkins = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT id, data_checkin, peso_atual_kg, adesao_plano_porcentagem, observacoes_paciente FROM checkins_semanais WHERE paciente_id = @id ORDER BY data_checkin DESC, id DESC;", conn);
    cmd.Parameters.AddWithValue("id", pacienteId);
    using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        checkins.Add(new
        {
            id = Convert.ToInt32(reader.GetValue(0)),
            data_checkin = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
            peso_atual_kg = reader.GetDecimal(2),
            adesao_plano_porcentagem = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
            observacoes_paciente = reader.IsDBNull(4) ? "" : reader.GetString(4)
        });
    }
    return Results.Ok(checkins);
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// Helper: busca Ãºltimos valores de peso/altura/%gordura de 3 fontes para um paciente
async Task<object> BuscarUltimosValoresPaciente(int pacienteId)
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var result = new Dictionary<string, object?>();

    // 1. AvaliaÃ§Ãµes fÃ­sicas (tem peso, altura, % gordura)
    using (var cmd = new NpgsqlCommand(@"
        SELECT peso_kg, altura_cm, percentual_gordura, criado_em
        FROM avaliacoes_fisicas
        WHERE paciente_id = @pid
        ORDER BY criado_em DESC, id DESC
        LIMIT 1;", conn))
    {
        cmd.Parameters.AddWithValue("pid", pacienteId);
        using var rd = await cmd.ExecuteReaderAsync();
        if (await rd.ReadAsync())
        {
            result["avaliacao_fisica"] = new
            {
                peso = rd.IsDBNull(0) ? (double?)null : Convert.ToDouble(rd.GetValue(0)),
                altura = rd.IsDBNull(1) ? (double?)null : Convert.ToDouble(rd.GetValue(1)),
                percentual_gordura = rd.IsDBNull(2) ? (double?)null : Convert.ToDouble(rd.GetValue(2)),
                data = rd.GetDateTime(3).ToString("yyyy-MM-dd"),
                fonte = "avaliacao_fisica"
            };
        }
    }

    // 2. EvoluÃ§Ã£o (tem peso, % gordura, medidas)
    using (var cmd = new NpgsqlCommand(@"
        SELECT peso_kg, percentual_gordura, cintura_cm, quadril_cm, abdomen_cm, braco_cm, coxa_cm, data_registro
        FROM evolucao_registros
        WHERE paciente_id = @pid
        ORDER BY data_registro DESC, id DESC
        LIMIT 1;", conn))
    {
        cmd.Parameters.AddWithValue("pid", pacienteId);
        using var rd = await cmd.ExecuteReaderAsync();
        if (await rd.ReadAsync())
        {
            result["evolucao"] = new
            {
                peso = rd.IsDBNull(0) ? (double?)null : Convert.ToDouble(rd.GetValue(0)),
                percentual_gordura = rd.IsDBNull(1) ? (double?)null : Convert.ToDouble(rd.GetValue(1)),
                cintura = rd.IsDBNull(2) ? (double?)null : Convert.ToDouble(rd.GetValue(2)),
                quadril = rd.IsDBNull(3) ? (double?)null : Convert.ToDouble(rd.GetValue(3)),
                abdomen = rd.IsDBNull(4) ? (double?)null : Convert.ToDouble(rd.GetValue(4)),
                braco = rd.IsDBNull(5) ? (double?)null : Convert.ToDouble(rd.GetValue(5)),
                coxa = rd.IsDBNull(6) ? (double?)null : Convert.ToDouble(rd.GetValue(6)),
                data = rd.GetDateTime(7).ToString("yyyy-MM-dd"),
                fonte = "evolucao"
            };
        }
    }

    // 3. Check-ins semanais (tem peso)
    using (var cmd = new NpgsqlCommand(@"
        SELECT peso_atual_kg, data_checkin
        FROM checkins_semanais
        WHERE paciente_id = @pid
        ORDER BY data_checkin DESC, id DESC
        LIMIT 1;", conn))
    {
        cmd.Parameters.AddWithValue("pid", pacienteId);
        using var rd = await cmd.ExecuteReaderAsync();
        if (await rd.ReadAsync())
        {
            result["checkin"] = new
            {
                peso = rd.IsDBNull(0) ? (double?)null : Convert.ToDouble(rd.GetValue(0)),
                data = rd.GetDateTime(1).ToString("yyyy-MM-dd"),
                fonte = "checkin"
            };
        }
    }

    // Determinar o valor mais recente geral (para sugestÃ£o rÃ¡pida)
    var todasFontes = new List<dynamic>();
    if (result.ContainsKey("avaliacao_fisica")) todasFontes.Add(result["avaliacao_fisica"]);
    if (result.ContainsKey("evolucao")) todasFontes.Add(result["evolucao"]);
    if (result.ContainsKey("checkin")) todasFontes.Add(result["checkin"]);

    dynamic? maisRecente = null;
    if (todasFontes.Count > 0)
    {
        maisRecente = todasFontes.OrderByDescending(x => DateTime.Parse(((dynamic)x).data)).First();
    }

    return new
    {
        paciente_id = pacienteId,
        fontes = result,
        mais_recente = maisRecente != null ? new
        {
            peso = maisRecente.peso,
            altura = maisRecente.altura,
            percentual_gordura = maisRecente.percentual_gordura,
            data = maisRecente.data,
            fonte = maisRecente.fonte
        } : null
    };
}

// ==================================================== HISTÃ“RICO DE VALORES (Equipe) ==========================================
// GET /api/historico-valores/{pacienteId} - Ãšltimo peso, altura, % gordura e data de avaliacoes_fisicas, evolucao_registros e checkins_semanais
app.MapGet("/api/historico-valores/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);

    // Validar se paciente pertence Ã  empresa do token
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using (var cmd = new NpgsqlCommand("SELECT 1 FROM pacientes WHERE id = @id AND empresa_id = @emp", conn))
    {
        cmd.Parameters.AddWithValue("id", pacienteId);
        cmd.Parameters.AddWithValue("emp", emp);
        if (await cmd.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });
    }

    var dados = await BuscarUltimosValoresPaciente(pacienteId);
    return Results.Ok(dados);
});

// ========================================================== ANAMNESE ===========================================================
// POST /api/anamnese - Salva/atualiza anamnese (nutricionista/admin da empresa, ou paciente prÃ³prio)
app.MapPost("/api/anamnese", async (AnamneseInput input, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    string? tipo = user.FindFirst("tipo")?.Value;
    int? pid = user.FindFirst("pid")?.Value != null ? int.Parse(user.FindFirst("pid")!.Value) : null;
    string? perfil = user.FindFirst("perfil")?.Value;

    // Validar ownership: paciente sÃ³ pode gravar a prÃ³pria anamnese
    if (tipo == "paciente" && pid.HasValue && pid.Value != input.PacienteId)
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    // Validar se paciente pertence Ã  empresa do token
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using (var cmdVal = new NpgsqlCommand("SELECT 1 FROM pacientes WHERE id = @id AND empresa_id = @emp", conn))
    {
        cmdVal.Parameters.AddWithValue("id", input.PacienteId);
        cmdVal.Parameters.AddWithValue("emp", emp);
        if (await cmdVal.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });
    }

    // Perfil: nutricionista/admin/profissional (equipe) podem gravar; paciente (prÃ³prio) tambÃ©m por enquanto
    if (tipo == "equipe" && !(perfil == "nutricionista" || perfil == "admin" || perfil == "profissional"))
        return Results.Forbid();

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO anamneses (paciente_id, empresa_id, historico_medico, alergias, intolerancias, rotina_diaria, historico_familiar, dados_extendidos)
        VALUES (@paciente_id, @emp, @historico, @alergias, @intolerancias, @rotina, @familiar, @dados_extendidos)
        ON CONFLICT (paciente_id) DO UPDATE SET
            historico_medico = EXCLUDED.historico_medico,
            alergias = EXCLUDED.alergias,
            intolerancias = EXCLUDED.intolerancias,
            rotina_diaria = EXCLUDED.rotina_diaria,
            historico_familiar = EXCLUDED.historico_familiar,
            dados_extendidos = EXCLUDED.dados_extendidos,
            atualizado_em = CURRENT_TIMESTAMP
        RETURNING id;", conn);

    cmd.Parameters.AddWithValue("paciente_id", input.PacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("historico", input.HistoricoMedico ?? "");
    cmd.Parameters.AddWithValue("alergias", input.Alergias ?? "");
    cmd.Parameters.AddWithValue("intolerancias", input.Intolerancias ?? "");
    cmd.Parameters.AddWithValue("rotina", input.RotinaDiaria ?? "");
    cmd.Parameters.AddWithValue("familiar", input.HistoricoFamiliar ?? "");
    var dadosExtendidosJson = input.DadosExtendidos != null ? System.Text.Json.JsonSerializer.Serialize(input.DadosExtendidos) : "{}";
    cmd.Parameters.Add(new NpgsqlParameter("dados_extendidos", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = dadosExtendidosJson });

    var id = await cmd.ExecuteScalarAsync();
    return Results.Ok(new { id, mensagem = "Anamnese salva com sucesso!" });
}).RequireAuthorization();

// GET /api/anamnese/{pacienteId} - LÃª anamnese (nutricionista/admin da empresa, ou paciente prÃ³prio)
app.MapGet("/api/anamnese/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    string? tipo = user.FindFirst("tipo")?.Value;
    int? pid = user.FindFirst("pid")?.Value != null ? int.Parse(user.FindFirst("pid")!.Value) : null;
    string? perfil = user.FindFirst("perfil")?.Value;

    // Validar ownership: paciente sÃ³ pode ler a prÃ³pria anamnese
    if (tipo == "paciente" && pid.HasValue && pid.Value != pacienteId)
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    // Validar se paciente pertence Ã  empresa do token
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using (var cmdVal = new NpgsqlCommand("SELECT 1 FROM pacientes WHERE id = @id AND empresa_id = @emp", conn))
    {
        cmdVal.Parameters.AddWithValue("id", pacienteId);
        cmdVal.Parameters.AddWithValue("emp", emp);
        if (await cmdVal.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });
    }

    // Perfil: nutricionista/admin/profissional (equipe) podem ler; paciente (prÃ³prio) tambÃ©m
    if (tipo == "equipe" && !(perfil == "nutricionista" || perfil == "admin" || perfil == "profissional"))
        return Results.Forbid();

    using var cmd = new NpgsqlCommand("SELECT historico_medico, alergias, intolerancias, rotina_diaria, historico_familiar, dados_extendidos FROM anamneses WHERE paciente_id = @id;", conn);
    cmd.Parameters.AddWithValue("id", pacienteId);
    using var reader = await cmd.ExecuteReaderAsync();

    if (await reader.ReadAsync())
    {
        var dadosExtendidos = reader.IsDBNull(5) ? "{}" : reader.GetString(5);
        return Results.Ok(new
        {
            historico_medico = reader.IsDBNull(0) ? "" : reader.GetString(0),
            alergias = reader.IsDBNull(1) ? "" : reader.GetString(1),
            intolerancias = reader.IsDBNull(2) ? "" : reader.GetString(2),
            rotina_diaria = reader.IsDBNull(3) ? "" : reader.GetString(3),
            historico_familiar = reader.IsDBNull(4) ? "" : reader.GetString(4),
            dados_extendidos = string.IsNullOrEmpty(dadosExtendidos) ? "{}" : dadosExtendidos
        });
    }

    return Results.Ok(new
    {
        historico_medico = "",
        alergias = "",
        intolerancias = "",
        rotina_diaria = "",
        historico_familiar = "",
        dados_extendidos = "{}"
    });
}).RequireAuthorization();

// =============================================== PLANO ALIMENTAR: REFEIÃ‡Ã•ES =====================================================
app.MapPost("/api/planos/refeicao", async (RefeicaoInput input, ClaimsPrincipal user) =>
{
    if (string.IsNullOrWhiteSpace(input.NomeRefeicao))
        return Results.BadRequest(new { mensagem = "Informe o nome da refeiÃ§Ã£o." });

    int emp = Auth.Empresa(user);

    // Validar se paciente pertence Ã  empresa do token
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using (var cmdVal = new NpgsqlCommand("SELECT 1 FROM pacientes WHERE id = @id AND empresa_id = @emp", conn))
    {
        cmdVal.Parameters.AddWithValue("id", input.PacienteId);
        cmdVal.Parameters.AddWithValue("emp", emp);
        if (await cmdVal.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });
    }

    // Um plano por paciente (cria sÃ³ se nÃ£o existir)
    using var cmdPlano = new NpgsqlCommand(@"
        INSERT INTO planos_alimentares (paciente_id, empresa_id, nome_plano, meta_calorica_kcal)
        VALUES (@paciente_id, @emp, 'Plano Personalizado', 2000)
        ON CONFLICT (paciente_id) DO UPDATE SET paciente_id = EXCLUDED.paciente_id
        RETURNING id;", conn);
    cmdPlano.Parameters.AddWithValue("paciente_id", input.PacienteId);
    cmdPlano.Parameters.AddWithValue("emp", emp);
    var planoId = Convert.ToInt32(await cmdPlano.ExecuteScalarAsync());

    using var cmdRef = new NpgsqlCommand(@"
        INSERT INTO refeicoes (plano_id, empresa_id, nome_refeicao, horario, observacoes)
        VALUES (@plano_id, @emp, @nome, @horario::time, @obs)
        ON CONFLICT (plano_id, nome_refeicao) DO NOTHING
        RETURNING id;", conn);
    cmdRef.Parameters.AddWithValue("plano_id", planoId);
    cmdRef.Parameters.AddWithValue("emp", emp);
    cmdRef.Parameters.AddWithValue("nome", input.NomeRefeicao.Trim());
    cmdRef.Parameters.AddWithValue("horario", string.IsNullOrWhiteSpace(input.Horario) ? "08:00" : input.Horario);
    cmdRef.Parameters.AddWithValue("obs", input.Observacoes ?? "");

    var id = await cmdRef.ExecuteScalarAsync();
    if (id is null)
        return Results.Conflict(new { mensagem = "JÃ¡ existe uma refeiÃ§Ã£o com esse nome neste plano." });

    return Results.Created($"/api/planos/refeicao/{id}", new { id, mensagem = "RefeiÃ§Ã£o criada!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPut("/api/planos/refeicao/{id}", async (int id, RefeicaoInput input, ClaimsPrincipal user) =>
{
    if (string.IsNullOrWhiteSpace(input.NomeRefeicao))
        return Results.BadRequest(new { mensagem = "Informe o nome da refeiÃ§Ã£o." });

    int emp = Auth.Empresa(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        UPDATE refeicoes
        SET nome_refeicao = @nome, horario = @horario::time, observacoes = @obs
        WHERE id = @id AND empresa_id = @emp;", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("nome", input.NomeRefeicao.Trim());
    cmd.Parameters.AddWithValue("horario", string.IsNullOrWhiteSpace(input.Horario) ? "08:00" : input.Horario);
    cmd.Parameters.AddWithValue("obs", input.Observacoes ?? "");

    try
    {
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0
            ? Results.Ok(new { mensagem = "RefeiÃ§Ã£o atualizada!" })
            : Results.NotFound(new { mensagem = "RefeiÃ§Ã£o nÃ£o encontrada." });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        return Results.Conflict(new { mensagem = "JÃ¡ existe uma refeiÃ§Ã£o com esse nome neste plano." });
    }
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapDelete("/api/planos/refeicao/{id}", async (int id, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM refeicoes WHERE id = @id AND empresa_id = @emp", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "RefeiÃ§Ã£o excluÃ­da!" })
        : Results.NotFound(new { mensagem = "RefeiÃ§Ã£o nÃ£o encontrada." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));
// Exclui uma opÃ§Ã£o inteira (todos os itens e substituiÃ§Ãµes dela)
app.MapDelete("/api/planos/refeicao/{refeicaoId}/opcao/{opcao}", async (int refeicaoId, int opcao, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    // Valida que a refeiÃ§Ã£o pertence Ã  empresa do token
    using (var cmdCheck = new NpgsqlCommand("SELECT 1 FROM refeicoes WHERE id = @r AND empresa_id = @emp", conn))
    {
        cmdCheck.Parameters.AddWithValue("r", refeicaoId);
        cmdCheck.Parameters.AddWithValue("emp", emp);
        if (await cmdCheck.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "RefeiÃ§Ã£o nÃ£o encontrada." });
    }
    using var cmd = new NpgsqlCommand(
        "DELETE FROM itens_refeicao WHERE refeicao_id = @r AND opcao = @o", conn);
    cmd.Parameters.AddWithValue("r", refeicaoId);
    cmd.Parameters.AddWithValue("o", opcao);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensagem = "OpÃ§Ã£o excluÃ­da!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));
//===================================PLANO ALIMENTAR: ITENS (alimentos, opÃ§Ãµes e substituiÃ§Ãµes)========================================================
app.MapPost("/api/planos/item", async (ItemInput input, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    int refeicaoId = input.RefeicaoId;
    int opcao = input.Opcao > 0 ? input.Opcao : 1;

    // SubstituiÃ§Ã£o herda refeiÃ§Ã£o e opÃ§Ã£o do item original
    if (input.SubstituiItemId.HasValue)
    {
        using var cmdPai = new NpgsqlCommand(
            "SELECT refeicao_id, opcao FROM itens_refeicao WHERE id = @id AND substitui_item_id IS NULL;", conn);
        cmdPai.Parameters.AddWithValue("id", input.SubstituiItemId.Value);
        using var rd = await cmdPai.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
            return Results.BadRequest(new { mensagem = "Item original nÃ£o encontrado." });
        refeicaoId = rd.GetInt32(0);
        opcao = rd.GetInt32(1);
    }

    // Valida que a refeiÃ§Ã£o pertence Ã  empresa do token
    using (var cmdCheck = new NpgsqlCommand(
        "SELECT 1 FROM refeicoes r JOIN planos_alimentares p ON p.id = r.plano_id WHERE r.id = @r AND p.empresa_id = @emp", conn))
    {
        cmdCheck.Parameters.AddWithValue("r", refeicaoId);
        cmdCheck.Parameters.AddWithValue("emp", emp);
        if (await cmdCheck.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "RefeiÃ§Ã£o nÃ£o encontrada." });
    }

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO itens_refeicao
            (refeicao_id, empresa_id, opcao, substitui_item_id, alimento_nome_livre, quantidade_g, medida_caseira,
             kcal_manual, proteinas_manual, carbos_manual, gorduras_manual, e_substituicao)
        VALUES
            (@refeicao_id, @emp, @opcao, @sub, @nome, @qtd, @medida, @kcal, @prot, @carbo, @gord, @e_sub)
        RETURNING id;", conn);

    cmd.Parameters.AddWithValue("refeicao_id", refeicaoId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("opcao", opcao);
    cmd.Parameters.Add(new NpgsqlParameter("sub", NpgsqlTypes.NpgsqlDbType.Integer)
    {
        Value = (object?)input.SubstituiItemId ?? DBNull.Value
    });
    cmd.Parameters.AddWithValue("nome", string.IsNullOrWhiteSpace(input.AlimentoNome) ? "Alimento" : input.AlimentoNome.Trim());
    cmd.Parameters.AddWithValue("qtd", input.QuantidadeG);
    cmd.Parameters.AddWithValue("medida", input.MedidaCaseira ?? "");
    cmd.Parameters.AddWithValue("kcal", input.Kcal);
    cmd.Parameters.AddWithValue("prot", input.Proteinas);
    cmd.Parameters.AddWithValue("carbo", input.Carbos);
    cmd.Parameters.AddWithValue("gord", input.Gorduras);
    cmd.Parameters.AddWithValue("e_sub", input.SubstituiItemId.HasValue);

    var id = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/planos/item/{id}", new { id, mensagem = "Item adicionado!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// POST /api/planos/item/calcular - Calcula macros via Python (TACO) e insere item
app.MapPost("/api/planos/item/calcular", async (ItemCalcularInput input, ClaimsPrincipal user, IHttpClientFactory clientFactory, ILogger<Program> logger) =>
{
    int emp = Auth.Empresa(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    int refeicaoId = input.RefeicaoId;
    int opcao = input.Opcao > 0 ? input.Opcao : 1;

    // SubstituiÃ§Ã£o herda refeiÃ§Ã£o e opÃ§Ã£o do item original
    if (input.SubstituiItemId.HasValue)
    {
        using var cmdPai = new NpgsqlCommand(
            "SELECT refeicao_id, opcao FROM itens_refeicao WHERE id = @id AND substitui_item_id IS NULL;", conn);
        cmdPai.Parameters.AddWithValue("id", input.SubstituiItemId.Value);
        using var rd = await cmdPai.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
            return Results.BadRequest(new { mensagem = "Item original nÃ£o encontrado." });
        refeicaoId = rd.GetInt32(0);
        opcao = rd.GetInt32(1);
    }

    // Valida que a refeiÃ§Ã£o pertence Ã  empresa do token
    using (var cmdCheck = new NpgsqlCommand(
        "SELECT 1 FROM refeicoes r JOIN planos_alimentares p ON p.id = r.plano_id WHERE r.id = @r AND p.empresa_id = @emp", conn))
    {
        cmdCheck.Parameters.AddWithValue("r", refeicaoId);
        cmdCheck.Parameters.AddWithValue("emp", emp);
        if (await cmdCheck.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "RefeiÃ§Ã£o nÃ£o encontrada." });
    }

    // Chama o microserviÃ§o Python para calcular
    var httpClient = clientFactory.CreateClient("Python");
    var payloadPython = new
    {
        alimento_id = input.AlimentoId,
        target_kcal = input.TargetKcal,
        target_proteina = input.TargetProteina,
        target_carbo = input.TargetCarbo,
        target_gordura = input.TargetGordura,
        gramas = input.Gramas,
        medida_caseira = input.MedidaCaseira,
        quantidade_medida = input.QuantidadeMedida,
        observacao_medida = input.ObservacaoMedida ?? "",
        refeicao = input.Refeicao ?? "lanche",
        observacao = input.Observacao ?? ""
    };

    var content = new StringContent(JsonSerializer.Serialize(payloadPython), Encoding.UTF8, "application/json");

    HttpResponseMessage responsePython;
    try
    {
        responsePython = await httpClient.PostAsync($"{pythonBaseUrl}/calcular-item", content);
    }
    catch (HttpRequestException ex)
    {
        logger.LogError(ex, "Falha de rede/ligaÃ§Ã£o ao chamar Python Service /calcular-item");
        return Results.Json(new { mensagem = "O serviÃ§o de cÃ¡lculos nutricionais estÃ¡ temporariamente indisponÃ­vel. Tente novamente em alguns instantes." }, statusCode: 503);
    }
    catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || ex.CancellationToken.IsCancellationRequested)
    {
        logger.LogError(ex, "Timeout ao chamar Python Service /calcular-item (60s)");
        return Results.Json(new { mensagem = "O serviÃ§o de cÃ¡lculos nutricionais estÃ¡ temporariamente indisponÃ­vel. Tente novamente em alguns instantes." }, statusCode: 503);
    }

    if (!responsePython.IsSuccessStatusCode)
    {
        var errorBody = await responsePython.Content.ReadAsStringAsync();
        var logBody = errorBody.Length > 500 ? errorBody[..500] + "..." : errorBody;

        if ((int)responsePython.StatusCode >= 500 || responsePython.StatusCode == System.Net.HttpStatusCode.BadGateway || responsePython.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable || responsePython.StatusCode == System.Net.HttpStatusCode.GatewayTimeout || responsePython.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogWarning("Python Service /calcular-item retornou {StatusCode}: {Body}", responsePython.StatusCode, logBody);
            return Results.Json(new { mensagem = "O serviÃ§o de cÃ¡lculos nutricionais estÃ¡ temporariamente indisponÃ­vel. Tente novamente em alguns instantes." }, statusCode: 503);
        }
        else if ((int)responsePython.StatusCode >= 400)
        {
            logger.LogWarning("Python Service /calcular-item retornou erro 4xx {StatusCode}: {Body}", responsePython.StatusCode, logBody);
            return Results.Json(new { mensagem = "Dados invÃ¡lidos para cÃ¡lculo nutricional. Verifique os campos informados." }, statusCode: 400);
        }
        else
        {
            logger.LogWarning("Python Service /calcular-item retornou {StatusCode}: {Body}", responsePython.StatusCode, logBody);
            return Results.Json(new { mensagem = "Erro inesperado no serviÃ§o de cÃ¡lculos nutricionais." }, statusCode: 502);
        }
    }

    var jsonResp = await responsePython.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(jsonResp);
    var root = doc.RootElement;

    // Extrai valores calculados (podem ser null se dado nÃ£o disponÃ­vel na TACO)
    string alimentoNome = root.GetProperty("alimento_nome").GetString()!;
    double gramas = root.GetProperty("gramas").GetDouble();

    // Helper para ler double opcional do JSON
    double? GetOptionalDouble(JsonElement root, string propertyName)
    {
        var prop = root.GetProperty(propertyName);
        return prop.ValueKind == JsonValueKind.Null ? null : prop.GetDouble();
    }

    double? kcal = GetOptionalDouble(root, "kcal");
    double? proteina = GetOptionalDouble(root, "proteina");
    double? carbo = GetOptionalDouble(root, "carbo");
    double? gordura = GetOptionalDouble(root, "gordura");
    string baseCalculo = root.GetProperty("base_calculo").GetString()!;

    // Se dados insuficientes, retorna erro informativo
    if (baseCalculo == "dados_insuficientes")
    {
        return Results.BadRequest(new
        {
            mensagem = $"Alimento '{alimentoNome}' nÃ£o possui dados nutricionais disponÃ­veis na TACO para cÃ¡lculo automÃ¡tico. Use entrada manual."
        });
    }

    // Insere o item com valores calculados (usa DBNull.Value para macros nÃ£o disponÃ­veis)
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO itens_refeicao
            (refeicao_id, empresa_id, opcao, substitui_item_id, alimento_nome_livre, quantidade_g, medida_caseira,
             kcal_manual, proteinas_manual, carbos_manual, gorduras_manual, e_substituicao)
        VALUES
            (@refeicao_id, @emp, @opcao, @sub, @nome, @qtd, @medida, @kcal, @prot, @carbo, @gord, @e_sub)
        RETURNING id;", conn);

    cmd.Parameters.AddWithValue("refeicao_id", refeicaoId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("opcao", opcao);
    cmd.Parameters.Add(new NpgsqlParameter("sub", NpgsqlTypes.NpgsqlDbType.Integer)
    {
        Value = (object?)input.SubstituiItemId ?? DBNull.Value
    });
    cmd.Parameters.AddWithValue("nome", alimentoNome);
    cmd.Parameters.AddWithValue("qtd", gramas);
    cmd.Parameters.AddWithValue("medida", $"{gramas}g (calc: {baseCalculo})");
    cmd.Parameters.Add(new NpgsqlParameter("kcal", NpgsqlTypes.NpgsqlDbType.Numeric) { Value = (object?)kcal ?? DBNull.Value });
    cmd.Parameters.Add(new NpgsqlParameter("prot", NpgsqlTypes.NpgsqlDbType.Numeric) { Value = (object?)proteina ?? DBNull.Value });
    cmd.Parameters.Add(new NpgsqlParameter("carbo", NpgsqlTypes.NpgsqlDbType.Numeric) { Value = (object?)carbo ?? DBNull.Value });
    cmd.Parameters.Add(new NpgsqlParameter("gord", NpgsqlTypes.NpgsqlDbType.Numeric) { Value = (object?)gordura ?? DBNull.Value });
    cmd.Parameters.AddWithValue("e_sub", input.SubstituiItemId.HasValue);

    var newId = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/planos/item/{newId}", new
    {
        id = newId,
        mensagem = "Item adicionado com cÃ¡lculo automÃ¡tico!",
        calculo = new { baseCalculo = baseCalculo, gramas, kcal, proteina, carbo, gordura }
    });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPut("/api/planos/item/{id}", async (int id, ItemInput input, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Valida que o item pertence Ã  empresa do token
    using (var cmdCheck = new NpgsqlCommand(
        "SELECT 1 FROM itens_refeicao ir JOIN refeicoes r ON r.id = ir.refeicao_id JOIN planos_alimentares p ON p.id = r.plano_id WHERE ir.id = @id AND p.empresa_id = @emp", conn))
    {
        cmdCheck.Parameters.AddWithValue("id", id);
        cmdCheck.Parameters.AddWithValue("emp", emp);
        if (await cmdCheck.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "Item nÃ£o encontrado." });
    }

    using var cmd = new NpgsqlCommand(@"
        UPDATE itens_refeicao SET
            alimento_nome_livre = @nome, quantidade_g = @qtd, medida_caseira = @medida,
            kcal_manual = @kcal, proteinas_manual = @prot, carbos_manual = @carbo, gorduras_manual = @gord
        WHERE id = @id;", conn);

    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("nome", string.IsNullOrWhiteSpace(input.AlimentoNome) ? "Alimento" : input.AlimentoNome.Trim());
    cmd.Parameters.AddWithValue("qtd", input.QuantidadeG);
    cmd.Parameters.AddWithValue("medida", input.MedidaCaseira ?? "");
    cmd.Parameters.AddWithValue("kcal", input.Kcal);
    cmd.Parameters.AddWithValue("prot", input.Proteinas);
    cmd.Parameters.AddWithValue("carbo", input.Carbos);
    cmd.Parameters.AddWithValue("gord", input.Gorduras);

    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Item atualizado!" })
        : Results.NotFound(new { mensagem = "Item nÃ£o encontrado." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapDelete("/api/planos/item/{id}", async (int id, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Valida que o item pertence Ã  empresa do token
    using (var cmdCheck = new NpgsqlCommand(
        "SELECT 1 FROM itens_refeicao ir JOIN refeicoes r ON r.id = ir.refeicao_id JOIN planos_alimentares p ON p.id = r.plano_id WHERE ir.id = @id AND p.empresa_id = @emp", conn))
    {
        cmdCheck.Parameters.AddWithValue("id", id);
        cmdCheck.Parameters.AddWithValue("emp", emp);
        if (await cmdCheck.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "Item nÃ£o encontrado." });
    }

    using var cmd = new NpgsqlCommand("DELETE FROM itens_refeicao WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);

    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    return rowsAffected > 0
        ? Results.Ok(new { mensagem = "Item excluÃ­do com sucesso!" })
        : Results.NotFound(new { mensagem = "Item nÃ£o encontrado." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// ============================================PLANO ALIMENTAR: LEITURA E VERSÃ•ES===========================================
// Plano completo (refeiÃ§Ãµes â†’ itens, com opÃ§Ã£o e substituiÃ§Ã£o)
app.MapGet("/api/planos/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    return Results.Ok(await PlanoHelper.Montar(connString, pacienteId, Auth.Empresa(user)));
}).AddEndpointFilter(new TenantIsolationFilter(connString));
// Salvar versÃ£o (snapshot do plano + orientaÃ§Ãµes)
app.MapPost("/api/planos/{pacienteId}/versoes", async (int pacienteId, VersaoInput input, ClaimsPrincipal user) =>
{
    var plano = await PlanoHelper.Montar(connString, pacienteId, Auth.Empresa(user));
    var refeicoes = (List<Dictionary<string, object?>>)plano["refeicoes"]!;
    if (refeicoes.Count == 0)
        return Results.BadRequest(new { mensagem = "O plano estÃ¡ vazio. Adicione refeiÃ§Ãµes antes de salvar uma versÃ£o." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    string orientacoes = "";
    using (var cmdO = new NpgsqlCommand("SELECT texto FROM orientacoes_nutricionais WHERE paciente_id = @p", conn))
    {
        cmdO.Parameters.AddWithValue("p", pacienteId);
        orientacoes = (await cmdO.ExecuteScalarAsync())?.ToString() ?? "";
    }

    var snapshot = JsonSerializer.Serialize(new { plano, orientacoes });

    int numero;
    using (var cmdN = new NpgsqlCommand(
        "SELECT COALESCE(MAX(numero), 0) + 1 FROM planos_versoes WHERE paciente_id = @p", conn))
    {
        cmdN.Parameters.AddWithValue("p", pacienteId);
        numero = Convert.ToInt32(await cmdN.ExecuteScalarAsync());
    }

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO planos_versoes (paciente_id, empresa_id, numero, descricao, snapshot)
        VALUES (@p, (SELECT empresa_id FROM pacientes WHERE id = @p), @n, @d, @s) RETURNING id;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
    cmd.Parameters.AddWithValue("n", numero);
    cmd.Parameters.AddWithValue("d", input.Descricao ?? "");
    cmd.Parameters.Add(new NpgsqlParameter("s", NpgsqlTypes.NpgsqlDbType.Jsonb) { Value = snapshot });

    var id = await cmd.ExecuteScalarAsync();
    return Results.Ok(new { id, numero, mensagem = $"VersÃ£o {numero} salva!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapGet("/api/planos/{pacienteId}/versoes", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT id, numero, COALESCE(descricao, ''), criado_em
        FROM planos_versoes WHERE paciente_id = @p AND empresa_id = @emp ORDER BY numero DESC;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        lista.Add(new
        {
            id = rd.GetInt32(0),
            numero = rd.GetInt32(1),
            descricao = rd.GetString(2),
            criado_em = rd.GetDateTime(3).ToString("yyyy-MM-ddTHH:mm:ss")
        });
    }
    return Results.Ok(lista);
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapGet("/api/planos/versao/{id}", async (int id, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    var tipoClaim = user.FindFirst("tipo")?.Value;
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Para token de paciente: validar que a versÃ£o pertence ao prÃ³prio paciente
    if (tipoClaim == "paciente")
    {
        var pidClaim = user.FindFirst("pid");
        if (pidClaim == null || !int.TryParse(pidClaim.Value, out var tokenPacienteId))
            return Results.NotFound(new { mensagem = "VersÃ£o nÃ£o encontrada." });

        using var cmd = new NpgsqlCommand(@"
            SELECT numero, COALESCE(descricao, ''), criado_em, snapshot::text
            FROM planos_versoes WHERE id = @id AND empresa_id = @emp AND paciente_id = @pid;", conn);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("emp", emp);
        cmd.Parameters.AddWithValue("pid", tokenPacienteId);
        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
            return Results.NotFound(new { mensagem = "VersÃ£o nÃ£o encontrada." });

        return Results.Ok(new
        {
            numero = rd.GetInt32(0),
            descricao = rd.GetString(1),
            criado_em = rd.GetDateTime(2).ToString("yyyy-MM-ddTHH:mm:ss"),
            snapshot = JsonDocument.Parse(rd.GetString(3)).RootElement
        });
    }

    // Token de equipe: validar apenas empresa_id
    using var cmdEquipe = new NpgsqlCommand(@"
        SELECT numero, COALESCE(descricao, ''), criado_em, snapshot::text
        FROM planos_versoes WHERE id = @id AND empresa_id = @emp;", conn);
    cmdEquipe.Parameters.AddWithValue("id", id);
    cmdEquipe.Parameters.AddWithValue("emp", emp);
    using var rdEquipe = await cmdEquipe.ExecuteReaderAsync();
    if (!await rdEquipe.ReadAsync())
        return Results.NotFound(new { mensagem = "VersÃ£o nÃ£o encontrada." });

    return Results.Ok(new
    {
        numero = rdEquipe.GetInt32(0),
        descricao = rdEquipe.GetString(1),
        criado_em = rdEquipe.GetDateTime(2).ToString("yyyy-MM-ddTHH:mm:ss"),
        snapshot = JsonDocument.Parse(rdEquipe.GetString(3)).RootElement
    });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// ======================================ORIENTAÃ‡Ã•ES NUTRICIONAIS===============================================================
app.MapPost("/api/orientacoes", async (OrientacaoDto dto, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, dto.PacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO orientacoes_nutricionais (paciente_id, empresa_id, texto, atualizado_em)
        VALUES (@pacienteId, @emp, @texto, CURRENT_TIMESTAMP)
        ON CONFLICT (paciente_id)
        DO UPDATE SET texto = EXCLUDED.texto, atualizado_em = CURRENT_TIMESTAMP;", conn);
    cmd.Parameters.AddWithValue("pacienteId", dto.PacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("texto", dto.Texto ?? "");
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensagem = "OrientaÃ§Ãµes salvas com sucesso!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapGet("/api/orientacoes/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT texto FROM orientacoes_nutricionais WHERE paciente_id = @id AND empresa_id = @emp", conn);
    cmd.Parameters.AddWithValue("id", pacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    var resultado = await cmd.ExecuteScalarAsync();
    return Results.Ok(new { texto = resultado?.ToString() ?? "" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));
// ============================================= METAS DO PACIENTE=====================================================================
var tiposMeta = new HashSet<string> { "agua", "frutas", "refeicoes", "atividade", "sono", "outro" };
var resultadosMeta = new HashSet<string> { "mantida", "parcial", "nao_mantida" };
var statusMeta = new HashSet<string> { "ativa", "concluida", "arquivada" };

// Lista as metas do paciente jÃ¡ com o histÃ³rico de acompanhamento
app.MapGet("/api/metas/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var metas = new List<Dictionary<string, object?>>();
    var porId = new Dictionary<int, List<object>>();

    using (var cmd = new NpgsqlCommand(@"
        SELECT id, titulo, COALESCE(descricao, ''), tipo, prazo, status
        FROM metas_paciente
        WHERE paciente_id = @p AND empresa_id = @emp
        ORDER BY CASE status WHEN 'ativa' THEN 0 WHEN 'concluida' THEN 1 ELSE 2 END, id DESC;", conn))
    {
        cmd.Parameters.AddWithValue("p", pacienteId);
        cmd.Parameters.AddWithValue("emp", emp);
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            var acs = new List<object>();
            int id = rd.GetInt32(0);
            porId[id] = acs;
            metas.Add(new()
            {
                ["id"] = id,
                ["titulo"] = rd.GetString(1),
                ["descricao"] = rd.GetString(2),
                ["tipo"] = rd.GetString(3),
                ["prazo"] = rd.IsDBNull(4) ? null : rd.GetDateTime(4).ToString("yyyy-MM-dd"),
                ["status"] = rd.GetString(5),
                ["acompanhamentos"] = acs
            });
        }
    }

    using (var cmd = new NpgsqlCommand(@"
        SELECT a.id, a.meta_id, a.data_acompanhamento, a.resultado, COALESCE(a.observacao, '')
        FROM metas_acompanhamento a
        JOIN metas_paciente m ON m.id = a.meta_id
        WHERE m.paciente_id = @p AND m.empresa_id = @emp
        ORDER BY a.data_acompanhamento DESC, a.id DESC;", conn))
    {
        cmd.Parameters.AddWithValue("p", pacienteId);
        cmd.Parameters.AddWithValue("emp", emp);
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            if (!porId.TryGetValue(rd.GetInt32(1), out var lista)) continue;
            lista.Add(new
            {
                id = rd.GetInt32(0),
                data = rd.GetDateTime(2).ToString("yyyy-MM-dd"),
                resultado = rd.GetString(3),
                observacao = rd.GetString(4)
            });
        }
    }

    return Results.Ok(metas);
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPost("/api/metas", async (MetaInput input, ClaimsPrincipal user) =>
{
    if (string.IsNullOrWhiteSpace(input.Titulo))
        return Results.BadRequest(new { mensagem = "Informe o tÃ­tulo da meta." });

    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, input.PacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    var tipo = tiposMeta.Contains(input.Tipo ?? "") ? input.Tipo! : "outro";

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO metas_paciente (paciente_id, empresa_id, titulo, descricao, tipo, prazo)
        VALUES (@p, @emp, @titulo, @desc, @tipo, @prazo) RETURNING id;", conn);
    cmd.Parameters.AddWithValue("p", input.PacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("titulo", input.Titulo.Trim());
    cmd.Parameters.AddWithValue("desc", input.Descricao ?? "");
    cmd.Parameters.AddWithValue("tipo", tipo);
    cmd.Parameters.Add(new NpgsqlParameter("prazo", NpgsqlTypes.NpgsqlDbType.Date)
    {
        Value = DateTime.TryParse(input.Prazo, out var pz) ? (object)pz.Date : DBNull.Value
    });

    var id = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/metas/{id}", new { id, mensagem = "Meta criada!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPut("/api/metas/{id}", async (int id, MetaInput input, ClaimsPrincipal user) =>
{
    if (string.IsNullOrWhiteSpace(input.Titulo))
        return Results.BadRequest(new { mensagem = "Informe o tÃ­tulo da meta." });

    int emp = Auth.Empresa(user);

    var tipo = tiposMeta.Contains(input.Tipo ?? "") ? input.Tipo! : "outro";
    var status = statusMeta.Contains(input.Status ?? "") ? input.Status! : "ativa";

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        UPDATE metas_paciente
        SET titulo = @titulo, descricao = @desc, tipo = @tipo, prazo = @prazo, status = @status
        WHERE id = @id AND empresa_id = @emp;", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("titulo", input.Titulo.Trim());
    cmd.Parameters.AddWithValue("desc", input.Descricao ?? "");
    cmd.Parameters.AddWithValue("tipo", tipo);
    cmd.Parameters.AddWithValue("status", status);
    cmd.Parameters.Add(new NpgsqlParameter("prazo", NpgsqlTypes.NpgsqlDbType.Date)
    {
        Value = DateTime.TryParse(input.Prazo, out var pz) ? (object)pz.Date : DBNull.Value
    });

    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Meta atualizada!" })
        : Results.NotFound(new { mensagem = "Meta nÃ£o encontrada." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapDelete("/api/metas/{id}", async (int id, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM metas_paciente WHERE id = @id AND empresa_id = @emp", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Meta excluÃ­da!" })
        : Results.NotFound(new { mensagem = "Meta nÃ£o encontrada." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// Registra como a meta foi no check-in (mantida / parcial / nÃ£o mantida)
app.MapPost("/api/metas/acompanhamento", async (AcompanhamentoInput input, ClaimsPrincipal user) =>
{
    if (!resultadosMeta.Contains(input.Resultado ?? ""))
        return Results.BadRequest(new { mensagem = "Resultado invÃ¡lido." });

    int emp = Auth.Empresa(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Buscar empresa_id via meta -> paciente e validar que pertence Ã  empresa do usuÃ¡rio
    int empresaId;
    using (var cmdEmp = new NpgsqlCommand(@"
        SELECT p.empresa_id
        FROM metas_paciente m
        JOIN pacientes p ON p.id = m.paciente_id
        WHERE m.id = @metaId AND m.empresa_id = @emp", conn))
    {
        cmdEmp.Parameters.AddWithValue("metaId", input.MetaId);
        cmdEmp.Parameters.AddWithValue("emp", emp);
        var result = await cmdEmp.ExecuteScalarAsync();
        if (result == null) return Results.NotFound(new { mensagem = "Meta nÃ£o encontrada." });
        empresaId = Convert.ToInt32(result);
    }

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO metas_acompanhamento (meta_id, data_acompanhamento, resultado, observacao, empresa_id)
        VALUES (@meta, @data, @res, @obs, @emp) RETURNING id;", conn);
    cmd.Parameters.AddWithValue("meta", input.MetaId);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date)
    {
        Value = DateTime.TryParse(input.Data, out var dt) ? dt.Date : DateTime.Today
    });
    cmd.Parameters.AddWithValue("res", input.Resultado!);
    cmd.Parameters.AddWithValue("obs", input.Observacao ?? "");
    cmd.Parameters.AddWithValue("emp", empresaId);

    var id = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/metas/acompanhamento/{id}", new { id, mensagem = "Acompanhamento registrado!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapDelete("/api/metas/acompanhamento/{id}", async (int id, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM metas_acompanhamento WHERE id = @id AND empresa_id = @emp", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Acompanhamento excluÃ­do!" })
        : Results.NotFound(new { mensagem = "Acompanhamento nÃ£o encontrado." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// =====================================================================
// ARQUIVOS (fotos de evoluÃ§Ã£o e exames)
// =====================================================================
app.MapGet("/api/arquivos/{id}", async (int id, ClaimsPrincipal user, HttpContext http) =>
{
    int emp = int.Parse(user.FindFirst("empresa")!.Value);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Verificar se arquivo pertence Ã  empresa do usuÃ¡rio
    using var cmdCheck = new NpgsqlCommand(@"
        SELECT a.mime, a.dados, a.paciente_id
        FROM arquivos_paciente a
        JOIN pacientes p ON p.id = a.paciente_id
        WHERE a.id = @id AND p.empresa_id = @emp", conn);
    cmdCheck.Parameters.AddWithValue("id", id);
    cmdCheck.Parameters.AddWithValue("emp", emp);

    using var rd = await cmdCheck.ExecuteReaderAsync();
    if (!await rd.ReadAsync())
        return Results.NotFound(new { mensagem = "Arquivo nÃ£o encontrado." });

    var mime = rd.GetString(0);
    var dados = rd.GetFieldValue<byte[]>(1);
    var pacienteId = rd.GetInt64(2);

    // Auditoria: leitura de arquivo
    await RegistrarAuditoria(http, "leitura", id);

    return Results.File(dados, mime);
});

app.MapDelete("/api/arquivos/{id}", async (int id, ClaimsPrincipal user, HttpContext http) =>
{
    int emp = int.Parse(user.FindFirst("empresa")!.Value);

    // Validar token de paciente: sÃ³ pode excluir arquivos do prÃ³prio paciente
    var tipoClaim = user.FindFirst("tipo")?.Value;
    if (tipoClaim == "paciente")
    {
        var pidClaim = user.FindFirst("pid");
        if (pidClaim == null || !int.TryParse(pidClaim.Value, out var tokenPacienteId))
            return Results.NotFound(new { mensagem = "Arquivo nÃ£o encontrado." });

        using var connCheck = new NpgsqlConnection(connString);
        await connCheck.OpenAsync();
        using var cmdPid = new NpgsqlCommand("SELECT paciente_id FROM arquivos_paciente WHERE id = @id", connCheck);
        cmdPid.Parameters.AddWithValue("id", id);
        var arquivoPacienteId = await cmdPid.ExecuteScalarAsync();
        if (arquivoPacienteId == null || Convert.ToInt32(arquivoPacienteId) != tokenPacienteId)
            return Results.NotFound(new { mensagem = "Arquivo nÃ£o encontrado." });
    }

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Verificar se arquivo pertence Ã  empresa do usuÃ¡rio
    using var cmdCheck = new NpgsqlCommand(@"
        SELECT a.id FROM arquivos_paciente a
        JOIN pacientes p ON p.id = a.paciente_id
        WHERE a.id = @id AND p.empresa_id = @emp", conn);
    cmdCheck.Parameters.AddWithValue("id", id);
    cmdCheck.Parameters.AddWithValue("emp", emp);

    var exists = await cmdCheck.ExecuteScalarAsync();
    if (exists == null)
        return Results.NotFound(new { mensagem = "Arquivo nÃ£o encontrado." });

    using var cmd = new NpgsqlCommand("DELETE FROM arquivos_paciente WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);
    var rows = await cmd.ExecuteNonQueryAsync();

    if (rows > 0)
    {
        await RegistrarAuditoria(http, "exclusao", id);
        return Results.Ok(new { mensagem = "Arquivo excluÃ­do!" });
    }
    return Results.NotFound(new { mensagem = "Arquivo nÃ£o encontrado." });
});

// =====================================================================
// EVOLUÃ‡ÃƒO (peso, medidas, fotos, observaÃ§Ãµes)
// =====================================================================
app.MapGet("/api/evolucao/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var anexos = await ArquivoHelper.Listar(conn, pacienteId, "evolucao");
    var lista = new List<object>();

    using var cmd = new NpgsqlCommand(@"
        SELECT id, data_registro, peso_kg, percentual_gordura, cintura_cm, quadril_cm,
               abdomen_cm, braco_cm, coxa_cm, COALESCE(observacao, '')
        FROM evolucao_registros
        WHERE paciente_id = @p AND empresa_id = @emp
        ORDER BY data_registro DESC, id DESC;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        int id = rd.GetInt32(0);
        lista.Add(new
        {
            id,
            data = rd.GetDateTime(1).ToString("yyyy-MM-dd"),
            peso = ArquivoHelper.N(rd, 2),
            gordura = ArquivoHelper.N(rd, 3),
            cintura = ArquivoHelper.N(rd, 4),
            quadril = ArquivoHelper.N(rd, 5),
            abdomen = ArquivoHelper.N(rd, 6),
            braco = ArquivoHelper.N(rd, 7),
            coxa = ArquivoHelper.N(rd, 8),
            observacao = rd.GetString(9),
            fotos = anexos.TryGetValue(id, out var f) ? f : new List<object>()
        });
    }
    return Results.Ok(lista);
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPost("/api/evolucao", async (HttpRequest request, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    var form = await request.ReadFormAsync();
    if (!int.TryParse(form["pacienteId"], out var pacienteId))
        return Results.BadRequest(new { mensagem = "Paciente invÃ¡lido." });

    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    var c = EvolucaoCampos.Ler(form);
    if (c.Vazio && form.Files.Count == 0)
        return Results.BadRequest(new { mensagem = "Preencha ao menos um campo ou anexe uma foto." });

    var erro = ArquivoHelper.Validar(form.Files, somenteImagem: true);
    if (erro != null) return Results.BadRequest(new { mensagem = erro });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO evolucao_registros
            (paciente_id, data_registro, peso_kg, percentual_gordura, cintura_cm, quadril_cm,
             abdomen_cm, braco_cm, coxa_cm, observacao, empresa_id)
        VALUES (@p, @data, @peso, @gord, @cint, @quad, @abd, @braco, @coxa, @obs, @emp)
        RETURNING id;", conn, tx);
    cmd.Parameters.AddWithValue("p", pacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date) { Value = c.Data });
    cmd.Parameters.Add(ArquivoHelper.Num("peso", c.Peso));
    cmd.Parameters.Add(ArquivoHelper.Num("gord", c.Gordura));
    cmd.Parameters.Add(ArquivoHelper.Num("cint", c.Cintura));
    cmd.Parameters.Add(ArquivoHelper.Num("quad", c.Quadril));
    cmd.Parameters.Add(ArquivoHelper.Num("abd", c.Abdomen));
    cmd.Parameters.Add(ArquivoHelper.Num("braco", c.Braco));
    cmd.Parameters.Add(ArquivoHelper.Num("coxa", c.Coxa));
    cmd.Parameters.AddWithValue("obs", c.Obs);
    int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

    await ArquivoHelper.Salvar(conn, tx, pacienteId, "evolucao", id, form.Files);
    await tx.CommitAsync();

    return Results.Created($"/api/evolucao/{id}", new { id, mensagem = "Registro de evoluÃ§Ã£o salvo!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPut("/api/evolucao/{id}", async (int id, HttpRequest request, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    var form = await request.ReadFormAsync();
    var c = EvolucaoCampos.Ler(form);

    var erro = ArquivoHelper.Validar(form.Files, somenteImagem: true);
    if (erro != null) return Results.BadRequest(new { mensagem = erro });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    using var cmd = new NpgsqlCommand(@"
        UPDATE evolucao_registros SET
            data_registro = @data, peso_kg = @peso, percentual_gordura = @gord, cintura_cm = @cint,
            quadril_cm = @quad, abdomen_cm = @abd, braco_cm = @braco, coxa_cm = @coxa, observacao = @obs
        WHERE id = @id AND empresa_id = @emp
        RETURNING paciente_id;", conn, tx);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date) { Value = c.Data });
    cmd.Parameters.Add(ArquivoHelper.Num("peso", c.Peso));
    cmd.Parameters.Add(ArquivoHelper.Num("gord", c.Gordura));
    cmd.Parameters.Add(ArquivoHelper.Num("cint", c.Cintura));
    cmd.Parameters.Add(ArquivoHelper.Num("quad", c.Quadril));
    cmd.Parameters.Add(ArquivoHelper.Num("abd", c.Abdomen));
    cmd.Parameters.Add(ArquivoHelper.Num("braco", c.Braco));
    cmd.Parameters.Add(ArquivoHelper.Num("coxa", c.Coxa));
    cmd.Parameters.AddWithValue("obs", c.Obs);

    var pid = await cmd.ExecuteScalarAsync();
    if (pid is null)
        return Results.NotFound(new { mensagem = "Registro nÃ£o encontrado." });

    await ArquivoHelper.Salvar(conn, tx, Convert.ToInt32(pid), "evolucao", id, form.Files);
    await tx.CommitAsync();
    return Results.Ok(new { mensagem = "Registro atualizado!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapDelete("/api/evolucao/{id}", async (int id, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    using (var cmdA = new NpgsqlCommand(
        "DELETE FROM arquivos_paciente WHERE origem = 'evolucao' AND origem_id = @id", conn, tx))
    {
        cmdA.Parameters.AddWithValue("id", id);
        await cmdA.ExecuteNonQueryAsync();
    }

    using var cmd = new NpgsqlCommand("DELETE FROM evolucao_registros WHERE id = @id AND empresa_id = @emp", conn, tx);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    var rows = await cmd.ExecuteNonQueryAsync();
    await tx.CommitAsync();

    return rows > 0
        ? Results.Ok(new { mensagem = "Registro excluÃ­do!" })
        : Results.NotFound(new { mensagem = "Registro nÃ£o encontrado." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// ================================================ EXAMES=====================================================================
app.MapGet("/api/exames/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var anexos = await ArquivoHelper.Listar(conn, pacienteId, "exame");
    var lista = new List<object>();

    using var cmd = new NpgsqlCommand(@"
        SELECT id, titulo_exame, data_exame, COALESCE(observacao, ''), enviado_por
        FROM exames_paciente
        WHERE paciente_id = @p AND empresa_id = @emp
        ORDER BY data_exame DESC, id DESC;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        int id = rd.GetInt32(0);
        lista.Add(new
        {
            id,
            titulo = rd.GetString(1),
            data = rd.GetDateTime(2).ToString("yyyy-MM-dd"),
            observacao = rd.GetString(3),
            enviadoPor = rd.GetString(4),
            arquivos = anexos.TryGetValue(id, out var a) ? a : new List<object>()
        });
    }
    return Results.Ok(lista);
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPost("/api/exames", async (HttpRequest request, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    var form = await request.ReadFormAsync();
    if (!int.TryParse(form["pacienteId"], out var pacienteId))
        return Results.BadRequest(new { mensagem = "Paciente invÃ¡lido." });

    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    string titulo = form["titulo"].ToString().Trim();
    if (titulo == "")
        return Results.BadRequest(new { mensagem = "Informe o nome do exame." });

    var erro = ArquivoHelper.Validar(form.Files, somenteImagem: false);
    if (erro != null) return Results.BadRequest(new { mensagem = erro });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO exames_paciente (paciente_id, empresa_id, titulo_exame, data_exame, observacao)
        VALUES (@p, @emp, @titulo, @data, @obs) RETURNING id;", conn, tx);
    cmd.Parameters.AddWithValue("p", pacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("titulo", titulo.Length > 100 ? titulo[..100] : titulo);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date) { Value = EvolucaoCampos.LerData(form["data"]) });
    cmd.Parameters.AddWithValue("obs", form["observacao"].ToString());
    int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

    await ArquivoHelper.Salvar(conn, tx, pacienteId, "exame", id, form.Files);
    await tx.CommitAsync();

    return Results.Created($"/api/exames/{id}", new { id, mensagem = "Exame registrado!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPut("/api/exames/{id}", async (int id, HttpRequest request, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    var form = await request.ReadFormAsync();
    string titulo = form["titulo"].ToString().Trim();
    if (titulo == "")
        return Results.BadRequest(new { mensagem = "Informe o nome do exame." });
    var erro = ArquivoHelper.Validar(form.Files, somenteImagem: false);
    if (erro != null) return Results.BadRequest(new { mensagem = erro });
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    using var cmd = new NpgsqlCommand(@"
    UPDATE exames_paciente
    SET titulo_exame = @titulo, data_exame = @data, observacao = @obs
    WHERE id = @id AND empresa_id = @emp
    RETURNING paciente_id;", conn, tx);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("titulo", titulo.Length > 100 ? titulo[..100] : titulo);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date) { Value = EvolucaoCampos.LerData(form["data"]) });
    cmd.Parameters.AddWithValue("obs", form["observacao"].ToString());
    var pid = await cmd.ExecuteScalarAsync();
    if (pid is null)
        return Results.NotFound(new { mensagem = "Exame nÃ£o encontrado." });
    await ArquivoHelper.Salvar(conn, tx, Convert.ToInt32(pid), "exame", id, form.Files);
    await tx.CommitAsync();
    return Results.Ok(new { mensagem = "Exame atualizado!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapDelete("/api/exames/{id}", async (int id, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    using (var cmdA = new NpgsqlCommand(
    "DELETE FROM arquivos_paciente WHERE origem = 'exame' AND origem_id = @id", conn, tx))
    {
    cmdA.Parameters.AddWithValue("id", id);
    await cmdA.ExecuteNonQueryAsync();
    }
    using var cmd = new NpgsqlCommand("DELETE FROM exames_paciente WHERE id = @id AND empresa_id = @emp", conn, tx);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    var rows = await cmd.ExecuteNonQueryAsync();
    await tx.CommitAsync();
    return rows > 0
    ? Results.Ok(new { mensagem = "Exame excluÃ­do!" })
    : Results.NotFound(new { mensagem = "Exame nÃ£o encontrado." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));
// ======================================= OBSERVAÃ‡Ã•ES (semana do check-in e gerais)============================================
app.MapGet("/api/observacoes/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
    SELECT id, tipo, data_ref, texto
    FROM observacoes_paciente
    WHERE paciente_id = @p AND empresa_id = @emp
    ORDER BY data_ref DESC, id DESC;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        lista.Add(new
        {
            id = rd.GetInt32(0),
            tipo = rd.GetString(1),
            data = rd.GetDateTime(2).ToString("yyyy-MM-dd"),
            texto = rd.GetString(3)
        });
    }
    return Results.Ok(lista);
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPost("/api/observacoes", async (ObservacaoInput input, ClaimsPrincipal user) =>
{
    if (string.IsNullOrWhiteSpace(input.Texto))
        return Results.BadRequest(new { mensagem = "Escreva a observaÃ§Ã£o." });

    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, input.PacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    var tipo = input.Tipo == "geral" ? "geral" : "semana";
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
    INSERT INTO observacoes_paciente (paciente_id, empresa_id, tipo, data_ref, texto)
    VALUES (@p, @emp, @tipo, @data, @texto) RETURNING id;", conn);
    cmd.Parameters.AddWithValue("p", input.PacienteId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("tipo", tipo);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date)
    {
        Value = DateTime.TryParse(input.Data, out var dt) ? dt.Date : DateTime.Today
    });
    cmd.Parameters.AddWithValue("texto", input.Texto.Trim());
    var id = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/observacoes/{id}", new { id, mensagem = "ObservaÃ§Ã£o salva!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapPut("/api/observacoes/{id}", async (int id, ObservacaoInput input, ClaimsPrincipal user) =>
{
    if (string.IsNullOrWhiteSpace(input.Texto))
        return Results.BadRequest(new { mensagem = "Escreva a observaÃ§Ã£o." });

    int emp = Auth.Empresa(user);
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
    UPDATE observacoes_paciente SET data_ref = @data, texto = @texto WHERE id = @id AND empresa_id = @emp;", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date)
    {
        Value = DateTime.TryParse(input.Data, out var dt) ? dt.Date : DateTime.Today
    });
    cmd.Parameters.AddWithValue("texto", input.Texto.Trim());
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "ObservaÃ§Ã£o atualizada!" })
        : Results.NotFound(new { mensagem = "ObservaÃ§Ã£o nÃ£o encontrada." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

app.MapDelete("/api/observacoes/{id}", async (int id, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM observacoes_paciente WHERE id = @id AND empresa_id = @emp", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "ObservaÃ§Ã£o excluÃ­da!" })
        : Results.NotFound(new { mensagem = "ObservaÃ§Ã£o nÃ£o encontrada." });
}).AddEndpointFilter(new TenantIsolationFilter(connString));
// Duplica uma opÃ§Ã£o (itens + substituiÃ§Ãµes) para uma nova opÃ§Ã£o da mesma refeiÃ§Ã£o
app.MapPost("/api/planos/refeicao/{refeicaoId}/opcao/{opcao}/duplicar", async (int refeicaoId, int opcao, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Valida que a refeiÃ§Ã£o pertence Ã  empresa do token
    using (var cmdCheck = new NpgsqlCommand(
        "SELECT 1 FROM refeicoes r JOIN planos_alimentares p ON p.id = r.plano_id WHERE r.id = @r AND p.empresa_id = @emp", conn))
    {
        cmdCheck.Parameters.AddWithValue("r", refeicaoId);
        cmdCheck.Parameters.AddWithValue("emp", emp);
        if (await cmdCheck.ExecuteScalarAsync() == null)
            return Results.NotFound(new { mensagem = "RefeiÃ§Ã£o nÃ£o encontrada." });
    }

    using var tx = await conn.BeginTransactionAsync();
    int novaOpcao;
    using (var cmdMax = new NpgsqlCommand(
    "SELECT COALESCE(MAX(opcao), 0) + 1 FROM itens_refeicao WHERE refeicao_id = @r", conn, tx))
    {
    cmdMax.Parameters.AddWithValue("r", refeicaoId);
    novaOpcao = Convert.ToInt32(await cmdMax.ExecuteScalarAsync());
    }
    var origem = new List<(int id, int? pai, string nome, double qtd, string medida, double kcal, double prot, double carbo, double gord)>();
    using (var cmdSel = new NpgsqlCommand(@"
    SELECT id, substitui_item_id, COALESCE(alimento_nome_livre, 'Alimento'), COALESCE(quantidade_g, 0),
    COALESCE(medida_caseira, ''), COALESCE(kcal_manual, 0), COALESCE(proteinas_manual, 0),
    COALESCE(carbos_manual, 0), COALESCE(gorduras_manual, 0)
    FROM itens_refeicao
    WHERE refeicao_id = @r AND opcao = @o
    ORDER BY id;", conn, tx))
    {
    cmdSel.Parameters.AddWithValue("r", refeicaoId);
    cmdSel.Parameters.AddWithValue("o", opcao);
    using var rd = await cmdSel.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    origem.Add((rd.GetInt32(0), rd.IsDBNull(1) ? (int?)null : rd.GetInt32(1), rd.GetString(2),
    Convert.ToDouble(rd.GetValue(3)), rd.GetString(4), Convert.ToDouble(rd.GetValue(5)),
    Convert.ToDouble(rd.GetValue(6)), Convert.ToDouble(rd.GetValue(7)), Convert.ToDouble(rd.GetValue(8))));
    }
    if (origem.Count == 0)
    return Results.NotFound(new { mensagem = "OpÃ§Ã£o nÃ£o encontrada." });
    var mapa = new Dictionary<int, int>();
// primeiro os itens principais, depois as substituiÃ§Ãµes (precisam do novo id do pai)
    foreach (var it in origem.OrderBy(x => x.pai.HasValue ? 1 : 0).ThenBy(x => x.id))
    {
    using var cmd = new NpgsqlCommand(@"
    INSERT INTO itens_refeicao
    (refeicao_id, empresa_id, opcao, substitui_item_id, alimento_nome_livre, quantidade_g, medida_caseira,
    kcal_manual, proteinas_manual, carbos_manual, gorduras_manual, e_substituicao)
    VALUES (@r, @emp, @o, @sub, @nome, @qtd, @medida, @kcal, @prot, @carbo, @gord, @esub)
    RETURNING id;", conn, tx);
    cmd.Parameters.AddWithValue("r", refeicaoId);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("o", novaOpcao);
    cmd.Parameters.Add(new NpgsqlParameter("sub", NpgsqlTypes.NpgsqlDbType.Integer)
    {
    Value = it.pai.HasValue && mapa.ContainsKey(it.pai.Value) ? mapa[it.pai.Value] : DBNull.Value
    });
    cmd.Parameters.AddWithValue("nome", it.nome);
    cmd.Parameters.AddWithValue("qtd", it.qtd);
    cmd.Parameters.AddWithValue("medida", it.medida);
    cmd.Parameters.AddWithValue("kcal", it.kcal);
    cmd.Parameters.AddWithValue("prot", it.prot);
    cmd.Parameters.AddWithValue("carbo", it.carbo);
    cmd.Parameters.AddWithValue("gord", it.gord);
    cmd.Parameters.AddWithValue("esub", it.pai.HasValue);
    mapa[it.id] = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }
    await tx.CommitAsync();
    return Results.Ok(new { opcao = novaOpcao, mensagem = "OpÃ§Ã£o duplicada!" });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// Restaura uma versÃ£o (antes, guarda o plano atual como nova versÃ£o de seguranÃ§a)
app.MapPost("/api/planos/versao/{id}/restaurar", async (int id, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    int pacienteId, numeroOrigem;
    string snapJson;

    using (var conn0 = new NpgsqlConnection(connString))
    {
        await conn0.OpenAsync();
        using var cmd0 = new NpgsqlCommand(
            "SELECT paciente_id, numero, snapshot::text FROM planos_versoes WHERE id = @id AND empresa_id = @emp", conn0);
        cmd0.Parameters.AddWithValue("id", id);
        cmd0.Parameters.AddWithValue("emp", emp);
        using var rd0 = await cmd0.ExecuteReaderAsync();
        if (!await rd0.ReadAsync())
            return Results.NotFound(new { mensagem = "VersÃ£o nÃ£o encontrada." });
        pacienteId = rd0.GetInt32(0);
        numeroOrigem = rd0.GetInt32(1);
        snapJson = rd0.GetString(2);
    }

    var atual = await PlanoHelper.Montar(connString, pacienteId, Auth.Empresa(user));
    var refeicoesAtuais = (List<Dictionary<string, object?>>)atual["refeicoes"]!;

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    string orientacoesAtuais = "";
    using (var cmdO = new NpgsqlCommand("SELECT texto FROM orientacoes_nutricionais WHERE paciente_id = @p", conn, tx))
    {
        cmdO.Parameters.AddWithValue("p", pacienteId);
        orientacoesAtuais = (await cmdO.ExecuteScalarAsync())?.ToString() ?? "";
    }

    int? numeroBackup = null;
    if (refeicoesAtuais.Count > 0)
    {
        using var cmdN = new NpgsqlCommand(
            "SELECT COALESCE(MAX(numero), 0) + 1 FROM planos_versoes WHERE paciente_id = @p", conn, tx);
        cmdN.Parameters.AddWithValue("p", pacienteId);
        numeroBackup = Convert.ToInt32(await cmdN.ExecuteScalarAsync());

        using var cmdB = new NpgsqlCommand(@"
            INSERT INTO planos_versoes (paciente_id, empresa_id, numero, descricao, snapshot)
            VALUES (@p, (SELECT empresa_id FROM pacientes WHERE id = @p), @n, @d, @s);", conn, tx);
        cmdB.Parameters.AddWithValue("p", pacienteId);
        cmdB.Parameters.AddWithValue("n", numeroBackup.Value);
        cmdB.Parameters.AddWithValue("d", $"Backup automÃ¡tico antes de restaurar a versÃ£o {numeroOrigem}");
        cmdB.Parameters.Add(new NpgsqlParameter("s", NpgsqlTypes.NpgsqlDbType.Jsonb)
        {
            Value = JsonSerializer.Serialize(new { plano = atual, orientacoes = orientacoesAtuais })
        });
        await cmdB.ExecuteNonQueryAsync();
    }

    int planoId;
    using (var cmdP = new NpgsqlCommand(@"
        INSERT INTO planos_alimentares (paciente_id, empresa_id, nome_plano, meta_calorica_kcal)
        VALUES (@p, (SELECT empresa_id FROM pacientes WHERE id = @p), 'Plano Personalizado', 2000)
        ON CONFLICT (paciente_id) DO UPDATE SET paciente_id = EXCLUDED.paciente_id
        RETURNING id;", conn, tx))
    {
        cmdP.Parameters.AddWithValue("p", pacienteId);
        planoId = Convert.ToInt32(await cmdP.ExecuteScalarAsync());
    }
    using (var cmdDel = new NpgsqlCommand("DELETE FROM refeicoes WHERE plano_id = @pl", conn, tx))
    {
        cmdDel.Parameters.AddWithValue("pl", planoId);
        await cmdDel.ExecuteNonQueryAsync();
    }

    using var doc = JsonDocument.Parse(snapJson);
    var raiz = doc.RootElement;
    var planoSnap = raiz.GetProperty("plano");

    foreach (var r in planoSnap.GetProperty("refeicoes").EnumerateArray())
    {
        int novaRefeicaoId;
        using (var cmdR = new NpgsqlCommand(@"
            INSERT INTO refeicoes (plano_id, empresa_id, nome_refeicao, horario, observacoes)
            VALUES (@pl, (SELECT empresa_id FROM planos_alimentares WHERE id = @pl), @nome, @h::time, @obs) RETURNING id;", conn, tx))
        {
            cmdR.Parameters.AddWithValue("pl", planoId);
            cmdR.Parameters.AddWithValue("nome", r.GetProperty("nome").GetString() ?? "RefeiÃ§Ã£o");
            cmdR.Parameters.AddWithValue("h", r.GetProperty("horario").GetString() ?? "08:00");
            cmdR.Parameters.AddWithValue("obs", r.GetProperty("observacoes").GetString() ?? "");
            novaRefeicaoId = Convert.ToInt32(await cmdR.ExecuteScalarAsync());
        }

        var itens = r.GetProperty("itens").EnumerateArray().ToList();
        var mapa = new Dictionary<int, int>();

        foreach (var it in itens.OrderBy(x => x.GetProperty("substituiItemId").ValueKind == JsonValueKind.Null ? 0 : 1))
        {
            int? pai = it.GetProperty("substituiItemId").ValueKind == JsonValueKind.Null
                ? null : it.GetProperty("substituiItemId").GetInt32();

            using var cmdI = new NpgsqlCommand(@"
                INSERT INTO itens_refeicao
                    (refeicao_id, empresa_id, opcao, substitui_item_id, alimento_nome_livre, quantidade_g, medida_caseira,
                     kcal_manual, proteinas_manual, carbos_manual, gorduras_manual, e_substituicao)
                VALUES (@r, (SELECT r.empresa_id FROM refeicoes r JOIN planos_alimentares p ON p.id = r.plano_id WHERE r.id = @r), @o, @sub, @nome, @qtd, @medida, @kcal, @prot, @carbo, @gord, @esub)
                RETURNING id;", conn, tx);
            cmdI.Parameters.AddWithValue("r", novaRefeicaoId);
            cmdI.Parameters.AddWithValue("o", it.GetProperty("opcao").GetInt32());
            cmdI.Parameters.Add(new NpgsqlParameter("sub", NpgsqlTypes.NpgsqlDbType.Integer)
            {
                Value = pai.HasValue && mapa.ContainsKey(pai.Value) ? mapa[pai.Value] : DBNull.Value
            });
            cmdI.Parameters.AddWithValue("nome", it.GetProperty("alimento").GetString() ?? "Alimento");
            cmdI.Parameters.AddWithValue("qtd", it.GetProperty("quantidadeG").GetDouble());
            cmdI.Parameters.AddWithValue("medida", it.GetProperty("medidaCaseira").GetString() ?? "");
            cmdI.Parameters.AddWithValue("kcal", it.GetProperty("kcal").GetDouble());
            cmdI.Parameters.AddWithValue("prot", it.GetProperty("proteinas").GetDouble());
            cmdI.Parameters.AddWithValue("carbo", it.GetProperty("carbos").GetDouble());
            cmdI.Parameters.AddWithValue("gord", it.GetProperty("gorduras").GetDouble());
            cmdI.Parameters.AddWithValue("esub", pai.HasValue);
            mapa[it.GetProperty("id").GetInt32()] = Convert.ToInt32(await cmdI.ExecuteScalarAsync());
        }
    }

    string orientacoesVersao = raiz.TryGetProperty("orientacoes", out var o) ? o.GetString() ?? "" : "";
    using (var cmdOr = new NpgsqlCommand(@"
        INSERT INTO orientacoes_nutricionais (paciente_id, empresa_id, texto, atualizado_em)
        VALUES (@p, (SELECT empresa_id FROM pacientes WHERE id = @p), @t, CURRENT_TIMESTAMP)
        ON CONFLICT (paciente_id) DO UPDATE SET texto = EXCLUDED.texto, atualizado_em = CURRENT_TIMESTAMP;", conn, tx))
    {
        cmdOr.Parameters.AddWithValue("p", pacienteId);
        cmdOr.Parameters.AddWithValue("t", orientacoesVersao);
        await cmdOr.ExecuteNonQueryAsync();
    }

    await tx.CommitAsync();

    var msg = numeroBackup.HasValue
        ? $"VersÃ£o {numeroOrigem} restaurada! O plano anterior foi guardado como versÃ£o {numeroBackup}."
        : $"VersÃ£o {numeroOrigem} restaurada!";
    return Results.Ok(new { mensagem = msg });
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// =====================================================================
// AUTENTICAÃ‡ÃƒO E USUÃRIOS
// =====================================================================
app.MapPost("/api/auth/login", async (LoginInput input, ILogger<Program> logger, HttpContext http) =>
{
    var email = (input.Email ?? "").Trim().ToLowerInvariant();
    var senha = input.Senha ?? "";
    if (email == "" || string.IsNullOrEmpty(senha))
        return Results.BadRequest(new { mensagem = "Informe e-mail e senha." });

    // Em TestServer RemoteIpAddress pode ser null; default para ::1 (loopback IPv6) para testes funcionarem
    var ip = http.Connection.RemoteIpAddress?.ToString() ?? "::1";

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // 1. Verificar tentativas falhas por email+IP (janela 15 min, max 5)
    var (bloqueado, retryAfterSeg) = await VerificarTentativasFalhas(conn, "equipe", email, ip, logger);
    if (bloqueado)
    {
        logger.LogWarning("Login bloqueado por tentativas falhas: email={Email} ip={Ip}", email, ip);
        return RateLimitHelpers.JsonWithHeaders(new { mensagem = $"Muitas tentativas. Tente novamente em {Math.Ceiling(retryAfterSeg / 60.0)} minutos." }, 429, new Dictionary<string, string>
        {
            ["Retry-After"] = retryAfterSeg.ToString(),
            ["X-RateLimit-Limit"] = "5",
            ["X-RateLimit-Remaining"] = "0",
            ["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeg).ToUnixTimeSeconds().ToString()
        });
    }

    // 2. Buscar candidatos
    var candidatos = new List<(int id, int empresaId, string nome, string hash, string perfil)>();
    using (var cmd = new NpgsqlCommand(@"
        SELECT id, empresa_id, nome, senha_hash, perfil
        FROM usuarios
        WHERE lower(email) = @e AND perfil <> 'cliente' AND ativo = true
        ORDER BY id DESC;", conn))
    {
        cmd.Parameters.AddWithValue("e", email);
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            candidatos.Add((
                rd.GetInt32(0),
                rd.GetInt32(1),
                rd.GetString(2),
                rd.GetString(3),
                rd.GetString(4)
            ));
        }
    }

    if (candidatos.Count == 0)
    {
        // Registrar tentativa falha (anti-enumeração: mesmo tratamento para usuário inexistente)
        await RegistrarTentativaFalha(conn, "equipe", email, ip, logger);
        logger.LogWarning("Login falhou: nenhum usuÃ¡rio ativo com e-mail={Email}", email);
        return Results.Json(new { mensagem = "E-mail ou senha incorretos." }, statusCode: 401);
    }

    // 3. Verificar senha
    int id = 0, empresaId = 0; string nome = "", perfil = "";
    bool loginOk = false;

    foreach (var c in candidatos)
    {
        if (BCrypt.Net.BCrypt.Verify(senha, c.hash))
        {
            id = c.id;
            empresaId = c.empresaId;
            nome = c.nome;
            perfil = c.perfil;
            loginOk = true;
            break;
        }
    }

    if (!loginOk)
    {
        await RegistrarTentativaFalha(conn, "equipe", email, ip, logger);
        logger.LogWarning("Login falhou: senha incorreta para e-mail={Email} (candidatos={Count})", email, candidatos.Count);
        return Results.Json(new { mensagem = "E-mail ou senha incorretos." }, statusCode: 401);
    }

    // 4. Sucesso: zerar tentativas falhas para este email+IP
    await ZerarTentativasFalhas(conn, "equipe", email, ip, logger);

    // 5. MÃ³dulos ligados na empresa + nÃ­vel do usuÃ¡rio
    var modulos = new Dictionary<string, string>();
    using (var cm = new NpgsqlCommand(@"
        SELECT em.modulo, COALESCE(um.nivel, 'nenhum')
        FROM empresa_modulos em
        LEFT JOIN usuario_modulos um ON um.modulo = em.modulo AND um.usuario_id = @u
        WHERE em.empresa_id = @emp;", conn))
    {
        cm.Parameters.AddWithValue("u", id);
        cm.Parameters.AddWithValue("emp", empresaId);
        using var rd = await cm.ExecuteReaderAsync();
        while (await rd.ReadAsync())
            modulos[rd.GetString(0)] = (perfil == "admin" || perfil == "super_admin") ? "total" : rd.GetString(1);
    }

    var token = Auth.Gerar(jwtKey, id, empresaId, perfil, modulos);
    return Results.Ok(new { token, nome, perfil, modulos });
}).RequireRateLimiting("ip-auth");

// Pepper para hash do identificador (configurÃ¡vel via variÃ¡vel de ambiente RATE_LIMIT_PEPPER)
string ObterPepper() => builder.Configuration["RateLimit:Pepper"] ?? "default-pepper-change-in-production";

string HashIdentificador(string identificador, string pepper)
{
    using var sha = System.Security.Cryptography.SHA256.Create();
    var bytes = System.Text.Encoding.UTF8.GetBytes(identificador + pepper);
    return System.Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
}

// Helper methods para controle de tentativas falhas por identificador+IP (hash)
async Task<(bool bloqueado, int retryAfterSeg)> VerificarTentativasFalhas(NpgsqlConnection conn, string tipo, string identificador, string ip, ILogger logger)
{
    var hash = HashIdentificador(identificador, ObterPepper());

    // Validar IP - se for inválido (ex: "unknown"), pula rate limiting por banco
    if (!IPAddress.TryParse(ip, out _))
    {
        return (false, 0);
    }

    try
    {
        // Limpeza automática: remove registros com mais de 24 horas
        await LimparTentativasAntigas(conn);

        // Conta falhas nos últimos 15 minutos para este identificador+IP
        using var cmd = new NpgsqlCommand(@"
            SELECT COUNT(*), MAX(criado_em)
            FROM tentativas_login
            WHERE tipo = @tipo AND identificador_hash = @hash AND ip = @ip::inet
              AND sucesso = false AND criado_em > CURRENT_TIMESTAMP - interval '15 minutes';", conn);
        cmd.Parameters.AddWithValue("tipo", tipo);
        cmd.Parameters.AddWithValue("hash", hash);
        cmd.Parameters.AddWithValue("ip", ip);
        using var rd = await cmd.ExecuteReaderAsync();
        if (await rd.ReadAsync())
        {
            var count = rd.GetInt64(0);
            var ultimaTentativa = rd.IsDBNull(1) ? DateTime.MinValue : rd.GetDateTime(1);
            if (count >= 5)
            {
                var decorrido = (DateTime.UtcNow - ultimaTentativa).TotalSeconds;
                var restante = Math.Max(0, 900 - (int)decorrido);
                return (true, restante);
            }
        }
        return (false, 0);
    }
    catch (Exception ex)
    {
        // Graceful degradation: se a tabela nÃ£o existe ou hÃ¡ erro de banco, loga e permite o login (apenas limite por IP em memÃ³ria)
        logger.LogWarning(ex, "Erro ao verificar tentativas falhas para {Tipo} {IdentificadorHash} IP {Ip}. Rate limiting por banco desabilitado.", tipo, hash, ip);
        return (false, 0);
    }
}

async Task LimparTentativasAntigas(NpgsqlConnection conn)
{
    try
    {
        using var cmd = new NpgsqlCommand(@"
            DELETE FROM tentativas_login
            WHERE criado_em < CURRENT_TIMESTAMP - interval '24 hours';", conn);
        await cmd.ExecuteNonQueryAsync();
    }
    catch
    {
        // Ignora erros de limpeza (tabela pode nÃ£o existir ainda)
    }
}

async Task RegistrarTentativaFalha(NpgsqlConnection conn, string tipo, string identificador, string ip, ILogger logger)
{
    // Validar IP - se for invÃ¡lido (ex: "unknown"), pula registro
    if (!IPAddress.TryParse(ip, out _))
    {
        return;
    }

    var hash = HashIdentificador(identificador, ObterPepper());
    try
    {
        using var cmd = new NpgsqlCommand(@"
            INSERT INTO tentativas_login (tipo, identificador_hash, ip, sucesso, criado_em)
            VALUES (@tipo, @hash, @ip::inet, false, CURRENT_TIMESTAMP);", conn);
        cmd.Parameters.AddWithValue("tipo", tipo);
        cmd.Parameters.AddWithValue("hash", hash);
        cmd.Parameters.AddWithValue("ip", ip);
        await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        // Graceful degradation: loga erro mas nÃ£o bloqueia o fluxo
        logger.LogWarning(ex, "Erro ao registrar tentativa falha para {Tipo} {IdentificadorHash} IP {Ip}", tipo, hash, ip);
    }
}

async Task ZerarTentativasFalhas(NpgsqlConnection conn, string tipo, string identificador, string ip, ILogger logger)
{
    // Validar IP - se for invÃ¡lido (ex: "unknown"), pula limpeza
    if (!IPAddress.TryParse(ip, out _))
    {
        return;
    }

    var hash = HashIdentificador(identificador, ObterPepper());
    try
    {
        using var cmd = new NpgsqlCommand(@"
            DELETE FROM tentativas_login
            WHERE tipo = @tipo AND identificador_hash = @hash AND ip = @ip::inet AND sucesso = false;", conn);
        cmd.Parameters.AddWithValue("tipo", tipo);
        cmd.Parameters.AddWithValue("hash", hash);
        cmd.Parameters.AddWithValue("ip", ip);
        await cmd.ExecuteNonQueryAsync();
    }
    catch (Exception ex)
    {
        // Graceful degradation: loga erro mas nÃ£o bloqueia o fluxo
        logger.LogWarning(ex, "Erro ao zerar tentativas falhas para {Tipo} {IdentificadorHash} IP {Ip}", tipo, hash, ip);
    }
}

var perfisEquipe = new HashSet<string> { "super_admin", "admin", "profissional", "atendente" };
var niveisAcesso = new HashSet<string> { "nenhum", "leitura", "edicao", "total" };

// =====================================================================
// EMPRESAS E CONVITES (super_admin / admin)
// =====================================================================
// Helper para verificar se Ã© super_admin
bool IsSuperAdmin(ClaimsPrincipal u) => u.FindFirst("perfil")?.Value == "super_admin";

// POST /api/empresas - Criar nova empresa (apenas super_admin)
app.MapPost("/api/empresas", async (EmpresaInput input, ClaimsPrincipal user) =>
{
    if (!IsSuperAdmin(user))
        return Results.Forbid();

    var nome = (input.Nome ?? "").Trim();
    if (string.IsNullOrWhiteSpace(nome))
        return Results.BadRequest(new { mensagem = "Informe o nome da empresa." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO empresas (nome) VALUES (@nome) RETURNING id;", conn);
    cmd.Parameters.AddWithValue("nome", nome);
    int empresaId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

    // Ativar mÃ³dulo nutricao por padrÃ£o
    using var cm = new NpgsqlCommand(
        "INSERT INTO empresa_modulos (empresa_id, modulo) VALUES (@emp, 'nutricao') ON CONFLICT DO NOTHING;", conn);
    cm.Parameters.AddWithValue("emp", empresaId);
    await cm.ExecuteNonQueryAsync();

    return Results.Created($"/api/empresas/{empresaId}", new { id = empresaId, nome, mensagem = "Empresa criada!" });
})
.RequireAuthorization();

// GET /api/empresas - Listar empresas com status do plano e bypass_limites (apenas super_admin)
app.MapGet("/api/empresas", async (ClaimsPrincipal user) =>
{
    if (!IsSuperAdmin(user))
        return Results.Forbid();

    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT e.id, e.nome, e.criado_em,
               COALESCE(a.status, 'sem_plano') as plano_status,
               COALESCE(a.bypass_limites, false) as bypass_limites
        FROM empresas e
        LEFT JOIN (
            SELECT empresa_id, status, bypass_limites
            FROM assinaturas
            WHERE id = (
                SELECT MAX(id) FROM assinaturas a2 WHERE a2.empresa_id = assinaturas.empresa_id
            )
        ) a ON a.empresa_id = e.id
        ORDER BY e.nome;", conn);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
        lista.Add(new
        {
            id = rd.GetInt32(0),
            nome = rd.GetString(1),
            criadoEm = rd.GetDateTime(2),
            planoStatus = rd.IsDBNull(3) ? "sem_plano" : rd.GetString(3),
            bypassLimites = rd.IsDBNull(4) ? false : rd.GetBoolean(4)
        });
    return Results.Ok(lista);
})
.RequireAuthorization();

// GET /api/empresas/count - Contar empresas (pÃºblico, para verificar se precisa onboarding)
app.MapGet("/api/empresas/count", async () =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM empresas;", conn);
    var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    return Results.Ok(new { count });
});

// DELETE /api/admin/empresas/{empresaId} - Excluir empresa (apenas super_admin)
app.MapDelete("/api/admin/empresas/{empresaId:int}", async (int empresaId, ClaimsPrincipal user) =>
{
    if (!IsSuperAdmin(user))
        return Results.Forbid();

    // NÃ£o permitir excluir a prÃ³pria empresa do super_admin logado
    int currentEmpresaId = Auth.Empresa(user);
    if (empresaId == currentEmpresaId)
        return Results.BadRequest(new { mensagem = "NÃ£o Ã© possÃ­vel excluir a prÃ³pria empresa." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Verificar se a empresa existe
    using var checkCmd = new NpgsqlCommand("SELECT id, nome FROM empresas WHERE id = @id", conn);
    checkCmd.Parameters.AddWithValue("id", empresaId);
    using var checkRd = await checkCmd.ExecuteReaderAsync();
    if (!await checkRd.ReadAsync())
        return Results.NotFound(new { mensagem = "Empresa nÃ£o encontrada." });

    string empresaNome = checkRd.GetString(1);
    await checkRd.CloseAsync();

    // ExclusÃ£o fÃ­sica (cascata via FK ON DELETE CASCADE)
    // Ordem: tabelas filhas antes das pais (assinaturas, pacientes, etc. jÃ¡ tÃªm FK com cascade)
    using var tx = await conn.BeginTransactionAsync();
    try
    {
        // Excluir assinaturas (tem FK para empresas com ON DELETE CASCADE, mas vamos ser explÃ­citos)
        using (var cmd = new NpgsqlCommand("DELETE FROM assinaturas WHERE empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("emp", empresaId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Excluir webhook events relacionados
        using (var cmd = new NpgsqlCommand("DELETE FROM asaas_webhook_events WHERE asaas_event_id IN (SELECT asaas_event_id FROM asaas_webhook_events WHERE payload_jsonb->>'customer' IN (SELECT asaas_customer_id FROM assinaturas WHERE empresa_id = @emp))", conn, tx))
        {
            cmd.Parameters.AddWithValue("emp", empresaId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Excluir a empresa (cascata remove pacientes, anamneses, checkins, etc. via FK)
        using (var cmd = new NpgsqlCommand("DELETE FROM empresas WHERE id = @id", conn, tx))
        {
            cmd.Parameters.AddWithValue("id", empresaId);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.Problem($"Erro ao excluir empresa: {ex.Message}");
    }

    return Results.Ok(new { mensagem = $"Empresa '{empresaNome}' excluÃ­da com sucesso." });
})
.RequireAuthorization();

// GET /api/convites/info/{token} - Info do convite (pÃºblico, para tela de aceite)
app.MapGet("/api/convites/info/{token}", async (string token) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT c.email, c.perfil, c.nivel_nutricao, e.nome as empresa_nome
        FROM convites c
        JOIN empresas e ON e.id = c.empresa_id
        WHERE c.token = @t AND c.expira_em > CURRENT_TIMESTAMP AND c.usado_em IS NULL;", conn);
    cmd.Parameters.AddWithValue("t", token);
    using var rd = await cmd.ExecuteReaderAsync();
    if (await rd.ReadAsync())
    {
        return Results.Ok(new
        {
            email = rd.GetString(0),
            perfil = rd.GetString(1),
            nivelNutricao = rd.GetString(2),
            empresa = rd.GetString(3)
        });
    }
    return Results.NotFound(new { mensagem = "Convite nÃ£o encontrado ou expirado." });
});

// ============================================================
// CONVITES PACIENTE (Nutricionista â†’ Paciente)
// ============================================================

// Helper para obter URL do frontend
string GetFrontendUrl() => app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailConfig>>().Value.FrontendBaseUrl;

// POST /api/convites-paciente - Nutricionista cria convite para paciente
app.MapPost("/api/convites-paciente", async (ConvitePacienteInput input, ClaimsPrincipal user, IEmailService emailService) =>
{
    var empresaId = Auth.Empresa(user);
    var nutricionistaId = Auth.UsuarioId(user);
    var nutricionistaNome = Auth.Nome(user);

    var emailPaciente = (input.EmailPaciente ?? "").Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(emailPaciente) || !emailPaciente.Contains("@"))
        return Results.BadRequest(new { mensagem = "E-mail do paciente invÃ¡lido." });

    // Gerar token Ãºnico (UUID v4 sem hÃ­fens = 32 chars)
    var token = Guid.NewGuid().ToString("N");
    var expiraEm = DateTime.UtcNow.AddDays(7);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Verificar se jÃ¡ existe convite ativo para este email por este nutricionista
    using var check = new NpgsqlCommand(@"
        SELECT 1 FROM convites_paciente
        WHERE nutricionista_id = @nutri AND email_paciente = @email AND status = 'pendente' AND expira_em > CURRENT_TIMESTAMP", conn);
    check.Parameters.AddWithValue("nutri", nutricionistaId);
    check.Parameters.AddWithValue("email", emailPaciente);
    if (await check.ExecuteScalarAsync() != null)
        return Results.BadRequest(new { mensagem = "JÃ¡ existe um convite pendente para este e-mail." });

    // Verificar se paciente jÃ¡ estÃ¡ vinculado a este nutricionista
    using var checkVinculo = new NpgsqlCommand(@"
        SELECT 1 FROM acessos_paciente_nutri apn
        JOIN pacientes p ON p.id = apn.paciente_id
        WHERE apn.nutricionista_id = @nutri AND p.email = @email AND apn.empresa_id = @emp", conn);
    checkVinculo.Parameters.AddWithValue("nutri", nutricionistaId);
    checkVinculo.Parameters.AddWithValue("email", emailPaciente);
    checkVinculo.Parameters.AddWithValue("emp", empresaId);
    if (await checkVinculo.ExecuteScalarAsync() != null)
        return Results.BadRequest(new { mensagem = "Este paciente jÃ¡ estÃ¡ vinculado a vocÃª." });

    // Inserir convite
    int conviteId;
    using (var cmd = new NpgsqlCommand(@"
        INSERT INTO convites_paciente (empresa_id, nutricionista_id, email_paciente, token, expira_em, status)
        VALUES (@emp, @nutri, @email, @token, @exp, 'pendente') RETURNING id;", conn))
    {
        cmd.Parameters.AddWithValue("emp", empresaId);
        cmd.Parameters.AddWithValue("nutri", nutricionistaId);
        cmd.Parameters.AddWithValue("email", emailPaciente);
        cmd.Parameters.AddWithValue("token", token);
        cmd.Parameters.AddWithValue("exp", expiraEm);
        conviteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    // Enviar e-mail
    var linkAceite = $"{GetFrontendUrl()}/convite-paciente/{token}";
    var assunto = $"Convite para acompanhamento nutricional - {nutricionistaNome}";
    var htmlBody = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
            <h2 style='color:#6c34eb;'>Convite para acompanhamento nutricional</h2>
            <p>OlÃ¡,</p>
            <p><strong>{nutricionistaNome}</strong> te convidou para ser seu paciente no <strong>ACNUTRI</strong>.</p>
            <p>Clique no botÃ£o abaixo para aceitar o convite (vÃ¡lido por 7 dias):</p>
            <p style='text-align:center;margin:30px 0;'>
                <a href='{linkAceite}' style='background:#6c34eb;color:white;padding:14px 28px;text-decoration:none;border-radius:8px;font-weight:bold;display:inline-block;'>
                    Aceitar Convite
                </a>
            </p>
            <p>Ou copie este link no navegador:<br><small>{linkAceite}</small></p>
            <hr style='border:none;border-top:1px solid #eee;margin:20px 0;'>
            <p style='color:#888;font-size:12px;'>Se nÃ£o foi vocÃª, ignore este e-mail.<br>â€” Equipe ACNUTRI</p>
        </div>";

    var emailEnviado = await emailService.EnviarEmailAsync(emailPaciente, assunto, htmlBody);

    return Results.Ok(new
    {
        id = conviteId,
        token,
        emailPaciente,
        expiraEm,
        emailEnviado,
        mensagem = emailEnviado
            ? "Convite criado e e-mail enviado com sucesso!"
            : "Convite criado, mas houve falha no envio do e-mail."
    });
})
.RequireAuthorization();

// GET /api/convites-paciente/{token} - Valida token (pÃ¡gina de aceite pÃºblica)
app.MapGet("/api/convites-paciente/{token}", async (string token) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT cp.id, cp.email_paciente, cp.nutricionista_id, cp.expira_em, cp.status,
               u.nome as nutricionista_nome, e.nome as empresa_nome
        FROM convites_paciente cp
        JOIN usuarios u ON u.id = cp.nutricionista_id
        JOIN empresas e ON e.id = cp.empresa_id
        WHERE cp.token = @t;", conn);
    cmd.Parameters.AddWithValue("t", token);
    using var rd = await cmd.ExecuteReaderAsync();
    if (await rd.ReadAsync())
    {
        var status = rd.GetString(4);
        var expiraEm = rd.GetDateTime(3);
        var expirado = expiraEm < DateTime.UtcNow;

        return Results.Ok(new
        {
            id = rd.GetInt32(0),
            emailPaciente = rd.GetString(1),
            nutricionistaId = rd.GetInt32(2),
            expiraEm = expiraEm.ToString("yyyy-MM-ddTHH:mm:ss"),
            status = status,
            expirado = expirado,
            nutricionistaNome = rd.GetString(5),
            empresaNome = rd.GetString(6),
            valido = status == "pendente" && !expirado
        });
    }
    return Results.NotFound(new { mensagem = "Convite nÃ£o encontrado." });
})
.AllowAnonymous();

// POST /api/convites-paciente/{token}/aceitar - Paciente JÃ CADASTRADO aceita
app.MapPost("/api/convites-paciente/{token}/aceitar", async (string token, AceitarConvitePacienteInput input, ClaimsPrincipal user) =>
{
    var pacienteId = Auth.UsuarioId(user); // assumindo que paciente logado tem claim de usuario_id = paciente_id
    var empresaId = Auth.Empresa(user);

    // Para paciente, o ID pode vir de forma diferente. Vamos buscar pelo email do token.
    // Se paciente jÃ¡ logado, usar o ID do claim.

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    try
    {
        // Buscar convite vÃ¡lido
        int conviteId, nutricionistaId;
        string emailConvite;
        using (var cmd = new NpgsqlCommand(@"
            SELECT id, nutricionista_id, email_paciente
            FROM convites_paciente
            WHERE token = @t AND status = 'pendente' AND expira_em > CURRENT_TIMESTAMP", conn, tx))
        {
            cmd.Parameters.AddWithValue("t", token);
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                return Results.BadRequest(new { mensagem = "Convite invÃ¡lido ou expirado." });

            conviteId = rd.GetInt32(0);
            nutricionistaId = rd.GetInt32(1);
            emailConvite = rd.GetString(2);
        }

        // Se paciente logado, verificar se email bate
        // Se nÃ£o logado, este endpoint nÃ£o deve ser chamado (usar aceitar-com-cadastro)

        // Buscar ou criar paciente
        int pacienteIdFinal;
        using (var cmd = new NpgsqlCommand("SELECT id FROM pacientes WHERE email = @email AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("email", emailConvite);
            cmd.Parameters.AddWithValue("emp", empresaId);
            var result = await cmd.ExecuteScalarAsync();
            if (result != null)
            {
                pacienteIdFinal = Convert.ToInt32(result);
            }
            else
            {
                return Results.BadRequest(new { mensagem = "Paciente nÃ£o encontrado. Use o endpoint de cadastro." });
            }
        }

        // Criar vÃ­nculo
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO acessos_paciente_nutri (empresa_id, nutricionista_id, paciente_id)
            VALUES (@emp, @nutri, @pac)
            ON CONFLICT (nutricionista_id, paciente_id) DO NOTHING;", conn, tx))
        {
            cmd.Parameters.AddWithValue("emp", empresaId);
            cmd.Parameters.AddWithValue("nutri", nutricionistaId);
            cmd.Parameters.AddWithValue("pac", pacienteIdFinal);
            await cmd.ExecuteNonQueryAsync();
        }

        // Atualizar convite
        using (var cmd = new NpgsqlCommand(@"
            UPDATE convites_paciente SET status = 'aceito', aceito_em = CURRENT_TIMESTAMP, paciente_id = @pac
            WHERE id = @id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("pac", pacienteIdFinal);
            cmd.Parameters.AddWithValue("id", conviteId);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return Results.Ok(new { mensagem = "Convite aceito com sucesso! VÃ­nculo criado." });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.BadRequest(new { mensagem = "Erro ao aceitar convite: " + ex.Message });
    }
})
.RequireAuthorization();

// POST /api/convites-paciente/{token}/aceitar-com-cadastro - Paciente NOVO cadastra + aceita
app.MapPost("/api/convites-paciente/{token}/aceitar-com-cadastro", async (string token, AceitarConvitePacienteInput input) =>
{
    var nome = (input.Nome ?? "").Trim();
    var senha = input.Senha ?? "";
    var cpf = (input.Cpf ?? "").Trim().Replace(".", "").Replace("-", "");
    var dataNascimento = input.DataNascimento;
    var telefone = (input.Telefone ?? "").Trim();

    if (string.IsNullOrWhiteSpace(nome) || senha.Length < 8 || cpf.Length != 11 || dataNascimento == null)
        return Results.BadRequest(new { mensagem = "Todos os campos sÃ£o obrigatÃ³rios. Senha mÃ­nima 8 caracteres. CPF com 11 dÃ­gitos." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    try
    {
        // Buscar convite vÃ¡lido
        int conviteId, nutricionistaId, empresaId;
        string emailConvite;
        using (var cmd = new NpgsqlCommand(@"
            SELECT id, nutricionista_id, email_paciente, empresa_id
            FROM convites_paciente
            WHERE token = @t AND status = 'pendente' AND expira_em > CURRENT_TIMESTAMP", conn, tx))
        {
            cmd.Parameters.AddWithValue("t", token);
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                return Results.BadRequest(new { mensagem = "Convite invÃ¡lido ou expirado." });

            conviteId = rd.GetInt32(0);
            nutricionistaId = rd.GetInt32(1);
            emailConvite = rd.GetString(2);
            empresaId = rd.GetInt32(3);
        }

        // Verificar se email do cadastro bate com o do convite
        if (emailConvite != input.Nome?.Trim().ToLowerInvariant().Split('@').LastOrDefault()) // simplificaÃ§Ã£o
        {
            // Melhor: o input nÃ£o tem email, usar o do convite
        }

        // Verificar se CPF jÃ¡ existe
        using (var cmd = new NpgsqlCommand("SELECT 1 FROM pacientes WHERE cpf = @cpf AND empresa_id = @emp", conn, tx))
        {
            cmd.Parameters.AddWithValue("cpf", cpf);
            cmd.Parameters.AddWithValue("emp", empresaId);
            if (await cmd.ExecuteScalarAsync() != null)
                return Results.BadRequest(new { mensagem = "CPF jÃ¡ cadastrado." });
        }

        // Criar paciente
        int pacienteId;
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO pacientes (empresa_id, nome, email, telefone, data_nascimento, sexo, status, cpf)
            VALUES (@emp, @nome, @email, @tel, @dtnasc, 'N', 'ativo', @cpf) RETURNING id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("emp", empresaId);
            cmd.Parameters.AddWithValue("nome", nome);
            cmd.Parameters.AddWithValue("email", emailConvite);
            cmd.Parameters.AddWithValue("tel", telefone);
            cmd.Parameters.AddWithValue("dtnasc", dataNascimento.Value.Date);
            cmd.Parameters.AddWithValue("cpf", cpf);
            pacienteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Criar usuÃ¡rio (login) para o paciente
        var hash = BCrypt.Net.BCrypt.HashPassword(senha);
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO usuarios (empresa_id, nome, email, senha_hash, perfil)
            VALUES (@emp, @nome, @email, @hash, 'cliente') RETURNING id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("emp", empresaId);
            cmd.Parameters.AddWithValue("nome", nome);
            cmd.Parameters.AddWithValue("email", emailConvite);
            cmd.Parameters.AddWithValue("hash", hash);
            await cmd.ExecuteNonQueryAsync();
        }

        // Criar vÃ­nculo nutricionista â†” paciente
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO acessos_paciente_nutri (empresa_id, nutricionista_id, paciente_id)
            VALUES (@emp, @nutri, @pac)
            ON CONFLICT (nutricionista_id, paciente_id) DO NOTHING;", conn, tx))
        {
            cmd.Parameters.AddWithValue("emp", empresaId);
            cmd.Parameters.AddWithValue("nutri", nutricionistaId);
            cmd.Parameters.AddWithValue("pac", pacienteId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Atualizar convite
        using (var cmd = new NpgsqlCommand(@"
            UPDATE convites_paciente SET status = 'aceito', aceito_em = CURRENT_TIMESTAMP, paciente_id = @pac
            WHERE id = @id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("pac", pacienteId);
            cmd.Parameters.AddWithValue("id", conviteId);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();
        return Results.Ok(new { mensagem = "Cadastro realizado e convite aceito com sucesso!", pacienteId });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.BadRequest(new { mensagem = "Erro ao processar: " + ex.Message });
    }
})
.AllowAnonymous();

// GET /api/nutricionista/convites-paciente - Lista convites do nutricionista logado
app.MapGet("/api/nutricionista/convites-paciente", async (ClaimsPrincipal user) =>
{
    var empresaId = Auth.Empresa(user);
    var nutricionistaId = Auth.UsuarioId(user);

    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT id, email_paciente, token, status, expira_em, aceito_em, criado_em, paciente_id
        FROM convites_paciente
        WHERE nutricionista_id = @nutri AND empresa_id = @emp
        ORDER BY criado_em DESC;", conn);
    cmd.Parameters.AddWithValue("nutri", nutricionistaId);
    cmd.Parameters.AddWithValue("emp", empresaId);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        lista.Add(new
        {
            id = rd.GetInt32(0),
            emailPaciente = rd.GetString(1),
            token = rd.GetString(2),
            status = rd.GetString(3),
            expiraEm = rd.GetDateTime(4).ToString("yyyy-MM-ddTHH:mm:ss"),
            aceitoEm = rd.IsDBNull(5) ? null : rd.GetDateTime(5).ToString("yyyy-MM-ddTHH:mm:ss"),
            criadoEm = rd.GetDateTime(6).ToString("yyyy-MM-ddTHH:mm:ss"),
            pacienteId = rd.IsDBNull(7) ? (int?)null : rd.GetInt32(7)
        });
    }
    return Results.Ok(lista);
})
.RequireAuthorization();

// POST /api/nutricionista/convites-paciente/{token}/reenviar - Reenviar e-mail do convite
app.MapPost("/api/nutricionista/convites-paciente/{token}/reenviar", async (string token, ClaimsPrincipal user, IEmailService emailService) =>
{
    var empresaId = Auth.Empresa(user);
    var nutricionistaId = Auth.UsuarioId(user);
    var nutricionistaNome = Auth.Nome(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Buscar convite
    string emailPaciente;
    DateTime expiraEm;
    using (var cmd = new NpgsqlCommand(@"
        SELECT email_paciente, expira_em, nutricionista_id
        FROM convites_paciente
        WHERE token = @t AND nutricionista_id = @nutri AND empresa_id = @emp AND status = 'pendente'", conn))
    {
        cmd.Parameters.AddWithValue("t", token);
        cmd.Parameters.AddWithValue("nutri", nutricionistaId);
        cmd.Parameters.AddWithValue("emp", empresaId);
        using var rd = await cmd.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
            return Results.NotFound(new { mensagem = "Convite nÃ£o encontrado ou nÃ£o pode ser reenviado." });

        emailPaciente = rd.GetString(0);
        expiraEm = rd.GetDateTime(1);
    }

    // Verificar se nÃ£o expirou
    if (expiraEm < DateTime.UtcNow)
        return Results.BadRequest(new { mensagem = "Convite expirado. NÃ£o Ã© possÃ­vel reenviar." });

    // Buscar empresa
    string empresaNome;
    using (var cmd = new NpgsqlCommand("SELECT nome FROM empresas WHERE id = @id", conn))
    {
        cmd.Parameters.AddWithValue("id", empresaId);
        empresaNome = (await cmd.ExecuteScalarAsync())?.ToString() ?? "ACNUTRI";
    }

    // Reenviar e-mail
    var linkAceite = $"{GetFrontendUrl()}/convite-paciente/{token}";
    var assunto = $"Convite para acompanhamento nutricional - {nutricionistaNome} (Reenviado)";
    var htmlBody = $@"
        <div style='font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:20px;'>
            <h2 style='color:#6c34eb;'>Convite para acompanhamento nutricional (Reenviado)</h2>
            <p>OlÃ¡,</p>
            <p><strong>{nutricionistaNome}</strong> reenviou o convite para ser seu paciente no <strong>ACNUTRI</strong>.</p>
            <p>Clique no botÃ£o abaixo para aceitar o convite (vÃ¡lido por 7 dias):</p>
            <p style='text-align:center;margin:30px 0;'>
                <a href='{linkAceite}' style='background:#6c34eb;color:white;padding:14px 28px;text-decoration:none;border-radius:8px;font-weight:bold;display:inline-block;'>
                    Aceitar Convite
                </a>
            </p>
            <p>Ou copie este link no navegador:<br><small>{linkAceite}</small></p>
            <hr style='border:none;border-top:1px solid #eee;margin:20px 0;'>
            <p style='color:#888;font-size:12px;'>Se nÃ£o foi vocÃª, ignore este e-mail.<br>â€” Equipe ACNUTRI</p>
        </div>";

    var emailEnviado = await emailService.EnviarEmailAsync(emailPaciente, assunto, htmlBody);

    return Results.Ok(new
    {
        emailEnviado,
        mensagem = emailEnviado
            ? "Convite reenviado com sucesso!"
            : "Houve falha no envio do e-mail. Tente novamente."
    });
})
.RequireAuthorization();

// POST /api/nutricionista/convites-paciente/{token}/cancelar - Cancelar convite
app.MapPost("/api/nutricionista/convites-paciente/{token}/cancelar", async (string token, ClaimsPrincipal user) =>
{
    var empresaId = Auth.Empresa(user);
    var nutricionistaId = Auth.UsuarioId(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Atualizar status para cancelado
    using var cmd = new NpgsqlCommand(@"
        UPDATE convites_paciente
        SET status = 'cancelado'
        WHERE token = @t AND nutricionista_id = @nutri AND empresa_id = @emp AND status = 'pendente'", conn);
    cmd.Parameters.AddWithValue("t", token);
    cmd.Parameters.AddWithValue("nutri", nutricionistaId);
    cmd.Parameters.AddWithValue("emp", empresaId);
    var affected = await cmd.ExecuteNonQueryAsync();

    if (affected == 0)
        return Results.NotFound(new { mensagem = "Convite nÃ£o encontrado ou nÃ£o pode ser cancelado." });

    return Results.Ok(new { mensagem = "Convite cancelado com sucesso." });
})
.RequireAuthorization();

// POST /api/onboarding - Criar primeira empresa + super-admin (sem auth, sÃ³ se 0 usuÃ¡rios e 0 empresas)
app.MapPost("/api/onboarding", async (OnboardingInput input) =>
{
    // Verificar se jÃ¡ existem usuÃ¡rios OU empresas
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using (var checkUsers = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios", conn))
    using (var checkEmpresas = new NpgsqlCommand("SELECT COUNT(*) FROM empresas", conn))
    {
        if (Convert.ToInt32(await checkUsers.ExecuteScalarAsync()) > 0
            || Convert.ToInt32(await checkEmpresas.ExecuteScalarAsync()) > 0)
            return Results.Forbid();
    }

    var empresaNome = (input.EmpresaNome ?? "").Trim();
    var adminNome = (input.AdminNome ?? "").Trim();
    var adminEmail = (input.AdminEmail ?? "").Trim().ToLowerInvariant();
    var senha = input.AdminSenha ?? "";

    if (string.IsNullOrWhiteSpace(empresaNome) || string.IsNullOrWhiteSpace(adminNome) ||
        string.IsNullOrWhiteSpace(adminEmail) || senha.Length < 8)
        return Results.BadRequest(new { mensagem = "Todos os campos sÃ£o obrigatÃ³rios. Senha mÃ­nima 8 caracteres." });

    // Criar empresa
    using var cmdEmp = new NpgsqlCommand("INSERT INTO empresas (nome) VALUES (@n) RETURNING id;", conn);
    cmdEmp.Parameters.AddWithValue("n", empresaNome);
    int empresaId = Convert.ToInt32(await cmdEmp.ExecuteScalarAsync());

    // Ativar mÃ³dulo nutriÃ§Ã£o
    using var cmdMod = new NpgsqlCommand("INSERT INTO empresa_modulos (empresa_id, modulo) VALUES (@e, 'nutricao') ON CONFLICT DO NOTHING;", conn);
    cmdMod.Parameters.AddWithValue("e", empresaId);
    await cmdMod.ExecuteNonQueryAsync();

    // Criar super-admin
    using var cmdUser = new NpgsqlCommand(@"
        INSERT INTO usuarios (empresa_id, nome, email, senha_hash, perfil)
        VALUES (@e, @nome, @email, @hash, 'super_admin') RETURNING id;", conn);
    cmdUser.Parameters.AddWithValue("e", empresaId);
    cmdUser.Parameters.AddWithValue("nome", adminNome);
    cmdUser.Parameters.AddWithValue("email", adminEmail);
    cmdUser.Parameters.AddWithValue("hash", BCrypt.Net.BCrypt.HashPassword(senha));
    int userId = Convert.ToInt32(await cmdUser.ExecuteScalarAsync());

    // MÃ³dulo para o usuÃ¡rio
    using var cmdUM = new NpgsqlCommand(
        "INSERT INTO usuario_modulos (usuario_id, modulo, nivel) VALUES (@u, 'nutricao', 'total');", conn);
    cmdUM.Parameters.AddWithValue("u", userId);
    await cmdUM.ExecuteNonQueryAsync();

    return Results.Ok(new { mensagem = "Empresa e super-admin criados com sucesso!", empresaId, userId });
});

// POST /api/empresas/{id}/convite - Convidar usuÃ¡rio para empresa (admin/super_admin)
app.MapPost("/api/empresas/{empresaId:int}/convite", async (int empresaId, ConviteInput input, ClaimsPrincipal user, IEmailService emailService) =>
{
    var perfilToken = user.FindFirst("perfil")?.Value;
    if (perfilToken != "super_admin" && perfilToken != "admin")
        return Results.Forbid();

    // Se nÃ£o for super_admin, sÃ³ pode convidar para sua prÃ³pria empresa
    if (perfilToken != "super_admin" && Auth.Empresa(user) != empresaId)
        return Results.Forbid();

    var email = (input.Email ?? "").Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(email))
        return Results.BadRequest(new { mensagem = "Informe o e-mail do convidado." });

    var perfil = perfisEquipe.Contains(input.Perfil ?? "") ? input.Perfil! : "profissional";
    var nivel = niveisAcesso.Contains(input.NivelNutricao ?? "") ? input.NivelNutricao! : "leitura";

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Verificar se empresa existe e buscar nome
    string empresaNome = "";
    using (var check = new NpgsqlCommand("SELECT nome FROM empresas WHERE id = @id", conn))
    {
        check.Parameters.AddWithValue("id", empresaId);
        var result = await check.ExecuteScalarAsync();
        if (result == null)
            return Results.NotFound(new { mensagem = "Empresa nÃ£o encontrada." });
        empresaNome = result.ToString()!;
    }

    // Verificar se jÃ¡ existe usuÃ¡rio com esse email na empresa
    using var exist = new NpgsqlCommand("SELECT 1 FROM usuarios WHERE email = @e AND empresa_id = @emp", conn);
    exist.Parameters.AddWithValue("e", email);
    exist.Parameters.AddWithValue("emp", empresaId);
    if (await exist.ExecuteScalarAsync() != null)
        return Results.Conflict(new { mensagem = "JÃ¡ existe um usuÃ¡rio com esse e-mail nesta empresa." });

    // Gerar token de convite (expira em 7 dias)
    var conviteToken = Guid.NewGuid().ToString("N");
    var expiracao = DateTime.UtcNow.AddDays(7);

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO convites (empresa_id, email, perfil, nivel_nutricao, token, expira_em)
        VALUES (@emp, @email, @perfil, @nivel, @token, @exp) RETURNING id;", conn);
    cmd.Parameters.AddWithValue("emp", empresaId);
    cmd.Parameters.AddWithValue("email", email);
    cmd.Parameters.AddWithValue("perfil", perfil);
    cmd.Parameters.AddWithValue("nivel", nivel);
    cmd.Parameters.AddWithValue("token", conviteToken);
    cmd.Parameters.AddWithValue("exp", expiracao);
    int conviteId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

    // Enviar e-mail de convite
    var emailEnviado = await emailService.EnviarConviteAsync(email, empresaNome, conviteToken, perfil, nivel);

    return Results.Created($"/api/convites/{conviteId}", new
    {
        id = conviteId,
        token = conviteToken,
        urlConvite = $"/convite/{conviteToken}",
        expiraEm = expiracao,
        mensagem = emailEnviado
            ? "Convite criado e e-mail enviado!"
            : "Convite criado! (Falha ao enviar e-mail - configure SMTP no appsettings.json)"
    });
})
.RequireAuthorization();

// POST /api/auth/aceitar-convite - Aceitar convite e definir senha
app.MapPost("/api/auth/aceitar-convite", async (AceitarConviteInput input) =>
{
    var token = (input.Token ?? "").Trim();
    var senha = input.Senha ?? "";
    if (string.IsNullOrWhiteSpace(token) || senha.Length < 8)
        return Results.BadRequest(new { mensagem = "Token e senha (mÃ­n. 8 caracteres) sÃ£o obrigatÃ³rios." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    int conviteId = 0, empresaId = 0; string email = "", perfil = "", nivel = "";
    bool achou;

    using (var cmd = new NpgsqlCommand(@"
        SELECT id, empresa_id, email, perfil, nivel_nutricao
        FROM convites WHERE token = @t AND expira_em > CURRENT_TIMESTAMP AND usado_em IS NULL;", conn))
    {
        cmd.Parameters.AddWithValue("t", token);
        using var rd = await cmd.ExecuteReaderAsync();
        achou = await rd.ReadAsync();
        if (achou)
        {
            conviteId = rd.GetInt32(0);
            empresaId = rd.GetInt32(1);
            email = rd.GetString(2);
            perfil = rd.GetString(3);
            nivel = rd.GetString(4);
        }
    }

    if (!achou)
        return Results.Json(new { mensagem = "Convite invÃ¡lido ou expirado." }, statusCode: 400);

    // Verificar se usuÃ¡rio jÃ¡ existe
    using (var check = new NpgsqlCommand("SELECT 1 FROM usuarios WHERE email = @e AND empresa_id = @emp", conn))
    {
        check.Parameters.AddWithValue("e", email);
        check.Parameters.AddWithValue("emp", empresaId);
        if (await check.ExecuteScalarAsync() != null)
            return Results.Conflict(new { mensagem = "UsuÃ¡rio jÃ¡ cadastrado." });
    }

    // Criar usuÃ¡rio
    using var tx = await conn.BeginTransactionAsync();
    try
    {
        using var cmd = new NpgsqlCommand(@"
            INSERT INTO usuarios (empresa_id, nome, email, senha_hash, perfil)
            VALUES (@emp, @nome, @email, @hash, @perfil) RETURNING id;", conn, tx);
        cmd.Parameters.AddWithValue("emp", empresaId);
        cmd.Parameters.AddWithValue("nome", input.Nome?.Trim() ?? email.Split('@')[0]);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("hash", BCrypt.Net.BCrypt.HashPassword(senha));
        cmd.Parameters.AddWithValue("perfil", perfil);
        int userId = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        using var cm = new NpgsqlCommand(
            "INSERT INTO usuario_modulos (usuario_id, modulo, nivel) VALUES (@u, 'nutricao', @n);", conn, tx);
        cm.Parameters.AddWithValue("u", userId);
        cm.Parameters.AddWithValue("n", nivel);
        await cm.ExecuteNonQueryAsync();

        // Marcar convite como usado
        using var uc = new NpgsqlCommand(
            "UPDATE convites SET usado_em = CURRENT_TIMESTAMP WHERE id = @id;", conn, tx);
        uc.Parameters.AddWithValue("id", conviteId);
        await uc.ExecuteNonQueryAsync();

        await tx.CommitAsync();

        // Gerar token de acesso
        var modulos = new Dictionary<string, string> { ["nutricao"] = nivel };
        var jwt = Auth.Gerar(jwtKey, userId, empresaId, perfil, modulos);

        return Results.Ok(new { token = jwt, mensagem = "Bem-vindo! Conta criada com sucesso." });
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
});

// =====================================================================
// INPUT RECORDS PARA SOLICITAÃ‡Ã•ES DE ACESSO
// =====================================================================

// =====================================================================
// SOLICITAÃ‡Ã•ES DE ACESSO (PÃºblico + Super Admin)
// =====================================================================

// Helper: obter IP vÃ¡lido para banco (retorna IPAddress ou DBNull.Value se invÃ¡lido)
object ObterIpParaBanco(HttpContext http)
{
    var ipStr = http.Connection.RemoteIpAddress?.ToString();
    if (IPAddress.TryParse(ipStr, out var ip))
        return ip;
    return DBNull.Value;
}

// POST /api/acesso/solicitar - Visitante solicita acesso (pÃºblico)
app.MapPost("/api/acesso/solicitar", async (SolicitarAcessoInput input, HttpContext http) =>
{
    var nome = (input.Nome ?? "").Trim();
    var email = (input.Email ?? "").Trim().ToLowerInvariant();
    var telefone = (input.Telefone ?? "").Trim();
    var registroProfissional = (input.RegistroProfissional ?? "").Trim();
    var clinicaNome = (input.ClinicaNome ?? "").Trim();
    var mensagem = (input.Mensagem ?? "").Trim();
    var ip = http.Connection.RemoteIpAddress?.ToString() ?? "::1";
    var ipParaBanco = ObterIpParaBanco(http);
    var logger = http.RequestServices.GetRequiredService<ILogger<Program>>();

    if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
        return Results.BadRequest(new { mensagem = "Nome e e-mail válido são obrigatórios." });

    if (string.IsNullOrWhiteSpace(registroProfissional))
        return Results.BadRequest(new { mensagem = "Registro profissional (CRN/CRM) é obrigatório." });

    if (string.IsNullOrWhiteSpace(clinicaNome))
        return Results.BadRequest(new { mensagem = "Nome da clínica/empresa é obrigatório." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // 1. Verificar tentativas falhas por email+IP (janela 15 min, max 5)
    var (bloqueado, retryAfterSeg) = await VerificarTentativasFalhas(conn, "solicitar_acesso", email, ip, logger);
    if (bloqueado)
    {
        return RateLimitHelpers.JsonWithHeaders(new { mensagem = $"Muitas tentativas. Tente novamente em {Math.Ceiling(retryAfterSeg / 60.0)} minutos." }, 429, new Dictionary<string, string>
        {
            ["Retry-After"] = retryAfterSeg.ToString(),
            ["X-RateLimit-Limit"] = "5",
            ["X-RateLimit-Remaining"] = "0",
            ["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeg).ToUnixTimeSeconds().ToString()
        });
    }

    var userAgent = http.Request.Headers["User-Agent"].ToString();

    // 2. Verificar se já existe solicitação pendente para este email (evita exception de unique constraint)
    bool jaExistePendente = false;
    using (var cmd = new NpgsqlCommand("SELECT 1 FROM solicitacoes_acesso WHERE lower(email) = @email AND status = 'pendente'", conn))
    {
        cmd.Parameters.AddWithValue("email", email);
        using var rd = await cmd.ExecuteReaderAsync();
        jaExistePendente = await rd.ReadAsync();
    }

    if (jaExistePendente)
    {
        // Registrar tentativa falha (email já tem solicitação pendente)
        await RegistrarTentativaFalha(conn, "solicitar_acesso", email, ip, logger);
        return Results.BadRequest(new { mensagem = "Já existe uma solicitação pendente para este e-mail." });
    }

    // 3. Inserir solicitação
    using (var cmd = new NpgsqlCommand(@"
        INSERT INTO solicitacoes_acesso (nome, email, telefone, registro_profissional, clinica_nome, mensagem, ip, user_agent)
        VALUES (@nome, @email, @tel, @reg, @clinica, @msg, @ip, @ua) RETURNING id;", conn))
    {
        cmd.Parameters.AddWithValue("nome", nome);
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("tel", string.IsNullOrEmpty(telefone) ? DBNull.Value : (object)telefone);
        cmd.Parameters.AddWithValue("reg", registroProfissional);
        cmd.Parameters.AddWithValue("clinica", clinicaNome);
        cmd.Parameters.AddWithValue("msg", string.IsNullOrEmpty(mensagem) ? DBNull.Value : (object)mensagem);
        cmd.Parameters.AddWithValue("ip", ipParaBanco);
        cmd.Parameters.AddWithValue("ua", string.IsNullOrEmpty(userAgent) ? DBNull.Value : (object)userAgent);

        await cmd.ExecuteNonQueryAsync();
    }

    // 4. Sucesso: zerar tentativas falhas
    await ZerarTentativasFalhas(conn, "solicitar_acesso", email, ip, logger);

    // Sempre retornar sucesso genérico (anti-enumeração de email)
    return Results.Ok(new { mensagem = "Se os dados estiverem corretos, sua solicitação será analisada em até 2 dias úteis." });
})
.RequireRateLimiting("ip-auth")
.AllowAnonymous();

// GET /api/acessos - Lista solicitaÃ§Ãµes (super_admin)
app.MapGet("/api/acessos", async (ClaimsPrincipal user, string? status = null, int page = 1, int pageSize = 20) =>
{
    if (user.FindFirst("perfil")?.Value != "super_admin")
        return Results.Forbid();

    if (page < 1) page = 1;
    if (pageSize < 1 || pageSize > 100) pageSize = 20;
    var offset = (page - 1) * pageSize;

    var whereClause = "";
    var parameters = new List<NpgsqlParameter>();

    if (!string.IsNullOrEmpty(status) && new[] { "pendente", "aprovado", "recusado", "excluido" }.Contains(status))
    {
        whereClause = "WHERE sa.status = @status";
        parameters.Add(new NpgsqlParameter("@status", status));
    }

    var sqlCount = $@"
        SELECT COUNT(*) FROM solicitacoes_acesso sa
        LEFT JOIN empresas e ON e.id = sa.empresa_id
        LEFT JOIN usuarios u ON u.id = sa.usuario_id
        {whereClause};";

    var sqlList = $@"
        SELECT sa.id, sa.nome, sa.email, sa.telefone, sa.registro_profissional, sa.clinica_nome, sa.mensagem,
               sa.status, sa.empresa_id, sa.usuario_id, sa.criado_em, sa.atualizado_em,
               e.nome as empresa_nome, u.nome as usuario_nome
        FROM solicitacoes_acesso sa
        LEFT JOIN empresas e ON e.id = sa.empresa_id
        LEFT JOIN usuarios u ON u.id = sa.usuario_id
        {whereClause}
        ORDER BY sa.criado_em DESC
        LIMIT @limit OFFSET @offset;";

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Total count
    int total;
    using (var cmdCount = new NpgsqlCommand(sqlCount, conn))
    {
        foreach (var p in parameters) cmdCount.Parameters.Add(p);
        total = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
    }

    // List
    var lista = new List<object>();
    using (var cmd = new NpgsqlCommand(sqlList, conn))
    {
        foreach (var p in parameters) cmd.Parameters.Add(p);
        cmd.Parameters.AddWithValue("@limit", pageSize);
        cmd.Parameters.AddWithValue("@offset", offset);
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            lista.Add(new
            {
                id = rd.GetInt64(0),
                nome = rd.GetString(1),
                email = rd.GetString(2),
                telefone = rd.IsDBNull(3) ? null : rd.GetString(3),
                registroProfissional = rd.GetString(4),
                clinicaNome = rd.GetString(5),
                mensagem = rd.IsDBNull(6) ? null : rd.GetString(6),
                status = rd.GetString(7),
                empresaId = rd.IsDBNull(8) ? (long?)null : rd.GetInt64(8),
                usuarioId = rd.IsDBNull(9) ? (long?)null : rd.GetInt64(9),
                criadoEm = rd.GetDateTime(10).ToString("yyyy-MM-ddTHH:mm:ss"),
                atualizadoEm = rd.GetDateTime(11).ToString("yyyy-MM-ddTHH:mm:ss"),
                empresaNome = rd.IsDBNull(12) ? null : rd.GetString(12),
                usuarioNome = rd.IsDBNull(13) ? null : rd.GetString(13)
            });
        }
    }

    return Results.Ok(new
    {
        total,
        page,
        pageSize,
        totalPages = (int)Math.Ceiling((double)total / pageSize),
        data = lista
    });
})
.RequireAuthorization();

// POST /api/acessos/{id}/aprovar - Super admin aprova solicitaÃ§Ã£o
app.MapPost("/api/acessos/{id:long}/aprovar", async (long id, AprovarAcessoInput input, ClaimsPrincipal user, IEmailService emailService, ILogger<Program> logger) =>
{
    if (user.FindFirst("perfil")?.Value != "super_admin")
        return Results.Forbid();

    var adminId = Auth.UsuarioId(user);
    var adminNome = Auth.Nome(user) ?? "Super Admin";

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    try
    {
        // Buscar solicitaÃ§Ã£o pendente
        long solicitacaoId = 0;
        string nome = "", email = "", telefone = "", registroProfissional = "", clinicaNome = "", mensagem = "";
        using (var cmd = new NpgsqlCommand(@"
            SELECT id, nome, email, telefone, registro_profissional, clinica_nome, mensagem
            FROM solicitacoes_acesso
            WHERE id = @id AND status = 'pendente';", conn, tx))
        {
            cmd.Parameters.AddWithValue("@id", id);
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                return Results.NotFound(new { mensagem = "SolicitaÃ§Ã£o nÃ£o encontrada ou jÃ¡ processada." });

            solicitacaoId = rd.GetInt64(0);
            nome = rd.GetString(1);
            email = rd.GetString(2);
            telefone = rd.IsDBNull(3) ? "" : rd.GetString(3);
            registroProfissional = rd.GetString(4);
            clinicaNome = rd.GetString(5);
            mensagem = rd.IsDBNull(6) ? "" : rd.GetString(6);
        }

        // SEMPRE criar nova empresa (isolamento total)
        var empresaNome = !string.IsNullOrWhiteSpace(clinicaNome) ? clinicaNome : nome;
        int empresaId;
        using (var cmd = new NpgsqlCommand("INSERT INTO empresas (nome) VALUES (@n) RETURNING id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("@n", empresaNome);
            empresaId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // Ativar mÃ³dulo nutricao
        using (var cmd = new NpgsqlCommand("INSERT INTO empresa_modulos (empresa_id, modulo) VALUES (@e, 'nutricao') ON CONFLICT DO NOTHING;", conn, tx))
        {
            cmd.Parameters.AddWithValue("@e", empresaId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Criar usuÃ¡rio profissional
        var senhaTemporaria = Guid.NewGuid().ToString("N")[..12]; // senha aleatÃ³ria temporÃ¡ria
        var hash = BCrypt.Net.BCrypt.HashPassword(senhaTemporaria);
        int usuarioId;
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO usuarios (empresa_id, nome, email, senha_hash, perfil, ativo)
            VALUES (@emp, @nome, @email, @hash, 'profissional', true) RETURNING id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("@emp", empresaId);
            cmd.Parameters.AddWithValue("@nome", !string.IsNullOrWhiteSpace(input.NomeProfissional) ? input.NomeProfissional!.Trim() : nome);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@hash", hash);
            usuarioId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        // NÃ­vel de acesso nutricao = total (profissional dono da clÃ­nica)
        using (var cmd = new NpgsqlCommand(
            "INSERT INTO usuario_modulos (usuario_id, modulo, nivel) VALUES (@u, 'nutricao', 'total');", conn, tx))
        {
            cmd.Parameters.AddWithValue("@u", usuarioId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Criar assinatura com status 'sem_plano' (sem customer Asaas ainda)
        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO assinaturas (empresa_id, plano, status, valor_centavos, ciclo, limites)
            VALUES (@emp, 'sem_plano', 'sem_plano', 0, 'monthly', @limites);", conn, tx))
        {
            cmd.Parameters.AddWithValue("@emp", empresaId);
            var limitesSemPlano = Planos.ObterLimites("sem_plano");
            cmd.Parameters.Add(new NpgsqlParameter("limites", NpgsqlTypes.NpgsqlDbType.Jsonb)
            {
                Value = limitesSemPlano.ToJson()
            });
            await cmd.ExecuteNonQueryAsync();
        }

        // Gerar token de definiÃ§Ã£o de senha (expira em 48h, SHA-256 no banco, uso Ãºnico)
        // Token: 32 bytes aleatÃ³rios, base64url
        var tokenBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var tokenRaw = System.Convert.ToBase64String(tokenBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        // Guardar SHA-256(hex) como token_hash
        var tokenHash = System.Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tokenRaw))
        ).ToLowerInvariant();
        var expiraEm = DateTime.UtcNow.AddHours(48);

        // Invalidar tokens pendentes anteriores do mesmo usuÃ¡rio
        using (var cmd = new NpgsqlCommand(@"
            UPDATE tokens_redefinicao_senha
            SET usado_em = CURRENT_TIMESTAMP
            WHERE usuario_id = @u AND expira_em > CURRENT_TIMESTAMP AND usado_em IS NULL;", conn, tx))
        {
            cmd.Parameters.AddWithValue("@u", usuarioId);
            await cmd.ExecuteNonQueryAsync();
        }

        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO tokens_redefinicao_senha (usuario_id, token_hash, expira_em, usado_em, criado_em)
            VALUES (@u, @th, @exp, NULL, CURRENT_TIMESTAMP);", conn, tx))
        {
            cmd.Parameters.AddWithValue("@u", usuarioId);
            cmd.Parameters.AddWithValue("@th", tokenHash);
            cmd.Parameters.AddWithValue("@exp", expiraEm);
            await cmd.ExecuteNonQueryAsync();
        }

        // Atualizar solicitaÃ§Ã£o como aprovada
        using (var cmd = new NpgsqlCommand(@"
            UPDATE solicitacoes_acesso
            SET status = 'aprovado', empresa_id = @emp, usuario_id = @u, atualizado_em = CURRENT_TIMESTAMP
            WHERE id = @id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("@emp", empresaId);
            cmd.Parameters.AddWithValue("@u", usuarioId);
            cmd.Parameters.AddWithValue("@id", solicitacaoId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Auditoria
        await RegistrarAuditoriaDetalhada(conn, tx, empresaId, adminId, "aprovacao_acesso", $"/api/acessos/{id}/aprovar", solicitacaoId,
            $"SolicitaÃ§Ã£o de {nome} ({email}) aprovada. Empresa criada: {empresaNome} (ID: {empresaId}), UsuÃ¡rio: {usuarioId}");

        await tx.CommitAsync();

        // Enviar e-mail com link para definir senha (token raw, nÃ£o hash)
        var frontendUrl = GetFrontendUrl();
        var linkDefinirSenha = $"{frontendUrl.TrimEnd('/')}/definir-senha/{tokenRaw}";
        var assunto = $"Sua solicitaÃ§Ã£o de acesso foi aprovada - {empresaNome}";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #059669; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .btn {{ display: inline-block; background: #059669; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; margin-top: 20px; }}
        .btn:hover {{ background: #047857; }}
        .info {{ background: white; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #059669; }}
        .footer {{ text-align: center; color: #64748b; font-size: 14px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>ðŸŽ ACNUTRI</h1>
            <p>Sistema de NutriÃ§Ã£o Inteligente</p>
        </div>
        <div class='content'>
            <h2>SolicitaÃ§Ã£o Aprovada! âœ…</h2>
            <p>OlÃ¡ <strong>{nome}</strong>,</p>
            <p>Sua solicitaÃ§Ã£o de acesso ao <strong>ACNUTRI</strong> foi <strong>aprovada</strong>.</p>

            <div class='info'>
                <p><strong>Empresa/ClÃ­nica:</strong> {empresaNome}</p>
                <p><strong>Perfil:</strong> Profissional de NutriÃ§Ã£o</p>
                <p><strong>Validade do link:</strong> 48 horas</p>
            </div>

            <p>Clique no botÃ£o abaixo para definir sua senha e acessar o sistema:</p>

            <p style='text-align: center;'>
                <a href='{linkDefinirSenha}' class='btn' style='color:#ffffff !important; text-decoration:none;'>Definir Minha Senha e Acessar</a>
            </p>

            <p style='font-size: 14px; color: #64748b;'>
                Ou copie este link no navegador:<br>
                <a href='{linkDefinirSenha}'>{linkDefinirSenha}</a>
            </p>

            <p style='font-size: 14px; color: #ef4444;'>
                âš ï¸ Este link expira em 48 horas e pode ser usado apenas uma vez.
            </p>
        </div>
        <div class='footer'>
            <p>Este e-mail foi enviado automaticamente pelo ACNUTRI.<br>
            Se vocÃª nÃ£o solicitou este acesso, pode ignorar este e-mail.</p>
        </div>
    </div>
</body>
</html>";

        var emailEnviado = await emailService.EnviarEmailAsync(email, assunto, htmlBody);

        // Log apenas metadados, NUNCA o link, token ou senha
        if (!emailEnviado)
        {
            logger.LogWarning("Falha ao enviar e-mail de definiÃ§Ã£o de senha para solicitaÃ§Ã£o aprovada id={SolicitacaoId}, usuario={UsuarioId}, empresa={EmpresaId}",
                solicitacaoId, usuarioId, empresaId);
        }

        return Results.Ok(new
        {
            mensagem = "SolicitaÃ§Ã£o aprovada! Empresa, usuÃ¡rio e assinatura 'sem_plano' criados.",
            emailEnviado,
            avisoEmail = emailEnviado ? null : "âš ï¸ AprovaÃ§Ã£o concluÃ­da, mas houve falha no envio do e-mail de definiÃ§Ã£o de senha. Use o botÃ£o 'Reenviar e-mail' na tela Acessos.",
            empresaId,
            usuarioId
        });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.BadRequest(new { mensagem = "Erro ao aprovar: " + ex.Message });
    }
})
.RequireAuthorization();

// POST /api/acessos/{id}/reenviar-email - Super admin reenvia e-mail de definiÃ§Ã£o de senha para solicitaÃ§Ã£o aprovada
app.MapPost("/api/acessos/{id:long}/reenviar-email", async (long id, ClaimsPrincipal user, IEmailService emailService, ILogger<Program> logger) =>
{
    if (user.FindFirst("perfil")?.Value != "super_admin")
        return Results.Forbid();

    var adminId = Auth.UsuarioId(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    try
    {
        // Buscar solicitaÃ§Ã£o aprovada com usuario_id
        long solicitacaoId = 0;
        string nome = "", email = "";
        int usuarioId = 0;
        string empresaNome = "";
        using (var cmd = new NpgsqlCommand(@"
            SELECT sa.id, sa.nome, sa.email, sa.usuario_id, e.nome as empresa_nome
            FROM solicitacoes_acesso sa
            LEFT JOIN empresas e ON e.id = sa.empresa_id
            WHERE sa.id = @id AND sa.status = 'aprovado';", conn, tx))
        {
            cmd.Parameters.AddWithValue("@id", id);
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                return Results.NotFound(new { mensagem = "SolicitaÃ§Ã£o nÃ£o encontrada ou nÃ£o estÃ¡ aprovada." });

            solicitacaoId = rd.GetInt64(0);
            nome = rd.GetString(1);
            email = rd.GetString(2);
            usuarioId = rd.IsDBNull(3) ? 0 : rd.GetInt32(3);
            empresaNome = rd.IsDBNull(4) ? "" : rd.GetString(4);
        }

        if (usuarioId == 0)
            return Results.BadRequest(new { mensagem = "SolicitaÃ§Ã£o aprovada nÃ£o possui usuÃ¡rio associado." });

        // Invalidar tokens pendentes anteriores do mesmo usuÃ¡rio
        using (var cmd = new NpgsqlCommand(@"
            UPDATE tokens_redefinicao_senha
            SET usado_em = CURRENT_TIMESTAMP
            WHERE usuario_id = @u AND expira_em > CURRENT_TIMESTAMP AND usado_em IS NULL;", conn, tx))
        {
            cmd.Parameters.AddWithValue("@u", usuarioId);
            await cmd.ExecuteNonQueryAsync();
        }

        // Gerar novo token de definiÃ§Ã£o de senha (expira em 48h, SHA-256 no banco, uso Ãºnico)
        var tokenBytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }
        var tokenRaw = System.Convert.ToBase64String(tokenBytes)
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');
        var tokenHash = System.Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(tokenRaw))
        ).ToLowerInvariant();
        var expiraEm = DateTime.UtcNow.AddHours(48);

        using (var cmd = new NpgsqlCommand(@"
            INSERT INTO tokens_redefinicao_senha (usuario_id, token_hash, expira_em, usado_em, criado_em)
            VALUES (@u, @th, @exp, NULL, CURRENT_TIMESTAMP);", conn, tx))
        {
            cmd.Parameters.AddWithValue("@u", usuarioId);
            cmd.Parameters.AddWithValue("@th", tokenHash);
            cmd.Parameters.AddWithValue("@exp", expiraEm);
            await cmd.ExecuteNonQueryAsync();
        }

        // Auditoria
        await RegistrarAuditoriaDetalhada(conn, tx, 0, adminId, "reenviar_email_definicao_senha", $"/api/acessos/{id}/reenviar-email", solicitacaoId,
            $"Reenvio de e-mail de definiÃ§Ã£o de senha para {nome} ({email}) - solicitaÃ§Ã£o {solicitacaoId}");

        await tx.CommitAsync();

        // Enviar e-mail com novo link (token raw, nÃ£o hash)
        var frontendUrl = GetFrontendUrl();
        var linkDefinirSenha = $"{frontendUrl.TrimEnd('/')}/definir-senha/{tokenRaw}";
        var assunto = $"Defina sua senha no ACNUTRI - {empresaNome} (Reenviado)";
        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #059669; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .btn {{ display: inline-block; background: #059669; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; margin-top: 20px; }}
        .btn:hover {{ background: #047857; }}
        .info {{ background: white; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #059669; }}
        .footer {{ text-align: center; color: #64748b; font-size: 14px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>ðŸŽ ACNUTRI</h1>
            <p>Sistema de NutriÃ§Ã£o Inteligente</p>
        </div>
        <div class='content'>
            <h2>Defina sua senha para acessar o ACNUTRI âœ…</h2>
            <p>OlÃ¡ <strong>{nome}</strong>,</p>
            <p>Este e-mail foi reenviado pelo administrador para que vocÃª possa definir sua senha e acessar o <strong>ACNUTRI</strong>.</p>

            <div class='info'>
                <p><strong>Empresa/ClÃ­nica:</strong> {empresaNome}</p>
                <p><strong>Perfil:</strong> Profissional de NutriÃ§Ã£o</p>
                <p><strong>Validade do link:</strong> 48 horas</p>
            </div>

            <p>Clique no botÃ£o abaixo para definir sua senha e acessar o sistema:</p>

            <p style='text-align: center;'>
                <a href='{linkDefinirSenha}' class='btn' style='color:#ffffff !important; text-decoration:none;'>Definir Minha Senha e Acessar</a>
            </p>

            <p style='font-size: 14px; color: #64748b;'>
                Ou copie este link no navegador:<br>
                <a href='{linkDefinirSenha}'>{linkDefinirSenha}</a>
            </p>

            <p style='font-size: 14px; color: #ef4444;'>
                âš ï¸ Este link expira em 48 horas e pode ser usado apenas uma vez.
            </p>
        </div>
        <div class='footer'>
            <p>Este e-mail foi enviado automaticamente pelo ACNUTRI.<br>
            Se vocÃª nÃ£o solicitou este acesso, pode ignorar este e-mail.</p>
        </div>
    </div>
</body>
</html>";

        var emailEnviado = await emailService.EnviarEmailAsync(email, assunto, htmlBody);

        // Log apenas metadados, NUNCA o link, token ou senha
        if (!emailEnviado)
        {
            logger.LogWarning("Falha ao reenviar e-mail de definiÃ§Ã£o de senha para solicitaÃ§Ã£o id={SolicitacaoId}, usuario={UsuarioId}",
                solicitacaoId, usuarioId);
        }

        return Results.Ok(new
        {
            emailEnviado,
            mensagem = emailEnviado
                ? "E-mail de definiÃ§Ã£o de senha reenviado com sucesso!"
                : "Houve falha no envio do e-mail. Verifique a configuraÃ§Ã£o SMTP e tente novamente."
        });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.BadRequest(new { mensagem = "Erro ao reenviar e-mail: " + ex.Message });
    }
})
.RequireAuthorization();

// POST /api/acessos/{id}/recusar - Super admin recusa solicitaÃ§Ã£o
app.MapPost("/api/acessos/{id:long}/recusar", async (long id, ClaimsPrincipal user) =>
{
    if (user.FindFirst("perfil")?.Value != "super_admin")
        return Results.Forbid();

    var adminId = Auth.UsuarioId(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    try
    {
        // Buscar solicitaÃ§Ã£o pendente
        string nome = "", email = "";
        using (var cmd = new NpgsqlCommand(@"
            SELECT nome, email FROM solicitacoes_acesso
            WHERE id = @id AND status = 'pendente';", conn, tx))
        {
            cmd.Parameters.AddWithValue("@id", id);
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                return Results.NotFound(new { mensagem = "SolicitaÃ§Ã£o nÃ£o encontrada ou jÃ¡ processada." });

            nome = rd.GetString(0);
            email = rd.GetString(1);
        }

        // Atualizar status para recusado
        using (var cmd = new NpgsqlCommand(@"
            UPDATE solicitacoes_acesso
            SET status = 'recusado', atualizado_em = CURRENT_TIMESTAMP
            WHERE id = @id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("@id", id);
            await cmd.ExecuteNonQueryAsync();
        }

        // Auditoria
        await RegistrarAuditoriaDetalhada(conn, tx, 0, adminId, "recusa_acesso", $"/api/acessos/{id}/recusar", id,
            $"SolicitaÃ§Ã£o de {nome} ({email}) recusada.");

        await tx.CommitAsync();

        return Results.Ok(new { mensagem = "SolicitaÃ§Ã£o recusada." });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.BadRequest(new { mensagem = "Erro ao recusar: " + ex.Message });
    }
})
.RequireAuthorization();

// DELETE /api/acessos/{id} - Super admin exclui solicitaÃ§Ã£o
app.MapDelete("/api/acessos/{id:long}", async (long id, ClaimsPrincipal user) =>
{
    if (user.FindFirst("perfil")?.Value != "super_admin")
        return Results.Forbid();

    var adminId = Auth.UsuarioId(user);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    try
    {
        // Buscar solicitaÃ§Ã£o
        string status = "", nome = "", email = "";
        long? empresaId = null, usuarioId = null;
        using (var cmd = new NpgsqlCommand(@"
            SELECT status, nome, email, empresa_id, usuario_id
            FROM solicitacoes_acesso WHERE id = @id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("@id", id);
            using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                return Results.NotFound(new { mensagem = "SolicitaÃ§Ã£o nÃ£o encontrada." });

            status = rd.GetString(0);
            nome = rd.GetString(1);
            email = rd.GetString(2);
            empresaId = rd.IsDBNull(3) ? null : rd.GetInt64(3);
            usuarioId = rd.IsDBNull(4) ? null : rd.GetInt64(4);
        }

        if (status == "aprovado" && empresaId.HasValue && usuarioId.HasValue)
        {
            // Soft delete: desativar usuÃ¡rio (nÃ£o excluir empresa nem dados)
            using (var cmd = new NpgsqlCommand("UPDATE usuarios SET ativo = false WHERE id = @id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", usuarioId.Value);
                await cmd.ExecuteNonQueryAsync();
            }

            // Atualizar solicitaÃ§Ã£o para excluido
            using (var cmd = new NpgsqlCommand(@"
                UPDATE solicitacoes_acesso
                SET status = 'excluido', atualizado_em = CURRENT_TIMESTAMP
                WHERE id = @id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }

            // Auditoria
            await RegistrarAuditoriaDetalhada(conn, tx, (int)empresaId.Value, adminId, "exclusao_acesso", $"/api/acessos/{id}", id,
                $"Acesso aprovado de {nome} ({email}) excluÃ­do (soft delete). UsuÃ¡rio {usuarioId} desativado, empresa {empresaId} mantida.");
        }
        else
        {
            // Hard delete para pendente/recusado
            using (var cmd = new NpgsqlCommand("DELETE FROM solicitacoes_acesso WHERE id = @id;", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                await cmd.ExecuteNonQueryAsync();
            }

            // Auditoria (empresa_id = 0 pois nÃ£o hÃ¡ empresa associada)
            await RegistrarAuditoriaDetalhada(conn, tx, 0, adminId, "exclusao_acesso", $"/api/acessos/{id}", id,
                $"SolicitaÃ§Ã£o {status} de {nome} ({email}) excluÃ­da permanentemente.");
        }

        await tx.CommitAsync();

        return Results.Ok(new { mensagem = status == "aprovado" ? "Acesso desativado (usuÃ¡rio desativado, empresa mantida)." : "SolicitaÃ§Ã£o excluÃ­da permanentemente." });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        return Results.BadRequest(new { mensagem = "Erro ao excluir: " + ex.Message });
    }
})
.RequireAuthorization();

// =====================================================================
// DEFINIR SENHA (token de 48h, uso Ãºnico, hash no banco)
// =====================================================================

// GET /api/definir-senha/{token} - Valida token (para pÃ¡gina frontend)
app.MapGet("/api/definir-senha/{token}", async (string token) =>
{
    if (string.IsNullOrWhiteSpace(token) || token.Length < 32)
        return Results.NotFound(new { mensagem = "Token invÃ¡lido.", valido = false });

    // Calcular SHA-256 do token recebido
    var tokenHash = System.Convert.ToHexString(
        System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))
    ).ToLowerInvariant();

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Buscar token vÃ¡lido por hash (nÃ£o usado, nÃ£o expirado)
    int usuarioId = 0;
    string email = "", nome = "";
    using (var cmd = new NpgsqlCommand(@"
        SELECT tr.usuario_id, u.email, u.nome
        FROM tokens_redefinicao_senha tr
        JOIN usuarios u ON u.id = tr.usuario_id
        WHERE tr.token_hash = @th AND tr.expira_em > CURRENT_TIMESTAMP AND tr.usado_em IS NULL;", conn))
    {
        cmd.Parameters.AddWithValue("@th", tokenHash);
        using var rd = await cmd.ExecuteReaderAsync();
        if (await rd.ReadAsync())
        {
            usuarioId = rd.GetInt32(0);
            email = rd.GetString(1);
            nome = rd.GetString(2);
        }
    }

    if (usuarioId == 0)
        return Results.NotFound(new { mensagem = "Token invÃ¡lido ou expirado.", valido = false });

    return Results.Ok(new { valido = true, email, nome });
})
.AllowAnonymous();

// POST /api/definir-senha/{token} - Define nova senha
app.MapPost("/api/definir-senha/{token}", async (string token, DefinirSenhaInput input, HttpContext http) =>
{
    var senha = input.Senha ?? "";
    var confirmar = input.ConfirmarSenha ?? "";
    // Em TestServer RemoteIpAddress pode ser null; default para ::1 (loopback IPv6) para testes funcionarem
    var ip = http.Connection.RemoteIpAddress?.ToString() ?? "::1";
    var logger = http.RequestServices.GetRequiredService<ILogger<Program>>();

    if (string.IsNullOrWhiteSpace(token) || token.Length < 32)
        return Results.BadRequest(new { mensagem = "Token inválido." });

    if (senha.Length < 8)
        return Results.BadRequest(new { mensagem = "A senha precisa ter pelo menos 8 caracteres." });

    if (senha != confirmar)
        return Results.BadRequest(new { mensagem = "As senhas não conferem." });

    // Calcular SHA-256 do token recebido (usado como identificador para rate limiting)
    var tokenHash = System.Convert.ToHexString(
        System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token))
    ).ToLowerInvariant();

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // 1. Verificar tentativas falhas por token_hash+IP (janela 15 min, max 5) - ANTES de validar token no banco
    var (bloqueado, retryAfterSeg) = await VerificarTentativasFalhas(conn, "definir_senha", tokenHash, ip, logger);
    if (bloqueado)
    {
        return RateLimitHelpers.JsonWithHeaders(new { mensagem = $"Muitas tentativas. Tente novamente em {Math.Ceiling(retryAfterSeg / 60.0)} minutos." }, 429, new Dictionary<string, string>
        {
            ["Retry-After"] = retryAfterSeg.ToString(),
            ["X-RateLimit-Limit"] = "5",
            ["X-RateLimit-Remaining"] = "0",
            ["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeg).ToUnixTimeSeconds().ToString()
        });
    }

    // 2. Validar token e obter email do usuário
    string email = "";
    int usuarioId = 0;
    using (var cmd = new NpgsqlCommand(@"
        SELECT tr.usuario_id, u.email
        FROM tokens_redefinicao_senha tr
        JOIN usuarios u ON u.id = tr.usuario_id
        WHERE tr.token_hash = @th AND tr.expira_em > CURRENT_TIMESTAMP AND tr.usado_em IS NULL;", conn))
    {
        cmd.Parameters.AddWithValue("@th", tokenHash);
        using var rd = await cmd.ExecuteReaderAsync();
        if (await rd.ReadAsync())
        {
            usuarioId = rd.GetInt32(0);
            email = rd.GetString(1);
        }
    }

    if (usuarioId == 0)
    {
        // Registrar tentativa falha (token inválido)
        await RegistrarTentativaFalha(conn, "definir_senha", tokenHash, ip, logger);
        return Results.BadRequest(new { mensagem = "Token inválido, expirado ou já utilizado." });
    }

    using var tx = await conn.BeginTransactionAsync();

    try
    {
        // 3. Consumir token atomicamente
        using (var cmd = new NpgsqlCommand(@"
            UPDATE tokens_redefinicao_senha
            SET usado_em = CURRENT_TIMESTAMP
            WHERE token_hash = @th AND expira_em > CURRENT_TIMESTAMP AND usado_em IS NULL
            RETURNING usuario_id;", conn, tx))
        {
            cmd.Parameters.AddWithValue("@th", tokenHash);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
                return Results.BadRequest(new { mensagem = "Token invÃ¡lido, expirado ou jÃ¡ utilizado." });
        }

        // Verificar se usuÃ¡rio estÃ¡ ativo
        bool ativo = false;
        using (var cmd = new NpgsqlCommand("SELECT ativo FROM usuarios WHERE id = @id", conn, tx))
        {
            cmd.Parameters.AddWithValue("@id", usuarioId);
            var result = await cmd.ExecuteScalarAsync();
            if (result == null)
                return Results.BadRequest(new { mensagem = "UsuÃ¡rio nÃ£o encontrado." });
            ativo = Convert.ToBoolean(result);
        }

        if (!ativo)
            return Results.BadRequest(new { mensagem = "UsuÃ¡rio desativado. Contate o administrador." });

        // Atualizar senha
        var novoHash = BCrypt.Net.BCrypt.HashPassword(senha);
        using (var cmd = new NpgsqlCommand("UPDATE usuarios SET senha_hash = @hash WHERE id = @id", conn, tx))
        {
            cmd.Parameters.AddWithValue("@hash", novoHash);
            cmd.Parameters.AddWithValue("@id", usuarioId);
            await cmd.ExecuteNonQueryAsync();
        }

        await tx.CommitAsync();

        // 4. Sucesso: zerar tentativas falhas
        await ZerarTentativasFalhas(conn, "definir_senha", email, ip, logger);

        return Results.Ok(new { mensagem = "Senha definida com sucesso! FaÃ§a login para acessar." });
    }
    catch (Exception ex)
    {
        await tx.RollbackAsync();
        // Registrar tentativa falha
        await RegistrarTentativaFalha(conn, "definir_senha", email, ip, logger);
        return Results.BadRequest(new { mensagem = "Erro ao definir senha: " + ex.Message });
    }
})
.RequireRateLimiting("ip-auth")
.AllowAnonymous();

app.MapGet("/api/usuarios", async (ClaimsPrincipal user) =>
{
    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT u.id, u.nome, u.email, u.perfil, u.ativo, COALESCE(um.nivel, 'nenhum')
        FROM usuarios u
        LEFT JOIN usuario_modulos um ON um.usuario_id = u.id AND um.modulo = 'nutricao'
        WHERE u.empresa_id = @emp AND u.perfil <> 'cliente'
        ORDER BY u.nome;", conn);
    cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
        lista.Add(new { id = rd.GetInt32(0), nome = rd.GetString(1), email = rd.GetString(2),
                        perfil = rd.GetString(3), ativo = rd.GetBoolean(4), nivelNutricao = rd.GetString(5) });
    return Results.Ok(lista);
});

app.MapPost("/api/usuarios", async (UsuarioInput i, ClaimsPrincipal user) =>
{
    var email = (i.Email ?? "").Trim().ToLowerInvariant();
    if (string.IsNullOrWhiteSpace(i.Nome) || email == "")
        return Results.BadRequest(new { mensagem = "Informe nome e e-mail." });
    if ((i.Senha ?? "").Length < 8)
        return Results.BadRequest(new { mensagem = "A senha precisa ter pelo menos 8 caracteres." });

    var perfil = perfisEquipe.Contains(i.Perfil ?? "") ? i.Perfil! : "atendente";
    var nivel = niveisAcesso.Contains(i.NivelNutricao ?? "") ? i.NivelNutricao! : "leitura";

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    try
    {
        using var cmd = new NpgsqlCommand(@"
            INSERT INTO usuarios (empresa_id, nome, email, senha_hash, perfil)
            VALUES (@emp, @nome, @email, @hash, @perfil) RETURNING id;", conn);
        cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
        cmd.Parameters.AddWithValue("nome", i.Nome!.Trim());
        cmd.Parameters.AddWithValue("email", email);
        cmd.Parameters.AddWithValue("hash", BCrypt.Net.BCrypt.HashPassword(i.Senha));
        cmd.Parameters.AddWithValue("perfil", perfil);
        int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        using var cm = new NpgsqlCommand(
            "INSERT INTO usuario_modulos (usuario_id, modulo, nivel) VALUES (@u, 'nutricao', @n);", conn);
        cm.Parameters.AddWithValue("u", id);
        cm.Parameters.AddWithValue("n", nivel);
        await cm.ExecuteNonQueryAsync();

        return Results.Created($"/api/usuarios/{id}", new { id, mensagem = "UsuÃ¡rio criado!" });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        return Results.Conflict(new { mensagem = "JÃ¡ existe um usuÃ¡rio com esse e-mail." });
    }
});

app.MapPut("/api/usuarios/{id}", async (int id, UsuarioInput i, ClaimsPrincipal user) =>
{
    int meuId = int.Parse(user.FindFirst("uid")!.Value);
    var perfil = perfisEquipe.Contains(i.Perfil ?? "") ? i.Perfil! : "atendente";
    var nivel = niveisAcesso.Contains(i.NivelNutricao ?? "") ? i.NivelNutricao! : "leitura";
    bool ativo = i.Ativo ?? true;

    if (id == meuId && (!ativo || perfil != "admin"))
        return Results.BadRequest(new { mensagem = "VocÃª nÃ£o pode desativar nem rebaixar o prÃ³prio usuÃ¡rio." });
    if (!string.IsNullOrEmpty(i.Senha) && i.Senha.Length < 8)
        return Results.BadRequest(new { mensagem = "A senha precisa ter pelo menos 8 caracteres." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        UPDATE usuarios SET nome = COALESCE(NULLIF(@nome, ''), nome), perfil = @perfil, ativo = @ativo,
            senha_hash = CASE WHEN @hash = '' THEN senha_hash ELSE @hash END
        WHERE id = @id AND empresa_id = @emp;", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
    cmd.Parameters.AddWithValue("nome", (i.Nome ?? "").Trim());
    cmd.Parameters.AddWithValue("perfil", perfil);
    cmd.Parameters.AddWithValue("ativo", ativo);
    cmd.Parameters.AddWithValue("hash", string.IsNullOrEmpty(i.Senha) ? "" : BCrypt.Net.BCrypt.HashPassword(i.Senha));
    if (await cmd.ExecuteNonQueryAsync() == 0)
        return Results.NotFound(new { mensagem = "UsuÃ¡rio nÃ£o encontrado." });

    using var cm = new NpgsqlCommand(@"
        INSERT INTO usuario_modulos (usuario_id, modulo, nivel) VALUES (@u, 'nutricao', @n)
        ON CONFLICT (usuario_id, modulo) DO UPDATE SET nivel = EXCLUDED.nivel;", conn);
    cm.Parameters.AddWithValue("u", id);
    cm.Parameters.AddWithValue("n", nivel);
    await cm.ExecuteNonQueryAsync();

    return Results.Ok(new { mensagem = "UsuÃ¡rio atualizado!" });
});

// =====================================================================
// ÃREA DO PACIENTE
// =====================================================================
app.MapPost("/api/auth/paciente", async (PacienteLoginInput i, HttpContext ctx) =>
{
    const string generico = "CPF ou data de nascimento incorretos.";
    var cpf = Cpf.Limpar(i.Cpf);
    // Em TestServer RemoteIpAddress pode ser null; default para ::1 (loopback IPv6) para testes funcionarem
    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "::1";
    var logger = ctx.RequestServices.GetRequiredService<ILogger<Program>>();

    // Validar formato CPF e nascimento (necessário para rate limiting por identificador)
    if (!Cpf.Valido(cpf) || !DateTime.TryParse(i.Nascimento,
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var nasc))
        return Results.Json(new { mensagem = generico }, statusCode: 401);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // 1. Verificar tentativas falhas por CPF+IP (janela 15 min, max 5) - ANTES de buscar no banco
    var (bloqueado, retryAfterSeg) = await VerificarTentativasFalhas(conn, "paciente", cpf, ip, logger);
    if (bloqueado)
    {
        return RateLimitHelpers.JsonWithHeaders(new { mensagem = $"Muitas tentativas. Tente novamente em {Math.Ceiling(retryAfterSeg / 60.0)} minutos." }, 429, new Dictionary<string, string>
        {
            ["Retry-After"] = retryAfterSeg.ToString(),
            ["X-RateLimit-Limit"] = "5",
            ["X-RateLimit-Remaining"] = "0",
            ["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddSeconds(retryAfterSeg).ToUnixTimeSeconds().ToString()
        });
    }

    int pid = 0, empresa = 0; string nome = "";
    bool achou;
    using (var c = new NpgsqlCommand(@"
        SELECT id, nome, empresa_id FROM pacientes
        WHERE cpf = @c AND data_nascimento = @d AND COALESCE(status, 'ativo') = 'ativo';", conn))
    {
        c.Parameters.AddWithValue("c", cpf);
        c.Parameters.Add(new NpgsqlParameter("d", NpgsqlTypes.NpgsqlDbType.Date) { Value = nasc.Date });
        using var rd = await c.ExecuteReaderAsync();
        achou = await rd.ReadAsync();
        if (achou) { pid = rd.GetInt32(0); nome = rd.GetString(1); empresa = rd.GetInt32(2); }
    }

    if (!achou)
    {
        // Registrar tentativa falha
        await RegistrarTentativaFalha(conn, "paciente", cpf, ip, logger);
        return Results.Json(new { mensagem = generico }, statusCode: 401);
    }

    // 2. Sucesso: zerar tentativas falhas para este CPF+IP
    await ZerarTentativasFalhas(conn, "paciente", cpf, ip, logger);

    return Results.Ok(new { token = Auth.GerarPaciente(jwtKey, pid, empresa), nome = nome.Split(' ')[0], tipo = "paciente" });
}).RequireRateLimiting("ip-auth");

// Tudo que a paciente enxerga, numa chamada sÃ³ (o ID vem do token)
app.MapGet("/api/paciente/tudo", async (ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    string nome;
    using (var c = new NpgsqlCommand("SELECT nome FROM pacientes WHERE id = @p", conn))
    {
        c.Parameters.AddWithValue("p", pid);
        nome = (await c.ExecuteScalarAsync())?.ToString() ?? "";
    }

    var plano = await PlanoHelper.Montar(connString, pid, int.Parse(user.FindFirst("empresa")!.Value));

    string orientacoes;
    using (var c = new NpgsqlCommand("SELECT texto FROM orientacoes_nutricionais WHERE paciente_id = @p", conn))
    {
        c.Parameters.AddWithValue("p", pid);
        orientacoes = (await c.ExecuteScalarAsync())?.ToString() ?? "";
    }

    var metas = new List<Dictionary<string, object?>>();
    var porId = new Dictionary<int, List<object>>();
    using (var c = new NpgsqlCommand(@"
        SELECT id, titulo, COALESCE(descricao, ''), tipo, prazo, status
        FROM metas_paciente WHERE paciente_id = @p AND status IN ('ativa', 'concluida')
        ORDER BY CASE status WHEN 'ativa' THEN 0 ELSE 1 END, id DESC;", conn))
    {
        c.Parameters.AddWithValue("p", pid);
        using var rd = await c.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            var acs = new List<object>();
            int id = rd.GetInt32(0);
            porId[id] = acs;
            metas.Add(new()
            {
                ["titulo"] = rd.GetString(1), ["descricao"] = rd.GetString(2), ["tipo"] = rd.GetString(3),
                ["prazo"] = rd.IsDBNull(4) ? null : rd.GetDateTime(4).ToString("yyyy-MM-dd"),
                ["status"] = rd.GetString(5), ["acompanhamentos"] = acs
            });
        }
    }
    using (var c = new NpgsqlCommand(@"
        SELECT a.meta_id, a.data_acompanhamento, a.resultado
        FROM metas_acompanhamento a JOIN metas_paciente m ON m.id = a.meta_id
        WHERE m.paciente_id = @p ORDER BY a.data_acompanhamento DESC, a.id DESC;", conn))
    {
        c.Parameters.AddWithValue("p", pid);
        using var rd = await c.ExecuteReaderAsync();
        while (await rd.ReadAsync())
            if (porId.TryGetValue(rd.GetInt32(0), out var l))
                l.Add(new { data = rd.GetDateTime(1).ToString("yyyy-MM-dd"), resultado = rd.GetString(2) });
    }

    return Results.Ok(new { pid, nome, plano, orientacoes, metas });
});

// Para a equipe: Ãºltimos acessos da paciente
app.MapGet("/api/acessos/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    // Apenas usuÃ¡rios do tipo "equipe" podem acessar (nÃ£o pacientes)
    var tipo = user.FindFirst("tipo")?.Value;
    if (tipo != "equipe")
        return Results.Forbid();

    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa do usuÃ¡rio
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var c = new NpgsqlCommand(
        "SELECT criado_em FROM acessos_paciente WHERE paciente_id = @p AND sucesso ORDER BY id DESC LIMIT 10;", conn);
    c.Parameters.AddWithValue("p", pacienteId);
    using var rd = await c.ExecuteReaderAsync();
    while (await rd.ReadAsync()) lista.Add(new { em = rd.GetDateTime(0).ToString("yyyy-MM-ddTHH:mm:ss") });
    return Results.Ok(lista);
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// =====================================================================
// ÃREA DO PACIENTE: EXAMES
// =====================================================================

// POST /api/paciente/exames - Paciente envia exames (multipart: titulo, data, observacao, files)
app.MapPost("/api/paciente/exames", async (HttpRequest request, ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);
    int emp = int.Parse(user.FindFirst("empresa")!.Value);

    var form = await request.ReadFormAsync();
    string titulo = form["titulo"].ToString().Trim();
    if (titulo == "")
        return Results.BadRequest(new { mensagem = "Informe o nome do exame." });
    if (titulo.Length > 100) titulo = titulo[..100];

    var erro = ArquivoHelper.Validar(form.Files, somenteImagem: false);
    if (erro != null) return Results.BadRequest(new { mensagem = erro });

    // Limite: atÃ© 5 arquivos por envio
    if (form.Files.Count > 5)
        return Results.BadRequest(new { mensagem = "MÃ¡ximo 5 arquivos por envio." });

    // Limite: 30 exames por paciente
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using (var cmdCount = new NpgsqlCommand("SELECT COUNT(*) FROM exames_paciente WHERE paciente_id = @p AND enviado_por = 'paciente'", conn))
    {
        cmdCount.Parameters.AddWithValue("p", pid);
        var total = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());
        if (total >= 30)
            return Results.BadRequest(new { mensagem = "Limite de 30 exames por paciente atingido." });
    }

    using var tx = await conn.BeginTransactionAsync();
    try
    {
        using var cmd = new NpgsqlCommand(@"
            INSERT INTO exames_paciente (paciente_id, empresa_id, titulo_exame, data_exame, observacao, enviado_por)
            VALUES (@p, @emp, @titulo, @data, @obs, 'paciente') RETURNING id;", conn, tx);
        cmd.Parameters.AddWithValue("p", pid);
        cmd.Parameters.AddWithValue("emp", emp);
        cmd.Parameters.AddWithValue("titulo", titulo);
        cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date) { Value = EvolucaoCampos.LerData(form["data"]) });
        cmd.Parameters.AddWithValue("obs", form["observacao"].ToString());
        int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        await ArquivoHelper.Salvar(conn, tx, pid, "exame", id, form.Files);
        await tx.CommitAsync();

        return Results.Created($"/api/paciente/exames/{id}", new { id, mensagem = "Exame enviado com sucesso!" });
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
});

// GET /api/paciente/exames - Lista exames do paciente (com arquivos)
app.MapGet("/api/paciente/exames", async (ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);
    int emp = int.Parse(user.FindFirst("empresa")!.Value);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var anexos = await ArquivoHelper.Listar(conn, pid, "exame");
    var lista = new List<object>();

    using var cmd = new NpgsqlCommand(@"
        SELECT id, titulo_exame, data_exame, COALESCE(observacao, ''), enviado_por
        FROM exames_paciente
        WHERE paciente_id = @p
        ORDER BY data_exame DESC, id DESC;", conn);
    cmd.Parameters.AddWithValue("p", pid);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        int id = rd.GetInt32(0);
        lista.Add(new
        {
            id,
            titulo = rd.GetString(1),
            data = rd.GetDateTime(2).ToString("yyyy-MM-dd"),
            observacao = rd.GetString(3),
            enviado_por = rd.GetString(4),
            arquivos = anexos.TryGetValue(id, out var a) ? a : new List<object>()
        });
    }
    return Results.Ok(lista);
});

// GET /api/paciente/arquivos/{id} - Devolve arquivo do exame do paciente
app.MapGet("/api/paciente/arquivos/{id:int}", async (int id, ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT mime, dados FROM arquivos_paciente WHERE id = @id AND paciente_id = @p", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("p", pid);
    using var rd = await cmd.ExecuteReaderAsync();
    if (!await rd.ReadAsync())
        return Results.NotFound(new { mensagem = "Arquivo nÃ£o encontrado." });

    var result = Results.File(rd.GetFieldValue<byte[]>(1), rd.GetString(0));
    // Adiciona header de seguranÃ§a
    var httpContext = app.Services.GetRequiredService<IHttpContextAccessor>().HttpContext;
    if (httpContext != null)
        httpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
    return result;
});

// DELETE /api/paciente/exames/{id} - Apaga exame enviado pelo prÃ³prio paciente (com arquivos)
app.MapDelete("/api/paciente/exames/{id:int}", async (int id, ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    // Verifica se Ã© exame do paciente e enviado_por = 'paciente'
    using (var cmdCheck = new NpgsqlCommand("SELECT id FROM exames_paciente WHERE id = @id AND paciente_id = @p AND enviado_por = 'paciente'", conn, tx))
    {
        cmdCheck.Parameters.AddWithValue("id", id);
        cmdCheck.Parameters.AddWithValue("p", pid);
        if (await cmdCheck.ExecuteScalarAsync() == null)
        {
            await tx.RollbackAsync();
            return Results.NotFound(new { mensagem = "Exame nÃ£o encontrado ou nÃ£o pode ser excluÃ­do." });
        }
    }

    // Apaga arquivos
    using (var cmdA = new NpgsqlCommand("DELETE FROM arquivos_paciente WHERE origem = 'exame' AND origem_id = @id", conn, tx))
    {
        cmdA.Parameters.AddWithValue("id", id);
        await cmdA.ExecuteNonQueryAsync();
    }

    // Apaga exame
    using var cmd = new NpgsqlCommand("DELETE FROM exames_paciente WHERE id = @id", conn, tx);
    cmd.Parameters.AddWithValue("id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    await tx.CommitAsync();

    return rows > 0
        ? Results.Ok(new { mensagem = "Exame excluÃ­do com sucesso!" })
        : Results.NotFound(new { mensagem = "Exame nÃ£o encontrado." });
});

// POST /api/paciente/consentimentos - Registra consentimento do paciente (aceito=true) ou revoga (aceito=false)
app.MapPost("/api/paciente/consentimentos", async (HttpRequest request, ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);
    int emp = int.Parse(user.FindFirst("empresa")!.Value);

    var form = await request.ReadFormAsync();
    string tipo = form["tipo"].ToString().Trim();
    string versao = form["versao_texto"].ToString().Trim();
    if (tipo == "" || versao == "") return Results.BadRequest(new { mensagem = "Tipo e versÃ£o sÃ£o obrigatÃ³rios." });

    // aceito: opcional, default true. Se false, revoga o consentimento
    bool aceito = true;
    var aceitoStr = form["aceito"].ToString().Trim().ToLower();
    if (aceitoStr == "false" || aceitoStr == "0") aceito = false;

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var ip = request.HttpContext.Connection.RemoteIpAddress;

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO consentimentos (empresa_id, paciente_id, tipo, versao_texto, aceito, ip)
        VALUES (@emp, @pid, @tipo, @versao, @aceito, @ip)
        ON CONFLICT (empresa_id, paciente_id, tipo, versao_texto) DO UPDATE SET aceito = @aceito, ip = @ip, criado_em = CURRENT_TIMESTAMP;", conn);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("pid", pid);
    cmd.Parameters.AddWithValue("tipo", tipo);
    cmd.Parameters.AddWithValue("versao", versao);
    cmd.Parameters.AddWithValue("aceito", aceito);
    cmd.Parameters.AddWithValue("ip", (object?)ip ?? DBNull.Value);
    await cmd.ExecuteNonQueryAsync();

    return Results.Ok(new { mensagem = aceito ? "Consentimento registrado." : "Consentimento revogado." });
});

// GET /api/paciente/consentimentos - Lista consentimentos do paciente
app.MapGet("/api/paciente/consentimentos", async (ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var lista = new List<object>();
    using var cmd = new NpgsqlCommand(@"
        SELECT tipo, versao_texto, aceito, criado_em, ip
        FROM consentimentos
        WHERE paciente_id = @p
        ORDER BY tipo, criado_em DESC;", conn);
    cmd.Parameters.AddWithValue("p", pid);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        lista.Add(new
        {
            tipo = rd.GetString(0),
            versao_texto = rd.GetString(1),
            aceito = rd.GetBoolean(2),
            criado_em = rd.GetDateTime(3),
            ip = rd.IsDBNull(4) ? null : rd.GetFieldValue<System.Net.IPAddress>(4).ToString()
        });
    }
    return Results.Ok(lista);
});

// GET /api/paciente/ultimos-valores - Ãšltimos valores do paciente (peso, altura, % gordura) para prÃ©-preenchimento
app.MapGet("/api/paciente/ultimos-valores", async (ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);
    var dados = await BuscarUltimosValoresPaciente(pid);
    return Results.Ok(dados);
});

// =====================================================================
// LGPD - DIREITOS DO TITULAR (Paciente)
// =====================================================================

// GET /api/paciente/meus-dados - Exporta todos os dados do paciente (Art. 18 LGPD)
app.MapGet("/api/paciente/meus-dados", async (ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);
    int emp = int.Parse(user.FindFirst("empresa")!.Value);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var result = new Dictionary<string, object>();

    // 1. Dados cadastrais
    using (var cmd = new NpgsqlCommand("SELECT * FROM pacientes WHERE id = @p AND empresa_id = @e", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        cmd.Parameters.AddWithValue("e", emp);
        using var rd = await cmd.ExecuteReaderAsync();
        if (await rd.ReadAsync())
        {
            var paciente = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                paciente[rd.GetName(i)] = rd.GetValue(i);
            result["cadastro"] = paciente;
        }
    }

    // 2. Anamnese
    using (var cmd = new NpgsqlCommand("SELECT * FROM anamneses WHERE paciente_id = @p ORDER BY criado_em DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["anamnese"] = list;
    }

    // 3. Exames
    using (var cmd = new NpgsqlCommand("SELECT id, titulo_exame, data_exame, observacao, enviado_por, criado_em FROM exames_paciente WHERE paciente_id = @p ORDER BY data_exame DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["exames"] = list;
    }

    // 4. Metas
    using (var cmd = new NpgsqlCommand(@"
        SELECT m.*, json_agg(ma ORDER BY ma.data_acompanhamento DESC) AS acompanhamentos
        FROM metas_paciente m
        LEFT JOIN metas_acompanhamento ma ON ma.meta_id = m.id
        WHERE m.paciente_id = @p
        GROUP BY m.id
        ORDER BY m.criado_em DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["metas"] = list;
    }

    // 5. Check-ins
    using (var cmd = new NpgsqlCommand("SELECT * FROM checkins_semanais WHERE paciente_id = @p ORDER BY data_checkin DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["checkins"] = list;
    }

    // 6. EvoluÃ§Ã£o (registros antropomÃ©tricos)
    using (var cmd = new NpgsqlCommand("SELECT * FROM evolucao_registros WHERE paciente_id = @p ORDER BY data_registro DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["evolucao"] = list;
    }

    // 7. Planos alimentares
    using (var cmd = new NpgsqlCommand(@"
        SELECT p.*, json_agg(r ORDER BY r.ordem) AS refeicoes
        FROM planos_alimentares p
        LEFT JOIN plano_refeicoes r ON r.plano_id = p.id
        WHERE p.paciente_id = @p
        GROUP BY p.id
        ORDER BY p.criado_em DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["planos"] = list;
    }

    // 8. OrientaÃ§Ãµes nutricionais
    using (var cmd = new NpgsqlCommand("SELECT * FROM orientacoes_nutricionais WHERE paciente_id = @p ORDER BY criado_em DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["orientacoes"] = list;
    }

    // 9. ObservaÃ§Ãµes
    using (var cmd = new NpgsqlCommand("SELECT * FROM observacoes_paciente WHERE paciente_id = @p ORDER BY criado_em DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["observacoes"] = list;
    }

    // 10. Consentimentos
    using (var cmd = new NpgsqlCommand("SELECT tipo, versao_texto, aceito, criado_em, ip FROM consentimentos WHERE paciente_id = @p ORDER BY tipo, criado_em DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["consentimentos"] = list;
    }

    // 11. SolicitaÃ§Ãµes LGPD
    using (var cmd = new NpgsqlCommand("SELECT * FROM solicitacoes_lgpd WHERE paciente_id = @p ORDER BY criado_em DESC", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["solicitacoes"] = list;
    }

    // 12. Acessos (auditoria)
    using (var cmd = new NpgsqlCommand("SELECT * FROM acessos_paciente WHERE paciente_id = @p ORDER BY criado_em DESC LIMIT 100", conn))
    {
        cmd.Parameters.AddWithValue("p", pid);
        using var rd = await cmd.ExecuteReaderAsync();
        var list = new List<object>();
        while (await rd.ReadAsync())
        {
            var item = new Dictionary<string, object>();
            for (int i = 0; i < rd.FieldCount; i++)
                item[rd.GetName(i)] = rd.GetValue(i);
            list.Add(item);
        }
        result["acessos"] = list;
    }

    result["exportado_em"] = DateTimeOffset.UtcNow;
    result["versao"] = "1.0";

    return Results.Ok(result);
});

// POST /api/paciente/solicitacoes - Cria solicitaÃ§Ã£o LGPD (correcao, exclusao, informacao)
app.MapPost("/api/paciente/solicitacoes", async (HttpRequest request, ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);
    int emp = int.Parse(user.FindFirst("empresa")!.Value);

    var form = await request.ReadFormAsync();
    string tipo = form["tipo"].ToString().Trim().ToLower();
    string descricao = form["descricao"].ToString().Trim();

    if (!new[] { "correcao", "exclusao", "informacao" }.Contains(tipo))
        return Results.BadRequest(new { mensagem = "Tipo invÃ¡lido. Use: correcao, exclusao ou informacao." });
    if (descricao.Length < 10)
        return Results.BadRequest(new { mensagem = "DescriÃ§Ã£o muito curta (mÃ­n. 10 caracteres)." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO solicitacoes_lgpd (empresa_id, paciente_id, tipo, descricao)
        VALUES (@emp, @pid, @tipo, @desc)
        RETURNING id, criado_em;", conn);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("pid", pid);
    cmd.Parameters.AddWithValue("tipo", tipo);
    cmd.Parameters.AddWithValue("desc", descricao);

    using var rd = await cmd.ExecuteReaderAsync();
    await rd.ReadAsync();
    var id = rd.GetInt64(0);
    var criado = rd.GetDateTime(1);

    return Results.Ok(new { id, tipo, descricao, status = "pendente", criado_em = criado });
});

// GET /api/paciente/solicitacoes - Lista solicitaÃ§Ãµes do paciente
app.MapGet("/api/paciente/solicitacoes", async (ClaimsPrincipal user) =>
{
    int pid = int.Parse(user.FindFirst("pid")!.Value);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var lista = new List<object>();
    using var cmd = new NpgsqlCommand("SELECT id, tipo, descricao, status, criado_em, resolvido_em FROM solicitacoes_lgpd WHERE paciente_id = @p ORDER BY criado_em DESC", conn);
    cmd.Parameters.AddWithValue("p", pid);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        lista.Add(new
        {
            id = rd.GetInt64(0),
            tipo = rd.GetString(1),
            descricao = rd.GetString(2),
            status = rd.GetString(3),
            criado_em = rd.GetDateTime(4),
            resolvido_em = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5)
        });
    }
    return Results.Ok(lista);
});

// =====================================================================
// LGPD - EQUIPE (GestÃ£o de solicitaÃ§Ãµes da empresa)
// =====================================================================

// GET /api/empresa/solicitacoes - Lista solicitaÃ§Ãµes da empresa (equipe)
app.MapGet("/api/empresa/solicitacoes", async (ClaimsPrincipal user) =>
{
    int emp = int.Parse(user.FindFirst("empresa")!.Value);
    string perfil = user.FindFirst("perfil")?.Value ?? "";

    // Apenas admin e super_admin podem ver (profissional NÃƒO tem acesso ao LGPD)
    if (!new[] { "admin", "super_admin" }.Contains(perfil))
        return Results.Forbid();

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var lista = new List<object>();
    using var cmd = new NpgsqlCommand(@"
        SELECT s.id, s.paciente_id, p.nome AS paciente_nome, s.tipo, s.descricao, s.status, s.criado_em, s.resolvido_em
        FROM solicitacoes_lgpd s
        JOIN pacientes p ON p.id = s.paciente_id
        WHERE s.empresa_id = @e
        ORDER BY
            CASE s.status WHEN 'pendente' THEN 0 WHEN 'em_analise' THEN 1 WHEN 'atendida' THEN 2 ELSE 3 END,
            s.criado_em DESC", conn);
    cmd.Parameters.AddWithValue("e", emp);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
    {
        lista.Add(new
        {
            id = rd.GetInt64(0),
            paciente_id = rd.GetInt64(1),
            paciente_nome = rd.GetString(2),
            tipo = rd.GetString(3),
            descricao = rd.GetString(4),
            status = rd.GetString(5),
            criado_em = rd.GetDateTime(6),
            resolvido_em = rd.IsDBNull(7) ? (DateTime?)null : rd.GetDateTime(7)
        });
    }
    return Results.Ok(lista);
});

// PATCH /api/empresa/solicitacoes/{id}/atender - Marca solicitaÃ§Ã£o como atendida
app.MapPatch("/api/empresa/solicitacoes/{id:long}/atender", async (long id, HttpRequest request, ClaimsPrincipal user) =>
{
    int emp = int.Parse(user.FindFirst("empresa")!.Value);
    string perfil = user.FindFirst("perfil")?.Value ?? "";
    int uid = int.Parse(user.FindFirst("uid")!.Value);

    // Apenas admin e super_admin podem atender (profissional NÃƒO tem acesso ao LGPD)
    if (!new[] { "admin", "super_admin" }.Contains(perfil))
        return Results.Forbid();

    var form = await request.ReadFormAsync();
    string novoStatus = form["status"].ToString().Trim().ToLower();
    if (!new[] { "em_analise", "atendida", "recusada" }.Contains(novoStatus))
        return Results.BadRequest(new { mensagem = "Status invÃ¡lido." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    using var cmd = new NpgsqlCommand(@"
        UPDATE solicitacoes_lgpd
        SET status = @status, resolvido_em = CASE WHEN @status IN ('atendida','recusada') THEN CURRENT_TIMESTAMP ELSE NULL END
        WHERE id = @id AND empresa_id = @emp
        RETURNING id;", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("emp", emp);
    cmd.Parameters.AddWithValue("status", novoStatus);

    var result = await cmd.ExecuteScalarAsync();
    if (result == null) return Results.NotFound(new { mensagem = "SolicitaÃ§Ã£o nÃ£o encontrada." });

    return Results.Ok(new { id, status = novoStatus });
});

// =====================================================================
// HISTÃ“RICO UNIFICADO (linha do tempo do paciente)
// =====================================================================
app.MapGet("/api/historico/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    int emp = Auth.Empresa(user);
    // Validar se paciente pertence Ã  empresa (defesa em profundidade)
    if (!await TenantValidation.ValidarPacienteEmpresaAsync(connString, pacienteId, emp))
        return Results.NotFound(new { mensagem = "Paciente nÃ£o encontrado." });

    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT * FROM (
            SELECT 'plano' AS tipo, criado_em::date AS data, criado_em AS ordem,
                   'VersÃ£o ' || numero || ' do plano' AS titulo,
                   COALESCE(descricao, '') AS detalhe, '' AS extra
            FROM planos_versoes WHERE paciente_id = @p

            UNION ALL
            SELECT 'evolucao', data_registro, criado_em, 'Registro de evoluÃ§Ã£o',
                   COALESCE(observacao, ''),
                   CASE WHEN peso_kg IS NULL THEN '' ELSE replace(peso_kg::text, '.', ',') || ' kg' END
            FROM evolucao_registros WHERE paciente_id = @p

            UNION ALL
            SELECT 'checkin', data_checkin, criado_em, 'Check-in semanal',
                   COALESCE(observacoes_paciente, ''),
                   replace(peso_atual_kg::text, '.', ',') || ' kg Â· adesÃ£o ' || COALESCE(adesao_plano_porcentagem, 0) || '%'
            FROM checkins_semanais WHERE paciente_id = @p

            UNION ALL
            SELECT CASE WHEN tipo = 'semana' THEN 'obs_semana' ELSE 'obs_geral' END,
                   data_ref, criado_em,
                   CASE WHEN tipo = 'semana' THEN 'ObservaÃ§Ã£o da semana' ELSE 'ObservaÃ§Ã£o geral' END,
                   texto, ''
            FROM observacoes_paciente WHERE paciente_id = @p

            UNION ALL
            SELECT 'meta_acomp', a.data_acompanhamento, a.criado_em, m.titulo,
                   COALESCE(a.observacao, ''), a.resultado
            FROM metas_acompanhamento a JOIN metas_paciente m ON m.id = a.meta_id
            WHERE m.paciente_id = @p

            UNION ALL
            SELECT 'meta_nova', criado_em::date, criado_em, 'Nova meta: ' || titulo,
                   COALESCE(descricao, ''), ''
            FROM metas_paciente WHERE paciente_id = @p

            UNION ALL
            SELECT 'exame', data_exame, criado_em, titulo_exame, COALESCE(observacao, ''), ''
            FROM exames_paciente WHERE paciente_id = @p
        ) h
        ORDER BY data DESC, ordem DESC
        LIMIT 500;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
        lista.Add(new
        {
            tipo = rd.GetString(0),
            data = rd.GetDateTime(1).ToString("yyyy-MM-dd"),
            titulo = rd.GetString(3),
            detalhe = rd.GetString(4),
            extra = rd.GetString(5)
        });
    return Results.Ok(lista);
}).AddEndpointFilter(new TenantIsolationFilter(connString));

// ===========================EXECUÃ‡ÃƒO DA APLICAÃ‡ÃƒO=========================================
app.Run();

// =====================================================================
// TYPE DECLARATIONS (moved to end for top-level statements)
// =====================================================================

record SolicitarAcessoInput(string? Nome, string? Email, string? Telefone, string? RegistroProfissional, string? ClinicaNome, string? Mensagem);
record AprovarAcessoInput(string? NomeProfissional); // opcional: sobrescrever nome do profissional

// =====================================================================
// RATE LIMITING EM MEMÃ“RIA (para /api/acesso/solicitar)
// =====================================================================
static class RateLimiter
{
    private static readonly Dictionary<string, (int count, DateTime windowStart)> _requests = new();
    private static readonly object _lock = new();
    private const int MaxRequests = 5;
    private static readonly TimeSpan Window = TimeSpan.FromHours(1);

    public static bool TryConsume(string ip)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (_requests.TryGetValue(ip, out var entry))
            {
                if (now - entry.windowStart > Window)
                {
                    _requests[ip] = (1, now);
                    return true;
                }
                if (entry.count >= MaxRequests)
                    return false;
                _requests[ip] = (entry.count + 1, entry.windowStart);
                return true;
            }
            _requests[ip] = (1, now);
            return true;
        }
    }

    public static string GetClientIp(HttpContext ctx)
    {
        // UseForwardedHeaders jÃ¡ preenche RemoteIpAddress com o IP real do cliente (ForwardLimit=1)
        return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

record DefinirSenhaInput(string? Senha, string? ConfirmarSenha);

// ===============================================DTOs===============================================================

record PacienteLoginInput(string? Cpf, string? Nascimento);
record LoginInput(string? Email, string? Senha);
record UsuarioInput(string? Nome, string? Email, string? Senha, string? Perfil, string? NivelNutricao, bool? Ativo);
record ObservacaoInput(int PacienteId, string? Tipo, string? Data, string Texto);
record MetaInput(int PacienteId, string Titulo, string? Descricao, string? Tipo, string? Prazo, string? Status);
record AcompanhamentoInput(int MetaId, string Resultado, string? Observacao, string? Data);
record OrientacaoDto(int PacienteId, string? Texto);
record AvaliacaoInput(int PacienteId, int Idade, string Sexo, double PesoKg, double AlturaCm, string NivelAtividade, string Objetivo, double PercentualGordura, double MassaMagraKg);
record CheckinInput(int PacienteId, double PesoAtualKg, int AdesaoPlanoPorcentagem, string? Observacoes);
record AnamneseInput(int PacienteId, string? HistoricoMedico, string? Alergias, string? Intolerancias, string? RotinaDiaria, string? HistoricoFamiliar, object? DadosExtendidos);
record RefeicaoInput(int PacienteId, string NomeRefeicao, string? Horario, string? Observacoes);
record ItemInput(
    int RefeicaoId, int Opcao, int? SubstituiItemId, string? AlimentoNome,
    double QuantidadeG, string? MedidaCaseira,
    double Kcal, double Proteinas, double Carbos, double Gorduras);

// Entrada para cÃ¡lculo automÃ¡tico via Python service (TACO)
record ItemCalcularInput(
    int RefeicaoId, int Opcao, int? SubstituiItemId,
    string AlimentoId,           // ID do alimento na TACO (ex: "175" = banana)
    string? Refeicao,            // "cafe_manha", "almoco", "jantar", "lanche", "pre_treino", "pos_treino", "ceia"
    string? Observacao,
    // Um destes targets (prioridade: kcal > proteina > carbo > gordura)
    double? TargetKcal,
    double? TargetProteina,
    double? TargetCarbo,
    double? TargetGordura,
    // Ou gramas fixas (ignora targets se fornecido)
    double? Gramas,
    // Ou medida caseira (ex: "2 colheres de sopa")
    string? MedidaCaseira,
    double? QuantidadeMedida,
    string? ObservacaoMedida);

record VersaoInput(string? Descricao);
record EmpresaInput(string? Nome);
record ConviteInput(string? Email, string? Perfil, string? NivelNutricao);
record AceitarConviteInput(string? Token, string? Senha, string? Nome);
record ConvitePacienteInput(string? EmailPaciente);
record AceitarConvitePacienteInput(string? Token, string? Senha, string? Nome, string? Cpf, DateTime? DataNascimento, string? Telefone);
record OnboardingInput(string EmpresaNome, string AdminNome, string AdminEmail, string AdminSenha);
record MedidaCaseiraInput(string Medida, decimal GramasPorUnidade, string? Observacao = null);
// =====================================================================
// MONTA O PLANO COMPLETO (refeiÃ§Ãµes â†’ itens)
// =====================================================================
// =====================================================================
// MONTA O PLANO COMPLETO (refeiÃ§Ãµes â†’ itens)
// =====================================================================
static class PlanoHelper
{
    public static async Task<Dictionary<string, object?>> Montar(string connString, int pacienteId, int empresaId)
    {
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        int? planoId = null;
        using (var cmdP = new NpgsqlCommand("SELECT id FROM planos_alimentares WHERE paciente_id = @p AND empresa_id = @emp LIMIT 1;", conn))
        {
            cmdP.Parameters.AddWithValue("p", pacienteId);
            cmdP.Parameters.AddWithValue("emp", empresaId);
            var r = await cmdP.ExecuteScalarAsync();
            if (r != null) planoId = Convert.ToInt32(r);
        }

        var refeicoes = new List<Dictionary<string, object?>>();

        if (planoId != null)
        {
            var porRefeicao = new Dictionary<int, List<Dictionary<string, object?>>>();

            using (var cmdR = new NpgsqlCommand(@"
                SELECT id, nome_refeicao, horario, COALESCE(observacoes, '')
                FROM refeicoes WHERE plano_id = @pl ORDER BY horario, id;", conn))
            {
                cmdR.Parameters.AddWithValue("pl", planoId.Value);
                using var rd = await cmdR.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    var itens = new List<Dictionary<string, object?>>();
                    int id = rd.GetInt32(0);
                    porRefeicao[id] = itens;
                    refeicoes.Add(new()
                    {
                        ["id"] = id,
                        ["nome"] = rd.IsDBNull(1) ? "RefeiÃ§Ã£o" : rd.GetString(1),
                        ["horario"] = rd.IsDBNull(2) ? "08:00" : rd.GetTimeSpan(2).ToString(@"hh\:mm"),
                        ["observacoes"] = rd.GetString(3),
                        ["itens"] = itens
                    });
                }
            }

            using (var cmdI = new NpgsqlCommand(@"
                SELECT i.id, i.refeicao_id, i.opcao, i.substitui_item_id,
                       COALESCE(i.alimento_nome_livre, a.nome, 'Alimento'),
                       COALESCE(i.quantidade_g, 0),
                       COALESCE(i.medida_caseira, ''),
                       COALESCE(NULLIF(i.kcal_manual, 0),      ROUND((a.calorias_100g     * i.quantidade_g / 100)::numeric, 1), 0),
                       COALESCE(NULLIF(i.proteinas_manual, 0), ROUND((a.proteinas_100g    * i.quantidade_g / 100)::numeric, 1), 0),
                       COALESCE(NULLIF(i.carbos_manual, 0),    ROUND((a.carboidratos_100g * i.quantidade_g / 100)::numeric, 1), 0),
                       COALESCE(NULLIF(i.gorduras_manual, 0),  ROUND((a.gorduras_100g     * i.quantidade_g / 100)::numeric, 1), 0)
                FROM itens_refeicao i
                JOIN refeicoes r ON r.id = i.refeicao_id
                LEFT JOIN alimentos a ON a.id = i.alimento_id
                WHERE r.plano_id = @pl
                ORDER BY i.opcao, i.id;", conn))
            {
                cmdI.Parameters.AddWithValue("pl", planoId.Value);
                using var rd = await cmdI.ExecuteReaderAsync();
                while (await rd.ReadAsync())
                {
                    int refId = rd.GetInt32(1);
                    if (!porRefeicao.TryGetValue(refId, out var lista)) continue;

                    lista.Add(new()
                    {
                        ["id"] = rd.GetInt32(0),
                        ["refeicaoId"] = refId,
                        ["opcao"] = rd.GetInt32(2),
                        ["substituiItemId"] = rd.IsDBNull(3) ? (int?)null : rd.GetInt32(3),
                        ["alimento"] = rd.GetString(4),
                        ["quantidadeG"] = Convert.ToDouble(rd.GetValue(5)),
                        ["medidaCaseira"] = rd.GetString(6),
                        ["kcal"] = Convert.ToDouble(rd.GetValue(7)),
                        ["proteinas"] = Convert.ToDouble(rd.GetValue(8)),
                        ["carbos"] = Convert.ToDouble(rd.GetValue(9)),
                        ["gorduras"] = Convert.ToDouble(rd.GetValue(10))
                    });
                }
            }
        }

        return new Dictionary<string, object?>
        {
            ["planoId"] = planoId,
            ["refeicoes"] = refeicoes
        };
    }
}

// =====================================================================
// AUXILIARES: CAMPOS DA EVOLUÃ‡ÃƒO
// =====================================================================
class EvolucaoCampos
{
    public DateTime Data;
    public decimal? Peso, Gordura, Cintura, Quadril, Abdomen, Braco, Coxa;
    public string Obs = "";

    public bool Vazio =>
        !(Peso.HasValue || Gordura.HasValue || Cintura.HasValue || Quadril.HasValue ||
          Abdomen.HasValue || Braco.HasValue || Coxa.HasValue) && string.IsNullOrWhiteSpace(Obs);

    public static DateTime LerData(string? s) =>
        DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None, out var d) ? d.Date : DateTime.Today;

    public static EvolucaoCampos Ler(IFormCollection f) => new()
    {
        Data = LerData(f["data"]),
        Peso = ArquivoHelper.Dec(f["peso"]),
        Gordura = ArquivoHelper.Dec(f["gordura"]),
        Cintura = ArquivoHelper.Dec(f["cintura"]),
        Quadril = ArquivoHelper.Dec(f["quadril"]),
        Abdomen = ArquivoHelper.Dec(f["abdomen"]),
        Braco = ArquivoHelper.Dec(f["braco"]),
        Coxa = ArquivoHelper.Dec(f["coxa"]),
        Obs = f["observacao"].ToString()
    };
}

// =====================================================================
// AUXILIARES: ARQUIVOS (fotos e exames)
// =====================================================================
static class ArquivoHelper
{
    const long LimiteBytes = 10 * 1024 * 1024; // 10 MB por arquivo

    static readonly HashSet<string> Imagens = new() { "image/jpeg", "image/png", "image/webp" };

    public static decimal? Dec(string? s) =>
        decimal.TryParse(s?.Trim().Replace(',', '.'),
            System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var v) ? v : null;

    public static double? N(NpgsqlDataReader rd, int i) =>
        rd.IsDBNull(i) ? null : Convert.ToDouble(rd.GetValue(i));

    public static NpgsqlParameter Num(string nome, decimal? v) =>
        new(nome, NpgsqlTypes.NpgsqlDbType.Numeric) { Value = v.HasValue ? v.Value : DBNull.Value };

    public static string? Validar(IFormFileCollection files, bool somenteImagem)
    {
        foreach (var f in files)
        {
            if (f.Length == 0) return $"O arquivo \"{f.FileName}\" estÃ¡ vazio.";
            if (f.Length > LimiteBytes) return $"O arquivo \"{f.FileName}\" passa de 10 MB.";
            var tipo = (f.ContentType ?? "").ToLowerInvariant();
            bool ok = Imagens.Contains(tipo) || (!somenteImagem && tipo == "application/pdf");
            if (!ok)
                return somenteImagem
                    ? $"\"{f.FileName}\": envie apenas imagens (JPG, PNG ou WEBP)."
                    : $"\"{f.FileName}\": envie PDF, JPG, PNG ou WEBP.";
        }
        return null;
    }

    public static async Task Salvar(NpgsqlConnection conn, NpgsqlTransaction tx,
        int pacienteId, string origem, int origemId, IFormFileCollection files)
    {
        foreach (var f in files)
        {
            using var ms = new MemoryStream();
            await f.CopyToAsync(ms);

            using var cmd = new NpgsqlCommand(@"
                INSERT INTO arquivos_paciente (paciente_id, empresa_id, origem, origem_id, nome, mime, tamanho, dados)
                VALUES (@p, (SELECT empresa_id FROM pacientes WHERE id = @p), @o, @oid, @nome, @mime, @tam, @dados);", conn, tx);
            cmd.Parameters.AddWithValue("p", pacienteId);
            cmd.Parameters.AddWithValue("o", origem);
            cmd.Parameters.AddWithValue("oid", origemId);
            cmd.Parameters.AddWithValue("nome", Path.GetFileName(f.FileName));
            cmd.Parameters.AddWithValue("mime", f.ContentType!.ToLowerInvariant());
            cmd.Parameters.AddWithValue("tam", (int)f.Length);
            cmd.Parameters.Add(new NpgsqlParameter("dados", NpgsqlTypes.NpgsqlDbType.Bytea) { Value = ms.ToArray() });
            await cmd.ExecuteNonQueryAsync();
        }
    }

    // Lista os arquivos (sem o conteÃºdo) agrupados pelo registro de origem
    public static async Task<Dictionary<int, List<object>>> Listar(NpgsqlConnection conn, int pacienteId, string origem)
    {
        var mapa = new Dictionary<int, List<object>>();
        using var cmd = new NpgsqlCommand(@"
            SELECT id, origem_id, nome, mime FROM arquivos_paciente
            WHERE paciente_id = @p AND origem = @o ORDER BY id;", conn);
        cmd.Parameters.AddWithValue("p", pacienteId);
        cmd.Parameters.AddWithValue("o", origem);
        using var rd = await cmd.ExecuteReaderAsync();
        while (await rd.ReadAsync())
        {
            int oid = rd.GetInt32(1);
            if (!mapa.TryGetValue(oid, out var lista)) mapa[oid] = lista = new List<object>();
            lista.Add(new { id = rd.GetInt32(0), nome = rd.GetString(2), mime = rd.GetString(3) });
        }
        return mapa;
    }
}

static class Auth
{
    public static int Peso(string? n) => n switch { "leitura" => 1, "edicao" => 2, "total" => 3, _ => 0 };
    public static int Empresa(ClaimsPrincipal u) => int.Parse(u.FindFirst("empresa")!.Value);
    public static int UsuarioId(ClaimsPrincipal u) => int.Parse(u.FindFirst("uid")!.Value);
    public static string? Nome(ClaimsPrincipal u) => u.FindFirst("nome")?.Value;
    public static bool EhSuperAdmin(ClaimsPrincipal u) => u.FindFirst("perfil")?.Value == "super_admin";

    public static string Gerar(string key, int uid, int empresa, string perfil, Dictionary<string, string> modulos)
    {
        var claims = new List<Claim>
        {
            new("uid", uid.ToString()), new("empresa", empresa.ToString()),
            new("perfil", perfil), new("tipo", "equipe")
        };
        foreach (var (m, n) in modulos) claims.Add(new Claim($"mod_{m}", n));

        var cred = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("bybarhub", "bybarhub-app", claims,
            expires: DateTime.UtcNow.AddHours(8), signingCredentials: cred);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
     public static string GerarPaciente(string key, int pid, int empresa)
    {
        var claims = new List<Claim> { new("pid", pid.ToString()), new("empresa", empresa.ToString()), new("tipo", "paciente") };
        var cred = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken("bybarhub", "bybarhub-app", claims,
            expires: DateTime.UtcNow.AddHours(2), signingCredentials: cred);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// =====================================================================
// PLANO ENFORCEMENT - Verifica limites de assinatura
// =====================================================================
static class PlanoEnforcement
{
    // Verifica se a empresa tem assinatura ativa e retorna os limites
    // Retorna null se nÃ£o tem assinatura ou estÃ¡ cancelada/past_due
    // Para "sem_plano", retorna limites do catÃ¡logo (max 5 pacientes)
    public static async Task<(bool temAcesso, PlanoLimites limites, bool bypassLimites, string? erro)> VerificarAcessoAsync(
        NpgsqlDataSource db, int empresaId, string acaoRequerida)
    {
        // Buscar assinatura ativa (inclui bypass_limites)
        await using var cmd = db.CreateCommand(@"
            SELECT status, plano, limites, COALESCE(bypass_limites, false)
            FROM assinaturas
            WHERE empresa_id = @emp
            ORDER BY id DESC
            LIMIT 1;");
        cmd.Parameters.AddWithValue("emp", empresaId);
        await using var rd = await cmd.ExecuteReaderAsync();

        if (!await rd.ReadAsync())
        {
            // Sem assinatura = sem_plano (plano gratuito com limites)
            var semPlanoLimites = Planos.ObterLimites("sem_plano");
            return (true, semPlanoLimites, false, null);
        }

        var status = rd.GetString(0);
        var plano = rd.GetString(1);
        var limitesJson = rd.IsDBNull(2) ? null : rd.GetString(2);
        var bypassLimites = rd.GetBoolean(3);

        // Se status Ã© cancelada, past_due, inativa -> restriÃ§Ãµes (sem_plano Ã© plano vÃ¡lido com limites)
        if (status == "cancelada" || status == "past_due" || status == "inativa")
        {
            var semPlanoLimites = Planos.ObterLimites("sem_plano");
            return (false, semPlanoLimites, false, "plano_necessario");
        }

        // Tem assinatura ativa - desserializar limites
        var limites = string.IsNullOrEmpty(limitesJson)
            ? Planos.Limites.GetValueOrDefault(plano)
            : ACNutri.Billing.PlanoLimites.FromJson(limitesJson);

        // Verificar aÃ§Ã£o especÃ­fica
        if (acaoRequerida == "pdf_export" && status != "ativa")
        {
            return (false, limites, bypassLimites, "plano_necessario");
        }

        return (true, limites, bypassLimites, null);
    }

    // Verifica limite de pacientes para a empresa
    // Super Admin sempre pode criar (bypass total)
    // Se assinatura tem bypass_limites = true, tambÃ©m permite (ilimitado)
    public static async Task<(bool podeCriar, int maxPacientes, int atual)> VerificarLimitePacientesAsync(
        NpgsqlDataSource db, int empresaId, ClaimsPrincipal? user = null)
    {
        // Super Admin bypass total
        if (user != null && Auth.EhSuperAdmin(user))
        {
            // Contar pacientes atuais apenas para informaÃ§Ã£o
            await using var countCmd = db.CreateCommand(
                "SELECT COUNT(*) FROM pacientes WHERE empresa_id = @emp AND COALESCE(status, 'ativo') = 'ativo';");
            countCmd.Parameters.AddWithValue("emp", empresaId);
            var atual = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            return (true, int.MaxValue, atual);
        }

        var (temAcesso, limites, bypassLimites, _) = await VerificarAcessoAsync(db, empresaId, "pacientes");
        if (!temAcesso) return (false, limites.MaxPacientes, 0);

        // Se tem bypass_limites na assinatura, permite ilimitado
        if (bypassLimites)
        {
            await using var countCmd = db.CreateCommand(
                "SELECT COUNT(*) FROM pacientes WHERE empresa_id = @emp AND COALESCE(status, 'ativo') = 'ativo';");
            countCmd.Parameters.AddWithValue("emp", empresaId);
            var atual = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            return (true, int.MaxValue, atual);
        }

        await using var cmd = db.CreateCommand(
            "SELECT COUNT(*) FROM pacientes WHERE empresa_id = @emp AND COALESCE(status, 'ativo') = 'ativo';");
        cmd.Parameters.AddWithValue("emp", empresaId);
        var atualNormal = Convert.ToInt32(await cmd.ExecuteScalarAsync());

        return (atualNormal < limites.MaxPacientes, limites.MaxPacientes, atualNormal);
    }
}

// Cria o primeiro super-admin (sÃ³ se nÃ£o existir nenhum usuÃ¡rio)
static class Seed
{
    public static async Task Garantir(string connString, IConfiguration cfg)
    {
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        // SEMPRE garante schema base e migraÃ§Ãµes (idempotente)
        foreach (var sql in new[]
        {
            "INSERT INTO perfis (chave, nome) VALUES ('super_admin','Super Administrador') ON CONFLICT DO NOTHING",
            "INSERT INTO perfis (chave, nome) VALUES ('admin','Administrador') ON CONFLICT DO NOTHING",
            "INSERT INTO perfis (chave, nome) VALUES ('profissional','Profissional') ON CONFLICT DO NOTHING",
            "INSERT INTO perfis (chave, nome) VALUES ('atendente','Atendente') ON CONFLICT DO NOTHING",
            "INSERT INTO modulos (chave, nome) VALUES ('nutricao','NutriÃ§Ã£o') ON CONFLICT DO NOTHING",
            "INSERT INTO empresas (nome) SELECT 'Empresa Principal' WHERE NOT EXISTS (SELECT 1 FROM empresas)",
            "INSERT INTO empresa_modulos (empresa_id, modulo) SELECT MIN(id), 'nutricao' FROM empresas ON CONFLICT DO NOTHING",
            // MigraÃ§Ã£o: adicionar coluna dados_extendidos na tabela anamneses (se nÃ£o existir)
            @"ALTER TABLE anamneses ADD COLUMN IF NOT EXISTS dados_extendidos JSONB DEFAULT '{}'::jsonb;",
            // Ãndice para JSONB
            @"CREATE INDEX IF NOT EXISTS idx_anamneses_dados_extendidos ON anamneses USING GIN (dados_extendidos);",
            // MigraÃ§Ã£o: adicionar coluna bypass_limites na tabela assinaturas (se nÃ£o existir)
            @"ALTER TABLE assinaturas ADD COLUMN IF NOT EXISTS bypass_limites boolean NOT NULL DEFAULT false;",
            // Tabela para rate limiting por identificador+IP (hash do identificador)
            @"CREATE TABLE IF NOT EXISTS tentativas_login (
                id BIGSERIAL PRIMARY KEY,
                tipo VARCHAR(50) NOT NULL,
                identificador_hash CHAR(64) NOT NULL,
                ip INET NOT NULL,
                sucesso BOOLEAN NOT NULL DEFAULT false,
                criado_em TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            );",
            @"CREATE INDEX IF NOT EXISTS ix_tentativas_login_tipo_hash_ip_criado
                ON tentativas_login (tipo, identificador_hash, ip, criado_em DESC)
                WHERE sucesso = false;",
            @"CREATE INDEX IF NOT EXISTS ix_tentativas_login_criado_em
                ON tentativas_login (criado_em);"
        })
        {
            using var c = new NpgsqlCommand(sql, conn);
            await c.ExecuteNonQueryAsync();
        }

        // SÃ³ cria super-admin se nÃ£o existir nenhum usuÃ¡rio
        using (var c = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios", conn))
            if (Convert.ToInt32(await c.ExecuteScalarAsync()) > 0) return;

        // Super-admin config (novas chaves para multi-tenant)
        var email = cfg["Seed:SuperAdminEmail"]?.Trim().ToLowerInvariant();
        var senha = cfg["Seed:SuperAdminSenha"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha) || senha.Length < 8)
        {
            Console.WriteLine("!!! Nenhum usuÃ¡rio cadastrado. Defina Seed:SuperAdminEmail e Seed:SuperAdminSenha (mÃ­n. 8) nos user-secrets.");
            return;
        }

        // Cria super-admin (empresa_id = 1, mas perfil super_admin tem acesso total via middleware)
        using var ins = new NpgsqlCommand(@"
            INSERT INTO usuarios (empresa_id, nome, email, senha_hash, perfil)
            VALUES ((SELECT MIN(id) FROM empresas), 'Super Admin', @e, @h, 'super_admin');", conn);
        ins.Parameters.AddWithValue("e", email);
        ins.Parameters.AddWithValue("h", BCrypt.Net.BCrypt.HashPassword(senha));
        await ins.ExecuteNonQueryAsync();
        Console.WriteLine($">>> Super Admin criado: {email}");
    }
}
// Descobre a que paciente cada requisiÃ§Ã£o pertence e confere a empresa
static class Escopo
{
    static readonly Dictionary<string, string> Sql = new()
    {
        ["paciente"]  = "SELECT empresa_id FROM pacientes WHERE id = @v",
        ["refeicao"]   = "SELECT empresa_id FROM refeicoes WHERE id = @v",
        ["item"]       = "SELECT empresa_id FROM itens_refeicao WHERE id = @v",
        ["versao"]     = "SELECT empresa_id FROM planos_versoes WHERE id = @v",
        ["meta"]       = "SELECT empresa_id FROM metas_paciente WHERE id = @v",
        ["acomp"]      = "SELECT empresa_id FROM metas_acompanhamento WHERE id = @v",
        ["arquivo"]    = "SELECT empresa_id FROM arquivos_paciente WHERE id = @v",
        ["evolucao"]   = "SELECT empresa_id FROM evolucao_registros WHERE id = @v",
        ["exame"]      = "SELECT empresa_id FROM exames_paciente WHERE id = @v",
        ["obs"]        = "SELECT empresa_id FROM observacoes_paciente WHERE id = @v",
        ["checkin"]    = "SELECT empresa_id FROM checkins_semanais WHERE id = @v",
        ["orientacao"] = "SELECT empresa_id FROM orientacoes_nutricionais WHERE id = @v"
    };

    public static async Task<List<(string kind, int id)>> Alvos(HttpContext ctx)
    {
        var alvos = new List<(string, int)>();
        var seg = (ctx.Request.Path.Value ?? "").Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (seg.Length < 2) return alvos;

        var m = ctx.Request.Method;
        bool get = HttpMethods.IsGet(m), post = HttpMethods.IsPost(m);
        int Num(int i) => i < seg.Length && int.TryParse(seg[i], out var v) ? v : 0;
        int id = Num(2);

        void Add(string k, int v) => alvos.Add((k, v));

        switch (seg[1])
        {
            case "checkins": case "anamnese": case "orientacoes": case "avaliacoes": case "historico":
                Add("paciente", get ? id : await Corpo(ctx, "pacienteId")); break;

            case "acessos":
                // GET /api/acessos (sem ID) - super_admin: lista solicitacoes_acesso, NÃƒO Ã© paciente
                // POST /api/acessos/{id}/aprovar|recusar - super_admin: id Ã© solicitaÃ§Ã£o, nÃ£o paciente
                // POST /api/acessos/{id}/reenviar-email - super_admin: id Ã© solicitaÃ§Ã£o, nÃ£o paciente
                // DELETE /api/acessos/{id} - super_admin: id Ã© solicitaÃ§Ã£o, nÃ£o paciente
                // GET /api/acessos/{pacienteId} - nutricionista/team: isola por paciente (acessos_paciente)
                if (get && seg.Length == 3 && int.TryParse(seg[2], out var pid))
                {
                    Add("paciente", pid); // GET /api/acessos/{pacienteId} - isolamento por empresa
                }
                break;

            case "evolucao": case "exames": case "observacoes":
                var k = seg[1] == "evolucao" ? "evolucao" : seg[1] == "exames" ? "exame" : "obs";
                if (get) Add("paciente", id);
                else if (post) Add("paciente", await Corpo(ctx, "pacienteId"));
                else Add(k, id);
                break;

            case "metas":
                if (seg.Length > 2 && seg[2] == "acompanhamento")
                {
                    if (post) Add("meta", await Corpo(ctx, "metaId"));
                    else Add("acomp", Num(3));
                }
                else if (get) Add("paciente", id);
                else if (post) Add("paciente", await Corpo(ctx, "pacienteId"));
                else Add("meta", id);
                break;

            case "arquivos":
                Add("arquivo", id); break;

            case "planos":
                if (seg.Length > 2 && seg[2] == "refeicao")
                {
                    if (post && seg.Length == 3) Add("paciente", await Corpo(ctx, "pacienteId"));
                    else Add("refeicao", Num(3));          // PUT/DELETE, opcao, duplicar
                }
                else if (seg.Length > 2 && seg[2] == "item")
                {
                    if (post && (seg.Length == 3 || (seg.Length == 4 && seg[3] == "calcular")))
                    {
                        Add("refeicao", await Corpo(ctx, "refeicaoId"));
                        int sub = await Corpo(ctx, "substituiItemId");
                        if (sub > 0) Add("item", sub);
                    }
                    else Add("item", Num(3));
                }
                else if (seg.Length > 2 && seg[2] == "versao") Add("versao", Num(3));
                else Add("paciente", id);                    // /planos/{pid} e /planos/{pid}/versoes
                break;
        }
        return alvos;
    }

    // LÃª um nÃºmero do corpo (JSON ou formulÃ¡rio) sem consumir a requisiÃ§Ã£o
    static async Task<int> Corpo(HttpContext ctx, string campo)
    {
        var req = ctx.Request;
        try
        {
            if (req.HasFormContentType)
            {
                var form = await req.ReadFormAsync();
                return int.TryParse(form[campo], out var f) ? f : 0;
            }
            if ((req.ContentType ?? "").Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                req.EnableBuffering();
                using var doc = await JsonDocument.ParseAsync(req.Body);
                req.Body.Position = 0;
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return 0;
                foreach (var p in doc.RootElement.EnumerateObject())
                    if (p.Name.Equals(campo, StringComparison.OrdinalIgnoreCase)
                        && p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var n))
                        return n;
            }
        }
        catch { }
        return 0;
    }

    public static async Task<bool> Pertence(string connString, List<(string kind, int id)> alvos, int empresa)
    {
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        foreach (var (kind, id) in alvos)
        {
            if (id <= 0) return false;
            using var cmd = new NpgsqlCommand(Sql[kind], conn);
            cmd.Parameters.AddWithValue("v", id);
            var r = await cmd.ExecuteScalarAsync();
            if (r is null || Convert.ToInt32(r) != empresa) return false;
        }
        return true;
    }
}

// Helper para validaÃ§Ã£o de isolamento multi-tenant (defesa em profundidade)
static class TenantValidation
{
    // Valida se um recurso pertence Ã  empresa do usuÃ¡rio
    // Retorna true se vÃ¡lido, false se nÃ£o encontrado (para retornar 404 sem vazamento de informaÃ§Ã£o)
    public static async Task<bool> ValidarEmpresaAsync(
        string connString, string table, string idColumn, int resourceId, int empresaId)
    {
        if (resourceId <= 0) return false;
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand($"SELECT 1 FROM {table} WHERE {idColumn} = @id AND empresa_id = @emp", conn);
        cmd.Parameters.AddWithValue("id", resourceId);
        cmd.Parameters.AddWithValue("emp", empresaId);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }

    // Valida se um paciente pertence Ã  empresa do usuÃ¡rio
    public static async Task<bool> ValidarPacienteEmpresaAsync(string connString, int pacienteId, int empresaId)
    {
        if (pacienteId <= 0) return false;
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();
        using var cmd = new NpgsqlCommand("SELECT 1 FROM pacientes WHERE id = @id AND empresa_id = @emp", conn);
        cmd.Parameters.AddWithValue("id", pacienteId);
        cmd.Parameters.AddWithValue("emp", empresaId);
        var result = await cmd.ExecuteScalarAsync();
        return result != null;
    }
}

// Endpoint Filter reutilizÃ¡vel para isolamento multi-tenant por pacienteId
// Extrai pacienteId da rota (path) ou do body (JSON/form), valida se pertence Ã  empresa do token
// Se token for tipo "paciente": sÃ³ permite o prÃ³prio pid; caso contrÃ¡rio 404
class TenantIsolationFilter : IEndpointFilter
{
    private readonly string _connString;

    public TenantIsolationFilter(string connString)
    {
        _connString = connString;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;
        var user = httpContext.User;

        // Obter empresa_id do token
        var empresaClaim = user.FindFirst("empresa");
        if (empresaClaim == null || !int.TryParse(empresaClaim.Value, out var empresaId))
        {
            return Results.NotFound(new { mensagem = "Registro nÃ£o encontrado." });
        }

        // Obter tipo do token
        var tipoClaim = user.FindFirst("tipo")?.Value;

        // Extrair pacienteId: primeiro da rota, depois do body
        int pacienteId = 0;

        // 1. Tentar da rota (path parameters)
        // Para endpoints de pacientes (/api/pacientes/{id}), o parÃ¢metro "id" Ã© o pacienteId
        // Para outros endpoints, procurar parÃ¢metro explÃ­cito "pacienteId"
        var path = httpContext.Request.Path.Value ?? "";
        var isPacientesEndpoint = path.StartsWith("/api/pacientes/", StringComparison.OrdinalIgnoreCase);

        if (context.HttpContext.Request.RouteValues.TryGetValue("pacienteId", out var routeValue) && routeValue is int pid)
        {
            pacienteId = pid;
        }
        else if (context.HttpContext.Request.RouteValues.TryGetValue("pacienteId", out var routeStr) && int.TryParse(routeStr?.ToString(), out var pidFromStr))
        {
            pacienteId = pidFromStr;
        }
        // SÃ³ tratar "id" como pacienteId se for endpoint /api/pacientes/{id}
        else if (isPacientesEndpoint && context.HttpContext.Request.RouteValues.TryGetValue("id", out var idValue) && idValue is int idPid)
        {
            pacienteId = idPid;
        }
        else if (isPacientesEndpoint && context.HttpContext.Request.RouteValues.TryGetValue("id", out var idStr) && int.TryParse(idStr?.ToString(), out var idPidFromStr))
        {
            pacienteId = idPidFromStr;
        }

        // 2. Se nÃ£o veio da rota, tentar ler do body (JSON ou form)
        if (pacienteId <= 0)
        {
            pacienteId = await LerPacienteIdDoBody(httpContext);
        }

        // Se nÃ£o conseguiu extrair pacienteId, nÃ£o bloqueia (pode ser endpoint que nÃ£o usa pacienteId)
        if (pacienteId <= 0)
        {
            return await next(context);
        }

        // Se token Ã© de paciente, sÃ³ permite acessar o prÃ³prio pid
        if (tipoClaim == "paciente")
        {
            var pidClaim = user.FindFirst("pid");
            if (pidClaim == null || !int.TryParse(pidClaim.Value, out var tokenPacienteId) || tokenPacienteId != pacienteId)
            {
                return Results.NotFound(new { mensagem = "Registro nÃ£o encontrado." });
            }
            return await next(context);
        }

        // Token de equipe: validar se paciente pertence Ã  empresa do token
        if (!await TenantValidation.ValidarPacienteEmpresaAsync(_connString, pacienteId, empresaId))
        {
            return Results.NotFound(new { mensagem = "Registro nÃ£o encontrado." });
        }

        return await next(context);
    }

    private static async Task<int> LerPacienteIdDoBody(HttpContext httpContext)
    {
        var req = httpContext.Request;
        try
        {
            if (req.HasFormContentType)
            {
                var form = await req.ReadFormAsync();
                return int.TryParse(form["pacienteId"], out var f) ? f : 0;
            }
            if ((req.ContentType ?? "").Contains("json", StringComparison.OrdinalIgnoreCase))
            {
                req.EnableBuffering();
                using var doc = await JsonDocument.ParseAsync(req.Body);
                req.Body.Position = 0;
                if (doc.RootElement.ValueKind != JsonValueKind.Object) return 0;
                foreach (var p in doc.RootElement.EnumerateObject())
                    if (p.Name.Equals("pacienteId", StringComparison.OrdinalIgnoreCase)
                        && p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var n))
                        return n;
            }
        }
        catch { }
        return 0;
    }
}

static class Cpf
{
    public static string Limpar(string? s) => new string((s ?? "").Where(char.IsDigit).ToArray());

    public static bool Valido(string c)
    {
        if (c.Length != 11 || c.Distinct().Count() == 1) return false;
        for (int t = 9; t < 11; t++)
        {
            int soma = 0;
            for (int i = 0; i < t; i++) soma += (c[i] - '0') * (t + 1 - i);
            if ((soma * 10) % 11 % 10 != c[t] - '0') return false;
        }
        return true;
    }
}

// Custom IResult for JSON with headers (for rate limiting responses)
sealed class JsonWithHeadersResult : IResult
{
    private readonly object _value;
    private readonly int _statusCode;
    private readonly Dictionary<string, string> _headers;

    public JsonWithHeadersResult(object value, int statusCode, Dictionary<string, string> headers)
    {
        _value = value;
        _statusCode = statusCode;
        _headers = headers;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        foreach (var header in _headers)
        {
            httpContext.Response.Headers.Append(header.Key, header.Value);
        }
        httpContext.Response.StatusCode = _statusCode;
        return httpContext.Response.WriteAsJsonAsync(_value);
    }
}

static class RateLimitHelpers
{
    public static IResult JsonWithHeaders(object value, int statusCode, Dictionary<string, string> headers)
        => new JsonWithHeadersResult(value, statusCode, headers);
}


