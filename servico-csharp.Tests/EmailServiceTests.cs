using System;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Xunit;
using ACNutri.Email;

namespace ACNutri.Tests;

public class EmailServiceTests
{
    private readonly Mock<IOptions<EmailConfig>> _configMock;
    private readonly Mock<ILogger<EmailService>> _loggerMock;
    private readonly EmailService _emailService;
    private readonly HttpClient _httpClient;

    public EmailServiceTests()
    {
        _configMock = new Mock<IOptions<EmailConfig>>();
        _loggerMock = new Mock<ILogger<EmailService>>();
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Set environment to Development for tests
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");

        // Setup config com valores válidos para simulação
        var emailConfig = new EmailConfig
        {
            Provider = "smtp",
            Smtp = new SmtpConfig
            {
                Host = "", // Vazio = simula envio
                Port = 587,
                UserName = "teste@email.com",
                Password = "senha",
                EnableSsl = true,
                FromEmail = "teste@email.com",
                FromName = "ACNUTRI"
            },
            FrontendBaseUrl = "http://localhost:5500"
        };

        _configMock.Setup(c => c.Value).Returns(emailConfig);
        _emailService = new EmailService(_configMock.Object, _loggerMock.Object, _httpClient);
    }

    [Fact]
    public async Task EnviarEmailAsync_SemConfigSMTP_DeveSimularEnvioERetornarTrue()
    {
        // Arrange - config sem host SMTP
        var configSemSmtp = new EmailConfig
        {
            Provider = "smtp",
            Smtp = new SmtpConfig { Host = "" },
            FrontendBaseUrl = "http://localhost:5500"
        };
        _configMock.Setup(c => c.Value).Returns(configSemSmtp);

        // Act
        var result = await _emailService.EnviarEmailAsync("teste@email.com", "Assunto", "<p>Corpo</p>");

        // Assert
        result.Should().BeTrue();
        _loggerMock.Verify(
            l => l.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Email não configurado")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task EnviarConviteAsync_DeveRetornarTrue()
    {
        // Act
        var result = await _emailService.EnviarConviteAsync(
            "novo@email.com", "Minha Empresa", "token123", "profissional", "leitura");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task EnviarConvitePacienteAsync_DeveRetornarTrue()
    {
        // Act
        var result = await _emailService.EnviarConvitePacienteAsync(
            "paciente@email.com", "Dr. Nutri", "token456", "Clínica Vida");

        // Assert
        result.Should().BeTrue();
    }
}