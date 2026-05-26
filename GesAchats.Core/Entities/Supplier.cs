using System.ComponentModel.DataAnnotations;

namespace GesAchats.Core.Entities;

/// <summary>
/// Fournisseur de l'entreprise
/// </summary>
public class Supplier
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContactName { get; set; }

    [MaxLength(100)]
    [EmailAddress]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    public string? Address { get; set; }

    [MaxLength(20)]
    public string? PostalCode { get; set; }

    [MaxLength(100)]
    public string? City { get; set; }

    [MaxLength(100)]
    public string? Country { get; set; }

    [MaxLength(100)]
    public string? PaymentConditions { get; set; } // Net 30, Net 60, Immédiat, etc.

    public int? AverageDeliveryDelay { get; set; } // en jours

    public decimal? Rating { get; set; } // de 0 à 5

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
