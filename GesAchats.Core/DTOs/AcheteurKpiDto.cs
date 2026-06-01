namespace GesAchats.Core.DTOs;

public class AcheteurKpiDto
{
    public int BesEnCoursCount { get; set; }
    public int BesTransmisCount { get; set; }
    public int DevEnAttenteCount { get; set; }
    public int DevValideCount { get; set; }
    public int FournisseursActifsCount { get; set; }
    public int TotalBcCount { get; set; }

    public double BesEnCoursEvolution { get; set; }
    public double BesTransmisEvolution { get; set; }
    public double DevEnAttenteEvolution { get; set; }
    public double DevValideEvolution { get; set; }
    public double FournisseursActifsEvolution { get; set; }
    public double TotalBcEvolution { get; set; }
}
