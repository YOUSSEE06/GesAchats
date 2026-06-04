using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GesAchats.WPF.Converters;

public class BoolToGridLengthConverter : IValueConverter
{
    public GridLength TrueValue { get; set; } = new GridLength(80);
    public GridLength FalseValue { get; set; } = new GridLength(320);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isCollapsed)
        {
            return isCollapsed ? TrueValue : FalseValue;
        }
        return FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
