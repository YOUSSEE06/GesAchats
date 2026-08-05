using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using Serilog;

namespace GesAchats.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;
    private readonly SupabaseAuthClient _authClient;

    public AuthService(IUnitOfWork unitOfWork, IUserSession userSession, SupabaseAuthClient authClient)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
        _authClient = authClient;
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

            // Le mot de passe est validé par Supabase Auth (jamais stocké dans l'application).
            var signIn = await _authClient.SignInWithPasswordAsync(user.Email, password);
            if (!signIn.success)
                return null;

            // Lie le profil métier au compte Supabase si ce n'est pas déjà fait.
            if (user.SupabaseAuthId == null && signIn.userId.HasValue)
            {
                user.SupabaseAuthId = signIn.userId;
                user.UpdatedAt = DateTime.UtcNow;
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();

            _userSession.AccessToken = signIn.accessToken;
            _userSession.RefreshToken = signIn.refreshToken;
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
            if (user == null)
                return false;

            var signIn = await _authClient.SignInWithPasswordAsync(user.Email, oldPassword);
            if (!signIn.success)
                return false;

            var update = await _authClient.UpdatePasswordAsync(signIn.accessToken, newPassword);
            return update.success;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur lors du changement de mot de passe pour l'utilisateur {UserId}", userId);
            return false;
        }
    }

    /// <summary>Vérifie les identifiants d'un utilisateur connecté (nécessaire avant une action sensible).</summary>
    public async Task<bool> VerifyCredentialsAsync(int userId, string password)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return false;

        var signIn = await _authClient.SignInWithPasswordAsync(user.Email, password);
        return signIn.success;
    }

    /// <summary>Change l'email de l'utilisateur connecté à l'aide de son jeton de session.</summary>
    public async Task<(bool success, string message)> UpdateEmailAsync(int userId, string newEmail)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return (false, "Utilisateur introuvable.");

        var token = _userSession.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            return (false, "Session expirée. Veuillez vous reconnecter.");

        var update = await _authClient.UpdateEmailAsync(token, newEmail.Trim().ToLowerInvariant());
        if (update.success)
        {
            var normalizedNewEmail = newEmail.Trim().ToLowerInvariant();
            if (string.Equals(user.Login, user.Email, StringComparison.OrdinalIgnoreCase))
            {
                user.Login = normalizedNewEmail;
            }
            user.Email = normalizedNewEmail;
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
            _userSession.StartSession(user);
        }
        return update;
    }

    /// <summary>Change le mot de passe de l'utilisateur connecté à l'aide de son jeton de session.</summary>
    public async Task<(bool success, string message)> UpdatePasswordAsync(int userId, string newPassword)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return (false, "Utilisateur introuvable.");

        var token = _userSession.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            return (false, "Session expirée. Veuillez vous reconnecter.");

        var update = await _authClient.UpdatePasswordAsync(token, newPassword);
        if (update.success)
        {
            user.PasswordHash = string.Empty; // plus jamais utilisé : Supabase gère le mot de passe.
            user.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.CompleteAsync();
        }
        return update;
    }

    /// <summary>
    /// Modifie le nom complet de l'utilisateur connecté (table locale Users)
    /// et synchronise le nom dans les métadonnées Supabase (best effort).
    /// </summary>
    public async Task<(bool success, string message)> UpdateFullNameAsync(int userId, string newFullName)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
            return (false, "Utilisateur introuvable.");

        var name = newFullName.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Le nom complet est requis.");

        var token = _userSession.AccessToken;
        if (string.IsNullOrWhiteSpace(token))
            return (false, "Session expirée. Veuillez vous reconnecter.");

        var meta = await _authClient.UpdateUserMetadataAsync(token, name);
        if (!meta.success)
        {
            // Non bloquant : la table locale reste la source de vérité de l'application.
            Log.Warning("Supabase metadata update failed for user {UserId}: {Message}", userId, meta.message);
        }

        user.FullName = name;
        user.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.CompleteAsync();

        _userSession.StartSession(user);
        return (true, "Nom complet modifié avec succès.");
    }
}