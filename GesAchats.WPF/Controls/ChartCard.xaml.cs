using System.Windows;
using System.Windows.Controls;

namespace GesAchats.WPF.Controls;

public partial class ChartCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ChartCard),
            new PropertyMetadata(string.Empty, OnTitleChanged));

    public static readonly DependencyProperty ChartContentProperty =
        DependencyProperty.Register(nameof(ChartContent), typeof(object), typeof(ChartCard),
            new PropertyMetadata(null, OnChartContentChanged));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public object ChartContent
    {
        get => GetValue(ChartContentProperty);
        set => SetValue(ChartContentProperty, value);
    }

    public ChartCard()
    {
        InitializeComponent();
    }

    private static void OnTitleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChartCard control)
        {
            control.TitleControl.Text = (string)e.NewValue;
        }
    }

    private static void OnChartContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ChartCard control)
        {
            control.ChartContentControl.Content = e.NewValue;
        }
    }
}
