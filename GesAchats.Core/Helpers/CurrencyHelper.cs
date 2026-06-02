using System.Globalization;

namespace GesAchats.Core.Helpers;

public static class CurrencyHelper
{
    private static readonly CultureInfo MoroccanCulture = new CultureInfo("fr-MA");

    public static string FormatAmount(decimal amount)
    {
        // On utilise l'espace comme séparateur de milliers et le point comme séparateur décimal 
        // ou simplement le format standard suivi de " MAD"
        return $"{amount:N2} MAD";
    }

    public static string FormatAmount(double amount)
    {
        return $"{amount:N2} MAD";
    }
}
