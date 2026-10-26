// BillingModule.cs - Integração Asaas (billing/assinaturas)
// Adicione ao Program.cs: builder.Services.AddHttpClient<AsaasClient>();  e  app.MapBillingEndpoints();

using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace ACNutri.Billing;

public record AsaasConfig(string BaseUrl, string ApiKey, string WebhookSecret);
public record PlanoLimites(int MaxPacientes, int MaxProfissionais, int StorageMB, int MaxVersoesPlano);

public static class Planos
{
    public static readonly Dictionary<string, (decimal ValorMensal, PlanoLimites Limites)> Catalogo = new()
    {
        ["basico"]       = ( 97.00m, new PlanoLimites(  50,  2,   500,   5)),
        ["profissional"] = (297.00m, new PlanoLimites( 200, 10,  2000,  20)),
        ["enterprise"]   = (797.00m, new PlanoLimites(1000, 50, 10000, 100))
    };

    public static (decimal ValorMensal, PlanoLimites Limites) Obter(string plano)
        => Catalogo.TryGetValue(plano, out var p) ? p : Catalogo["basico"];
}

public class AsaasClient
{
    private readonly HttpClient _http;
    private readonly AsaasConfig _cfg;

    public AsaasClient(HttpClient http, AsaasConfig cfg)
    {
        _http = http;
        _cfg = cfg;
        _http.BaseAddress = new Uri(cfg.BaseUrl);
        _http.DefaultRequestHeaders.Add("access_token", cfg.ApiKey);
    }

    public async Task<JsonElement> CriarCustomerAsync(string name, string email, string phone, string cpfCnpj, string externalRef)
    {
        var body = new { name, email, phone, cpfCnpj, externalReference = externalRef };
        var res = await _http.PostAsJsonAsync("/v3/customers", body);
        return await TratarResposta(res);
    }

    public async Task<JsonElement> CriarAssinaturaAsync(string customerId, string plano, int valorCentavos, int cicloDias, DateTime? dataInicio = null)
    {
        var (valorMensal, _) = Planos.Obter(plano);
        var body = new
        {
            customer = customerId,
            billingType = "CREDIT_CARD",
            value = valorMensal,
            cycle = "MONTHLY",
            description = $"ACNUTRI - Plano {plano}",
            nextDueDate = (dataInicio ?? DateTime.UtcNow.AddDays(1)).ToString("yyyy-MM-dd"),
            externalReference = $"sub_{plano}_{customerId}"
        };
        var res = await _http.PostAsJsonAsync("/v3/subscriptions", body);
        return await TratarResposta(res);
    }

    public async Task<JsonElement> CancelarAssinaturaAsync(string subscriptionId)
    {
        var res = await _http.DeleteAsync($"/v3/subscriptions/{subscriptionId}");
        return await TratarResposta(res);
    }

    public async Task<JsonElement> ObterAssinaturaAsync(string subscriptionId)
    {
        var res = await _http.GetAsync($"/v3/subscriptions/{subscriptionId}");
        return await TratarResposta(res);
    }

    public async Task<JsonElement> CriarCheckoutAsync(string customerId, decimal valor, string descricao, string returnUrl)
    {
        var body = new
        {
            customer = customerId,
            billingType = "CREDIT_CARD",
            value = valor,
            description = descricao,
            returnUrl
        };
        var res = await _http.PostAsJsonAsync("/v3/payments", body);
        return await TratarResposta(res);
    }

    private static async Task<JsonElement> TratarResposta(HttpResponseMessage res)
    {
        var json = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
            throw new HttpRequestException($"Asaas {res.StatusCode}: {json}");
        return JsonDocument.Parse(json).RootElement;
    }
}



