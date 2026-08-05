using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.DTOs;
using GesAchats.Core.Security;
using Serilog;
using System.Text.RegularExpressions;

namespace GesAchats.Core.Services;

// Temporary storage class for pending info
internal class PendingEmployeeInfo
{
    public string FullName { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public int UserId { get; set; }
}

public class EmployeeService : IEmployeeService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly SupabaseAuthClient _authClient;
    private readonly ILogger _logger;
    private readonly Dictionary<string, PendingEmployeeInfo> _pendingEmployeeInfo = new();

    public EmployeeService(IUnitOfWork unitOfWork, ILogger logger, IEmailVerificationService emailVerificationService, SupabaseAuthClient authClient)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailVerificationService = emailVerificationService;
        _authClient = authClient;
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeesAsync()
    {
        try
        {
            var users = await _unitOfWork.UserRepository.GetAllIncludingAsync(u => u.Role);
            return users.Where(u => u.Role.Code != "ADMIN")
                        .Select(u => new EmployeeDto
                        {
                            Id = u.Id,
                            FullName = u.FullName ?? "",
                            Email = u.Email,
                            RoleCode = u.Role.Code,
                            RoleLabel = u.Role.Label,
                            IsEmailVerified = true, // No column yet in User entity, default true
                            IsActive = u.IsActive,
                            CreatedAt = u.CreatedAt,
                            LastLoginAt = u.LastLoginAt
                        }).ToList();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting employees");
            throw;
        }
    }

    public async Task<bool> HasAnotherActiveUserWithRoleAsync(int userId, string roleCode)
    {
        try
        {
            var activeUser = await _unitOfWork.UserRepository.GetActiveUserByRoleAsync(roleCode, userId);
            return activeUser != null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error checking active user for role {RoleCode}", roleCode);
            throw;
        }
    }

    public async Task ActivateEmployeeAsync(int userId)
    {
        try
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.Warning("User {UserId} not found for activation", userId);
                throw new KeyNotFoundException($"User {userId} not found");
            }

            // Get user role
            var userWithRole = (await _unitOfWork.UserRepository.GetAllIncludingAsync(u => u.Role))
                               .FirstOrDefault(u => u.Id == userId);
            if (userWithRole == null)
                throw new KeyNotFoundException($"User {userId} not found");

            // Check if another active user exists with same role
            var activeUser = await _unitOfWork.UserRepository.GetActiveUserByRoleAsync(userWithRole.Role.Code, userId);

            if (activeUser != null)
            {
                // Use a transaction to ensure both changes happen or neither do!
                using var transaction = await _unitOfWork.BeginTransactionAsync();
                try
                {
                    // Deactivate old user!
                    activeUser.IsActive = false;
                    activeUser.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.UserRepository.Update(activeUser);

                    // Activate new user!
                    user.IsActive = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.UserRepository.Update(user);

                    await _unitOfWork.SaveChangesAsync();
                    await transaction.CommitAsync();
                    _logger.Information("Successfully swapped active user for role {Role} from {OldUserId} to {NewUserId}", userWithRole.Role.Code, activeUser.Id, userId);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            else
            {
                // No existing active user, just activate!
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.UserRepository.Update(user);
                await _unitOfWork.SaveChangesAsync();
                _logger.Information("Successfully activated user {UserId}", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error activating user {UserId}", userId);
            throw;
        }
    }

    public async Task DeactivateEmployeeAsync(int userId)
    {
        try
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.Warning("User {UserId} not found for deactivation", userId);
                throw new KeyNotFoundException($"User {userId} not found");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.UserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync();
            _logger.Information("Successfully deactivated user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error deactivating user {UserId}", userId);
            throw;
        }
    }

    public async Task<(bool success, string message)> SendCreateUserCodeAsync(string fullName, string email, int roleId)
    {
        try
        {
            // Validate inputs!
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Veuillez saisir le nom complet.");

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(email))
                return (false, "Format email invalide.");

            if (roleId == 0)
                return (false, "Veuillez sélectionner un rôle.");

            // Check if email already exists!
            var normalizedEmail = email.Trim().ToLowerInvariant();
            var existingUser = await _unitOfWork.UserRepository.GetByEmailAsync(normalizedEmail);
            if (existingUser != null)
                return (false, "Cet email existe déjà.");

            // Check if role exists!
            var role = await _unitOfWork.Roles.GetByIdAsync(roleId);
            if (role == null)
                return (false, "Rôle invalide.");

            // Store pending info temporarily (NO USER CREATION YET)!
            _pendingEmployeeInfo[normalizedEmail] = new PendingEmployeeInfo
            {
                FullName = fullName,
                RoleId = roleId
            };

            // Use existing email verification service!
            return await _emailVerificationService.SendEmployeeCreationCodeAsync(fullName, normalizedEmail);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending create user code to {Email}", email);
            return (false, $"Erreur : {ex.Message} | Détails : {ex.InnerException?.Message}");
        }
    }

