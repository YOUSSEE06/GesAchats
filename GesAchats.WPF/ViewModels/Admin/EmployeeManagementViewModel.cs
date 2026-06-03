using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using System.Text.RegularExpressions;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using Serilog;
using BCryptNet = BCrypt.Net.BCrypt;

namespace GesAchats.WPF.ViewModels.Admin;

public enum AddEmployeeStep
{
    Info,
    Code
}

public enum AdminEmailEditStep
{
    Password,
    NewEmail,
    Code
}

public partial class EmployeeManagementViewModel : ObservableObject
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;
    private readonly IEmailVerificationService _emailVerificationService;

    [ObservableProperty]
    private bool _isAddEmployeeFormVisible;

    [ObservableProperty]
    private bool _isAdminEmailEditVisible;

    private AddEmployeeStep _currentAddEmployeeStep = AddEmployeeStep.Info;

    public AddEmployeeStep CurrentAddEmployeeStep
    {
        get => _currentAddEmployeeStep;
        set
        {
            SetProperty(ref _currentAddEmployeeStep, value);
            OnPropertyChanged(nameof(IsAddEmployeeInfoStep));
            OnPropertyChanged(nameof(IsAddEmployeeCodeStep));
        }
    }

    private AdminEmailEditStep _currentAdminEmailEditStep = AdminEmailEditStep.Password;

    public AdminEmailEditStep CurrentAdminEmailEditStep
    {
        get => _currentAdminEmailEditStep;
        set
        {
            SetProperty(ref _currentAdminEmailEditStep, value);
            OnPropertyChanged(nameof(IsPasswordStep));
            OnPropertyChanged(nameof(IsNewEmailStep));
            OnPropertyChanged(nameof(IsCodeStep));
        }
    }

    [ObservableProperty]
    private ObservableCollection<EmployeeDto> _employees = [];

    [ObservableProperty]
    private ObservableCollection<Role> _availableEmployeeRoles = [];

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _addEmployeeMessage = string.Empty;

    [ObservableProperty]
    private string _newEmployeeFullName = string.Empty;

    [ObservableProperty]
    private string _newEmployeeEmail = string.Empty;

    [ObservableProperty]
    private Role? _selectedEmployeeRole;
    
    [ObservableProperty]
    private int? _selectedEmployeeRoleId;

    [ObservableProperty]
    private string _newEmployeeVerificationCode = string.Empty;

    [ObservableProperty]
    private string _adminFullName = string.Empty;

    [ObservableProperty]
    private string _adminEmail = string.Empty;

    [ObservableProperty]
    private string _adminRoleDisplay = "Administrateur";

    [ObservableProperty]
    private string _adminSessionError = string.Empty;

    [ObservableProperty]
    private string _currentAdminPassword = string.Empty;

    [ObservableProperty]
    private string _newAdminEmail = string.Empty;

    [ObservableProperty]
    private string _adminEmailVerificationCode = string.Empty;

    [ObservableProperty]
    private bool _adminPasswordVerified;

    [ObservableProperty]
    private string _adminEmailEditMessage = string.Empty;

    public bool IsAddEmployeeInfoStep => CurrentAddEmployeeStep == AddEmployeeStep.Info;
    public bool IsAddEmployeeCodeStep => CurrentAddEmployeeStep == AddEmployeeStep.Code;
    public bool IsPasswordStep => CurrentAdminEmailEditStep == AdminEmailEditStep.Password;
    public bool IsNewEmailStep => CurrentAdminEmailEditStep == AdminEmailEditStep.NewEmail;
    public bool IsCodeStep => CurrentAdminEmailEditStep == AdminEmailEditStep.Code;

    public EmployeeManagementViewModel(IEmployeeService employeeService, ILogger logger, IUnitOfWork unitOfWork, IUserSession userSession, IEmailVerificationService emailVerificationService)
    {
        _employeeService = employeeService;
        _logger = logger;
        _unitOfWork = unitOfWork;
        _userSession = userSession;
        _emailVerificationService = emailVerificationService;
        // Load roles and admin data on initialization
        LoadRolesAsync().ConfigureAwait(false);
        LoadAdminData();
    }

    private void LoadAdminData()
    {
        try
        {
            if (_userSession.CurrentUser == null)
            {
                AdminSessionError = "Session administrateur introuvable.";
                _logger.Warning("Admin session not found when loading employee management page");
                return;
            }

            AdminFullName = _userSession.CurrentUser.FullName ?? _userSession.CurrentUser.Login;
            AdminEmail = _userSession.CurrentUser.Email;
            AdminRoleDisplay = "Administrateur";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading admin data");
            AdminSessionError = "Erreur lors du chargement des données de session.";
        }
    }

    [RelayCommand]
    private void ShowAdminEmailEdit()
    {
        IsAdminEmailEditVisible = true;
        CurrentAdminEmailEditStep = AdminEmailEditStep.Password;
        AdminPasswordVerified = false;
        CurrentAdminPassword = string.Empty;
        NewAdminEmail = string.Empty;
        AdminEmailVerificationCode = string.Empty;
        AdminEmailEditMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelAdminEmailEdit()
    {
        IsAdminEmailEditVisible = false;
        CurrentAdminEmailEditStep = AdminEmailEditStep.Password;
        AdminPasswordVerified = false;
        CurrentAdminPassword = string.Empty;
        NewAdminEmail = string.Empty;
        AdminEmailVerificationCode = string.Empty;
        AdminEmailEditMessage = "Action annulée.";
    }

    [RelayCommand]
    private async Task VerifyAdminPassword()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(CurrentAdminPassword))
            {
                AdminEmailEditMessage = "Veuillez saisir le mot de passe actuel.";
                return;
            }

            IsBusy = true;
            AdminEmailEditMessage = string.Empty;

            if (_userSession.CurrentUser == null)
            {
                AdminEmailEditMessage = "Session administrateur introuvable.";
                return;
            }

            // Get fresh user from DB to make sure we have the latest PasswordHash
            var currentAdmin = await _unitOfWork.Users.GetByIdAsync(_userSession.CurrentUser.Id);
            if (currentAdmin == null)
            {
                AdminEmailEditMessage = "Session administrateur introuvable.";
                return;
            }

            if (!BCryptNet.Verify(CurrentAdminPassword, currentAdmin.PasswordHash))
            {
                AdminEmailEditMessage = "Mot de passe incorrect.";
                return;
            }

            AdminPasswordVerified = true;
            CurrentAdminEmailEditStep = AdminEmailEditStep.NewEmail;
            AdminEmailEditMessage = "Mot de passe vérifié avec succès.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error verifying admin password");
            AdminEmailEditMessage = "Une erreur est survenue.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SendAdminEmailChangeCode()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewAdminEmail))
            {
                AdminEmailEditMessage = "Veuillez saisir un email.";
                return;
            }

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(NewAdminEmail))
            {
                AdminEmailEditMessage = "Format email invalide.";
                return;
            }

            IsBusy = true;
            AdminEmailEditMessage = string.Empty;
            var normalizedNewEmail = NewAdminEmail.Trim().ToLowerInvariant();

            if (_userSession.CurrentUser == null)
            {
                AdminEmailEditMessage = "Session administrateur introuvable.";
                return;
            }

            if (normalizedNewEmail == _userSession.CurrentUser.Email.Trim().ToLowerInvariant())
            {
                AdminEmailEditMessage = "Le nouvel email doit être différent de l'email actuel.";
                return;
            }

            // Check for duplicate email (excluding current admin)
            var existingUser = await _unitOfWork.Users.GetByEmailAsync(normalizedNewEmail);
            if (existingUser != null && existingUser.Id != _userSession.CurrentUser.Id)
            {
                AdminEmailEditMessage = "Cet email existe déjà.";
                return;
            }

            // Send verification code using existing service
            var result = await _emailVerificationService.SendChangeEmailCodeAsync(normalizedNewEmail, AdminFullName);
            if (result.success)
            {
                AdminEmailEditMessage = result.message;
                CurrentAdminEmailEditStep = AdminEmailEditStep.Code;
            }
            else
            {
                AdminEmailEditMessage = result.message;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending admin email change code");
            AdminEmailEditMessage = "Une erreur est survenue.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmAdminEmailChange()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(AdminEmailVerificationCode))
            {
                AdminEmailEditMessage = "Veuillez saisir le code de validation.";
                return;
            }

            IsBusy = true;
            AdminEmailEditMessage = string.Empty;
            var normalizedNewEmail = NewAdminEmail.Trim().ToLowerInvariant();

            // Verify the code
            var verifyResult = await _emailVerificationService.VerifyChangeEmailCodeAsync(normalizedNewEmail, AdminEmailVerificationCode);
            if (!verifyResult.success)
            {
                AdminEmailEditMessage = verifyResult.message;
                return;
            }

            // Update the admin's email
            if (_userSession.CurrentUser == null)
            {
                AdminEmailEditMessage = "Session administrateur introuvable.";
                return;
            }

            var currentAdmin = await _unitOfWork.Users.GetByIdAsync(_userSession.CurrentUser.Id);
            if (currentAdmin == null)
            {
                AdminEmailEditMessage = "Session administrateur introuvable.";
                return;
            }

            currentAdmin.Email = normalizedNewEmail;
            // If login is the same as email, update login too
            if (currentAdmin.Login == _userSession.CurrentUser.Email)
            {
                currentAdmin.Login = normalizedNewEmail;
            }
            currentAdmin.UpdatedAt = DateTime.UtcNow;

            _unitOfWork.Users.Update(currentAdmin);
            await _unitOfWork.CompleteAsync();

            // Update the session user
            _userSession.StartSession(currentAdmin);

            // Update the ViewModel properties
            AdminEmail = normalizedNewEmail;

            // Hide the edit panel
            IsAdminEmailEditVisible = false;
            CurrentAdminEmailEditStep = AdminEmailEditStep.Password;
            AdminPasswordVerified = false;
            CurrentAdminPassword = string.Empty;
            NewAdminEmail = string.Empty;
            AdminEmailVerificationCode = string.Empty;
            AdminEmailEditMessage = "Email administrateur modifié avec succès.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error confirming admin email change");
            AdminEmailEditMessage = "Une erreur est survenue.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadRolesAsync()
    {
        try
        {
            var allRoles = await _unitOfWork.Roles.GetAllAsync();
            // Filter out ADMIN role
            AvailableEmployeeRoles = new ObservableCollection<Role>(
                allRoles.Where(r => r.Code != "ADMIN"));
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading roles");
        }
    }

    [RelayCommand]
    private async Task LoadEmployeesAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Chargement des employés...";
            // Load roles first if not loaded
            if (AvailableEmployeeRoles.Count == 0)
            {
                await LoadRolesAsync();
            }
            var employees = await _employeeService.GetEmployeesAsync();
            Employees = new ObservableCollection<EmployeeDto>(employees);
            StatusMessage = $"Chargement terminé. {Employees.Count} employés.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading employees");
            StatusMessage = "Erreur lors du chargement des employés.";
            MessageBox.Show("Une erreur est survenue lors du chargement des employés.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void ShowAddEmployeeForm()
    {
        IsAddEmployeeFormVisible = true;
        ResetAddEmployeeForm();
    }

    [RelayCommand]
    private void HideAddEmployeeForm()
    {
        IsAddEmployeeFormVisible = false;
        ResetAddEmployeeForm();
    }

    private void ResetAddEmployeeForm()
    {
        CurrentAddEmployeeStep = AddEmployeeStep.Info;
        NewEmployeeFullName = string.Empty;
        NewEmployeeEmail = string.Empty;
        SelectedEmployeeRole = null;
        SelectedEmployeeRoleId = null;
        NewEmployeeVerificationCode = string.Empty;
        AddEmployeeMessage = string.Empty;
    }



    [RelayCommand]
    private async Task SendEmployeeCodeAsync()
    {
        try
        {
            // Validate all required fields
            if (string.IsNullOrWhiteSpace(NewEmployeeFullName))
            {
                AddEmployeeMessage = "Veuillez saisir le nom complet.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewEmployeeEmail))
            {
                AddEmployeeMessage = "Veuillez saisir l'email professionnel.";
                return;
            }

            if (SelectedEmployeeRoleId is null)
            {
                AddEmployeeMessage = "Veuillez sélectionner un rôle.";
                return;
            }

            IsBusy = true;
            AddEmployeeMessage = "Envoi du code de validation...";
            
            // Normalize email
            var normalizedEmail = NewEmployeeEmail.Trim().ToLowerInvariant();
            
            var result = await _employeeService.SendCreateUserCodeAsync(NewEmployeeFullName, normalizedEmail, SelectedEmployeeRoleId.Value);
            if (result.success)
            {
                AddEmployeeMessage = result.message;
                CurrentAddEmployeeStep = AddEmployeeStep.Code;
            }
            else
            {
                AddEmployeeMessage = result.message;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending employee verification code to {Email}", NewEmployeeEmail);
            AddEmployeeMessage = "Impossible d'envoyer le code de validation. Vérifiez la configuration SMTP.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task CreateEmployeeAsync()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(NewEmployeeVerificationCode))
            {
                AddEmployeeMessage = "Veuillez saisir le code de validation.";
                return;
            }

            IsBusy = true;
            AddEmployeeMessage = "Création du compte employé...";
            
            // Normalize email
            var normalizedEmail = NewEmployeeEmail.Trim().ToLowerInvariant();
            
            var result = await _employeeService.CreateEmployeeAsync(normalizedEmail, NewEmployeeVerificationCode);
            if (result.success)
            {
                AddEmployeeMessage = string.Empty;
                MessageBox.Show(result.message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                HideAddEmployeeForm();
                await LoadEmployeesAsync();
            }
            else
            {
                AddEmployeeMessage = result.message;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating employee for {Email}", NewEmployeeEmail);
            AddEmployeeMessage = "Une erreur est survenue.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleEmployeeStatusAsync(int userId)
    {
        try
        {
            var employee = Employees.FirstOrDefault(e => e.Id == userId);
            if (employee == null) return;

            if (employee.IsActive)
            {
                var dialogResult = MessageBox.Show(
                    "Voulez-vous vraiment désactiver ce compte?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);
                if (dialogResult != MessageBoxResult.Yes)
                    return;

                await _employeeService.DeactivateEmployeeAsync(userId);
                StatusMessage = "Compte désactivé avec succès.";
                MessageBox.Show("Compte désactivé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                var hasActive = await _employeeService.HasAnotherActiveUserWithRoleAsync(userId, employee.RoleCode);
                if (hasActive)
                {
                    var dialogResult = MessageBox.Show(
                        $"Un autre compte avec le rôle {employee.RoleLabel} est déjà actif. Voulez-vous désactiver l'ancien compte et activer celui-ci?",
                        "Confirmation",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);
                    if (dialogResult != MessageBoxResult.Yes)
                        return;
                }

                await _employeeService.ActivateEmployeeAsync(userId);
                StatusMessage = "Compte activé avec succès.";
                MessageBox.Show("Compte activé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            await LoadEmployeesAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error toggling employee status {UserId}", userId);
            StatusMessage = "Erreur lors du changement de statut du compte.";
            MessageBox.Show("Une erreur est survenue lors du changement de statut du compte.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void EditEmployee(int userId)
    {
        // For now, just show a message!
        MessageBox.Show("Fonctionnalité d'édition à implémenter.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