// ============================================================
// ENDPOINTS (chame app.MapBillingEndpoints() no Program.cs)
// ============================================================
public static class BillingEndpoints
{
    public static void MapBillingEndpoints(this WebApplication app, string connString, AsaasConfig asaasCfg)
    {
        var grupo = app.MapGroup("/api/billing").RequireAuthorization();

        // ---- GET: assinatura da empresa logada ----
grupo.MapGet("/assinatura", async (ClaimsPrincipal user, NpgsqlDataSource db) =>
{
    int emp = Auth.Empresa(user);
    await using var cmd = db.CreateCommand(@"
        SELECT a.id, a.plano, a.status, a.valor_centavos, a.ciclo,
               a.trial_fim, a.inicio_vigencia, a.fim_vigencia,
               a.asaas_customer_id, a.asaas_subscription_id,
               CASE a.plano
                   WHEN 'basico'       THEN jsonb_build_object('max_pacientes', 50,  'max_profissionais', 2,  'storage_mb', 500,   'max_versoes_plano', 5)
                   WHEN 'profissional' THEN jsonb_build_object('max_pacientes', 200, 'max_profissionais', 10, 'storage_mb', 2000,  'max_versoes_plano', 20)
                   WHEN 'enterprise'   THEN jsonb_build_object('max_pacientes', 1000,'max_profissionais', 50, 'storage_mb', 10000, 'max_versoes_plano', 100)
               END AS limites
        FROM assinaturas a
        WHERE a.empresa_id = @emp;");
    cmd.Parameters.AddWithValue("emp", emp);

    await using var rd = await cmd.ExecuteReaderAsync();
    if (!await rd.ReadAsync())
        return Results.NotFound(new { mensagem = "Empresa sem assinatura configurada." });

    return Results.Ok(new
    {
        id = rd.GetInt64(0),
        plano = rd.GetString(1),
        status = rd.GetString(2),
        valor_centavos = rd.GetInt32(3),
        ciclo = rd.GetString(4),
        trial_fim = rd.IsDBNull(5) ? (DateTime?)null : rd.GetDateTime(5),
        inicio_vigencia = rd.GetDateTime(6),
        fim_vigencia = rd.IsDBNull(7) ? (DateTime?)null : rd.GetDateTime(7),
        asaas_customer_id = rd.IsDBNull(8) ? null : rd.GetString(8),
        asaas_subscription_id = rd.IsDBNull(9) ? null : rd.GetString(9),
        limites = JsonSerializer.Deserialize<PlanoLimites>(rd.GetString(10))
    });
});

        // ---- POST: iniciar/alterar plano (cria customer + subscription no Asaas) ----
        grupo.MapPost("/assinatura/iniciar", async (IniciarPlanoInput input, ClaimsPrincipal user, NpgsqlDataSource db, AsaasClient asaas) =>
        {
            if (!Planos.Catalogo.ContainsKey(input.Plano))
                return Results.BadRequest(new { mensagem = "Plano inválido." });

            int emp = Auth.Empresa(user);
            var perfil = user.FindFirst("perfil")?.Value;
            if (perfil != "admin" && perfil != "super_admin")
                return Results.Forbid();

            // Verifica se já tem assinatura ativa
            await using var check = db.CreateCommand("SELECT id FROM assinaturas WHERE empresa_id = @emp AND status IN ('ativa','trial')");
            check.Parameters.AddWithValue("emp", emp);
            if (await check.ExecuteScalarAsync() != null)
                return Results.Conflict(new { mensagem = "Já existe assinatura ativa/trial para esta empresa." });

            // Busca dados da empresa para criar customer
            await using var empCmd = db.CreateCommand("SELECT nome, email, telefone, cnpj FROM empresas WHERE id = @emp");
            empCmd.Parameters.AddWithValue("emp", emp);
            await using var empRd = await empCmd.ExecuteReaderAsync();
            if (!await empRd.ReadAsync())
                return Results.NotFound(new { mensagem = "Empresa não encontrada." });

            string empNome = empRd.GetString(0);
            string empEmail = empRd.IsDBNull(1) ? "" : empRd.GetString(1);
            string empPhone = empRd.IsDBNull(2) ? "" : empRd.GetString(2);
            string empCnpj = empRd.IsDBNull(3) ? "" : empRd.GetString(3);

            // 1. Criar customer no Asaas
            var customer = await asaas.CriarCustomerAsync(empNome, empEmail, empPhone, empCnpj, $"emp_{emp}");
            string customerId = customer.GetProperty("id").GetString()!;

            // 2. Criar subscription no Asaas (trial de 14 dias se solicitado)
            DateTime? trialFim = input.TrialDias > 0 ? DateTime.UtcNow.AddDays(input.TrialDias) : null;
            var sub = await asaas.CriarAssinaturaAsync(customerId, input.Plano, 0, 30, trialFim?.AddDays(1));
            string subscriptionId = sub.GetProperty("id").GetString()!;

            // 3. Salvar no banco
            var (valorMensal, limites) = Planos.Obter(input.Plano);
            await using var conn = db.CreateConnection();
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();
            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO assinaturas (empresa_id, asaas_customer_id, asaas_subscription_id, plano, status, valor_centavos, ciclo, trial_fim)
                    VALUES (@emp, @cust, @sub, @plano, @status, @valor, @ciclo, @trial)
                    ON CONFLICT (empresa_id) DO UPDATE SET
                        asaas_customer_id = EXCLUDED.asaas_customer_id,
                        asaas_subscription_id = EXCLUDED.asaas_subscription_id,
                        plano = EXCLUDED.plano,
                        status = EXCLUDED.status,
                        valor_centavos = EXCLUDED.valor_centavos,
                        ciclo = EXCLUDED.ciclo,
                        trial_fim = EXCLUDED.trial_fim,
                        atualizado_em = CURRENT_TIMESTAMP";
                cmd.Parameters.AddWithValue("emp", emp);
                cmd.Parameters.AddWithValue("cust", customerId);
                cmd.Parameters.AddWithValue("sub", subscriptionId);
                cmd.Parameters.AddWithValue("plano", input.Plano);
                cmd.Parameters.AddWithValue("status", trialFim.HasValue ? "trial" : "ativa");
                cmd.Parameters.AddWithValue("valor", (int)(valorMensal * 100));
                cmd.Parameters.AddWithValue("ciclo", "monthly");
                cmd.Parameters.AddWithValue("trial", (object?)trialFim ?? DBNull.Value);

                await cmd.ExecuteNonQueryAsync();
                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return Results.Ok(new { mensagem = $"Plano {input.Plano} iniciado!", trial_fim = trialFim });
        });

        // ---- POST: cancelar assinatura ----
        grupo.MapPost("/assinatura/cancelar", async (ClaimsPrincipal user, NpgsqlDataSource db, AsaasClient asaas) =>
        {
            int emp = Auth.Empresa(user);
            var perfil = user.FindFirst("perfil")?.Value;
            if (perfil != "admin" && perfil != "super_admin")
                return Results.Forbid();

            await using var cmd = db.CreateCommand("SELECT asaas_subscription_id, status FROM assinaturas WHERE empresa_id = @emp");
            cmd.Parameters.AddWithValue("emp", emp);
            await using var rd = await cmd.ExecuteReaderAsync();
            if (!await rd.ReadAsync())
                return Results.NotFound(new { mensagem = "Assinatura não encontrada." });

            string subId = rd.IsDBNull(0) ? "" : rd.GetString(0);
            string statusAtual = rd.GetString(1);

            if (statusAtual == "cancelada")
                return Results.BadRequest(new { mensagem = "Assinatura já cancelada." });

            if (!string.IsNullOrEmpty(subId))
                await asaas.CancelarAssinaturaAsync(subId);

            await using var up = db.CreateCommand(@"
                UPDATE assinaturas SET status = 'cancelada', cancelada_em = CURRENT_TIMESTAMP, fim_vigencia = CURRENT_TIMESTAMP
                WHERE empresa_id = @emp");
            up.Parameters.AddWithValue("emp", emp);
            await up.ExecuteNonQueryAsync();

            return Results.Ok(new { mensagem = "Assinatura cancelada." });
        });

        // ---- WEBHOOK Asaas (público, valida assinatura) ----
        app.MapPost("/api/webhook/asaas", async (HttpRequest req, NpgsqlDataSource db) =>
        {
            // Valida assinatura HMAC (Asaas envia header 'asaas-signature')
            var assinaturaRecebida = req.Headers["asaas-signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(assinaturaRecebida))
                return Results.BadRequest("Assinatura ausente");

            req.EnableBuffering();
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            req.Body.Position = 0;

            var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(asaasCfg.WebhookSecret));
            var hash = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLower();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(hash), Encoding.UTF8.GetBytes(assinaturaRecebida)))
                return Results.Unauthorized();

            // Idempotência: já processou este event_id?
            var evt = JsonDocument.Parse(body).RootElement;
            string eventId = evt.GetProperty("id").GetString()!;
            string eventType = evt.GetProperty("event").GetString()!;

            await using var checkEvt = db.CreateCommand("SELECT 1 FROM asaas_webhook_events WHERE asaas_event_id = @id");
            checkEvt.Parameters.AddWithValue("id", eventId);
            if (await checkEvt.ExecuteScalarAsync() != null)
                return Results.Ok(new { mensagem = "Evento já processado." });

            // Salva evento raw
            await using var logCmd = db.CreateCommand(@"
                INSERT INTO asaas_webhook_events (event_type, asaas_event_id, payload_jsonb)
                VALUES (@type, @id, @payload::jsonb)");
            logCmd.Parameters.AddWithValue("type", eventType);
            logCmd.Parameters.AddWithValue("id", eventId);
            logCmd.Parameters.AddWithValue("payload", body);
            await logCmd.ExecuteNonQueryAsync();

            // Processa eventos relevantes
            try
            {
                await ProcessarEventoAsaas(db, eventType, evt);
                await using var okCmd = db.CreateCommand("UPDATE asaas_webhook_events SET processado_em = CURRENT_TIMESTAMP WHERE asaas_event_id = @id");
                okCmd.Parameters.AddWithValue("id", eventId);
                await okCmd.ExecuteNonQueryAsync();
            }
            catch (Exception ex)
            {
                await using var errCmd = db.CreateCommand("UPDATE asaas_webhook_events SET erro = @err, processado_em = CURRENT_TIMESTAMP WHERE asaas_event_id = @id");
                errCmd.Parameters.AddWithValue("err", ex.Message);
                errCmd.Parameters.AddWithValue("id", eventId);
                await errCmd.ExecuteNonQueryAsync();
                // Não retorna erro pro Asaas (ele reenvia); logamos e retornamos 200
            }

            return Results.Ok(new { recebido = true });
        }).AllowAnonymous();

       // ---- GET: limites do plano atual (para o front bloquear UI) ----
// ---- GET: limites do plano atual (para o front bloquear UI) ----
grupo.MapGet("/limites", async (ClaimsPrincipal user, NpgsqlDataSource db) =>
{
    int emp = Auth.Empresa(user);
    await using var cmd = db.CreateCommand("SELECT * FROM v_assinaturas_ativas WHERE empresa_id = @emp");
    cmd.Parameters.AddWithValue("emp", emp);
    await using var rd = await cmd.ExecuteReaderAsync();

    if (!await rd.ReadAsync())
    {
        // Fallback: plano básico em snake_case (o front espera assim)
        var lim = Planos.Catalogo["basico"].Limites;
        return Results.Ok(new
        {
            tem_assinatura = false,
            limites = new
            {
                max_pacientes = lim.MaxPacientes,
                max_profissionais = lim.MaxProfissionais,
                storage_mb = lim.StorageMB,
                max_versoes_plano = lim.MaxVersoesPlano
            }
        });
    }

    return Results.Ok(new
    {
        tem_assinatura = true,
        plano = rd.GetString(1),
        status = rd.GetString(2),
        limites = JsonSerializer.Deserialize<PlanoLimites>(rd.GetString(8))
    });
});

        // ---- GET: webhook log (apenas super_admin) ----
        grupo.MapGet("/webhook-log", async (ClaimsPrincipal user, NpgsqlDataSource db) =>
        {
            var perfil = user.FindFirst("perfil")?.Value;
            if (perfil != "super_admin")
                return Results.Forbid();

            await using var cmd = db.CreateCommand(@"
                SELECT id, event_type, asaas_event_id, payload_jsonb, processado_em, erro, criado_em
                FROM asaas_webhook_events
                ORDER BY criado_em DESC
                LIMIT 50");
            await using var rd = await cmd.ExecuteReaderAsync();
            var lista = new List<object>();
            while (await rd.ReadAsync())
            {
                lista.Add(new
                {
                    id = rd.GetInt64(0),
                    event_type = rd.GetString(1),
                    asaas_event_id = rd.GetString(2),
                    payload_jsonb = rd.GetFieldValue<JsonElement>(3),
                    processado_em = rd.IsDBNull(4) ? (DateTime?)null : rd.GetDateTime(4),
                    erro = rd.IsDBNull(5) ? null : rd.GetString(5),
                    criado_em = rd.GetDateTime(6)
                });
            }
            return Results.Ok(lista);
        });
    }

    private static async Task ProcessarEventoAsaas(NpgsqlDataSource db, string eventType, JsonElement evt)
    {
        // Exemplos de eventos: SUBSCRIPTION_CREATED, SUBSCRIPTION_UPDATED, SUBSCRIPTION_CANCELLED,
        // PAYMENT_RECEIVED, PAYMENT_OVERDUE, INVOICE_CREATED, etc.
        // O payload tem .payment ou .subscription com customer/externalReference

        string? customerId = null;
        string? subscriptionId = null;

        if (evt.TryGetProperty("subscription", out var sub))
        {
            subscriptionId = sub.GetProperty("id").GetString();
            customerId = sub.GetProperty("customer").GetString();
        }
        else if (evt.TryGetProperty("payment", out var pay))
        {
            customerId = pay.GetProperty("customer").GetString();
        }

        if (string.IsNullOrEmpty(customerId)) return;

        // Acha empresa pelo asaas_customer_id
        await using var empCmd = db.CreateCommand("SELECT empresa_id FROM assinaturas WHERE asaas_customer_id = @cust");
        empCmd.Parameters.AddWithValue("cust", customerId);
        var empObj = await empCmd.ExecuteScalarAsync();
        if (empObj == null) return; // customer não é nosso
        int emp = Convert.ToInt32(empObj);

        switch (eventType)
        {
            case "SUBSCRIPTION_CREATED":
            case "SUBSCRIPTION_UPDATED":
                if (!string.IsNullOrEmpty(subscriptionId))
                {
                    var statusAsaas = sub.GetProperty("status").GetString()?.ToLower();
                    string statusNosso;
                    switch (statusAsaas)
                    {
                        case "active": statusNosso = "ativa"; break;
                        case "past_due": statusNosso = "past_due"; break;
                        case "cancelled": statusNosso = "cancelada"; break;
                        case "pending": statusNosso = "trial"; break;
                        default: statusNosso = "inativa"; break;
                    }
                    DateTime? fimVigencia = null;
                    if (sub.TryGetProperty("nextDueDate", out var nxt) && DateTime.TryParse(nxt.GetString(), out var dt))
                        fimVigencia = dt;

                    await using var up = db.CreateCommand(@"
                        UPDATE assinaturas SET status = @st, fim_vigencia = @fim, atualizado_em = CURRENT_TIMESTAMP
                        WHERE empresa_id = @emp");
                    up.Parameters.AddWithValue("st", statusNosso);
                    up.Parameters.AddWithValue("fim", (object?)fimVigencia ?? DBNull.Value);
                    up.Parameters.AddWithValue("emp", emp);
                    await up.ExecuteNonQueryAsync();
                }
                break;

            case "SUBSCRIPTION_CANCELLED":
                {
                    await using var canc = db.CreateCommand(@"
                        UPDATE assinaturas SET status = 'cancelada', cancelada_em = CURRENT_TIMESTAMP, fim_vigencia = CURRENT_TIMESTAMP
                        WHERE empresa_id = @emp");
                    canc.Parameters.AddWithValue("emp", emp);
                    await canc.ExecuteNonQueryAsync();
                }
                break;

            case "PAYMENT_RECEIVED":
                {
                    // Opcional: reativar se estava past_due
                    await using var pag = db.CreateCommand(@"
                        UPDATE assinaturas SET status = 'ativa', atualizado_em = CURRENT_TIMESTAMP
                        WHERE empresa_id = @emp AND status = 'past_due'");
                    pag.Parameters.AddWithValue("emp", emp);
                    await pag.ExecuteNonQueryAsync();
                }
                break;

            case "PAYMENT_OVERDUE":
                {
                    await using var atr = db.CreateCommand(@"
                        UPDATE assinaturas SET status = 'past_due', atualizado_em = CURRENT_TIMESTAMP
                        WHERE empresa_id = @emp");
                    atr.Parameters.AddWithValue("emp", emp);
                    await atr.ExecuteNonQueryAsync();
                }
                break;
        }
    }
}


public record IniciarPlanoInput(string Plano, int TrialDias = 14);