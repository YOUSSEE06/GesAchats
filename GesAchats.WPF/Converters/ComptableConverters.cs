using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace GesAchats.WPF.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? "";
        return status switch
        {
            "Payée" or "Conforme" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")),
            "Vérifiée" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")),
            "En attente" or "EnAttente" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107")),
            "Partiellement payée" or "PartiellementPayee" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")),
            "Rejetée" or "Rejetee" or "Retard" or "NonConforme" => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")),
            _ => new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")),
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? "";
        string param = parameter?.ToString() ?? "";

        if (param.StartsWith("!"))
        {
            return status != param.Substring(1) ? Visibility.Visible : Visibility.Collapsed;
        }
        
        return status == param ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StatusVisibilityMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2) return Visibility.Collapsed;

        string status = values[0]?.ToString() ?? "";
        bool isAdmin = values[1] is bool b && b;

        // Si c'est un admin, on masque toujours le bouton payer
        if (isAdmin) return Visibility.Collapsed;

        // Sinon (pour le comptable), on affiche si la facture n'est pas payée
        return status != "Payée" ? Visibility.Visible : Visibility.Collapsed;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StaticValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return parameter ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class EqualityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (value is true ? parameter?.ToString() : Binding.DoNothing) ?? Binding.DoNothing;
    }
}

public class CurrencyFormatterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null) return "0.00 MAD";
        
        decimal amount = 0;
        if (value is decimal m) amount = m;
        else if (value is double d) amount = (decimal)d;
        else if (value is float f) amount = (decimal)f;
        else if (value is int i) amount = i;

        return $"{amount:N2} MAD";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ConformityToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string status = value?.ToString() ?? "";
        string target = parameter?.ToString() ?? "";
        return status == target ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        string? str = value?.ToString();
        return !string.IsNullOrWhiteSpace(str) ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
