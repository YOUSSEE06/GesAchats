using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace GesAchats.WPF.Converters
{
    public class StatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new SolidColorBrush(Color.FromRgb(100, 116, 139));

            string? statusStr = value.ToString()?.ToLowerInvariant().Trim();
            if (string.IsNullOrEmpty(statusStr))
                return new SolidColorBrush(Color.FromRgb(100, 116, 139));

            // Green - Effectuée, Payée
            if (statusStr == "effectuée" || statusStr == "effectué" || statusStr == "payée" || statusStr == "payé")
                return new SolidColorBrush(Color.FromRgb(134, 239, 172)); // #86EFAC

            // Orange - Partiellement payée
            if (statusStr == "partiellement payée" || statusStr == "partiellement payé")
                return new SolidColorBrush(Color.FromRgb(254, 215, 170)); // #FED7AA

            // Gray - Brouillon
            if (statusStr == "brouillon")
                return new SolidColorBrush(Color.FromRgb(209, 213, 219)); // #D1D5DB

            // Yellow/Amber - En attente
            if (statusStr == "en attente" || statusStr == "à valider" || statusStr == "relancé")
                return new SolidColorBrush(Color.FromRgb(253, 230, 138)); // #FDE68A

            // Red - Annulée, Rejetée
            if (statusStr == "annulé" || statusStr == "annulée" || statusStr == "rejeté" || statusStr == "rejetée")
                return new SolidColorBrush(Color.FromRgb(254, 202, 202)); // #FECACA

            // Blue - En cours
            if (statusStr == "en cours" || statusStr == "en cours d'achat" || statusStr == "transmis à l'achat")
                return new SolidColorBrush(Color.FromRgb(191, 219, 254)); // #BFDBFE

            // Teal/Emerald - Validée
            if (statusStr == "validé" || statusStr == "validée")
                return new SolidColorBrush(Color.FromRgb(167, 243, 208)); // #A7F3D0

            // Dark Gray - Expirée
            if (statusStr.Contains("expiré"))
                return new SolidColorBrush(Color.FromRgb(107, 114, 128)); // #6B7280

            // Purple - Suspendue
            if (statusStr.Contains("suspendu"))
                return new SolidColorBrush(Color.FromRgb(216, 180, 254)); // #D8B4FE

            return new SolidColorBrush(Color.FromRgb(100, 116, 139)); // #64748B (default)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return new SolidColorBrush(Color.FromRgb(15, 23, 42));

            string? statusStr = value.ToString()?.ToLowerInvariant().Trim();
            if (string.IsNullOrEmpty(statusStr))
                return new SolidColorBrush(Color.FromRgb(15, 23, 42));

            // Green - Effectuée, Payée
            if (statusStr == "effectuée" || statusStr == "effectué" || statusStr == "payée" || statusStr == "payé")
                return new SolidColorBrush(Color.FromRgb(6, 78, 59)); // #064F3B

            // Orange - Partiellement payée
            if (statusStr == "partiellement payée" || statusStr == "partiellement payé")
                return new SolidColorBrush(Color.FromRgb(124, 45, 18)); // #7C2D12

            // Gray - Brouillon
            if (statusStr == "brouillon")
                return new SolidColorBrush(Color.FromRgb(55, 65, 81)); // #374151

            // Yellow/Amber - En attente
            if (statusStr == "en attente" || statusStr == "à valider" || statusStr == "relancé")
                return new SolidColorBrush(Color.FromRgb(120, 53, 15)); // #78350F

            // Red - Annulée, Rejetée
            if (statusStr == "annulé" || statusStr == "annulée" || statusStr == "rejeté" || statusStr == "rejetée")
                return new SolidColorBrush(Color.FromRgb(127, 29, 29)); // #7F1D1D

            // Blue - En cours
            if (statusStr == "en cours" || statusStr == "en cours d'achat" || statusStr == "transmis à l'achat")
                return new SolidColorBrush(Color.FromRgb(30, 64, 175)); // #1E40AF

            // Teal/Emerald - Validée
            if (statusStr == "validé" || statusStr == "validée")
                return new SolidColorBrush(Color.FromRgb(6, 78, 59)); // #064F3B

            // Dark Gray - Expirée
            if (statusStr.Contains("expiré"))
                return new SolidColorBrush(Color.FromRgb(248, 250, 252)); // #F8FAFC

            // Purple - Suspendue
            if (statusStr.Contains("suspendu"))
                return new SolidColorBrush(Color.FromRgb(88, 28, 135)); // #581C87

            return new SolidColorBrush(Color.FromRgb(15, 23, 42)); // #0F172A (default)
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}