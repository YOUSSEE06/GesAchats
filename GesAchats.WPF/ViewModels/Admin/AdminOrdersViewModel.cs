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

    public AdminOrderItemViewModel(PurchaseOrder po)
    {
        PurchaseOrder = po;
    }
}

public class AdminOrdersViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;

    private string _searchOrderNumber = string.Empty;
    private string _searchSupplier = string.Empty;
    private string _searchQuotationRef = string.Empty;
    private DateTime? _searchDate;

    public string SearchOrderNumber
    {
        get => _searchOrderNumber;
        set { SetProperty(ref _searchOrderNumber, value); FilterData(); }
    }

    public string SearchSupplier
    {
        get => _searchSupplier;
        set { SetProperty(ref _searchSupplier, value); FilterData(); }
    }

    public string SearchQuotationRef
    {
        get => _searchQuotationRef;
        set { SetProperty(ref _searchQuotationRef, value); FilterData(); }
    }

    public DateTime? SearchDate
    {
        get => _searchDate;
        set { SetProperty(ref _searchDate, value); FilterData(); }
    }

    private ObservableCollection<AdminOrderItemViewModel> _allOrders = new();
    public ObservableCollection<AdminOrderItemViewModel> Orders { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand AddOrderCommand { get; }
    public ICommand InspectCommand { get; }
    public ICommand PrintPdfCommand { get; }

    public AdminOrdersViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "Gestion des Bons de Commande";

        RefreshCommand = new RelayCommand(async _ => await LoadData());
        AddOrderCommand = new RelayCommand(_ => ExecuteAddOrder());
        InspectCommand = new RelayCommand(p => ExecuteInspect(p as AdminOrderItemViewModel));
        PrintPdfCommand = new RelayCommand(p => ExecutePrintPdf(p as AdminOrderItemViewModel));

        _ = LoadData();
    }

    private async Task LoadData()
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
            FilterData();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterData()
    {
        var filtered = _allOrders.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchOrderNumber))
            filtered = filtered.Where(x => x.OrderNumber.Contains(SearchOrderNumber, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(SearchSupplier))
            filtered = filtered.Where(x => x.SupplierName.Contains(SearchSupplier, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(SearchQuotationRef))
            filtered = filtered.Where(x => x.QuotationRef.Contains(SearchQuotationRef, StringComparison.OrdinalIgnoreCase));

        if (SearchDate.HasValue)
            filtered = filtered.Where(x => x.PurchaseOrder.OrderDate.Date == SearchDate.Value.Date);

        Orders.Clear();
        foreach (var item in filtered)
        {
            Orders.Add(item);
        }
    }

    private void ExecuteAddOrder()
    {
        MessageBox.Show("Fonctionnalité d'ajout de Bon de Commande (Admin) à implémenter.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
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
