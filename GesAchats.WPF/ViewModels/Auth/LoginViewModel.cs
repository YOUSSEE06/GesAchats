using System.Windows;
using System.Windows.Input;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Views.Auth;
using Serilog;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.ViewModels.Auth;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private string _loginOrEmail = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _passwordVisible;
    private string _passwordToggleIcon = "👁";
    private string _password = string.Empty;

    public string LoginOrEmail
    {
        get => _loginOrEmail;
        set => SetProperty(ref _loginOrEmail, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public bool PasswordVisible
    {
        get => _passwordVisible;
        set => SetProperty(ref _passwordVisible, value);
    }

    public string PasswordToggleIcon
    {
        get => _passwordToggleIcon;
        set => SetProperty(ref _passwordToggleIcon, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public CommunityToolkit.Mvvm.Input.IRelayCommand<object?> LoginCommand { get; }
    public CommunityToolkit.Mvvm.Input.IRelayCommand ForgotPasswordCommand { get; }
    public CommunityToolkit.Mvvm.Input.IRelayCommand TogglePasswordCommand { get; }

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<object?>(ExecuteLoginAsync, CanExecuteLogin);
        ForgotPasswordCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ExecuteForgotPassword);
        TogglePasswordCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(ExecuteTogglePassword);
        Title = "Connexion - GesAchats";
    }

    private void ExecuteTogglePassword()
    {
        PasswordVisible = !PasswordVisible;
        PasswordToggleIcon = PasswordVisible ? "👁‍🗨" : "👁";
    }

    private bool CanExecuteLogin(object? parameter) => !IsBusy;

    private async Task ExecuteLoginAsync(object? parameter)
    {
        string password;
        if (PasswordVisible)
        {
            password = Password;
        }
        else
        {
            var passwordBox = parameter as System.Windows.Controls.PasswordBox;
            if (passwordBox == null) return;
            password = passwordBox.Password;
        }

        if (string.IsNullOrWhiteSpace(LoginOrEmail))
        {
            ErrorMessage = "Veuillez saisir votre email ou identifiant.";
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ErrorMessage = "Veuillez saisir votre mot de passe.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;
        LoginCommand.NotifyCanExecuteChanged();

        try
        {
            var user = await _authService.LoginAsync(LoginOrEmail, password);
            if (user != null)
            {
                Log.Information("Connexion réussie pour {LoginOrEmail}", LoginOrEmail);
                NavigationRouter.GoToRole(user);
            }
            else
            {
                ErrorMessage = "Identifiants incorrects.";
            }
        }
        catch (InvalidOperationException ex)
        {
            ErrorMessage = ex.Message;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Erreur inattendue lors de la connexion");
            ErrorMessage = $"Erreur: {ex.Message}\n{ex.InnerException?.Message}";
        }
        finally
        {
            IsBusy = false;
            LoginCommand.NotifyCanExecuteChanged();
        }
    }

    private void ExecuteForgotPassword()
    {
        try
        {
            var serviceProvider = ((App)Application.Current).ServiceProvider;
            var resetWindow = serviceProvider.GetRequiredService<ResetPasswordWindow>();
            resetWindow.Show();

            var currentLoginWindow = Application.Current.Windows.OfType<LoginWindow>().FirstOrDefault();
            currentLoginWindow?.Hide();

            resetWindow.Closed += (s, e) => currentLoginWindow?.Show();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error opening forgot password window");
            ErrorMessage = "Une erreur est survenue. Veuillez réessayer.";
        }
    }
}
