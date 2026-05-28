using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace GesAchats.Core.Entities;

public class Magasin
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Nom { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<Product> Produits { get; set; } = new List<Product>();
}
