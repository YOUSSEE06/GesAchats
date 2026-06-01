using System.ComponentModel.DataAnnotations;

namespace GesAchats.Core.Entities;

public class DashboardKpiSnapshot
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime SnapshotDate { get; set; } = DateTime.UtcNow.Date;

    public int BesEnCoursCount { get; set; }

    public int BesTransmisCount { get; set; }

    public int DevEnAttenteCount { get; set; }

    public int DevValideCount { get; set; }

    public int FournisseursActifsCount { get; set; }

    public int TotalBcCount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
