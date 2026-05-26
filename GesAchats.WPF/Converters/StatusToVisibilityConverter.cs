using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using GesAchats.Core.Entities;

namespace GesAchats.WPF.Converters;

public class StatusToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not NeedStatus status) return Visibility.Collapsed;
        string action = parameter as string ?? "";

        return action switch
        {
            "CanEdit" => (status == NeedStatus.Draft || status == NeedStatus.Rejected) ? Visibility.Visible : Visibility.Collapsed,
            "CanDelete" => (status == NeedStatus.Draft) ? Visibility.Visible : Visibility.Collapsed,
            "CanTransmit" => (status == NeedStatus.ToValidate) ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Visible
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
