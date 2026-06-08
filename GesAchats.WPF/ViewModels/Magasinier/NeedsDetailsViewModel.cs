using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class NeedsDetailsViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPdfGeneratorService _pdfService;
    private readonly int _needId;
    private Need? _need;

    public Need? Need { get => _need; set => SetProperty(ref _need, value); }
    public ObservableCollection<NeedDetail> Details { get; } = new();
    public ObservableCollection<PurchaseOrder> PurchaseOrders { get; } = new();
    public ObservableCollection<DeliveryNote> DeliveryNotes { get; } = new();

    public ICommand CloseCommand { get; }
    public ICommand ExportPdfCommand { get; }
    public ICommand CancelOrderCommand { get; }
    public ICommand CancelNeedCommand { get; }
    public event EventHandler? RequestClose;

    public NeedsDetailsViewModel(IUnitOfWork unitOfWork, IPdfGeneratorService pdfService, int needId)
    {
        _unitOfWork = unitOfWork;
        _pdfService = pdfService;
        _needId = needId;

        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty)); 
        ExportPdfCommand = new RelayCommand(async _ => await ExecuteExportPdf());
        CancelOrderCommand = new RelayCommand(async p => await ExecuteCancelOrder(p as PurchaseOrder));
        CancelNeedCommand = new RelayCommand(async _ => await ExecuteDeleteNeed());

        _ = LoadData();
    }

    private async Task ExecuteDeleteNeed()
    {
        if (Need == null) return;
        var result = MessageBox.Show("Voulez-vous vraiment SUPPRIMER définitivement ce besoin ? Cette action est irréversible et il ne paraîtra plus dans l'historique.", "Confirmation de Suppression", MessageBoxButton.YesNo, MessageBoxImage.Stop);
        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            // Suppression physique du besoin (les détails sont normalement supprimés en cascade par EF si configuré, 
            // sinon on les supprime manuellement ici par sécurité)
            var details = await _unitOfWork.NeedDetails.FindAsync(d => d.NeedId == Need.Id);
            foreach (var d in details)
            {
                _unitOfWork.NeedDetails.Remove(d);
            }

            _unitOfWork.Needs.Remove(Need);
            await _unitOfWork.CompleteAsync();
            
            // Show custom success modal
            var modal = new Views.Components.SuccessModalWindow
            {
                Message = "Besoin supprimé définitivement."
            };
            modal.ShowDialog();
            
            // Fermer la fenêtre pour revenir à l'historique
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de la suppression du besoin : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private async Task ExecuteCancelOrder(PurchaseOrder? order)
    {
        if (order == null) return;
        if (order.Status == "Cancelled") return;

        var result = MessageBox.Show($"Voulez-vous vraiment annuler la commande {order.OrderNumber} ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            order.Status = "Cancelled";
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.PurchaseOrders.Update(order);
            await _unitOfWork.CompleteAsync();
            // Show custom success modal
            var modal = new Views.Components.SuccessModalWindow
            {
                Message = $"Commande {order.OrderNumber} annulée."
            };
            modal.ShowDialog();
            
            // Recharger les données pour mettre à jour l'UI
            await LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'annulation de la commande : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private async Task ExecuteExportPdf()
    {
        if (Need == null) return;
        IsBusy = true;
        try
        {
            var path = await _pdfService.GenerateNeedPdfAsync(Need);
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'export PDF : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsBusy = false; }
    }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            // Récupération du besoin avec TOUS ses détails et produits inclus
            Need = await _unitOfWork.Needs.GetByIdWithDetailsAsync(_needId);
            if (Need == null) return;

            // Chargement des détails (articles) directement depuis l'entité Need
            Details.Clear();
            foreach (var d in Need.Details) Details.Add(d);

            // Chargement des BC liés (basé sur NumeroBesoin pour l'instant)
            var orders = await _unitOfWork.PurchaseOrders.FindAsync(o => o.OrderNumber.Contains(Need.NumeroBesoin));
            PurchaseOrders.Clear();
            foreach (var o in orders) PurchaseOrders.Add(o);

            // Chargement des BL liés via les BC
            var orderIds = orders.Select(o => o.Id).ToList();
            var deliveries = await _unitOfWork.DeliveryNotes.FindAsync(d => orderIds.Contains(d.PurchaseOrderId));
            DeliveryNotes.Clear();
            foreach (var d in deliveries) DeliveryNotes.Add(d);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
