using System.Windows;
using System.Windows.Input;

namespace GesAchats.WPF.Views.Components;

public partial class SuccessModalWindow : Window
{
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register(nameof(Message), typeof(string), typeof(SuccessModalWindow), new PropertyMetadata(""));

    public static readonly DependencyProperty CountProperty =
        DependencyProperty.Register(nameof(Count), typeof(int), typeof(SuccessModalWindow), new PropertyMetadata(1));

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public int Count
    {
        get => (int)GetValue(CountProperty);
        set => SetValue(CountProperty, value);
    }

    public event EventHandler? ViewListRequested;
    public event EventHandler? CreateNewRequested;

    public SuccessModalWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void ViewListButton_Click(object sender, RoutedEventArgs e)
    {
        ViewListRequested?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void CreateNewButton_Click(object sender, RoutedEventArgs e)
    {
        CreateNewRequested?.Invoke(this, EventArgs.Empty);
        Close();
    }
}
