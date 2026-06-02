using GesAchats.WPF.ViewModels.Auth;
using System.Windows;

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
        };

        TxtConfirmPassword.PasswordChanged += (s, e) =>
        {
            if (DataContext is ResetPasswordViewModel vm)
            {
                vm.ConfirmPassword = TxtConfirmPassword.Password;
            }
        };
    }
}
