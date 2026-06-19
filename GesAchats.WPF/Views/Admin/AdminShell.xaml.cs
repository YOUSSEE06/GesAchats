using System.Windows;
using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Admin;
using GesAchats.WPF.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.Views.Admin;

public partial class AdminShell : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _navigationService;

    public AdminShell(AdminShellViewModel viewModel, IServiceProvider serviceProvider, INavigationService navigationService)
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
            "Dashboard" => _serviceProvider.GetRequiredService<Dashboard.AdminDashboardPage>(),
            "StockGlobal" => _serviceProvider.GetRequiredService<Stock.AdminStockPage>(),
            "NeedsHistory" => _serviceProvider.GetRequiredService<NeedsHistory.AdminNeedsHistoryPage>(),
            "HistoriqueSorties" => _serviceProvider.GetRequiredService<StockExits.AdminHistoriqueSortiesPage>(),
            "DeliveryNotes" => _serviceProvider.GetRequiredService<DeliveryNotes.AdminDeliveryNotesPage>(),
            "PriceAnalysis" => _serviceProvider.GetRequiredService<Acheteur.Analyses.PurchaseHistoryPage>(),
            "Historique" => _serviceProvider.GetRequiredService<Acheteur.Analyses.PurchaseHistoryPage>(), // Pour le bouton retour depuis ProductStats
            "Invoices" => _serviceProvider.GetRequiredService<Comptable.Factures.InvoicePaymentTrackingPage>(),
            "InvoicesAdmin" => _serviceProvider.GetRequiredService<Comptable.Factures.FacturesPage>(),
            "Payments" => _serviceProvider.GetRequiredService<Comptable.Reglements.ReglementsPage>(),
            "ProductStats" => _serviceProvider.GetRequiredService<Acheteur.Analyses.ProductStatsPage>(),
            "Employees" => _serviceProvider.GetRequiredService<Employees.EmployeeManagementPage>(),
            "Orders" => _serviceProvider.GetRequiredService<Orders.OrderManagementPage>(),
            "SuiviFournisseurs" => _serviceProvider.GetRequiredService<SuiviFournisseurs.SuiviFournisseursPage>(),
            "SituationFournisseur" => _serviceProvider.GetRequiredService<SuiviFournisseurs.SituationFournisseurPage>(),
            _ => null
        };

        if (page is Page wpfPage)
        {
            if (wpfPage is INavigatable navigatablePage)
            {
                navigatablePage.OnNavigatedTo(parameter!);
            }
            else if (wpfPage.DataContext is INavigatable navigatableVm)
            {
                navigatableVm.OnNavigatedTo(parameter!);
            }
            MainFrame.Navigate(wpfPage);
        }
    }
}
