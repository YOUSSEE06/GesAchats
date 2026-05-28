using System.Collections.ObjectModel;
using System.Dynamic;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class QuotationSelectionViewModel : BaseViewModel
{
    private bool _isSelected = true;
    public Quotation Quotation { get; }
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public QuotationSelectionViewModel(Quotation quotation)
    {
        Quotation = quotation;
    }
}

public class AdvancedComparativeAnalysisViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private ObservableCollection<ExpandoObject> _comparisonData = new();
    private ObservableCollection<Quotation> _selectedSuppliers = new();

    public ObservableCollection<QuotationSelectionViewModel> AvailableQuotations { get; } = new();
    
    public ObservableCollection<ExpandoObject> ComparisonData
    {
        get => _comparisonData;
        set => SetProperty(ref _comparisonData, value);
    }

    public ObservableCollection<Quotation> SelectedSuppliers
    {
        get => _selectedSuppliers;
        set => SetProperty(ref _selectedSuppliers, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand CompareCommand { get; }
    public ICommand GenerateOrderCommand { get; }

    public AdvancedComparativeAnalysisViewModel(IUnitOfWork unitOfWork, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        Title = "Analyse Comparative Avancée";

        RefreshCommand = new RelayCommand(async _ => await LoadQuotations());
        CompareCommand = new RelayCommand(_ => ExecuteComparison());
        GenerateOrderCommand = new RelayCommand(p => ExecuteGenerateOrder(p as Quotation));

        _ = LoadQuotations();
    }

    private async Task LoadQuotations()
    {
        IsBusy = true;
        try
        {
            // Charger tous les devis avec leurs détails
            var allQuotes = await _unitOfWork.Quotations.GetAllWithAllRelatedAsync();
            var quotes = allQuotes.Where(q => q.Status == "Pending" || q.Status == "Sent" || q.Status == "Received");
            
            AvailableQuotations.Clear();
            ComparisonData.Clear();
            SelectedSuppliers.Clear();

            foreach (var q in quotes.OrderByDescending(x => x.Date))
            {
                // Charger les détails pour chaque devis pour avoir les produits
                var fullQ = await _unitOfWork.Quotations.GetWithDetailsAsync(q.Id);
                if (fullQ != null)
                {
                    AvailableQuotations.Add(new QuotationSelectionViewModel(fullQ) { IsSelected = false });
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void ExecuteComparison()
    {
        ComparisonData.Clear();
        SelectedSuppliers.Clear();

        var selectedQuotes = AvailableQuotations.Where(q => q.IsSelected).Select(q => q.Quotation).ToList();
        if (!selectedQuotes.Any()) return;

        foreach (var q in selectedQuotes) SelectedSuppliers.Add(q);

        // Récupérer tous les produits uniques à travers tous les devis sélectionnés
        var allProducts = selectedQuotes
            .SelectMany(q => q.Details)
            .GroupBy(d => d.ProductId)
            .Select(g => g.First().Product)
            .ToList();

        var newData = new ObservableCollection<ExpandoObject>();

        foreach (var product in allProducts)
        {
            if (product == null) continue;

            dynamic row = new ExpandoObject();
            row.ProductId = product.Id;
            row.ProductName = product.Designation;
            
            // Trouver la quantité demandée (on prend la max trouvée dans les devis ou on pourrait chercher dans le besoin lié)
            var quantity = selectedQuotes
                .SelectMany(q => q.Details)
                .Where(d => d.ProductId == product.Id)
                .Max(d => d.Quantity);

            row.Quantity = quantity;
            row.Unit = product.Unit ?? "U";

            decimal minPrice = decimal.MaxValue;
            int bestSupplierId = -1;

            foreach (var quote in selectedQuotes)
            {
                var detail = quote.Details.FirstOrDefault(d => d.ProductId == product.Id);
                decimal? price = detail?.UnitPriceHT;
                
                var supplierKey = $"Price_{quote.Id}";
                
                if (price.HasValue && price.Value > 0)
                {
                    ((IDictionary<string, object>)row)[supplierKey] = price.Value.ToString("N2") + " DH";
                    
                    if (price.Value < minPrice)
                    {
                        minPrice = price.Value;
                        bestSupplierId = quote.SupplierId;
                    }
                }
                else
                {
                    ((IDictionary<string, object>)row)[supplierKey] = "Non proposé";
                }
            }

            row.BestPrice = minPrice == decimal.MaxValue ? "N/A" : minPrice.ToString("N2") + " DH";
            row.BestSupplierId = bestSupplierId;
            
            newData.Add(row);
        }

        ComparisonData = newData;
    }

    private async void ExecuteGenerateOrder(Quotation? quotation)
    {
        if (quotation == null || !quotation.NeedId.HasValue) return;

        IsBusy = true;
        try
        {
            // On s'assure d'avoir les détails du besoin lié pour récupérer les quantités originales
            var needDetails = await _unitOfWork.NeedDetails.FindAsync(d => d.NeedId == quotation.NeedId.Value);
            var detailsList = needDetails?.ToList() ?? new List<NeedDetail>();

            _navigationService.NavigateTo("Commandes", new BonCommandeCreationContext
            {
                Supplier = quotation.Supplier,
                Quotation = quotation,
                Items = quotation.Details?.Select(d => 
                {
                    // On récupère la quantité demandée dans le besoin original pour cet article
                    var requestedQty = detailsList.FirstOrDefault(nd => nd.ProductId == d.ProductId)?.Quantity ?? d.Quantity;

                    return new PurchaseOrderDetail
                    {
                        ProductId = d.ProductId,
                        Product = d.Product,
                        Quantity = requestedQty, // Quantité du besoin original
                        UnitPriceHT = d.UnitPriceHT, // PRIX DU DEVIS DU FOURNISSEUR
                        UnitPriceTTC = d.UnitPriceTTC
                    };
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de la préparation de la commande : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
