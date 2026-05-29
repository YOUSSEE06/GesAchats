using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using System.Diagnostics;
using System.Windows;

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

    public ICommand LoadPaymentsCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand AddPaymentCommand { get; }
    public ICommand ViewProofCommand { get; }
    public ICommand ResetFiltersCommand { get; }

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

        SelectedPaymentMethod = "Tous";

        _ = LoadPaymentsAsync();
    }

    private void ResetFilters()
    {
        SelectedSupplier = Suppliers.FirstOrDefault();
        SelectedDate = null;
        SelectedPaymentMethod = "Tous";
        SearchText = string.Empty;
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

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            string search = SearchText.ToLower();
            filtered = filtered.Where(p => 
                (p.Invoice != null && p.Invoice.ExternalInvoiceNumber != null && p.Invoice.ExternalInvoiceNumber.ToLower().Contains(search)) ||
                (p.ReferenceNumber != null && p.ReferenceNumber.ToLower().Contains(search))
            );
        }

        Payments = new ObservableCollection<Payment>(filtered);
    }

    private void ExportToExcel()
    {
        // TODO: Implémenter ExportService
    }
}
