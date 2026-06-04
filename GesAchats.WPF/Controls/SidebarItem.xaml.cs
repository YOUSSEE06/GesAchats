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

    public static readonly DependencyProperty IsCollapsedProperty =
        DependencyProperty.Register(nameof(IsCollapsed), typeof(bool), typeof(SidebarItem),
            new PropertyMetadata(false, OnIsCollapsedChanged));

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

    public bool IsCollapsed
    {
        get => (bool)GetValue(IsCollapsedProperty);
        set => SetValue(IsCollapsedProperty, value);
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
        MainBorder.MouseEnter += (s, e) => UpdateVisualState();
        MainBorder.MouseLeave += (s, e) => UpdateVisualState();
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

    private static void OnIsCollapsedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SidebarItem control)
        {
            control.TextControl.Visibility = (bool)e.NewValue ? Visibility.Collapsed : Visibility.Visible;
        }
    }

    private void UpdateVisualState()
    {
        if (IsActive)
        {
            MainBorder.Background = new SolidColorBrush(Color.FromArgb(0x40, 0xFF, 0xFF, 0xFF));
            MainBorder.BorderThickness = new Thickness(0, 0, 4, 0);
            MainBorder.BorderBrush = Brushes.White;
            TextControl.Foreground = Brushes.White;
            IconControl.Foreground = Brushes.White;
        }
        else if (IsMouseOver)
        {
            MainBorder.Background = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));
            MainBorder.BorderThickness = new Thickness(0);
            TextControl.Foreground = Brushes.White;
            IconControl.Foreground = Brushes.White;
        }
        else
        {
            MainBorder.Background = Brushes.Transparent;
            MainBorder.BorderThickness = new Thickness(0);
            TextControl.Foreground = new SolidColorBrush(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF));
            IconControl.Foreground = new SolidColorBrush(Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF));
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == IsMouseOverProperty)
        {
            UpdateVisualState();
        }
    }
}
