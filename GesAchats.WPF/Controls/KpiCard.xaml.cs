using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FontAwesome.Sharp;

namespace GesAchats.WPF.Controls;

public partial class KpiCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(KpiCard),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public static readonly DependencyProperty ValueProperty =
        DependencyProperty.Register(nameof(Value), typeof(object), typeof(KpiCard),
            new PropertyMetadata(null, OnValueChanged));

    public static readonly DependencyProperty UnitProperty =
        DependencyProperty.Register(nameof(Unit), typeof(string), typeof(KpiCard),
            new PropertyMetadata(string.Empty, OnUnitChanged));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(IconChar), typeof(KpiCard),
            new PropertyMetadata(IconChar.None, OnIconChanged));

    public static readonly DependencyProperty IconColorProperty =
        DependencyProperty.Register(nameof(IconColor), typeof(Color), typeof(KpiCard),
            new PropertyMetadata(Colors.Transparent, OnIconColorChanged));

    public static readonly DependencyProperty TrendTextProperty =
        DependencyProperty.Register(nameof(TrendText), typeof(string), typeof(KpiCard),
            new PropertyMetadata(string.Empty, OnTrendTextChanged));

    public static readonly DependencyProperty IsPositiveTrendProperty =
        DependencyProperty.Register(nameof(IsPositiveTrend), typeof(bool), typeof(KpiCard),
            new PropertyMetadata(true, OnTrendChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Unit
    {
        get => (string)GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public IconChar Icon
    {
        get => (IconChar)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Color IconColor
    {
        get => (Color)GetValue(IconColorProperty);
        set => SetValue(IconColorProperty, value);
    }

    public string TrendText
    {
        get => (string)GetValue(TrendTextProperty);
        set => SetValue(TrendTextProperty, value);
    }

    public bool IsPositiveTrend
    {
        get => (bool)GetValue(IsPositiveTrendProperty);
        set => SetValue(IsPositiveTrendProperty, value);
    }

    public KpiCard()
    {
        InitializeComponent();
        Loaded += (s, e) =>
        {
            if (IconColor != Colors.Transparent)
            {
                IconBadge.Background = new SolidColorBrush(Color.FromArgb(30, IconColor.R, IconColor.G, IconColor.B));
                IconControl.Foreground = new SolidColorBrush(IconColor);
            }
            UpdateTrend();
        };
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCard control)
        {
            control.TitleControl.Text = (string)e.NewValue;
        }
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCard control)
        {
            control.ValueControl.Text = e.NewValue?.ToString() ?? "0";
        }
    }

    private static void OnUnitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCard control)
        {
            control.UnitControl.Text = (string)e.NewValue;
        }
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCard control)
        {
            control.IconControl.Icon = (IconChar)e.NewValue;
        }
    }

    private static void OnIconColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCard control && e.NewValue is Color color)
        {
            control.IconBadge.Background = new SolidColorBrush(Color.FromArgb(30, color.R, color.G, color.B));
            control.IconControl.Foreground = new SolidColorBrush(color);
        }
    }

    private static void OnTrendTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCard control)
        {
            control.TrendTextControl.Text = (string)e.NewValue;
        }
    }

    private static void OnTrendChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is KpiCard control)
        {
            control.UpdateTrend();
        }
    }

    private void UpdateTrend()
    {
        if (IsPositiveTrend)
        {
            TrendIconControl.Text = "▲";
            TrendIconControl.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
            TrendTextControl.Foreground = new SolidColorBrush(Color.FromRgb(34, 197, 94));
        }
        else
        {
            TrendIconControl.Text = "▼";
            TrendIconControl.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
            TrendTextControl.Foreground = new SolidColorBrush(Color.FromRgb(239, 68, 68));
        }
    }
}
