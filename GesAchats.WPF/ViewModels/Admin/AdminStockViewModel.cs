using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Services;
using GesAchats.Core.DTOs;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using Serilog;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminStockItemViewModel : BaseViewModel
{
    public int Id { get; }
    public string Designation { get; }
    public decimal CurrentStock { get; }
    public decimal MinimumStock { get; }
    public string Unit { get; }
    public StockState State { get; }
    public string StateText { get; }
    public string StateColor { get; }

    public AdminStockItemViewModel(StockGlobalDto dto)
    {
        Id = dto.Id;
        Designation = dto.Designation;
        CurrentStock = dto.CurrentStock;
        MinimumStock = dto.MinimumStock;
        Unit = dto.Unit;
        State = dto.State;
        StateText = dto.State switch
        {
            StockState.Ok => "OK",
            StockState.Alert => "Alerte",
            StockState.OutOfStock => "Rupture",
            _ => ""
        };
        StateColor = dto.State switch
        {
            StockState.Ok => "#10B981",
            StockState.Alert => "#FFC107",
            StockState.OutOfStock => "#F44336",
            _ => "#64748B"
        };
    }
}

public class AdminStockViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStockService _stockService;
    private readonly ILogger _logger;
    private CancellationTokenSource? _searchCts;
    private int _loadVersion;

    // Pagination properties
    private int _currentPage = 1;
    private int _totalItems;
    private const int PageSize = 20;
    private string _searchText = string.Empty;
    private string _selectedStatus = "Tous";

    // Stats KPIs
    private int _totalProducts;
    private decimal _totalStock;
    private int _productsOk;
    private int _productsAlert;
    private int _productsOutOfStock;
    private string _totalProductsTrendText = string.Empty;
    private string _totalStockTrendText = string.Empty;
    private string _productsOkTrendText = string.Empty;
    private string _productsAlertTrendText = string.Empty;
    private string _productsOutOfStockTrendText = string.Empty;

    public ObservableCollection<string> StatusOptions { get; } = new() { "Tous", "OK", "Alerte", "Rupture" };
    public ObservableCollection<AdminStockItemViewModel> FilteredProducts { get; } = new();

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(FirstDisplayedItem));
                OnPropertyChanged(nameof(LastDisplayedItem));
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));
                OnPropertyChanged(nameof(PaginationInfo));
            }
        }
    }

    public int TotalItems
    {
        get => _totalItems;
        set
        {
            if (SetProperty(ref _totalItems, value))
            {
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(FirstDisplayedItem));
                OnPropertyChanged(nameof(LastDisplayedItem));
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));
                OnPropertyChanged(nameof(PaginationInfo));
            }
        }
    }

    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);

    public int FirstDisplayedItem => TotalItems == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;

    public int LastDisplayedItem => Math.Min(CurrentPage * PageSize, TotalItems);

    public bool CanGoToFirstPage => CurrentPage > 1 && TotalItems > 0;
    public bool CanGoToPreviousPage => CurrentPage > 1 && TotalItems > 0;
    public bool CanGoToNextPage => CurrentPage < TotalPages && TotalItems > 0;
    public bool CanGoToLastPage => CurrentPage < TotalPages && TotalItems > 0;

    public string PaginationInfo
    {
        get
        {
            if (TotalItems == 0)
                return "Affichage de 0 à 0 sur 0 articles";
            return $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} articles";
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = DebouncedFilterAsync();
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
                _ = ResetAndLoadPageAsync();
            }
        }
    }

    // Stats KPIs
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

    public string TotalProductsTrendText
    {
        get => _totalProductsTrendText;
        set => SetProperty(ref _totalProductsTrendText, value);
    }

    public string TotalStockTrendText
    {
        get => _totalStockTrendText;
        set => SetProperty(ref _totalStockTrendText, value);
    }

    public string ProductsOkTrendText
    {
        get => _productsOkTrendText;
        set => SetProperty(ref _productsOkTrendText, value);
    }

    public string ProductsAlertTrendText
    {
        get => _productsAlertTrendText;
        set => SetProperty(ref _productsAlertTrendText, value);
    }

    public string ProductsOutOfStockTrendText
    {
        get => _productsOutOfStockTrendText;
        set => SetProperty(ref _productsOutOfStockTrendText, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    public AdminStockViewModel(IUnitOfWork unitOfWork, IStockService stockService, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _stockService = stockService;
        _logger = logger;
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        FirstPageCommand = new RelayCommand(async _ => await GoToFirstPageAsync(), _ => CanGoToFirstPage);
        PreviousPageCommand = new RelayCommand(async _ => await GoToPreviousPageAsync(), _ => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(async _ => await GoToNextPageAsync(), _ => CanGoToNextPage);
        LastPageCommand = new RelayCommand(async _ => await GoToLastPageAsync(), _ => CanGoToLastPage);
        Title = "Stock global";
    }

    public async void OnNavigatedTo(object parameter)
    {
        await LoadDataAsync();
    }

    private async Task DebouncedFilterAsync()
    {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _searchCts, newCts);

        if (oldCts != null)
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        try
        {
            await Task.Delay(400, newCts.Token);
            CurrentPage = 1;
            await LoadPageAsync(newCts.Token);
        }
        catch (OperationCanceledException) when (newCts.IsCancellationRequested)
        {
            // Normal cancellation, do nothing
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during debounced filter");
        }
        finally
        {
            if (ReferenceEquals(_searchCts, newCts))
            {
                Interlocked.CompareExchange(ref _searchCts, null, newCts);
            }
            newCts.Dispose();
        }
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            await CalculateStatsAsync();
            await LoadPageAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CalculateStatsAsync()
    {
        var allProducts = await _stockService.GetAllProductsWithMagasinAsync();
        var productsList = allProducts.ToList();

        TotalProducts = productsList.Count;
        TotalStock = productsList.Sum(p => p.CurrentStock);
        ProductsOk = productsList.Count(p => p.Etat == StockState.Ok);
        ProductsAlert = productsList.Count(p => p.Etat == StockState.Alert);
        ProductsOutOfStock = productsList.Count(p => p.Etat == StockState.OutOfStock);

        // Calculate yesterday's data
        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);

        // Get products created yesterday
        var yesterdayProducts = productsList.Where(p =>
            p.CreatedAt.Date >= yesterday && p.CreatedAt.Date < today).ToList();

        int yesterdayTotal = yesterdayProducts.Count;
        decimal yesterdayTotalStock = yesterdayProducts.Sum(p => p.CurrentStock);
        int yesterdayOk = yesterdayProducts.Count(p => p.Etat == StockState.Ok);
        int yesterdayAlert = yesterdayProducts.Count(p => p.Etat == StockState.Alert);
        int yesterdayOutOfStock = yesterdayProducts.Count(p => p.Etat == StockState.OutOfStock);

        // Calculate trend texts
        TotalProductsTrendText = CalculateTrendText(TotalProducts, yesterdayTotal);
        TotalStockTrendText = CalculateTrendText(TotalStock, yesterdayTotalStock);
        ProductsOkTrendText = CalculateTrendText(ProductsOk, yesterdayOk);
        ProductsAlertTrendText = CalculateTrendText(ProductsAlert, yesterdayAlert);
        ProductsOutOfStockTrendText = CalculateTrendText(ProductsOutOfStock, yesterdayOutOfStock);
    }

    private async Task ResetAndLoadPageAsync()
    {
        CurrentPage = 1;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync(CancellationToken cancellationToken = default)
    {
        var version = Interlocked.Increment(ref _loadVersion);

        try
        {
            var result = await _stockService.GetStockGlobalPagedAsync(
                CurrentPage,
                PageSize,
                SearchText,
                SelectedStatus,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (version != _loadVersion)
                return;

            // Update UI on dispatcher thread
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                UpdateItems(result);
            }
            else
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => UpdateItems(result), DispatcherPriority.Normal, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Operation cancelled, ignore
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading page");
        }
    }

    private void UpdateItems(PagedResult<StockGlobalDto> result)
    {
        FilteredProducts.Clear();
        foreach (var dto in result.Items)
        {
            FilteredProducts.Add(new AdminStockItemViewModel(dto));
        }

        TotalItems = result.TotalCount;

        // If current page is beyond total pages, go to last page
        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
            _ = LoadPageAsync();
        }
    }

    private async Task GoToFirstPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage = 1;
            await LoadPageAsync();
        }
    }

    private async Task GoToPreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadPageAsync();
        }
    }

    private async Task GoToNextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadPageAsync();
        }
    }

    private async Task GoToLastPageAsync()
    {
        if (TotalPages > 0 && CurrentPage != TotalPages)
        {
            CurrentPage = TotalPages;
            await LoadPageAsync();
        }
    }
}
