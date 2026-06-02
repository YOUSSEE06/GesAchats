using System;
using System.Globalization;
using System.Windows.Data;

namespace GesAchats.WPF.Converters;

public class MultiplicationConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 2) return 0m;

        decimal result = 1m;
        foreach (var value in values)
        {
            if (value is decimal d)
                result *= d;
            else if (value is int i)
                result *= i;
            else if (value is double db)
                result *= (decimal)db;
            else if (value != null && decimal.TryParse(value.ToString(), out decimal parsed))
                result *= parsed;
            else
                return 0m;
        }

        if (targetType == typeof(double) || targetType == typeof(double?))
            return (double)result;
        return result;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ProgressBarWidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values == null || values.Length < 3)
            return 0.0;

        double actualWidth = 0;
        double value = 0;
        double max = 100;

        if (values[0] is double w) actualWidth = w;
        if (values[1] is double v) value = v;
        if (values[2] is double m) max = m;

        if (max <= 0) return 0.0;
        return actualWidth * (value / max);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
