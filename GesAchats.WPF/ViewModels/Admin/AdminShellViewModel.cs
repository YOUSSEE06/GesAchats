using System.Windows.Input;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminShellViewModel : BaseViewModel
{
    private readonly IUserSession _userSession;
    private readonly INavigationService _navigationService;
    private string _userName = string.Empty;

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    public AdminShellViewModel(IUserSession userSession, INavigationService navigationService)
    {
        _userSession = userSession;
        _navigationService = navigationService;
        UserName = _userSession.CurrentUser?.FullName ?? "Administrateur";
        Title = "GesAchats - Espace Admin";

        NavigateCommand = new RelayCommand(p => _navigationService.NavigateTo(p?.ToString() ?? "Dashboard"));
        LogoutCommand = new RelayCommand(_ => ExecuteLogout());
    }

    private void ExecuteLogout()
    {
        _userSession.EndSession();
        var serviceProvider = ((App)System.Windows.Application.Current).ServiceProvider;
        var loginWindow = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Views.Auth.LoginWindow>(serviceProvider);
        loginWindow.Show();

        System.Windows.Application.Current.MainWindow?.Close();
        System.Windows.Application.Current.MainWindow = loginWindow;
    }
}
