using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GesAchats.Core.Entities;

/// <summary>
/// Log d'audit pour la traçabilité des actions
/// </summary>
public class AuditLog
{
    [Key]
    public int Id { get; set; }

    public int? UserId { get; set; }

    [ForeignKey(nameof(UserId))]
    public virtual User? User { get; set; }

    [MaxLength(50)]
    public string Action { get; set; } = string.Empty; // CREATE, UPDATE, DELETE

    [MaxLength(100)]
    public string EntityName { get; set; } = string.Empty; // Invoice, Quotation, etc.

    public int EntityId { get; set; }

    public string? OldValues { get; set; } // JSON serialized

    public string? NewValues { get; set; } // JSON serialized

    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string? IpAddress { get; set; }
}
