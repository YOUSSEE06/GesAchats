using System.Windows;
using System.Windows.Controls;
using GesAchats.WPF.Services;
using Microsoft.Extensions.DependencyInjection;
using GesAchats.WPF.Views.Comptable.Factures;
using GesAchats.WPF.Views.Comptable.Reglements;
using GesAchats.WPF.Views.Comptable.Dashboard;
using GesAchats.WPF.Views.Auth;
using GesAchats.WPF.Views.Magasinier.Livraisons;

namespace GesAchats.WPF.Views.Comptable;

public partial class ComptableShell : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _navigationService;

    public ComptableShell(IServiceProvider serviceProvider, INavigationService navigationService)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;

        _navigationService.OnNavigate += OnNavigate;
        
        // Navigation par défaut
        NavigateToDashboard(null, null);
    }

    private void OnNavigate(string pageName, object? parameter)
    {
        Page? page = pageName switch
        {
            "Factures" => _serviceProvider.GetRequiredService<FacturesPage>(),
            "InvoiceForm" => _serviceProvider.GetRequiredService<InvoiceFormPage>(),
            "ConformityCheck" => _serviceProvider.GetRequiredService<ConformityCheckPage>(),
            "PaymentForm" => _serviceProvider.GetRequiredService<PaymentFormPage>(),
            "Reglements" => _serviceProvider.GetRequiredService<ReglementsPage>(),
            "Dashboard" => _serviceProvider.GetRequiredService<ComptableDashboardPage>(),
            "Livraisons" => _serviceProvider.GetRequiredService<LivraisonsPage>(),
            "InvoicePaymentTracking" => _serviceProvider.GetRequiredService<InvoicePaymentTrackingPage>(),
            _ => null
        };

        if (page != null)
        {
            if (page is INavigatable navigatable)
            {
                navigatable.OnNavigatedTo(parameter!);
            }
            else if (page.DataContext is INavigatable navigatableVm)
            {
                navigatableVm.OnNavigatedTo(parameter!);
            }
            MainFrame.Navigate(page);
        }
    }

    private void NavigateToDashboard(object? sender, RoutedEventArgs? e) => _navigationService.NavigateTo("Dashboard");
    private void NavigateToFactures(object? sender, RoutedEventArgs? e) => _navigationService.NavigateTo("Factures");
    private void NavigateToLivraisons(object? sender, RoutedEventArgs? e) => _navigationService.NavigateTo("Livraisons");
    private void NavigateToReglements(object? sender, RoutedEventArgs? e) => _navigationService.NavigateTo("Reglements");
    private void NavigateToInvoicePaymentTracking(object? sender, RoutedEventArgs? e) => _navigationService.NavigateTo("InvoicePaymentTracking");

    private void Logout(object sender, RoutedEventArgs e)
    {
        var loginWindow = _serviceProvider.GetRequiredService<LoginWindow>();
        loginWindow.Show();
        this.Close();
    }
}
