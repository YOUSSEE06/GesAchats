namespace GesAchats.Core.Interfaces;

public interface IEmailVerificationService
{
    Task<(bool success, string message)> SendVerificationCodeAsync(string email);
    Task<(bool success, string message)> VerifyCodeOnlyAsync(string email, string code);
    Task<(bool success, string message)> ResetPasswordAsync(string email, string code, string newPassword);
}
