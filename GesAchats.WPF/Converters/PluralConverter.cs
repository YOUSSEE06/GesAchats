using System.Globalization;
using System.Windows.Data;

namespace GesAchats.WPF.Converters;

public class PluralConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count && count > 1)
        {
            return parameter ?? "s";
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
