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
    private IEnumerable<ISeries> _paymentModePieSeries = [];

    [ObservableProperty]
    private Axis[] _xAxes = [];

    [ObservableProperty]
    private double _totalPaymentModeAmount;

    [ObservableProperty]
    private double _totalInvoicesCount;

    [ObservableProperty]
    private double _totalBlCount;

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
        TotalPaymentModeAmount = Data.PaymentModeDistribution.Sum(d => d.Value);
        TotalInvoicesCount = Data.InvoiceStatusDistribution.Sum(d => d.Value);
        TotalBlCount = Data.BlStatusDistribution.Sum(d => d.Value);

        // 1. Payment Mode Distribution (Doughnut)
        PaymentModePieSeries = Data.PaymentModeDistribution.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Value },
            Name = d.Label,
            InnerRadius = 55,
            ToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue:N2} MAD",
            Fill = d.Label.Contains("Virement") ? new SolidColorPaint(new SKColor(34, 197, 94)) :
                   d.Label.Contains("Chèque") || d.Label.Contains("Cheque") ? new SolidColorPaint(new SKColor(245, 158, 11)) :
                   d.Label.Contains("Lettre") ? new SolidColorPaint(new SKColor(59, 130, 246)) :
                   new SolidColorPaint(new SKColor(100, 116, 139))
        }).ToList();

        // 2. Invoice Status (Doughnut)
        InvoiceStatusSeries = Data.InvoiceStatusDistribution.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Value },
            Name = d.Label,
            InnerRadius = 55,
            ToolTipLabelFormatter = point => $"{point.Coordinate.PrimaryValue:N0}",
            Fill = d.Label == "Payée" ? new SolidColorPaint(new SKColor(16, 185, 129)) :
                   d.Label == "EnAttente" ? new SolidColorPaint(new SKColor(245, 158, 11)) :
                   d.Label == "Partiellement payée" ? new SolidColorPaint(new SKColor(139, 92, 246)) :
                   new SolidColorPaint(new SKColor(100, 116, 139))
        }).ToList();

        // 3. BL Status (Doughnut)
        BlStatusSeries = Data.BlStatusDistribution.Select(d => new PieSeries<double>
        {
            Values = new[] { d.Value },
            Name = d.Label,
            InnerRadius = 55,
            Fill = d.Label.Contains("Valide") || d.Label.Contains("Validé") ? new SolidColorPaint(new SKColor(16, 185, 129)) : 
                   d.Label.Contains("EnAttente") || d.Label.Contains("En attente") ? new SolidColorPaint(new SKColor(245, 158, 11)) :
                   d.Label.Contains("FullyReceived") || d.Label.Contains("Reçu") ? new SolidColorPaint(new SKColor(59, 130, 246)) : 
                   new SolidColorPaint(new SKColor(100, 116, 139))
        }).ToList();

        // 4. Evolution (Line)
        PaymentEvolutionSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = Data.PaymentEvolution.Select(d => d.Value).ToArray(),
                Name = "Total Paiements",
                Stroke = new SolidColorPaint(new SKColor(37, 99, 235)) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(new SKColor(37, 99, 235, 30)),
                GeometrySize = 8,
                GeometryFill = new SolidColorPaint(new SKColor(37, 99, 235)),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                LineSmoothness = 0.5
            },
            new LineSeries<double>
            {
                Values = Data.BalanceEvolution.Select(d => d.Value).ToArray(),
                Name = "Total Soldes",
                Stroke = new SolidColorPaint(new SKColor(239, 68, 68)) { StrokeThickness = 3 },
                Fill = new SolidColorPaint(new SKColor(239, 68, 68, 30)),
                GeometrySize = 8,
                GeometryFill = new SolidColorPaint(new SKColor(239, 68, 68)),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                LineSmoothness = 0.5
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
                LabelsRotation = 0,
                SeparatorsPaint = new SolidColorPaint(new SKColor(229, 231, 235)),
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(new SKColor(100, 116, 139))
            }
        };


    }
}
