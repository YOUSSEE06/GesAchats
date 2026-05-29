using System;
using System.Globalization;
using System.Windows.Data;

namespace GesAchats.WPF.Converters;

public class BooleanToStringConverter : IValueConverter
{
    public string TrueValue { get; set; } = "True";
    public string FalseValue { get; set; } = "False";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value is bool boolValue && boolValue;
        
        if (parameter is string paramStr && paramStr.Contains("|"))
        {
            var parts = paramStr.Split('|');
            return b ? parts[0] : parts[1];
        }

        return b ? TrueValue : FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
