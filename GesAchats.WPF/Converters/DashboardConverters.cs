using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GesAchats.WPF.Converters
{
    /// <summary>
    /// Convertit une valeur booléenne en Visibilité
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Collapsed;
            }

            return true;
        }
    }

    /// <summary>
    /// Convertit un nombre décimal en format monétaire MAD
    /// </summary>
    public class CurrencyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is decimal decimalValue)
            {
                return decimalValue.ToString("C", new CultureInfo("fr-MA"));
            }
            else if (value is double doubleValue)
            {
                return ((decimal)doubleValue).ToString("C", new CultureInfo("fr-MA"));
            }

            return "0,00 MAD";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (decimal.TryParse(stringValue.Replace(" MAD", "").Replace(",", "."), out var result))
                {
                    return result;
                }
            }

            return 0m;
        }
    }

    /// <summary>
    /// Convertit un nombre en format pourcentage
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                return doubleValue.ToString("F1") + "%";
            }
            else if (value is int intValue)
            {
                return intValue.ToString() + "%";
            }

            return "0%";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit une DateTime en format lisible
    /// </summary>
    public class DateFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("dd/MM/yyyy", new CultureInfo("fr-FR"));
            }

            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string stringValue)
            {
                if (DateTime.TryParse(stringValue, new CultureInfo("fr-FR"), DateTimeStyles.None, out var result))
                {
                    return result;
                }
            }

            return DateTime.Now;
        }
    }

    /// <summary>
    /// Convertit une valeur numérique en couleur selon un seuil
    /// </summary>
    public class ValueToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double doubleValue)
            {
                // Positive = Green, Negative = Red
                if (doubleValue > 0)
                {
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
                }
                else if (doubleValue < 0)
                {
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
                }
                else
                {
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(117, 117, 117)); // Gray
                }
            }

            return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(117, 117, 117));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Convertit un type d'alerte en icône appropriée
    /// </summary>
    public class AlertTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string alertType)
            {
                return alertType switch
                {
                    "warning" => "⚠️",
                    "danger" => "❌",
                    "success" => "✅",
                    "info" => "ℹ️",
                    _ => "📌"
                };
            }

            return "📌";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Formatage des nombres avec séparateurs
    /// </summary>
    public class NumberFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int intValue)
            {
                return intValue.ToString("N0", new CultureInfo("fr-MA"));
            }
            else if (value is double doubleValue)
            {
                return doubleValue.ToString("N2", new CultureInfo("fr-MA"));
            }
            else if (value is decimal decimalValue)
            {
                return decimalValue.ToString("N2", new CultureInfo("fr-MA"));
            }

            return value?.ToString() ?? "0";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
