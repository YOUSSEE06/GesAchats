using System.Windows;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using GesAchats.Data.Context;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Helpers;
using GesAchats.Data;
using GesAchats.Data.Repositories;
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
using Npgsql;

using GesAchats.WPF.Views.Magasinier.NeedsHistory;

namespace GesAchats.WPF;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; } = null!;
    public IConfiguration Configuration { get; private set; } = null!;

    private static string _resolvedConnectionString = null!;
    private static string _resolvedEndpointLabel = "configuration .env";
    private static string _resolvedEndpointDiagnostic = "(aucun)";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        this.DispatcherUnhandledException += (s, args) =>
        {
            Log.Error(args.Exception, "Dispatcher unhandled exception");
            MessageBox.Show($"Une erreur non gérée est survenue : {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                Log.Error(ex, "AppDomain unhandled exception");
                MessageBox.Show($"Une erreur fatale est survenue : {ex.Message}\n\n{ex.StackTrace}", "Erreur Fatale", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            Log.Error(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        try
        {
            EnvLoader.Load(AppDomain.CurrentDomain.BaseDirectory);

            var anonKey = EnvLoader.Get("SUPABASE_ANON_KEY") ?? EnvLoader.Get("SUPABASE_PUBLISHABLE_KEY");
            if (string.IsNullOrWhiteSpace(anonKey) || anonKey.Contains("COLLEZ", StringComparison.OrdinalIgnoreCase) || anonKey.Contains("TON_", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(
                    "SUPABASE_ANON_KEY / SUPABASE_PUBLISHABLE_KEY n'est pas configurée dans le fichier .env.\n\n" +
                    "La connexion et le bootstrap Supabase Auth échoueront tant que la clé n'est pas renseignée " +
                    "(Dashboard Supabase → Settings → API → anon key).",
                    "Configuration incomplète", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            var builder = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            Configuration = builder.Build();

            var serviceCollection = new ServiceCollection();

            var (connStr, label, diag) = await ProbeBestConnectionStringAsync();
            _resolvedConnectionString = connStr;
            _resolvedEndpointLabel = label;
            _resolvedEndpointDiagnostic = diag;
            Log.Information("Endpoint BDD sélectionné : {Label} — diagnostic : {Diag}", label, diag);

            ConfigureServices(serviceCollection, connStr);

            ServiceProvider = serviceCollection.BuildServiceProvider();

            try
            {
                using (var scope = ServiceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<GesAchatsDbContext>();

                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$
                        BEGIN
                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                           WHERE table_name = 'Products' AND column_name = 'LastPurchaseDate') THEN
                                ALTER TABLE ""Products"" ADD COLUMN ""LastPurchaseDate"" TIMESTAMP WITH TIME ZONE NULL;
                            END IF;

                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                           WHERE table_name = 'Products' AND column_name = 'DailyConsumption') THEN
                                ALTER TABLE ""Products"" ADD COLUMN ""DailyConsumption"" NUMERIC(18,2) NOT NULL DEFAULT 1;
                            END IF;

                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                           WHERE table_name = 'Products' AND column_name = 'IsNew') THEN
                                ALTER TABLE ""Products"" ADD COLUMN ""IsNew"" BOOLEAN NOT NULL DEFAULT false;
                            END IF;

                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                           WHERE table_name = 'Products' AND column_name = 'CreatedBy') THEN
                                ALTER TABLE ""Products"" ADD COLUMN ""CreatedBy"" VARCHAR(255) NULL;
                            END IF;

                            IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                           WHERE table_name = 'Users' AND column_name = 'SupabaseAuthId') THEN
                                ALTER TABLE ""Users"" ADD COLUMN ""SupabaseAuthId"" UUID NULL;
                            END IF;

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

                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$
                        BEGIN
                            IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory') THEN
                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260430151534_InitialPostgres') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260430151534_InitialPostgres', '8.0.10');
                                END IF;

                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260505165725_UpdateComptableModule') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260505165725_UpdateComptableModule', '8.0.10');
                                END IF;

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

                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260528151908_AddMagasin') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260528151908_AddMagasin', '8.0.10');
                                END IF;

                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260530010111_AddSetNullForPurchaseOrderNeedId') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260530010111_AddSetNullForPurchaseOrderNeedId', '8.0.10');
                                END IF;

                                IF NOT EXISTS (SELECT 1 FROM ""__EFMigrationsHistory"" WHERE ""MigrationId"" = '20260601020010_AddDashboardKpiSnapshot') THEN
                                    INSERT INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES ('20260601020010_AddDashboardKpiSnapshot', '10.0.7');
                                END IF;
                            END IF;
                        END $$;
                    ");

                    await context.Database.MigrateAsync();
                    
                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$
                        BEGIN
                            DELETE FROM ""Magasins""
                            WHERE ""Id"" NOT IN (
                                SELECT MIN(""Id"")
                                FROM ""Magasins""
                                GROUP BY ""Nom""
                            );

                            UPDATE ""Products""
                            SET ""MagasinId"" = (SELECT MIN(""Id"") FROM ""Magasins"" WHERE ""Nom"" = 'Magasin Principal')
                            WHERE ""MagasinId"" NOT IN (SELECT ""Id"" FROM ""Magasins"");
                        END $$;
                    ");

                    var supabaseAuthClient = scope.ServiceProvider.GetRequiredService<SupabaseAuthClient>();
                    await DbInitializer.SeedRolesAsync(context);
                    var adminLinked = await DbInitializer.BootstrapAdminAsync(context, supabaseAuthClient);
                    if (!adminLinked)
                    {
                        MessageBox.Show(
                            "Le compte administrateur n'a pas pu être initialisé chez Supabase Auth.\n\n" +
                            "Vérifiez dans le fichier .env :\n" +
                            "  • ADMIN_EMAIL / ADMIN_PASSWORD (email et mot de passe admin)\n" +
                            "  • SUPABASE_SECRET_KEY (clé service_role, Dashboard Supabase → Settings → API)\n" +
                            "  • SUPABASE_URL / SUPABASE_ANON_KEY\n\n" +
                            "Vous pouvez aussi utiliser « Mot de passe oublié » sur l'écran de connexion.",
                            "Compte admin non initialisé", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }

                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$
                        BEGIN
                            IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'StockExits') THEN
                                CREATE TABLE ""StockExits"" (
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
                            END IF;
                        END $$;
                    ");

                    await context.Database.ExecuteSqlRawAsync(@"
                        DO $$
                        BEGIN
                            IF NOT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'DashboardKpiSnapshots') THEN
                                CREATE TABLE ""DashboardKpiSnapshots"" (
                                    ""Id"" INTEGER GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                                    ""SnapshotDate"" TIMESTAMP WITH TIME ZONE NOT NULL,
                                    ""BesEnCoursCount"" INTEGER NOT NULL,
                                    ""BesTransmisCount"" INTEGER NOT NULL,
                                    ""DevEnAttenteCount"" INTEGER NOT NULL,
                                    ""DevValideCount"" INTEGER NOT NULL,
                                    ""FournisseursActifsCount"" INTEGER NOT NULL,
                                    ""TotalBcCount"" INTEGER NOT NULL,
                                    ""CreatedAt"" TIMESTAMP WITH TIME ZONE NOT NULL
                                );
                            END IF;
                        END $$;
                    ");
                }
            }
            catch (Exception ex)
            {
                ShowDatabaseStartupError(ex, _resolvedConnectionString, _resolvedEndpointLabel, _resolvedEndpointDiagnostic);
            }

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

    /// <summary>
    /// Sonde plusieurs endpoints (celui du .env, puis les poolers aws-0 et aws-1 en
    /// mode session et transaction) et retourne la 1ère chaîne de connexion qui
    /// fonctionne. Inclut un diagnostic clair si aucun endpoint ne répond.
    /// </summary>
    private static async Task<(string ConnectionString, string Label, string Diagnostic)> ProbeBestConnectionStringAsync()
    {
        var errors = new System.Collections.Generic.List<string>();
        var poolerUser = EnvLoader.BuildPoolerUsername();

        var candidates = new System.Collections.Generic.List<(string ConnStr, string Label)>();

        try
        {
            candidates.Add((EnvLoader.BuildConnectionString(), "Endpoint .env principal"));
        }
        catch (Exception ex)
        {
            errors.Add($"[.env] Impossible de construire la chaîne principale : {ex.Message}");
        }

        foreach (var ep in EnvLoader.FallbackPoolerEndpoints)
        {
            try
            {
                var cs = EnvLoader.BuildConnectionString(
                    host: ep.Host,
                    port: ep.Port.ToString(),
                    username: poolerUser);
                candidates.Add((cs, ep.Label));
            }
            catch { }
        }

        var refDb = EnvLoader.InferProjectRef();
        if (!string.IsNullOrEmpty(refDb))
        {
            try
            {
                var directCs = EnvLoader.BuildConnectionString(
                    host: $"db.{refDb}.supabase.co",
                    port: "5432",
                    username: "postgres");
                candidates.Add((directCs, $"Connexion directe db.{refDb}.supabase.co (IPv6 uniquement)"));
            }
            catch { }
        }

        foreach (var (connStr, label) in candidates)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(12));
                await using var conn = new NpgsqlConnection(connStr);
                await conn.OpenAsync(cts.Token);
                await using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT 1";
                await cmd.ExecuteNonQueryAsync(cts.Token);
                return (connStr, label, $"OK via {label}");
            }
            catch (Exception ex)
            {
                var shortMsg = ex.InnerException?.Message ?? ex.Message;
                errors.Add($"[{label}] {ex.GetType().Name}: {Truncate(shortMsg, 180)}");
                Log.Warning("Échec sonde BDD {Label} : {Msg}", label, shortMsg);
            }
        }

        var diag = "Aucun endpoint Supabase n'a pu être joint. " +
                   "Le pooler Supavisor renvoie « tenant/user not found » : le mot de passe base de données " +
                   "(SUPABASE_PASSWORD) doit être réinitialisé depuis le Dashboard Supabase → Settings → Database " +
                   "puis recopié dans le .env. Si vous êtes sur un réseau IPv4-only, n'utilisez pas la connexion " +
                   "directe db.* (IPv6 uniquement) — prenez le Pooler Session / Transaction dans le bouton « Connect ».";

        var fallbackConn = candidates.Count > 0 ? candidates[0].ConnStr : EnvLoader.BuildConnectionString();
        return (fallbackConn, candidates.Count > 0 ? candidates[0].Label : ".env (défaut)",
            diag + "\n\nDétails des sondages :\n- " + string.Join("\n- ", errors));
    }

    private static void ShowDatabaseStartupError(Exception ex, string connStr, string label, string? probeDiagnostic = null)
    {
        var shortConn = MaskConnStr(connStr);
        var combinedErrText = ex.Message + " " + ex.InnerException?.Message;
        var isTenantNotFound = combinedErrText.Contains("tenant", StringComparison.OrdinalIgnoreCase)
                               || combinedErrText.Contains("ENOTFOUND", StringComparison.OrdinalIgnoreCase);
        var isSocketNoData = combinedErrText.Contains("aucune donnée du type requise", StringComparison.OrdinalIgnoreCase)
                            || combinedErrText.Contains("no such host", StringComparison.OrdinalIgnoreCase)
                            || combinedErrText.Contains("Socket", StringComparison.OrdinalIgnoreCase)
                            || combinedErrText.Contains("WSANO_DATA", StringComparison.OrdinalIgnoreCase)
                            || combinedErrText.Contains("TemporaryFailure", StringComparison.OrdinalIgnoreCase);
        var isPasswordAuth = combinedErrText.Contains("password authentication", StringComparison.OrdinalIgnoreCase)
                            || combinedErrText.Contains("mot de passe", StringComparison.OrdinalIgnoreCase);

        string hint;
        if (isTenantNotFound)
        {
            hint =
                "🐛 Cause : Le Pooler Supavisor (aws-0 / aws-1) ne reconnaît PAS le couple tenant + mot de passe.\n" +
                "   C'est quasi-systématiquement un problème de synchronisation mot-de-passe <-> pooler chez Supabase.\n\n" +
                "👉 À faire IMPÉRATIVEMENT dans cet ordre :\n" +
                "   1. Dashboard Supabase → Project Settings → Database.\n" +
                "   2. Section « Database password » → bouton « Reset database password ».\n" +
                "      ⚠️ Choisissez un mdp SANS le caractère « ; » (séparateur de la connection string).\n" +
                "   3. ATTENDEZ 1 à 2 MINUTES (le pooler Supavisor a un cache sur les secrets).\n" +
                "   4. Bouton vert « Connect » en haut à droite → Connection string → onglet « C# / ADO.NET ».\n" +
                "   5. Choisissez « Session pooler » (PORT 5432 — IPv4, compatible tous réseaux) et « Copy ».\n" +
                "   6. Mettez à jour le fichier « .env » À DEUX ENDROITS :\n" +
                "        • GesAchats.WPF\\.env\n" +
                "        • GesAchats.WPF\\bin\\Debug\\net10.0-windows\\.env\n" +
                "      SUPABASE_HOST     = aws-X-eu-west-2.pooler.supabase.com\n" +
                "      SUPABASE_PORT     = 5432\n" +
                "      SUPABASE_USERNAME = postgres.ukntkgwtkspyjzrnlmxg\n" +
                "      SUPABASE_PASSWORD = <le NOUVEAU mot de passe (étape 2)>\n\n" +
                "   7. Relancez l'application. Si ça échoue encore, attendez 60s de plus (propagation Supabase).";
        }
        else if (isPasswordAuth)
        {
            hint =
                "🐛 Cause : Échec « password authentication failed » avec un rôle Postgres valide.\n\n" +
                "👉 À faire :\n" +
                "   • Réinitialisez le mot de passe DB dans Supabase Dashboard → Settings → Database.\n" +
                "   • Vérifiez que SUPABASE_PASSWORD dans le .env ne contient pas de « ; » ou d'espace trailing.\n" +
                "   • Attendez 1-2 min après le reset (délai pooler).";
        }
        else if (isSocketNoData)
        {
            hint =
                "🐛 Cause la plus fréquente : réseau IPv4-only + connexion directe db.*.supabase.co (IPv6-only).\n" +
                "   Sinon : DNS / firewall d'entreprise / proxy / VPN qui bloque le pooler.\n\n" +
                "👉 À faire :\n" +
                "   1. Dans Dashboard Supabase → Connect → Connection string → prenez « Session pooler » (IPv4).\n" +
                "   2. Vérifiez les règles firewall : trafic sortant TCP vers aws-0-eu-west-2.pooler.supabase.com:5432\n" +
                "      et aws-1-eu-west-2.pooler.supabase.com:5432 / :6543.\n" +
                "   3. Si VPN / proxy / réseau d'entreprise : testez depuis une connexion 4G/5G pour isoler.";
        }
        else
        {
            hint =
                "👉 Conseils génériques :\n" +
                "   • Vérifiez SUPABASE_HOST / PORT / USERNAME / PASSWORD dans le .env.\n" +
                "   • Reset du mot de passe DB dans Dashboard Supabase → Settings → Database.\n" +
                "   • Les 2 fichiers .env (source + bin\\…) doivent être à jour.";
        }

        var msg =
            $"Erreur lors de l'initialisation de la base de données.\n\n" +
            $"Endpoint utilisé  : {label}\n" +
            $"Connection string : {shortConn}\n\n" +
            $"Type    : {ex.GetType().Name}\n" +
            $"Message : {ex.Message}\n" +
            $"Inner   : {ex.InnerException?.Message}\n" +
            $"------------------------------------------------------------------\n" +
            $"{hint}\n" +
            $"------------------------------------------------------------------\n";

        if (!string.IsNullOrWhiteSpace(probeDiagnostic) && !probeDiagnostic.StartsWith("OK via", StringComparison.OrdinalIgnoreCase))
        {
            msg += $"\n📋 RÉSULTAT DES 6 SONDAGES (avant initialisation) :\n{probeDiagnostic}\n\n";
        }

        msg += $"Stack trace :\n{ex.StackTrace}";

        MessageBox.Show(msg, "Erreur de Base de Données (Supabase / PostgreSQL)",
            MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private static string Truncate(string s, int n) => string.IsNullOrEmpty(s) ? "" : s.Length <= n ? s : s.Substring(0, n) + "…";

    private static string MaskConnStr(string cs)
    {
        if (string.IsNullOrEmpty(cs)) return "(vide)";
        try
        {
            var cb = new NpgsqlConnectionStringBuilder(cs);
            return $"Host={cb.Host};Port={cb.Port};Database={cb.Database};Username={cb.Username};Password=***;SslMode={cb.SslMode}";
        }
        catch
        {
            return "(non affichable)";
        }
    }

    private void ConfigureServices(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<GesAchatsDbContext>(options =>
            options.UseNpgsql(connectionString),
            ServiceLifetime.Transient);

        // Configuration Smtp - valeurs issues uniquement du fichier .env
        var smtpSettings = new GesAchats.Core.Helpers.SmtpSettings
        {
            Host = EnvLoader.Get("SMTP_HOST") ?? string.Empty,
            Port = int.TryParse(EnvLoader.Get("SMTP_PORT"), out var smtpPort) ? smtpPort : 587,
            Username = EnvLoader.Get("SMTP_USERNAME") ?? string.Empty,
            Password = EnvLoader.Get("SMTP_PASSWORD") ?? string.Empty,
            FromEmail = EnvLoader.Get("SMTP_FROM_EMAIL") ?? string.Empty,
            FromName = EnvLoader.Get("SMTP_FROM_NAME") ?? string.Empty,
            UseStartTls = string.Equals(EnvLoader.Get("SMTP_USE_STARTTLS"), "true", StringComparison.OrdinalIgnoreCase)
        };
        services.AddSingleton(smtpSettings);

        // Injection des dépendances Data & Services
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddTransient<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IUserSession, UserSession>();
        services.AddSingleton<SupabaseAuthClient>();
        services.AddTransient<IAuthService, AuthService>();
        services.AddTransient<IStockService, StockService>();
        services.AddTransient<IDashboardService, DashboardService>();
        services.AddTransient<IComparativeAnalysisService, ComparativeAnalysisService>();
        services.AddTransient<IPriceAnalysisService, PriceAnalysisService>();
        services.AddTransient<INeedsAnalyticsService, NeedsAnalyticsService>();
        services.AddTransient<IPdfGeneratorService, PdfGeneratorService>();
        services.AddTransient<IQuotationPdfService, QuotationPdfService>();
        services.AddTransient<IEmailService, EmailService>();
        services.AddTransient<IEmailVerificationService, EmailVerificationService>();
        services.AddTransient<IConformityService, ConformityService>();
        services.AddTransient<IFileStorageService>(s => new FileStorageService(AppDomain.CurrentDomain.BaseDirectory));
        services.AddTransient<IInvoiceRepository, InvoiceRepository>();
        
        // Dashboard services
        services.AddTransient<IPurchaseOrderService, PurchaseOrderService>();
        services.AddTransient<INeedsService, NeedsService>();
        services.AddTransient<ISupplierService, SupplierService>();
        services.AddTransient<IInvoiceService, InvoiceService>();
        services.AddTransient<ISuiviFournisseurService, SuiviFournisseurService>();
        
        // Employee management services
        services.AddTransient<IEmployeeService, EmployeeService>();
        
        // Serilog - journalisation persistante dans Logs\ (aide au diagnostic des erreurs Auth/SMTP).
        var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        try
        {
            Directory.CreateDirectory(logDir);
        }
        catch
        {
            logDir = Path.Combine(Path.GetTempPath(), "GesAchats", "Logs");
            Directory.CreateDirectory(logDir);
        }

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(
                Path.Combine(logDir, "gesachats-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true)
            .CreateLogger();
        services.AddSingleton<Serilog.ILogger>(Log.Logger);

        // ViewModels - Focus Magasinier
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ResetPasswordViewModel>();
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
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.AdminHistoriqueSortiesViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.EmployeeManagementViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.SuiviFournisseursViewModel>();
        services.AddTransient<GesAchats.WPF.ViewModels.Admin.SituationFournisseurViewModel>();

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
        services.AddTransient<ResetPasswordWindow>();
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
         services.AddTransient<GesAchats.WPF.Views.Admin.StockExits.AdminHistoriqueSortiesPage>();
         services.AddTransient<GesAchats.WPF.Views.Admin.SuiviFournisseurs.SuiviFournisseursPage>();
         services.AddTransient<GesAchats.WPF.Views.Admin.SuiviFournisseurs.SituationFournisseurPage>();

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
