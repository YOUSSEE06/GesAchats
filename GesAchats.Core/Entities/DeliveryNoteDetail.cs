using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Détail d'un Bon de Livraison (BL) - BonLivraisonLigne
/// </summary>
public class DeliveryNoteDetail
{
    [Key]
    public int Id { get; set; }

    public int DeliveryNoteId { get; set; }

    [ForeignKey(nameof(DeliveryNoteId))]
    public virtual DeliveryNote DeliveryNote { get; set; } = null!;

    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    public decimal QuantityOrdered { get; set; }

    public decimal QuantityReceived { get; set; }

    public decimal UnitPriceHT { get; set; }

    public decimal UnitPriceTTC { get; set; }

    public decimal Total { get; set; }

    public bool IsValidated { get; set; }
}
