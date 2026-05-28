using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Article ou produit géré en stock
/// </summary>
public class Product
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Designation { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unit { get; set; } = "pcs"; // kg, m2, pcs, etc.

    public decimal CurrentStock { get; set; }

    public decimal MinimumStock { get; set; }

    [MaxLength(100)]
    public string? Category { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastPurchaseDate { get; set; }

    public decimal DailyConsumption { get; set; } = 1;

    public bool IsNew { get; set; } = false; // 1 si produit ajouté par Magasinier (Stock=0)

    public string? CreatedBy { get; set; } // Nom du Magasinier si IsNew=1

    public int? MagasinId { get; set; }

    [ForeignKey(nameof(MagasinId))]
    public Magasin? Magasin { get; set; }

    public bool IsLowStock => CurrentStock <= MinimumStock;

    public decimal DaysUntilStockout => DailyConsumption > 0 ? Math.Floor(CurrentStock / DailyConsumption) : 999;

    public StockState Etat => CurrentStock == 0 ? StockState.OutOfStock :
                              CurrentStock <= MinimumStock ? StockState.Alert : StockState.Ok;
}

public enum StockState
{
    Ok,
    Alert,
    OutOfStock
}
