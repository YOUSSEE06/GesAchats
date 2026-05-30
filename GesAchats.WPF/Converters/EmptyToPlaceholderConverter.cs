using System;
using System.Globalization;
using System.Windows.Data;

namespace GesAchats.WPF.Converters;

public class EmptyToPlaceholderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? str = value?.ToString();
        return string.IsNullOrWhiteSpace(str) ? (parameter?.ToString() ?? "Tous") : str;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
