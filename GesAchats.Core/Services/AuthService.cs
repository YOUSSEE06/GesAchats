using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public interface IAuthService
{
    Task<User?> LoginAsync(string username, string password);
    Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword);
}

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;

    public AuthService(IUnitOfWork unitOfWork, IUserSession userSession)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
    }

    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _unitOfWork.Users.GetByLoginAsync(username);
        
        // Simulation simple de hash (à remplacer par BCrypt plus tard)
        if (user != null && user.PasswordHash == password && user.IsActive)
        {
            user.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            
            _userSession.StartSession(user);
            return user;
        }

        return null;
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user != null && user.PasswordHash == oldPassword)
        {
            user.PasswordHash = newPassword;
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            return true;
        }
        return false;
    }
}
