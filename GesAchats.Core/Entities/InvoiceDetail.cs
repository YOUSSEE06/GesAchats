using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Détails d'une facture (Articles facturés)
/// </summary>
public class InvoiceDetail
{
    [Key]
    public int Id { get; set; }

    public int InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public virtual Invoice Invoice { get; set; } = null!;

    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }

    public decimal UnitPriceHT { get; set; }

    public decimal TotalHT { get; set; }

    public decimal TaxRate { get; set; } = 20.00m;

    public decimal TotalTTC { get; set; }
}
