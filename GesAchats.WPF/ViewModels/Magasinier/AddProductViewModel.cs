using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class AddProductViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;
    private string _designation = string.Empty;
    private string _unit = "pcs";
    private decimal _minimumStock = 10;
    private string _errorMessage = string.Empty;

    public string Designation { get => _designation; set => SetProperty(ref _designation, value); }
    public string Unit { get => _unit; set => SetProperty(ref _unit, value); }
    public decimal MinimumStock { get => _minimumStock; set => SetProperty(ref _minimumStock, value); }
    public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
    public decimal InitialStock => 0;

    public List<string> Units { get; } = new List<string> { "pcs", "sac", "m³", "kg", "barre", "l" };

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action<Product?>? OnResult;

    public AddProductViewModel(IUnitOfWork unitOfWork, IUserSession userSession)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
        Title = "Ajouter un nouveau produit";

        SaveCommand = new RelayCommand(async _ => await ExecuteSave(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => OnResult?.Invoke(null));
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Designation) && MinimumStock >= 0;

    private async Task ExecuteSave()
    {
        IsBusy = true;
        ErrorMessage = string.Empty;
        try
        {
            var product = new Product
            {
                Designation = Designation,
                Unit = Unit,
                CurrentStock = 0,
                MinimumStock = MinimumStock,
                IsNew = true,
                CreatedBy = _userSession.CurrentUser?.FullName ?? "Magasinier",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _unitOfWork.Products.AddAsync(product);
            await _unitOfWork.CompleteAsync();

            System.Windows.Application.Current.Dispatcher.Invoke(() => OnResult?.Invoke(product));
        }
        catch (Exception ex)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => ErrorMessage = $"Erreur : {ex.Message}");
        }
        finally
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() => IsBusy = false);
        }
    }
}
