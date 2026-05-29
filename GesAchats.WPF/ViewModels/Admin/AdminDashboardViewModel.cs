using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.WPF.ViewModels.Base;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminDashboardViewModel : BaseViewModel
{
    private bool _showCharts = true;
    private string _totalAmount = "21,044.82 MAD";
    private int _paymentCount = 11;
    private int _supplierCount = 4;
    private string _averageAmount = "1,913.17 MAD";

    public bool ShowCharts
    {
        get => _showCharts;
        set => SetProperty(ref _showCharts, value);
    }

    public string TotalAmount
    {
        get => _totalAmount;
        set => SetProperty(ref _totalAmount, value);
    }

    public int PaymentCount
    {
        get => _paymentCount;
        set => SetProperty(ref _paymentCount, value);
    }

    public int SupplierCount
    {
        get => _supplierCount;
        set => SetProperty(ref _supplierCount, value);
    }

    public string AverageAmount
    {
        get => _averageAmount;
        set => SetProperty(ref _averageAmount, value);
    }

    public ISeries[] SupplierDistribution { get; set; }
    public ISeries[] PaymentsByDate { get; set; }
    public ISeries[] PaymentMethods { get; set; }
    public Axis[] XAxesPayments { get; set; }
    public Axis[] XAxesMethods { get; set; }

    public ICommand ToggleChartsCommand { get; }

    public AdminDashboardViewModel()
    {
        Title = "ESPACE ADMIN";
        ToggleChartsCommand = new RelayCommand(_ => ShowCharts = !ShowCharts);

        // Chart 1: Distribution par Fournisseur (Pie Chart)
        SupplierDistribution = new ISeries[]
        {
            new PieSeries<double> { Values = new double[] { 55 }, Name = "Cimenterie du Nord", Pushout = 2 },
            new PieSeries<double> { Values = new double[] { 34 }, Name = "sdgfs", Pushout = 2 },
            new PieSeries<double> { Values = new double[] { 8 }, Name = "Société Matériaux SA", Pushout = 2 },
            new PieSeries<double> { Values = new double[] { 3 }, Name = "Aciers Modernes", Pushout = 2 }
        };

        // Chart 2: Paiements par Date (Area/Line Chart)
        PaymentsByDate = new ISeries[]
        {
            new LineSeries<double>
            {
                Values = new double[] { 1000, 3500, 6000, 8500, 10000, 11000, 10500, 8000, 5000, 1500 },
                Name = "Paiements (MAD)",
                Fill = new SolidColorPaint(SKColors.DeepSkyBlue.WithAlpha(50)),
                Stroke = new SolidColorPaint(SKColors.DeepSkyBlue, 3),
                GeometrySize = 12,
                GeometryFill = new SolidColorPaint(SKColors.White),
                GeometryStroke = new SolidColorPaint(SKColors.DeepSkyBlue, 3),
                LineSmoothness = 1 // Smooth curve like in the image
            }
        };

        XAxesPayments = new Axis[]
        {
            new Axis { Labels = new string[] { "06/05", "07/05", "08/05", "09/05", "10/05", "11/05", "12/05", "13/05", "14/05", "15/05", "16/05", "17/05", "18/05", "19/05" } }
        };

        // Chart 3: Modes de Paiement (Bar Chart)
        PaymentMethods = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Values = new double[] { 8500, 10000, 1500 },
                Name = "Montant par Mode",
                Fill = new SolidColorPaint(SKColors.MidnightBlue),
                Padding = 20
            }
        };

        XAxesMethods = new Axis[]
        {
            new Axis 
            { 
                Labels = new string[] { "Virement", "Lettre d'échange", "Chèque" },
                LabelsRotation = -45
            }
        };
    }
}
