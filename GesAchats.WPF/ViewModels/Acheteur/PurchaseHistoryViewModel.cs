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
    private ProductHistoryViewModel? _selectedProductForHistory;
    private string _searchText = string.Empty;
    private List<ProductHistoryViewModel> _allProducts = new();
    private List<ProductHistoryViewModel> _allFilteredProducts = new();

    private int _currentPage = 1;
    private int _itemsPerPage = 20;
    private int _totalPages = 1;
    private int _totalItems;

    public ObservableCollection<ProductHistoryViewModel> Products { get; } = new ObservableCollection<ProductHistoryViewModel>();
    public ObservableCollection<DeliveryNoteDetail> DetailedHistory { get; } = new ObservableCollection<DeliveryNoteDetail>();

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
                if (value != null)
                {
                    SelectedProductForHistory = value;
                    _ = LoadDetailedHistory();
                }
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    // Article affiché dans le panneau droit (conservé indépendamment de la page/recherche)
    public ProductHistoryViewModel? SelectedProductForHistory
    {
        get => _selectedProductForHistory;
        set => SetProperty(ref _selectedProductForHistory, value);
    }

    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    // ===================== PAGINATION =====================
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                UpdatePagedProducts();
            }
        }
    }

    public int ItemsPerPage
    {
        get => _itemsPerPage;
        set
        {
            if (SetProperty(ref _itemsPerPage, value) && value > 0)
            {
                CalculateTotalPages();
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                UpdatePagedProducts();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        set
        {
            if (SetProperty(ref _totalItems, value))
            {
                CalculateTotalPages();
                OnPropertyChanged(nameof(FirstDisplayedItem));
                OnPropertyChanged(nameof(LastDisplayedItem));
                OnPropertyChanged(nameof(PaginationText));
            }
        }
    }

    public int FirstDisplayedItem => TotalItems == 0 ? 0 : ((CurrentPage - 1) * ItemsPerPage) + 1;

    public int LastDisplayedItem => Math.Min(CurrentPage * ItemsPerPage, TotalItems);

    public string PaginationText
    {
        get
        {
            if (TotalItems == 0) return "Aucun article";
            var unit = TotalItems > 1 ? "articles" : "article";
            return $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} {unit}";
        }
    }

    public PurchaseHistoryViewModel(IUnitOfWork unitOfWork, IPriceAnalysisService priceService, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _priceService = priceService;
        _navigationService = navigationService;
        Title = "Historique des Achats";

        NavigateToStatsCommand = new RelayCommand(
            _ => _navigationService.NavigateTo("ProductStats", SelectedProductForHistory?.Product),
            _ => SelectedProductForHistory != null
        );
        FirstPageCommand = new RelayCommand(_ => CurrentPage = 1, _ => CurrentPage > 1);
        PreviousPageCommand = new RelayCommand(_ => { if (CurrentPage > 1) CurrentPage--; }, _ => CurrentPage > 1);
        NextPageCommand = new RelayCommand(_ => { if (CurrentPage < TotalPages) CurrentPage++; }, _ => CurrentPage < TotalPages);
        LastPageCommand = new RelayCommand(_ => CurrentPage = TotalPages, _ => CurrentPage < TotalPages);

        _ = LoadProducts();
    }

    private async Task LoadProducts()
    {
        IsBusy = true;
        try
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            _allProducts.Clear();
            foreach (var p in products)
            {
                var history = await _priceService.GetPriceHistoryForProductAsync(p.Id);
                var vm = new ProductHistoryViewModel(p)
                {
                    PurchaseCount = history.Count(),
                    AveragePrice = history.Any() ? history.Average(x => x.UnitPriceHT) : 0
                };
                _allProducts.Add(vm);
            }
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
            filtered = filtered.Where(p => 
                p.Product.Designation.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }
        
        var filteredList = filtered.ToList();
        _allFilteredProducts = filteredList;

        // Réinitialise la pagination (recalcul du total + retour page 1)
        TotalItems = _allFilteredProducts.Count;
        CurrentPage = 1;
        UpdatePagedProducts();
    }

    private void CalculateTotalPages()
    {
        TotalPages = (int)Math.Ceiling((double)_allFilteredProducts.Count / ItemsPerPage);
        if (TotalPages < 1) TotalPages = 1;
    }

    private void UpdatePagedProducts()
    {
        Products.Clear();
        var start = (CurrentPage - 1) * ItemsPerPage;
        foreach (var p in _allFilteredProducts.Skip(start).Take(ItemsPerPage))
        {
            Products.Add(p);
        }

        OnPropertyChanged(nameof(FirstDisplayedItem));
        OnPropertyChanged(nameof(LastDisplayedItem));
        OnPropertyChanged(nameof(PaginationText));

        // Notifie les commandes de pagination (états désactivés des boutons)
        CommandManager.InvalidateRequerySuggested();
    }

    private async Task LoadDetailedHistory()
    {
        if (SelectedProductForHistory == null)
        {
            DetailedHistory.Clear();
            return;
        }

        IsBusy = true;
        try
        {
            var history = await _priceService.GetPriceHistoryForProductAsync(SelectedProductForHistory.Product.Id);
            DetailedHistory.Clear();
            foreach (var item in history.OrderByDescending(x => x.DeliveryNote.ReceptionDate))
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
