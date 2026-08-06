using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using System.Diagnostics;
using System.Windows;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Microsoft.EntityFrameworkCore;
using AsyncRelayCommand = CommunityToolkit.Mvvm.Input.AsyncRelayCommand;
using IAsyncRelayCommand = CommunityToolkit.Mvvm.Input.IAsyncRelayCommand;

namespace GesAchats.WPF.ViewModels.Comptable;

public class PaymentHistoryViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUserSession _userSession;
    
    // Debounce timer and CTS
    private CancellationTokenSource? _loadCts;
    private System.Windows.Threading.DispatcherTimer? _searchDebounceTimer;
    
    private ObservableCollection<PaymentListDto> _payments = new();
    public ObservableCollection<PaymentListDto> Payments
    {
        get => _payments;
        set => SetProperty(ref _payments, value);
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
            if (SetProperty(ref _selectedSupplier, value))
                OnFilterChanged();
        }
    }

    private DateTime? _selectedDate;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
                OnFilterChanged();
        }
    }

    private string? _selectedPaymentMethod;
    public string? SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            if (SetProperty(ref _selectedPaymentMethod, value))
                OnFilterChanged();
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                OnFilterChanged();
        }
    }

    private string _searchInvoiceNumber = string.Empty;
    public string SearchInvoiceNumber
    {
        get => _searchInvoiceNumber;
        set
        {
            if (SetProperty(ref _searchInvoiceNumber, value))
                DebounceFilter();
        }
    }

    public ObservableCollection<string> PaymentMethods { get; set; } = new()
    {
        "Tous", "Virement", "Chèque", "Lettre de change"
    };

    private decimal _totalPaidMonth;
    public decimal TotalPaidMonth
    {
        get => _totalPaidMonth;
        set => SetProperty(ref _totalPaidMonth, value);
    }

    private int _pendingInvoicesCount;
    public int PendingInvoicesCount
    {
        get => _pendingInvoicesCount;
        set => SetProperty(ref _pendingInvoicesCount, value);
    }

    private int _latePaymentsCount;
    public int LatePaymentsCount
    {
        get => _latePaymentsCount;
        set => SetProperty(ref _latePaymentsCount, value);
    }

    private decimal _totalAmountRegle;
    public decimal TotalAmountRegle
    {
        get => _totalAmountRegle;
        set => SetProperty(ref _totalAmountRegle, value);
    }

    private int _fournisseursPayesCount;
    public int FournisseursPayesCount
    {
        get => _fournisseursPayesCount;
        set => SetProperty(ref _fournisseursPayesCount, value);
    }

    private int _totalOperationsReglementCount;
    public int TotalOperationsReglementCount
    {
        get => _totalOperationsReglementCount;
        set => SetProperty(ref _totalOperationsReglementCount, value);
    }

    // Filtre de période (identique au Dashboard principal)
    private DateTime _startDate = DateTime.Today.AddMonths(-1);
    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    private DateTime _endDate = DateTime.Today;
    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    // Couleurs de tendance KPI
    private bool _totalPaidMonthIsPositiveTrend = true;
    public bool TotalPaidMonthIsPositiveTrend { get => _totalPaidMonthIsPositiveTrend; set => SetProperty(ref _totalPaidMonthIsPositiveTrend, value); }

    private bool _pendingInvoicesCountIsPositiveTrend = true;
    public bool PendingInvoicesCountIsPositiveTrend { get => _pendingInvoicesCountIsPositiveTrend; set => SetProperty(ref _pendingInvoicesCountIsPositiveTrend, value); }

    private bool _latePaymentsCountIsPositiveTrend = true;
    public bool LatePaymentsCountIsPositiveTrend { get => _latePaymentsCountIsPositiveTrend; set => SetProperty(ref _latePaymentsCountIsPositiveTrend, value); }

    private bool _totalAmountRegleIsPositiveTrend = true;
    public bool TotalAmountRegleIsPositiveTrend { get => _totalAmountRegleIsPositiveTrend; set => SetProperty(ref _totalAmountRegleIsPositiveTrend, value); }

    private bool _fournisseursPayesCountIsPositiveTrend = true;
    public bool FournisseursPayesCountIsPositiveTrend { get => _fournisseursPayesCountIsPositiveTrend; set => SetProperty(ref _fournisseursPayesCountIsPositiveTrend, value); }

    private bool _totalOperationsReglementCountIsPositiveTrend = true;
    public bool TotalOperationsReglementCountIsPositiveTrend { get => _totalOperationsReglementCountIsPositiveTrend; set => SetProperty(ref _totalOperationsReglementCountIsPositiveTrend, value); }

    // Trend texts
    private string _totalPaidMonthTrendText = string.Empty;
    public string TotalPaidMonthTrendText
    {
        get => _totalPaidMonthTrendText;
        set => SetProperty(ref _totalPaidMonthTrendText, value);
    }

    private string _pendingInvoicesCountTrendText = string.Empty;
    public string PendingInvoicesCountTrendText
    {
        get => _pendingInvoicesCountTrendText;
        set => SetProperty(ref _pendingInvoicesCountTrendText, value);
    }

    private string _latePaymentsCountTrendText = string.Empty;
    public string LatePaymentsCountTrendText
    {
        get => _latePaymentsCountTrendText;
        set => SetProperty(ref _latePaymentsCountTrendText, value);
    }

    private string _totalAmountRegleTrendText = string.Empty;
    public string TotalAmountRegleTrendText
    {
        get => _totalAmountRegleTrendText;
        set => SetProperty(ref _totalAmountRegleTrendText, value);
    }

    private string _fournisseursPayesCountTrendText = string.Empty;
    public string FournisseursPayesCountTrendText
    {
        get => _fournisseursPayesCountTrendText;
        set => SetProperty(ref _fournisseursPayesCountTrendText, value);
    }

    private string _totalOperationsReglementCountTrendText = string.Empty;
    public string TotalOperationsReglementCountTrendText
    {
        get => _totalOperationsReglementCountTrendText;
        set => SetProperty(ref _totalOperationsReglementCountTrendText, value);
    }

    // Chart Properties - Use collections that can be updated instead of replaced
    private ObservableCollection<ISeries> _paymentMethodSeries = new();
    public ObservableCollection<ISeries> PaymentMethodSeries
    {
        get => _paymentMethodSeries;
        set => SetProperty(ref _paymentMethodSeries, value);
    }

    private double _totalPaymentMethodAmount;
    public double TotalPaymentMethodAmount
    {
        get => _totalPaymentMethodAmount;
        set => SetProperty(ref _totalPaymentMethodAmount, value);
    }

    private ObservableCollection<ISeries> _paymentsByDate = new();
    public ObservableCollection<ISeries> PaymentsByDate
    {
        get => _paymentsByDate;
        set => SetProperty(ref _paymentsByDate, value);
    }

    private Axis[] _xAxesPayments = Array.Empty<Axis>();
    public Axis[] XAxesPayments
    {
        get => _xAxesPayments;
        set => SetProperty(ref _xAxesPayments, value);
    }

    public ICommand LoadPaymentsCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand AddPaymentCommand { get; }
    public ICommand ViewProofCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand ToggleChartsCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    private bool _showCharts = true;
    public bool ShowCharts
    {
        get => _showCharts;
        set => SetProperty(ref _showCharts, value);
    }

    public bool CanAddReglement => _userSession.HasRole("COMPTABLE");

    // Cached data
    private List<PaymentListDto> _allPaymentsCache = new();

    public PaymentHistoryViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IFileStorageService fileStorageService, IUserSession userSession)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _fileStorageService = fileStorageService;
        _userSession = userSession;
        Title = "Suivi des Règlements";

        LoadPaymentsCommand = new RelayCommand(async _ => await LoadPaymentsAsync());
        ExportExcelCommand = new RelayCommand(_ => ExportToExcel());
        AddPaymentCommand = new RelayCommand(_ => _navigationService.NavigateTo("PaymentForm"));
        ViewProofCommand = new RelayCommand(p => ViewProof(p as PaymentListDto));
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
        ToggleChartsCommand = new RelayCommand(_ => ShowCharts = !ShowCharts);
        RefreshCommand = new AsyncRelayCommand(LoadPaymentsAsync);

        SelectedPaymentMethod = "Tous";
    }

    private void DebounceFilter()
    {
        if (_searchDebounceTimer != null)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Tick -= OnSearchDebounceTimerTick;
        }

        _searchDebounceTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _searchDebounceTimer.Tick += OnSearchDebounceTimerTick;
        _searchDebounceTimer.Start();
    }

    private void OnSearchDebounceTimerTick(object? sender, EventArgs e)
    {
        if (_searchDebounceTimer != null)
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Tick -= OnSearchDebounceTimerTick;
        }
        ApplyFilters();
    }

    private void OnFilterChanged()
    {
        // For non-search filters, apply immediately without debounce
        if (string.IsNullOrWhiteSpace(SearchInvoiceNumber))
        {
            ApplyFilters();
        }
    }

    private void ResetFilters()
    {
        SelectedSupplier = Suppliers.FirstOrDefault();
        SelectedDate = null;
        SelectedPaymentMethod = "Tous";
        SearchText = string.Empty;
        SearchInvoiceNumber = string.Empty;
    }

    public void OnNavigatedTo(object parameter)
    {
        _ = LoadPaymentsAsync();
    }

    private async void ViewProof(PaymentListDto? payment)
    {
        if (payment == null || string.IsNullOrWhiteSpace(payment.ProofFilePath))
            return;

        try
        {
            string fullPath = await _fileStorageService.GetFullPathAsync(payment.ProofFilePath);
            if (!System.IO.File.Exists(fullPath))
            {
                MessageBox.Show($"Fichier introuvable : {fullPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadPaymentsAsync()
    {
        // Cancel any ongoing load
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();
        var ct = _loadCts.Token;
        
        IsBusy = true;
        try
        {
            // Load suppliers
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync(ct);
            var supplierList = new ObservableCollection<Supplier>
            {
                new Supplier { Id = 0, CompanyName = "Tous" }
            };
            foreach (var supplier in suppliers.OrderBy(s => s.CompanyName))
            {
                supplierList.Add(supplier);
            }
            Suppliers = supplierList;
            SelectedSupplier ??= supplierList.First();

            // Load payments with projections to DTO (no tracking, only needed columns)
            var paymentsQuery = _unitOfWork.Payments.GetQueryable(true)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentListDto
                {
                    Id = p.Id,
                    PaymentDate = p.PaymentDate,
                    SupplierId = p.SupplierId,
                    SupplierCompanyName = p.Supplier.CompanyName,
                    InvoiceId = p.InvoiceId,
                    InvoiceNumber = p.Invoice.ExternalInvoiceNumber,
                    AmountPaid = p.AmountPaid,
                    PaymentMethod = p.PaymentMethod,
                    ReferenceNumber = p.ReferenceNumber,
                    ProofFilePath = p.ProofFilePath
                });

            _allPaymentsCache = await paymentsQuery.ToListAsync(ct);
            
            // Période actuelle vs période précédente de même durée (comme le Dashboard principal)
            var start = StartDate.Date;
            var end = EndDate.Date.AddDays(1);
            var duration = EndDate - StartDate;
            var previousStart = StartDate - duration;
            var previousEnd = StartDate;

            var currentPayments = _allPaymentsCache
                .Where(p => p.PaymentDate >= start && p.PaymentDate < end).ToList();
            var previousPayments = _allPaymentsCache
                .Where(p => p.PaymentDate >= previousStart && p.PaymentDate < previousEnd).ToList();

            // KPI 1 : Total payé sur la période
            TotalPaidMonth = currentPayments.Sum(p => p.AmountPaid);
            var previousPaidMonth = previousPayments.Sum(p => p.AmountPaid);

            // KPI 4 : Total réglé (cumul tous temps) vs cumul jusqu'à la fin de la période précédente
            var totalAmountAllTime = _allPaymentsCache.Sum(p => p.AmountPaid);
            var amountUntilPreviousEnd = _allPaymentsCache
                .Where(p => p.PaymentDate < previousEnd).Sum(p => p.AmountPaid);
            TotalAmountRegle = totalAmountAllTime;

            // KPI 5 : Fournisseurs payés sur la période
            FournisseursPayesCount = currentPayments.Select(p => p.SupplierId).Distinct().Count();
            var previousFournisseursPayes = previousPayments.Select(p => p.SupplierId).Distinct().Count();

            // KPI 6 : Total opérations sur la période
            TotalOperationsReglementCount = currentPayments.Count;
            var previousOperations = previousPayments.Count;

            // KPI 2 : Factures en attente créées sur la période
            PendingInvoicesCount = await _unitOfWork.Invoices.GetQueryable(true)
                .CountAsync(i => i.InvoiceDate >= start && i.InvoiceDate < end && i.Status == "EnAttente", ct);
            var previousPendingInvoices = await _unitOfWork.Invoices.GetQueryable(true)
                .CountAsync(i => i.InvoiceDate >= previousStart && i.InvoiceDate < previousEnd && i.Status == "EnAttente", ct);

            // KPI 3 : Retards de paiement (factures de la période échues et non payées)
            LatePaymentsCount = await _unitOfWork.Invoices.GetQueryable(true)
                .CountAsync(i => i.InvoiceDate >= start && i.InvoiceDate < end && i.DueDate.HasValue && i.DueDate.Value < DateTime.Now && i.Status != "Payee" && i.Status != "Rejetee", ct);
            var previousLatePayments = await _unitOfWork.Invoices.GetQueryable(true)
                .CountAsync(i => i.InvoiceDate >= previousStart && i.InvoiceDate < previousEnd && i.DueDate.HasValue && i.DueDate.Value < DateTime.Now && i.Status != "Payee" && i.Status != "Rejetee", ct);

            // Tendances (texte + couleur), formule ((current - previous) / previous) * 100
            TotalPaidMonthTrendText = FormatTrend(CalculateVariation(TotalPaidMonth, previousPaidMonth), out bool tpPos);
            TotalPaidMonthIsPositiveTrend = tpPos;
            PendingInvoicesCountTrendText = FormatTrend(CalculateVariation(PendingInvoicesCount, previousPendingInvoices), out bool piPos);
            PendingInvoicesCountIsPositiveTrend = piPos;
            LatePaymentsCountTrendText = FormatTrend(CalculateVariation(LatePaymentsCount, previousLatePayments), out bool lpPos);
            LatePaymentsCountIsPositiveTrend = lpPos;
            TotalAmountRegleTrendText = FormatTrend(CalculateVariation(TotalAmountRegle, amountUntilPreviousEnd), out bool arPos);
            TotalAmountRegleIsPositiveTrend = arPos;
            FournisseursPayesCountTrendText = FormatTrend(CalculateVariation(FournisseursPayesCount, previousFournisseursPayes), out bool fpPos);
            FournisseursPayesCountIsPositiveTrend = fpPos;
            TotalOperationsReglementCountTrendText = FormatTrend(CalculateVariation(TotalOperationsReglementCount, previousOperations), out bool opPos);
            TotalOperationsReglementCountIsPositiveTrend = opPos;

            // Apply initial filters
            ApplyFilters();
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelling
        }
        finally
        {
            if (!ct.IsCancellationRequested)
                IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allPaymentsCache.AsEnumerable();

        if (SelectedSupplier != null && SelectedSupplier.Id != 0)
            filtered = filtered.Where(p => p.SupplierId == SelectedSupplier.Id);

        if (SelectedDate.HasValue)
            filtered = filtered.Where(p => p.PaymentDate.Date == SelectedDate.Value.Date);

        if (!string.IsNullOrEmpty(SelectedPaymentMethod) && SelectedPaymentMethod != "Tous")
            filtered = filtered.Where(p => p.PaymentMethod == SelectedPaymentMethod);

        if (!string.IsNullOrWhiteSpace(SearchInvoiceNumber))
        {
            string searchInv = SearchInvoiceNumber.ToLower();
            filtered = filtered.Where(p => 
                (p.InvoiceNumber != null && p.InvoiceNumber.ToLower().Contains(searchInv)) ||
                (p.ReferenceNumber != null && p.ReferenceNumber.ToLower().Contains(searchInv))
            );
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string search = SearchText.ToLower();
            filtered = filtered.Where(p => 
                (p.ReferenceNumber != null && p.ReferenceNumber.ToLower().Contains(search))
            );
        }

        // Update Payments collection in one go
        Payments = new ObservableCollection<PaymentListDto>(filtered);
        UpdateCharts();
    }

    private void UpdateCharts()
    {
        if (Payments == null || !Payments.Any())
        {
            PaymentMethodSeries.Clear();
            PaymentsByDate.Clear();
            TotalPaymentMethodAmount = 0;
            return;
        }

        // 1. Répartition par mode de paiement (Doughnut Chart)
        var methodData = Payments
            .GroupBy(p => NormalizePaymentMode(p.PaymentMethod))
            .Select(g => new { Method = g.Key, Total = (double)g.Sum(p => p.AmountPaid) })
            .ToList();

        TotalPaymentMethodAmount = methodData.Sum(x => x.Total);

        // Update PaymentMethodSeries
        PaymentMethodSeries.Clear();
        foreach (var data in methodData)
        {
            var color = data.Method switch
            {
                "Virement" => new SKColor(34, 197, 94),
                "Chèque" => new SKColor(245, 158, 11),
                "Espèce" => new SKColor(168, 85, 246),
                "Lettres d'échange" => new SKColor(59, 130, 246),
                "Lettre d'échange" => new SKColor(59, 130, 246),
                _ => SKColors.Gray
            };
            
            PaymentMethodSeries.Add(new PieSeries<double>
            {
                Values = new[] { data.Total },
                Name = data.Method,
                InnerRadius = 55,
                Fill = new SolidColorPaint(color)
            });
        }

        // 2. Évolution des paiements par date (Line Chart)
        var dateData = Payments
            .GroupBy(p => p.PaymentDate.Date)
            .Select(g => new { Date = g.Key, Total = (double)g.Sum(p => p.AmountPaid) })
            .OrderBy(x => x.Date)
            .ToList();

        PaymentsByDate.Clear();
        PaymentsByDate.Add(new LineSeries<double>
        {
            Values = dateData.Select(x => x.Total).ToArray(),
            Name = "Paiements (MAD)",
            Fill = new SolidColorPaint(SKColors.DeepSkyBlue.WithAlpha(50)),
            Stroke = new SolidColorPaint(SKColors.DeepSkyBlue, 3),
            GeometrySize = 10,
            LineSmoothness = 1
        });

        XAxesPayments = new Axis[]
        {
            new Axis
            {
                Labels = dateData.Select(x => x.Date.ToString("dd/MM")).ToArray()
            }
        };
    }
    
    private string NormalizePaymentMode(string paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
            return "Non défini";
            
        var normalized = paymentMethod.Trim().ToLowerInvariant();
        
        if (normalized.Contains("cheque") || normalized.Contains("chèque"))
            return "Chèque";
        if (normalized.Contains("virement"))
            return "Virement";
        if (normalized.Contains("espece") || normalized.Contains("espèce"))
            return "Espèce";
        if (normalized.Contains("lettre") && (normalized.Contains("echange") || normalized.Contains("échange")) || normalized == "le")
            return "Lettres d'échange";
            
        return paymentMethod.Trim();
    }

    private void ExportToExcel()
    {
        // TODO: Implémenter ExportService
    }
}
