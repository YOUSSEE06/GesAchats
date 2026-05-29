using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Admin;

public class ProductPriceSummaryViewModel : BaseViewModel
{
    public Product Product { get; }
    public string Name => Product.Designation;
    public string Category => Product.Category ?? "Général";
    
    private int _purchaseCount;
    public int PurchaseCount
    {
        get => _purchaseCount;
        set => SetProperty(ref _purchaseCount, value);
    }

    private decimal _averagePrice;
    public decimal AveragePrice
    {
        get => _averagePrice;
        set => SetProperty(ref _averagePrice, value);
    }

    public string PurchaseInfo => $"{PurchaseCount} achats";
    public string PriceInfo => $"Moy: {AveragePrice:N2} MAD";

    public ProductPriceSummaryViewModel(Product product)
    {
        Product = product;
    }
}

public class AdminPriceAnalysisViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPriceAnalysisService _priceAnalysisService;

    private ObservableCollection<ProductPriceSummaryViewModel> _products = new();
    public ObservableCollection<ProductPriceSummaryViewModel> Products
    {
        get => _products;
        set => SetProperty(ref _products, value);
    }

    private ProductPriceSummaryViewModel? _selectedProduct;
    public ProductPriceSummaryViewModel? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                _ = LoadProductHistory();
            }
        }
    }

    private ObservableCollection<PurchaseOrderDetail> _purchaseHistory = new();
    public ObservableCollection<PurchaseOrderDetail> PurchaseHistory
    {
        get => _purchaseHistory;
        set => SetProperty(ref _purchaseHistory, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand ViewStatsCommand { get; }

    public AdminPriceAnalysisViewModel(IUnitOfWork unitOfWork, IPriceAnalysisService priceAnalysisService)
    {
        _unitOfWork = unitOfWork;
        _priceAnalysisService = priceAnalysisService;
        Title = "Historique des Prix et Achats";

        RefreshCommand = new RelayCommand(async _ => await LoadData());
        ViewStatsCommand = new RelayCommand(_ => ExecuteViewStats());

        _ = LoadData();
    }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var allProducts = await _unitOfWork.Products.GetAllAsync();
            var summaries = new List<ProductPriceSummaryViewModel>();

            foreach (var p in allProducts.OrderBy(x => x.Designation))
            {
                var history = await _unitOfWork.PurchaseOrderDetails.GetHistoryByProductAsync(p.Id);
                var historyList = history.ToList();

                var summary = new ProductPriceSummaryViewModel(p)
                {
                    PurchaseCount = historyList.Count,
                    AveragePrice = historyList.Any() ? historyList.Average(h => h.UnitPriceHT) : 0
                };
                summaries.Add(summary);
            }

            Products = new ObservableCollection<ProductPriceSummaryViewModel>(summaries);
            
            if (Products.Any() && SelectedProduct == null)
            {
                SelectedProduct = Products.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors du chargement des prix : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadProductHistory()
    {
        if (SelectedProduct == null)
        {
            PurchaseHistory.Clear();
            return;
        }

        try
        {
            var history = await _priceAnalysisService.GetPriceHistoryForProductAsync(SelectedProduct.Product.Id);
            PurchaseHistory = new ObservableCollection<PurchaseOrderDetail>(history.OrderByDescending(h => h.PurchaseOrder.OrderDate));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors du chargement de l'historique : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteViewStats()
    {
        if (SelectedProduct == null) return;
        MessageBox.Show($"Statistiques détaillées pour {SelectedProduct.Name} à implémenter.", "Statistiques", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
