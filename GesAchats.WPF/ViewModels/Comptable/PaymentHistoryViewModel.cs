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

    // Chart Properties
    private ISeries[] _supplierDistribution = Array.Empty<ISeries>();
    public ISeries[] SupplierDistribution
    {
        get => _supplierDistribution;
        set => SetProperty(ref _supplierDistribution, value);
    }

    private ISeries[] _paymentsByDate = Array.Empty<ISeries>();
    public ISeries[] PaymentsByDate
    {
        get => _paymentsByDate;
        set => SetProperty(ref _paymentsByDate, value);
    }

    private ISeries[] _paymentMethodsDistribution = Array.Empty<ISeries>();
    public ISeries[] PaymentMethodsDistribution
    {
        get => _paymentMethodsDistribution;
        set => SetProperty(ref _paymentMethodsDistribution, value);
    }

    private Axis[] _xAxesPayments = Array.Empty<Axis>();
    public Axis[] XAxesPayments
    {
        get => _xAxesPayments;
        set => SetProperty(ref _xAxesPayments, value);
    }

    private Axis[] _xAxesMethods = Array.Empty<Axis>();
    public Axis[] XAxesMethods
    {
        get => _xAxesMethods;
        set => SetProperty(ref _xAxesMethods, value);
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

    public PaymentHistoryViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _fileStorageService = fileStorageService;
        Title = "Suivi des Règlements";

        LoadPaymentsCommand = new RelayCommand(async _ => await LoadPaymentsAsync());
        ExportExcelCommand = new RelayCommand(_ => ExportToExcel());
        AddPaymentCommand = new RelayCommand(_ => _navigationService.NavigateTo("PaymentForm"));
        ViewProofCommand = new RelayCommand(p => ViewProof(p as Payment));
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
        ToggleChartsCommand = new RelayCommand(_ => ShowCharts = !ShowCharts);

        SelectedPaymentMethod = "Tous";

        _ = LoadPaymentsAsync();
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
            _allPayments = new ObservableCollection<Payment>(payments.OrderByDescending(p => p.PaymentDate));
            ApplyFilters();
            
            TotalPaidMonth = Payments
                .Where(p => p.PaymentDate.Month == DateTime.Now.Month && p.PaymentDate.Year == DateTime.Now.Year)
                .Sum(p => p.AmountPaid);
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
            SupplierDistribution = Array.Empty<ISeries>();
            PaymentsByDate = Array.Empty<ISeries>();
            PaymentMethodsDistribution = Array.Empty<ISeries>();
            return;
        }

        // 1. Distribution par fournisseur (Pie Chart)
        var supplierData = Payments
            .GroupBy(p => p.Supplier?.CompanyName ?? "Inconnu")
            .Select(g => new { Name = g.Key, Total = (double)g.Sum(p => p.AmountPaid) })
            .OrderByDescending(x => x.Total)
            .Take(5) // Top 5
            .ToList();

        SupplierDistribution = supplierData.Select(s => new PieSeries<double>
        {
            Values = new double[] { s.Total },
            Name = s.Name,
            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
            DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:N2} MAD"
        }).ToArray();

        // 2. Modes de paiement (Bar Chart)
        var methodData = Payments
            .GroupBy(p => p.PaymentMethod ?? "Non spécifié")
            .Select(g => new { Method = g.Key, Total = (double)g.Sum(p => p.AmountPaid) })
            .ToList();

        PaymentMethodsDistribution = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = methodData.Select(x => x.Total).ToArray(),
                Name = "Montant par Mode",
                Fill = new SolidColorPaint(SKColors.MidnightBlue),
                Padding = 20
            }
        };

        XAxesMethods = new Axis[]
        {
            new Axis
            {
                Labels = methodData.Select(x => x.Method).ToArray(),
                LabelsRotation = -45
            }
        };

        // 3. Évolution des paiements par date (Line Chart)
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

    private void ExportToExcel()
    {
        // TODO: Implémenter ExportService
    }
}
