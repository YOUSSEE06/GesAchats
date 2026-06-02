using GesAchats.Core.Services;
using GesAchats.Core.Helpers;
using Serilog;

namespace GesAchats.Tests;

public class EmailServiceTest
{
    [Fact]
    public async Task SendTestEmail_ShouldSucceed()
    {
        // Configure Serilog for testing
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateLogger();

        // Arrange - Hardcode SMTP settings since we know them from appsettings.json
        var smtpSettings = new SmtpSettings
        {
            Host = "smtp.gmail.com",
            Port = 587,
            Username = "youssefoularbi630@gmail.com",
            Password = "xivf jido kujz dfiv",
            FromEmail = "youssefoularbi630@gmail.com",
            FromName = "GesAchats",
            UseStartTls = true
        };

        var emailService = new EmailService(smtpSettings);

        var testEmail = "youssefoularbi628@gmail.com";
        var subject = "Test d'envoi d'email - GesAchats";
        var body = @"
            <html>
                <body>
                    <h1>Test réussi ! 🎉</h1>
                    <p>Ceci est un email de test depuis l'application GesAchats.</p>
                    <p>Si vous recevez ce message, le service d'email fonctionne correctement.</p>
                    <p>Cordialement,<br>L'équipe GesAchats</p>
                </body>
            </html>
        ";

        // Act
        Log.Information($"Tentative d'envoi d'email à {testEmail}...");
        Log.Information($"SMTP Host: {smtpSettings.Host}");
        Log.Information($"SMTP Port: {smtpSettings.Port}");
        Log.Information($"SMTP Username: {smtpSettings.Username}");

        var result = await emailService.SendEmailAsync(testEmail, subject, body);

        // Assert
        Log.Information($"Résultat de l'envoi: {(result ? "SUCCÈS" : "ÉCHEC")}");
        Assert.True(result, "L'email de test n'a pas pu être envoyé");
    }
}
