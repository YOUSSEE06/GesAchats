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

    public decimal TotalTTC => _allInvoices.Sum(i => i.Invoice.AmountTTC);
    public decimal TotalPayments => _allInvoices.Sum(i => i.TotalPayments);
    public decimal TotalBalance => TotalTTC - TotalPayments;

    // Nouveaux KPIs
    public int PaidInvoicesCount => _allInvoices.Count(i => i.StatusCalculated == "Payée");
    public int PartialInvoicesCount => _allInvoices.Count(i => i.StatusCalculated == "Partiellement payée");
    public int BilledSuppliersCount => _allInvoices.Select(i => i.Invoice.SupplierId).Distinct().Count();

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

        _ = LoadDataAsync();
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

            ApplyFilters();

            // Update total stats
            OnPropertyChanged(nameof(TotalTTC));
            OnPropertyChanged(nameof(TotalPayments));
            OnPropertyChanged(nameof(TotalBalance));
            OnPropertyChanged(nameof(PaidInvoicesCount));
            OnPropertyChanged(nameof(PartialInvoicesCount));
            OnPropertyChanged(nameof(BilledSuppliersCount));
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
}
