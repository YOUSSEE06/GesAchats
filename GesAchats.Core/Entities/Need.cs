using System;
using System.ComponentModel.DataAnnotations;

namespace GesAchats.Core.Entities;

public enum NeedStatus
{
    Draft,
    ToValidate,
    TransmittedToPurchasing, // Transmis au resp. achats
    Validated,
    InPurchase,
    Cancelled,
    Rejected,   // Rejeté
    Relaunched  // Relancé
}

public enum NeedReason
{
    RegularRestock, // Réappro régulière
    Urgency,        // Urgence
    Stockout,       // Rupture imminente
    CriticalStock,  // Stock critique
    SpecificProject // Projet spécifique
}

public enum NeedPriority
{
    Low,    // Basse
    Normal, // Normale
    High    // Haute
}

public class Need
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string NumeroBesoin { get; set; } = string.Empty; // #1, #2...

    [Required]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public int ProductId { get; set; }
    public virtual Product? Product { get; set; }
    
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }

    // Nouveaux champs demandés
    public NeedReason Reason { get; set; } = NeedReason.RegularRestock;
    public NeedPriority Priority { get; set; } = NeedPriority.Normal;
    public DateTime DesiredUrgencyDate { get; set; } = DateTime.UtcNow;
    public int RequiredDelayDays { get; set; } = 7;
    public string? Notes { get; set; }
    
    public NeedStatus Status { get; set; } = NeedStatus.Draft;
    
    public string? Justification { get; set; }
    
    public int RequestedById { get; set; }
    public virtual User? RequestedBy { get; set; }
    
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public int? ValidatedById { get; set; }
    public virtual User? ValidatedBy { get; set; }

    public DateTime? DateTransmission { get; set; }
    public DateTime? DateCompletion { get; set; }
    public string? MotifRejet { get; set; }
    public DateTime? DateRejet { get; set; }

    public virtual ICollection<NeedDetail> Details { get; set; } = new List<NeedDetail>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();

    // Pour le traçage (optionnel si on utilise la table Tracabilite)
    public string? History { get; set; }
}
