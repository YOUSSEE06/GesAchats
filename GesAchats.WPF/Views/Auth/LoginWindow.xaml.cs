using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using GesAchats.WPF.ViewModels.Auth;

namespace GesAchats.WPF.Views.Auth;

public partial class LoginWindow : Window
{
    private static readonly Brush DefaultBorder = new SolidColorBrush(Color.FromRgb(0xDD, 0xE3, 0xEE));
    private static readonly Brush HoverBorder = new SolidColorBrush(Color.FromRgb(0xC4, 0xCD, 0xDE));
    private static readonly Brush FocusBorder = new SolidColorBrush(Color.FromRgb(0x4F, 0x46, 0xE5));
    private static readonly DropShadowEffect FocusShadow = new DropShadowEffect
    {
        Color = Color.FromRgb(0x4F, 0x46, 0xE5),
        BlurRadius = 15,
        ShadowDepth = 0,
        Opacity = 0.2
    };

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        TxtPassword.PasswordChanged += (s, e) =>
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.Password = TxtPassword.Password;
            }
            UpdatePasswordPlaceholder();
        };

        TogglePasswordBtn.Click += (s, e) =>
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.TogglePasswordCommand.Execute(null);
                
                if (vm.PasswordVisible)
                {
                    TxtVisiblePassword.Text = TxtPassword.Password;
                }
                else
                {
                    TxtPassword.Password = TxtVisiblePassword.Text;
                }
            }
            UpdatePasswordPlaceholder();
        };

        // Email field events
        EmailBorder.MouseEnter += (s, e) => UpdateBorder(EmailBorder, EmailTextBox.IsFocused, true);
        EmailBorder.MouseLeave += (s, e) => UpdateBorder(EmailBorder, EmailTextBox.IsFocused, false);
        EmailTextBox.GotFocus += (s, e) => UpdateBorder(EmailBorder, true, EmailBorder.IsMouseOver);
        EmailTextBox.LostFocus += (s, e) => UpdateBorder(EmailBorder, false, EmailBorder.IsMouseOver);
        EmailTextBox.TextChanged += (s, e) => UpdateEmailPlaceholder();

        // Password field events
        PasswordBorder.MouseEnter += (s, e) => UpdatePasswordBorder(true);
        PasswordBorder.MouseLeave += (s, e) => UpdatePasswordBorder(false);
        TxtPassword.GotFocus += (s, e) => UpdatePasswordBorder(PasswordBorder.IsMouseOver);
        TxtPassword.LostFocus += (s, e) => UpdatePasswordBorder(PasswordBorder.IsMouseOver);
        TxtVisiblePassword.GotFocus += (s, e) => UpdatePasswordBorder(PasswordBorder.IsMouseOver);
        TxtVisiblePassword.LostFocus += (s, e) => UpdatePasswordBorder(PasswordBorder.IsMouseOver);
        TxtVisiblePassword.TextChanged += (s, e) => UpdatePasswordPlaceholder();

        // Initial placeholder update
        UpdateEmailPlaceholder();
        UpdatePasswordPlaceholder();
    }

    private void UpdateBorder(System.Windows.Controls.Border border, bool isFocused, bool isHovered)
    {
        if (isFocused)
        {
            border.BorderBrush = FocusBorder;
            border.BorderThickness = new Thickness(2);
            border.Effect = FocusShadow;
        }
        else if (isHovered)
        {
            border.BorderBrush = HoverBorder;
            border.BorderThickness = new Thickness(1);
            border.Effect = null;
        }
        else
        {
            border.BorderBrush = DefaultBorder;
            border.BorderThickness = new Thickness(1);
            border.Effect = null;
        }
    }

    private void UpdatePasswordBorder(bool isHovered)
    {
        bool isFocused = TxtPassword.IsFocused || TxtVisiblePassword.IsFocused;
        UpdateBorder(PasswordBorder, isFocused, isHovered);
    }

    private void UpdateEmailPlaceholder()
    {
        var emailPlaceholder = (EmailTextBox.Parent as Grid)?.Children[1] as TextBlock;
        if (emailPlaceholder != null)
        {
            emailPlaceholder.Visibility = string.IsNullOrEmpty(EmailTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdatePasswordPlaceholder()
    {
        bool isEmpty;
        if (DataContext is LoginViewModel vm)
        {
            isEmpty = string.IsNullOrEmpty(vm.Password);
        }
        else
        {
            isEmpty = TxtPassword.Password.Length == 0 && string.IsNullOrEmpty(TxtVisiblePassword.Text);
        }
        PasswordPlaceholder.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
    }
}
