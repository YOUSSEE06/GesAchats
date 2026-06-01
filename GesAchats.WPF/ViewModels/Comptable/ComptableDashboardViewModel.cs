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

namespace GesAchats.WPF.ViewModels.Comptable;

public partial class ComptableDashboardViewModel : ObservableObject
{
    private readonly IDashboardService _dashboardService;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddMonths(-1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private ComptableDashboardDto _data = new();

    // Chart Series
    [ObservableProperty]
    private IEnumerable<ISeries> _supplierDistributionSeries = [];

    [ObservableProperty]
    private IEnumerable<ISeries> _invoiceStatusSeries = [];

    [ObservableProperty]
    private IEnumerable<ISeries> _blStatusSeries = [];

    [ObservableProperty]
    private IEnumerable<ISeries> _paymentEvolutionSeries = [];

    [ObservableProperty]
    private IEnumerable<ISeries> _paymentModeSeries = [];

    [ObservableProperty]
    private Axis[] _xAxes = [];

    public ComptableDashboardViewModel(IDashboardService dashboardService)
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
            Data = await _dashboardService.GetComptableDashboardStatsAsync(StartDate, EndDate);
            UpdateCharts();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors du chargement du dashboard comptable");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void UpdateCharts()
    {
        // 1. Supplier Distribution (Doughnut)
        SupplierDistributionSeries = Data.SupplierDistribution.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Value },
            Name = d.Label,
            InnerRadius = 60,
            ToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue:N2} €"
        }).ToList();

        // 2. Invoice Status (Doughnut)
        InvoiceStatusSeries = Data.InvoiceStatusDistribution.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Value },
            Name = d.Label,
            InnerRadius = 60
        }).ToList();

        // 3. BL Status (Doughnut)
        BlStatusSeries = Data.BlStatusDistribution.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Value },
            Name = d.Label,
            InnerRadius = 60
        }).ToList();

        // 4. Evolution (Line)
        PaymentEvolutionSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = Data.PaymentEvolution.Select(d => d.Value).ToArray(),
                Name = "Paiements",
                Fill = new SolidColorPaint(SKColors.Blue.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.Blue) { StrokeThickness = 3 },
                GeometrySize = 10,
                GeometryFill = new SolidColorPaint(SKColors.Blue),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
            },
            new LineSeries<double>
            {
                Values = Data.BalanceEvolution.Select(d => d.Value).ToArray(),
                Name = "Soldes",
                Fill = new SolidColorPaint(SKColors.Red.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.Red) { StrokeThickness = 3 },
                GeometrySize = 10,
                GeometryFill = new SolidColorPaint(SKColors.Red),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
            }
        };

        var allDates = Data.PaymentEvolution.Select(d => d.Date)
            .Union(Data.BalanceEvolution.Select(d => d.Date))
            .OrderBy(d => d)
            .Select(d => d.ToString("dd/MM"))
            .Distinct()
            .ToArray();

        XAxes = new[]
        {
            new Axis
            {
                Labels = allDates,
                LabelsRotation = 45,
                SeparatorsPaint = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 1 }
            }
        };

        // 5. Modes de Paiement (Bar)
        PaymentModeSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = Data.PaymentModeDistribution.Select(d => d.Value).ToArray(),
                Name = "Montant par mode",
                Fill = new SolidColorPaint(SKColors.Teal),
                Padding = 10
            }
        };
    }
}
