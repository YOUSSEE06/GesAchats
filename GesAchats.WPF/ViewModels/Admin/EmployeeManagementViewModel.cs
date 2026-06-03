using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
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

public enum AdminPasswordChangeStep
{
    SendCode,
    VerifyCode,
    NewPassword
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

    private string _searchEmployeeText = string.Empty;
    public string SearchEmployeeText
    {
        get => _searchEmployeeText;
        set
        {
            if (_searchEmployeeText != value)
            {
                _searchEmployeeText = value;
                OnPropertyChanged(nameof(SearchEmployeeText));
                EmployeesView?.Refresh();
            }
        }
    }

    private ICollectionView? _employeesView;
    public ICollectionView? EmployeesView
    {
        get => _employeesView;
        private set
        {
            _employeesView = value;
            OnPropertyChanged(nameof(EmployeesView));
        }
    }

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

    // Password Change Properties
    [ObservableProperty]
    private bool _isAdminPasswordChangeVisible;

    private AdminPasswordChangeStep _currentAdminPasswordChangeStep = AdminPasswordChangeStep.SendCode;
    public AdminPasswordChangeStep CurrentAdminPasswordChangeStep
    {
        get => _currentAdminPasswordChangeStep;
        set
        {
            SetProperty(ref _currentAdminPasswordChangeStep, value);
            OnPropertyChanged(nameof(IsPasswordChangeSendCodeStep));
            OnPropertyChanged(nameof(IsPasswordChangeVerifyCodeStep));
            OnPropertyChanged(nameof(IsPasswordChangeNewPasswordStep));
        }
    }

    [ObservableProperty]
    private string _adminPasswordVerificationCode = string.Empty;

    [ObservableProperty]
    private string _newAdminPassword = string.Empty;

    [ObservableProperty]
    private string _confirmAdminPassword = string.Empty;

    [ObservableProperty]
    private bool _adminPasswordCodeVerified;

    [ObservableProperty]
    private int _adminPasswordScore;

    [ObservableProperty]
    private string _adminPasswordStrengthLabel = "Faible";

    [ObservableProperty]
    private bool _hasAdminMinLength;

    [ObservableProperty]
    private bool _hasAdminLowercase;

    [ObservableProperty]
    private bool _hasAdminUppercase;

    [ObservableProperty]
    private bool _hasAdminDigit;

    [ObservableProperty]
    private bool _hasAdminSpecialCharacter;

    [ObservableProperty]
    private bool _isAdminPasswordStrong;

    [ObservableProperty]
    private bool _doAdminPasswordsMatch;

    [ObservableProperty]
    private string _adminPasswordChangeMessage = string.Empty;

    // Visibility Properties
    public bool IsAddEmployeeInfoStep => CurrentAddEmployeeStep == AddEmployeeStep.Info;
    public bool IsAddEmployeeCodeStep => CurrentAddEmployeeStep == AddEmployeeStep.Code;
    public bool IsPasswordStep => CurrentAdminEmailEditStep == AdminEmailEditStep.Password;
    public bool IsNewEmailStep => CurrentAdminEmailEditStep == AdminEmailEditStep.NewEmail;
    public bool IsCodeStep => CurrentAdminEmailEditStep == AdminEmailEditStep.Code;
    public bool IsPasswordChangeSendCodeStep => CurrentAdminPasswordChangeStep == AdminPasswordChangeStep.SendCode;
    public bool IsPasswordChangeVerifyCodeStep => CurrentAdminPasswordChangeStep == AdminPasswordChangeStep.VerifyCode;
    public bool IsPasswordChangeNewPasswordStep => CurrentAdminPasswordChangeStep == AdminPasswordChangeStep.NewPassword;

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

    #region Password Change
    [RelayCommand]
    private void ShowAdminPasswordChange()
    {
        IsAdminPasswordChangeVisible = true;
        CurrentAdminPasswordChangeStep = AdminPasswordChangeStep.SendCode;
        AdminPasswordCodeVerified = false;
        AdminPasswordVerificationCode = string.Empty;
        NewAdminPassword = string.Empty;
        ConfirmAdminPassword = string.Empty;
        AdminPasswordChangeMessage = string.Empty;
    }

