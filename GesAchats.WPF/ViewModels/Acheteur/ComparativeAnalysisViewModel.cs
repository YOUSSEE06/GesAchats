using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class QuotationComparisonItemViewModel : BaseViewModel
{
    private readonly IComparativeAnalysisService _analysisService;
    private readonly INavigationService _navigationService;
    public Quotation Quotation { get; }
    public decimal Score { get; }

    public ICommand GeneratePurchaseOrderCommand { get; }

    public QuotationComparisonItemViewModel(Quotation quotation, IComparativeAnalysisService analysisService, INavigationService navigationService, IUnitOfWork unitOfWork)
    {
        Quotation = quotation;
        _analysisService = analysisService;
        _navigationService = navigationService;
        Score = _analysisService.CalculateSupplierScore(quotation);

        GeneratePurchaseOrderCommand = new RelayCommand(async _ => 
        {
            if (!Quotation.NeedId.HasValue) return;

            IsBusy = true;
            try
            {
                // Charger les détails du besoin pour les quantités
                var needDetails = await unitOfWork.NeedDetails.FindAsync(nd => nd.NeedId == Quotation.NeedId.Value);
                var detailsList = needDetails?.ToList() ?? new List<NeedDetail>();

                _navigationService.NavigateTo("Commandes", new BonCommandeCreationContext
                {
                    Supplier = Quotation.Supplier,
                    Quotation = Quotation,
                    Items = Quotation.Details?.Select(d => 
                    {
                        var requestedQty = detailsList.FirstOrDefault(nd => nd.ProductId == d.ProductId)?.Quantity ?? d.Quantity;
                        return new PurchaseOrderDetail
                        {
                            ProductId = d.ProductId,
                            Product = d.Product,
                            Quantity = requestedQty,
                            UnitPriceHT = d.UnitPriceHT, // Utilise le prix du devis
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
        });
    }

    public string ScoreColor => Score >= 8 ? "#4CAF50" : Score >= 5 ? "#FFC107" : "#F44336";
}

public class ComparativeAnalysisViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IComparativeAnalysisService _analysisService;
    private readonly INavigationService _navigationService;
    private Need? _selectedNeed;

    public ObservableCollection<Need> PendingNeeds { get; } = new ObservableCollection<Need>();
    public ObservableCollection<QuotationComparisonItemViewModel> Quotations { get; } = new ObservableCollection<QuotationComparisonItemViewModel>();

    public Need? SelectedNeed
    {
        get => _selectedNeed;
        set
        {
            if (SetProperty(ref _selectedNeed, value))
            {
                _ = LoadQuotationsForNeed();
            }
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand SelectBestCommand { get; }

    public ComparativeAnalysisViewModel(IUnitOfWork unitOfWork, IComparativeAnalysisService analysisService, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _analysisService = analysisService;
        _navigationService = navigationService;
        Title = "Analyse Comparative";

        RefreshCommand = new RelayCommand(async _ => await LoadNeeds());
        SelectBestCommand = new RelayCommand(p => ExecuteSelectBest(p as QuotationComparisonItemViewModel));

        _ = LoadNeeds();
    }

    private async Task LoadNeeds()
    {
        IsBusy = true;
        try
        {
            var needs = await _unitOfWork.Needs.GetPendingNeedsWithProductsAsync();
            PendingNeeds.Clear();
            if (needs != null)
            {
                foreach (var n in needs)
                {
                    PendingNeeds.Add(n);
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadQuotationsForNeed()
    {
        if (SelectedNeed == null)
        {
            Quotations.Clear();
            return;
        }

        IsBusy = true;
        try
        {
            var quotations = await _analysisService.CompareQuotationsForNeedAsync(SelectedNeed.Id);
            Quotations.Clear();
            foreach (var q in quotations)
            {
                Quotations.Add(new QuotationComparisonItemViewModel(q, _analysisService, _navigationService, _unitOfWork));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async void ExecuteSelectBest(QuotationComparisonItemViewModel? item)
    {
        if (item == null || !item.Quotation.NeedId.HasValue) return;
        
        IsBusy = true;
        try
        {
            // Charger les détails du besoin pour les quantités
            var needDetails = await _unitOfWork.NeedDetails.FindAsync(nd => nd.NeedId == item.Quotation.NeedId.Value);
            var detailsList = needDetails?.ToList() ?? new List<NeedDetail>();

            _navigationService.NavigateTo("Commandes", new BonCommandeCreationContext
            {
                Supplier = item.Quotation.Supplier,
                Quotation = item.Quotation,
                Items = item.Quotation.Details?.Select(d => 
                {
                    var requestedQty = detailsList.FirstOrDefault(nd => nd.ProductId == d.ProductId)?.Quantity ?? d.Quantity;
                    return new PurchaseOrderDetail
                    {
                        ProductId = d.ProductId,
                        Product = d.Product,
                        Quantity = requestedQty,
                        UnitPriceHT = d.UnitPriceHT, // Utilise le prix du devis
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
