using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public class EmailService : IEmailService
{
    public async Task<bool> SendEmailAsync(string to, string subject, string body, string? attachmentPath = null)
    {
        // Simulation d'envoi d'email
        await Task.Delay(500);
        return true;
    }
}
