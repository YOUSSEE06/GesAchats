using System;
using System.Windows;
using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;
using GesAchats.WPF.Services;
using GesAchats.WPF.Views.Acheteur.Dashboard;
using GesAchats.WPF.Views.Acheteur.Besoins;
using GesAchats.WPF.Views.Acheteur.Devis;
using GesAchats.WPF.Views.Acheteur.Analyses;
using GesAchats.WPF.Views.Acheteur.Fournisseurs;
using GesAchats.WPF.Views.Acheteur.Commandes;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.Views.Acheteur;

public partial class AcheteurShell : Window
{
    private readonly IServiceProvider _serviceProvider;

    private readonly INavigationService _navigationService;

    public AcheteurShell(AcheteurShellViewModel viewModel, IServiceProvider serviceProvider, INavigationService navigationService)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;
        DataContext = viewModel;

        // Configuration de la navigation
        _navigationService.OnNavigate += OnNavigate;

        // Page par défaut
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
            "Dashboard" => _serviceProvider.GetRequiredService<AcheteurDashboardPage>(),
            "Besoins" => _serviceProvider.GetRequiredService<BesoinsMagasinPage>(),
            "Devis" => _serviceProvider.GetRequiredService<QuotesManagementPage>(),
            "SaisiePrix" => _serviceProvider.GetRequiredService<QuotationPriceEntryPage>(),
            "Analyses" => _serviceProvider.GetRequiredService<AdvancedComparativeAnalysisPage>(),
            "AnalysesOld" => _serviceProvider.GetRequiredService<AnalyseComparativePage>(),
            "Historique" => _serviceProvider.GetRequiredService<PurchaseHistoryPage>(),
            "ProductStats" => _serviceProvider.GetRequiredService<ProductStatsPage>(),
            "Fournisseurs" => _serviceProvider.GetRequiredService<GestionFournisseursPage>(),
            "Commandes" => _serviceProvider.GetRequiredService<BonsCommandePage>(),
            _ => null
        };

        if (page is Page wpfPage)
        {
            // Si on a un paramètre, on essaie de le passer au ViewModel de la page
            if (parameter != null && wpfPage.DataContext is INavigatable navigatable)
            {
                navigatable.OnNavigatedTo(parameter);
            }
            
            MainFrame.Navigate(wpfPage);
        }
    }
}
