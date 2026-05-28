using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GesAchats.Data.Context;
using GesAchats.Core.Interfaces;
using GesAchats.Data;
using GesAchats.Core.Services;
using GesAchats.WPF.ViewModels.Auth;
using GesAchats.WPF.ViewModels.Magasinier;
using GesAchats.WPF.Views.Auth;
using GesAchats.WPF.Views.Magasinier;
using GesAchats.WPF.Views.Magasinier.Dashboard;
using GesAchats.WPF.Views.Magasinier.Stock;
using GesAchats.WPF.Views.Magasinier.Livraisons;
using GesAchats.WPF.Views.Magasinier.NeedsList;
using GesAchats.WPF.ViewModels.Acheteur;
using GesAchats.WPF.ViewModels.Comptable;
using GesAchats.WPF.Views.Acheteur;
using GesAchats.WPF.Views.Acheteur.Dashboard;
using GesAchats.WPF.Views.Acheteur.Besoins;
using GesAchats.WPF.Views.Acheteur.Devis;
using GesAchats.WPF.Views.Acheteur.Analyses;
using GesAchats.WPF.Views.Acheteur.Fournisseurs;
using GesAchats.WPF.Views.Acheteur.Commandes;
using GesAchats.WPF.Services;
using Microsoft.Extensions.Configuration;

using GesAchats.WPF.Views.Magasinier.NeedsHistory;

namespace GesAchats.WPF;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Correction globale pour Npgsql/PostgreSQL : force l'utilisation des DateTime UTC
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        // Gestion globale des erreurs
        this.DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Une erreur non gérée est survenue : {args.Exception.Message}", "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                Configuration = builder.Build();

                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);

                ServiceProvider = serviceCollection.BuildServiceProvider();

                // Initialisation de la base de données (Seed) de manière asynchrone sans bloquer l'UI
                try
                {
                    using (var scope = ServiceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<GesAchatsDbContext>();
                        await DbInitializer.SeedDataAsync(context);
                    }
                }
                catch (Exception dbEx)
                {
                    MessageBox.Show($"Avertissement : Impossible d'initialiser la base de données.\n\nErreur : {dbEx.Message}", 
                        "Avertissement Base de Données", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                // Démarrage de l'interface utilisateur
                var loginWindow = ServiceProvider.GetRequiredService<LoginWindow>();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur fatale au démarrage de l'application :\n{ex.Message}\n\n{ex.StackTrace}", 
                    "Erreur de Démarrage", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Configuration de la base de données - Transient pour WPF pour éviter les conflits de DbContext
        services.AddDbContext<GesAchatsDbContext>(options =>
            options.UseNpgsql(Configuration.GetConnectionString("DefaultConnection")),
            ServiceLifetime.Transient);

        // Injection des dépendances Data & Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IUserSession, UserSession>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IStockService, StockService>();
        services.AddTransient<IComparativeAnalysisService, ComparativeAnalysisService>();
        services.AddTransient<IPriceAnalysisService, PriceAnalysisService>();
        services.AddTransient<INeedsAnalyticsService, NeedsAnalyticsService>();
        services.AddTransient<IPdfGeneratorService, PdfGeneratorService>();
        services.AddTransient<IQuotationPdfService, QuotationPdfService>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IConformityService, ConformityService>();
        services.AddTransient<IFileStorageService>(s => new FileStorageService(AppDomain.CurrentDomain.BaseDirectory));

        // ViewModels - Focus Magasinier
        services.AddTransient<LoginViewModel>();
        services.AddTransient<MagasinierShellViewModel>();
        services.AddTransient<MagasinierDashboardViewModel>();
        services.AddTransient<DeliveryNotesViewModel>();
        services.AddTransient<AddProductViewModel>();
        services.AddTransient<StockAnalysisViewModel>();
        services.AddTransient<NeedsListViewModel>();
        services.AddTransient<NeedsHistoryViewModel>();
        services.AddTransient<NeedsDetailsViewModel>();
        services.AddTransient<NeedsStatisticsViewModel>();

        // ViewModels - Focus Acheteur
        services.AddTransient<AcheteurShellViewModel>();
        services.AddTransient<ReceivedNeedsViewModel>();
        services.AddTransient<QuotesManagementViewModel>();
        services.AddTransient<SupplierManagementViewModel>();
        services.AddTransient<SupplierDialogViewModel>();
        services.AddTransient<QuotationPriceEntryViewModel>();
        services.AddTransient<AdvancedComparativeAnalysisViewModel>();
        services.AddTransient<ComparativeAnalysisViewModel>();
        services.AddTransient<BonsCommandeViewModel>();
        services.AddTransient<OrderTrackingViewModel>();
        services.AddTransient<PurchaseHistoryViewModel>();
        services.AddTransient<ProductStatsViewModel>();

        // ViewModels - Focus Admin
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.AdminShellViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.AdminDashboardViewModel>();

        // ViewModels - Focus Comptable
        services.AddTransient<ComptableShellViewModel>();
        services.AddTransient<ComptableDashboardViewModel>();
        services.AddTransient<FacturesViewModel>();
        services.AddTransient<InvoiceFormViewModel>();
        services.AddTransient<ConformityCheckViewModel>();
        services.AddTransient<PaymentFormViewModel>();
        services.AddTransient<PaymentHistoryViewModel>();

        // Views - Focus Magasinier
        services.AddTransient<LoginWindow>();
        services.AddTransient<MagasinierShell>();
        services.AddTransient<MagasinierDashboardPage>();
        services.AddTransient<StockPage>();
        services.AddTransient<LivraisonsPage>();
        services.AddTransient<NeedsListPage>();
        services.AddTransient<NeedsHistoryPage>();
        services.AddTransient<NeedsDetailsWindow>();
        services.AddTransient<NeedsStatisticsWindow>();
        services.AddTransient<AddProductDialog>();

        // Views - Focus Acheteur
         services.AddTransient<AcheteurShell>();
         services.AddTransient<AcheteurDashboardPage>();
         services.AddTransient<BesoinsMagasinPage>();
         services.AddTransient<QuotesManagementPage>();
         services.AddTransient<QuotationPriceEntryPage>();
         services.AddTransient<AdvancedComparativeAnalysisPage>();
         services.AddTransient<AnalyseComparativePage>();
         services.AddTransient<GestionFournisseursPage>();
         services.AddTransient<BonsCommandePage>();
         services.AddTransient<OrderTrackingPage>();
         services.AddTransient<PurchaseHistoryPage>();
         services.AddTransient<ProductStatsPage>();

         // Views - Focus Admin
         services.AddTransient<GesAchats.WPF.Views.Admin.AdminShell>();
         services.AddTransient<GesAchats.WPF.Views.Admin.Dashboard.AdminDashboardPage>();
         services.AddTransient<GesAchats.WPF.Views.Admin.Employees.EmployeeManagementPage>();
         services.AddTransient<GesAchats.WPF.Views.Admin.Orders.OrderManagementPage>();

         // Views - Focus Comptable
         services.AddTransient<GesAchats.WPF.Views.Comptable.ComptableShell>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Dashboard.ComptableDashboardPage>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.FacturesPage>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.InvoiceFormPage>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.ConformityCheckPage>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.PaymentFormPage>();
          services.AddTransient<GesAchats.WPF.Views.Comptable.Reglements.ReglementsPage>();
      }
}