    public async Task<(bool success, string message)> CreateEmployeeAsync(string email, string code)
    {
        try
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            
            // Retrieve pending info!
            if (!_pendingEmployeeInfo.TryGetValue(normalizedEmail, out var pendingInfo))
                return (false, "Aucune demande de création en cours pour cet email.");

            // Verify code using existing service!
            var verifyResult = await _emailVerificationService.VerifyEmployeeCreationCodeAsync(normalizedEmail, code);
            if (!verifyResult.success)
                return (false, verifyResult.message);

            // Get role!
            var role = await _unitOfWork.Roles.GetByIdAsync(pendingInfo.RoleId);
            if (role == null)
                return (false, "Rôle invalide.");

            // First check if there's existing active user for that role!
            var hasExistingActive = await _unitOfWork.UserRepository.GetActiveUserByRoleAsync(role.Code) != null;

            // Create user NOW!
            var user = new User
            {
                FullName = pendingInfo.FullName,
                Email = normalizedEmail,
                Login = normalizedEmail,
                RoleId = role.Id,
                PasswordHash = string.Empty, // Supabase Auth gère le mot de passe.
                IsActive = !hasExistingActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.UserRepository.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            // Crée le compte correspondant dans Supabase Auth : l'employé pourra se connecter
            // immédiatement avec le mot de passe temporaire (Confirm email désactivé).
            var tempPassword = GenerateRandomPassword();
            var signUp = await _authClient.SignUpAsync(normalizedEmail, tempPassword);
            if (signUp.success && signUp.userId.HasValue)
            {
                user.SupabaseAuthId = signUp.userId;
                user.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.CompleteAsync();
                _logger.Information("Compte Supabase créé pour {Email}", normalizedEmail);
            }
            else if (!signUp.alreadyExists)
            {
                _logger.Warning("Création du compte Supabase échouée pour {Email} : {Message}", normalizedEmail, signUp.message);
            }

            // Remove from pending!
            _pendingEmployeeInfo.Remove(normalizedEmail);

            return (true, "Compte employé créé avec succès. Le mot de passe temporaire a été créé chez Supabase : remettez-le à l'employé.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating employee for email {Email}", email);
            return (false, "Une erreur est survenue.");
        }
    }

    private string GenerateRandomPassword()
    {
        return PasswordGenerator.Generate();
    }

    public async Task<(bool success, string message)> DeleteEmployeeAsync(int userId)
    {
        try
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return (false, "Utilisateur introuvable.");
            }

            // Check if user is admin
            var userWithRole = (await _unitOfWork.UserRepository.GetAllIncludingAsync(u => u.Role))
                               .FirstOrDefault(u => u.Id == userId);
            if (userWithRole?.Role.Code == "ADMIN")
            {
                return (false, "Impossible de supprimer un administrateur.");
            }

            // Check if user is active
            if (user.IsActive)
            {
                return (false, "Impossible de supprimer un utilisateur actif. Désactivez le compte avant de le supprimer.");
            }

            // Delete user!
            _unitOfWork.UserRepository.Remove(user);
            await _unitOfWork.CompleteAsync();

            return (true, "Utilisateur supprimé avec succès.");
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
        {
            _logger.Error(ex, "Error deleting employee {UserId} because of foreign key constraints", userId);
            return (false, "Impossible de supprimer cet utilisateur car il est lié à d'autres données.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error deleting employee {UserId}", userId);
            return (false, "Une erreur est survenue.");
        }
    }

    /// <summary>
    /// Crée les comptes Supabase Auth manquants pour les utilisateurs existants (migration).
    /// Retourne la liste (email, mot de passe temporaire) à transmettre aux employés.
    /// </summary>
    public async Task<List<(string Email, string TemporaryPassword, string FullName)>> SyncSupabaseAccountsAsync()
    {
        var results = new List<(string Email, string TemporaryPassword, string FullName)>();

        var users = await _unitOfWork.UserRepository.GetAllIncludingAsync(u => u.Role);
        var toSync = users.Where(u => u.SupabaseAuthId == null).ToList();

        if (toSync.Count == 0)
        {
            _logger.Information("SyncSupabaseAccounts: aucun compte à synchroniser.");
            return results;
        }

        foreach (var user in toSync)
        {
            var password = GenerateRandomPassword();
            var signUp = await _authClient.SignUpAsync(user.Email, password);

            if (signUp.success && signUp.userId.HasValue)
            {
                user.SupabaseAuthId = signUp.userId;
                user.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.UserRepository.Update(user);
                results.Add((user.Email, password, user.FullName ?? user.Login));
            }
            else if (signUp.alreadyExists)
            {
                // Déjà présent chez Supabase : tente une liaison avec le mot de passe généré.
                var signIn = await _authClient.SignInWithPasswordAsync(user.Email, password);
                if (signIn.success && signIn.userId.HasValue)
                {
                    user.SupabaseAuthId = signIn.userId;
                    user.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.UserRepository.Update(user);
                    results.Add((user.Email, password, user.FullName ?? user.Login));
                }
                else
                {
                    _logger.Warning("Compte Supabase existant pour {Email} avec un autre mot de passe : « mot de passe oublié » requis.", user.Email);
                }
            }
            else
            {
                _logger.Warning("Échec de création Supabase pour {Email} : {Message}", user.Email, signUp.message);
            }
        }

        await _unitOfWork.CompleteAsync();
        return results;
    }
}
