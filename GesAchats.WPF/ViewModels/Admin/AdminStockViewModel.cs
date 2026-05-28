using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminStockViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockService _stockService;

    private int _totalProducts;
    private decimal _totalStock;
    private int _productsOk;
    private int _productsAlert;
    private int _productsOutOfStock;
    private string _searchText = string.Empty;
    private string _selectedStatus = "Tous";
    private ObservableCollection<Product> _filteredProducts = new();
    private ObservableCollection<string> _statusOptions = new() { "Tous", "OK", "Alerte", "Rupture" };
    private List<Product> _allProducts = new();

    public int TotalProducts
    {
        get => _totalProducts;
        set => SetProperty(ref _totalProducts, value);
    }

    public decimal TotalStock
    {
        get => _totalStock;
        set => SetProperty(ref _totalStock, value);
    }

    public int ProductsOk
    {
        get => _productsOk;
        set => SetProperty(ref _productsOk, value);
    }

    public int ProductsAlert
    {
        get => _productsAlert;
        set => SetProperty(ref _productsAlert, value);
    }

    public int ProductsOutOfStock
    {
        get => _productsOutOfStock;
        set => SetProperty(ref _productsOutOfStock, value);
    }

    public ObservableCollection<Product> FilteredProducts
    {
        get => _filteredProducts;
        set => SetProperty(ref _filteredProducts, value);
    }

    public ObservableCollection<string> StatusOptions
    {
        get => _statusOptions;
        set => SetProperty(ref _statusOptions, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                ApplyFilter();
            }
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                ApplyFilter();
            }
        }
    }

    public ICommand RefreshCommand { get; }

    public AdminStockViewModel(IUnitOfWork unitOfWork, IStockService stockService)
    {
        _unitOfWork = unitOfWork;
        _stockService = stockService;
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        Title = "Tableau de bord du stock (Admin)";
        
        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            // Load all products with their Magasin
            var productsList = await _stockService.GetAllProductsWithMagasinAsync();
            _allProducts = productsList.ToList();

            CalculateStats();
            ApplyFilter();
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

    private void ApplyFilter()
    {
        var filtered = _allProducts.AsEnumerable();

        // Search by designation (contains, case-insensitive)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(p => p.Designation.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Filter by status
        if (SelectedStatus != "Tous")
        {
            filtered = filtered.Where(p =>
                (SelectedStatus == "OK" && p.Etat == StockState.Ok) ||
                (SelectedStatus == "Alerte" && p.Etat == StockState.Alert) ||
                (SelectedStatus == "Rupture" && p.Etat == StockState.OutOfStock));
        }

        FilteredProducts.Clear();
        foreach (var p in filtered)
        {
            FilteredProducts.Add(p);
        }
    }

    private void CalculateStats()
    {
        TotalProducts = _allProducts.Count;
        TotalStock = _allProducts.Sum(p => p.CurrentStock);
        ProductsOk = _allProducts.Count(p => p.Etat == StockState.Ok);
        ProductsAlert = _allProducts.Count(p => p.Etat == StockState.Alert);
        ProductsOutOfStock = _allProducts.Count(p => p.Etat == StockState.OutOfStock);
    }
}