    [RelayCommand]
    private void CancelAdminPasswordChange()
    {
        IsAdminPasswordChangeVisible = false;
        CurrentAdminPasswordChangeStep = AdminPasswordChangeStep.SendCode;
        AdminPasswordCodeVerified = false;
        AdminPasswordVerificationCode = string.Empty;
        NewAdminPassword = string.Empty;
        ConfirmAdminPassword = string.Empty;
        AdminPasswordChangeMessage = "Action annulée.";
    }

    [RelayCommand]
    private async Task SendAdminPasswordCode()
    {
        try
        {
            IsBusy = true;
            AdminPasswordChangeMessage = string.Empty;

            if (_userSession.CurrentUser == null)
            {
                AdminPasswordChangeMessage = "Session administrateur introuvable.";
                return;
            }

            // Send verification code using existing service
            var result = await _emailVerificationService.SendVerificationCodeAsync(_userSession.CurrentUser.Email);
            if (result.success)
            {
                CurrentAdminPasswordChangeStep = AdminPasswordChangeStep.VerifyCode;
                AdminPasswordChangeMessage = "Code de vérification envoyé avec succès.";
            }
            else
            {
                AdminPasswordChangeMessage = result.message;
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error sending admin password change code");
            AdminPasswordChangeMessage = "Une erreur est survenue.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task VerifyAdminPasswordCode()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(AdminPasswordVerificationCode))
            {
                AdminPasswordChangeMessage = "Veuillez saisir le code de vérification.";
                return;
            }

            IsBusy = true;
            AdminPasswordChangeMessage = string.Empty;

            if (_userSession.CurrentUser == null)
            {
                AdminPasswordChangeMessage = "Session administrateur introuvable.";
                return;
            }

            var verifyResult = await _emailVerificationService.VerifyCodeOnlyAsync(_userSession.CurrentUser.Email, AdminPasswordVerificationCode);
            if (!verifyResult.success)
            {
                AdminPasswordChangeMessage = verifyResult.message;
                return;
            }

            AdminPasswordCodeVerified = true;
            CurrentAdminPasswordChangeStep = AdminPasswordChangeStep.NewPassword;
            AdminPasswordChangeMessage = "Code vérifié avec succès. Veuillez saisir votre nouveau mot de passe.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error verifying admin password change code");
            AdminPasswordChangeMessage = "Une erreur est survenue.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ConfirmAdminPasswordChange()
    {
        try
        {
            if (!AdminPasswordCodeVerified)
            {
                AdminPasswordChangeMessage = "Le code n'a pas été vérifié.";
                return;
            }

            if (!IsAdminPasswordStrong)
            {
                AdminPasswordChangeMessage = "Le mot de passe doit contenir au moins 8 caractères, une majuscule, une minuscule, un chiffre et un caractère spécial.";
                return;
            }

            if (!DoAdminPasswordsMatch)
            {
                AdminPasswordChangeMessage = "Les mots de passe ne correspondent pas.";
                return;
            }

            IsBusy = true;
            AdminPasswordChangeMessage = string.Empty;

            if (_userSession.CurrentUser == null)
            {
                AdminPasswordChangeMessage = "Session administrateur introuvable.";
                return;
            }

            // Reset password using existing service
            var resetResult = await _emailVerificationService.ResetPasswordAsync(_userSession.CurrentUser.Email, AdminPasswordVerificationCode, NewAdminPassword);
            if (!resetResult.success)
            {
                AdminPasswordChangeMessage = resetResult.message;
                return;
            }

            // Update session user (though password won't change)
            var currentAdmin = await _unitOfWork.Users.GetByIdAsync(_userSession.CurrentUser.Id);
            _userSession.StartSession(currentAdmin);

            IsAdminPasswordChangeVisible = false;
            CurrentAdminPasswordChangeStep = AdminPasswordChangeStep.SendCode;
            AdminPasswordCodeVerified = false;
            AdminPasswordVerificationCode = string.Empty;
            NewAdminPassword = string.Empty;
            ConfirmAdminPassword = string.Empty;
            AdminPasswordChangeMessage = "Mot de passe modifié avec succès.";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error confirming admin password change");
            AdminPasswordChangeMessage = "Une erreur est survenue.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CalculateAdminPasswordStrength()
    {
        var password = NewAdminPassword;
        HasAdminMinLength = password.Length >= 8;
        HasAdminLowercase = Regex.IsMatch(password, @"[a-z]");
        HasAdminUppercase = Regex.IsMatch(password, @"[A-Z]");
        HasAdminDigit = Regex.IsMatch(password, @"[0-9]");
        HasAdminSpecialCharacter = Regex.IsMatch(password, @"[^\w\d]");

        AdminPasswordScore = 0;
        if (HasAdminMinLength) AdminPasswordScore++;
        if (HasAdminLowercase) AdminPasswordScore++;
        if (HasAdminUppercase) AdminPasswordScore++;
        if (HasAdminDigit) AdminPasswordScore++;
        if (HasAdminSpecialCharacter) AdminPasswordScore++;

        AdminPasswordStrengthLabel = AdminPasswordScore switch
        {
            0 or 1 => "Faible",
            2 or 3 => "Moyen",
            4 => "Bon",
            5 => "Fort",
            _ => "Faible"
        };

        IsAdminPasswordStrong = AdminPasswordScore == 5;
        DoAdminPasswordsMatch = NewAdminPassword == ConfirmAdminPassword;
    }

    partial void OnNewAdminPasswordChanged(string value)
    {
        CalculateAdminPasswordStrength();
    }

    partial void OnConfirmAdminPasswordChanged(string value)
    {
        DoAdminPasswordsMatch = NewAdminPassword == ConfirmAdminPassword;
    }
    #endregion

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

    private bool FilterEmployee(object obj)
    {
        if (obj is not EmployeeDto employee)
            return false;

        var search = SearchEmployeeText?.Trim();

        if (string.IsNullOrWhiteSpace(search))
            return true;

        search = search.ToLowerInvariant();

        var fullName = employee.FullName?.ToLowerInvariant() ?? "";
        var email = employee.Email?.ToLowerInvariant() ?? "";
        var role = employee.RoleLabel?.ToLowerInvariant() ?? "";

        return fullName.Contains(search)
            || email.Contains(search)
            || role.Contains(search);
    }

    private void InitializeEmployeesView()
    {
        EmployeesView = CollectionViewSource.GetDefaultView(Employees);
        EmployeesView.Filter = FilterEmployee;
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
            InitializeEmployeesView();
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

    [RelayCommand]
    private async Task DeleteEmployeeAsync(int userId)
    {
        try
        {
            // Get employee to check status
            var employee = Employees.FirstOrDefault(e => e.Id == userId);
            if (employee == null)
            {
                StatusMessage = "Utilisateur introuvable.";
                MessageBox.Show("Utilisateur introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // If employee is active, show error and return!
            if (employee.IsActive)
            {
                var errorMsg = "Impossible de supprimer un utilisateur actif. Désactivez le compte avant de le supprimer.";
                StatusMessage = errorMsg;
                MessageBox.Show(errorMsg, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // If deactivated, show confirmation dialog
            var dialogResult = MessageBox.Show(
                "Voulez-vous vraiment supprimer cet utilisateur désactivé?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (dialogResult != MessageBoxResult.Yes)
            {
                return;
            }

            // Call service to delete!
            IsBusy = true;
            StatusMessage = "Suppression en cours...";
            var result = await _employeeService.DeleteEmployeeAsync(userId);
            if (result.success)
            {
                StatusMessage = result.message;
                MessageBox.Show(result.message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadEmployeesAsync();
            }
            else
            {
                StatusMessage = result.message;
                MessageBox.Show(result.message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error deleting employee {UserId}", userId);
            StatusMessage = "Une erreur est survenue.";
            MessageBox.Show("Une erreur est survenue.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
