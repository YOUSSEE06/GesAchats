using System.Collections.ObjectModel;
using System.Windows.Input;
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

namespace GesAchats.WPF.ViewModels.Comptable;

public class PaymentHistoryViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUserSession _userSession;

    private ObservableCollection<Payment> _allPayments = new();
    private ObservableCollection<Payment> _payments = new();
    public ObservableCollection<Payment> Payments
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
                ApplyFilters();
        }
    }

    private DateTime? _selectedDate;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            if (SetProperty(ref _selectedDate, value))
                ApplyFilters();
        }
    }

    private string? _selectedPaymentMethod;
    public string? SelectedPaymentMethod
    {
        get => _selectedPaymentMethod;
        set
        {
            if (SetProperty(ref _selectedPaymentMethod, value))
                ApplyFilters();
        }
    }

    private string _searchText = string.Empty;
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilters();
        }
    }

    private string _searchInvoiceNumber = string.Empty;
    public string SearchInvoiceNumber
    {
        get => _searchInvoiceNumber;
        set
        {
            if (SetProperty(ref _searchInvoiceNumber, value))
                ApplyFilters();
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

    // Chart Properties
    private IEnumerable<ISeries> _paymentMethodSeries = [];
    public IEnumerable<ISeries> PaymentMethodSeries
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

    private ISeries[] _paymentsByDate = Array.Empty<ISeries>();
    public ISeries[] PaymentsByDate
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

    private bool _showCharts = true;
    public bool ShowCharts
    {
        get => _showCharts;
        set => SetProperty(ref _showCharts, value);
    }

    public bool CanAddReglement => _userSession.HasRole("COMPTABLE");

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
        ViewProofCommand = new RelayCommand(p => ViewProof(p as Payment));
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
        ToggleChartsCommand = new RelayCommand(_ => ShowCharts = !ShowCharts);

        SelectedPaymentMethod = "Tous";
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

    private void ViewProof(Payment? payment)
    {
        if (payment == null || string.IsNullOrWhiteSpace(payment.ProofFilePath))
            return;

        try
        {
            string fullPath = _fileStorageService.GetFullPath(payment.ProofFilePath);
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
        IsBusy = true;
        try
        {
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            var supplierList = new ObservableCollection<Supplier>();
            supplierList.Add(new Supplier { Id = 0, CompanyName = "Tous" });
            foreach (var supplier in suppliers.OrderBy(s => s.CompanyName))
            {
                supplierList.Add(supplier);
            }
            Suppliers = supplierList;
            SelectedSupplier = supplierList.First();

            var payments = await _unitOfWork.Payments.GetAllIncludingAsync(
                p => p.Supplier,
                p => p.Invoice,
                p => p.CreatedBy
            );
            var allPaymentsList = payments.ToList();
            _allPayments = new ObservableCollection<Payment>(allPaymentsList.OrderByDescending(p => p.PaymentDate));
            ApplyFilters();
            
            // Calcul des KPIs (Données réelles)
            var now = DateTime.Now;
            
            // KPI 1: Total payé (Mois)
            TotalPaidMonth = allPaymentsList
                .Where(p => p.PaymentDate.Month == now.Month && p.PaymentDate.Year == now.Year)
                .Sum(p => p.AmountPaid);

            // KPI 4: Total Réglé
            TotalAmountRegle = allPaymentsList.Sum(p => p.AmountPaid);

            // KPI 5: Fournisseurs Payés
            FournisseursPayesCount = allPaymentsList.Select(p => p.SupplierId).Distinct().Count();

            // KPI 6: Total Opérations
            TotalOperationsReglementCount = allPaymentsList.Count;

            // Pour les factures en attente et retards
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            
            // KPI 2: Factures Attente
            PendingInvoicesCount = invoices.Count(i => i.Status == "EnAttente");

            // KPI 3: Retards Paiement (Dépassement date d'échéance et non payée)
            LatePaymentsCount = invoices.Count(i => i.DueDate.HasValue && i.DueDate.Value < now && i.Status != "Payee" && i.Status != "Rejetee");

            // Calculate yesterday's data for trends
            DateTime today = DateTime.Today;
            DateTime yesterday = today.AddDays(-1);

            var yesterdayPayments = allPaymentsList.Where(p => 
                p.CreatedAt.Date >= yesterday && p.CreatedAt.Date < today).ToList();
            var yesterdayInvoices = invoices.Where(i => 
                i.CreatedAt.Date >= yesterday && i.CreatedAt.Date < today).ToList();

            decimal yesterdayPaidMonth = yesterdayPayments
                .Where(p => p.PaymentDate.Month == today.Month && p.PaymentDate.Year == today.Year)
                .Sum(p => p.AmountPaid);
            int yesterdayPendingInvoices = yesterdayInvoices.Count(i => i.Status == "EnAttente");
            int yesterdayLatePayments = yesterdayInvoices.Count(i => i.DueDate.HasValue && i.DueDate.Value < today && i.Status != "Payee" && i.Status != "Rejetee");
            decimal yesterdayAmountRegle = yesterdayPayments.Sum(p => p.AmountPaid);
            int yesterdayFournisseursPayes = yesterdayPayments.Select(p => p.SupplierId).Distinct().Count();
            int yesterdayOperations = yesterdayPayments.Count;

            // Calculate trend texts
            TotalPaidMonthTrendText = CalculateTrendText(TotalPaidMonth, yesterdayPaidMonth);
            PendingInvoicesCountTrendText = CalculateTrendText(PendingInvoicesCount, yesterdayPendingInvoices);
            LatePaymentsCountTrendText = CalculateTrendText(LatePaymentsCount, yesterdayLatePayments);
            TotalAmountRegleTrendText = CalculateTrendText(TotalAmountRegle, yesterdayAmountRegle);
            FournisseursPayesCountTrendText = CalculateTrendText(FournisseursPayesCount, yesterdayFournisseursPayes);
            TotalOperationsReglementCountTrendText = CalculateTrendText(TotalOperationsReglementCount, yesterdayOperations);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allPayments.AsEnumerable();

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
                (p.Invoice != null && p.Invoice.ExternalInvoiceNumber != null && p.Invoice.ExternalInvoiceNumber.ToLower().Contains(searchInv)) ||
                (p.ReferenceNumber != null && p.ReferenceNumber.ToLower().Contains(searchInv))
            );
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string search = SearchText.ToLower();
            filtered = filtered.Where(p => 
                (p.ReferenceNumber != null && p.ReferenceNumber.ToLower().Contains(search)) ||
                (p.PaymentNumber != null && p.PaymentNumber.ToLower().Contains(search))
            );
        }

        Payments = new ObservableCollection<Payment>(filtered);
        UpdateCharts();
    }

    private void UpdateCharts()
    {
        if (Payments == null || !Payments.Any())
        {
            PaymentMethodSeries = [];
            PaymentsByDate = Array.Empty<ISeries>();
            TotalPaymentMethodAmount = 0;
            return;
        }

        // 1. Répartition par mode de paiement (Doughnut Chart)
        var methodData = Payments
            .GroupBy(p => NormalizePaymentMode(p.PaymentMethod))
            .Select(g => new { Method = g.Key, Total = (double)g.Sum(p => p.AmountPaid) })
            .ToList();

        TotalPaymentMethodAmount = methodData.Sum(x => x.Total);

        PaymentMethodSeries = methodData.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Total },
            Name = d.Method,
            InnerRadius = 55,
            Fill = d.Method switch
            {
                "Virement" => new SolidColorPaint(new SKColor(34, 197, 94)),
                "Chèque" => new SolidColorPaint(new SKColor(245, 158, 11)),
                "Espèce" => new SolidColorPaint(new SKColor(168, 85, 246)),
                "Lettres d'échange" => new SolidColorPaint(new SKColor(59, 130, 246)),
                "Lettre d'échange" => new SolidColorPaint(new SKColor(59, 130, 246)),
                _ => new SolidColorPaint(SKColors.Gray)
            }
        }).ToList();

        // 2. Évolution des paiements par date (Line Chart)
        var dateData = Payments
            .GroupBy(p => p.PaymentDate.Date)
            .Select(g => new { Date = g.Key, Total = (double)g.Sum(p => p.AmountPaid) })
            .OrderBy(x => x.Date)
            .ToList();

        PaymentsByDate = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = dateData.Select(x => x.Total).ToArray(),
                Name = "Paiements (MAD)",
                Fill = new SolidColorPaint(SKColors.DeepSkyBlue.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.DeepSkyBlue, 3),
                GeometrySize = 10,
                LineSmoothness = 1
            }
        };

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
