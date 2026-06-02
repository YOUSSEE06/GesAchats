using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IAuthService
{
    Task<User?> LoginAsync(string loginOrEmail, string password);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
}
