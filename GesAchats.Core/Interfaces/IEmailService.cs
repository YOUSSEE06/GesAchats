namespace GesAchats.Core.Interfaces;

public interface IEmailService
{
    Task<(bool success, string? error)> SendEmailAsync(string to, string subject, string body, string? attachmentPath = null);
}
