using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.DTOs;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GesAchats.WPF.Views.Comptable.Factures;
using Serilog;
using AsyncRelayCommand = CommunityToolkit.Mvvm.Input.AsyncRelayCommand;
using IAsyncRelayCommand = CommunityToolkit.Mvvm.Input.IAsyncRelayCommand;

namespace GesAchats.WPF.ViewModels.Comptable;

public class InvoicePaymentTrackingViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private CancellationTokenSource? _searchCts;
    private int _loadVersion;
    private List<Invoice> _allInvoicesForStats = new();
    private List<Payment> _allPaymentsForStats = new();
    private bool _isInitialized;
    private bool _isInitializing;
    private bool _isLoading;
    private readonly SemaphoreSlim _dbGate = new(1, 1);

    public ObservableCollection<InvoiceWithPaymentsViewModel> FilteredInvoices { get; set; } = new();

    // Pagination properties
    private int _currentPage = 1;
    private int _totalItems;
    private const int PageSize = 20;
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
                return "Affichage de 0 à 0 sur 0 factures";
            return $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} factures";
        }
    }

    // Filters
    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                if (!_isInitializing && !_isLoading)
                {
                    _ = ResetAndLoadPageAsync();
                }
            }
        }
    }

    private string? _selectedStatus;
    public string? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                if (!_isInitializing && !_isLoading)
                {
                    _ = ResetAndLoadPageAsync();
                }
            }
        }
    }

    private DateTime? _selectedDate;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
            {
                if (!_isInitializing && !_isLoading)
                {
                    _ = ResetAndLoadPageAsync();
                }
            }
        }
    }

    private string? _searchInvoiceNumber;
    public string? SearchInvoiceNumber
    {
        get => _searchInvoiceNumber;
        set
        {
            if (SetProperty(ref _searchInvoiceNumber, value))
            {
                if (!_isInitializing && !_isLoading)
                {
                    _ = DebouncedFilterAsync();
                }
            }
        }
    }

    public ObservableCollection<Supplier> Suppliers { get; set; } = new();
    public ObservableCollection<string> StatusOptions { get; set; } = new()
    {
        "Tous", "Payée", "Partiellement payée", "En attente"
    };

    // KPIs (unchanged, calculated on all invoices)
    private decimal _totalTTC;
    public decimal TotalTTC
    {
        get => _totalTTC;
        set => SetProperty(ref _totalTTC, value);
    }

    private decimal _totalPayments;
    public decimal TotalPayments
    {
        get => _totalPayments;
        set => SetProperty(ref _totalPayments, value);
    }

    private decimal _totalBalance;
    public decimal TotalBalance
    {
        get => _totalBalance;
        set => SetProperty(ref _totalBalance, value);
    }

    private int _paidInvoicesCount;
    public int PaidInvoicesCount
    {
        get => _paidInvoicesCount;
        set => SetProperty(ref _paidInvoicesCount, value);
    }

    private int _partialInvoicesCount;
    public int PartialInvoicesCount
    {
        get => _partialInvoicesCount;
        set => SetProperty(ref _partialInvoicesCount, value);
    }

    private int _billedSuppliersCount;
    public int BilledSuppliersCount
    {
        get => _billedSuppliersCount;
        set => SetProperty(ref _billedSuppliersCount, value);
    }

    // Filtre de période (identique au Dashboard principal)
    private DateTime _dateDebut = DateTime.Today.AddMonths(-1);
    public DateTime DateDebut
    {
        get => _dateDebut;
        set
        {
            if (SetProperty(ref _dateDebut, value))
            {
                if (!_isInitializing && !_isLoading)
                {
                    _ = RefreshFromPeriodAsync();
                }
            }
        }
    }

    private DateTime _dateFin = DateTime.Today;
    public DateTime DateFin
    {
        get => _dateFin;
        set
        {
            if (SetProperty(ref _dateFin, value))
            {
                if (!_isInitializing && !_isLoading)
                {
                    _ = RefreshFromPeriodAsync();
                }
            }
        }
    }

    // Couleurs de tendance KPI
    private bool _totalTTCIsPositiveTrend = true;
    public bool TotalTTCIsPositiveTrend { get => _totalTTCIsPositiveTrend; set => SetProperty(ref _totalTTCIsPositiveTrend, value); }

    private bool _totalPaymentsIsPositiveTrend = true;
    public bool TotalPaymentsIsPositiveTrend { get => _totalPaymentsIsPositiveTrend; set => SetProperty(ref _totalPaymentsIsPositiveTrend, value); }

    private bool _totalBalanceIsPositiveTrend = true;
    public bool TotalBalanceIsPositiveTrend { get => _totalBalanceIsPositiveTrend; set => SetProperty(ref _totalBalanceIsPositiveTrend, value); }

    private bool _paidInvoicesCountIsPositiveTrend = true;
    public bool PaidInvoicesCountIsPositiveTrend { get => _paidInvoicesCountIsPositiveTrend; set => SetProperty(ref _paidInvoicesCountIsPositiveTrend, value); }

    private bool _partialInvoicesCountIsPositiveTrend = true;
    public bool PartialInvoicesCountIsPositiveTrend { get => _partialInvoicesCountIsPositiveTrend; set => SetProperty(ref _partialInvoicesCountIsPositiveTrend, value); }

    private bool _billedSuppliersCountIsPositiveTrend = true;
    public bool BilledSuppliersCountIsPositiveTrend { get => _billedSuppliersCountIsPositiveTrend; set => SetProperty(ref _billedSuppliersCountIsPositiveTrend, value); }

    private string _totalTTCTrendText = string.Empty;
    public string TotalTTCTrendText
    {
        get => _totalTTCTrendText;
        set => SetProperty(ref _totalTTCTrendText, value);
    }

    private string _totalPaymentsTrendText = string.Empty;
    public string TotalPaymentsTrendText
    {
        get => _totalPaymentsTrendText;
        set => SetProperty(ref _totalPaymentsTrendText, value);
    }

    private string _totalBalanceTrendText = string.Empty;
    public string TotalBalanceTrendText
    {
        get => _totalBalanceTrendText;
        set => SetProperty(ref _totalBalanceTrendText, value);
    }

    private string _paidInvoicesCountTrendText = string.Empty;
    public string PaidInvoicesCountTrendText
    {
        get => _paidInvoicesCountTrendText;
        set => SetProperty(ref _paidInvoicesCountTrendText, value);
    }

    private string _partialInvoicesCountTrendText = string.Empty;
    public string PartialInvoicesCountTrendText
    {
        get => _partialInvoicesCountTrendText;
        set => SetProperty(ref _partialInvoicesCountTrendText, value);
    }

    private string _billedSuppliersCountTrendText = string.Empty;
    public string BilledSuppliersCountTrendText
    {
        get => _billedSuppliersCountTrendText;
        set => SetProperty(ref _billedSuppliersCountTrendText, value);
    }

    // Commands
    public ICommand LoadDataCommand { get; }
    public ICommand ViewInvoiceCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public InvoicePaymentTrackingViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IServiceProvider serviceProvider, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _logger = logger;
        Title = "PV Comptable - Suivi des Factures et Règlements";

        LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
        ViewInvoiceCommand = new RelayCommand(async param => await ViewInvoiceAsync(param as InvoiceWithPaymentsViewModel));
        ResetFiltersCommand = new RelayCommand(async _ => await ExecuteClearFiltersAsync());
        FirstPageCommand = new RelayCommand(async _ => await GoToFirstPageAsync(), _ => CanGoToFirstPage);
        PreviousPageCommand = new RelayCommand(async _ => await GoToPreviousPageAsync(), _ => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(async _ => await GoToNextPageAsync(), _ => CanGoToNextPage);
        LastPageCommand = new RelayCommand(async _ => await GoToLastPageAsync(), _ => CanGoToLastPage);
        RefreshCommand = new AsyncRelayCommand(RefreshFromPeriodAsync);

        _ = LoadDataAsync();
    }

    public async void OnNavigatedTo(object parameter)
    {
        await LoadDataAsync();
    }

    /// <summary>
    /// Recharge les données (KPI + liste) avec la période courante Du → Au.
    /// Utilisé quand l'utilisateur change les dates ou clique sur "Actualiser".
    /// </summary>
    private async Task RefreshFromPeriodAsync()
    {
        if (_isLoading) return;
        _isInitialized = false;
        CurrentPage = 1;
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

    public async Task LoadDataAsync()
    {
        if (_isInitialized || _isLoading)
            return;

        IsBusy = true;
        try
        {
            _isInitializing = true;
            await CalculateStatsAsync();
            _isInitializing = false;
            
            _isInitialized = true;

            // Ne PAS laisser _isLoading = true ici : LoadPageAsync() a sa propre garde
            // `if (_isLoading) return;` et n'aurait jamais exécuté le chargement initial.
            await LoadPageAsync();
        }
        finally
        {
            _isInitializing = false;
            IsBusy = false;
        }
    }

    private async Task CalculateStatsAsync()
    {
        await _dbGate.WaitAsync();
        try
        {
            // Load all invoices and payments for stats
            var invoices = await _unitOfWork.Invoices.GetAllIncludingAsync(i => i.Supplier, i => i.Details);
            var payments = await _unitOfWork.Payments.GetAllAsync();
            _allInvoicesForStats = invoices.ToList();
            _allPaymentsForStats = payments.ToList();

            // Load suppliers for filter
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            Suppliers.Clear();
            var tousSupplier = new Supplier { Id = 0, CompanyName = "Tous" };
            Suppliers.Add(tousSupplier);
            foreach (var supplier in suppliers.OrderBy(s => s.CompanyName))
            {
                Suppliers.Add(supplier);
            }
            SelectedSupplier = tousSupplier;
            SelectedStatus = "Tous";
            SelectedDate = null;
            SearchInvoiceNumber = string.Empty;
            CurrentPage = 1;

            // Calculate KPIs
            CalculateStats();
        }
        finally
        {
            _dbGate.Release();
        }
    }

    private void CalculateStats()
    {
        // Calculate KPIs based on the selected period vs the previous period of same duration
        var invoiceVms = _allInvoicesForStats.Select(i => new InvoiceWithPaymentsViewModel(i)
        {
            Payments = new ObservableCollection<Payment>(_allPaymentsForStats.Where(p => p.InvoiceId == i.Id))
        }).ToList();

        var start = DateDebut.Date;
        var end = DateFin.Date.AddDays(1);
        var duration = DateFin.Date - DateDebut.Date;
        var previousStart = DateDebut.Date - duration;
        var previousEnd = DateDebut.Date;

        var currentInvoices = invoiceVms.Where(i => i.InvoiceDate >= start && i.InvoiceDate < end).ToList();
        var previousInvoices = invoiceVms.Where(i => i.InvoiceDate >= previousStart && i.InvoiceDate < previousEnd).ToList();
        var currentPayments = _allPaymentsForStats.Where(p => p.PaymentDate >= start && p.PaymentDate < end).ToList();
        var previousPayments = _allPaymentsForStats.Where(p => p.PaymentDate >= previousStart && p.PaymentDate < previousEnd).ToList();

        TotalTTC = currentInvoices.Sum(i => i.AmountTTC);
        TotalPayments = currentPayments.Sum(p => p.AmountPaid);
        TotalBalance = TotalTTC - TotalPayments;
        PaidInvoicesCount = currentInvoices.Count(i => i.StatusCalculated == "Payée");
        PartialInvoicesCount = currentInvoices.Count(i => i.StatusCalculated == "Partiellement payée");
        BilledSuppliersCount = currentInvoices.Select(i => i.SupplierId).Distinct().Count();

        decimal previousTTC = previousInvoices.Sum(i => i.AmountTTC);
        decimal previousPaymentAmount = previousPayments.Sum(p => p.AmountPaid);
        decimal previousBalance = previousTTC - previousPaymentAmount;
        int previousPaidCount = previousInvoices.Count(i => i.StatusCalculated == "Payée");
        int previousPartialCount = previousInvoices.Count(i => i.StatusCalculated == "Partiellement payée");
        int previousSuppliersCount = previousInvoices.Select(i => i.SupplierId).Distinct().Count();

        // Tendances (texte + couleur), formule ((current - previous) / previous) * 100
        TotalTTCTrendText = FormatTrend(CalculateVariation(TotalTTC, previousTTC), out bool ttcPos);
        TotalTTCIsPositiveTrend = ttcPos;
        TotalPaymentsTrendText = FormatTrend(CalculateVariation(TotalPayments, previousPaymentAmount), out bool payPos);
        TotalPaymentsIsPositiveTrend = payPos;
        TotalBalanceTrendText = FormatTrend(CalculateVariation(TotalBalance, previousBalance), out bool balPos);
        TotalBalanceIsPositiveTrend = balPos;
        PaidInvoicesCountTrendText = FormatTrend(CalculateVariation(PaidInvoicesCount, previousPaidCount), out bool paidPos);
        PaidInvoicesCountIsPositiveTrend = paidPos;
        PartialInvoicesCountTrendText = FormatTrend(CalculateVariation(PartialInvoicesCount, previousPartialCount), out bool partPos);
        PartialInvoicesCountIsPositiveTrend = partPos;
        BilledSuppliersCountTrendText = FormatTrend(CalculateVariation(BilledSuppliersCount, previousSuppliersCount), out bool supPos);
        BilledSuppliersCountIsPositiveTrend = supPos;
    }

    private async Task ResetAndLoadPageAsync()
    {
        if (_isLoading) return;
        CurrentPage = 1;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync(CancellationToken cancellationToken = default)
    {
        // Sérialise l'accès au DbContext : si une autre opération est en cours
        // (stats, debounce, setter de filtre...), on attend qu'elle se termine,
        // au lieu de lancer une seconde opération concurrente sur le même contexte.
        await _dbGate.WaitAsync();

        if (_isLoading)
        {
            _dbGate.Release();
            return;
        }

        var version = Interlocked.Increment(ref _loadVersion);
        _isLoading = true;
        
        try
        {
            var result = await _unitOfWork.Invoices.GetInvoicesPagedAsync(
                CurrentPage,
                PageSize,
                SearchInvoiceNumber,
                SelectedSupplier?.Id,
                SelectedStatus,
                SelectedDate,
                DateDebut,
                DateFin,
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
        finally
        {
            _isLoading = false;
            _dbGate.Release();
        }
    }

    private void UpdateItems(PagedResult<InvoiceDto> result)
    {
        FilteredInvoices.Clear();
        foreach (var dto in result.Items)
        {
            FilteredInvoices.Add(new InvoiceWithPaymentsViewModel(dto));
        }

        TotalItems = result.TotalCount;

        // If current page is beyond total pages, go to last page
        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
            if (!_isLoading)
            {
                _ = LoadPageAsync();
            }
        }
    }

    private async Task GoToFirstPageAsync()
    {
        if (CurrentPage > 1 && !_isLoading)
        {
            CurrentPage = 1;
            await LoadPageAsync();
        }
    }

    private async Task GoToPreviousPageAsync()
    {
        if (CurrentPage > 1 && !_isLoading)
        {
            CurrentPage--;
            await LoadPageAsync();
        }
    }

    private async Task GoToNextPageAsync()
    {
        if (CurrentPage < TotalPages && !_isLoading)
        {
            CurrentPage++;
            await LoadPageAsync();
        }
    }

    private async Task GoToLastPageAsync()
    {
        if (TotalPages > 0 && CurrentPage != TotalPages && !_isLoading)
        {
            CurrentPage = TotalPages;
            await LoadPageAsync();
        }
    }

    private async Task ExecuteClearFiltersAsync()
    {
        if (_isLoading) return;
        _isInitializing = true;
        try
        {
            SelectedSupplier = Suppliers.FirstOrDefault();
            SelectedStatus = "Tous";
            SelectedDate = null;
            SearchInvoiceNumber = string.Empty;
        }
        finally
        {
            _isInitializing = false;
        }
        await ResetAndLoadPageAsync();
    }

    private async Task ViewInvoiceAsync(InvoiceWithPaymentsViewModel? invoiceVm)
    {
        if (invoiceVm == null) return;

        // Load full invoice from database if we only have a DTO
        if (invoiceVm.Invoice == null && invoiceVm.InvoiceDto != null)
        {
            var fullInvoice = await _unitOfWork.Invoices.GetByIdWithDetailsAsync(invoiceVm.InvoiceDto.Id);
            if (fullInvoice == null) return;
            
            // Replace the view model with one that has the full invoice entity
            var payments = await _unitOfWork.Payments.GetAllAsync();
            invoiceVm = new InvoiceWithPaymentsViewModel(fullInvoice);
            var invoicePayments = payments.Where(p => p.InvoiceId == fullInvoice.Id);
            foreach (var payment in invoicePayments)
            {
                invoiceVm.Payments.Add(payment);
            }
        }

        // Create and show the popup using ServiceProvider
        using (var scope = _serviceProvider.CreateScope())
        {
            var popupViewModel = ActivatorUtilities.CreateInstance<InvoicePaymentDetailsPopupViewModel>(scope.ServiceProvider, invoiceVm);
            var popup = ActivatorUtilities.CreateInstance<InvoicePaymentDetailsPopup>(scope.ServiceProvider, popupViewModel);
            popup.Owner = Application.Current.MainWindow;
            
            var result = popup.ShowDialog();
            
            if (result == true)
            {
                // Refresh data if changes were made
                await LoadDataAsync();
            }
        }
    }
}
