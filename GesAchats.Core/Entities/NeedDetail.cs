using System.ComponentModel.DataAnnotations;

namespace GesAchats.Core.Entities;

public class NeedDetail
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int NeedId { get; set; }
    public virtual Need? Need { get; set; }

    [Required]
    public int ProductId { get; set; }
    public virtual Product? Product { get; set; }

    [Required]
    public decimal Quantity { get; set; }

    public decimal? UnitPriceHT { get; set; }

    public bool IsNewProduct { get; set; } = false;
}
