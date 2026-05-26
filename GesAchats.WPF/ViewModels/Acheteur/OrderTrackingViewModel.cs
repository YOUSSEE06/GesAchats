using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class OrderTrackingViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPdfGeneratorService _pdfService;
    private PurchaseOrder? _selectedOrder;
    private string _statusFilter = "Tous";

    public ObservableCollection<PurchaseOrder> Orders { get; } = new ObservableCollection<PurchaseOrder>();
    public List<string> StatusFilters { get; } = new List<string> { "Tous", "Draft", "Issued", "PartiallyDelivered", "Delivered", "Closed" };

    public string StatusFilter
    {
        get => _statusFilter;
        set
        {
            if (SetProperty(ref _statusFilter, value))
            {
                _ = LoadOrders();
            }
        }
    }

    public PurchaseOrder? SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand UpdateStatusCommand { get; }
    public ICommand PrintPdfCommand { get; }

    public OrderTrackingViewModel(IUnitOfWork unitOfWork, IPdfGeneratorService pdfService)
    {
        _unitOfWork = unitOfWork;
        _pdfService = pdfService;
        Title = "Suivi des Commandes";

        RefreshCommand = new RelayCommand(async _ => await LoadOrders());
        UpdateStatusCommand = new RelayCommand(async p => await ExecuteUpdateStatus(p?.ToString()));
        PrintPdfCommand = new RelayCommand(async _ => await ExecutePrintPdf());

        _ = LoadOrders();
    }

    private async Task ExecutePrintPdf()
    {
        if (SelectedOrder == null) return;

        IsBusy = true;
        try
        {
            // Recharger l'ordre avec ses détails
            var fullOrder = await _unitOfWork.PurchaseOrders.GetWithDetailsAsync(SelectedOrder.Id);
            if (fullOrder == null) return;

            var filePath = await _pdfService.GeneratePurchaseOrderPdfAsync(fullOrder);
            
            var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true };
            process.Start();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de l'impression : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadOrders()
    {
        IsBusy = true;
        try
        {
            IEnumerable<PurchaseOrder> orders;
            if (StatusFilter == "Tous")
            {
                orders = await _unitOfWork.PurchaseOrders.GetAllAsync();
            }
            else
            {
                orders = await _unitOfWork.PurchaseOrders.FindAsync(o => o.Status == StatusFilter);
            }

            Orders.Clear();
            foreach (var o in orders.OrderByDescending(x => x.OrderDate))
            {
                Orders.Add(o);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteUpdateStatus(string? newStatus)
    {
        if (SelectedOrder == null || string.IsNullOrEmpty(newStatus)) return;

        try
        {
            SelectedOrder.Status = newStatus;
            SelectedOrder.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.PurchaseOrders.Update(SelectedOrder);
            await _unitOfWork.CompleteAsync();
            
            await LoadOrders();
            System.Windows.MessageBox.Show($"Statut de la commande {SelectedOrder.OrderNumber} mis à jour : {newStatus}", "Succès", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
}
