using GesAchats.WPF.ViewModels.Auth;
using System.Windows;
using System.Windows.Controls;

namespace GesAchats.WPF.Views.Auth;

public partial class ResetPasswordWindow : Window
{
    public ResetPasswordWindow(ResetPasswordViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        TxtNewPassword.PasswordChanged += (s, e) =>
        {
            if (DataContext is ResetPasswordViewModel vm)
            {
                vm.NewPassword = TxtNewPassword.Password;
            }
            UpdateNewPasswordPlaceholder();
        };

        TxtConfirmPassword.PasswordChanged += (s, e) =>
        {
            if (DataContext is ResetPasswordViewModel vm)
            {
                vm.ConfirmPassword = TxtConfirmPassword.Password;
            }
            UpdateConfirmPasswordPlaceholder();
        };

        // Step 1 Email Placeholder
        Step1EmailTextBox.TextChanged += (s, e) =>
        {
            var emailPlaceholder = (Step1EmailTextBox.Parent as Grid)?.Children[1] as TextBlock;
            if (emailPlaceholder != null)
            {
                emailPlaceholder.Visibility = string.IsNullOrEmpty(Step1EmailTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        };

        // Step 2 Code Placeholder
        Step2CodeTextBox.TextChanged += (s, e) =>
        {
            var codePlaceholder = (Step2CodeTextBox.Parent as Grid)?.Children[1] as TextBlock;
            if (codePlaceholder != null)
            {
                codePlaceholder.Visibility = string.IsNullOrEmpty(Step2CodeTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
            }
        };

        // Initial Placeholder updates
        if ((Step1EmailTextBox.Parent as Grid)?.Children[1] is TextBlock emailPlaceholderInitial)
        {
            emailPlaceholderInitial.Visibility = string.IsNullOrEmpty(Step1EmailTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        if ((Step2CodeTextBox.Parent as Grid)?.Children[1] is TextBlock codePlaceholderInitial)
        {
            codePlaceholderInitial.Visibility = string.IsNullOrEmpty(Step2CodeTextBox.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        UpdateNewPasswordPlaceholder();
        UpdateConfirmPasswordPlaceholder();
    }

    private void UpdateNewPasswordPlaceholder()
    {
        if (NewPasswordPlaceholder != null)
        {
            NewPasswordPlaceholder.Visibility = string.IsNullOrEmpty(TxtNewPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void UpdateConfirmPasswordPlaceholder()
    {
        if (ConfirmPasswordPlaceholder != null)
        {
            ConfirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(TxtConfirmPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
