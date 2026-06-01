using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class StockExitViewModel : BaseViewModel
{
    private readonly IStockService _stockService;
    private readonly IUserSession _userSession;
    private readonly IUnitOfWork _unitOfWork;

    private Product? _selectedProduct;
    private decimal _quantity;
    private DateTime _exitDate = DateTime.Now;
    private string? _reason;
    private string _searchText = string.Empty;
    private DateTime? _filterDate;
    private int _totalExits;
    private decimal _totalQuantityExited;

    public ObservableCollection<Product> Products { get; } = new();
    public ObservableCollection<StockExit> StockExits { get; } = new();
    private List<StockExit> _allExits = new();

    public int TotalExits
    {
        get => _totalExits;
        set => SetProperty(ref _totalExits, value);
    }

    public decimal TotalQuantityExited
    {
        get => _totalQuantityExited;
        set => SetProperty(ref _totalQuantityExited, value);
    }

    public Product? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public decimal Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public DateTime ExitDate
    {
        get => _exitDate;
        set => SetProperty(ref _exitDate, value);
    }

    public string? Reason
    {
        get => _reason;
        set => SetProperty(ref _reason, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilters();
        }
    }

    public DateTime? FilterDate
    {
        get => _filterDate;
        set
        {
            if (SetProperty(ref _filterDate, value))
                ApplyFilters();
        }
    }

    public ICommand SaveExitCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CancelExitCommand { get; }
    public ICommand OpenAddDialogCommand { get; }

    public StockExitViewModel(IStockService stockService, IUserSession userSession, IUnitOfWork unitOfWork)
    {
        _stockService = stockService;
        _userSession = userSession;
        _unitOfWork = unitOfWork;

        Title = "Sortie des besoins";

        SaveExitCommand = new RelayCommand(async _ => await SaveExitAsync(), _ => CanSaveExit());
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        CancelExitCommand = new RelayCommand(async p => await CancelExitAsync(p as StockExit));
        OpenAddDialogCommand = new RelayCommand(_ => ExecuteOpenAddDialog());

        _ = LoadDataAsync();
    }

    private void ExecuteOpenAddDialog()
    {
        // Reset form first
        Quantity = 0;
        Reason = string.Empty;
        SelectedProduct = Products.FirstOrDefault();

        var dialog = new Views.Magasinier.StockExits.StockExitDialog
        {
            DataContext = this,
            Owner = Application.Current.MainWindow
        };
        dialog.ShowDialog();
    }

    private bool CanSaveExit()
    {
        // On autorise la sauvegarde même si le motif est vide pour le moment, ou on s'assure que les conditions sont souples
        return SelectedProduct != null && Quantity > 0 && Quantity <= SelectedProduct.CurrentStock;
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var products = await _stockService.GetAllProductsAsync();
            Products.Clear();
            foreach (var p in products.OrderBy(p => p.Designation))
            {
                Products.Add(p);
            }

            var exits = await _stockService.GetAllStockExitsAsync();
            _allExits = exits.ToList();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allExits.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(e => e.Product != null && e.Product.Designation.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (FilterDate.HasValue)
        {
            filtered = filtered.Where(e => e.ExitDate.Date == FilterDate.Value.Date);
        }

        var filteredList = filtered.ToList();
        StockExits.Clear();
        foreach (var e in filteredList)
        {
            StockExits.Add(e);
        }

        // Calculate statistics
        TotalExits = filteredList.Count;
        TotalQuantityExited = filteredList.Sum(e => e.Quantity);
    }

    private async Task SaveExitAsync()
    {
        if (SelectedProduct == null) return;

        IsBusy = true;
        try
        {
            var exit = new StockExit
            {
                ProductId = SelectedProduct.Id,
                Quantity = Quantity,
                ExitDate = ExitDate.ToUniversalTime(),
                Reason = Reason,
                CreatedById = _userSession.CurrentUser?.Id ?? 1 // Fallback for tests
            };

            bool success = await _stockService.RecordStockExitAsync(exit);
            if (success)
            {
                MessageBox.Show("Sortie de stock enregistrée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Find and close the dialog
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.Magasinier.StockExits.StockExitDialog)
                    {
                        window.DialogResult = true;
                        window.Close();
                        break;
                    }
                }
                
                await LoadDataAsync();
            }
            else
            {
                MessageBox.Show("Erreur lors de l'enregistrement. Vérifiez le stock disponible.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CancelExitAsync(StockExit? exit)
    {
        if (exit == null) return;

        var result = MessageBox.Show("Voulez-vous vraiment annuler cette sortie de stock ? Le stock sera réincrémenté.", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            // Update stock back
            await _stockService.UpdateStockAsync(exit.ProductId, exit.Quantity);
            
            // Delete exit record
            _unitOfWork.StockExits.Remove(exit);
            await _unitOfWork.CompleteAsync();

            await LoadDataAsync();
            MessageBox.Show("Sortie annulée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'annulation : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
