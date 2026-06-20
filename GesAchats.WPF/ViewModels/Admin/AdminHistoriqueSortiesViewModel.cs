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

public class AdminSortieItemViewModel : BaseViewModel
{
    public int Id { get; }
    public string Date { get; }
    public string ProductDesignation { get; }
    public decimal Quantity { get; }
    public string? Reason { get; }
    public decimal StockAfterExit { get; }

    public AdminSortieItemViewModel(StockExitHistoryDto dto)
    {
        Id = dto.Id;
        Date = dto.ExitDate.ToString("dd/MM/yyyy HH:mm");
        ProductDesignation = dto.ProductDesignation;
        Quantity = dto.Quantity;
        Reason = dto.Reason;
        StockAfterExit = dto.StockAfterExit;
    }
}

public class AdminHistoriqueSortiesViewModel : BaseViewModel, INavigatable
{
    private readonly IStockService _stockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger _logger;
    private CancellationTokenSource? _searchCts;
    private int _loadVersion;

    // Pagination properties
    private int _currentPage = 1;
    private int _totalItems;
    private const int PageSize = 20;
    private string _searchText = string.Empty;
    private DateTime? _filterDate;

    // Stats KPIs
    private int _totalExits;
    private decimal _totalQuantityExited;
    private string _totalExitsTrendText = string.Empty;
    private string _totalQuantityExitedTrendText = string.Empty;

    public ObservableCollection<AdminSortieItemViewModel> StockExits { get; } = new();

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
                return "Affichage de 0 à 0 sur 0 sorties";
            return $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} sorties";
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

    public DateTime? FilterDate
    {
        get => _filterDate;
        set
        {
            if (SetProperty(ref _filterDate, value))
            {
                _ = ResetAndLoadPageAsync();
            }
        }
    }

    // Stats KPIs
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

    public string TotalExitsTrendText
    {
        get => _totalExitsTrendText;
        set => SetProperty(ref _totalExitsTrendText, value);
    }

    public string TotalQuantityExitedTrendText
    {
        get => _totalQuantityExitedTrendText;
        set => SetProperty(ref _totalQuantityExitedTrendText, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    public AdminHistoriqueSortiesViewModel(IStockService stockService, IUnitOfWork unitOfWork, ILogger logger)
    {
        _stockService = stockService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        Title = "Historique des sorties";

        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        ResetFiltersCommand = new RelayCommand(async _ => await ExecuteResetFiltersAsync());
        FirstPageCommand = new RelayCommand(async _ => await GoToFirstPageAsync(), _ => CanGoToFirstPage);
        PreviousPageCommand = new RelayCommand(async _ => await GoToPreviousPageAsync(), _ => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(async _ => await GoToNextPageAsync(), _ => CanGoToNextPage);
        LastPageCommand = new RelayCommand(async _ => await GoToLastPageAsync(), _ => CanGoToLastPage);
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

    private async Task ExecuteResetFiltersAsync()
    {
        SearchText = string.Empty;
        FilterDate = null;
        await ResetAndLoadPageAsync();
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
        var allExits = await _stockService.GetAllStockExitsAsync();
        var exitsList = allExits.ToList();

        TotalExits = exitsList.Count;
        TotalQuantityExited = exitsList.Sum(e => e.Quantity);

        // Calculate yesterday's values
        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);

        var yesterdayExits = exitsList.Where(e =>
            e.CreatedAt.Date >= yesterday && e.CreatedAt.Date < today).ToList();

        int yesterdayTotalCount = yesterdayExits.Count;
        decimal yesterdayTotalQuantity = yesterdayExits.Sum(e => e.Quantity);

        // Calculate trend texts
        TotalExitsTrendText = CalculateTrendText(TotalExits, yesterdayTotalCount);
        TotalQuantityExitedTrendText = CalculateTrendText(TotalQuantityExited, yesterdayTotalQuantity);
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
            var result = await _unitOfWork.StockExits.GetStockExitsPagedAsync(
                CurrentPage,
                PageSize,
                SearchText,
                FilterDate,
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

    private void UpdateItems(PagedResult<StockExitHistoryDto> result)
    {
        StockExits.Clear();
        foreach (var dto in result.Items)
        {
            StockExits.Add(new AdminSortieItemViewModel(dto));
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
