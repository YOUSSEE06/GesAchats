using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using Serilog;
using System.Linq;
using System.Text.RegularExpressions;
using BCryptNet = BCrypt.Net.BCrypt;

namespace GesAchats.Core.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;

    public EmailVerificationService(IUnitOfWork unitOfWork, IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _emailService = emailService;
    }

    public async Task<(bool success, string message)> SendVerificationCodeAsync(string email)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return (false, "Veuillez saisir votre email.");
            }

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
            {
                return (false, "Format email invalide.");
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null)
            {
                return (false, "Email invalide ou introuvable.");
            }

            if (!user.IsActive)
            {
                return (false, "Ce compte est désactivé.");
            }

            var code = GenerateRandomCode();
            var codeHash = BCryptNet.HashPassword(code);
            var expiresAt = DateTime.UtcNow.AddMinutes(10);

            var verificationCode = new EmailVerificationCode
            {
                UserId = user.Id,
                CodeHash = codeHash,
                ExpiresAt = expiresAt,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.EmailVerificationCodes.AddAsync(verificationCode);
            await _unitOfWork.CompleteAsync();

            var emailBody = $@"
                <html>
                    <body>
                        <h2>Réinitialisation du mot de passe GesAchats</h2>
                        <p>Bonjour {user.FullName ?? user.Login},</p>
                        <p>Vous avez demandé la réinitialisation de votre mot de passe.</p>
                        <p>Votre code de validation est: <strong>{code}</strong></p>
                        <p>Ce code expirera dans 10 minutes.</p>
                        <p>Si vous n'avez pas demandé cette réinitialisation, ignorez cet email.</p>
                        <p>Cordialement,<br>L'équipe GesAchats</p>
                    </body>
                </html>";

            Log.Information("Sending verification code email to {Email}", email);
            var emailSent = await _emailService.SendEmailAsync(email, "Réinitialisation de votre mot de passe GesAchats", emailBody);
            
            if (!emailSent)
            {
                Log.Error("Failed to send verification code email to {Email}", email);
                return (false, "Échec de l'envoi du code de validation. Veuillez réessayer.");
            }
            
            Log.Information("Verification code email sent successfully to {Email}", email);
            return (true, "Code de validation envoyé avec succès.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending verification code to {Email}", email);
            return (false, "Une erreur est survenue. Veuillez réessayer.");
        }
    }

    public async Task<(bool success, string message)> VerifyCodeOnlyAsync(string email, string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return (false, "Veuillez saisir le code de validation.");
            }

            Log.Information("Starting reset code verification for email: {Email}", email);

            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null)
            {
                Log.Information("Reset code verification failed: user not found for email: {Email}", email);
                return (false, "Email invalide ou introuvable.");
            }

            var verificationCode = await GetLatestValidCodeAsync(user.Id);
            if (verificationCode == null || !BCryptNet.Verify(code, verificationCode.CodeHash))
            {
                Log.Information("Reset code verification failed: invalid or expired code for email: {Email}", email);
                return (false, "Code invalide ou expiré.");
            }

            Log.Information("Reset code verification success for email: {Email}", email);
            return (true, "Code validé avec succès. Veuillez saisir votre nouveau mot de passe.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error verifying code for {Email}", email);
            return (false, "Une erreur est survenue lors de la vérification du code.");
        }
    }

    public async Task<(bool success, string message)> ResetPasswordAsync(string email, string code, string newPassword)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                return (false, "Veuillez saisir le nouveau mot de passe.");
            }

            if (!IsPasswordStrong(newPassword))
            {
                return (false, "Le mot de passe doit contenir au moins 8 caractères, une majuscule, une minuscule, un chiffre et un caractère spécial.");
            }

            var user = await _unitOfWork.Users.GetByEmailAsync(email);
            if (user == null)
            {
                return (false, "Email invalide ou introuvable.");
            }

            var verificationCode = await GetLatestValidCodeAsync(user.Id);
            if (verificationCode == null || !BCryptNet.Verify(code, verificationCode.CodeHash))
            {
                return (false, "Code invalide ou expiré.");
            }

            user.PasswordHash = BCryptNet.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;
            verificationCode.IsUsed = true;

            await _unitOfWork.CompleteAsync();
            return (true, "Mot de passe réinitialisé avec succès.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resetting password for {Email}", email);
            return (false, "Une erreur est survenue. Veuillez réessayer.");
        }
    }

    private async Task<EmailVerificationCode?> GetLatestValidCodeAsync(int userId)
    {
        var codes = await _unitOfWork.EmailVerificationCodes.FindAsync(
            evc => evc.UserId == userId
                && !evc.IsUsed
                && evc.ExpiresAt > DateTime.UtcNow);
        return codes.OrderByDescending(evc => evc.CreatedAt).FirstOrDefault();
    }

    private string GenerateRandomCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    bool IsPasswordStrong(string password)
    {
        if (password.Length < 8) return false;
        if (!password.Any(char.IsUpper)) return false;
        if (!password.Any(char.IsLower)) return false;
        if (!password.Any(char.IsDigit)) return false;
        if (!password.Any(ch => !char.IsLetterOrDigit(ch))) return false;
        return true;
    }

    public async Task<(bool success, string message)> SendEmployeeCreationCodeAsync(string fullName, string email)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            
            // Generate code (reuse existing method!)
            var code = GenerateRandomCode();
            var codeHash = BCryptNet.HashPassword(code);
            var expiresAt = DateTime.UtcNow.AddMinutes(10);

            // Store verification code (using Email and Purpose)
            var verificationCode = new EmailVerificationCode
            {
                UserId = null,
                Email = normalizedEmail,
                Purpose = "CreateUser",
                CodeHash = codeHash,
                ExpiresAt = expiresAt,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.EmailVerificationCodes.AddAsync(verificationCode);
            await _unitOfWork.CompleteAsync();

            // Send email (reuse IEmailService!)
            var emailBody = $@"
                <html>
                    <body>
                        <h2>Création de votre compte GesAchats</h2>
                        <p>Bonjour {fullName},</p>
                        <p>Un administrateur a commencé la création de votre compte GesAchats.</p>
                        <p>Votre code de validation est: <strong>{code}</strong></p>
                        <p>Ce code expirera dans 10 minutes.</p>
                        <p>Si vous n'attendiez pas ce message, contactez l'administrateur.</p>
                        <p>Cordialement,<br>L'équipe GesAchats</p>
                    </body>
                </html>";

            Log.Information("Sending employee creation verification code to {Email}", normalizedEmail);
            var emailSent = await _emailService.SendEmailAsync(normalizedEmail, "Création de votre compte GesAchats", emailBody);
            
            if (!emailSent)
            {
                Log.Error("Failed to send employee creation verification code email to {Email}", normalizedEmail);
                return (false, "Échec de l'envoi du code de validation. Vérifiez la configuration SMTP.");
            }
            
            Log.Information("Employee creation verification code email sent successfully to {Email}", normalizedEmail);
            return (true, "Code de validation envoyé avec succès.");
        }
        catch (MailKit.Net.Smtp.SmtpCommandException ex)
        {
            Log.Error(ex, "SMTP command error sending employee creation code to {Email}", email);
            return (false, $"Erreur SMTP : {ex.Message}");
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            Log.Error(ex, "SMTP authentication error sending employee creation code to {Email}", email);
            return (false, $"Erreur d'authentification SMTP : {ex.Message}");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending employee creation code to {Email}", email);
            return (false, $"Une erreur est survenue lors de l'envoi du code de validation.");
        }
    }

    public async Task<(bool success, string message)> VerifyEmployeeCreationCodeAsync(string email, string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
                return (false, "Veuillez saisir le code de validation.");
            
            var normalizedEmail = email.Trim().ToLowerInvariant();

            // Get valid code (email-based, purpose = CreateUser)
            var codes = await _unitOfWork.EmailVerificationCodes.FindAsync(
                evc => evc.Email == normalizedEmail
                    && evc.Purpose == "CreateUser"
                    && !evc.IsUsed
                    && evc.ExpiresAt > DateTime.UtcNow);
            
            var verificationCode = codes.OrderByDescending(evc => evc.CreatedAt).FirstOrDefault();
            
            if (verificationCode == null || !BCryptNet.Verify(code, verificationCode.CodeHash))
                return (false, "Code invalide ou expiré.");
            
            verificationCode.IsUsed = true;
            await _unitOfWork.CompleteAsync();
            
            return (true, "Code validé avec succès.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error verifying employee creation code for email {Email}", email);
            return (false, "Une erreur est survenue lors de la vérification du code.");
        }
    }

    public async Task<(bool success, string message)> SendChangeEmailCodeAsync(string newEmail, string adminFullName)
    {
        try
        {
            var normalizedEmail = newEmail.Trim().ToLowerInvariant();
            
            var code = GenerateRandomCode();
            var codeHash = BCryptNet.HashPassword(code);
            var expiresAt = DateTime.UtcNow.AddMinutes(10);

            var verificationCode = new EmailVerificationCode
            {
                Email = normalizedEmail,
                Purpose = "ChangeEmail",
                CodeHash = codeHash,
                ExpiresAt = expiresAt,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.EmailVerificationCodes.AddAsync(verificationCode);
            await _unitOfWork.CompleteAsync();

            var emailBody = $@"
                <html>
                    <body>
                        <h2>Modification de votre email GesAchats</h2>
                        <p>Bonjour {adminFullName},</p>
                        <p>Vous avez demandé la modification de votre email sur GesAchats.</p>
                        <p>Votre code de validation est: <strong>{code}</strong></p>
                        <p>Ce code expirera dans 10 minutes.</p>
                        <p>Si vous n'avez pas demandé cette modification, contactez l'administrateur.</p>
                        <p>Cordialement,<br>L'équipe GesAchats</p>
                    </body>
                </html>";

            Log.Information("Sending change email verification code to {Email}", normalizedEmail);
            var emailSent = await _emailService.SendEmailAsync(normalizedEmail, "Modification de votre email GesAchats", emailBody);
            
            if (!emailSent)
            {
                Log.Error("Failed to send change email verification code to {Email}", normalizedEmail);
                return (false, "Échec de l'envoi du code de validation. Vérifiez la configuration SMTP.");
            }
            
            return (true, "Code de validation envoyé au nouvel email.");
        }
        catch (MailKit.Net.Smtp.SmtpCommandException ex)
        {
            Log.Error(ex, "SMTP command error sending change email code to {Email}", newEmail);
            return (false, "Erreur SMTP : " + ex.Message);
        }
        catch (MailKit.Security.AuthenticationException ex)
        {
            Log.Error(ex, "SMTP authentication error sending change email code to {Email}", newEmail);
            return (false, "Erreur d'authentification SMTP : " + ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending change email code to {Email}", newEmail);
            return (false, "Une erreur est survenue lors de l'envoi du code de validation.");
        }
    }

    public async Task<(bool success, string message)> VerifyChangeEmailCodeAsync(string newEmail, string code)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
                return (false, "Veuillez saisir le code de validation.");
            
            var normalizedEmail = newEmail.Trim().ToLowerInvariant();

            var codes = await _unitOfWork.EmailVerificationCodes.FindAsync(
                evc => evc.Email == normalizedEmail
                    && evc.Purpose == "ChangeEmail"
                    && !evc.IsUsed
                    && evc.ExpiresAt > DateTime.UtcNow);
            
            var verificationCode = codes.OrderByDescending(evc => evc.CreatedAt).FirstOrDefault();
            
            if (verificationCode == null || !BCryptNet.Verify(code, verificationCode.CodeHash))
                return (false, "Code invalide ou expiré.");
            
            verificationCode.IsUsed = true;
            await _unitOfWork.CompleteAsync();
            
            return (true, "Code validé avec succès.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error verifying change email code for {Email}", newEmail);
            return (false, "Une erreur est survenue lors de la vérification du code.");
        }
    }
}
