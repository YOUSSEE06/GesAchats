using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Bon de Livraison (BL)
/// </summary>
public class DeliveryNote
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Le numéro de BL est obligatoire")]
    [MaxLength(50)]
    public string DeliveryNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "La date de réception est obligatoire")]
    public DateTime ReceptionDate { get; set; } = DateTime.UtcNow;

    [Required]
    public int PurchaseOrderId { get; set; }

    [ForeignKey(nameof(PurchaseOrderId))]
    public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier Supplier { get; set; } = null!;

    public decimal ReceivedQuantity { get; set; }

    public decimal CompliantQuantity { get; set; }

    public decimal DefectiveQuantity { get; set; }

    public string? Observations { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "EnAttente"; // EnAttente (waiting for invoice), Valide (invoice created)

    public string? FilePath { get; set; } // Chemin du PDF/IMG du BL

    public virtual ICollection<DeliveryNoteDetail> Details { get; set; } = new List<DeliveryNoteDetail>();

    public int ReceivedById { get; set; }

    [ForeignKey(nameof(ReceivedById))]
    public virtual User ReceivedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
