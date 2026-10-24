using Npgsql;
using System.Text.Json;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Linq;
using System.Collections.Generic;
using ACNutri.Billing;
using ACNutri.Email;

var builder = WebApplication.CreateBuilder(args);

    string connString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Defina ConnectionStrings:Default nos user-secrets.");
    string jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Defina Jwt:Key nos user-secrets (mínimo 32 caracteres).");
var origens = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "null" };
// "null" = página aberta direto do disco (file://). Com Live Server, adicione http://127.0.0.1:5500

    builder.Services.AddCors(o => o.AddPolicy("Front", p =>
    p.WithOrigins(origens).AllowAnyMethod().AllowAnyHeader()));

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
    // <img> e links de arquivo não enviam header: aceita ?t= só em GET /api/arquivos
    o.Events = new JwtBearerEvents
    {
    OnMessageReceived = ctx =>
    {
    if (HttpMethods.IsGet(ctx.Request.Method)
    && ctx.Request.Path.StartsWithSegments("/api/arquivos")
    && ctx.Request.Query.TryGetValue("t", out var t))
    ctx.Token = t;
    return Task.CompletedTask;
    }
    };
    });
builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<AsaasClient>();
builder.Services.Configure<AsaasConfig>(builder.Configuration.GetSection("Asaas"));
builder.Services.Configure<EmailConfig>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IEmailService, EmailService>();

// Registrar NpgsqlDataSource para injeção de dependência (usado no BillingModule)
builder.Services.AddSingleton<NpgsqlDataSource>(_ => NpgsqlDataSource.Create(connString));

var app = builder.Build();
app.UseRouting();              // ← adicione
app.UseCors("Front");
app.UseAuthentication();
app.UseAuthorization();

// Billing endpoints (Asaas)
var asaasCfg = builder.Configuration.GetSection("Asaas").Get<AsaasConfig>();
if (asaasCfg is not null)
    app.MapBillingEndpoints(connString, asaasCfg);

await Seed.Garantir(connString, builder.Configuration);
        // Qualquer erro vira JSON { mensagem } (o front mostra no alert)
app.Use(async (context, next) =>
        {
        try
        {
        await next();
        }
        catch (Exception ex)
        {
        Console.WriteLine($"=== ERRO em {context.Request.Method} {context.Request.Path}: {ex} ===");
        if (!context.Response.HasStarted)
        {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { mensagem = ex.Message }));
        }
        }
        });
        // PORTEIRO: toda rota /api exige token de equipe e nível no módulo nutrição
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
    || HttpMethods.IsOptions(m))
{ await next(); return; }
    async Task Negar(int status, string msg)
    {
        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(new { mensagem = msg });
    }
    var u = ctx.User;
        if (u.Identity?.IsAuthenticated != true) { await Negar(401, "Sessão expirada. Entre novamente."); return; }
    // Token de paciente: só GET em /api/paciente/*
    if (u.FindFirst("tipo")?.Value == "paciente")
    {
        if (!path.StartsWith("/api/paciente/") || !HttpMethods.IsGet(m))
        { await Negar(403, "Acesso não permitido."); return; }
        await next(); return;
    }
    if (u.FindFirst("tipo")?.Value != "equipe" || path.StartsWith("/api/paciente/"))
    { await Negar(403, "Acesso não permitido."); return; }
    if (path.StartsWith("/api/usuarios") && u.FindFirst("perfil")?.Value is not ("admin" or "super_admin"))
    { await Negar(403, "Apenas administradores."); return; }
    int nivel = Auth.Peso(u.FindFirst("mod_nutricao")?.Value);
    int exigido = HttpMethods.IsGet(m) ? 1
                : (HttpMethods.IsDelete(m) && path.StartsWith("/api/pacientes")) ? 3
                : 2;
    if (nivel < exigido) { await Negar(403, "Seu perfil não tem permissão para esta ação."); return; }

    // VALIDAÇÃO: usuário pertence à empresa do token (exceto super_admin)
    var perfil = u.FindFirst("perfil")?.Value;
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
            await Negar(403, "Usuário não pertence à empresa do token.");
            return;
        }
    }

    // ISOLAMENTO POR EMPRESA: o registro precisa pertencer à empresa do token
    var alvos = await Escopo.Alvos(ctx);

    if (alvos.Count > 0 && !await Escopo.Pertence(connString, alvos, Auth.Empresa(u)))
    { await Negar(404, "Registro não encontrado."); return; }
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
            id = reader.GetInt32(0),
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

// 2. CADASTRAR PACIENTE
app.MapPost("/api/pacientes", async (HttpRequest request, ClaimsPrincipal user) =>
{
    var data = await JsonSerializer.DeserializeAsync<JsonElement>(request.Body);

    string nome = data.GetProperty("nome").GetString()!;
    string email = data.GetProperty("email").GetString()!;
    string telefone = data.TryGetProperty("telefone", out var tel) ? tel.GetString() ?? "" : "";
    string dataNascimento = data.GetProperty("data_nascimento").GetString()!;
    string sexo = data.GetProperty("sexo").GetString()!;
    string status = data.TryGetProperty("status", out var st) ? st.GetString() ?? "ativo" : "ativo";
    var cpf = Cpf.Limpar(data.TryGetProperty("cpf", out var cp) ? cp.GetString() : "");
    if (cpf != "" && !Cpf.Valido(cpf)) return Results.BadRequest(new { mensagem = "CPF inválido." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO pacientes (nome, email, telefone, data_nascimento, sexo, status, empresa_id, cpf)
        VALUES (@nome, @email, @telefone, @data_nascimento, @sexo, @status, @emp, @cpf) RETURNING id;", conn);

    cmd.Parameters.AddWithValue("nome", nome);
    cmd.Parameters.AddWithValue("email", email);
    cmd.Parameters.AddWithValue("telefone", telefone);
    cmd.Parameters.AddWithValue("data_nascimento", DateTime.Parse(dataNascimento));
    cmd.Parameters.AddWithValue("sexo", sexo);
    cmd.Parameters.AddWithValue("status", status);
    cmd.Parameters.AddWithValue("emp", Auth.Empresa(user));
    cmd.Parameters.Add(new NpgsqlParameter("cpf", NpgsqlTypes.NpgsqlDbType.Varchar) { Value = cpf == "" ? DBNull.Value : cpf });

    try
    {
        var newId = await cmd.ExecuteScalarAsync();
        return Results.Created($"/api/pacientes/{newId}", new { id = newId, mensagem = "Paciente cadastrado com sucesso!" });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        return Results.Conflict(new { mensagem = "Já existe um paciente com esse e-mail ou CPF." });
    }
    });
// 3. ATUALIZAR PACIENTE
// 3. ATUALIZAR PACIENTE
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
    if (cpf != "" && !Cpf.Valido(cpf)) return Results.BadRequest(new { mensagem = "CPF inválido." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
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
            : Results.NotFound(new { mensagem = "Paciente não encontrado." });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        return Results.Conflict(new { mensagem = "Já existe um paciente com esse e-mail ou CPF." });
    }
});

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
        ? Results.Ok(new { mensagem = "Paciente excluído com sucesso!" })
        : Results.NotFound(new { mensagem = "Paciente não encontrado." });
});

