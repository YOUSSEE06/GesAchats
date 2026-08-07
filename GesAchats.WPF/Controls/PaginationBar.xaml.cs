using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GesAchats.WPF.Controls;

public partial class PaginationBar : UserControl
{
    public static readonly DependencyProperty PaginationTextProperty =
        DependencyProperty.Register(nameof(PaginationText), typeof(string), typeof(PaginationBar), new PropertyMetadata(""));

    public static readonly DependencyProperty CurrentPageProperty =
        DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(PaginationBar), new PropertyMetadata(1));

    public static readonly DependencyProperty TotalPagesProperty =
        DependencyProperty.Register(nameof(TotalPages), typeof(int), typeof(PaginationBar), new PropertyMetadata(0));

    public static readonly DependencyProperty FirstPageCommandProperty =
        DependencyProperty.Register(nameof(FirstPageCommand), typeof(ICommand), typeof(PaginationBar), new PropertyMetadata(null));

    public static readonly DependencyProperty PreviousPageCommandProperty =
        DependencyProperty.Register(nameof(PreviousPageCommand), typeof(ICommand), typeof(PaginationBar), new PropertyMetadata(null));

    public static readonly DependencyProperty NextPageCommandProperty =
        DependencyProperty.Register(nameof(NextPageCommand), typeof(ICommand), typeof(PaginationBar), new PropertyMetadata(null));

    public static readonly DependencyProperty LastPageCommandProperty =
        DependencyProperty.Register(nameof(LastPageCommand), typeof(ICommand), typeof(PaginationBar), new PropertyMetadata(null));

    public PaginationBar()
    {
        InitializeComponent();
    }

    public string PaginationText
    {
        get => (string)GetValue(PaginationTextProperty);
        set => SetValue(PaginationTextProperty, value);
    }

    public int CurrentPage
    {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    public int TotalPages
    {
        get => (int)GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, value);
    }

    public ICommand? FirstPageCommand
    {
        get => (ICommand?)GetValue(FirstPageCommandProperty);
        set => SetValue(FirstPageCommandProperty, value);
    }

    public ICommand? PreviousPageCommand
    {
        get => (ICommand?)GetValue(PreviousPageCommandProperty);
        set => SetValue(PreviousPageCommandProperty, value);
    }

    public ICommand? NextPageCommand
    {
        get => (ICommand?)GetValue(NextPageCommandProperty);
        set => SetValue(NextPageCommandProperty, value);
    }

    public ICommand? LastPageCommand
    {
        get => (ICommand?)GetValue(LastPageCommandProperty);
        set => SetValue(LastPageCommandProperty, value);
    }
}