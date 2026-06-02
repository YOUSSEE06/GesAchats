using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GesAchats.Core.DTOs;
using GesAchats.Core.Interfaces;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GesAchats.WPF.ViewModels.Admin;

public partial class AdminDashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private AdminDashboardDto _data = new();

    // KPI Trend Properties
    [ObservableProperty]
    private string _produitsTrend = "";
    [ObservableProperty]
    private bool _produitsIsPositive = true;

    [ObservableProperty]
    private string _stockTotalTrend = "";
    [ObservableProperty]
    private bool _stockTotalIsPositive = true;

    [ObservableProperty]
    private string _besoinsTrend = "";
    [ObservableProperty]
    private bool _besoinsIsPositive = true;

    [ObservableProperty]
    private string _sortiesStockTrend = "";
    [ObservableProperty]
    private bool _sortiesStockIsPositive = true;

    [ObservableProperty]
    private string _bonsLivraisonTrend = "";
    [ObservableProperty]
    private bool _bonsLivraisonIsPositive = true;

    [ObservableProperty]
    private string _bonsCommandeTrend = "";
    [ObservableProperty]
    private bool _bonsCommandeIsPositive = true;

    [ObservableProperty]
    private string _facturesTotalTrend = "";
    [ObservableProperty]
    private bool _facturesTotalIsPositive = true;

    [ObservableProperty]
    private string _facturesPayeesTrend = "";
    [ObservableProperty]
    private bool _facturesPayeesIsPositive = true;

    [ObservableProperty]
    private string _facturesEnAttenteTrend = "";
    [ObservableProperty]
    private bool _facturesEnAttenteIsPositive = true;

    [ObservableProperty]
    private string _totalRegleTrend = "";
    [ObservableProperty]
    private bool _totalRegleIsPositive = true;

    [ObservableProperty]
    private string _soldeTotalTrend = "";
    [ObservableProperty]
    private bool _soldeTotalIsPositive = true;

    [ObservableProperty]
    private string _fournisseursActifsTrend = "";
    [ObservableProperty]
    private bool _fournisseursActifsIsPositive = true;

    // Footer
    [ObservableProperty]
    private DateTime _lastUpdated = DateTime.Now;

    // Chart Series
    [ObservableProperty]
    private IEnumerable<ISeries> _operationDistributionSeries = [];

    [ObservableProperty]
    private IEnumerable<ISeries> _stockStatusSeries = [];

    [ObservableProperty]
    private IEnumerable<ISeries> _invoiceStatusSeries = [];

    [ObservableProperty]
    private IEnumerable<ISeries> _monthlyMovementsSeries = [];

    [ObservableProperty]
    private IEnumerable<ISeries> _paymentMethodSeries = [];

    [ObservableProperty]
    private Axis[] _xAxes = [];

    [ObservableProperty]
    private Axis[] _monthlyXAxes = [];

    [ObservableProperty]
    private double _totalStockStatusCount;

    [ObservableProperty]
    private double _totalInvoiceStatusCount;

    [ObservableProperty]
    private double _totalPaymentMethodAmount;

    public AdminDashboardViewModel(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
        RefreshCommand = new AsyncRelayCommand(LoadDataAsync);
        
        // Initial load
        _ = LoadDataAsync();
    }

    public IAsyncRelayCommand RefreshCommand { get; }

    private async Task LoadDataAsync()
    {
        if (IsBusy) return;
        
        IsBusy = true;
        try
        {
            Serilog.Log.Information("Calling GetAdminDashboardDataAsync...");
            Data = await _dashboardService.GetAdminDashboardDataAsync(StartDate, EndDate);
            Serilog.Log.Information($"AdminDashboardDto loaded: Produits.Value = {Data.Produits.Value}, StockTotal.Value = {Data.StockTotal.Value}");
            FormatTrends();
            UpdateCharts();
            LastUpdated = DateTime.Now;
            Serilog.Log.Information("Trends and Charts updated!");
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors du chargement du dashboard admin");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FormatTrends()
    {
        FormatSingleTrend(Data.Produits.VariationPercentage, out var pTrend, out var pPos);
        ProduitsTrend = pTrend; ProduitsIsPositive = pPos;
        
        FormatSingleTrend(Data.StockTotal.VariationPercentage, out var sTrend, out var sPos);
        StockTotalTrend = sTrend; StockTotalIsPositive = sPos;
        
        FormatSingleTrend(Data.Besoins.VariationPercentage, out var bTrend, out var bPos);
        BesoinsTrend = bTrend; BesoinsIsPositive = bPos;
        
        FormatSingleTrend(Data.SortiesDeStock.VariationPercentage, out var ssTrend, out var ssPos);
        SortiesStockTrend = ssTrend; SortiesStockIsPositive = ssPos;
        
        FormatSingleTrend(Data.BonsDeLivraison.VariationPercentage, out var blTrend, out var blPos);
        BonsLivraisonTrend = blTrend; BonsLivraisonIsPositive = blPos;
        
        FormatSingleTrend(Data.BonsDeCommande.VariationPercentage, out var bcTrend, out var bcPos);
        BonsCommandeTrend = bcTrend; BonsCommandeIsPositive = bcPos;
        
        FormatSingleTrend(Data.FacturesTotal.VariationPercentage, out var ftTrend, out var ftPos);
        FacturesTotalTrend = ftTrend; FacturesTotalIsPositive = ftPos;
        
        FormatSingleTrend(Data.FacturesPayees.VariationPercentage, out var fpTrend, out var fpPos);
        FacturesPayeesTrend = fpTrend; FacturesPayeesIsPositive = fpPos;
        
        FormatSingleTrend(Data.FacturesEnAttente.VariationPercentage, out var feaTrend, out var feaPos);
        FacturesEnAttenteTrend = feaTrend; FacturesEnAttenteIsPositive = feaPos;
        
        FormatSingleTrend(Data.TotalRegle.VariationPercentage, out var trTrend, out var trPos);
        TotalRegleTrend = trTrend; TotalRegleIsPositive = trPos;
        
        FormatSingleTrend(Data.SoldeTotal.VariationPercentage, out var solTrend, out var solPos);
        SoldeTotalTrend = solTrend; SoldeTotalIsPositive = solPos;
        
        FormatSingleTrend(Data.FournisseursActifs.VariationPercentage, out var faTrend, out var faPos);
        FournisseursActifsTrend = faTrend; FournisseursActifsIsPositive = faPos;
    }

    private void FormatSingleTrend(double percentage, out string trendText, out bool isPositive)
    {
        if (percentage > 0)
        {
            trendText = $"+{percentage:F1}%";
            isPositive = true;
        }
        else if (percentage < 0)
        {
            trendText = $"{percentage:F1}%";
            isPositive = false;
        }
        else
        {
            trendText = "0%";
            isPositive = true;
        }
    }

    private void UpdateCharts()
    {
        // Calculations for totals on charts
        TotalStockStatusCount = Data.StockStatus.Sum(d => d.Value);
        TotalInvoiceStatusCount = Data.InvoiceStatus.Sum(d => d.Value);
        TotalPaymentMethodAmount = Data.PaymentMethodDistribution.Sum(d => d.Value);

        // 1. Operation Distribution (Column Chart) - custom colors matching reference photo
        var barColors = new SKColor[]
        {
            new SKColor(59, 130, 246),   // Stock - blue
            new SKColor(168, 85, 247),   // Besoins - purple
            new SKColor(34, 197, 94),    // Sorties - green
            new SKColor(59, 130, 246),   // BL - blue
            new SKColor(249, 115, 22),   // BC - orange
            new SKColor(168, 85, 247),   // Factures - purple
            new SKColor(59, 130, 246)    // Paiements - blue
        };
        
        OperationDistributionSeries = Data.OperationDistribution.Select((d, i) => 
            new ColumnSeries<double>
            {
                Values = new double[] { d.Value },
                Name = d.Label,
                Fill = new SolidColorPaint(barColors[i % barColors.Length]),
                Padding = 20
            }
        ).ToArray();
        
        XAxes = new[]
        {
            new Axis
            {
                Labels = Data.OperationDistribution.Select(d => d.Label).ToArray(),
                LabelsRotation = 0,
                SeparatorsPaint = new SolidColorPaint(new SKColor(229, 231, 235)),
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(new SKColor(100, 116, 139))
            }
        };

        // 2. Stock Status (Doughnut Chart - same style as Comptable)
        StockStatusSeries = Data.StockStatus.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Value },
            Name = d.Label,
            InnerRadius = 55,
            Fill = d.Label switch
            {
                "OK" => new SolidColorPaint(new SKColor(34, 197, 94)),
                "Alerte" => new SolidColorPaint(new SKColor(245, 158, 11)),
                "Rupture" => new SolidColorPaint(new SKColor(239, 68, 68)),
                _ => new SolidColorPaint(SKColors.Gray)
            }
        }).ToList();

        // 3. Invoice Status (Doughnut Chart - same style as Comptable)
        InvoiceStatusSeries = Data.InvoiceStatus.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Value },
            Name = d.Label,
            InnerRadius = 55,
            Fill = d.Label switch
            {
                "Payée" => new SolidColorPaint(new SKColor(34, 197, 94)),
                "Partiellement payée" => new SolidColorPaint(new SKColor(168, 85, 246)),
                "En attente" => new SolidColorPaint(new SKColor(59, 130, 246)),
                _ => new SolidColorPaint(SKColors.Gray)
            }
        }).ToList();

        // 4. Monthly Movements (Line Chart)
        var incomingValues = Data.MonthlyIncoming.Select(m => m.Value).ToArray();
        var outgoingValues = Data.MonthlyOutgoing.Select(m => m.Value).ToArray();
        MonthlyMovementsSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<double>
            {
                Values = incomingValues,
                Name = "Entrées de stock",
                Fill = new SolidColorPaint(new SKColor(34, 197, 94, 30)),
                Stroke = new SolidColorPaint(new SKColor(34, 197, 94)) { StrokeThickness = 3 },
                GeometrySize = 8,
                GeometryFill = new SolidColorPaint(new SKColor(34, 197, 94)),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                LineSmoothness = 0.5
            },
            new LineSeries<double>
            {
                Values = outgoingValues,
                Name = "Sorties de stock",
                Fill = new SolidColorPaint(new SKColor(239, 68, 68, 30)),
                Stroke = new SolidColorPaint(new SKColor(239, 68, 68)) { StrokeThickness = 3 },
                GeometrySize = 8,
                GeometryFill = new SolidColorPaint(new SKColor(239, 68, 68)),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                LineSmoothness = 0.5
            }
        };
        MonthlyXAxes = new[]
        {
            new Axis
            {
                Labels = Data.MonthlyIncoming.Select(d => d.Date.ToString("MMM")).ToArray(),
                LabelsRotation = 0,
                SeparatorsPaint = new SolidColorPaint(new SKColor(229, 231, 235)),
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(new SKColor(100, 116, 139))
            }
        };

        // 5. Payment Method Distribution (Doughnut Chart - same style as Comptable)
        PaymentMethodSeries = Data.PaymentMethodDistribution.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Value },
            Name = d.Label,
            InnerRadius = 55,
            Fill = d.Label switch
            {
                "Virement" => new SolidColorPaint(new SKColor(34, 197, 94)),
                "Chèque" => new SolidColorPaint(new SKColor(245, 158, 11)),
                "Espèce" => new SolidColorPaint(new SKColor(168, 85, 246)),
                _ => new SolidColorPaint(SKColors.Gray)
            }
        }).ToList();
    }
}