//==================================== AVALIAÇÃO FÍSICA (integra com o microserviço Python)======================================
app.MapPost("/api/avaliacoes", async (AvaliacaoInput input, IHttpClientFactory clientFactory) =>
{
    var httpClient = clientFactory.CreateClient();
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
    var responsePython = await httpClient.PostAsync("http://127.0.0.1:5002/calcular-plano", content);

    if (!responsePython.IsSuccessStatusCode)
        return Results.Problem("Erro ao comunicar com o microserviço em Python de cálculos nutricionais.");

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
        mensagem = "Avaliação registrada com sucesso!",
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
            id = reader.GetInt32(0),
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

// Busca alimentos por nome (para autocomplete/preenchimento automático)
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
            id = reader.GetInt32(0),
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

// Busca alimento exato por ID (para preencher formulário ao selecionar)
app.MapGet("/api/alimentos/{id:int}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT id, nome, categoria, calorias_100g, proteinas_100g, carboidratos_100g, gorduras_100g, fibras_100g FROM alimentos WHERE id = @id;", conn);
    cmd.Parameters.AddWithValue("id", id);
    using var reader = await cmd.ExecuteReaderAsync();
    if (!await reader.ReadAsync())
        return Results.NotFound(new { mensagem = "Alimento não encontrado." });

    return Results.Ok(new
    {
        id = reader.GetInt32(0),
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
    // Verificar se alimento pertence à empresa (alimentos globais não têm empresa_id, ou verificar se é da empresa)
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
    if (rows == 0) return Results.NotFound(new { mensagem = "Medida não encontrada." });
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
    if (rows == 0) return Results.NotFound(new { mensagem = "Medida não encontrada." });
    return Results.Ok(new { mensagem = "Medida caseira excluída!" });
})
.RequireAuthorization();

// ====================================================== CHECK-INS============================================================
app.MapPost("/api/checkins", async (CheckinInput input) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO checkins_semanais (paciente_id, empresa_id, peso_atual_kg, adesao_plano_porcentagem, observacoes_paciente)
        VALUES (@paciente_id, (SELECT empresa_id FROM pacientes WHERE id = @paciente_id), @peso, @adesao, @obs) RETURNING id;", conn);

    cmd.Parameters.AddWithValue("paciente_id", input.PacienteId);
    cmd.Parameters.AddWithValue("peso", input.PesoAtualKg);
    cmd.Parameters.AddWithValue("adesao", input.AdesaoPlanoPorcentagem);
    cmd.Parameters.AddWithValue("obs", input.Observacoes ?? "");

    var checkinId = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/checkins/{checkinId}", new { id = checkinId, mensagem = "Check-in registrado com sucesso!" });
});
app.MapGet("/api/checkins/{pacienteId}", async (int pacienteId) =>
{
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
            id = reader.GetInt32(0),
            data_checkin = reader.GetDateTime(1).ToString("yyyy-MM-dd"),
            peso_atual_kg = reader.GetDecimal(2),
            adesao_plano_porcentagem = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            observacoes_paciente = reader.IsDBNull(4) ? "" : reader.GetString(4)
        });
    }
    return Results.Ok(checkins);
});
// ========================================================== ANAMNESE ===========================================================
app.MapPost("/api/anamnese", async (AnamneseInput input) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO anamneses (paciente_id, empresa_id, historico_medico, alergias, intolerancias, rotina_diaria, historico_familiar)
        VALUES (@paciente_id, (SELECT empresa_id FROM pacientes WHERE id = @paciente_id), @historico, @alergias, @intolerancias, @rotina, @familiar)
        ON CONFLICT (paciente_id) DO UPDATE SET
            historico_medico = EXCLUDED.historico_medico,
            alergias = EXCLUDED.alergias,
            intolerancias = EXCLUDED.intolerancias,
            rotina_diaria = EXCLUDED.rotina_diaria,
            historico_familiar = EXCLUDED.historico_familiar,
            atualizado_em = CURRENT_TIMESTAMP
        RETURNING id;", conn);

    cmd.Parameters.AddWithValue("paciente_id", input.PacienteId);
    cmd.Parameters.AddWithValue("historico", input.HistoricoMedico ?? "");
    cmd.Parameters.AddWithValue("alergias", input.Alergias ?? "");
    cmd.Parameters.AddWithValue("intolerancias", input.Intolerancias ?? "");
    cmd.Parameters.AddWithValue("rotina", input.RotinaDiaria ?? "");
    cmd.Parameters.AddWithValue("familiar", input.HistoricoFamiliar ?? "");

    var id = await cmd.ExecuteScalarAsync();
    return Results.Ok(new { id, mensagem = "Anamnese salva com sucesso!" });
});
// Se não houver anamnese, devolve campos vazios (200) em vez de 404
app.MapGet("/api/anamnese/{pacienteId}", async (int pacienteId) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT historico_medico, alergias, intolerancias, rotina_diaria, historico_familiar FROM anamneses WHERE paciente_id = @id;", conn);
    cmd.Parameters.AddWithValue("id", pacienteId);
    using var reader = await cmd.ExecuteReaderAsync();

    if (await reader.ReadAsync())
    {
        return Results.Ok(new
        {
            historico_medico = reader.IsDBNull(0) ? "" : reader.GetString(0),
            alergias = reader.IsDBNull(1) ? "" : reader.GetString(1),
            intolerancias = reader.IsDBNull(2) ? "" : reader.GetString(2),
            rotina_diaria = reader.IsDBNull(3) ? "" : reader.GetString(3),
            historico_familiar = reader.IsDBNull(4) ? "" : reader.GetString(4)
        });
    }

    return Results.Ok(new
    {
        historico_medico = "",
        alergias = "",
        intolerancias = "",
        rotina_diaria = "",
        historico_familiar = ""
    });
});

