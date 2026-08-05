using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IAuthService
{
    Task<User?> LoginAsync(string loginOrEmail, string password);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
    Task<bool> VerifyCredentialsAsync(int userId, string password);
    Task<(bool success, string message)> UpdatePasswordAsync(int userId, string newPassword);
    Task<(bool success, string message)> UpdateEmailAsync(int userId, string newEmail);
}
