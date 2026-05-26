using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class StockProductViewModel : BaseViewModel
{
    public Product Product { get; }
    
    public StockProductViewModel(Product product)
    {
        Product = product;
    }

    public string StatusColor => Product.IsNew ? "#9C27B0" : 
                                Product.CurrentStock <= 0 ? "#F44336" : 
                                Product.CurrentStock <= Product.MinimumStock ? "#FFC107" : "#4CAF50";
    
    public string StatusIcon => Product.IsNew ? "✨" : 
                               Product.CurrentStock <= 0 ? "🔴" : 
                               Product.CurrentStock <= Product.MinimumStock ? "🟡" : "🟢";
}

public class StockAnalysisViewModel : BaseViewModel
{
    private readonly IStockService _stockService;
    private string _searchText = string.Empty;
    private string _selectedFilter = "Tous";

    public ObservableCollection<StockProductViewModel> Products { get; } = new ObservableCollection<StockProductViewModel>();
    
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) FilterProducts(); }
    }

    public List<string> Filters { get; } = new List<string> { "Tous", "Rupture", "Sous minimum", "Normal", "Nouveaux" };
    
    public string SelectedFilter
    {
        get => _selectedFilter;
        set { if (SetProperty(ref _selectedFilter, value)) FilterProducts(); }
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateNeedCommand { get; }
    public ICommand AddProductCommand { get; set; }

    public Action? OnCreateNeedRequested { get; set; }

    private List<Product> _allProducts = new List<Product>();

    public StockAnalysisViewModel(IStockService stockService)
    {
        _stockService = stockService;
        Title = "Analyse du Stock";
        
        RefreshCommand = new RelayCommand(async _ => await LoadData());
        CreateNeedCommand = new RelayCommand(_ => OnCreateNeedRequested?.Invoke());
        AddProductCommand = new RelayCommand(_ => { }); // Sera injecté par la vue

        _ = LoadData();
    }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var products = await _stockService.GetAllProductsAsync();
            _allProducts = products.ToList();
            FilterProducts();
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
            filtered = filtered.Where(p => p.Designation.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        filtered = SelectedFilter switch
        {
            "Rupture" => filtered.Where(p => p.CurrentStock <= 0 && !p.IsNew),
            "Sous minimum" => filtered.Where(p => p.CurrentStock > 0 && p.CurrentStock <= p.MinimumStock && !p.IsNew),
            "Normal" => filtered.Where(p => p.CurrentStock > p.MinimumStock && !p.IsNew),
            "Nouveaux" => filtered.Where(p => p.IsNew),
            _ => filtered
        };

        Products.Clear();
        foreach (var p in filtered)
        {
            Products.Add(new StockProductViewModel(p));
        }
    }

    private void NavigateToNeeds()
    {
        // Sera géré par le Shell
    }
}
