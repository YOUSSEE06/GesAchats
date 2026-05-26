using System.ComponentModel.DataAnnotations;

namespace GesAchats.Core.Entities;

/// <summary>
/// Représente un rôle utilisateur (Admin, Acheteur, Magasinier, Comptable)
/// </summary>
public class Role
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty; // 'ADMIN', 'ACHETEUR', etc.

    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>
    /// Liste des permissions au format JSON
    /// </summary>
    public string? PermissionsJson { get; set; }

    public virtual ICollection<User> Users { get; set; } = new HashSet<User>();
}
