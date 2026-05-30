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
            "DeliveryNotes" => _serviceProvider.GetRequiredService<DeliveryNotes.AdminDeliveryNotesPage>(),
            "PriceAnalysis" => _serviceProvider.GetRequiredService<Acheteur.Analyses.PurchaseHistoryPage>(),
            "Invoices" => _serviceProvider.GetRequiredService<Comptable.Factures.InvoicePaymentTrackingPage>(),
            "Payments" => _serviceProvider.GetRequiredService<Comptable.Reglements.ReglementsPage>(),
            "ProductStats" => _serviceProvider.GetRequiredService<Acheteur.Analyses.ProductStatsPage>(),
            "Employees" => _serviceProvider.GetRequiredService<Employees.EmployeeManagementPage>(),
            "Orders" => _serviceProvider.GetRequiredService<Orders.OrderManagementPage>(),
            _ => null
        };

        if (page is Page wpfPage)
        {
            if (parameter != null && wpfPage.DataContext is INavigatable navigatable)
            {
                navigatable.OnNavigatedTo(parameter);
            }
            MainFrame.Navigate(wpfPage);
        }
    }
}