// =============================================== PLANO ALIMENTAR: REFEIÇÕES =====================================================
app.MapPost("/api/planos/refeicao", async (RefeicaoInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.NomeRefeicao))
        return Results.BadRequest(new { mensagem = "Informe o nome da refeição." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Um plano por paciente (cria só se não existir)
    using var cmdPlano = new NpgsqlCommand(@"
        INSERT INTO planos_alimentares (paciente_id, empresa_id, nome_plano, meta_calorica_kcal)
        VALUES (@paciente_id, (SELECT empresa_id FROM pacientes WHERE id = @paciente_id), 'Plano Personalizado', 2000)
        ON CONFLICT (paciente_id) DO UPDATE SET paciente_id = EXCLUDED.paciente_id
        RETURNING id;", conn);
    cmdPlano.Parameters.AddWithValue("paciente_id", input.PacienteId);
    var planoId = (int)(await cmdPlano.ExecuteScalarAsync())!;

    using var cmdRef = new NpgsqlCommand(@"
        INSERT INTO refeicoes (plano_id, empresa_id, nome_refeicao, horario, observacoes)
        VALUES (@plano_id, (SELECT empresa_id FROM planos_alimentares WHERE id = @plano_id), @nome, @horario::time, @obs)
        ON CONFLICT (plano_id, nome_refeicao) DO NOTHING
        RETURNING id;", conn);
    cmdRef.Parameters.AddWithValue("plano_id", planoId);
    cmdRef.Parameters.AddWithValue("nome", input.NomeRefeicao.Trim());
    cmdRef.Parameters.AddWithValue("horario", string.IsNullOrWhiteSpace(input.Horario) ? "08:00" : input.Horario);
    cmdRef.Parameters.AddWithValue("obs", input.Observacoes ?? "");

    var id = await cmdRef.ExecuteScalarAsync();
    if (id is null)
        return Results.Conflict(new { mensagem = "Já existe uma refeição com esse nome neste plano." });

    return Results.Created($"/api/planos/refeicao/{id}", new { id, mensagem = "Refeição criada!" });
});

app.MapPut("/api/planos/refeicao/{id}", async (int id, RefeicaoInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.NomeRefeicao))
        return Results.BadRequest(new { mensagem = "Informe o nome da refeição." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        UPDATE refeicoes
        SET nome_refeicao = @nome, horario = @horario::time, observacoes = @obs
        WHERE id = @id;", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("nome", input.NomeRefeicao.Trim());
    cmd.Parameters.AddWithValue("horario", string.IsNullOrWhiteSpace(input.Horario) ? "08:00" : input.Horario);
    cmd.Parameters.AddWithValue("obs", input.Observacoes ?? "");

    try
    {
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0
            ? Results.Ok(new { mensagem = "Refeição atualizada!" })
            : Results.NotFound(new { mensagem = "Refeição não encontrada." });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        return Results.Conflict(new { mensagem = "Já existe uma refeição com esse nome neste plano." });
    }
});

app.MapDelete("/api/planos/refeicao/{id}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM refeicoes WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Refeição excluída!" })
        : Results.NotFound(new { mensagem = "Refeição não encontrada." });
});
// Exclui uma opção inteira (todos os itens e substituições dela)
app.MapDelete("/api/planos/refeicao/{refeicaoId}/opcao/{opcao}", async (int refeicaoId, int opcao) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(
        "DELETE FROM itens_refeicao WHERE refeicao_id = @r AND opcao = @o", conn);
    cmd.Parameters.AddWithValue("r", refeicaoId);
    cmd.Parameters.AddWithValue("o", opcao);
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensagem = "Opção excluída!" });
});
//===================================PLANO ALIMENTAR: ITENS (alimentos, opções e substituições)========================================================
app.MapPost("/api/planos/item", async (ItemInput input) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    int refeicaoId = input.RefeicaoId;
    int opcao = input.Opcao > 0 ? input.Opcao : 1;

    // Substituição herda refeição e opção do item original
    if (input.SubstituiItemId.HasValue)
    {
        using var cmdPai = new NpgsqlCommand(
            "SELECT refeicao_id, opcao FROM itens_refeicao WHERE id = @id AND substitui_item_id IS NULL;", conn);
        cmdPai.Parameters.AddWithValue("id", input.SubstituiItemId.Value);
        using var rd = await cmdPai.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
            return Results.BadRequest(new { mensagem = "Item original não encontrado." });
        refeicaoId = rd.GetInt32(0);
        opcao = rd.GetInt32(1);
    }

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO itens_refeicao
            (refeicao_id, empresa_id, opcao, substitui_item_id, alimento_nome_livre, quantidade_g, medida_caseira,
             kcal_manual, proteinas_manual, carbos_manual, gorduras_manual, e_substituicao)
        VALUES
            (@refeicao_id, (SELECT p.empresa_id FROM refeicoes r JOIN planos_alimentares p ON p.id = r.plano_id WHERE r.id = @refeicao_id), @opcao, @sub, @nome, @qtd, @medida, @kcal, @prot, @carbo, @gord, @e_sub)
        RETURNING id;", conn);

    cmd.Parameters.AddWithValue("refeicao_id", refeicaoId);
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
});

// POST /api/planos/item/calcular - Calcula macros via Python (TACO) e insere item
app.MapPost("/api/planos/item/calcular", async (ItemCalcularInput input, IHttpClientFactory clientFactory) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    int refeicaoId = input.RefeicaoId;
    int opcao = input.Opcao > 0 ? input.Opcao : 1;

    // Substituição herda refeição e opção do item original
    if (input.SubstituiItemId.HasValue)
    {
        using var cmdPai = new NpgsqlCommand(
            "SELECT refeicao_id, opcao FROM itens_refeicao WHERE id = @id AND substitui_item_id IS NULL;", conn);
        cmdPai.Parameters.AddWithValue("id", input.SubstituiItemId.Value);
        using var rd = await cmdPai.ExecuteReaderAsync();
        if (!await rd.ReadAsync())
            return Results.BadRequest(new { mensagem = "Item original não encontrado." });
        refeicaoId = rd.GetInt32(0);
        opcao = rd.GetInt32(1);
    }

    // Chama o microserviço Python para calcular
    var httpClient = clientFactory.CreateClient();
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
    var responsePython = await httpClient.PostAsync("http://127.0.0.1:5002/calcular-item", content);

    if (!responsePython.IsSuccessStatusCode)
    {
        var errorContent = await responsePython.Content.ReadAsStringAsync();
        return Results.Problem($"Erro ao calcular item: {errorContent}");
    }

    var jsonResp = await responsePython.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(jsonResp);
    var root = doc.RootElement;

    // Extrai valores calculados (podem ser null se dado não disponível na TACO)
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
            mensagem = $"Alimento '{alimentoNome}' não possui dados nutricionais disponíveis na TACO para cálculo automático. Use entrada manual."
        });
    }

    // Insere o item com valores calculados (usa DBNull.Value para macros não disponíveis)
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO itens_refeicao
            (refeicao_id, empresa_id, opcao, substitui_item_id, alimento_nome_livre, quantidade_g, medida_caseira,
             kcal_manual, proteinas_manual, carbos_manual, gorduras_manual, e_substituicao)
        VALUES
            (@refeicao_id, (SELECT p.empresa_id FROM refeicoes r JOIN planos_alimentares p ON p.id = r.plano_id WHERE r.id = @refeicao_id), @opcao, @sub, @nome, @qtd, @medida, @kcal, @prot, @carbo, @gord, @e_sub)
        RETURNING id;", conn);

    cmd.Parameters.AddWithValue("refeicao_id", refeicaoId);
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
        mensagem = "Item adicionado com cálculo automático!",
        calculo = new { baseCalculo = baseCalculo, gramas, kcal, proteina, carbo, gordura }
    });
});

app.MapPut("/api/planos/item/{id}", async (int id, ItemInput input) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
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
        : Results.NotFound(new { mensagem = "Item não encontrado." });
});

app.MapDelete("/api/planos/item/{id}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM itens_refeicao WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);

    var rowsAffected = await cmd.ExecuteNonQueryAsync();
    return rowsAffected > 0
        ? Results.Ok(new { mensagem = "Item excluído com sucesso!" })
        : Results.NotFound(new { mensagem = "Item não encontrado." });
});

// ============================================PLANO ALIMENTAR: LEITURA E VERSÕES===========================================
// Plano completo (refeições → itens, com opção e substituição)
app.MapGet("/api/planos/{pacienteId}", async (int pacienteId, ClaimsPrincipal user) =>
{
    return Results.Ok(await PlanoHelper.Montar(connString, pacienteId, Auth.Empresa(user)));
});
// Salvar versão (snapshot do plano + orientações)
app.MapPost("/api/planos/{pacienteId}/versoes", async (int pacienteId, VersaoInput input, ClaimsPrincipal user) =>
{
    var plano = await PlanoHelper.Montar(connString, pacienteId, Auth.Empresa(user));
    var refeicoes = (List<Dictionary<string, object?>>)plano["refeicoes"]!;
    if (refeicoes.Count == 0)
        return Results.BadRequest(new { mensagem = "O plano está vazio. Adicione refeições antes de salvar uma versão." });

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
    return Results.Ok(new { id, numero, mensagem = $"Versão {numero} salva!" });
});

app.MapGet("/api/planos/{pacienteId}/versoes", async (int pacienteId) =>
{
    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT id, numero, COALESCE(descricao, ''), criado_em
        FROM planos_versoes WHERE paciente_id = @p ORDER BY numero DESC;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
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
});

app.MapGet("/api/planos/versao/{id}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT numero, COALESCE(descricao, ''), criado_em, snapshot::text
        FROM planos_versoes WHERE id = @id;", conn);
    cmd.Parameters.AddWithValue("id", id);
    using var rd = await cmd.ExecuteReaderAsync();
    if (!await rd.ReadAsync())
        return Results.NotFound(new { mensagem = "Versão não encontrada." });

    return Results.Ok(new
    {
        numero = rd.GetInt32(0),
        descricao = rd.GetString(1),
        criado_em = rd.GetDateTime(2).ToString("yyyy-MM-ddTHH:mm:ss"),
        snapshot = JsonDocument.Parse(rd.GetString(3)).RootElement
    });
});

