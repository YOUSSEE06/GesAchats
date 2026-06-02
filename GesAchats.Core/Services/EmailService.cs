using GesAchats.Core.Interfaces;
using GesAchats.Core.Helpers;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using Serilog;

namespace GesAchats.Core.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _smtpSettings;

    public EmailService(SmtpSettings smtpSettings)
    {
        _smtpSettings = smtpSettings;
        // Log smtp settings on initialization (without password!)
        Log.Information("EmailService initialized with: Host={Host}, Port={Port}, Username={Username}, FromEmail={FromEmail}, UseStartTls={UseStartTls}",
            smtpSettings.Host, smtpSettings.Port, smtpSettings.Username, smtpSettings.FromEmail, smtpSettings.UseStartTls);
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, string? attachmentPath = null)
    {
        try
        {
            Log.Information("Preparing to send email to {To} with subject '{Subject}'", to, subject);
            
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = body
            };

            if (!string.IsNullOrEmpty(attachmentPath) && File.Exists(attachmentPath))
            {
                bodyBuilder.Attachments.Add(attachmentPath);
            }

            email.Body = bodyBuilder.ToMessageBody();

            Log.Information("Connecting to SMTP server {Host}:{Port}", _smtpSettings.Host, _smtpSettings.Port);
            using var smtpClient = new SmtpClient();
            var secureSocketOptions = _smtpSettings.UseStartTls
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await smtpClient.ConnectAsync(
                _smtpSettings.Host,
                _smtpSettings.Port,
                secureSocketOptions);

            Log.Information("SMTP connected, authenticating as {Username}", _smtpSettings.Username);
            await smtpClient.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);

            Log.Information("Authenticated, sending email");
            await smtpClient.SendAsync(email);
            
            await smtpClient.DisconnectAsync(true);

            Log.Information("Email sent successfully to {To}", to);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send email to {To}", to);
            return false;
        }
    }
}
