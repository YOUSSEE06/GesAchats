namespace GesAchats.Core.Interfaces;

public interface IEmailVerificationService
{
    Task<(bool success, string message)> SendVerificationCodeAsync(string email);
    Task<(bool success, string message)> VerifyCodeOnlyAsync(string email, string code);
    Task<(bool success, string message)> ResetPasswordAsync(string email, string code, string newPassword);
    Task<(bool success, string message)> SendEmployeeCreationCodeAsync(string fullName, string email);
    Task<(bool success, string message)> VerifyEmployeeCreationCodeAsync(string email, string code);
    Task<(bool success, string message)> SendChangeEmailCodeAsync(string newEmail, string adminFullName);
    Task<(bool success, string message)> VerifyChangeEmailCodeAsync(string newEmail, string code);
}
