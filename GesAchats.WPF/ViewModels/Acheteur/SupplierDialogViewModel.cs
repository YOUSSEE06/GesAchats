using System.Text.RegularExpressions;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class SupplierDialogViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private Supplier? _supplier;
    private string _errorMessage = string.Empty;
    private bool _isEditMode;

    public Supplier? Supplier => _supplier;

    private string _companyName = string.Empty;
    private string? _contactName;
    private string? _email;
    private string? _phone;
    private string? _address;
    private string? _city;
    private bool _isActive = true;

    public string CompanyName
    {
        get => _companyName;
        set => SetProperty(ref _companyName, value);
    }

    public string? ContactName
    {
        get => _contactName;
        set => SetProperty(ref _contactName, value);
    }

    public string? Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string? Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string? Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string? City
    {
        get => _city;
        set => SetProperty(ref _city, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action<Supplier?>? OnResult;

    public SupplierDialogViewModel(IUnitOfWork unitOfWork, Supplier? supplier = null)
    {
        _unitOfWork = unitOfWork;
        _supplier = supplier;

        if (supplier != null)
        {
            IsEditMode = true;
            Title = "Modifier le Fournisseur";
            CompanyName = supplier.CompanyName;
            ContactName = supplier.ContactName;
            Email = supplier.Email;
            Phone = supplier.Phone;
            Address = supplier.Address;
            City = supplier.City;
            IsActive = supplier.IsActive;
        }
        else
        {
            IsEditMode = false;
            Title = "Ajouter un Fournisseur";
        }

        SaveCommand = new RelayCommand(async _ => await ExecuteSave(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => OnResult?.Invoke(null));
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(CompanyName);

    private string? ValidateInputs()
    {
        var trimmedCompanyName = CompanyName.Trim();
        if (string.IsNullOrWhiteSpace(trimmedCompanyName))
        {
            return "Le nom de la société est obligatoire.";
        }

        if (!string.IsNullOrWhiteSpace(Email))
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            if (!emailRegex.IsMatch(Email.Trim()))
            {
                return "L'adresse email n'est pas valide.";
            }
        }

        if (!string.IsNullOrWhiteSpace(Phone))
        {
            var phoneRegex = new Regex(@"^\+?[0-9\s]+$");
            if (!phoneRegex.IsMatch(Phone.Trim()))
            {
                return "Le numéro de téléphone ne doit contenir que des chiffres et éventuellement un '+' au début.";
            }
        }

        return null;
    }

    private string CleanString(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        return Regex.Replace(input.Trim(), @"\s+", " ");
    }

    private async Task<bool> CheckForDuplicates(string cleanedCompanyName, string? cleanedEmail)
    {
        var existingSuppliers = await _unitOfWork.Suppliers.FindAsync(s =>
            (s.CompanyName.ToLower() == cleanedCompanyName.ToLower() ||
             (!string.IsNullOrWhiteSpace(cleanedEmail) && s.Email != null && s.Email.ToLower() == cleanedEmail.ToLower())) &&
            (!IsEditMode || s.Id != _supplier!.Id));

        return existingSuppliers.Any();
    }

    private async Task ExecuteSave()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var validationError = ValidateInputs();
            if (validationError != null)
            {
                ErrorMessage = validationError;
                return;
            }

            var cleanedCompanyName = CleanString(CompanyName);
            var cleanedContactName = CleanString(ContactName);
            var cleanedEmail = !string.IsNullOrWhiteSpace(Email) ? CleanString(Email) : null;
            var cleanedPhone = !string.IsNullOrWhiteSpace(Phone) ? CleanString(Phone) : null;
            var cleanedAddress = !string.IsNullOrWhiteSpace(Address) ? CleanString(Address) : null;
            var cleanedCity = !string.IsNullOrWhiteSpace(City) ? CleanString(City) : null;

            var hasDuplicate = await CheckForDuplicates(cleanedCompanyName, cleanedEmail);
            if (hasDuplicate)
            {
                ErrorMessage = "Un fournisseur avec le même nom de société ou email existe déjà.";
                return;
            }

            if (IsEditMode && _supplier != null)
            {
                _supplier.CompanyName = cleanedCompanyName;
                _supplier.ContactName = cleanedContactName;
                _supplier.Email = cleanedEmail;
                _supplier.Phone = cleanedPhone;
                _supplier.Address = cleanedAddress;
                _supplier.City = cleanedCity;
                _supplier.IsActive = IsActive;
                _supplier.UpdatedAt = DateTime.UtcNow;

                _unitOfWork.Suppliers.Update(_supplier);
            }
            else
            {
                _supplier = new Supplier
                {
                    CompanyName = cleanedCompanyName,
                    ContactName = cleanedContactName,
                    Email = cleanedEmail,
                    Phone = cleanedPhone,
                    Address = cleanedAddress,
                    City = cleanedCity,
                    IsActive = IsActive,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Suppliers.AddAsync(_supplier);
            }

            await _unitOfWork.CompleteAsync();
            System.Windows.Application.Current.Dispatcher.Invoke(() => OnResult?.Invoke(_supplier));
        }
        catch (Exception ex)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => ErrorMessage = $"Erreur lors de l'enregistrement : {ex.Message}");
        }
        finally
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
        }
    }
}
