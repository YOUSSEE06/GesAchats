using System.Windows.Input;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Auth;

public class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private string _username = string.Empty;
    private string _errorMessage = string.Empty;

    public string Username
    {
        get => _username;
        set => SetProperty(ref _username, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand LoginCommand { get; }

    public LoginViewModel(IAuthService authService)
    {
        _authService = authService;
        LoginCommand = new RelayCommand(async p => await ExecuteLogin(p), _ => !IsBusy);
        Title = "Connexion - GesAchats";
    }

    private async Task ExecuteLogin(object? parameter)
    {
        var passwordBox = parameter as System.Windows.Controls.PasswordBox;
        if (passwordBox == null) return;

        string password = passwordBox.Password;

        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(password))
        {
            ErrorMessage = "Veuillez saisir votre identifiant et votre mot de passe.";
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var user = await _authService.LoginAsync(Username, password);
            if (user != null)
            {
                NavigationRouter.GoToRole(user);
            }
            else
            {
                ErrorMessage = "Identifiants invalides.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Une erreur est survenue : {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