// ======================================ORIENTAÇÕES NUTRICIONAIS===============================================================
app.MapPost("/api/orientacoes", async (OrientacaoDto dto) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO orientacoes_nutricionais (paciente_id, empresa_id, texto, atualizado_em)
        VALUES (@pacienteId, (SELECT empresa_id FROM pacientes WHERE id = @pacienteId), @texto, CURRENT_TIMESTAMP)
        ON CONFLICT (paciente_id)
        DO UPDATE SET texto = EXCLUDED.texto, atualizado_em = CURRENT_TIMESTAMP;", conn);
    cmd.Parameters.AddWithValue("pacienteId", dto.PacienteId);
    cmd.Parameters.AddWithValue("texto", dto.Texto ?? "");
    await cmd.ExecuteNonQueryAsync();
    return Results.Ok(new { mensagem = "Orientações salvas com sucesso!" });
});

app.MapGet("/api/orientacoes/{pacienteId}", async (int pacienteId) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT texto FROM orientacoes_nutricionais WHERE paciente_id = @id", conn);
    cmd.Parameters.AddWithValue("id", pacienteId);
    var resultado = await cmd.ExecuteScalarAsync();
    return Results.Ok(new { texto = resultado?.ToString() ?? "" });
});
// ============================================= METAS DO PACIENTE=====================================================================
var tiposMeta = new HashSet<string> { "agua", "frutas", "refeicoes", "atividade", "sono", "outro" };
var resultadosMeta = new HashSet<string> { "mantida", "parcial", "nao_mantida" };
var statusMeta = new HashSet<string> { "ativa", "concluida", "arquivada" };

// Lista as metas do paciente já com o histórico de acompanhamento
app.MapGet("/api/metas/{pacienteId}", async (int pacienteId) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var metas = new List<Dictionary<string, object?>>();
    var porId = new Dictionary<int, List<object>>();

    using (var cmd = new NpgsqlCommand(@"
        SELECT id, titulo, COALESCE(descricao, ''), tipo, prazo, status
        FROM metas_paciente
        WHERE paciente_id = @p
        ORDER BY CASE status WHEN 'ativa' THEN 0 WHEN 'concluida' THEN 1 ELSE 2 END, id DESC;", conn))
    {
        cmd.Parameters.AddWithValue("p", pacienteId);
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
        WHERE m.paciente_id = @p
        ORDER BY a.data_acompanhamento DESC, a.id DESC;", conn))
    {
        cmd.Parameters.AddWithValue("p", pacienteId);
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
});

app.MapPost("/api/metas", async (MetaInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Titulo))
        return Results.BadRequest(new { mensagem = "Informe o título da meta." });

    var tipo = tiposMeta.Contains(input.Tipo ?? "") ? input.Tipo! : "outro";

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        INSERT INTO metas_paciente (paciente_id, empresa_id, titulo, descricao, tipo, prazo)
        VALUES (@p, (SELECT empresa_id FROM pacientes WHERE id = @p), @titulo, @desc, @tipo, @prazo) RETURNING id;", conn);
    cmd.Parameters.AddWithValue("p", input.PacienteId);
    cmd.Parameters.AddWithValue("titulo", input.Titulo.Trim());
    cmd.Parameters.AddWithValue("desc", input.Descricao ?? "");
    cmd.Parameters.AddWithValue("tipo", tipo);
    cmd.Parameters.Add(new NpgsqlParameter("prazo", NpgsqlTypes.NpgsqlDbType.Date)
    {
        Value = DateTime.TryParse(input.Prazo, out var pz) ? (object)pz.Date : DBNull.Value
    });

    var id = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/metas/{id}", new { id, mensagem = "Meta criada!" });
});

app.MapPut("/api/metas/{id}", async (int id, MetaInput input) =>
{
    if (string.IsNullOrWhiteSpace(input.Titulo))
        return Results.BadRequest(new { mensagem = "Informe o título da meta." });

    var tipo = tiposMeta.Contains(input.Tipo ?? "") ? input.Tipo! : "outro";
    var status = statusMeta.Contains(input.Status ?? "") ? input.Status! : "ativa";

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        UPDATE metas_paciente
        SET titulo = @titulo, descricao = @desc, tipo = @tipo, prazo = @prazo, status = @status
        WHERE id = @id;", conn);
    cmd.Parameters.AddWithValue("id", id);
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
        : Results.NotFound(new { mensagem = "Meta não encontrada." });
});

app.MapDelete("/api/metas/{id}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM metas_paciente WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Meta excluída!" })
        : Results.NotFound(new { mensagem = "Meta não encontrada." });
});

// Registra como a meta foi no check-in (mantida / parcial / não mantida)
app.MapPost("/api/metas/acompanhamento", async (AcompanhamentoInput input) =>
{
    if (!resultadosMeta.Contains(input.Resultado ?? ""))
        return Results.BadRequest(new { mensagem = "Resultado inválido." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Buscar empresa_id via meta -> paciente
    int empresaId;
    using (var cmdEmp = new NpgsqlCommand(@"
        SELECT p.empresa_id
        FROM metas_paciente m
        JOIN pacientes p ON p.id = m.paciente_id
        WHERE m.id = @metaId", conn))
    {
        cmdEmp.Parameters.AddWithValue("metaId", input.MetaId);
        var result = await cmdEmp.ExecuteScalarAsync();
        if (result == null) return Results.NotFound(new { mensagem = "Meta não encontrada." });
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
});

app.MapDelete("/api/metas/acompanhamento/{id}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM metas_acompanhamento WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Acompanhamento excluído!" })
        : Results.NotFound(new { mensagem = "Acompanhamento não encontrado." });
});

// =====================================================================
// ARQUIVOS (fotos de evolução e exames)
// =====================================================================
app.MapGet("/api/arquivos/{id}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT mime, dados FROM arquivos_paciente WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);
    using var rd = await cmd.ExecuteReaderAsync();
    if (!await rd.ReadAsync())
        return Results.NotFound(new { mensagem = "Arquivo não encontrado." });

    return Results.File(rd.GetFieldValue<byte[]>(1), rd.GetString(0));
});

app.MapDelete("/api/arquivos/{id}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM arquivos_paciente WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
        ? Results.Ok(new { mensagem = "Arquivo excluído!" })
        : Results.NotFound(new { mensagem = "Arquivo não encontrado." });
});

// =====================================================================
// EVOLUÇÃO (peso, medidas, fotos, observações)
// =====================================================================
app.MapGet("/api/evolucao/{pacienteId}", async (int pacienteId) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var anexos = await ArquivoHelper.Listar(conn, pacienteId, "evolucao");
    var lista = new List<object>();

    using var cmd = new NpgsqlCommand(@"
        SELECT id, data_registro, peso_kg, percentual_gordura, cintura_cm, quadril_cm,
               abdomen_cm, braco_cm, coxa_cm, COALESCE(observacao, '')
        FROM evolucao_registros
        WHERE paciente_id = @p
        ORDER BY data_registro DESC, id DESC;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
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
});

