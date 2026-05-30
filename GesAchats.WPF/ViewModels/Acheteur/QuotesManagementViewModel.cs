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
        Unit = detail.Need?.Unit ?? "Unité";
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
    private readonly INavigationService _navigationService;
    private Need? _selectedNeed;
    private bool _isEditMode;
    private Quotation? _editingQuotation;
    private bool _isCreateDialogOpen;
    private string _filterReference = string.Empty;
    private int? _filterSupplierId;
    private string _filterSupplierName = string.Empty;
    private string _filterStatus = "Tous";

    public ObservableCollection<Need> PendingNeeds { get; } = new ObservableCollection<Need>();
    public ObservableCollection<ArticleSelectionViewModel> ArticlesToQuote { get; } = new ObservableCollection<ArticleSelectionViewModel>();
    public ObservableCollection<SupplierSelectionViewModel> AvailableSuppliers { get; } = new ObservableCollection<SupplierSelectionViewModel>();
    public ObservableCollection<object> FilterSuppliers { get; } = new ObservableCollection<object>();
    private object? _selectedFilterSupplier;
    public object? SelectedFilterSupplier
    {
        get => _selectedFilterSupplier;
        set
        {
            if (SetProperty(ref _selectedFilterSupplier, value))
            {
                if (value is Supplier supplier)
                {
                    FilterSupplierId = supplier.Id;
                }
                else
                {
                    FilterSupplierId = null;
                }
                ApplyFilters();
            }
        }
    }
    public ObservableCollection<Quotation> AllQuotations { get; } = new ObservableCollection<Quotation>();
    public ObservableCollection<Quotation> FilteredQuotations { get; } = new ObservableCollection<Quotation>();

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

    public bool IsCreateDialogOpen
    {
        get => _isCreateDialogOpen;
        set => SetProperty(ref _isCreateDialogOpen, value);
    }

    public string FilterReference
    {
        get => _filterReference;
        set
        {
            if (SetProperty(ref _filterReference, value))
            {
                ApplyFilters();
            }
        }
    }

    public int? FilterSupplierId
    {
        get => _filterSupplierId;
        set
        {
            if (SetProperty(ref _filterSupplierId, value))
            {
                ApplyFilters();
            }
        }
    }

    public string FilterStatus
    {
        get => _filterStatus;
        set
        {
            if (SetProperty(ref _filterStatus, value))
            {
                ApplyFilters();
            }
        }
    }

    public string FilterSupplierName
    {
        get => _filterSupplierName;
        set
        {
            if (SetProperty(ref _filterSupplierName, value))
            {
                ApplyFilters();
            }
        }
    }

    private int _totalDevis;
    public int TotalDevis
    {
        get => _totalDevis;
        set => SetProperty(ref _totalDevis, value);
    }

    private int _devisEnvoyes;
    public int DevisEnvoyes
    {
        get => _devisEnvoyes;
        set => SetProperty(ref _devisEnvoyes, value);
    }

    private int _reponseRecue;
    public int ReponseRecue
    {
        get => _reponseRecue;
        set => SetProperty(ref _reponseRecue, value);
    }

    private int _devisAcceptes;
    public int DevisAcceptes
    {
        get => _devisAcceptes;
        set => SetProperty(ref _devisAcceptes, value);
    }

    private int _devisValides;
    public int DevisValides
    {
        get => _devisValides;
        set => SetProperty(ref _devisValides, value);
    }

    private decimal _montantTotal;
    public decimal MontantTotal
    {
        get => _montantTotal;
        set => SetProperty(ref _montantTotal, value);
    }

    private decimal _moyenneParDevis;
    public decimal MoyenneParDevis
    {
        get => _moyenneParDevis;
        set => SetProperty(ref _moyenneParDevis, value);
    }

    private string _fournisseurPlusSollicite = string.Empty;
    public string FournisseurPlusSollicite
    {
        get => _fournisseurPlusSollicite;
        set => SetProperty(ref _fournisseurPlusSollicite, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand OpenCreateDialogCommand { get; }
    public ICommand CloseCreateDialogCommand { get; }
    public ICommand CreateQuotesCommand { get; }
    public ICommand ViewQuotationCommand { get; }
    public ICommand EditQuotationCommand { get; }
    public ICommand DeleteQuotationCommand { get; }
    public ICommand PrintPdfCommand { get; }
    public ICommand ChiffrerCommand { get; }
    public ICommand NavigateToDashboardCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    public QuotesManagementViewModel(IUnitOfWork unitOfWork, IUserSession userSession, IPdfGeneratorService pdfService, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
        _pdfService = pdfService;
        _navigationService = navigationService;
        Title = "Gestion des Devis";

        RefreshCommand = new RelayCommand(async _ => await LoadInitialData());
        OpenCreateDialogCommand = new RelayCommand(_ => OpenCreateDialog());
        CloseCreateDialogCommand = new RelayCommand(_ => CloseCreateDialog());
        CreateQuotesCommand = new RelayCommand(async _ => await ExecuteCreateQuotes(), _ => CanCreateQuotes());
        ViewQuotationCommand = new RelayCommand(async p => await ExecuteViewQuotation(p as Quotation));
        EditQuotationCommand = new RelayCommand(async p => await ExecuteEditQuotation(p as Quotation), p => CanEditQuotation(p as Quotation));
        DeleteQuotationCommand = new RelayCommand(async p => await ExecuteDeleteQuotation(p as Quotation), p => CanEditQuotation(p as Quotation));
        PrintPdfCommand = new RelayCommand(async p => await ExecutePrintPdf(p as Quotation));
        ChiffrerCommand = new RelayCommand(p => ExecuteChiffrer(p as Quotation));
        NavigateToDashboardCommand = new RelayCommand(_ => _navigationService.NavigateTo("Dashboard"));
        ClearFiltersCommand = new RelayCommand(_ => ClearFilters());

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

    private void OpenCreateDialog()
    {
        ResetForm();
        IsCreateDialogOpen = true;
    }

    private void CloseCreateDialog()
    {
        IsCreateDialogOpen = false;
        ResetForm();
    }

    private async Task ExecuteViewQuotation(Quotation? quotation)
    {
        if (quotation == null) return;
        System.Windows.MessageBox.Show($"Affichage des détails du devis {quotation.ReferenceNumber}", "Détails", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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

            foreach (var s in AvailableSuppliers)
            {
                s.IsSelected = s.Supplier.Id == _editingQuotation.SupplierId;
            }

            foreach (var a in ArticlesToQuote)
            {
                a.IsSelected = _editingQuotation.Details.Any(d => d.ProductId == a.ProductId);
            }

            IsCreateDialogOpen = true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteDeleteQuotation(Quotation? quotation)
    {
        if (quotation == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Êtes-vous sûr de vouloir supprimer le devis {quotation.ReferenceNumber} ?",
            "Confirmation de suppression",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            IsBusy = true;
            try
            {
                _unitOfWork.Quotations.Remove(quotation);
                await _unitOfWork.CompleteAsync();
                await LoadInitialData();
                System.Windows.MessageBox.Show("Le devis a été supprimé.", "Succès", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
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

    private void ExecuteChiffrer(Quotation? quotation)
    {
        if (quotation == null) return;
        _navigationService.NavigateTo("SaisiePrix", quotation.Id);
    }

    private async Task LoadInitialData()
    {
        IsBusy = true;
        try
        {
            var needs = await _unitOfWork.Needs.GetPendingNeedsWithProductsAsync();
            
            var currentSelectedId = SelectedNeed?.Id ?? _pendingNavigationNeed?.Id;

            PendingNeeds.Clear();
            if (needs != null)
            {
                foreach (var n in needs) PendingNeeds.Add(n);
            }

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

            var suppliers = await _unitOfWork.Suppliers.FindAsync(s => s.IsActive);
            AvailableSuppliers.Clear();
            FilterSuppliers.Clear();
            FilterSuppliers.Add("Tous");
            foreach (var s in suppliers)
            {
                AvailableSuppliers.Add(new SupplierSelectionViewModel(s));
                FilterSuppliers.Add(s);
            }

            var quotes = await _unitOfWork.Quotations.GetAllWithAllRelatedAsync();
            AllQuotations.Clear();
            foreach (var q in quotes) AllQuotations.Add(q);

            SelectedFilterSupplier = "Tous";
            FilterStatus = "Tous";
            CalculateStatistics();
            ApplyFilters();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void CalculateStatistics()
    {
        TotalDevis = AllQuotations.Count;
        DevisEnvoyes = AllQuotations.Count;
        ReponseRecue = AllQuotations.Count(q => q.ResponseDate.HasValue);
        DevisAcceptes = AllQuotations.Count(q => q.Status == QuotationStatus.Validated);
        DevisValides = AllQuotations.Count(q => q.Status == QuotationStatus.Validated);
        MontantTotal = AllQuotations.Sum(q => q.TotalAmountTTC);
        MoyenneParDevis = TotalDevis > 0 ? MontantTotal / TotalDevis : 0;
        
        if (AllQuotations.Any())
        {
            var supplierStats = AllQuotations
                .Where(q => q.Supplier != null)
                .GroupBy(q => q.Supplier.CompanyName)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            
            FournisseurPlusSollicite = supplierStats != null 
                ? $"{supplierStats.Key} ({supplierStats.Count()} devis)" 
                : "-";
        }
        else
        {
            FournisseurPlusSollicite = "-";
        }
    }

    private void ApplyFilters()
    {
        FilteredQuotations.Clear();
        var filtered = AllQuotations.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FilterReference))
        {
            filtered = filtered.Where(q => q.ReferenceNumber.Contains(FilterReference, StringComparison.OrdinalIgnoreCase));
        }

        if (FilterSupplierId.HasValue)
        {
            filtered = filtered.Where(q => q.SupplierId == FilterSupplierId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(FilterSupplierName))
        {
            var searchLower = FilterSupplierName.ToLower();
            filtered = filtered.Where(q => q.Supplier != null && q.Supplier.CompanyName.ToLower().Contains(searchLower));
        }

        if (FilterStatus != "Tous")
        {
            filtered = filtered.Where(q => q.Status == FilterStatus);
        }

        foreach (var q in filtered)
        {
            FilteredQuotations.Add(q);
        }
    }

    private void ClearFilters()
    {
        FilterReference = string.Empty;
        SelectedFilterSupplier = "Tous";
        FilterSupplierId = null;
        FilterSupplierName = string.Empty;
        FilterStatus = "Tous";
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
            ArticlesToQuote.Add(new ArticleSelectionViewModel(SelectedNeed));
        }
    }

    private bool CanCreateQuotes()
    {
        return SelectedNeed != null && 
               ArticlesToQuote.Any(a => a.IsSelected) && 
               AvailableSuppliers.Any(s => s.IsSelected);
    }

    private bool CanEditQuotation(Quotation? quotation)
    {
        if (quotation == null) return false;
        return quotation.Status != QuotationStatus.Validated;
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
                var supplierSelection = selectedSuppliers.FirstOrDefault();
                if (supplierSelection == null) return;

                _editingQuotation.SupplierId = supplierSelection.Supplier.Id;
                _editingQuotation.NeedId = SelectedNeed!.Id;
                _editingQuotation.UpdatedAt = DateTime.UtcNow;

                var oldDetails = await _unitOfWork.QuotationDetails.FindAsync(d => d.QuotationId == _editingQuotation.Id);
                foreach (var d in oldDetails) _unitOfWork.QuotationDetails.Remove(d);

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
                foreach (var supplierSelection in selectedSuppliers)
                {
                    var quotation = new Quotation
                    {
                        ReferenceNumber = $"DEV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                        Date = DateTime.UtcNow,
                        SupplierId = supplierSelection.Supplier.Id,
                        NeedId = SelectedNeed!.Id,
                        Status = QuotationStatus.Pending,
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

            if (SelectedNeed!.Status == NeedStatus.TransmittedToPurchasing)
            {
                SelectedNeed.Status = NeedStatus.InPurchase;
                _unitOfWork.Needs.Update(SelectedNeed);
            }

            await _unitOfWork.CompleteAsync();
            
            System.Windows.MessageBox.Show(IsEditMode ? "Le devis a été mis à jour." : $"{selectedSuppliers.Count} demandes de devis ont été générées.", "Succès", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            
            CloseCreateDialog();
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
