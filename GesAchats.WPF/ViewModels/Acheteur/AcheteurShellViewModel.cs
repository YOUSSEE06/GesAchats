using System.Windows.Input;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class AcheteurShellViewModel : BaseViewModel
{
    private readonly IUserSession _userSession;
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _navigationService;
    private string _userName = string.Empty;

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }

    public AcheteurShellViewModel(IUserSession userSession, IServiceProvider serviceProvider, INavigationService navigationService)
    {
        _userSession = userSession;
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;
        UserName = _userSession.CurrentUser?.FullName ?? "Responsable Achat";
        Title = "Tableau de bord";

        NavigateCommand = new RelayCommand(p => _navigationService.NavigateTo(p?.ToString() ?? "Dashboard"));
        LogoutCommand = new RelayCommand(_ => ExecuteLogout());
    }

    private void ExecuteLogout()
    {
        _userSession.EndSession();
        var loginWindow = _serviceProvider.GetRequiredService<Views.Auth.LoginWindow>();
        loginWindow.Show();

        System.Windows.Application.Current.MainWindow?.Close();
        System.Windows.Application.Current.MainWindow = loginWindow;
    }
}
