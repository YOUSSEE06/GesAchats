using System.Globalization;
using System.Windows.Data;

namespace GesAchats.WPF.Converters;

public class NullToBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNotNull = value != null;
        if (parameter?.ToString() == "Inverse") isNotNull = !isNotNull;
        return isNotNull;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
