using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels;
using GesAchats.WPF.ViewModels.Base;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using FontAwesome.Sharp;

namespace GesAchats.WPF.ViewModels.Magasinier;

public partial class MagasinierDashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private MagasinierDashboardStats _stats = new();

    // Filtre de période (Date début → Date fin)
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    // Tendances KPI (texte + couleur)
    [ObservableProperty]
    private string _totalArticlesTrendText = string.Empty;
    [ObservableProperty]
    private bool _totalArticlesIsPositiveTrend = true;
    [ObservableProperty]
    private string _stockNormalTrendText = string.Empty;
    [ObservableProperty]
    private bool _stockNormalIsPositiveTrend = true;
    [ObservableProperty]
    private string _stockSousMinimumTrendText = string.Empty;
    [ObservableProperty]
    private bool _stockSousMinimumIsPositiveTrend = true;
    [ObservableProperty]
    private string _stockEnRuptureTrendText = string.Empty;
    [ObservableProperty]
    private bool _stockEnRuptureIsPositiveTrend = true;
    [ObservableProperty]
    private string _blEnAttenteTrendText = string.Empty;
    [ObservableProperty]
    private bool _blEnAttenteIsPositiveTrend = true;
    [ObservableProperty]
    private string _blValidesTrendText = string.Empty;
    [ObservableProperty]
    private bool _blValidesIsPositiveTrend = true;
    [ObservableProperty]
    private string _besoinsEnCoursTrendText = string.Empty;
    [ObservableProperty]
    private bool _besoinsEnCoursIsPositiveTrend = true;
    [ObservableProperty]
    private string _besoinsTransmisTrendText = string.Empty;
    [ObservableProperty]
    private bool _besoinsTransmisIsPositiveTrend = true;
    [ObservableProperty]
    private string _bcEnAttenteTrendText = string.Empty;
    [ObservableProperty]
    private bool _bcEnAttenteIsPositiveTrend = true;
    [ObservableProperty]
    private string _bcValidesTrendText = string.Empty;
    [ObservableProperty]
    private bool _bcValidesIsPositiveTrend = true;

    [ObservableProperty]
    private PurchaseOrderChartViewModel _purchaseOrderChartViewModel;

    // Charts Properties
    public ISeries[] StockStatusSeries { get; set; } = [];
    public ISeries[] BcStatusSeries { get; set; } = [];
    public Axis[] BcStatusXAxes { get; set; } = [];
    public ISeries[] StockMovementSeries { get; set; } = [];
    public Axis[] StockMovementXAxes { get; set; } = [];

    [ObservableProperty]
    private int _selectedPeriod = 7;

    public MagasinierDashboardViewModel(IDashboardService dashboardService, INavigationService navigationService)
    {
        _dashboardService = dashboardService;
        _navigationService = navigationService;
        
        PurchaseOrderChartViewModel = new PurchaseOrderChartViewModel();
        
        LoadDataCommand = new AsyncRelayCommand(() => LoadDataAsync());
        RefreshCommand = new AsyncRelayCommand(() => LoadDataAsync());
        ChangePeriodCommand = new AsyncRelayCommand<string>(async (p) => 
        {
            if (int.TryParse(p, out int days))
            {
                SelectedPeriod = days;
                StartDate = EndDate.AddDays(-days);
                await LoadDataAsync();
            }
        });
        
        _ = LoadDataAsync();
    }

    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public IAsyncRelayCommand<string> ChangePeriodCommand { get; }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var dto = await _dashboardService.GetMagasinierDashboardStatsAsync(StartDate, EndDate);
            Stats = MapToViewModelStats(dto);
            ApplyVariations(dto);
            UpdateCharts();
        }
        catch (Exception)
        {
            // Log error
        }
        finally
        {
            IsBusy = false;
        }
    }

    // Applique les pourcentages d'évolution aux KPI (période actuelle vs précédente)
    private void ApplyVariations(DashboardStatsDto dto)
    {
        TotalArticlesTrendText = BaseViewModel.FormatTrend(dto.TotalArticlesVariation, out bool totalPos);
        TotalArticlesIsPositiveTrend = totalPos;
        StockNormalTrendText = BaseViewModel.FormatTrend(dto.StockNormalVariation, out bool normalPos);
        StockNormalIsPositiveTrend = normalPos;
        StockSousMinimumTrendText = BaseViewModel.FormatTrend(dto.StockSousMinimumVariation, out bool sousMinPos);
        StockSousMinimumIsPositiveTrend = sousMinPos;
        StockEnRuptureTrendText = BaseViewModel.FormatTrend(dto.StockEnRuptureVariation, out bool rupturePos);
        StockEnRuptureIsPositiveTrend = rupturePos;
        BlEnAttenteTrendText = BaseViewModel.FormatTrend(dto.BlEnAttenteVariation, out bool blAttentePos);
        BlEnAttenteIsPositiveTrend = blAttentePos;
        BlValidesTrendText = BaseViewModel.FormatTrend(dto.BlValidesVariation, out bool blValidesPos);
        BlValidesIsPositiveTrend = blValidesPos;
        BesoinsEnCoursTrendText = BaseViewModel.FormatTrend(dto.BesoinsEnCoursVariation, out bool besoinsCoursPos);
        BesoinsEnCoursIsPositiveTrend = besoinsCoursPos;
        BesoinsTransmisTrendText = BaseViewModel.FormatTrend(dto.BesoinsTransmisVariation, out bool besoinsTransmisPos);
        BesoinsTransmisIsPositiveTrend = besoinsTransmisPos;
        BcEnAttenteTrendText = BaseViewModel.FormatTrend(dto.BcEnAttenteVariation, out bool bcAttentePos);
        BcEnAttenteIsPositiveTrend = bcAttentePos;
        BcValidesTrendText = BaseViewModel.FormatTrend(dto.BcValidesVariation, out bool bcValidesPos);
        BcValidesIsPositiveTrend = bcValidesPos;
    }

    private MagasinierDashboardStats MapToViewModelStats(DashboardStatsDto dto)
    {
        var vmStats = new MagasinierDashboardStats
        {
            TotalArticles = dto.TotalArticles,
            StockNormalCount = dto.StockNormalCount,
            StockSousMinimumCount = dto.StockSousMinimumCount,
            StockEnRuptureCount = dto.StockEnRuptureCount,
            BlEnAttenteCount = dto.BlEnAttenteCount,
            BlValidesCount = dto.BlValidesCount,
            BesoinsEnCoursCount = dto.BesoinsEnCoursCount,
            BesoinsTransmisCount = dto.BesoinsTransmisCount,
            BcEnAttenteCount = dto.BcEnAttenteCount,
            BcValidesCount = dto.BcValidesCount,
            CriticalProducts = dto.CriticalProducts,
            StockMovements = dto.StockMovements
        };

        // Combiner les BL, BC et Besoins dans les dernières opérations
        var operations = new List<RecentOperationViewModel>();

        foreach (var bl in dto.RecentBls)
        {
            operations.Add(new RecentOperationViewModel
            {
                Date = bl.Date,
                Reference = bl.Number,
                TypeIcon = IconChar.FileInvoice,
                TypeColor = new SolidColorBrush(Colors.MediumSeaGreen),
                TypeText = "Bon de Livraison",
                MovementIcon = IconChar.ArrowUp,
                MovementColor = new SolidColorBrush(Colors.MediumSeaGreen),
                MovementText = "Entrée",
                Supplier = bl.Supplier,
                CriticalArticle = bl.FirstArticle,
                Status = bl.Status,
                StatusColor = bl.Status == "Validé" ? new SolidColorBrush(Colors.MediumSeaGreen) : new SolidColorBrush(Colors.Orange)
            });
        }

        foreach (var bc in dto.RecentBcs)
        {
            operations.Add(new RecentOperationViewModel
            {
                Date = bc.Date,
                Reference = bc.Number,
                TypeIcon = IconChar.CartShopping,
                TypeColor = new SolidColorBrush(Colors.Orange),
                TypeText = "Bon de Commande",
                MovementIcon = IconChar.ArrowRight,
                MovementColor = new SolidColorBrush(Colors.Orange),
                MovementText = "Commande",
                Supplier = bc.Supplier,
                CriticalArticle = bc.FirstArticle,
                Status = bc.Status,
                StatusColor = bc.Status switch
                {
                    "Validé" => new SolidColorBrush(Colors.MediumSeaGreen),
                    "Annulé" => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Orange)
                }
            });
        }

        foreach (var besoin in dto.RecentNeeds)
        {
            operations.Add(new RecentOperationViewModel
            {
                Date = besoin.Date,
                Reference = besoin.Number,
                TypeIcon = IconChar.ClipboardList,
                TypeColor = new SolidColorBrush(Colors.Purple),
                TypeText = "Besoin",
                MovementIcon = IconChar.ArrowUp,
                MovementColor = new SolidColorBrush(Colors.Purple),
                MovementText = "Besoin",
                Status = besoin.Status,
                StatusColor = besoin.Status switch
                {
                    "Transmis" => new SolidColorBrush(Colors.MediumSeaGreen),
                    "Annulé" => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Orange)
                }
            });
        }

        vmStats.RecentOperations = operations.OrderByDescending(o => o.Date).Take(5).ToList();
        return vmStats;
    }

    private void UpdateCharts()
    {
        // 1. État du stock (Donut)
        StockStatusSeries = new ISeries[]
        {
            new PieSeries<int> { Values = new[] { Stats.StockNormalCount }, Name = "Normal", Fill = new SolidColorPaint(new SKColor(16, 185, 129)), InnerRadius = 55 },
            new PieSeries<int> { Values = new[] { Stats.StockSousMinimumCount }, Name = "Sous minimum", Fill = new SolidColorPaint(new SKColor(245, 158, 11)), InnerRadius = 55 },
            new PieSeries<int> { Values = new[] { Stats.StockEnRuptureCount }, Name = "En rupture", Fill = new SolidColorPaint(new SKColor(239, 68, 68)), InnerRadius = 55 }
        };

        // 2. Bons de commande (Bar) - Uniquement 2 barres comme demandé
        BcStatusSeries = new ISeries[]
        {
            new ColumnSeries<int> 
            { 
                Values = new[] { Stats.BcEnAttenteCount }, 
                Name = "BC en attente", 
                Fill = new SolidColorPaint(new SKColor(255, 138, 0)),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
            },
            new ColumnSeries<int> 
            { 
                Values = new[] { Stats.BcValidesCount }, 
                Name = "BC validés", 
                Fill = new SolidColorPaint(new SKColor(34, 197, 94)),
                DataLabelsPaint = new SolidColorPaint(SKColors.Black),
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top
            }
        };

        BcStatusXAxes = new Axis[]
        {
            new Axis
            {
                Labels = new[] { "BC en attente", "BC validés" },
                LabelsRotation = 0
            }
        };

        // 3. Mouvements de stock (Line) - Styled like Acheteur's chart
        StockMovementSeries = new ISeries[]
        {
            new LineSeries<decimal> 
            { 
                Values = Stats.StockMovements.Select(m => m.In).ToArray(), 
                Name = "Mouvement de stock d'entrée", 
                Stroke = new SolidColorPaint(new SKColor(37, 99, 235)) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(new SKColor(37, 99, 235, 30)),
                GeometrySize = 8
            },
            new LineSeries<decimal> 
            { 
                Values = Stats.StockMovements.Select(m => m.Out).ToArray(), 
                Name = "Mouvement de stock de sortie", 
                Stroke = new SolidColorPaint(new SKColor(239, 68, 68)) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(new SKColor(239, 68, 68, 30)),
                GeometrySize = 8
            }
        };

        StockMovementXAxes = new Axis[]
        {
            new Axis
            {
                Labels = Stats.StockMovements.Select(m => m.Date.ToString("dd/MM")).ToArray(),
                LabelsRotation = 0,
                SeparatorsPaint = new SolidColorPaint(new SKColor(229, 231, 235))
            }
        };
        
        // 4. Mettre à jour le contrôle personnalisé des bons de commande
        int maxValue = Math.Max(Stats.BcEnAttenteCount, Stats.BcValidesCount);
        maxValue = maxValue == 0 ? 10 : maxValue + 10; // Ajouter un peu de marge
        double chartHeight = 200;
        
        PurchaseOrderChartViewModel.ChartData = new ObservableCollection<ChartBarData>
        {
            new ChartBarData
            {
                Label = "BC en attente",
                Value = Stats.BcEnAttenteCount,
                BarHeight = (Stats.BcEnAttenteCount / (double)maxValue) * chartHeight,
                BarColor = new SolidColorBrush(Color.FromArgb(255, 255, 157, 92))
            },
            new ChartBarData
            {
                Label = "BC validés",
                Value = Stats.BcValidesCount,
                BarHeight = (Stats.BcValidesCount / (double)maxValue) * chartHeight,
                BarColor = new SolidColorBrush(Color.FromArgb(255, 16, 185, 129))
            }
        };

        OnPropertyChanged(nameof(StockStatusSeries));
        OnPropertyChanged(nameof(BcStatusSeries));
        OnPropertyChanged(nameof(BcStatusXAxes));
        OnPropertyChanged(nameof(StockMovementSeries));
        OnPropertyChanged(nameof(StockMovementXAxes));
    }
}

public class MagasinierDashboardStats
{
    public int TotalArticles { get; set; }
    public int StockNormalCount { get; set; }
    public int StockSousMinimumCount { get; set; }
    public int StockEnRuptureCount { get; set; }
    public int BlEnAttenteCount { get; set; }
    public int BlValidesCount { get; set; }
    public int BesoinsEnCoursCount { get; set; }
    public int BesoinsTransmisCount { get; set; }
    public int BcEnAttenteCount { get; set; }
    public int BcValidesCount { get; set; }
    public List<CriticalProductDto> CriticalProducts { get; set; } = new();
    public List<RecentOperationViewModel> RecentOperations { get; set; } = new();
    public List<StockMovementDto> StockMovements { get; set; } = new();
}

public class RecentOperationViewModel
{
    public DateTime Date { get; set; }
    public string Reference { get; set; } = string.Empty;
    public IconChar TypeIcon { get; set; }
    public Brush TypeColor { get; set; } = Brushes.Gray;
    public string TypeText { get; set; } = string.Empty;
    public IconChar MovementIcon { get; set; }
    public Brush MovementColor { get; set; } = Brushes.Gray;
    public string MovementText { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string CriticalArticle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Brush StatusColor { get; set; } = Brushes.Gray;
}
