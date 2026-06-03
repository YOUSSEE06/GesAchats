using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.DTOs;
using Serilog;
using BCryptNet = BCrypt.Net.BCrypt;
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
    private readonly ILogger _logger;
    private readonly Dictionary<string, PendingEmployeeInfo> _pendingEmployeeInfo = new();

    public EmployeeService(IUnitOfWork unitOfWork, ILogger logger, IEmailVerificationService emailVerificationService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _emailVerificationService = emailVerificationService;
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

            // Generate random temporary password!
            var tempPassword = GenerateRandomPassword();

            // Create user NOW!
            var user = new User
            {
                FullName = pendingInfo.FullName,
                Email = normalizedEmail,
                Login = normalizedEmail,
                RoleId = role.Id,
                PasswordHash = BCryptNet.HashPassword(tempPassword),
                IsActive = !hasExistingActive,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.UserRepository.AddAsync(user);
            await _unitOfWork.CompleteAsync();

            // Remove from pending!
            _pendingEmployeeInfo.Remove(normalizedEmail);

            return (true, "Compte employé créé avec succès. L'employé doit utiliser la réinitialisation du mot de passe pour définir son mot de passe.");
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating employee for email {Email}", email);
            return (false, "Une erreur est survenue.");
        }
    }

    private string GenerateRandomPassword()
    {
        // Generate a random password that meets security requirements!
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numberChars = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        var random = new Random();
        var password = new List<char>
        {
            upperChars[random.Next(upperChars.Length)],
            lowerChars[random.Next(lowerChars.Length)],
            numberChars[random.Next(numberChars.Length)],
            specialChars[random.Next(specialChars.Length)]
        };

        // Add 4 more random characters
        var allChars = lowerChars + upperChars + numberChars + specialChars;
        for (int i = 0; i < 4; i++)
        {
            password.Add(allChars[random.Next(allChars.Length)]);
        }

        // Shuffle the password!
        return new string(password.OrderBy(c => random.Next()).ToArray());
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
}
