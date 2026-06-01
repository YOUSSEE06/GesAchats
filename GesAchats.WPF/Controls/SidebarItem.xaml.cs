using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FontAwesome.Sharp;

namespace GesAchats.WPF.Controls;

public partial class SidebarItem : UserControl
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(IconChar), typeof(SidebarItem),
            new PropertyMetadata(IconChar.None, OnIconChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(nameof(Text), typeof(string), typeof(SidebarItem),
            new PropertyMetadata(string.Empty, OnTextChanged));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(SidebarItem),
            new PropertyMetadata(false, OnIsActiveChanged));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(SidebarItem),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(SidebarItem),
            new PropertyMetadata(null));

    public IconChar Icon
    {
        get => (IconChar)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public ICommand Command
    {
        get => (ICommand)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public SidebarItem()
    {
        InitializeComponent();
        Loaded += (s, e) => UpdateVisualState();
        MainBorder.MouseLeftButtonUp += (s, e) => Command?.Execute(CommandParameter);
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarItem control)
        {
            control.IconControl.Icon = (IconChar)e.NewValue;
        }
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarItem control)
        {
            control.TextControl.Text = (string)e.NewValue;
        }
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarItem control)
        {
            control.UpdateVisualState();
        }
    }

    private void UpdateVisualState()
    {
        if (IsActive)
        {
            MainBorder.Background = (Brush)FindResource("SidebarActiveBrush");
            MainBorder.BorderThickness = new Thickness(0, 0, 4, 0);
            MainBorder.BorderBrush = (Brush)FindResource("PrimaryBrush");
            TextControl.Foreground = (Brush)FindResource("SidebarTextBrush");
            IconControl.Foreground = (Brush)FindResource("PrimaryBrush");
        }
        else
        {
            MainBorder.Background = Brushes.Transparent;
            MainBorder.BorderThickness = new Thickness(0);
            TextControl.Foreground = (Brush)FindResource("SidebarTextBrush");
            IconControl.Foreground = (Brush)FindResource("SidebarTextBrush");
        }
    }
}
