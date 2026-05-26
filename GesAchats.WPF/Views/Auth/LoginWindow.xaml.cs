using System.Windows;
using GesAchats.WPF.ViewModels.Auth;

namespace GesAchats.WPF.Views.Auth;

public partial class LoginWindow : Window
{
    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
