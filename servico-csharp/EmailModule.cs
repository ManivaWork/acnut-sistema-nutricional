// EmailModule.cs - Serviço de envio de e-mails
// Adicione ao Program.cs: builder.Services.AddSingleton<EmailService>();

using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;

namespace ACNutri.Email;

public record EmailConfig
{
    public string Provider { get; init; } = "smtp";
    public SmtpConfig Smtp { get; init; } = new();
    public string FrontendBaseUrl { get; init; } = "http://localhost:5500";
}

public record SmtpConfig
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string UserName { get; init; } = "";
    public string Password { get; init; } = "";
    public bool EnableSsl { get; init; } = true;
    public string FromEmail { get; init; } = "";
    public string FromName { get; init; } = "ACNUTRI";
}

public interface IEmailService
{
    Task<bool> EnviarConviteAsync(string emailDestino, string empresaNome, string tokenConvite, string perfil, string nivelNutricao);
    Task<bool> EnviarConvitePacienteAsync(string emailPaciente, string nutricionistaNome, string tokenConvite, string empresaNome);
    Task<bool> EnviarEmailAsync(string to, string subject, string htmlBody);
}

public class EmailService : IEmailService
{
    private readonly EmailConfig _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailConfig> config, ILogger<EmailService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<bool> EnviarConviteAsync(string emailDestino, string empresaNome, string tokenConvite, string perfil, string nivelNutricao)
    {
        var urlConvite = $"{_config.FrontendBaseUrl.TrimEnd('/')}/convite/{tokenConvite}";

        var subject = $"Convite para ingressar na {empresaNome} - ACNUTRI";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #0f172a; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .btn {{ display: inline-block; background: #3b82f6; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; margin-top: 20px; }}
        .btn:hover {{ background: #2563eb; }}
        .info {{ background: white; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #3b82f6; }}
        .footer {{ text-align: center; color: #64748b; font-size: 14px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍎 ACNUTRI</h1>
            <p>Sistema de Nutrição Inteligente</p>
        </div>
        <div class='content'>
            <h2>Você foi convidado(a)!</h2>
            <p>Olá! Você recebeu um convite para fazer parte da equipe da <strong>{empresaNome}</strong> no ACNUTRI.</p>

            <div class='info'>
                <p><strong>Perfil:</strong> {TraduzirPerfil(perfil)}</p>
                <p><strong>Acesso Nutrição:</strong> {TraduzirNivel(nivelNutricao)}</p>
                <p><strong>Validade:</strong> 7 dias</p>
            </div>

            <p>Clique no botão abaixo para aceitar o convite e criar sua conta:</p>

            <p style='text-align: center;'>
                <a href='{urlConvite}' class='btn'>Aceitar Convite e Criar Conta</a>
            </p>

            <p style='font-size: 14px; color: #64748b;'>
                Ou copie este link no navegador:<br>
                <a href='{urlConvite}'>{urlConvite}</a>
            </p>

            <p style='font-size: 14px; color: #ef4444;'>
                ⚠️ Este convite expira em 7 dias. Após expirar, solicite um novo convite ao administrador.
            </p>
        </div>
        <div class='footer'>
            <p>Este e-mail foi enviado automaticamente pelo ACNUTRI.<br>
            Se você não esperava este convite, pode ignorar este e-mail.</p>
        </div>
    </div>
</body>
</html>";

        return await EnviarEmailAsync(emailDestino, subject, htmlBody);
    }

    public async Task<bool> EnviarEmailAsync(string to, string subject, string htmlBody)
    {
        try
        {
            if (_config.Provider != "smtp" || string.IsNullOrWhiteSpace(_config.Smtp.Host))
            {
                _logger.LogWarning("Email não configurado (Provider: {Provider}, Host: {Host}). Simulando envio para: {To}",
                    _config.Provider, _config.Smtp.Host, to);
                _logger.LogInformation("Assunto: {Subject}", subject);
                return true; // Simula sucesso em desenvolvimento
            }

            using var client = new SmtpClient(_config.Smtp.Host, _config.Smtp.Port)
            {
                Credentials = new NetworkCredential(_config.Smtp.UserName, _config.Smtp.Password),
                EnableSsl = _config.Smtp.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_config.Smtp.FromEmail, _config.Smtp.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            message.To.Add(to);

            await client.SendMailAsync(message);
            _logger.LogInformation("E-mail enviado com sucesso para: {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar e-mail para: {To}", to);
            return false;
        }
    }

    private static string TraduzirPerfil(string perfil) => perfil switch
    {
        "super_admin" => "Super Administrador",
        "admin" => "Administrador",
        "profissional" => "Profissional de Nutrição",
        "atendente" => "Atendente/Recepção",
        _ => perfil
    };

    private static string TraduzirNivel(string nivel) => nivel switch
    {
        "total" => "Acesso Total (criar/editar/excluir)",
        "escrita" => "Escrita (criar/editar próprios)",
        "leitura" => "Apenas Leitura",
        _ => nivel
    };

    public async Task<bool> EnviarConvitePacienteAsync(string emailPaciente, string nutricionistaNome, string tokenConvite, string empresaNome)
    {
        var urlConvite = $"{_config.FrontendBaseUrl.TrimEnd('/')}/convite-paciente/{tokenConvite}";

        var subject = $"Convite para acompanhamento nutricional - {nutricionistaNome}";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background: #6c34eb; color: white; padding: 30px; text-align: center; border-radius: 8px 8px 0 0; }}
        .content {{ background: #f8fafc; padding: 30px; border-radius: 0 0 8px 8px; }}
        .btn {{ display: inline-block; background: #6c34eb; color: white; padding: 14px 28px; text-decoration: none; border-radius: 6px; font-weight: 600; margin-top: 20px; }}
        .btn:hover {{ background: #5b2bc7; }}
        .info {{ background: white; padding: 20px; border-radius: 6px; margin: 20px 0; border-left: 4px solid #6c34eb; }}
        .footer {{ text-align: center; color: #64748b; font-size: 14px; margin-top: 30px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🍎 ACNUTRI</h1>
            <p>Sistema de Nutrição Inteligente</p>
        </div>
        <div class='content'>
            <h2>Convite para acompanhamento nutricional</h2>
            <p>Olá!</p>
            <p><strong>{nutricionistaNome}</strong> te convidou para ser seu paciente no <strong>ACNUTRI</strong>.</p>

            <div class='info'>
                <p><strong>Nutricionista:</strong> {nutricionistaNome}</p>
                <p><strong>Clínica/Empresa:</strong> {empresaNome}</p>
                <p><strong>Validade:</strong> 7 dias</p>
            </div>

            <p>Clique no botão abaixo para aceitar o convite e acessar sua área de paciente:</p>

            <p style='text-align: center;'>
                <a href='{urlConvite}' class='btn'>Aceitar Convite</a>
            </p>

            <p style='font-size: 14px; color: #64748b;'>
                Ou copie este link no navegador:<br>
                <a href='{urlConvite}'>{urlConvite}</a>
            </p>

            <p style='font-size: 14px; color: #ef4444;'>
                ⚠️ Este convite expira em 7 dias. Após expirar, solicite um novo convite ao seu nutricionista.
            </p>
        </div>
        <div class='footer'>
            <p>Este e-mail foi enviado automaticamente pelo ACNUTRI.<br>
            Se você não esperava este convite, pode ignorar este e-mail.</p>
        </div>
    </div>
</body>
</html>";

        return await EnviarEmailAsync(emailPaciente, subject, htmlBody);
    }
}