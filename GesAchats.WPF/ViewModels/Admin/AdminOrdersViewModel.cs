using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminOrderItemViewModel : BaseViewModel
{
    public PurchaseOrder PurchaseOrder { get; }
    
    public string Date => PurchaseOrder.OrderDate.ToString("dd/MM/yyyy");
    public string OrderNumber => PurchaseOrder.OrderNumber;
    public string SupplierName => PurchaseOrder.Supplier?.CompanyName ?? "Inconnu";
    public string QuotationRef => PurchaseOrder.Quotation?.ReferenceNumber ?? "N/A";
    public string TotalTTC => $"{PurchaseOrder.TotalAmountTTC:N2} MAD";
    public string Status => PurchaseOrder.Status;

    public AdminOrderItemViewModel(PurchaseOrder po)
    {
        PurchaseOrder = po;
    }
}

public class AdminOrdersViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;

    private string _searchText = string.Empty;
    private string _selectedSupplier = string.Empty;
    private string _selectedStatus = string.Empty;
    private DateTime? _searchDate;

    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); FilterData(); }
    }

    public string SelectedSupplier
    {
        get => _selectedSupplier;
        set { SetProperty(ref _selectedSupplier, value); FilterData(); }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set { SetProperty(ref _selectedStatus, value); FilterData(); }
    }

    public DateTime? SearchDate
    {
        get => _searchDate;
        set { SetProperty(ref _searchDate, value); FilterData(); }
    }

    public ObservableCollection<string> Statuses { get; } = new()
    {
        "", // Tous les statuts
        PurchaseOrderStatus.Pending,
        PurchaseOrderStatus.Validated,
        PurchaseOrderStatus.Cancelled
    };

    public ObservableCollection<string> Suppliers { get; } = new();


    protected ObservableCollection<AdminOrderItemViewModel> _allOrders = new();
    public ObservableCollection<AdminOrderItemViewModel> Orders { get; } = new();

    public ICommand InspectCommand { get; }
    public ICommand PrintPdfCommand { get; }
    public ICommand ResetFiltersCommand { get; }

    public AdminOrdersViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "Gestion des Bons de Commande";

        InspectCommand = new RelayCommand(p => ExecuteInspect(p as AdminOrderItemViewModel));
        PrintPdfCommand = new RelayCommand(p => ExecutePrintPdf(p as AdminOrderItemViewModel));
        ResetFiltersCommand = new RelayCommand(_ => ResetFilters());

        _ = LoadData();
    }

    protected virtual async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var pos = await _unitOfWork.PurchaseOrders.GetAllWithSuppliersAsync();
            _allOrders.Clear();
            foreach (var po in pos.OrderByDescending(x => x.OrderDate))
            {
                _allOrders.Add(new AdminOrderItemViewModel(po));
            }

            // Populate unique suppliers
            Suppliers.Clear();
            Suppliers.Add(""); // Tous les fournisseurs
            var uniqueSuppliers = _allOrders
                .Select(x => x.SupplierName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .OrderBy(x => x);
            foreach (var supplier in uniqueSuppliers)
            {
                Suppliers.Add(supplier);
            }

            FilterData();
        }
        finally
        {
            IsBusy = false;
        }
    }

    protected virtual void FilterData()
    {
        var filtered = _allOrders.AsEnumerable();

        // Filtre par texte (N° BC / Réf. Devis)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(x => 
                x.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || 
                x.QuotationRef.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        // Filtre par fournisseur
        if (!string.IsNullOrWhiteSpace(SelectedSupplier))
            filtered = filtered.Where(x => x.SupplierName == SelectedSupplier);

        // Filtre par statut
        if (!string.IsNullOrWhiteSpace(SelectedStatus))
            filtered = filtered.Where(x => x.Status == SelectedStatus);

        // Filtre par date
        if (SearchDate.HasValue)
            filtered = filtered.Where(x => x.PurchaseOrder.OrderDate.Date == SearchDate.Value.Date);

        Orders.Clear();
        foreach (var item in filtered)
        {
            Orders.Add(item);
        }
    }

    private void ResetFilters()
    {
        SearchText = string.Empty;
        SelectedSupplier = string.Empty;
        SelectedStatus = string.Empty;
        SearchDate = null;
    }

    private async void ExecuteInspect(AdminOrderItemViewModel? item)
    {
        if (item == null) return;
        
        try
        {
            var poWithDetails = await _unitOfWork.PurchaseOrders.GetWithDetailsAsync(item.PurchaseOrder.Id);
            if (poWithDetails == null) return;

            var window = new Views.Admin.Orders.OrderDetailsWindow();
            window.DataContext = new OrderDetailsViewModel(poWithDetails);
            window.Owner = Application.Current.MainWindow;
            window.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors du chargement des détails : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ExecutePrintPdf(AdminOrderItemViewModel? item)
    {
        if (item == null) return;

        try
        {
            IsBusy = true;
            var poWithDetails = await _unitOfWork.PurchaseOrders.GetWithDetailsAsync(item.PurchaseOrder.Id);
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
