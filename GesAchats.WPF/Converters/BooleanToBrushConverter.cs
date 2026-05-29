using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

using System.Windows;

namespace GesAchats.WPF.Converters;

public class BooleanToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;

        if (parameter is string paramStr && paramStr.Contains("|"))
        {
            var parts = paramStr.Split('|');
            string brushKey = boolValue ? parts[0] : parts[1];
            
            if (Application.Current.Resources.Contains(brushKey))
                return Application.Current.Resources[brushKey];
            
            if (Application.Current.TryFindResource(brushKey) is Brush brush)
                return brush;
        }

        if (boolValue)
        {
            return new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Success Green
        }
        return new SolidColorBrush(Color.FromRgb(244, 67, 54)); // Danger Red
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
