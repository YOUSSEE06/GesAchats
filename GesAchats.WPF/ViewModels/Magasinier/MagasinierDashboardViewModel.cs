using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GesAchats.WPF.ViewModels.Magasinier;

public partial class MagasinierDashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private DashboardStatsDto _stats = new();

    // Charts Properties
    public ISeries[] StockStatusSeries { get; set; } = [];
    public ISeries[] BlReceptionSeries { get; set; } = [];
    public ISeries[] BcStatusSeries { get; set; } = [];
    public ISeries[] StockMovementSeries { get; set; } = [];
    public Axis[] StockMovementXAxes { get; set; } = [];

    public MagasinierDashboardViewModel(IDashboardService dashboardService, INavigationService navigationService)
    {
        _dashboardService = dashboardService;
        _navigationService = navigationService;
        
        LoadDataCommand = new AsyncRelayCommand(() => LoadDataAsync(30));
        RefreshCommand = new AsyncRelayCommand(() => LoadDataAsync(30));
        
        // Initialisation des commandes d'action rapide
        NewProductCommand = new RelayCommand(() => _navigationService.NavigateTo("Stock"));
        AddBlCommand = new RelayCommand(() => _navigationService.NavigateTo("Livraisons"));
        ViewPendingBcCommand = new RelayCommand(() => _navigationService.NavigateTo("Orders", PurchaseOrderStatus.Pending));
        CreateNeedCommand = new RelayCommand(() => _navigationService.NavigateTo("Needs"));

        _ = LoadDataAsync(30);
    }

    public IAsyncRelayCommand LoadDataCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }
    public ICommand NewProductCommand { get; }
    public ICommand AddBlCommand { get; }
    public ICommand ViewPendingBcCommand { get; }
    public ICommand CreateNeedCommand { get; }

    private async Task LoadDataAsync(int days)
    {
        IsBusy = true;
        try
        {
            Stats = await _dashboardService.GetMagasinierDashboardStatsAsync(days);
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

    private void UpdateCharts()
    {
        // 1. État du stock (Donut)
        StockStatusSeries = new ISeries[]
        {
            new PieSeries<int> { Values = new[] { Stats.StockNormalCount }, Name = "Normal", Fill = new SolidColorPaint(SKColors.MediumSeaGreen) },
            new PieSeries<int> { Values = new[] { Stats.StockSousMinimumCount }, Name = "Sous minimum", Fill = new SolidColorPaint(SKColors.Orange) },
            new PieSeries<int> { Values = new[] { Stats.StockEnRuptureCount }, Name = "En rupture", Fill = new SolidColorPaint(SKColors.Crimson) }
        };

        // 2. Réceptions BL (Bar)
        BlReceptionSeries = new ISeries[]
        {
            new ColumnSeries<int> { Values = new[] { Stats.BlEnAttenteCount }, Name = "En attente", Fill = new SolidColorPaint(SKColors.Orange) },
            new ColumnSeries<int> { Values = new[] { Stats.BlValidesCount }, Name = "Validé", Fill = new SolidColorPaint(SKColors.MediumSeaGreen) }
        };

        // 3. Bons de commande (Bar)
        BcStatusSeries = new ISeries[]
        {
            new ColumnSeries<int> { Values = new[] { Stats.BcEnAttenteCount }, Name = "En attente", Fill = new SolidColorPaint(SKColors.Orange) },
            new ColumnSeries<int> { Values = new[] { Stats.BcValidesCount }, Name = "Validé", Fill = new SolidColorPaint(SKColors.MediumSeaGreen) }
        };

        // 4. Mouvements de stock (Line/Area)
        StockMovementSeries = new ISeries[]
        {
            new LineSeries<decimal> 
            { 
                Values = Stats.StockMovements.Select(m => m.In).ToArray(), 
                Name = "Entrées", 
                Stroke = new SolidColorPaint(SKColors.RoyalBlue) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(SKColors.RoyalBlue.WithAlpha(30)),
                GeometrySize = 8
            },
            new LineSeries<decimal> 
            { 
                Values = Stats.StockMovements.Select(m => m.Out).ToArray(), 
                Name = "Sorties", 
                Stroke = new SolidColorPaint(SKColors.Crimson) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(SKColors.Crimson.WithAlpha(30)),
                GeometrySize = 8
            }
        };

        StockMovementXAxes = new Axis[]
        {
            new Axis
            {
                Labels = Stats.StockMovements.Select(m => m.Date.ToString("dd/MM")).ToArray(),
                LabelsRotation = 0,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray.WithAlpha(50))
            }
        };

        OnPropertyChanged(nameof(StockStatusSeries));
        OnPropertyChanged(nameof(BlReceptionSeries));
        OnPropertyChanged(nameof(BcStatusSeries));
        OnPropertyChanged(nameof(StockMovementSeries));
        OnPropertyChanged(nameof(StockMovementXAxes));
    }
}