app.MapPost("/api/evolucao", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    if (!int.TryParse(form["pacienteId"], out var pacienteId))
        return Results.BadRequest(new { mensagem = "Paciente inválido." });

    var c = EvolucaoCampos.Ler(form);
    if (c.Vazio && form.Files.Count == 0)
        return Results.BadRequest(new { mensagem = "Preencha ao menos um campo ou anexe uma foto." });

    var erro = ArquivoHelper.Validar(form.Files, somenteImagem: true);
    if (erro != null) return Results.BadRequest(new { mensagem = erro });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    // Buscar empresa_id do paciente
    int empresaId;
    using (var cmdEmp = new NpgsqlCommand("SELECT empresa_id FROM pacientes WHERE id = @pid", conn, tx))
    {
        cmdEmp.Parameters.AddWithValue("pid", pacienteId);
        var result = await cmdEmp.ExecuteScalarAsync();
        if (result == null) return Results.NotFound(new { mensagem = "Paciente não encontrado." });
        empresaId = Convert.ToInt32(result);
    }

    using var cmd = new NpgsqlCommand(@"
        INSERT INTO evolucao_registros
            (paciente_id, data_registro, peso_kg, percentual_gordura, cintura_cm, quadril_cm,
             abdomen_cm, braco_cm, coxa_cm, observacao, empresa_id)
        VALUES (@p, @data, @peso, @gord, @cint, @quad, @abd, @braco, @coxa, @obs, @emp)
        RETURNING id;", conn, tx);
    cmd.Parameters.AddWithValue("p", pacienteId);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date) { Value = c.Data });
    cmd.Parameters.Add(ArquivoHelper.Num("peso", c.Peso));
    cmd.Parameters.Add(ArquivoHelper.Num("gord", c.Gordura));
    cmd.Parameters.Add(ArquivoHelper.Num("cint", c.Cintura));
    cmd.Parameters.Add(ArquivoHelper.Num("quad", c.Quadril));
    cmd.Parameters.Add(ArquivoHelper.Num("abd", c.Abdomen));
    cmd.Parameters.Add(ArquivoHelper.Num("braco", c.Braco));
    cmd.Parameters.Add(ArquivoHelper.Num("coxa", c.Coxa));
    cmd.Parameters.AddWithValue("obs", c.Obs);
    cmd.Parameters.AddWithValue("emp", empresaId);
    int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

    await ArquivoHelper.Salvar(conn, tx, pacienteId, "evolucao", id, form.Files);
    await tx.CommitAsync();

    return Results.Created($"/api/evolucao/{id}", new { id, mensagem = "Registro de evolução salvo!" });
});

app.MapPut("/api/evolucao/{id}", async (int id, HttpRequest request) =>
{
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
        WHERE id = @id
        RETURNING paciente_id;", conn, tx);
    cmd.Parameters.AddWithValue("id", id);
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
        return Results.NotFound(new { mensagem = "Registro não encontrado." });

    await ArquivoHelper.Salvar(conn, tx, Convert.ToInt32(pid), "evolucao", id, form.Files);
    await tx.CommitAsync();
    return Results.Ok(new { mensagem = "Registro atualizado!" });
});

app.MapDelete("/api/evolucao/{id}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();

    using (var cmdA = new NpgsqlCommand(
        "DELETE FROM arquivos_paciente WHERE origem = 'evolucao' AND origem_id = @id", conn, tx))
    {
        cmdA.Parameters.AddWithValue("id", id);
        await cmdA.ExecuteNonQueryAsync();
    }

    using var cmd = new NpgsqlCommand("DELETE FROM evolucao_registros WHERE id = @id", conn, tx);
    cmd.Parameters.AddWithValue("id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    await tx.CommitAsync();

    return rows > 0
        ? Results.Ok(new { mensagem = "Registro excluído!" })
        : Results.NotFound(new { mensagem = "Registro não encontrado." });
});

// ================================================ EXAMES=====================================================================
app.MapGet("/api/exames/{pacienteId}", async (int pacienteId) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    var anexos = await ArquivoHelper.Listar(conn, pacienteId, "exame");
    var lista = new List<object>();

    using var cmd = new NpgsqlCommand(@"
        SELECT id, titulo_exame, data_exame, COALESCE(observacao, '')
        FROM exames_paciente
        WHERE paciente_id = @p
        ORDER BY data_exame DESC, id DESC;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
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
            arquivos = anexos.TryGetValue(id, out var a) ? a : new List<object>()
        });
    }
    return Results.Ok(lista);
});

app.MapPost("/api/exames", async (HttpRequest request) =>
{
    var form = await request.ReadFormAsync();
    if (!int.TryParse(form["pacienteId"], out var pacienteId))
        return Results.BadRequest(new { mensagem = "Paciente inválido." });

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
        VALUES (@p, (SELECT empresa_id FROM pacientes WHERE id = @p), @titulo, @data, @obs) RETURNING id;", conn, tx);
    cmd.Parameters.AddWithValue("p", pacienteId);
    cmd.Parameters.AddWithValue("titulo", titulo.Length > 100 ? titulo[..100] : titulo);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date) { Value = EvolucaoCampos.LerData(form["data"]) });
    cmd.Parameters.AddWithValue("obs", form["observacao"].ToString());
    int id = Convert.ToInt32(await cmd.ExecuteScalarAsync());

    await ArquivoHelper.Salvar(conn, tx, pacienteId, "exame", id, form.Files);
    await tx.CommitAsync();

    return Results.Created($"/api/exames/{id}", new { id, mensagem = "Exame registrado!" });
});

app.MapPut("/api/exames/{id}", async (int id, HttpRequest request) =>
{
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
    WHERE id = @id
    RETURNING paciente_id;", conn, tx);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.AddWithValue("titulo", titulo.Length > 100 ? titulo[..100] : titulo);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date) { Value = EvolucaoCampos.LerData(form["data"]) });
    cmd.Parameters.AddWithValue("obs", form["observacao"].ToString());
    var pid = await cmd.ExecuteScalarAsync();
    if (pid is null)
    return Results.NotFound(new { mensagem = "Exame não encontrado." });
    await ArquivoHelper.Salvar(conn, tx, Convert.ToInt32(pid), "exame", id, form.Files);
    await tx.CommitAsync();
    return Results.Ok(new { mensagem = "Exame atualizado!" });
});
app.MapDelete("/api/exames/{id}", async (int id) =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var tx = await conn.BeginTransactionAsync();
    using (var cmdA = new NpgsqlCommand(
    "DELETE FROM arquivos_paciente WHERE origem = 'exame' AND origem_id = @id", conn, tx))
    {
    cmdA.Parameters.AddWithValue("id", id);
    await cmdA.ExecuteNonQueryAsync();
    }
    using var cmd = new NpgsqlCommand("DELETE FROM exames_paciente WHERE id = @id", conn, tx);
    cmd.Parameters.AddWithValue("id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    await tx.CommitAsync();
    return rows > 0
    ? Results.Ok(new { mensagem = "Exame excluído!" })
    : Results.NotFound(new { mensagem = "Exame não encontrado." });
    });
// ======================================= OBSERVAÇÕES (semana do check-in e gerais)============================================
app.MapGet("/api/observacoes/{pacienteId}", async (int pacienteId) =>
    {
    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
    SELECT id, tipo, data_ref, texto
    FROM observacoes_paciente
    WHERE paciente_id = @p
    ORDER BY data_ref DESC, id DESC;", conn);
    cmd.Parameters.AddWithValue("p", pacienteId);
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
    });
app.MapPost("/api/observacoes", async (ObservacaoInput input) =>
    {
    if (string.IsNullOrWhiteSpace(input.Texto))
    return Results.BadRequest(new { mensagem = "Escreva a observação." });
    var tipo = input.Tipo == "geral" ? "geral" : "semana";
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
    INSERT INTO observacoes_paciente (paciente_id, empresa_id, tipo, data_ref, texto)
    VALUES (@p, (SELECT empresa_id FROM pacientes WHERE id = @p), @tipo, @data, @texto) RETURNING id;", conn);
    cmd.Parameters.AddWithValue("p", input.PacienteId);
    cmd.Parameters.AddWithValue("tipo", tipo);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date)
    {
    Value = DateTime.TryParse(input.Data, out var dt) ? dt.Date : DateTime.Today
    });
    cmd.Parameters.AddWithValue("texto", input.Texto.Trim());
    var id = await cmd.ExecuteScalarAsync();
    return Results.Created($"/api/observacoes/{id}", new { id, mensagem = "Observação salva!" });
    });
