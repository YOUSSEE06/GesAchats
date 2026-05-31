using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.Core.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Serilog;

namespace GesAchats.WPF.ViewModels.Acheteur
{
    public class AcheteurDashboardViewModel : BaseViewModel
    {
        private readonly IPurchaseOrderService _purchaseOrderService;
        private readonly INeedsService _needsService;
        private readonly ISupplierService _supplierService;
        private readonly IStockService _stockService;
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger _logger;

        // KPI Properties
        private int _demandesEnCours;
        private int _devisEnAttente;
        private int _devisValides;
        private int _bonsCommandeEnAttente;
        private int _fournisseursActifs;
        private int _articlesSuivis;

        // Comparaisons périodes précédentes (%)
        private double _demandesProgression;
        private double _devisAttentProgression;
        private double _devisValidesProgression;
        private double _bonsCommandeProgression;
        private double _fournisseursProgression;
        private double _articlesSuivisProgression;

        // Données pour les graphiques
        private ObservableCollection<PurchaseOrderData> _purchaseOrdersData;
        private ObservableCollection<SupplierExpenseData> _supplierExpensesData;
        private ObservableCollection<OrderStatusData> _orderStatusData;
        private ObservableCollection<ProductPriceAnalysisData> _priceAnalysisData;
        private ObservableCollection<OperationModel> _latestOperations;
        private ObservableCollection<AlertData> _alertsData;

        // LiveCharts Series
        private ISeries[] _purchaseOrdersSeries;
        private ISeries[] _supplierExpensesSeries;
        private ISeries[] _orderStatusSeries;
        private Axis[] _purchaseOrdersXAxes;

        // Filtres
        private int _selectedPeriodDays = 30;
        private DateTime _periodStartDate;
        private DateTime _periodEndDate;

        public AcheteurDashboardViewModel(
            IPurchaseOrderService purchaseOrderService,
            INeedsService needsService,
            ISupplierService supplierService,
            IStockService stockService,
            IInvoiceService invoiceService,
            ILogger logger)
        {
            _purchaseOrderService = purchaseOrderService;
            _needsService = needsService;
            _supplierService = supplierService;
            _stockService = stockService;
            _invoiceService = invoiceService;
            _logger = logger;

            // Initialiser les collections
                _purchaseOrdersData = new ObservableCollection<PurchaseOrderData>();
                _supplierExpensesData = new ObservableCollection<SupplierExpenseData>();
                _orderStatusData = new ObservableCollection<OrderStatusData>();
                _priceAnalysisData = new ObservableCollection<ProductPriceAnalysisData>();
                _latestOperations = new ObservableCollection<OperationModel>();
                _alertsData = new ObservableCollection<AlertData>();

            // Initialize LiveCharts fields to avoid nullable warnings
            _purchaseOrdersSeries = Array.Empty<ISeries>();
            _supplierExpensesSeries = Array.Empty<ISeries>();
            _orderStatusSeries = Array.Empty<ISeries>();
            _purchaseOrdersXAxes = Array.Empty<Axis>();

            // Initialiser les commandes
            RefreshCommand = new RelayCommand(async _ => await LoadData());
            ChangePeriodCommand = new RelayCommand(async _ => 
            {
                if (_ is int days)
                    await ChangePeriod(days);
            });

            // Charger les données au démarrage
            LoadData().ConfigureAwait(false);
        }

        // ===================== PROPRIÉTÉS KPI =====================

        public int DemandesEnCours
        {
            get => _demandesEnCours;
            set => SetProperty(ref _demandesEnCours, value);
        }

        public int DevisEnAttente
        {
            get => _devisEnAttente;
            set => SetProperty(ref _devisEnAttente, value);
        }

        public int DevisValides
        {
            get => _devisValides;
            set => SetProperty(ref _devisValides, value);
        }

        public int BonsCommandeEnAttente
        {
            get => _bonsCommandeEnAttente;
            set => SetProperty(ref _bonsCommandeEnAttente, value);
        }

