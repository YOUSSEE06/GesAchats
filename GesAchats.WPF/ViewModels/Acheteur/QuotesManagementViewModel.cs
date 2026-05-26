using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class ArticleSelectionViewModel : BaseViewModel
{
    private bool _isSelected = true;
    public int ProductId { get; }
    public string ProductName { get; }
    public decimal Quantity { get; }
    public string Unit { get; }
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ArticleSelectionViewModel(Need need)
    {
        ProductId = need.ProductId;
        ProductName = need.Product?.Designation ?? "Produit inconnu";
        Quantity = need.Quantity;
        Unit = need.Unit ?? "Unité";
    }

    public ArticleSelectionViewModel(NeedDetail detail)
    {
        ProductId = detail.ProductId;
        ProductName = detail.Product?.Designation ?? "Produit inconnu";
        Quantity = detail.Quantity;
        Unit = detail.Need?.Unit ?? "Unité"; // Ou detail.Product.Unit si disponible
    }
}

public class SupplierSelectionViewModel : BaseViewModel
{
    private bool _isSelected;
    public Supplier Supplier { get; }
    
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public SupplierSelectionViewModel(Supplier supplier)
    {
        Supplier = supplier;
    }
}

public class QuotesManagementViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;
    private readonly IPdfGeneratorService _pdfService;
    private Need? _selectedNeed;
    private bool _isEditMode;
    private Quotation? _editingQuotation;

    public ObservableCollection<Need> PendingNeeds { get; } = new ObservableCollection<Need>();
    public ObservableCollection<ArticleSelectionViewModel> ArticlesToQuote { get; } = new ObservableCollection<ArticleSelectionViewModel>();
    public ObservableCollection<SupplierSelectionViewModel> AvailableSuppliers { get; } = new ObservableCollection<SupplierSelectionViewModel>();
    public ObservableCollection<Quotation> ExistingQuotations { get; } = new ObservableCollection<Quotation>();

    public Need? SelectedNeed
    {
        get => _selectedNeed;
        set
        {
            if (SetProperty(ref _selectedNeed, value))
            {
                LoadArticlesForNeed();
            }
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand CreateQuotesCommand { get; }
    public ICommand EditQuotationCommand { get; }
    public ICommand PrintPdfCommand { get; }
    public ICommand CancelEditCommand { get; }

    public QuotesManagementViewModel(IUnitOfWork unitOfWork, IUserSession userSession, IPdfGeneratorService pdfService)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
        _pdfService = pdfService;
        Title = "Gestion des Devis";

        RefreshCommand = new RelayCommand(async _ => await LoadInitialData());
        CreateQuotesCommand = new RelayCommand(async _ => await ExecuteCreateQuotes(), _ => CanCreateQuotes());
        EditQuotationCommand = new RelayCommand(async p => await ExecuteEditQuotation(p as Quotation));
        PrintPdfCommand = new RelayCommand(async p => await ExecutePrintPdf(p as Quotation));
        CancelEditCommand = new RelayCommand(_ => ResetForm());

        _ = LoadInitialData();
    }

    private Need? _pendingNavigationNeed;

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is Need need)
        {
            _pendingNavigationNeed = need;
            TryApplyPendingNeed();
        }
    }

    private void TryApplyPendingNeed()
    {
        if (_pendingNavigationNeed == null) return;

        // Si la liste est déjà chargée, on applique la sélection
        if (PendingNeeds.Any())
        {
            var existing = PendingNeeds.FirstOrDefault(n => n.Id == _pendingNavigationNeed.Id);
            if (existing == null)
            {
                PendingNeeds.Add(_pendingNavigationNeed);
                existing = _pendingNavigationNeed;
            }
            SelectedNeed = existing;
            _pendingNavigationNeed = null;
        }
    }

    private async Task ExecuteEditQuotation(Quotation? quotation)
    {
        if (quotation == null) return;

        IsBusy = true;
        try
        {
            _editingQuotation = await _unitOfWork.Quotations.GetWithDetailsAsync(quotation.Id);
            if (_editingQuotation == null) return;

            IsEditMode = true;
            SelectedNeed = _editingQuotation.Need;

            // Sélectionner le fournisseur
            foreach (var s in AvailableSuppliers)
            {
                s.IsSelected = s.Supplier.Id == _editingQuotation.SupplierId;
            }

            // Sélectionner les articles
            foreach (var a in ArticlesToQuote)
            {
                a.IsSelected = _editingQuotation.Details.Any(d => d.ProductId == a.ProductId);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecutePrintPdf(Quotation? quotation)
    {
        if (quotation == null) return;

        IsBusy = true;
        try
        {
            var fullQuotation = await _unitOfWork.Quotations.GetWithDetailsAsync(quotation.Id);
            if (fullQuotation == null) return;

            var filePath = await _pdfService.GenerateQuotationRequestPdfAsync(fullQuotation);
            
            var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true };
            process.Start();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de l'impression : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadInitialData()
    {
        IsBusy = true;
        try
        {
            // 1. Charger les besoins en attente avec produits
            var needs = await _unitOfWork.Needs.GetPendingNeedsWithProductsAsync();
            
            // On garde le besoin sélectionné s'il y en a un
            var currentSelectedId = SelectedNeed?.Id ?? _pendingNavigationNeed?.Id;

            PendingNeeds.Clear();
            if (needs != null)
            {
                foreach (var n in needs) PendingNeeds.Add(n);
            }

            // Réappliquer la sélection ou le besoin en attente
            if (currentSelectedId.HasValue)
            {
                var toSelect = PendingNeeds.FirstOrDefault(n => n.Id == currentSelectedId.Value);
                if (toSelect == null && _pendingNavigationNeed != null)
                {
                    PendingNeeds.Add(_pendingNavigationNeed);
                    toSelect = _pendingNavigationNeed;
                }
                
                if (toSelect != null)
                {
                    SelectedNeed = toSelect;
                }
            }
            
            _pendingNavigationNeed = null;

            // 2. Charger les fournisseurs
            var suppliers = await _unitOfWork.Suppliers.FindAsync(s => s.IsActive);
            AvailableSuppliers.Clear();
            foreach (var s in suppliers) AvailableSuppliers.Add(new SupplierSelectionViewModel(s));

            // 3. Charger les devis récents
            var quotes = await _unitOfWork.Quotations.GetAllWithSuppliersAsync();
            ExistingQuotations.Clear();
            foreach (var q in quotes.Take(20)) ExistingQuotations.Add(q);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadArticlesForNeed()
    {
        ArticlesToQuote.Clear();
        if (SelectedNeed == null) return;

        if (SelectedNeed.Details != null && SelectedNeed.Details.Any())
        {
            foreach (var detail in SelectedNeed.Details)
            {
                ArticlesToQuote.Add(new ArticleSelectionViewModel(detail));
            }
        }
        else
        {
            // Fallback pour les anciens besoins sans Details
            ArticlesToQuote.Add(new ArticleSelectionViewModel(SelectedNeed));
        }
    }

    private bool CanCreateQuotes()
    {
        return SelectedNeed != null && 
               ArticlesToQuote.Any(a => a.IsSelected) && 
               AvailableSuppliers.Any(s => s.IsSelected);
    }

    private async Task ExecuteCreateQuotes()
    {
        if (!CanCreateQuotes()) return;

        IsBusy = true;
        try
        {
            var selectedArticles = ArticlesToQuote.Where(a => a.IsSelected).ToList();
            var selectedSuppliers = AvailableSuppliers.Where(s => s.IsSelected).ToList();

            if (IsEditMode && _editingQuotation != null)
            {
                // Mode Edition : On ne modifie qu'un seul devis
                var supplierSelection = selectedSuppliers.FirstOrDefault();
                if (supplierSelection == null) return;

                _editingQuotation.SupplierId = supplierSelection.Supplier.Id;
                _editingQuotation.NeedId = SelectedNeed!.Id;
                _editingQuotation.UpdatedAt = DateTime.UtcNow;

                // Supprimer les anciens détails
                var oldDetails = await _unitOfWork.QuotationDetails.FindAsync(d => d.QuotationId == _editingQuotation.Id);
                foreach (var d in oldDetails) _unitOfWork.QuotationDetails.Remove(d);

                // Ajouter les nouveaux détails
                foreach (var articleSelection in selectedArticles)
                {
                    _editingQuotation.Details.Add(new QuotationDetail
                    {
                        ProductId = articleSelection.ProductId,
                        Quantity = articleSelection.Quantity
                    });
                }

                _unitOfWork.Quotations.Update(_editingQuotation);
            }
            else
            {
                // Mode Création : On peut créer plusieurs devis (un par fournisseur)
                foreach (var supplierSelection in selectedSuppliers)
                {
                    var quotation = new Quotation
                    {
                        ReferenceNumber = $"DEV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                        Date = DateTime.UtcNow,
                        SupplierId = supplierSelection.Supplier.Id,
                        NeedId = SelectedNeed!.Id,
                        Status = "Sent",
                        CreatedById = _userSession.CurrentUser?.Id ?? 1,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    foreach (var articleSelection in selectedArticles)
                    {
                        quotation.Details.Add(new QuotationDetail
                        {
                            ProductId = articleSelection.ProductId,
                            Quantity = articleSelection.Quantity
                        });
                    }

                    await _unitOfWork.Quotations.AddAsync(quotation);
                }
            }

            // Mettre à jour le statut du besoin
            if (SelectedNeed!.Status == NeedStatus.TransmittedToPurchasing)
            {
                SelectedNeed.Status = NeedStatus.InPurchase;
                _unitOfWork.Needs.Update(SelectedNeed);
            }

            await _unitOfWork.CompleteAsync();
            
            System.Windows.MessageBox.Show(IsEditMode ? "Le devis a été mis à jour." : $"{selectedSuppliers.Count} demandes de devis ont été générées.", "Succès", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            
            ResetForm();
            await LoadInitialData();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetForm()
    {
        SelectedNeed = null;
        ArticlesToQuote.Clear();
        foreach (var s in AvailableSuppliers) s.IsSelected = false;
        IsEditMode = false;
        _editingQuotation = null;
    }
}