app.MapPut("/api/observacoes/{id}", async (int id, ObservacaoInput input) =>
    {
    if (string.IsNullOrWhiteSpace(input.Texto))
    return Results.BadRequest(new { mensagem = "Escreva a observação." });
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
    UPDATE observacoes_paciente SET data_ref = @data, texto = @texto WHERE id = @id;", conn);
    cmd.Parameters.AddWithValue("id", id);
    cmd.Parameters.Add(new NpgsqlParameter("data", NpgsqlTypes.NpgsqlDbType.Date)
    {
    Value = DateTime.TryParse(input.Data, out var dt) ? dt.Date : DateTime.Today
    });
    cmd.Parameters.AddWithValue("texto", input.Texto.Trim());
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
    ? Results.Ok(new { mensagem = "Observação atualizada!" })
    : Results.NotFound(new { mensagem = "Observação não encontrada." });
    });
app.MapDelete("/api/observacoes/{id}", async (int id) =>
    {
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("DELETE FROM observacoes_paciente WHERE id = @id", conn);
    cmd.Parameters.AddWithValue("id", id);
    var rows = await cmd.ExecuteNonQueryAsync();
    return rows > 0
    ? Results.Ok(new { mensagem = "Observação excluída!" })
    : Results.NotFound(new { mensagem = "Observação não encontrada." });
    });
// Duplica uma opção (itens + substituições) para uma nova opção da mesma refeição
app.MapPost("/api/planos/refeicao/{refeicaoId}/opcao/{opcao}/duplicar", async (int refeicaoId, int opcao) =>
    {
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
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
    return Results.NotFound(new { mensagem = "Opção não encontrada." });
    var mapa = new Dictionary<int, int>();
// primeiro os itens principais, depois as substituições (precisam do novo id do pai)
    foreach (var it in origem.OrderBy(x => x.pai.HasValue ? 1 : 0).ThenBy(x => x.id))
    {
    using var cmd = new NpgsqlCommand(@"
    INSERT INTO itens_refeicao
    (refeicao_id, empresa_id, opcao, substitui_item_id, alimento_nome_livre, quantidade_g, medida_caseira,
    kcal_manual, proteinas_manual, carbos_manual, gorduras_manual, e_substituicao)
    VALUES (@r, (SELECT r.empresa_id FROM refeicoes r JOIN planos_alimentares p ON p.id = r.plano_id WHERE r.id = @r), @o, @sub, @nome, @qtd, @medida, @kcal, @prot, @carbo, @gord, @esub)
    RETURNING id;", conn, tx);
    cmd.Parameters.AddWithValue("r", refeicaoId);
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
    return Results.Ok(new { opcao = novaOpcao, mensagem = "Opção duplicada!" });
});

// Restaura uma versão (antes, guarda o plano atual como nova versão de segurança)
app.MapPost("/api/planos/versao/{id}/restaurar", async (int id, ClaimsPrincipal user) =>
{
    int pacienteId, numeroOrigem;
    string snapJson;

    using (var conn0 = new NpgsqlConnection(connString))
    {
        await conn0.OpenAsync();
        using var cmd0 = new NpgsqlCommand(
            "SELECT paciente_id, numero, snapshot::text FROM planos_versoes WHERE id = @id", conn0);
        cmd0.Parameters.AddWithValue("id", id);
        using var rd0 = await cmd0.ExecuteReaderAsync();
        if (!await rd0.ReadAsync())
            return Results.NotFound(new { mensagem = "Versão não encontrada." });
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
        cmdB.Parameters.AddWithValue("d", $"Backup automático antes de restaurar a versão {numeroOrigem}");
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
            cmdR.Parameters.AddWithValue("nome", r.GetProperty("nome").GetString() ?? "Refeição");
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
        ? $"Versão {numeroOrigem} restaurada! O plano anterior foi guardado como versão {numeroBackup}."
        : $"Versão {numeroOrigem} restaurada!";
    return Results.Ok(new { mensagem = msg });
});

// =====================================================================
// AUTENTICAÇÃO E USUÁRIOS
// =====================================================================
app.MapPost("/api/auth/login", async (LoginInput input) =>
{
    var email = (input.Email ?? "").Trim().ToLowerInvariant();
    if (email == "" || string.IsNullOrEmpty(input.Senha))
        return Results.BadRequest(new { mensagem = "Informe e-mail e senha." });

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    int id = 0, empresaId = 0; string nome = "", hash = "", perfil = ""; bool ativo = false, bloqueado = false;
    bool achou;
    using (var cmd = new NpgsqlCommand(@"
        SELECT id, empresa_id, nome, senha_hash, perfil, ativo,
               (bloqueado_ate IS NOT NULL AND bloqueado_ate > CURRENT_TIMESTAMP)
        FROM usuarios WHERE lower(email) = @e AND perfil <> 'cliente';", conn))
    {
        cmd.Parameters.AddWithValue("e", email);
        using var rd = await cmd.ExecuteReaderAsync();
        achou = await rd.ReadAsync();
        if (achou)
        {
            id = rd.GetInt32(0); empresaId = rd.GetInt32(1); nome = rd.GetString(2);
            hash = rd.GetString(3); perfil = rd.GetString(4); ativo = rd.GetBoolean(5); bloqueado = rd.GetBoolean(6);
        }
    }

    if (!achou || !ativo)
        return Results.Json(new { mensagem = "E-mail ou senha incorretos." }, statusCode: 401);

    if (bloqueado)
        return Results.Json(new { mensagem = "Muitas tentativas. Tente novamente em alguns minutos." }, statusCode: 429);

    if (!BCrypt.Net.BCrypt.Verify(input.Senha, hash))
    {
        using var f = new NpgsqlCommand(@"
            UPDATE usuarios SET
                tentativas_falhas = CASE WHEN tentativas_falhas + 1 >= 5 THEN 0 ELSE tentativas_falhas + 1 END,
                bloqueado_ate = CASE WHEN tentativas_falhas + 1 >= 5
                                     THEN CURRENT_TIMESTAMP + interval '15 minutes' ELSE bloqueado_ate END
            WHERE id = @id;", conn);
        f.Parameters.AddWithValue("id", id);
        await f.ExecuteNonQueryAsync();
        return Results.Json(new { mensagem = "E-mail ou senha incorretos." }, statusCode: 401);
    }

    using (var ok = new NpgsqlCommand(
        "UPDATE usuarios SET tentativas_falhas = 0, bloqueado_ate = NULL WHERE id = @id;", conn))
    {
        ok.Parameters.AddWithValue("id", id);
        await ok.ExecuteNonQueryAsync();
    }

    // Módulos ligados na empresa + nível do usuário (admin = total em tudo)
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
});

var perfisEquipe = new HashSet<string> { "super_admin", "admin", "profissional", "atendente" };
var niveisAcesso = new HashSet<string> { "nenhum", "leitura", "edicao", "total" };

// =====================================================================
// EMPRESAS E CONVITES (super_admin / admin)
// =====================================================================
// Helper para verificar se é super_admin
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

    // Ativar módulo nutricao por padrão
    using var cm = new NpgsqlCommand(
        "INSERT INTO empresa_modulos (empresa_id, modulo) VALUES (@emp, 'nutricao') ON CONFLICT DO NOTHING;", conn);
    cm.Parameters.AddWithValue("emp", empresaId);
    await cm.ExecuteNonQueryAsync();

    return Results.Created($"/api/empresas/{empresaId}", new { id = empresaId, nome, mensagem = "Empresa criada!" });
})
.RequireAuthorization();

// GET /api/empresas - Listar empresas (apenas super_admin)
app.MapGet("/api/empresas", async (ClaimsPrincipal user) =>
{
    if (!IsSuperAdmin(user))
        return Results.Forbid();

    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT id, nome, criado_em FROM empresas ORDER BY nome;", conn);
    using var rd = await cmd.ExecuteReaderAsync();
    while (await rd.ReadAsync())
        lista.Add(new { id = rd.GetInt32(0), nome = rd.GetString(1), criadoEm = rd.GetDateTime(2) });
    return Results.Ok(lista);
})
.RequireAuthorization();

// GET /api/empresas/count - Contar empresas (público, para verificar se precisa onboarding)
app.MapGet("/api/empresas/count", async () =>
{
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM empresas;", conn);
    var count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
    return Results.Ok(new { count });
});

// GET /api/convites/info/{token} - Info do convite (público, para tela de aceite)
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
    return Results.NotFound(new { mensagem = "Convite não encontrado ou expirado." });
});

// POST /api/onboarding - Criar primeira empresa + super-admin (sem auth, só se 0 usuários e 0 empresas)
app.MapPost("/api/onboarding", async (OnboardingInput input) =>
{
    // Verificar se já existem usuários OU empresas
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
        return Results.BadRequest(new { mensagem = "Todos os campos são obrigatórios. Senha mínima 8 caracteres." });

    // Criar empresa
    using var cmdEmp = new NpgsqlCommand("INSERT INTO empresas (nome) VALUES (@n) RETURNING id;", conn);
    cmdEmp.Parameters.AddWithValue("n", empresaNome);
    int empresaId = Convert.ToInt32(await cmdEmp.ExecuteScalarAsync());

    // Ativar módulo nutrição
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

    // Módulo para o usuário
    using var cmdUM = new NpgsqlCommand(
        "INSERT INTO usuario_modulos (usuario_id, modulo, nivel) VALUES (@u, 'nutricao', 'total');", conn);
    cmdUM.Parameters.AddWithValue("u", userId);
    await cmdUM.ExecuteNonQueryAsync();

    return Results.Ok(new { mensagem = "Empresa e super-admin criados com sucesso!", empresaId, userId });
});

// POST /api/empresas/{id}/convite - Convidar usuário para empresa (admin/super_admin)
app.MapPost("/api/empresas/{empresaId:int}/convite", async (int empresaId, ConviteInput input, ClaimsPrincipal user, IEmailService emailService) =>
{
    var perfilToken = user.FindFirst("perfil")?.Value;
    if (perfilToken != "super_admin" && perfilToken != "admin")
        return Results.Forbid();

    // Se não for super_admin, só pode convidar para sua própria empresa
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
            return Results.NotFound(new { mensagem = "Empresa não encontrada." });
        empresaNome = result.ToString()!;
    }

    // Verificar se já existe usuário com esse email na empresa
    using var exist = new NpgsqlCommand("SELECT 1 FROM usuarios WHERE email = @e AND empresa_id = @emp", conn);
    exist.Parameters.AddWithValue("e", email);
    exist.Parameters.AddWithValue("emp", empresaId);
    if (await exist.ExecuteScalarAsync() != null)
        return Results.Conflict(new { mensagem = "Já existe um usuário com esse e-mail nesta empresa." });

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
        return Results.BadRequest(new { mensagem = "Token e senha (mín. 8 caracteres) são obrigatórios." });

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
        return Results.Json(new { mensagem = "Convite inválido ou expirado." }, statusCode: 400);

    // Verificar se usuário já existe
    using (var check = new NpgsqlCommand("SELECT 1 FROM usuarios WHERE email = @e AND empresa_id = @emp", conn))
    {
        check.Parameters.AddWithValue("e", email);
        check.Parameters.AddWithValue("emp", empresaId);
        if (await check.ExecuteScalarAsync() != null)
            return Results.Conflict(new { mensagem = "Usuário já cadastrado." });
    }

    // Criar usuário
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

        return Results.Created($"/api/usuarios/{id}", new { id, mensagem = "Usuário criado!" });
    }
    catch (PostgresException ex) when (ex.SqlState == "23505")
    {
        return Results.Conflict(new { mensagem = "Já existe um usuário com esse e-mail." });
    }
});

