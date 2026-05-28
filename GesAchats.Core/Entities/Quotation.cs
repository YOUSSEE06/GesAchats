using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Constantes pour les statuts de devis
/// </summary>
public static class QuotationStatus
{
    public const string Pending = "En attente";
    public const string Validated = "Validé";
}

/// <summary>
/// Devis reçu d'un fournisseur
/// </summary>
public class Quotation
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string ReferenceNumber { get; set; } = string.Empty; // DEV-YYYY-####

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int SupplierId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier Supplier { get; set; } = null!;

    public int? NeedId { get; set; } // Référence au besoin d'origine

    [ForeignKey(nameof(NeedId))]
    public virtual Need? Need { get; set; }

    public decimal TotalAmountHT { get; set; }

    public decimal TotalAmountTTC { get; set; }

    public DateTime? ResponseDate { get; set; }

    public string? Observations { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = QuotationStatus.Pending;

    public virtual ICollection<QuotationDetail> Details { get; set; } = new List<QuotationDetail>();

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public virtual User CreatedBy { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
