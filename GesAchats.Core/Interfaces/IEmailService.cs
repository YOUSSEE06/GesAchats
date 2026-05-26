namespace GesAchats.Core.Interfaces;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, string? attachmentPath = null);
}