app.MapPut("/api/usuarios/{id}", async (int id, UsuarioInput i, ClaimsPrincipal user) =>
{
    int meuId = int.Parse(user.FindFirst("uid")!.Value);
    var perfil = perfisEquipe.Contains(i.Perfil ?? "") ? i.Perfil! : "atendente";
    var nivel = niveisAcesso.Contains(i.NivelNutricao ?? "") ? i.NivelNutricao! : "leitura";
    bool ativo = i.Ativo ?? true;

    if (id == meuId && (!ativo || perfil != "admin"))
        return Results.BadRequest(new { mensagem = "Você não pode desativar nem rebaixar o próprio usuário." });
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
        return Results.NotFound(new { mensagem = "Usuário não encontrado." });

    using var cm = new NpgsqlCommand(@"
        INSERT INTO usuario_modulos (usuario_id, modulo, nivel) VALUES (@u, 'nutricao', @n)
        ON CONFLICT (usuario_id, modulo) DO UPDATE SET nivel = EXCLUDED.nivel;", conn);
    cm.Parameters.AddWithValue("u", id);
    cm.Parameters.AddWithValue("n", nivel);
    await cm.ExecuteNonQueryAsync();

    return Results.Ok(new { mensagem = "Usuário atualizado!" });
});

// =====================================================================
// ÁREA DO PACIENTE
// =====================================================================
app.MapPost("/api/auth/paciente", async (PacienteLoginInput i, HttpContext ctx) =>
{
    const string generico = "CPF ou data de nascimento incorretos.";
    var cpf = Cpf.Limpar(i.Cpf);
    var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "?";

    if (!Cpf.Valido(cpf) || !DateTime.TryParse(i.Nascimento,
            System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var nasc))
        return Results.Json(new { mensagem = generico }, statusCode: 401);

    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    // Bloqueio por CPF (5) e por IP (20) nos últimos 15 minutos
    long falhasCpf, falhasIp;
    using (var c = new NpgsqlCommand(@"
        SELECT
          (SELECT COUNT(*) FROM acessos_paciente WHERE cpf_tentado = @c AND NOT sucesso AND criado_em > CURRENT_TIMESTAMP - interval '15 minutes'),
          (SELECT COUNT(*) FROM acessos_paciente WHERE ip = @ip AND NOT sucesso AND criado_em > CURRENT_TIMESTAMP - interval '15 minutes');", conn))
    {
        c.Parameters.AddWithValue("c", cpf);
        c.Parameters.AddWithValue("ip", ip);
        using var rd = await c.ExecuteReaderAsync();
        await rd.ReadAsync();
        falhasCpf = rd.GetInt64(0); falhasIp = rd.GetInt64(1);
    }
    if (falhasCpf >= 5 || falhasIp >= 20)
        return Results.Json(new { mensagem = "Muitas tentativas. Tente novamente em 15 minutos." }, statusCode: 429);

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

    using (var log = new NpgsqlCommand(
        "INSERT INTO acessos_paciente (paciente_id, cpf_tentado, ip, sucesso) VALUES (@p, @c, @ip, @s);", conn))
    {
        log.Parameters.Add(new NpgsqlParameter("p", NpgsqlTypes.NpgsqlDbType.Integer) { Value = achou ? pid : DBNull.Value });
        log.Parameters.AddWithValue("c", cpf);
        log.Parameters.AddWithValue("ip", ip);
        log.Parameters.AddWithValue("s", achou);
        await log.ExecuteNonQueryAsync();
    }

    if (!achou) return Results.Json(new { mensagem = generico }, statusCode: 401);

    using (var limpa = new NpgsqlCommand(
        "DELETE FROM acessos_paciente WHERE cpf_tentado = @c AND NOT sucesso;", conn))
    {
        limpa.Parameters.AddWithValue("c", cpf);
        await limpa.ExecuteNonQueryAsync();
    }

    return Results.Ok(new { token = Auth.GerarPaciente(jwtKey, pid, empresa), nome = nome.Split(' ')[0], tipo = "paciente" });
});

// Tudo que a paciente enxerga, numa chamada só (o ID vem do token)
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

    return Results.Ok(new { nome, plano, orientacoes, metas });
});

// Para a equipe: últimos acessos da paciente
app.MapGet("/api/acessos/{pacienteId}", async (int pacienteId) =>
{
    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var c = new NpgsqlCommand(
        "SELECT criado_em FROM acessos_paciente WHERE paciente_id = @p AND sucesso ORDER BY id DESC LIMIT 10;", conn);
    c.Parameters.AddWithValue("p", pacienteId);
    using var rd = await c.ExecuteReaderAsync();
    while (await rd.ReadAsync()) lista.Add(new { em = rd.GetDateTime(0).ToString("yyyy-MM-ddTHH:mm:ss") });
    return Results.Ok(lista);
});

// =====================================================================
// HISTÓRICO UNIFICADO (linha do tempo do paciente)
// =====================================================================
app.MapGet("/api/historico/{pacienteId}", async (int pacienteId) =>
{
    var lista = new List<object>();
    using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();
    using var cmd = new NpgsqlCommand(@"
        SELECT * FROM (
            SELECT 'plano' AS tipo, criado_em::date AS data, criado_em AS ordem,
                   'Versão ' || numero || ' do plano' AS titulo,
                   COALESCE(descricao, '') AS detalhe, '' AS extra
            FROM planos_versoes WHERE paciente_id = @p

            UNION ALL
            SELECT 'evolucao', data_registro, criado_em, 'Registro de evolução',
                   COALESCE(observacao, ''),
                   CASE WHEN peso_kg IS NULL THEN '' ELSE replace(peso_kg::text, '.', ',') || ' kg' END
            FROM evolucao_registros WHERE paciente_id = @p

            UNION ALL
            SELECT 'checkin', data_checkin, criado_em, 'Check-in semanal',
                   COALESCE(observacoes_paciente, ''),
                   replace(peso_atual_kg::text, '.', ',') || ' kg · adesão ' || COALESCE(adesao_plano_porcentagem, 0) || '%'
            FROM checkins_semanais WHERE paciente_id = @p

            UNION ALL
            SELECT CASE WHEN tipo = 'semana' THEN 'obs_semana' ELSE 'obs_geral' END,
                   data_ref, criado_em,
                   CASE WHEN tipo = 'semana' THEN 'Observação da semana' ELSE 'Observação geral' END,
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
});

// ===========================EXECUÇÃO DA APLICAÇÃO=========================================
app.Run();
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
record AnamneseInput(int PacienteId, string? HistoricoMedico, string? Alergias, string? Intolerancias, string? RotinaDiaria, string? HistoricoFamiliar);
record RefeicaoInput(int PacienteId, string NomeRefeicao, string? Horario, string? Observacoes);
record ItemInput(
    int RefeicaoId, int Opcao, int? SubstituiItemId, string? AlimentoNome,
    double QuantidadeG, string? MedidaCaseira,
    double Kcal, double Proteinas, double Carbos, double Gorduras);

// Entrada para cálculo automático via Python service (TACO)
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
record OnboardingInput(string EmpresaNome, string AdminNome, string AdminEmail, string AdminSenha);
record MedidaCaseiraInput(string Medida, decimal GramasPorUnidade, string? Observacao = null);
// =====================================================================
// MONTA O PLANO COMPLETO (refeições → itens)
// =====================================================================
// =====================================================================
// MONTA O PLANO COMPLETO (refeições → itens)
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
                        ["nome"] = rd.IsDBNull(1) ? "Refeição" : rd.GetString(1),
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
// AUXILIARES: CAMPOS DA EVOLUÇÃO
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
            if (f.Length == 0) return $"O arquivo \"{f.FileName}\" está vazio.";
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

    // Lista os arquivos (sem o conteúdo) agrupados pelo registro de origem
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

// Cria o primeiro super-admin (só se não existir nenhum usuário)
static class Seed
{
    public static async Task Garantir(string connString, IConfiguration cfg)
    {
        using var conn = new NpgsqlConnection(connString);
        await conn.OpenAsync();

        using (var c = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios", conn))
            if (Convert.ToInt32(await c.ExecuteScalarAsync()) > 0) return;

        // Super-admin config (novas chaves para multi-tenant)
        var email = cfg["Seed:SuperAdminEmail"]?.Trim().ToLowerInvariant();
        var senha = cfg["Seed:SuperAdminSenha"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(senha) || senha.Length < 8)
        {
            Console.WriteLine("!!! Nenhum usuário cadastrado. Defina Seed:SuperAdminEmail e Seed:SuperAdminSenha (mín. 8) nos user-secrets.");
            return;
        }

        // Garante tabelas base
        foreach (var sql in new[]
        {
            "INSERT INTO perfis (chave, nome) VALUES ('super_admin','Super Administrador') ON CONFLICT DO NOTHING",
            "INSERT INTO perfis (chave, nome) VALUES ('admin','Administrador') ON CONFLICT DO NOTHING",
            "INSERT INTO perfis (chave, nome) VALUES ('profissional','Profissional') ON CONFLICT DO NOTHING",
            "INSERT INTO perfis (chave, nome) VALUES ('atendente','Atendente') ON CONFLICT DO NOTHING",
            "INSERT INTO modulos (chave, nome) VALUES ('nutricao','Nutrição') ON CONFLICT DO NOTHING",
            "INSERT INTO empresas (nome) SELECT 'Empresa Principal' WHERE NOT EXISTS (SELECT 1 FROM empresas)",
            "INSERT INTO empresa_modulos (empresa_id, modulo) SELECT MIN(id), 'nutricao' FROM empresas ON CONFLICT DO NOTHING"
        })
        {
            using var c = new NpgsqlCommand(sql, conn);
            await c.ExecuteNonQueryAsync();
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
// Descobre a que paciente cada requisição pertence e confere a empresa
static class Escopo
{
    static readonly Dictionary<string, string> Sql = new()
    {
        ["paciente"]  = "SELECT empresa_id FROM pacientes WHERE id = @v",
        ["refeicao"]  = "SELECT empresa_id FROM refeicoes WHERE id = @v",
        ["item"]      = "SELECT empresa_id FROM itens_refeicao WHERE id = @v",
        ["versao"]    = "SELECT empresa_id FROM planos_versoes WHERE id = @v",
        ["meta"]      = "SELECT empresa_id FROM metas_paciente WHERE id = @v",
        ["acomp"]     = "SELECT empresa_id FROM metas_acompanhamento WHERE id = @v",
        ["arquivo"]   = "SELECT empresa_id FROM arquivos_paciente WHERE id = @v",
        ["evolucao"]  = "SELECT empresa_id FROM evolucao_registros WHERE id = @v",
        ["exame"]     = "SELECT empresa_id FROM exames_paciente WHERE id = @v",
        ["obs"]       = "SELECT empresa_id FROM observacoes_paciente WHERE id = @v"
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
               case "checkins": case "anamnese": case "orientacoes": case "avaliacoes": case "acessos": case "historico":
                Add("paciente", get ? id : await Corpo(ctx, "pacienteId")); break;

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

    // Lê um número do corpo (JSON ou formulário) sem consumir a requisição
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