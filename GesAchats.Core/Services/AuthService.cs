using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using Serilog;
using BCryptNet = BCrypt.Net.BCrypt;

namespace GesAchats.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;

    public AuthService(IUnitOfWork unitOfWork, IUserSession userSession)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
    }

    public async Task<User?> LoginAsync(string loginOrEmail, string password)
    {
        if (string.IsNullOrWhiteSpace(loginOrEmail) || string.IsNullOrWhiteSpace(password))
            return null;

        try
        {
            var user = await _unitOfWork.Users.GetByLoginOrEmailAsync(loginOrEmail);
            
            if (user == null)
                return null;

            if (!user.IsActive)
                throw new InvalidOperationException("Ce compte est désactivé.");

            // TODO: Ajouter vérification EmailVerified quand la propriété existe
            // if (!user.IsEmailVerified)
            //     throw new InvalidOperationException("Veuillez valider votre email avant de vous connecter.");

            bool passwordValid = false;

            try
            {
                // First try BCrypt verification
                passwordValid = BCryptNet.Verify(password, user.PasswordHash);
            }
            catch
            {
                // If BCrypt verification fails (e.g., hash is not a valid BCrypt hash), fall back to plain text (for backward compatibility)
                passwordValid = user.PasswordHash == password;
            }

            if (!passwordValid)
                return null;

            // If password was plain text, update it to BCrypt for security
            if (user.PasswordHash == password)
            {
                user.PasswordHash = BCryptNet.HashPassword(password);
                Log.Information("Mise à jour du mot de passe en BCrypt pour l'utilisateur {UserId}", user.Id);
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            
            _userSession.StartSession(user);
            return user;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors de la connexion pour {LoginOrEmail}", loginOrEmail);
            throw;
        }
    }

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        try
        {
            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            
            bool passwordValid = false;
            try
            {
                passwordValid = BCryptNet.Verify(oldPassword, user.PasswordHash);
            }
            catch
            {
                passwordValid = user.PasswordHash == oldPassword;
            }

            if (user != null && passwordValid)
            {
                user.PasswordHash = BCryptNet.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CompleteAsync();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors du changement de mot de passe pour l'utilisateur {UserId}", userId);
            return false;
        }
    }
}