        public int FournisseursActifs
        {
            get => _fournisseursActifs;
            set => SetProperty(ref _fournisseursActifs, value);
        }

        public int ArticlesSuivis
        {
            get => _articlesSuivis;
            set => SetProperty(ref _articlesSuivis, value);
        }

        // ===================== PROGRESSIONS (vs période précédente) =====================

        public double DemandesProgression
        {
            get => _demandesProgression;
            set => SetProperty(ref _demandesProgression, value);
        }

        public double DevisAttentProgression
        {
            get => _devisAttentProgression;
            set => SetProperty(ref _devisAttentProgression, value);
        }

        public double DevisValidesProgression
        {
            get => _devisValidesProgression;
            set => SetProperty(ref _devisValidesProgression, value);
        }

        public double BonsCommandeProgression
        {
            get => _bonsCommandeProgression;
            set => SetProperty(ref _bonsCommandeProgression, value);
        }

        public double FournisseursProgression
        {
            get => _fournisseursProgression;
            set => SetProperty(ref _fournisseursProgression, value);
        }

        public double ArticlesSuivisProgression
        {
            get => _articlesSuivisProgression;
            set => SetProperty(ref _articlesSuivisProgression, value);
        }

        // ===================== DONNÉES POUR GRAPHIQUES =====================

        public ObservableCollection<PurchaseOrderData> PurchaseOrdersData
        {
            get => _purchaseOrdersData;
            set => SetProperty(ref _purchaseOrdersData, value);
        }

        public ObservableCollection<SupplierExpenseData> SupplierExpensesData
        {
            get => _supplierExpensesData;
            set => SetProperty(ref _supplierExpensesData, value);
        }

        public ObservableCollection<OrderStatusData> OrderStatusData
        {
            get => _orderStatusData;
            set => SetProperty(ref _orderStatusData, value);
        }

        public ObservableCollection<ProductPriceAnalysisData> PriceAnalysisData
        {
            get => _priceAnalysisData;
            set => SetProperty(ref _priceAnalysisData, value);
        }

        public ObservableCollection<OperationModel> LatestOperations
        {
            get => _latestOperations;
            set => SetProperty(ref _latestOperations, value);
        }

        public ObservableCollection<AlertData> AlertsData
        {
            get => _alertsData;
            set => SetProperty(ref _alertsData, value);
        }

        // ===================== LIVE CHARTS PROPERTIES =====================

        public ISeries[] PurchaseOrdersSeries
        {
            get => _purchaseOrdersSeries;
            set => SetProperty(ref _purchaseOrdersSeries, value);
        }

        public ISeries[] SupplierExpensesSeries
        {
            get => _supplierExpensesSeries;
            set => SetProperty(ref _supplierExpensesSeries, value);
        }

        public ISeries[] OrderStatusSeries
        {
            get => _orderStatusSeries;
            set => SetProperty(ref _orderStatusSeries, value);
        }

        public Axis[] PurchaseOrdersXAxes
        {
            get => _purchaseOrdersXAxes;
            set => SetProperty(ref _purchaseOrdersXAxes, value);
        }

        // ===================== FILTRES & INDICATEURS =====================

        public int SelectedPeriodDays
        {
            get => _selectedPeriodDays;
            set => SetProperty(ref _selectedPeriodDays, value);
        }

        public DateTime PeriodStartDate
        {
            get => _periodStartDate;
            set => SetProperty(ref _periodStartDate, value);
        }

        public DateTime PeriodEndDate
        {
            get => _periodEndDate;
            set => SetProperty(ref _periodEndDate, value);
        }

        // ===================== COMMANDES =====================

        public ICommand RefreshCommand { get; }
        public ICommand ChangePeriodCommand { get; }

        // ===================== MÉTHODES DE CHARGEMENT =====================

