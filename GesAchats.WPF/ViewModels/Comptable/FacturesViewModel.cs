using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using GesAchats.WPF.Views.Comptable.Factures;

namespace GesAchats.WPF.ViewModels.Comptable;

public class FacturesViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;

    private ObservableCollection<InvoiceWithPaymentsViewModel> _allFactures = new();
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
            if (SetProperty(ref _selectedSupplier, value))
                ApplyFilters();
        }
    }

    private string _selectedStatus = "Tous";
    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
                ApplyFilters();
        }
    }

    private DateTime? _startDate;
    public DateTime? StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value))
                ApplyFilters();
        }
    }

    private DateTime? _endDate;
    public DateTime? EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value))
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

    // Stats
    private decimal _totalAmount;
    public decimal TotalAmount
    {
        get => _totalAmount;
        set => SetProperty(ref _totalAmount, value);
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

    private InvoiceWithPaymentsViewModel? _selectedFacture;
    public InvoiceWithPaymentsViewModel? SelectedFacture
    {
        get => _selectedFacture;
        set => SetProperty(ref _selectedFacture, value);
    }

    public ICommand LoadFacturesCommand { get; }
    public ICommand AddFactureCommand { get; }
    public ICommand VerifyConformityCommand { get; }
    public ICommand RegisterPaymentCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand ViewBCCommand { get; }
    public ICommand ViewBLCommand { get; }
    public ICommand ViewFileCommand { get; }

    public FacturesViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        Title = "Factures Fournisseurs";

        LoadFacturesCommand = new RelayCommand(async _ => await LoadFacturesAsync());
        AddFactureCommand = new RelayCommand(_ => _navigationService.NavigateTo("InvoiceForm"));
        
        ViewDetailsCommand = new RelayCommand(_ => 
        {
            if (SelectedFacture == null)
                return;
            
            using (var scope = _serviceProvider.CreateScope())
            {
                var vm = ActivatorUtilities.CreateInstance<FactureDetailsViewModel>(scope.ServiceProvider, SelectedFacture.Invoice.Id);
                var win = ActivatorUtilities.CreateInstance<FactureDetailsWindow>(scope.ServiceProvider, vm);
                win.Owner = System.Windows.Application.Current.MainWindow;
                win.ShowDialog();
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
                _navigationService.NavigateTo("ConformityCheck", SelectedFacture.Invoice.Id);
        }, _ => SelectedFacture != null);
        
        RegisterPaymentCommand = new RelayCommand(_ => 
        {
            if (SelectedFacture != null)
                _navigationService.NavigateTo("PaymentForm", SelectedFacture.Invoice.Id);
        }, _ => SelectedFacture != null);

        _startDate = DateTime.Today.AddMonths(-1);
        _endDate = DateTime.Today;

        _ = LoadFacturesAsync();
    }

    private async Task LoadFacturesAsync()
    {
        IsBusy = true;
        try
        {
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            Suppliers = new ObservableCollection<Supplier>(suppliers.OrderBy(s => s.CompanyName));

            // Charger les factures avec les entités liées
            var factures = await _unitOfWork.Invoices.GetAllIncludingAsync(
                f => f.Supplier,
                f => f.PurchaseOrder,
                f => f.DeliveryNote
            );

            // Charger les paiements
            var payments = await _unitOfWork.Payments.GetAllAsync();

            // Créer les viewmodels avec les paiements
            _allFactures = new ObservableCollection<InvoiceWithPaymentsViewModel>();
            foreach (var invoice in factures.OrderByDescending(f => f.InvoiceDate))
            {
                var invoiceVm = new InvoiceWithPaymentsViewModel(invoice);
                var invoicePayments = payments.Where(p => p.InvoiceId == invoice.Id);
                foreach (var payment in invoicePayments)
                {
                    invoiceVm.Payments.Add(payment);
                }
                _allFactures.Add(invoiceVm);
            }
            
            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allFactures.AsEnumerable();

        if (SelectedSupplier != null)
            filtered = filtered.Where(f => f.Invoice.SupplierId == SelectedSupplier.Id);

        if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Tous")
            filtered = filtered.Where(f => f.StatusCalculated == SelectedStatus);

        if (StartDate.HasValue)
            filtered = filtered.Where(f => f.Invoice.InvoiceDate.Date >= StartDate.Value.Date);

        if (EndDate.HasValue)
            filtered = filtered.Where(f => f.Invoice.InvoiceDate.Date <= EndDate.Value.Date);

        if (!string.IsNullOrEmpty(SearchText))
        {
            string search = SearchText.ToLower();
            filtered = filtered.Where(f => 
                f.Invoice.InvoiceNumber.ToLower().Contains(search) ||
                f.Invoice.Supplier.CompanyName.ToLower().Contains(search) ||
                (f.Invoice.PurchaseOrder != null && f.Invoice.PurchaseOrder.OrderNumber.ToLower().Contains(search)) ||
                (f.Invoice.DeliveryNote != null && f.Invoice.DeliveryNote.DeliveryNumber.ToLower().Contains(search))
            );
        }

        Factures = new ObservableCollection<InvoiceWithPaymentsViewModel>(filtered);
        CalculateStats();
    }

    private void CalculateStats()
    {
        TotalAmount = Factures.Sum(f => f.Invoice.AmountTTC);
        
        // Calculer le solde total
        PendingAmount = Factures.Sum(f => f.Balance);
        
        decimal totalPaid = Factures.Sum(f => f.TotalPayments);
        PaymentRate = TotalAmount > 0 ? (double)(totalPaid / TotalAmount * 100) : 0;
    }
}
