using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using GesAchats.WPF.Views.Comptable.Factures;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GesAchats.WPF.ViewModels.Comptable;

public class FacturesViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserSession _userSession;
    private readonly ILogger _logger;

    private bool _isAdmin;
    public bool IsAdmin
    {
        get => _isAdmin;
        set => SetProperty(ref _isAdmin, value);
    }

    // Pagination properties
    private int _currentPage = 1;
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));
                OnPropertyChanged(nameof(PaginationInfo));
                if (!_isLoading && !_isInitializing)
                {
                    _ = LoadPageAsync();
                }
            }
        }
    }

    private int _totalItems;
    public int TotalItems
    {
        get => _totalItems;
        set
        {
            if (SetProperty(ref _totalItems, value))
            {
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));
                OnPropertyChanged(nameof(PaginationInfo));
            }
        }
    }

    public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
    public int PageSize => 20;

    public int FirstDisplayedItem => TotalItems == 0 ? 0 : (CurrentPage - 1) * PageSize + 1;
    public int LastDisplayedItem => Math.Min(CurrentPage * PageSize, TotalItems);
    public string PaginationInfo => $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} factures";

    public bool CanGoToFirstPage => CurrentPage > 1;
    public bool CanGoToPreviousPage => CurrentPage > 1;
    public bool CanGoToNextPage => CurrentPage < TotalPages;
    public bool CanGoToLastPage => CurrentPage < TotalPages;

    private ObservableCollection<InvoiceWithPaymentsViewModel> _factures = new();
    public ObservableCollection<InvoiceWithPaymentsViewModel> Factures
    {
        get => _factures;
        set => SetProperty(ref _factures, value);
    }

    private ObservableCollection<Supplier> _suppliers = new();
    public ObservableCollection<Supplier> Suppliers
    {
        get => _suppliers;
        set => SetProperty(ref _suppliers, value);
    }

    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value) && !_isInitializing)
            {
                _ = ResetAndLoadPageAsync();
            }
        }
    }

    private string _selectedStatus = "Tous";
    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value) && !_isInitializing)
            {
                _ = ResetAndLoadPageAsync();
            }
        }
    }

    private DateTime? _selectedDate;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value) && !_isInitializing)
            {
                _ = ResetAndLoadPageAsync();
            }
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value) && !_isInitializing)
            {
                _debounceTimer?.Stop();
                _debounceTimer?.Start();
            }
        }
    }

    // Stats
    private int _totalFacturesCount;
    public int TotalFacturesCount
    {
        get => _totalFacturesCount;
        set => SetProperty(ref _totalFacturesCount, value);
    }

    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set => SetProperty(ref _totalAmount, value);
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

    private int _waitingInvoicesCount;
    public int WaitingInvoicesCount
    {
        get => _waitingInvoicesCount;
        set => SetProperty(ref _waitingInvoicesCount, value);
    }

    private decimal _pendingAmount;
    public decimal PendingAmount
    {
        get => _pendingAmount;
        set => SetProperty(ref _pendingAmount, value);
    }

    private double _paymentRate;
    public double PaymentRate
    {
        get => _paymentRate;
        set => SetProperty(ref _paymentRate, value);
    }

    // Trend texts
    private string _totalFacturesCountTrendText = string.Empty;
    public string TotalFacturesCountTrendText
    {
        get => _totalFacturesCountTrendText;
        set => SetProperty(ref _totalFacturesCountTrendText, value);
    }

    private string _totalAmountTrendText = string.Empty;
    public string TotalAmountTrendText
    {
        get => _totalAmountTrendText;
        set => SetProperty(ref _totalAmountTrendText, value);
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

    private string _waitingInvoicesCountTrendText = string.Empty;
    public string WaitingInvoicesCountTrendText
    {
        get => _waitingInvoicesCountTrendText;
        set => SetProperty(ref _waitingInvoicesCountTrendText, value);
    }

    private string _pendingAmountTrendText = string.Empty;
    public string PendingAmountTrendText
    {
        get => _pendingAmountTrendText;
        set => SetProperty(ref _pendingAmountTrendText, value);
    }

    private InvoiceWithPaymentsViewModel? _selectedFacture;
    public InvoiceWithPaymentsViewModel? SelectedFacture
    {
        get => _selectedFacture;
        set => SetProperty(ref _selectedFacture, value);
    }

    // Commands
    public ICommand LoadFacturesCommand { get; }
    public ICommand AddFactureCommand { get; }
    public ICommand VerifyConformityCommand { get; }
    public ICommand RegisterPaymentCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand ViewBCCommand { get; }
    public ICommand ViewBLCommand { get; }
    public ICommand ViewFileCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    // Debounce and cancellation
    private readonly DispatcherTimer _debounceTimer;
    private CancellationTokenSource? _cancellationTokenSource;
    private bool _isLoading;
    private bool _isInitialized;
    private bool _isInitializing;

    public FacturesViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IServiceProvider serviceProvider, IUserSession userSession, ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _userSession = userSession;
        _logger = logger;
        Title = "Factures Fournisseurs";
        IsAdmin = _userSession.CurrentUser?.Role?.Code?.ToUpper() == "ADMIN";

        _debounceTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _debounceTimer.Tick += async (s, e) =>
        {
            _debounceTimer.Stop();
            await ResetAndLoadPageAsync();
        };

        LoadFacturesCommand = new RelayCommand(async _ => await InitializeAsync());
        AddFactureCommand = new RelayCommand(_ => _navigationService.NavigateTo("InvoiceForm"));
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());

        FirstPageCommand = new RelayCommand(_ => CurrentPage = 1, _ => CanGoToFirstPage);
        PreviousPageCommand = new RelayCommand(_ => CurrentPage--, _ => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(_ => CurrentPage++, _ => CanGoToNextPage);
        LastPageCommand = new RelayCommand(_ => CurrentPage = TotalPages, _ => CanGoToLastPage);

        ViewDetailsCommand = new RelayCommand(async _ =>
        {
            if (SelectedFacture == null)
                return;

            using (var scope = _serviceProvider.CreateScope())
            {
                var vm = ActivatorUtilities.CreateInstance<FactureDetailsViewModel>(scope.ServiceProvider, SelectedFacture.Id);
                var win = ActivatorUtilities.CreateInstance<FactureDetailsWindow>(scope.ServiceProvider, vm);
                win.Owner = Application.Current.MainWindow;
                win.ShowDialog();

                // Refresh after dialog closes in case something changed
                await RefreshDataAsync();
            }
        }, _ => SelectedFacture != null);

        ViewBCCommand = new RelayCommand(id =>
        {
            if (id is int bcId)
                _navigationService.NavigateTo("PurchaseOrderDetails", bcId);
        });

        ViewBLCommand = new RelayCommand(id =>
        {
            if (id is int blId)
                _navigationService.NavigateTo("DeliveryNoteDetails", blId);
        });

        ViewFileCommand = new RelayCommand(path =>
        {
            if (path is string filePath && !string.IsNullOrEmpty(filePath))
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true }); }
                catch { /* Handle error */ }
            }
        });

        VerifyConformityCommand = new RelayCommand(_ =>
        {
            if (SelectedFacture != null)
                _navigationService.NavigateTo("ConformityCheck", SelectedFacture.Id);
        }, _ => SelectedFacture != null);

        RegisterPaymentCommand = new RelayCommand(_ =>
        {
            if (SelectedFacture != null)
                _navigationService.NavigateTo("PaymentForm", SelectedFacture.Id);
        }, _ => SelectedFacture != null);
    }

    public async void OnNavigatedTo(object parameter)
    {
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        if (_isInitialized || _isLoading)
            return;

        _isInitialized = true;
        _isInitializing = true;
        _isLoading = true;
        IsBusy = true;
        
        try
        {
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            var supplierList = new ObservableCollection<Supplier>();
            var tousSupplier = new Supplier { Id = 0, CompanyName = "Tous" };
            supplierList.Add(tousSupplier);
            foreach (var supplier in suppliers.OrderBy(s => s.CompanyName))
            {
                supplierList.Add(supplier);
            }
            Suppliers = supplierList;
            
            SelectedSupplier = tousSupplier;
            SelectedStatus = "Tous";
            SelectedDate = null;
            SearchText = string.Empty;
            CurrentPage = 1;
            
            await CalculateStatsAsync();
            await LoadPageAsync();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Erreur lors du chargement initial de la page Factures Comptable");
            MessageBox.Show($"Erreur lors du chargement initial des factures: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isInitializing = false;
            _isLoading = false;
            IsBusy = false;
        }
    }

    private async Task RefreshDataAsync()
    {
        await CalculateStatsAsync();
        await LoadPageAsync();
    }

    private async Task ResetAndLoadPageAsync()
    {
        CurrentPage = 1;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        if (_isLoading)
            return;

        // Cancel previous operation
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = _cancellationTokenSource.Token;

        try
        {
            _isLoading = true;
            var result = await _unitOfWork.Invoices.GetInvoicesPagedAsync(
                CurrentPage,
                PageSize,
                SearchText,
                SelectedSupplier?.Id,
                SelectedStatus,
                SelectedDate,
                cancellationToken);

            if (cancellationToken.IsCancellationRequested)
                return;

            TotalItems = result.TotalCount;
            UpdateItems(result.Items);
        }
        catch (OperationCanceledException)
        {
            // Expected when user types quickly
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading page");
            MessageBox.Show($"Erreur lors du chargement de la page: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void UpdateItems(List<InvoiceDto> items)
    {
        var newFactures = new ObservableCollection<InvoiceWithPaymentsViewModel>();
        foreach (var dto in items)
        {
            newFactures.Add(new InvoiceWithPaymentsViewModel(dto));
        }
        Factures = newFactures;
    }

    protected virtual async Task CalculateStatsAsync()
    {
        // Load all invoices for stats calculation
        var allInvoices = await _unitOfWork.Invoices.GetAllIncludingAsync(
            i => i.Supplier,
            i => i.PurchaseOrder,
            i => i.DeliveryNote
        );
        var allPayments = await _unitOfWork.Payments.GetAllAsync();

        var allInvoiceVms = new List<InvoiceWithPaymentsViewModel>();
        foreach (var invoice in allInvoices.OrderByDescending(i => i.InvoiceDate))
        {
            var vm = new InvoiceWithPaymentsViewModel(invoice);
            var invoicePayments = allPayments.Where(p => p.InvoiceId == invoice.Id);
            foreach (var payment in invoicePayments)
            {
                vm.Payments.Add(payment);
            }
            allInvoiceVms.Add(vm);
        }

        TotalFacturesCount = allInvoiceVms.Count;
        TotalAmount = allInvoiceVms.Sum(f => f.AmountTTC);
        PaidInvoicesCount = allInvoiceVms.Count(f => f.StatusCalculated == "Payée");
        PartialInvoicesCount = allInvoiceVms.Count(f => f.StatusCalculated == "Partiellement payée");
        WaitingInvoicesCount = allInvoiceVms.Count(f => f.StatusCalculated == "En attente");
        PendingAmount = allInvoiceVms.Sum(f => f.Balance);

        // Calculate yesterday's data
        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);

        var yesterdayInvoices = allInvoiceVms.Where(f =>
            f.InvoiceDate.Date >= yesterday && f.InvoiceDate.Date < today).ToList();

        int yesterdayTotalCount = yesterdayInvoices.Count;
        decimal yesterdayTotalAmount = yesterdayInvoices.Sum(f => f.AmountTTC);
        int yesterdayPaidCount = yesterdayInvoices.Count(f => f.StatusCalculated == "Payée");
        int yesterdayPartialCount = yesterdayInvoices.Count(f => f.StatusCalculated == "Partiellement payée");
        int yesterdayWaitingCount = yesterdayInvoices.Count(f => f.StatusCalculated == "En attente");
        decimal yesterdayPendingAmount = yesterdayInvoices.Sum(f => f.Balance);

        // Calculate trend texts
        TotalFacturesCountTrendText = CalculateTrendText(TotalFacturesCount, yesterdayTotalCount);
        TotalAmountTrendText = CalculateTrendText(TotalAmount, yesterdayTotalAmount);
        PaidInvoicesCountTrendText = CalculateTrendText(PaidInvoicesCount, yesterdayPaidCount);
        PartialInvoicesCountTrendText = CalculateTrendText(PartialInvoicesCount, yesterdayPartialCount);
        WaitingInvoicesCountTrendText = CalculateTrendText(WaitingInvoicesCount, yesterdayWaitingCount);
        PendingAmountTrendText = CalculateTrendText(PendingAmount, yesterdayPendingAmount);
    }

    private async void ResetFilters()
    {
        if (_isLoading) return;
        _isInitializing = true;
        try
        {
            SelectedSupplier = Suppliers.FirstOrDefault();
            SelectedStatus = "Tous";
            SelectedDate = null;
            SearchText = string.Empty;
        }
        finally
        {
            _isInitializing = false;
        }
        await ResetAndLoadPageAsync();
    }

    private new string CalculateTrendText(int current, int previous)
    {
        if (previous == 0) return "";
        int diff = current - previous;
        if (diff > 0) return $"+{diff}";
        if (diff < 0) return $"{diff}";
        return "0";
    }

    private new string CalculateTrendText(decimal current, decimal previous)
    {
        if (previous == 0) return "";
        decimal diff = current - previous;
        if (diff > 0) return $"+{diff:N2}";
        if (diff < 0) return $"{diff:N2}";
        return "0";
    }
}