        public async Task LoadData()
        {
            try
            {
                IsBusy = true;

                // Calculer les dates
                PeriodEndDate = DateTime.Now;
                PeriodStartDate = PeriodEndDate.AddDays(-SelectedPeriodDays);

                // Charger les KPIs
                await LoadKPIs();

                // Charger les données des graphiques
                await LoadChartData();

                // Charger les alertes
                await LoadAlerts();

                _logger.Information("Dashboard Acheteur chargé avec succès");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors du chargement du dashboard");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private async Task LoadKPIs()
        {
            // Exact sample data
            DemandesEnCours = 23;
            DevisEnAttente = 14;
            DevisValides = 38;
            BonsCommandeEnAttente = 7;
            FournisseursActifs = 26;
            ArticlesSuivis = 156;

            // Progression sample data
            DemandesProgression = 27.0;
            DevisAttentProgression = 12.0;
            DevisValidesProgression = 18.0;
            BonsCommandeProgression = 8.0;
            FournisseursProgression = 6.0;
            ArticlesSuivisProgression = 11.0;
        }

        private async Task LoadChartData()
        {
            // 1. Évolution des achats (derniers 6 mois)
            await LoadPurchaseOrdersEvolution();

            // 2. Dépenses par fournisseur (Top 5)
            await LoadSupplierExpenses();

            // 3. Répartition des bons de commande
            await LoadOrderStatus();

            // 4. Analyse des prix (Top Articles)
            await LoadPriceAnalysis();

            // 5. Dernières opérations
            await LoadRecentOperations();
        }

        private async Task LoadPurchaseOrdersEvolution()
        {
            try
            {
                // Exact sample data
                var sampleData = new[]
                {
                    new { Month = "Déc 2024", Total = 130000m },
                    new { Month = "Jan 2025", Total = 155000m },
                    new { Month = "Fév 2025", Total = 335000m },
                    new { Month = "Mar 2025", Total = 140000m },
                    new { Month = "Avr 2025", Total = 255000m },
                    new { Month = "Mai 2025", Total = 260000m },
                    new { Month = "Juin 2025", Total = 360000m },
                    new { Month = "Juil 2025", Total = 245000m },
                    new { Month = "Août 2025", Total = 455000m }
                };

                PurchaseOrdersData.Clear();
                foreach (var item in sampleData)
                {
                    PurchaseOrdersData.Add(new PurchaseOrderData
                    {
                        Month = item.Month,
                        Total = item.Total
                    });
                }

                // Setup LiveCharts for Purchase Orders Evolution with exact blue color
                PurchaseOrdersSeries = new ISeries[]
                {
                    new LineSeries<decimal>
                    {
                        Values = PurchaseOrdersData.Select(x => x.Total).ToArray(),
                        Name = "Achats (MAD)",
                        Stroke = new SolidColorPaint(new SKColor(37, 99, 235)) { StrokeThickness = 3 },
                        Fill = new SolidColorPaint(new SKColor(37, 99, 235, 30)),
                        GeometrySize = 8
                    }
                };

                PurchaseOrdersXAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = PurchaseOrdersData.Select(x => x.Month).ToArray(),
                        LabelsRotation = 0,
                        SeparatorsPaint = new SolidColorPaint(new SKColor(229, 231, 235))
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors du chargement de l'évolution des achats");
            }
        }

        private async Task LoadSupplierExpenses()
        {
            try
            {
                // Exact sample data
                var sampleData = new[]
                {
                    new { Name = "Société Matériaux SA", TotalExpense = 485000m },
                    new { Name = "Cimenterie du Nord", TotalExpense = 392000m },
                    new { Name = "Aciers Modernes", TotalExpense = 287000m },
                    new { Name = "Jean Stock", TotalExpense = 214000m },
                    new { Name = "Autres", TotalExpense = 176000m }
                };

                SupplierExpensesData.Clear();
                foreach (var item in sampleData)
                {
                    SupplierExpensesData.Add(new SupplierExpenseData
                    {
                        Name = item.Name,
                        TotalExpense = item.TotalExpense
                    });
                }

                // Setup LiveCharts for Supplier Expenses with exact blue color
                SupplierExpensesSeries = new ISeries[]
                {
                    new ColumnSeries<decimal>
                    {
                        Values = SupplierExpensesData.Select(x => x.TotalExpense).ToArray(),
                        Name = "Dépenses (MAD)",
                        Fill = new SolidColorPaint(new SKColor(37, 99, 235))
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors du chargement des dépenses par fournisseur");
            }
        }

        private async Task LoadOrderStatus()
        {
            try
            {
                // Exact sample data
                int pending = 7;
                int validated = 23;
                int cancelled = 8;

                OrderStatusData.Clear();
                OrderStatusData.Add(new OrderStatusData { Status = "En attente", Count = pending, Percentage = 18 });
                OrderStatusData.Add(new OrderStatusData { Status = "Validé", Count = validated, Percentage = 61 });
                OrderStatusData.Add(new OrderStatusData { Status = "Annulé", Count = cancelled, Percentage = 21 });

                // Setup LiveCharts for Order Status (Donut Chart) with exact colors
                OrderStatusSeries = new ISeries[]
                {
                    new PieSeries<int>
                    {
                        Values = new[] { pending },
                        Name = "En attente",
                        Fill = new SolidColorPaint(new SKColor(245, 158, 11)),
                        InnerRadius = 55
                    },
                    new PieSeries<int>
                    {
                        Values = new[] { validated },
                        Name = "Validé",
                        Fill = new SolidColorPaint(new SKColor(34, 197, 94)),
                        InnerRadius = 55
                    },
                    new PieSeries<int>
                    {
                        Values = new[] { cancelled },
                        Name = "Annulé",
                        Fill = new SolidColorPaint(new SKColor(37, 99, 235)),
                        InnerRadius = 55
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors du chargement du statut des commandes");
            }
        }

        private async Task LoadPriceAnalysis()
        {
            try
            {
                // Charger les articles avec l'analyse des prix
                var data = await _stockService.GetProductPriceAnalysisAsync(PeriodStartDate, PeriodEndDate, 10);

                PriceAnalysisData.Clear();
                foreach (var item in data)
                {
                    PriceAnalysisData.Add(new ProductPriceAnalysisData
                    {
                        Name = item.Name,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        PriceChangePercentage = item.PriceChangePercentage,
                        EvolutionText = item.EvolutionText
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors du chargement de l'analyse des prix");
            }
        }

        private async Task LoadRecentOperations()
        {
            try
            {
                LatestOperations.Clear();
                
                // Exact sample data
                LatestOperations.Add(new OperationModel
                {
                    Reference = "DEV-20260530-F4A0",
                    Type = "Devis",
                    Fournisseur = "sdfgs",
                    Date = "30/05/2026",
                    Montant = "330.00",
                    Statut = "En attente"
                });
                
                LatestOperations.Add(new OperationModel
                {
                    Reference = "DEV-20260530-D9A8",
                    Type = "Devis",
                    Fournisseur = "Société Matériaux SA",
                    Date = "30/05/2026",
                    Montant = "3,300.00",
                    Statut = "Validé"
                });
                
                LatestOperations.Add(new OperationModel
                {
                    Reference = "BC-2026-7499",
                    Type = "BC",
                    Fournisseur = "sdfgs",
                    Date = "30/05/2026",
                    Montant = "330.00",
                    Statut = "Validé"
                });
                
                LatestOperations.Add(new OperationModel
                {
                    Reference = "BC-2026-81AC",
                    Type = "BC",
                    Fournisseur = "Société Matériaux SA",
                    Date = "30/05/2026",
                    Montant = "18.00",
                    Statut = "Validé"
                });
                
                LatestOperations.Add(new OperationModel
                {
                    Reference = "BC-2026-01F8",
                    Type = "BC",
                    Fournisseur = "Société Matériaux SA",
                    Date = "30/05/2026",
                    Montant = "5,760.00",
                    Statut = "Validé"
                });
                
                LatestOperations.Add(new OperationModel
                {
                    Reference = "BC-2026-03C1",
                    Type = "BC",
                    Fournisseur = "Société Matériaux SA",
                    Date = "29/05/2026",
                    Montant = "6,534.00",
                    Statut = "En attente"
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors du chargement des dernières opérations");
            }
        }

        private async Task LoadAlerts()
        {
            try
            {
                AlertsData.Clear();

                // Alerte : Stock faible
                var lowStockCount = await _stockService.GetLowStockProductsCountAsync();
                if (lowStockCount > 0)
                {
                    AlertsData.Add(new AlertData
                    {
                        Type = "warning",
                        Title = "Stock faible",
                        Description = "Articles en dessous du seuil minimum",
                        Count = lowStockCount,
                        Icon = "⚠️"
                    });
                }

                // Alerte : Devis en retard
                var delayedQuotesCount = 5; // À adapter
                AlertsData.Add(new AlertData
                {
                    Type = "info",
                    Title = "Devis en retard",
                    Description = "Devis en attente depuis plus de 7 jours",
                    Count = delayedQuotesCount,
                    Icon = "⏰"
                });

                // Alerte : Bons de commande en attente
                var pendingOrdersCount = await _purchaseOrderService.GetPendingPurchaseOrdersCountAsync(DateTime.Now.AddDays(-7), DateTime.Now);
                AlertsData.Add(new AlertData
                {
                    Type = "info",
                    Title = "Bons de commande en attente",
                    Description = "À transmettre ou à confirmer",
                    Count = pendingOrdersCount,
                    Icon = "🛒"
                });

                // Alerte : Fournisseurs à évaluer
                AlertsData.Add(new AlertData
                {
                    Type = "info",
                    Title = "Fournisseurs à évaluer",
                    Description = "Dernière évaluation > 6 mois",
                    Count = 3,
                    Icon = "👥"
                });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors du chargement des alertes");
            }
        }

        private async Task<double> CalculateProgressionAsync(string type, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Calculer la progression vs période précédente
                var previousStartDate = startDate.AddDays(-(endDate - startDate).Days);
                var previousEndDate = startDate;

                int currentCount = 0;
                int previousCount = 0;

                if (type == "needs")
                {
                    currentCount = await _needsService.GetPendingNeedsCountAsync(startDate, endDate);
                    previousCount = await _needsService.GetPendingNeedsCountAsync(previousStartDate, previousEndDate);
                }
                else if (type == "purchaseOrders")
                {
                    currentCount = await _purchaseOrderService.GetPendingPurchaseOrdersCountAsync(startDate, endDate);
                    previousCount = await _purchaseOrderService.GetPendingPurchaseOrdersCountAsync(previousStartDate, previousEndDate);
                }

                if (previousCount == 0) return 0;
                return ((currentCount - previousCount) / (double)previousCount) * 100;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Erreur lors du calcul de progression");
                return 0;
            }
        }

        private async Task ChangePeriod(int days)
        {
            SelectedPeriodDays = days;
            await LoadData();
        }
    }

    // ===================== MODÈLES DE DONNÉES =====================

    public class PurchaseOrderData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal Total { get; set; }
        public decimal Average { get; set; }
    }

    public class SupplierExpenseData
    {
        public string Name { get; set; } = string.Empty;
        public decimal TotalExpense { get; set; }
        public double PercentageOfTotal { get; set; }
    }

    public class OrderStatusData
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class OperationModel
    {
        public string Reference { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Fournisseur { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Montant { get; set; } = string.Empty;
        public string Statut { get; set; } = string.Empty;
    }

    public class OperationData
    {
        public string Reference { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Supplier { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class AlertData
    {
        public string Type { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Icon { get; set; } = string.Empty;
    }
}
