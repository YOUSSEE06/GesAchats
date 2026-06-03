using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using Serilog;

namespace GesAchats.WPF.ViewModels.Admin;

public enum AddEmployeeStep
{
    Info,
    Code
}

public partial class EmployeeManagementViewModel : ObservableObject
{
    private readonly IEmployeeService _employeeService;
    private readonly ILogger _logger;
    private readonly IUnitOfWork _unitOfWork;

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

    public bool IsAddEmployeeInfoStep => CurrentAddEmployeeStep == AddEmployeeStep.Info;
    public bool IsAddEmployeeCodeStep => CurrentAddEmployeeStep == AddEmployeeStep.Code;

    public EmployeeManagementViewModel(IEmployeeService employeeService, ILogger logger, IUnitOfWork unitOfWork)
    {
        _employeeService = employeeService;
        _logger = logger;
        _unitOfWork = unitOfWork;
        // Load roles on initialization
        LoadRolesAsync().ConfigureAwait(false);
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
    private void ShowAdminEmailEdit()
    {
        IsAdminEmailEditVisible = true;
    }

    [RelayCommand]
    private void HideAdminEmailEdit()
    {
        IsAdminEmailEditVisible = false;
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
