using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Détail d'un Bon de Commande (BC)
/// </summary>
public class PurchaseOrderDetail
{
    [Key]
    public int Id { get; set; }

    public int PurchaseOrderId { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPriceHT { get; set; }

    public decimal UnitPriceTTC { get; set; }
}
