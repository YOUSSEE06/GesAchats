using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class PurchasePoint
{
    public DateTime Date { get; set; }
    public double Price { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public double Quantity { get; set; }
}

public class ProductStatsViewModel : BaseViewModel, INavigatable
{
    private readonly IPriceAnalysisService _priceService;
    private readonly INavigationService _navigationService;
    private Product? _product;

    private decimal _averagePrice;
    private decimal _minPrice;
    private decimal _maxPrice;
    private double _totalQuantity;
    private int _purchaseCount;

    private string _selectedPeriod = "Tout";
    private DateTime? _startDate;
    private DateTime? _endDate;
    private bool _isCustomDateEnabled;

    public List<string> Periods { get; } = new() { "7 derniers jours", "30 derniers jours", "Personnalisé", "Tout" };

    public string SelectedPeriod
    {
        get => _selectedPeriod;
        set
        {
            if (SetProperty(ref _selectedPeriod, value))
            {
                IsCustomDateEnabled = value == "Personnalisé";
                if (value != "Personnalisé")
                {
                    _ = LoadStatsAsync();
                }
            }
        }
    }

    public DateTime? StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value))
            {
                if (SelectedPeriod == "Personnalisé") _ = LoadStatsAsync();
            }
        }
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value))
            {
                if (SelectedPeriod == "Personnalisé") _ = LoadStatsAsync();
            }
        }
    }

    public bool IsCustomDateEnabled
    {
        get => _isCustomDateEnabled;
        set => SetProperty(ref _isCustomDateEnabled, value);
    }

    public Product? Product
    {
        get => _product;
        set => SetProperty(ref _product, value);
    }

    public decimal AveragePrice
    {
        get => _averagePrice;
        set => SetProperty(ref _averagePrice, value);
    }

    public decimal MinPrice
    {
        get => _minPrice;
        set => SetProperty(ref _minPrice, value);
    }

    public decimal MaxPrice
    {
        get => _maxPrice;
        set => SetProperty(ref _maxPrice, value);
    }

    public double TotalQuantity
    {
        get => _totalQuantity;
        set => SetProperty(ref _totalQuantity, value);
    }

    public int PurchaseCount
    {
        get => _purchaseCount;
        set => SetProperty(ref _purchaseCount, value);
    }

    public ObservableCollection<ISeries> Series { get; set; } = new();
    public ObservableCollection<Axis> XAxes { get; set; } = new();
    public ObservableCollection<Axis> YAxes { get; set; } = new();

    public ICommand BackCommand { get; }

    public ProductStatsViewModel(IPriceAnalysisService priceService, INavigationService navigationService)
    {
        _priceService = priceService;
        _navigationService = navigationService;
        Title = "Statistiques du Produit";

        BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("Historique"));

        XAxes.Add(new Axis
        {
            Labeler = value => 
            {
                try 
                {
                    return new DateTime((long)value).ToString("dd/MM/yyyy");
                }
                catch 
                {
                    return string.Empty;
                }
            },
            UnitWidth = TimeSpan.FromDays(1).Ticks,
            MinStep = TimeSpan.FromDays(1).Ticks,
            Name = "Date"
        });

        YAxes.Add(new Axis
        {
            Labeler = value => value.ToString("N2") + " MAD",
            Name = "Prix Unitaire HT"
        });
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is Product product)
        {
            Product = product;
            await LoadStatsAsync();
        }
    }

    private async Task LoadStatsAsync()
    {
        if (Product == null) return;

        IsBusy = true;
        try
        {
            var history = (await _priceService.GetPriceHistoryForProductAsync(Product.Id))
                .OrderBy(x => x.PurchaseOrder.OrderDate)
                .ToList();

            // Filtrage par période
            if (SelectedPeriod == "7 derniers jours")
            {
                var threshold = DateTime.Now.AddDays(-7);
                history = history.Where(x => x.PurchaseOrder.OrderDate >= threshold).ToList();
            }
            else if (SelectedPeriod == "30 derniers jours")
            {
                var threshold = DateTime.Now.AddDays(-30);
                history = history.Where(x => x.PurchaseOrder.OrderDate >= threshold).ToList();
            }
            else if (SelectedPeriod == "Personnalisé")
            {
                if (StartDate.HasValue)
                    history = history.Where(x => x.PurchaseOrder.OrderDate >= StartDate.Value).ToList();
                if (EndDate.HasValue)
                    history = history.Where(x => x.PurchaseOrder.OrderDate <= EndDate.Value).ToList();
            }

            if (!history.Any())
            {
                PurchaseCount = 0;
                AveragePrice = 0;
                MinPrice = 0;
                MaxPrice = 0;
                TotalQuantity = 0;
                Series.Clear();
                return;
            }

            PurchaseCount = history.Count;
            AveragePrice = history.Average(x => x.UnitPriceHT);
            MinPrice = history.Min(x => x.UnitPriceHT);
            MaxPrice = history.Max(x => x.UnitPriceHT);
            TotalQuantity = (double)history.Sum(x => x.Quantity);

            var values = history.Select(x => new PurchasePoint
            {
                Date = x.PurchaseOrder.OrderDate,
                Price = (double)x.UnitPriceHT,
                SupplierName = x.PurchaseOrder.Supplier?.CompanyName ?? "Inconnu",
                Quantity = (double)x.Quantity
            }).ToArray();

            Series.Clear();
            Series.Add(new LineSeries<PurchasePoint>
            {
                Values = values,
                Name = string.Empty,
                Mapping = (point, index) => new LiveChartsCore.Kernel.Coordinate(point.Date.Ticks, point.Price),
                YToolTipLabelFormatter = (chartPoint) => 
                    $"Prix: {chartPoint.Model?.Price:N2} MAD\r\n" +
                    $"Fournisseur: {chartPoint.Model?.SupplierName}\r\n" +
                    $"Quantité: {chartPoint.Model?.Quantity}",
                Fill = new SolidColorPaint(SKColors.AliceBlue.WithAlpha(100)),
                Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
                GeometrySize = 10,
                GeometryFill = new SolidColorPaint(SKColors.DeepSkyBlue),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 }
            });
        }
        finally
        {
            IsBusy = false;
        }
    }
}
