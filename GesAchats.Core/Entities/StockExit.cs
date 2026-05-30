using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Enregistrement d'une sortie de stock (matières premières, consommables, etc.)
/// </summary>
public class StockExit
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual Product? Product { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    public DateTime ExitDate { get; set; } = DateTime.UtcNow;

    [MaxLength(200)]
    public string? ProjectOrChantier { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; } // Motif de sortie

    public decimal StockAfterExit { get; set; }

    public int CreatedById { get; set; }

    [ForeignKey(nameof(CreatedById))]
    public virtual User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
