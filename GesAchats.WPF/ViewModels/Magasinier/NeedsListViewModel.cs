using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class NeedItemViewModel : BaseViewModel
{
    private bool _isSelected;
    private decimal _quantityToOrder;

    public Product Product { get; }
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public decimal QuantityToOrder { get => _quantityToOrder; set => SetProperty(ref _quantityToOrder, value); }

    public NeedItemViewModel(Product product)
    {
        Product = product;
        QuantityToOrder = product.MinimumStock > product.CurrentStock ? product.MinimumStock - product.CurrentStock : 0;
        IsSelected = QuantityToOrder > 0;
    }
}

public class NeedsListViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;
    private string _searchText = string.Empty;

    public ObservableCollection<NeedItemViewModel> Items { get; } = new ObservableCollection<NeedItemViewModel>();
    
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) FilterItems(); }
    }

    public ICommand AddProductCommand { get; set; }
    public ICommand TransmitCommand { get; }
    public ICommand CancelCommand { get; set; }
    public ICommand RefreshCommand { get; }

    private List<Product> _allProducts = new List<Product>();

    public NeedsListViewModel(IUnitOfWork unitOfWork, IUserSession userSession)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
        Title = "Créer une Liste de Besoins";

        AddProductCommand = new RelayCommand(_ => { }); // Sera injecté par la vue
        TransmitCommand = new RelayCommand(async _ => await ExecuteTransmit(), _ => Items.Any(i => i.IsSelected && i.QuantityToOrder > 0));
        CancelCommand = new RelayCommand(_ => ExecuteCancel());
        RefreshCommand = new RelayCommand(async _ => await LoadData());

        _ = LoadData();
    }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var products = await _unitOfWork.Products.GetAllAsync();
            _allProducts = products.ToList();
            FilterItems();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterItems()
    {
        Items.Clear();
        var filtered = _allProducts.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(p => p.Designation.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        foreach (var p in filtered)
            Items.Add(new NeedItemViewModel(p));
    }

    private void ExecuteAddProduct()
    {
        // Sera géré par une popup dans la vue
    }

    private async Task ExecuteTransmit()
    {
        IsBusy = true;
        try
        {
            var selectedItems = Items.Where(i => i.IsSelected && i.QuantityToOrder > 0).ToList();
            
            if (!selectedItems.Any())
            { 
                System.Windows.MessageBox.Show("Veuillez sélectionner au moins un article avec une quantité valide.", "Sélection vide", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            // Création d'UNE SEULE liste de besoins pour tous les articles sélectionnés
            var need = new Need
            {
                NumeroBesoin = $"BES-{DateTime.Now:yyyyMMddHHmmss}",
                Description = $"Demande groupée de {selectedItems.Count} article(s)",
                Status = NeedStatus.InPurchase,
                RequestedById = _userSession.CurrentUser?.Id ?? 1,
                RequestedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                // On peut mettre le premier produit comme référence principale si nécessaire pour la compatibilité
                ProductId = selectedItems.First().Product.Id, 
                Quantity = selectedItems.First().QuantityToOrder,
                Unit = selectedItems.First().Product.Unit
            };

            foreach (var item in selectedItems)
            {
                // Vérification de l'existence du produit en base
                var productExists = await _unitOfWork.Products.GetByIdAsync(item.Product.Id);
                if (productExists == null)
                {
                    throw new Exception($"Le produit '{item.Product.Designation}' n'existe plus dans la base de données.");
                }

                // Ajout de chaque article dans la collection Details de l'unique Need
                need.Details.Add(new NeedDetail
                {
                    ProductId = item.Product.Id,
                    Quantity = item.QuantityToOrder,
                    IsNewProduct = false
                });
            }

            // Persistence de l'unique entité Need (avec ses Details)
            await _unitOfWork.Needs.AddAsync(need);
            await _unitOfWork.CompleteAsync();
            
            System.Windows.MessageBox.Show($"La liste de besoins {need.NumeroBesoin} contenant {selectedItems.Count} article(s) a été transmise avec succès.", "Transmission réussie", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            
            // Réinitialisation de l'interface
            ExecuteCancel();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Une erreur est survenue lors de la transmission : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExecuteCancel()
    {
        // Réinitialisation de la liste (décocher tout)
        foreach (var item in Items)
        {
            item.IsSelected = false;
            item.QuantityToOrder = 0;
        }
        
        // On peut aussi recharger les données pour être sûr d'avoir le stock à jour
        _ = LoadData();
    }
}
