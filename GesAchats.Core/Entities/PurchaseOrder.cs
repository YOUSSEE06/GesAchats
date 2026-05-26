using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Bon de Commande (BC)
/// </summary>
public class PurchaseOrder
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty; // BC-YYYY-####

    public DateTime OrderDate { get; set; } = DateTime.UtcNow;

    public int SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier Supplier { get; set; } = null!;

    public int? QuotationId { get; set; }

    [ForeignKey(nameof(QuotationId))]
    public virtual Quotation? Quotation { get; set; }

    public int? NeedId { get; set; } // Référence au besoin d'origine

    [ForeignKey(nameof(NeedId))]
    public virtual Need? Need { get; set; }

    public decimal TotalAmountHT { get; set; }

    public decimal TotalAmountTTC { get; set; }

    public decimal TotalVAT { get; set; }

    [MaxLength(100)]
    public string? PaymentConditions { get; set; }

    public int? RequestedDeliveryDelay { get; set; } // en jours

    [MaxLength(50)]
    public string Status { get; set; } = "Draft"; // Draft, Issued, PartiallyDelivered, Delivered, Closed

    public DateTime? ExpectedDeliveryDate { get; set; }

    public virtual ICollection<PurchaseOrderDetail> Details { get; set; } = new List<PurchaseOrderDetail>();

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; } = null!;

    public string? Observations { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
