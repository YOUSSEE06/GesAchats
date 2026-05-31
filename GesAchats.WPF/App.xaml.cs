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
using GesAchats.WPF.Views.Magasinier.StockExits;
using GesAchats.WPF.Views.Magasinier.Orders;
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
using Serilog;

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
            MessageBox.Show($"Une erreur non gérée est survenue : {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
            {
                var builder = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

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
                    
                    // Étape 1: Vérifier et ajouter les colonnes manquantes à la table Products
                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$
                        BEGIN
                            -- Ajouter LastPurchaseDate si elle n'existe pas
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                           WHERE table_name = 'Products' AND column_name = 'LastPurchaseDate') THEN
                                ALTER TABLE ""Products"" ADD COLUMN ""LastPurchaseDate"" TIMESTAMP WITH TIME ZONE NULL;
                            END IF;

                            -- Ajouter DailyConsumption si elle n'existe pas
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                           WHERE table_name = 'Products' AND column_name = 'DailyConsumption') THEN
                                ALTER TABLE ""Products"" ADD COLUMN ""DailyConsumption"" NUMERIC(18,2) NOT NULL DEFAULT 1;
                            END IF;

                            -- Ajouter IsNew si elle n'existe pas
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                           WHERE table_name = 'Products' AND column_name = 'IsNew') THEN
                                ALTER TABLE ""Products"" ADD COLUMN ""IsNew"" BOOLEAN NOT NULL DEFAULT false;
                            END IF;

                            -- Ajouter CreatedBy si elle n'existe pas
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                           WHERE table_name = 'Products' AND column_name = 'CreatedBy') THEN
                                ALTER TABLE ""Products"" ADD COLUMN ""CreatedBy"" VARCHAR(255) NULL;
                            END IF;

                            -- RÉPARATION DU MODULE COMPTABLE (bc_id, bl_id, etc.)
                            -- 1. Renommage des tables si nécessaire
                            IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Invoices') AND 
                               NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'factures') THEN
                                ALTER TABLE ""Invoices"" RENAME TO factures;
                            END IF;

                            IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'Payments') AND 
                               NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'reglements') THEN
                                ALTER TABLE ""Payments"" RENAME TO reglements;
                            END IF;

                            IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'DeliveryNotes') AND 
                               NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'bons_livraison') THEN
                                ALTER TABLE ""DeliveryNotes"" RENAME TO bons_livraison;
                            END IF;

                            -- 2. Ajout des colonnes à la table factures
                            IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'factures') THEN
                                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='bc_id') THEN
                                    ALTER TABLE factures ADD COLUMN bc_id INTEGER;
                                END IF;
                                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='bl_id') THEN
                                    ALTER TABLE factures ADD COLUMN bl_id INTEGER;
                                END IF;
                                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='montant_ht') THEN
                                    ALTER TABLE factures ADD COLUMN montant_ht NUMERIC(18,2) DEFAULT 0;
                                END IF;
                                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='montant_tva') THEN
                                    ALTER TABLE factures ADD COLUMN montant_tva NUMERIC(18,2) DEFAULT 0;
                                END IF;
                                IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='factures' AND column_name='taux_tva') THEN
                                    ALTER TABLE factures ADD COLUMN taux_tva NUMERIC(18,2) DEFAULT 20.00;
                                END IF;
                            END IF;
                        END $$;
                    ");

                    // Étape 2: Vérifier si la table __EFMigrationsHistory existe, et marquer InitialPostgres comme appliquée si nécessaire
                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$
                        BEGIN
                            -- Vérifier si la table __EFMigrationsHistory existe
                            IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory') THEN
                                -- Vérifier si InitialPostgres est déjà enregistrée
                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260430151534_InitialPostgres') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260430151534_InitialPostgres', '8.0.10');
                                END IF;

                                -- Vérifier si les autres migrations sont déjà enregistrées (si vous en avez)
                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260506113458_AddInvoiceFilePath') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260506113458_AddInvoiceFilePath', '8.0.10');
                                END IF;
                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260506113841_SyncInvoiceModel') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260506113841_SyncInvoiceModel', '8.0.10');
                                END IF;
                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260506114425_FixPendingChanges') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260506114425_FixPendingChanges', '8.0.10');
                                END IF;
                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260505165725_UpdateComptableModule') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260505165725_UpdateComptableModule', '8.0.10');
                                END IF;
                            END IF;
                        END $$;
                    ");

                    // Étape 3: Appliquer les migrations restantes (notamment AddMagasin)
                    await context.Database.MigrateAsync();
                    
                    // Étape 4: Nettoyer les magasins dupliqués
                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$
                        BEGIN
                            -- Garder seulement le premier magasin pour chaque nom, et supprimer les doublons
                            DELETE FROM ""Magasins""
                            WHERE ""Id"" NOT IN (
                                SELECT MIN(""Id"")
                                FROM ""Magasins""
                                GROUP BY ""Nom""
                            );

                            -- Mettre à jour les produits qui pointaient vers des magasins supprimés, pour pointer vers le premier magasin
                            UPDATE ""Products""
                            SET ""MagasinId"" = (SELECT MIN(""Id"") FROM ""Magasins"" WHERE ""Nom"" = 'Magasin Principal')
                            WHERE ""MagasinId"" NOT IN (SELECT ""Id"" FROM ""Magasins"");
                        END $$;
                    ");

                    // Étape 5: Seed des données
                    await DbInitializer.SeedDataAsync(context);

                    // Étape 6: Créer la table StockExits manuellement si elle n'existe pas
                    await context.Database.ExecuteSqlRawAsync(@"
                        CREATE TABLE IF NOT EXISTS ""StockExits"" (
                            ""Id"" INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                            ""ProductId"" INTEGER NOT NULL,
                            ""Quantity"" NUMERIC(18,2) NOT NULL,
                            ""ExitDate"" TIMESTAMP WITH TIME ZONE NOT NULL,
                            ""ProjectOrChantier"" VARCHAR(200),
                            ""Reason"" VARCHAR(500),
                            ""StockAfterExit"" NUMERIC(18,2) NOT NULL,
                            ""CreatedById"" INTEGER NOT NULL,
                            ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL,
                            CONSTRAINT ""FK_StockExits_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE,
                            CONSTRAINT ""FK_StockExits_Users_CreatedById"" FOREIGN KEY (""CreatedById"") REFERENCES ""Users"" (""Id"") ON DELETE CASCADE
                        );
                    ");
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
                MessageBox.Show($"Erreur fatale au démarrage de l'application :\n{ex.Message}\n\n{ex.StackTrace}\n\nInner Exception : {ex.InnerException?.Message}", 
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
        services.AddTransient<IDashboardService, DashboardService>();
        services.AddTransient<IComparativeAnalysisService, ComparativeAnalysisService>();
        services.AddTransient<IPriceAnalysisService, PriceAnalysisService>();
        services.AddTransient<INeedsAnalyticsService, NeedsAnalyticsService>();
        services.AddTransient<IPdfGeneratorService, PdfGeneratorService>();
        services.AddTransient<IQuotationPdfService, QuotationPdfService>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IConformityService, ConformityService>();
        services.AddTransient<IFileStorageService>(s => new FileStorageService(AppDomain.CurrentDomain.BaseDirectory));
        
        // Dashboard services
        services.AddTransient<IPurchaseOrderService, PurchaseOrderService>();
        services.AddTransient<INeedsService, NeedsService>();
        services.AddTransient<ISupplierService, SupplierService>();
        services.AddTransient<IInvoiceService, InvoiceService>();
        
        // Serilog - simple logger for now
        services.AddSingleton<Serilog.ILogger>(sp => 
            new LoggerConfiguration()
                .MinimumLevel.Information()
                .CreateLogger());

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
        services.AddTransient<StockExitViewModel>();
        services.AddTransient<MagasinierOrdersViewModel>();

        // ViewModels - Focus Acheteur
        services.AddTransient<AcheteurShellViewModel>();
        services.AddTransient<AcheteurDashboardViewModel>();
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
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.AdminStockViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.AdminNeedsHistoryViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.AdminDeliveryNotesViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.AdminPriceAnalysisViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Acheteur.PurchaseHistoryViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Acheteur.ProductStatsViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.AdminOrdersViewModel>();

        // ViewModels - Focus Comptable
        services.AddTransient<ComptableShellViewModel>();
        services.AddTransient<ComptableDashboardViewModel>();
        services.AddTransient<FacturesViewModel>();
        services.AddTransient<InvoiceFormViewModel>();
        services.AddTransient<ConformityCheckViewModel>();
        services.AddTransient<PaymentFormViewModel>();
        services.AddTransient<PaymentHistoryViewModel>();
        services.AddTransient<FactureDetailsViewModel>();
        services.AddTransient<InvoicePaymentTrackingViewModel>();
        services.AddTransient<InvoicePaymentDetailsPopupViewModel>();

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
        services.AddTransient<GesAchats.WPF.Views.Magasinier.StockExits.StockExitPage>();
        services.AddTransient<MagasinierOrdersPage>();

        // Views - Focus Acheteur
         services.AddTransient<AcheteurShell>();
         services.AddTransient<GesAchats.WPF.Views.Acheteur.Dashboard.AcheteurDashboardPage>();
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
         services.AddTransient<GesAchats.WPF.Views.Admin.Stock.AdminStockPage>();
         services.AddTransient<GesAchats.WPF.Views.Admin.PriceAnalysis.PriceAnalysisPage>();
         services.AddTransient<GesAchats.WPF.Views.Acheteur.Analyses.ProductStatsPage>();
         services.AddTransient<GesAchats.WPF.Views.Admin.NeedsHistory.AdminNeedsHistoryPage>();
         services.AddTransient<GesAchats.WPF.Views.Admin.DeliveryNotes.AdminDeliveryNotesPage>();

         // Views - Focus Comptable
         services.AddTransient<GesAchats.WPF.Views.Comptable.ComptableShell>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Dashboard.ComptableDashboardPage>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.FacturesPage>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.InvoiceFormPage>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.ConformityCheckPage>();
         services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.PaymentFormPage>();
          services.AddTransient<GesAchats.WPF.Views.Comptable.Reglements.ReglementsPage>();
          services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.FactureDetailsWindow>();
          services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.InvoicePaymentTrackingPage>();
          services.AddTransient<GesAchats.WPF.Views.Comptable.Factures.InvoicePaymentDetailsPopup>();
      }
}
