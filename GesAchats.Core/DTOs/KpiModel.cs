namespace GesAchats.Core.DTOs;

/// <summary>
/// Modèle KPI réutilisable : valeur courante, valeur précédente et pourcentage d'évolution.
/// Formule identique aux KPI du Dashboard principal : ((current - previous) / previous) * 100.
/// </summary>
public class KpiModel
{
    public double Current { get; set; }
    public double Previous { get; set; }
    public double Percentage => CalculatePercentage(Current, Previous);

    public static double CalculatePercentage(double current, double previous)
    {
        if (previous == 0)
        {
            return current > 0 ? 100 : 0;
        }
        return Math.Round(((current - previous) / previous) * 100, 1);
    }
}