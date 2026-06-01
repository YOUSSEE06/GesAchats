using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using GesAchats.WPF.Views.Comptable.Factures;

namespace GesAchats.WPF.ViewModels.Comptable;

public class FacturesViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUserSession _userSession;

    private bool _isAdmin;
    public bool IsAdmin
    {
        get => _isAdmin;
        set => SetProperty(ref _isAdmin, value);
    }

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
    public ICommand ResetFiltersCommand { get; }

    public FacturesViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IServiceProvider serviceProvider, IUserSession userSession)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _serviceProvider = serviceProvider;
        _userSession = userSession;
        Title = "Factures Fournisseurs";
        IsAdmin = _userSession.CurrentUser?.Role?.Code?.ToUpper() == "ADMIN";

        LoadFacturesCommand = new RelayCommand(async _ => await LoadFacturesAsync());
        AddFactureCommand = new RelayCommand(_ => _navigationService.NavigateTo("InvoiceForm"));
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());
        
        ViewDetailsCommand = new RelayCommand(async _ => 
        {
            if (SelectedFacture == null)
                return;
            
            using (var scope = _serviceProvider.CreateScope())
            {
                var vm = ActivatorUtilities.CreateInstance<FactureDetailsViewModel>(scope.ServiceProvider, SelectedFacture.Invoice.Id);
                var win = ActivatorUtilities.CreateInstance<FactureDetailsWindow>(scope.ServiceProvider, vm);
                win.Owner = System.Windows.Application.Current.MainWindow;
                win.ShowDialog();
                
                // Refresh after dialog closes in case something changed
                await LoadFacturesAsync();
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
    }

    public async void OnNavigatedTo(object parameter)
    {
        await LoadFacturesAsync();
    }

    private async Task LoadFacturesAsync()
    {
        IsBusy = true;
        try
        {
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            var supplierList = new ObservableCollection<Supplier>();
            // Add "Tous" option first
            var tousSupplier = new Supplier { Id = 0, CompanyName = "Tous" };
            supplierList.Add(tousSupplier);
            foreach (var supplier in suppliers.OrderBy(s => s.CompanyName))
            {
                supplierList.Add(supplier);
            }
            Suppliers = supplierList;
            SelectedSupplier = tousSupplier;

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

        if (SelectedSupplier != null && SelectedSupplier.Id != 0)
            filtered = filtered.Where(f => f.Invoice.SupplierId == SelectedSupplier.Id);

        if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Tous")
            filtered = filtered.Where(f => f.StatusCalculated == SelectedStatus);

        if (SelectedDate.HasValue)
            filtered = filtered.Where(f => f.Invoice.InvoiceDate.Date == SelectedDate.Value.Date);

        if (!string.IsNullOrEmpty(SearchText))
        {
            string search = SearchText.ToLower();
            filtered = filtered.Where(f => 
                (f.Invoice.InvoiceNumber != null && f.Invoice.InvoiceNumber.ToLower().Contains(search)) ||
                (f.Invoice.ExternalInvoiceNumber != null && f.Invoice.ExternalInvoiceNumber.ToLower().Contains(search)) ||
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
        TotalFacturesCount = _allFactures.Count;
        TotalAmount = _allFactures.Sum(f => f.Invoice.AmountTTC);
        PaidInvoicesCount = _allFactures.Count(f => f.StatusCalculated == "Payée");
        PartialInvoicesCount = _allFactures.Count(f => f.StatusCalculated == "Partiellement payée");
        WaitingInvoicesCount = _allFactures.Count(f => f.StatusCalculated == "En attente");
        PendingAmount = _allFactures.Sum(f => f.Balance);
    }

    private void ResetFilters()
    {
        SelectedSupplier = Suppliers.FirstOrDefault();
        SelectedStatus = "Tous";
        SelectedDate = null;
        SearchText = string.Empty;
    }
}
