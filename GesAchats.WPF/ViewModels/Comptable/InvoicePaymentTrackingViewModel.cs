using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using GesAchats.WPF.Views.Comptable.Factures;

namespace GesAchats.WPF.ViewModels.Comptable;

public class InvoicePaymentTrackingViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    private ObservableCollection<InvoiceWithPaymentsViewModel> _allInvoices = new();
    public ObservableCollection<InvoiceWithPaymentsViewModel> FilteredInvoices { get; set; } = new();

    private Supplier? _selectedSupplier;
    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            SetProperty(ref _selectedSupplier, value);
            ApplyFilters();
        }
    }

    private string? _selectedStatus;
    public string? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            SetProperty(ref _selectedStatus, value);
            ApplyFilters();
        }
    }

    private DateTime? _selectedDate;
    public DateTime? SelectedDate
    {
        get => _selectedDate;
        set
        {
            SetProperty(ref _selectedDate, value);
            ApplyFilters();
        }
    }

    private string? _searchInvoiceNumber;
    public string? SearchInvoiceNumber
    {
        get => _searchInvoiceNumber;
        set
        {
            SetProperty(ref _searchInvoiceNumber, value);
            ApplyFilters();
        }
    }

    public ObservableCollection<Supplier> Suppliers { get; set; } = new();
    public ObservableCollection<string> StatusOptions { get; set; } = new()
    {
        "Tous", "Payée", "Partiellement payée", "En attente"
    };

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

    // Nouveaux KPIs
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

    // Trend texts
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

    private List<Payment> _allPayments = new();

    public ICommand LoadDataCommand { get; }
    public ICommand ViewInvoiceCommand { get; }
    public ICommand ResetFiltersCommand { get; }

    public InvoicePaymentTrackingViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        Title = "PV Comptable - Suivi des Factures et Règlements";

        LoadDataCommand = new RelayCommand(async _ => await LoadDataAsync());
        ViewInvoiceCommand = new RelayCommand(async param => await ViewInvoiceAsync(param as InvoiceWithPaymentsViewModel));
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
    }

    public async void OnNavigatedTo(object parameter)
    {
        await LoadDataAsync();
    }

    public async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            // Load all invoices with supplier and payments
            var invoices = await _unitOfWork.Invoices.GetAllIncludingAsync(
                i => i.Supplier,
                i => i.Details
            );

            // Load all payments
            var payments = await _unitOfWork.Payments.GetAllAsync();

            _allInvoices.Clear();
            foreach (var invoice in invoices)
            {
                var vm = new InvoiceWithPaymentsViewModel(invoice);
                var invoicePayments = payments.Where(p => p.InvoiceId == invoice.Id);
                foreach (var payment in invoicePayments)
                {
                    vm.Payments.Add(payment);
                }
                _allInvoices.Add(vm);
            }

            // Load suppliers for filter
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            Suppliers.Clear();
            // Add "Tous" option first
            var tousSupplier = new Supplier { Id = 0, CompanyName = "Tous" };
            Suppliers.Add(tousSupplier);
            foreach (var supplier in suppliers.OrderBy(s => s.CompanyName))
            {
                Suppliers.Add(supplier);
            }
            SelectedSupplier = tousSupplier;

            // Store all payments for trend calculation
            _allPayments = payments.ToList();

            ApplyFilters();

            CalculateStats();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allInvoices.AsEnumerable();

        if (SelectedSupplier != null && SelectedSupplier.Id != 0)
        {
            filtered = filtered.Where(i => i.Invoice.SupplierId == SelectedSupplier.Id);
        }

        if (SelectedStatus != null && SelectedStatus != "Tous")
        {
            filtered = filtered.Where(i => i.StatusCalculated == SelectedStatus);
        }

        if (SelectedDate.HasValue)
        {
            filtered = filtered.Where(i => i.Invoice.InvoiceDate.Date == SelectedDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(SearchInvoiceNumber))
        {
            string search = SearchInvoiceNumber.ToLower();
            filtered = filtered.Where(i => 
                (i.Invoice.ExternalInvoiceNumber != null && i.Invoice.ExternalInvoiceNumber.ToLower().Contains(search)) ||
                (i.Invoice.InvoiceNumber != null && i.Invoice.InvoiceNumber.ToLower().Contains(search))
            );
        }

        FilteredInvoices.Clear();
        foreach (var invoice in filtered)
        {
            FilteredInvoices.Add(invoice);
        }
    }

    private async Task ViewInvoiceAsync(InvoiceWithPaymentsViewModel? invoiceVm)
    {
        if (invoiceVm == null) return;

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

    private void ResetFilters()
    {
        SelectedSupplier = Suppliers.FirstOrDefault();
        SelectedStatus = "Tous";
        SelectedDate = null;
        SearchInvoiceNumber = string.Empty;
    }

    private void CalculateStats()
    {
        TotalTTC = _allInvoices.Sum(i => i.Invoice.AmountTTC);
        TotalPayments = _allInvoices.Sum(i => i.TotalPayments);
        TotalBalance = TotalTTC - TotalPayments;
        PaidInvoicesCount = _allInvoices.Count(i => i.StatusCalculated == "Payée");
        PartialInvoicesCount = _allInvoices.Count(i => i.StatusCalculated == "Partiellement payée");
        BilledSuppliersCount = _allInvoices.Select(i => i.Invoice.SupplierId).Distinct().Count();

        // Calculate yesterday's data
        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);

        var yesterdayInvoices = _allInvoices.Where(i => 
            i.Invoice.CreatedAt.Date >= yesterday && i.Invoice.CreatedAt.Date < today).ToList();
        var yesterdayPayments = _allPayments.Where(p => 
            p.CreatedAt.Date >= yesterday && p.CreatedAt.Date < today).ToList();

        decimal yesterdayTTC = yesterdayInvoices.Sum(i => i.Invoice.AmountTTC);
        decimal yesterdayPaymentAmount = yesterdayPayments.Sum(p => p.AmountPaid);
        decimal yesterdayBalance = yesterdayTTC - yesterdayPaymentAmount;
        int yesterdayPaidCount = yesterdayInvoices.Count(i => i.StatusCalculated == "Payée");
        int yesterdayPartialCount = yesterdayInvoices.Count(i => i.StatusCalculated == "Partiellement payée");
        int yesterdaySuppliers = yesterdayInvoices.Select(i => i.Invoice.SupplierId).Distinct().Count();

        // Calculate trend texts
        TotalTTCTrendText = CalculateTrendText(TotalTTC, yesterdayTTC);
        TotalPaymentsTrendText = CalculateTrendText(TotalPayments, yesterdayPaymentAmount);
        TotalBalanceTrendText = CalculateTrendText(TotalBalance, yesterdayBalance);
        PaidInvoicesCountTrendText = CalculateTrendText(PaidInvoicesCount, yesterdayPaidCount);
        PartialInvoicesCountTrendText = CalculateTrendText(PartialInvoicesCount, yesterdayPartialCount);
        BilledSuppliersCountTrendText = CalculateTrendText(BilledSuppliersCount, yesterdaySuppliers);
    }
}
