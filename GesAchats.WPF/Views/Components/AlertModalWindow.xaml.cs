using System.Windows;
using System.Windows.Input;

namespace GesAchats.WPF.Views.Components;

public enum AlertType
{
    Success,
    Warning,
    Error
}

public partial class AlertModalWindow : Window
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(AlertModalWindow), new PropertyMetadata(""));

    public static readonly DependencyProperty AlertTypeProperty =
        DependencyProperty.Register(nameof(AlertType), typeof(AlertType), typeof(AlertModalWindow), new PropertyMetadata(AlertType.Success));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public AlertType AlertType
    {
        get => (AlertType)GetValue(AlertTypeProperty);
        set => SetValue(AlertTypeProperty, value);
    }

    public AlertModalWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += AlertModalWindow_Loaded;
    }

    private void AlertModalWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Show the correct icon based on AlertType
        SuccessIconBorder.Visibility = AlertType == AlertType.Success ? Visibility.Visible : Visibility.Collapsed;
        WarningIconBorder.Visibility = AlertType == AlertType.Warning ? Visibility.Visible : Visibility.Collapsed;
        ErrorIconBorder.Visibility = AlertType == AlertType.Error ? Visibility.Visible : Visibility.Collapsed;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
