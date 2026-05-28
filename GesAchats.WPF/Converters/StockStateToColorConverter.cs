using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GesAchats.Core.Entities;

namespace GesAchats.WPF.Converters;

public class StockStateToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StockState state)
        {
            return state switch
            {
                StockState.Ok => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // Green
                StockState.Alert => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // Orange
                StockState.OutOfStock => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // Red
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StockStateToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StockState state)
        {
            return state switch
            {
                StockState.Ok => "OK",
                StockState.Alert => "ALERTE",
                StockState.OutOfStock => "RUPTURE",
                _ => "INCONNU"
            };
        }
        return "INCONNU";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
