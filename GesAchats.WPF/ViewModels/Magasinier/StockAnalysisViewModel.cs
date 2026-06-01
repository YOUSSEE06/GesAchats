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

    public string StatusColor => Product.Etat switch
    {
        StockState.OutOfStock => "#F44336",
        StockState.Alert => "#FFC107",
        StockState.Ok => "#4CAF50",
        _ => "#808080"
    };
    
    public string StatusIcon => Product.Etat switch
    {
        StockState.OutOfStock => "🔴",
        StockState.Alert => "🟡",
        StockState.Ok => "🟢",
        _ => "❓"
    };
}

public class StockAnalysisViewModel : BaseViewModel
{
    private readonly IStockService _stockService;
    private string _searchText = string.Empty;
    private string _selectedFilter = "Tous";
    private int _totalArticles;
    private int _normalArticles;
    private int _lowStockArticles;
    private int _outOfStockArticles;

    public ObservableCollection<StockProductViewModel> Products { get; } = new ObservableCollection<StockProductViewModel>();
    
    public int TotalArticles
    {
        get => _totalArticles;
        set => SetProperty(ref _totalArticles, value);
    }
    
    public int NormalArticles
    {
        get => _normalArticles;
        set => SetProperty(ref _normalArticles, value);
    }
    
    public int LowStockArticles
    {
        get => _lowStockArticles;
        set => SetProperty(ref _lowStockArticles, value);
    }
    
    public int OutOfStockArticles
    {
        get => _outOfStockArticles;
        set => SetProperty(ref _outOfStockArticles, value);
    }
    
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) FilterProducts(); }
    }

    public List<string> Filters { get; } = new List<string> { "Tous", "Rupture", "Sous minimum", "Normal" };
    
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
            "Rupture" => filtered.Where(p => p.Etat == StockState.OutOfStock),
            "Sous minimum" => filtered.Where(p => p.Etat == StockState.Alert),
            "Normal" => filtered.Where(p => p.Etat == StockState.Ok),
            _ => filtered
        };

        var filteredList = filtered.ToList();
        
        Products.Clear();
        foreach (var p in filteredList)
        {
            Products.Add(new StockProductViewModel(p));
        }
        
        // Calculate statistics
        TotalArticles = filteredList.Count;
        NormalArticles = filteredList.Count(p => p.Etat == StockState.Ok);
        LowStockArticles = filteredList.Count(p => p.Etat == StockState.Alert);
        OutOfStockArticles = filteredList.Count(p => p.Etat == StockState.OutOfStock);
    }

    private void NavigateToNeeds()
    {
        // Sera géré par le Shell
    }
}
