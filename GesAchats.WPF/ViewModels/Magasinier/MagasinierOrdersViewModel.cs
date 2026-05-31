using System;
using System.Linq;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Admin;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class MagasinierOrdersViewModel : AdminOrdersViewModel
{
    public MagasinierOrdersViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
        : base(unitOfWork, serviceProvider)
    {
        Title = "Gestion des Bons de Commande";
        
        // Retirer le statut "Annulé" pour le magasinier
        if (Statuses.Contains(PurchaseOrderStatus.Cancelled))
        {
            Statuses.Remove(PurchaseOrderStatus.Cancelled);
        }
    }

    protected override void FilterData()
    {
        // On récupère la liste de base
        var filtered = _allOrders.AsEnumerable();

        // RÈGLE : Le magasinier ne voit JAMAIS les bons de commande annulés
        filtered = filtered.Where(x => x.Status != PurchaseOrderStatus.Cancelled);

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
}
