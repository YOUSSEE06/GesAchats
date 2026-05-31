using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Détail d'un devis fournisseur
/// </summary>
public class QuotationDetail
{
    [Key]
    public int Id { get; set; }

    public int QuotationId { get; set; }

    [ForeignKey(nameof(QuotationId))]
    public virtual Quotation Quotation { get; set; } = null!;

    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPriceHT { get; set; }

    public decimal UnitPriceTTC { get; set; }

    public int? DeliveryDelayDays { get; set; } // Délai spécifique par article si nécessaire

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalLigneHT => Quantity * UnitPriceHT;
}
