using System.Windows;
using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Magasinier;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Views.Magasinier.Dashboard;
using GesAchats.WPF.Views.Magasinier.Stock;
using GesAchats.WPF.Views.Magasinier.Livraisons;
using GesAchats.WPF.Views.Magasinier.NeedsList;
using GesAchats.WPF.Views.Magasinier.NeedsHistory;
using GesAchats.WPF.Views.Magasinier.StockExits;
using GesAchats.WPF.Views.Magasinier.Orders;
using GesAchats.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.Views.Magasinier;

public partial class MagasinierShell : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _navigationService;

    public MagasinierShell(MagasinierShellViewModel viewModel, IServiceProvider serviceProvider, INavigationService navigationService)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;
        DataContext = viewModel;

        // Écouter les demandes de navigation du ViewModel
        _navigationService.OnNavigate += OnNavigate;

        // Charger la page par défaut
        NavigateToPage("Dashboard");

        this.Closed += (s, e) => _navigationService.OnNavigate -= OnNavigate;
    }

    private void OnNavigate(string pageName, object? parameter)
    {
        NavigateToPage(pageName, parameter);
    }

    private void NavigateToPage(string pageName, object? parameter = null)
    {
        object? page = pageName switch
        {
            "Dashboard" => _serviceProvider.GetRequiredService<MagasinierDashboardPage>(),
            "Stock" => CreateStockPage(),
            "Livraisons" => _serviceProvider.GetRequiredService<LivraisonsPage>(),
            "Needs" => CreateNeedsListPage(),
            "NeedsHistory" => _serviceProvider.GetRequiredService<NeedsHistoryPage>(),
            "StockExits" => _serviceProvider.GetRequiredService<StockExitPage>(),
            "Orders" => _serviceProvider.GetRequiredService<MagasinierOrdersPage>(),
            _ => null
        };

        if (page is Page wpfPage)
        {
            if (wpfPage.DataContext is INavigatable navigatable)
            {
                navigatable.OnNavigatedTo(parameter!);
            }
            MainFrame.Navigate(wpfPage);
        }
    }

    private StockPage CreateStockPage()
    {
        var page = _serviceProvider.GetRequiredService<StockPage>();
        return page;
    }

    private NeedsListPage CreateNeedsListPage()
    {
        var page = _serviceProvider.GetRequiredService<NeedsListPage>();
        var vm = (NeedsListViewModel)page.DataContext;
        vm.CancelCommand = new RelayCommand(_ => NavigateToPage("Stock"));
        return page;
    }
}
