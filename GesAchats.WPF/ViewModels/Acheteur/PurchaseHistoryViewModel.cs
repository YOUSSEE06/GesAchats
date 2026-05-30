using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class ProductHistoryViewModel : BaseViewModel
{
    public Product Product { get; }
    public int PurchaseCount { get; set; }
    public decimal AveragePrice { get; set; }
    public string BestSupplier { get; set; } = "N/A";

    public ProductHistoryViewModel(Product product)
    {
        Product = product;
    }
}

public class PurchaseHistoryViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceAnalysisService _priceService;
    private readonly INavigationService _navigationService;
    private ProductHistoryViewModel? _selectedProduct;
    private string _searchText = string.Empty;
    private List<ProductHistoryViewModel> _allProducts = new();

    public ObservableCollection<ProductHistoryViewModel> Products { get; } = new ObservableCollection<ProductHistoryViewModel>();
    public ObservableCollection<PurchaseOrderDetail> DetailedHistory { get; } = new ObservableCollection<PurchaseOrderDetail>();

    public ICommand NavigateToStatsCommand { get; }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterProducts();
            }
        }
    }

    public ProductHistoryViewModel? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                _ = LoadDetailedHistory();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public PurchaseHistoryViewModel(IUnitOfWork unitOfWork, IPriceAnalysisService priceService, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _priceService = priceService;
        _navigationService = navigationService;
        Title = "Historique des Achats";

        NavigateToStatsCommand = new RelayCommand(
            _ => _navigationService.NavigateTo("ProductStats", SelectedProduct?.Product),
            _ => SelectedProduct != null
        );

        _ = LoadProducts();
    }

    private async Task LoadProducts()
    {
        IsBusy = true;
        try
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            _allProducts.Clear();
            Products.Clear();
            foreach (var p in products)
            {
                var history = await _priceService.GetPriceHistoryForProductAsync(p.Id);
                var vm = new ProductHistoryViewModel(p)
                {
                    PurchaseCount = history.Count(),
                    AveragePrice = history.Any() ? history.Average(x => x.UnitPriceHT) : 0
                };
                _allProducts.Add(vm);
                Products.Add(vm);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterProducts()
    {
        var filtered = _allProducts.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(p => 
                p.Product.Designation.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }
        
        Products.Clear();
        foreach (var p in filtered)
        {
            Products.Add(p);
        }
    }

    private async Task LoadDetailedHistory()
    {
        if (SelectedProduct == null)
        {
            DetailedHistory.Clear();
            return;
        }

        IsBusy = true;
        try
        {
            var history = await _priceService.GetPriceHistoryForProductAsync(SelectedProduct.Product.Id);
            DetailedHistory.Clear();
            foreach (var item in history.OrderByDescending(x => x.PurchaseOrder.OrderDate))
            {
                DetailedHistory.Add(item);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
