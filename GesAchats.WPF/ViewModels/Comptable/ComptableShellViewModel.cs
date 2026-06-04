using System.Windows.Input;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.ViewModels.Comptable;

public class ComptableShellViewModel : BaseViewModel
{
    private readonly IUserSession _userSession;
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _navigationService;
    private string _userName = string.Empty;
    private string _activePage = "Dashboard";
    private bool _isCollapsed;

    public string UserName
    {
        get => _userName;
        set => SetProperty(ref _userName, value);
    }

    public string ActivePage
    {
        get => _activePage;
        set => SetProperty(ref _activePage, value);
    }

    public bool IsCollapsed
    {
        get => _isCollapsed;
        set => SetProperty(ref _isCollapsed, value);
    }

    public ICommand NavigateCommand { get; }
    public ICommand LogoutCommand { get; }
    public ICommand ToggleSidebarCommand { get; }

    public ComptableShellViewModel(IUserSession userSession, IServiceProvider serviceProvider, INavigationService navigationService)
    {
        _userSession = userSession;
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;
        UserName = _userSession.CurrentUser?.FullName ?? "Comptable";
        Title = "GesAchats - Espace Comptable";

        NavigateCommand = new RelayCommand(p =>
        {
            var page = p?.ToString() ?? "Dashboard";
            ActivePage = page;
            _navigationService.NavigateTo(page);
        });
        LogoutCommand = new RelayCommand(_ => ExecuteLogout());
        ToggleSidebarCommand = new RelayCommand(_ => IsCollapsed = !IsCollapsed);
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
