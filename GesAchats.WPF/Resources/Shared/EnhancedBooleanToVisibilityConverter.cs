using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GesAchats.WPF.Resources.Shared;

public class EnhancedBooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        
        if (parameter?.ToString() == "Inverse")
        {
            boolValue = !boolValue;
        }
        else if (value is int count)
        {
            // Support pour les listes (count > 0)
            boolValue = count > 0;
            if (parameter?.ToString() == "Inverse") boolValue = !boolValue;
        }

        return boolValue ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
