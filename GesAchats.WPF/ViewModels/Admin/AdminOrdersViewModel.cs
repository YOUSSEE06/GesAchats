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
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminOrderItemViewModel : BaseViewModel
{
    public int Id { get; }
    public string Date { get; }
    public string OrderNumber { get; }
    public string SupplierName { get; }
    public string QuotationRef { get; }
    public string TotalTTC { get; }
    public string Status { get; }

    public AdminOrderItemViewModel(PurchaseOrderDto dto)
    {
        Id = dto.Id;
        Date = dto.OrderDate.ToString("dd/MM/yyyy");
        OrderNumber = dto.OrderNumber;
        SupplierName = dto.SupplierName;
        QuotationRef = dto.QuotationRef ?? "N/A";
        TotalTTC = $"{dto.TotalTTC:N2} MAD";
        Status = dto.Status;
    }
}

public class AdminOrdersViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly bool _excludeCancelled;
    private CancellationTokenSource? _searchCts;
    private int _loadVersion;
    
    // Pagination properties
    private int _currentPage = 1;
    private int _totalItems;
    private const int PageSize = 20;
    private string _searchText = string.Empty;
    private string _selectedSupplier = string.Empty;
    private string _selectedStatus = string.Empty;
    private DateTime? _searchDate;
    
    // KPI Properties
    private int _totalBc;
    private int _bcValides;
    private int _bcEnAttente;
    private int _bcAnnules;
    private decimal _valeurTotaleBc;
    private int _fournisseursActifs;
    private string _totalBcTrendText = string.Empty;
    private string _bcValidesTrendText = string.Empty;
    private string _bcEnAttenteTrendText = string.Empty;
    private string _bcAnnulesTrendText = string.Empty;
    private string _valeurTotaleBcTrendText = string.Empty;
    private string _fournisseursActifsTrendText = string.Empty;

    public ObservableCollection<string> Statuses { get; } = new()
    {
        "", // Tous les statuts
        PurchaseOrderStatus.Pending,
        PurchaseOrderStatus.Validated,
        PurchaseOrderStatus.Cancelled
    };

    public ObservableCollection<string> Suppliers { get; } = new();

    public ObservableCollection<AdminOrderItemViewModel> Orders { get; } = new();

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
                return "Affichage de 0 à 0 sur 0 bons de commande";
            return $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} bons de commande";
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

    public string SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                _ = ResetAndLoadPageAsync();
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

    public DateTime? SearchDate
    {
        get => _searchDate;
        set
        {
            if (SetProperty(ref _searchDate, value))
            {
                _ = ResetAndLoadPageAsync();
            }
        }
    }

    // KPI Properties
    public int TotalBc { get => _totalBc; set => SetProperty(ref _totalBc, value); }
    public int BcValides { get => _bcValides; set => SetProperty(ref _bcValides, value); }
    public int BcEnAttente { get => _bcEnAttente; set => SetProperty(ref _bcEnAttente, value); }
    public int BcAnnules { get => _bcAnnules; set => SetProperty(ref _bcAnnules, value); }
    public decimal ValeurTotaleBc { get => _valeurTotaleBc; set => SetProperty(ref _valeurTotaleBc, value); }
    public int FournisseursActifs { get => _fournisseursActifs; set => SetProperty(ref _fournisseursActifs, value); }

    public string TotalBcTrendText
    {
        get => _totalBcTrendText;
        set => SetProperty(ref _totalBcTrendText, value);
    }
    public string BcValidesTrendText
    {
        get => _bcValidesTrendText;
        set => SetProperty(ref _bcValidesTrendText, value);
    }
    public string BcEnAttenteTrendText
    {
        get => _bcEnAttenteTrendText;
        set => SetProperty(ref _bcEnAttenteTrendText, value);
    }
    public string BcAnnulesTrendText
    {
        get => _bcAnnulesTrendText;
        set => SetProperty(ref _bcAnnulesTrendText, value);
    }
    public string ValeurTotaleBcTrendText
    {
        get => _valeurTotaleBcTrendText;
        set => SetProperty(ref _valeurTotaleBcTrendText, value);
    }
    public string FournisseursActifsTrendText
    {
        get => _fournisseursActifsTrendText;
        set => SetProperty(ref _fournisseursActifsTrendText, value);
    }

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is string status)
        {
            SelectedStatus = status;
        }
    }

    public ICommand InspectCommand { get; }
    public ICommand PrintPdfCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    public AdminOrdersViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger logger, bool excludeCancelled = false)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _excludeCancelled = excludeCancelled;
        Title = "Gestion des Bons de Commande";

        InspectCommand = new RelayCommand(async p => await ExecuteInspect(p as AdminOrderItemViewModel));
        PrintPdfCommand = new RelayCommand(async p => await ExecutePrintPdf(p as AdminOrderItemViewModel));
        ResetFiltersCommand = new RelayCommand(async _ => await ExecuteResetFiltersAsync());
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        FirstPageCommand = new RelayCommand(async _ => await GoToFirstPageAsync(), _ => CanGoToFirstPage);
        PreviousPageCommand = new RelayCommand(async _ => await GoToPreviousPageAsync(), _ => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(async _ => await GoToNextPageAsync(), _ => CanGoToNextPage);
        LastPageCommand = new RelayCommand(async _ => await GoToLastPageAsync(), _ => CanGoToLastPage);

        _ = LoadDataAsync();
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

    protected virtual async Task CalculateStatsAsync()
    {
        var allPos = await _unitOfWork.PurchaseOrders.GetAllWithSuppliersAsync();
        var posList = allPos.ToList();

        var filteredPos = _excludeCancelled 
            ? posList.Where(x => x.Status != PurchaseOrderStatus.Cancelled).ToList() 
            : posList;

        TotalBc = filteredPos.Count;
        BcValides = filteredPos.Count(x => x.Status == PurchaseOrderStatus.Validated);
        BcEnAttente = filteredPos.Count(x => x.Status == PurchaseOrderStatus.Pending);
        BcAnnules = _excludeCancelled ? 0 : posList.Count(x => x.Status == PurchaseOrderStatus.Cancelled);
        ValeurTotaleBc = filteredPos.Sum(x => x.TotalAmountTTC);
        FournisseursActifs = filteredPos.Where(x => x.SupplierId > 0).Select(x => x.SupplierId).Distinct().Count();

        // Calculate yesterday's data
        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);
        var yesterdayPos = filteredPos.Where(n =>
            n.CreatedAt.Date >= yesterday && n.CreatedAt.Date < today).ToList();

        int yesterdayTotal = yesterdayPos.Count;
        int yesterdayValides = yesterdayPos.Count(x => x.Status == PurchaseOrderStatus.Validated);
        int yesterdayEnAttente = yesterdayPos.Count(x => x.Status == PurchaseOrderStatus.Pending);
        int yesterdayAnnules = yesterdayPos.Count(x => x.Status == PurchaseOrderStatus.Cancelled);
        decimal yesterdayValeur = yesterdayPos.Sum(x => x.TotalAmountTTC);
        int yesterdayFournisseurs = yesterdayPos.Where(x => x.SupplierId > 0).Select(x => x.SupplierId).Distinct().Count();

        // Calculate trend texts
        TotalBcTrendText = CalculateTrendText(TotalBc, yesterdayTotal);
        BcValidesTrendText = CalculateTrendText(BcValides, yesterdayValides);
        BcEnAttenteTrendText = CalculateTrendText(BcEnAttente, yesterdayEnAttente);
        BcAnnulesTrendText = CalculateTrendText(BcAnnules, yesterdayAnnules);
        ValeurTotaleBcTrendText = CalculateTrendText(ValeurTotaleBc, yesterdayValeur);
        FournisseursActifsTrendText = CalculateTrendText(FournisseursActifs, yesterdayFournisseurs);

        // Populate unique suppliers
        Suppliers.Clear();
        Suppliers.Add(""); // Tous les fournisseurs
        var uniqueSuppliers = posList
            .Where(x => x.Supplier != null && !string.IsNullOrWhiteSpace(x.Supplier.CompanyName))
            .Select(x => x.Supplier.CompanyName)
            .Distinct()
            .OrderBy(x => x);
        foreach (var supplier in uniqueSuppliers)
        {
            Suppliers.Add(supplier);
        }
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
            var result = await _unitOfWork.PurchaseOrders.GetPurchaseOrdersPagedAsync(
                CurrentPage,
                PageSize,
                SearchText,
                SelectedSupplier,
                SelectedStatus,
                SearchDate,
                _excludeCancelled,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (version != _loadVersion)
                return;

            // Update UI on dispatcher thread
            if (Application.Current.Dispatcher.CheckAccess())
            {
                UpdateItems(result);
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(() => UpdateItems(result), DispatcherPriority.Normal, cancellationToken);
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

    private void UpdateItems(PagedResult<PurchaseOrderDto> result)
    {
        Orders.Clear();
        foreach (var dto in result.Items)
        {
            Orders.Add(new AdminOrderItemViewModel(dto));
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

    private async Task ExecuteResetFiltersAsync()
    {
        SearchText = string.Empty;
        SelectedSupplier = string.Empty;
        SelectedStatus = string.Empty;
        SearchDate = null;
        await ResetAndLoadPageAsync();
    }

    private async Task ExecuteInspect(AdminOrderItemViewModel? item)
    {
        if (item == null) return;
        
        try
        {
            var poWithDetails = await _unitOfWork.PurchaseOrders.GetWithDetailsAsync(item.Id);
            if (poWithDetails == null) return;

            var window = new Views.Admin.Orders.OrderDetailsWindow();
            window.DataContext = new OrderDetailsViewModel(poWithDetails);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
            
            // Refresh data after inspection in case something changed
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors du chargement des détails : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task ExecutePrintPdf(AdminOrderItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            IsBusy = true;
            var poWithDetails = await _unitOfWork.PurchaseOrders.GetWithDetailsAsync(item.Id);
            if (poWithDetails == null) return;

            var pdfService = _serviceProvider.GetRequiredService<IPdfGeneratorService>();
            string filePath = await pdfService.GeneratePurchaseOrderPdfAsync(poWithDetails);

            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de la génération du PDF : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

public class OrderDetailsViewModel : BaseViewModel
{
    public PurchaseOrder PurchaseOrder { get; }
    public ObservableCollection<PurchaseOrderDetail> Details { get; }

    public string TitleText => $"Détails du Bon de Commande : {PurchaseOrder.OrderNumber}";
    public string SupplierName => PurchaseOrder.Supplier?.CompanyName ?? "Inconnu";
    public string OrderDate => PurchaseOrder.OrderDate.ToString("dd/MM/yyyy");
    public string QuotationRef => PurchaseOrder.Quotation?.ReferenceNumber ?? "N/A";
    public string TotalHT => $"{PurchaseOrder.TotalAmountHT:N2} MAD";
    public string TotalTTC => $"{PurchaseOrder.TotalAmountTTC:N2} MAD";
    public string TotalVAT => $"{PurchaseOrder.TotalVAT:N2} MAD";
    public string Status => PurchaseOrder.Status;

    public OrderDetailsViewModel(PurchaseOrder po)
    {
        PurchaseOrder = po;
        Details = new ObservableCollection<PurchaseOrderDetail>(po.Details ?? new List<PurchaseOrderDetail>());
    }
}
