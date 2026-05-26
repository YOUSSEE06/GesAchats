using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Facture fournisseur
/// </summary>
public class Invoice
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty; // N° interne GesAchats

    [MaxLength(100)]
    public string? ExternalInvoiceNumber { get; set; } // N° facture fournisseur

    public DateTime InvoiceDate { get; set; } // Date d'émission fournisseur

    public DateTime ReceptionDate { get; set; } = DateTime.UtcNow; // Date de réception

    public int SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier Supplier { get; set; } = null!;

    public int? PurchaseOrderId { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public virtual PurchaseOrder? PurchaseOrder { get; set; }

    public int? DeliveryNoteId { get; set; }

    [ForeignKey(nameof(DeliveryNoteId))]
    public virtual DeliveryNote? DeliveryNote { get; set; }

    public decimal AmountHT { get; set; }

    public decimal TaxRate { get; set; } = 20.00m;

    public decimal TaxAmount { get; set; }

    public decimal AmountTTC { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "EnAttente"; // EnAttente, Verifiee, PartiellementPayee, Payee, Rejetee

    [MaxLength(50)]
    public string ConformityStatus { get; set; } = "NonVerifiee"; // NonVerifiee, Conforme, EcartMineur, NonConforme

    public string? ConformityJustification { get; set; }

    public string? Observations { get; set; }

    public string? FilePath { get; set; } // Chemin du PDF/IMG de la facture

    public DateTime? DueDate { get; set; }

    public virtual ICollection<InvoiceDetail> Details { get; set; } = new List<InvoiceDetail>();

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
