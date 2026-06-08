using System.Windows;

namespace GesAchats.WPF.Views.Components;

public enum AlertType
{
    Success,
    Warning,
    Error,
    Confirmation
}

public enum AlertButtonType
{
    Ok,
    YesNo
}

public partial class AlertModalWindow : Window
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(AlertModalWindow), new PropertyMetadata(""));

    public static readonly DependencyProperty AlertTypeProperty =
        DependencyProperty.Register(nameof(AlertType), typeof(AlertType), typeof(AlertModalWindow), new PropertyMetadata(AlertType.Success));

    public static readonly DependencyProperty ButtonTypeProperty =
        DependencyProperty.Register(nameof(ButtonType), typeof(AlertButtonType), typeof(AlertModalWindow), new PropertyMetadata(AlertButtonType.Ok));

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

    public AlertButtonType ButtonType
    {
        get => (AlertButtonType)GetValue(ButtonTypeProperty);
        set => SetValue(ButtonTypeProperty, value);
    }

    public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

    public AlertModalWindow()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += AlertModalWindow_Loaded;
    }

    private void AlertModalWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // Hide all icons first
        SuccessIconBorder.Visibility = Visibility.Collapsed;
        WarningIconBorder.Visibility = Visibility.Collapsed;
        ErrorIconBorder.Visibility = Visibility.Collapsed;
        ConfirmIconBorder.Visibility = Visibility.Collapsed;

        // Show the correct icon based on AlertType
        switch (AlertType)
        {
            case AlertType.Success:
                SuccessIconBorder.Visibility = Visibility.Visible;
                OkButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0x63, 0x66, 0xF1),
                    System.Windows.Media.Color.FromRgb(0x4F, 0x46, 0xE5),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 0));
                YesButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0x63, 0x66, 0xF1),
                    System.Windows.Media.Color.FromRgb(0x4F, 0x46, 0xE5),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 0));
                break;
            case AlertType.Warning:
                WarningIconBorder.Visibility = Visibility.Visible;
                OkButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0x98, 0x00),
                    System.Windows.Media.Color.FromRgb(0xF5, 0x7C, 0x00),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 0));
                YesButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0xFF, 0x98, 0x00),
                    System.Windows.Media.Color.FromRgb(0xF5, 0x7C, 0x00),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 0));
                break;
            case AlertType.Error:
                ErrorIconBorder.Visibility = Visibility.Visible;
                OkButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0xF4, 0x43, 0x36),
                    System.Windows.Media.Color.FromRgb(0xD3, 0x2F, 0x2F),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 0));
                YesButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0xF4, 0x43, 0x36),
                    System.Windows.Media.Color.FromRgb(0xD3, 0x2F, 0x2F),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 0));
                break;
            case AlertType.Confirmation:
                ConfirmIconBorder.Visibility = Visibility.Visible;
                OkButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0x21, 0x96, 0xF3),
                    System.Windows.Media.Color.FromRgb(0x19, 0x76, 0xD2),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 0));
                YesButton.Background = new System.Windows.Media.LinearGradientBrush(
                    System.Windows.Media.Color.FromRgb(0x21, 0x96, 0xF3),
                    System.Windows.Media.Color.FromRgb(0x19, 0x76, 0xD2),
                    new System.Windows.Point(0, 0),
                    new System.Windows.Point(1, 0));
                break;
        }

        // Show correct buttons based on ButtonType
        OkButton.Visibility = ButtonType == AlertButtonType.Ok ? Visibility.Visible : Visibility.Collapsed;
        YesButton.Visibility = ButtonType == AlertButtonType.YesNo ? Visibility.Visible : Visibility.Collapsed;
        NoButton.Visibility = ButtonType == AlertButtonType.YesNo ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.OK;
        Close();
    }

    private void YesButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.Yes;
        Close();
    }

    private void NoButton_Click(object sender, RoutedEventArgs e)
    {
        Result = MessageBoxResult.No;
        Close();
    }
}
