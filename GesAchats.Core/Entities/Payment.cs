using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Règlement / Paiement d'une facture
/// </summary>
public class Payment
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string PaymentNumber { get; set; } = string.Empty; // REG-YYYY-####

    public int InvoiceId { get; set; }

    [ForeignKey(nameof(InvoiceId))]
    public virtual Invoice Invoice { get; set; } = null!;

    public int SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier Supplier { get; set; } = null!;

    [MaxLength(50)]
    public string PaymentMethod { get; set; } = string.Empty; // Virement, Cheque, Especes

    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    public decimal AmountPaid { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Validé"; // EnAttente, Validé, Rejeté

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; } // N° chèque, réf. virement

    [MaxLength(100)]
    public string? BankName { get; set; }

    public string? ProofFilePath { get; set; } // Chemin vers la preuve de paiement

    [MaxLength(20)]
    public string? FileType { get; set; } // pdf, jpg, png, tiff

    public string? ReceiptFilePath { get; set; } // Chemin vers le reçu PDF généré

    public string? Observations { get; set; }

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
