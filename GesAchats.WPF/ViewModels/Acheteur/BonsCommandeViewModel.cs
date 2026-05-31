using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class BCItemViewModel : BaseViewModel
{
    private readonly Action _onTotalsChanged;
    private bool _isIncluded = true;
    private decimal _quantity;
    private decimal _unitPriceHT;
    private decimal _unitPriceTTC;
    private decimal _vatRate;

    public PurchaseOrderDetail Detail { get; }

    public BCItemViewModel(PurchaseOrderDetail detail, decimal vatRate, Action onTotalsChanged)
    {
        Detail = detail;
        _quantity = detail.Quantity;
        _unitPriceHT = detail.UnitPriceHT;
        _vatRate = vatRate;
        _onTotalsChanged = onTotalsChanged;
        
        // Calcul automatique initial du TTC basé sur le HT et le taux de TVA
        _unitPriceTTC = _unitPriceHT * (1 + _vatRate / 100);
        Detail.UnitPriceTTC = _unitPriceTTC;
    }

    public bool IsIncluded
    {
        get => _isIncluded;
        set
        {
            if (SetProperty(ref _isIncluded, value))
                _onTotalsChanged?.Invoke();
        }
    }

    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                Detail.Quantity = value;
                UpdateTotals();
            }
        }
    }

    public decimal UnitPriceHT
    {
        get => _unitPriceHT;
        set
        {
            if (SetProperty(ref _unitPriceHT, value))
            {
                Detail.UnitPriceHT = value;
                // Auto-calculate TTC based on HT and current VAT rate
                _unitPriceTTC = value * (1 + _vatRate / 100);
                OnPropertyChanged(nameof(UnitPriceTTC));
                Detail.UnitPriceTTC = _unitPriceTTC;
                UpdateTotals();
            }
        }
    }

    public decimal UnitPriceTTC
    {
        get => _unitPriceTTC;
        set
        {
            if (SetProperty(ref _unitPriceTTC, value))
            {
                Detail.UnitPriceTTC = value;
                // Auto-calculate HT based on TTC and current VAT rate
                if (_vatRate > -100) // Avoid division by zero
                {
                    _unitPriceHT = value / (1 + _vatRate / 100);
                    OnPropertyChanged(nameof(UnitPriceHT));
                    Detail.UnitPriceHT = _unitPriceHT;
                }
                UpdateTotals();
            }
        }
    }

    public decimal VatRate
    {
        get => _vatRate;
        set
        {
            if (SetProperty(ref _vatRate, value))
            {
                // Recalculate TTC based on HT when VAT rate changes
                _unitPriceTTC = _unitPriceHT * (1 + value / 100);
                OnPropertyChanged(nameof(UnitPriceTTC));
                Detail.UnitPriceTTC = _unitPriceTTC;
                UpdateTotals();
            }
        }
    }

    public decimal TotalHT => Quantity * UnitPriceHT;
    public decimal TotalTTC => Quantity * UnitPriceTTC;

    private void UpdateTotals()
    {
        OnPropertyChanged(nameof(TotalHT));
        OnPropertyChanged(nameof(TotalTTC));
        _onTotalsChanged?.Invoke();
    }
}

public class BonsCommandeViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;
    private readonly IPdfGeneratorService _pdfService;

    // Navigation Parameter Storage
    private BonCommandeCreationContext? _pendingNavigation;

    // View State
    private bool _isHistoryView = true;
    private bool _isEditView = false;
    private bool _isInspectView = false;

    // Filter properties
    private string _searchText = string.Empty;
    private DateTime? _searchDate;
    private string _selectedStatusFilter = "Tous";
    private Supplier? _selectedSupplierFilter;

    // Form properties
    private Supplier? _selectedSupplier;
    private Quotation? _selectedQuotation;
    private string _observations = string.Empty;
    private decimal _vatRate = 20; // Default 20%
    private decimal _totalHT;
    private decimal _totalTTC;
    private decimal _totalVAT;
    private PurchaseOrder? _selectedOrder;

    private List<PurchaseOrder> _allOrders = new List<PurchaseOrder>();

    public ObservableCollection<Supplier> Suppliers { get; } = new ObservableCollection<Supplier>();
    public ObservableCollection<Quotation> AvailableQuotations { get; } = new ObservableCollection<Quotation>();
    public ObservableCollection<BCItemViewModel> Items { get; } = new ObservableCollection<BCItemViewModel>();
    public ObservableCollection<PurchaseOrder> OrdersHistory { get; } = new ObservableCollection<PurchaseOrder>();

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is BonCommandeCreationContext navParam)
        {
            _pendingNavigation = navParam;
            TryApplyNavigation();
        }
    }

    private void TryApplyNavigation()
    {
        if (_pendingNavigation == null) return;

        try
        {
            // On passe directement à la vue de création (formulaire)
            ExecuteShowAddForm();

            // Si les fournisseurs sont déjà chargés, on applique immédiatement
            if (Suppliers != null && Suppliers.Any())
            {
                var supplier = Suppliers.FirstOrDefault(s => s.Id == _pendingNavigation.Supplier?.Id);
                if (supplier == null && _pendingNavigation.Supplier != null)
                {
                    Suppliers.Add(_pendingNavigation.Supplier);
                    supplier = _pendingNavigation.Supplier;
                }
                
                if (supplier != null)
                {
                    SelectedSupplier = supplier;
                }

                // Si des articles sont déjà fournis, on les utilise
                if (_pendingNavigation.Items != null && _pendingNavigation.Items.Any())
                {
                    Items.Clear();
                    foreach (var detail in _pendingNavigation.Items)
                    {
                        if (detail != null)
                        {
                            Items.Add(new BCItemViewModel(detail, VatRate, CalculateTotals));
                        }
                    }
                    CalculateTotals();
                }
                
                if (_pendingNavigation.Quotation != null)
                {
                    // On s'assure que le devis est dans la liste pour la sélection UI
                    if (!AvailableQuotations.Any(q => q.Id == _pendingNavigation.Quotation.Id))
                    {
                        AvailableQuotations.Add(_pendingNavigation.Quotation);
                    }
                    
                    // On définit le devis (cela déclenchera LoadQuotationDetailsAndItems s'il n'y a pas d'items)
                    SelectedQuotation = AvailableQuotations.FirstOrDefault(q => q.Id == _pendingNavigation.Quotation.Id) ?? _pendingNavigation.Quotation;
                }
                
                _pendingNavigation = null;
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de l'application de la navigation : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    // State Accessors
    public bool IsHistoryView { get => _isHistoryView; set => SetProperty(ref _isHistoryView, value); }
    public bool IsEditView { get => _isEditView; set => SetProperty(ref _isEditView, value); }
    public bool IsInspectView { get => _isInspectView; set => SetProperty(ref _isInspectView, value); }

    // Filter Accessors
    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) FilterOrders(); } }
    public DateTime? SearchDate { get => _searchDate; set { if (SetProperty(ref _searchDate, value)) FilterOrders(); } }
    public string SelectedStatusFilter { get => _selectedStatusFilter; set { if (SetProperty(ref _selectedStatusFilter, value)) FilterOrders(); } }
    public Supplier? SelectedSupplierFilter { get => _selectedSupplierFilter; set { if (SetProperty(ref _selectedSupplierFilter, value)) FilterOrders(); } }

    public ObservableCollection<string> StatusFilters { get; } = new ObservableCollection<string>
    {
        "Tous",
        PurchaseOrderStatus.Pending,
        PurchaseOrderStatus.Validated,
        PurchaseOrderStatus.Cancelled
    };

    // Form Accessors
    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                if (value != null && !IsInspectView)
                {
                    // On ne recharge les devis que si le changement vient de l'utilisateur (pas d'une sélection de devis)
                    if (_selectedQuotation == null || _selectedQuotation.SupplierId != value.Id)
                    {
                        LoadQuotationsForSupplier();
                    }
                }
                else if (value == null && !IsInspectView)
                {
                    Items.Clear();
                    CalculateTotals();
                }
            }
        }
    }

    public Quotation? SelectedQuotation
    {
        get => _selectedQuotation;
        set
        {
            if (SetProperty(ref _selectedQuotation, value))
            {
                if (value != null && !IsInspectView)
                {
                    // Si on sélectionne un devis, on s'assure que le fournisseur est le bon sans déclencher LoadQuotationsForSupplier
                    if (SelectedSupplier == null || SelectedSupplier.Id != value.SupplierId)
                    {
                        var supplier = Suppliers.FirstOrDefault(s => s.Id == value.SupplierId);
                        if (supplier != null)
                        {
                            _selectedSupplier = supplier;
                            OnPropertyChanged(nameof(SelectedSupplier));
                        }
                    }
                    
                    // On s'assure que les détails sont chargés
                    _ = LoadQuotationDetailsAndItems(value);
                }
                else if (value == null && !IsInspectView)
                {
                    Items.Clear();
                    CalculateTotals();
                }
            }
        }
    }

    public decimal VatRate
    {
        get => _vatRate;
        set
        {
            if (SetProperty(ref _vatRate, value))
            {
                foreach (var item in Items)
                {
                    item.VatRate = value;
                }
                CalculateTotals();
            }
        }
    }

    public string Observations { get => _observations; set => SetProperty(ref _observations, value); }
    public decimal TotalHT { get => _totalHT; set => SetProperty(ref _totalHT, value); }
    public decimal TotalTTC { get => _totalTTC; set => SetProperty(ref _totalTTC, value); }
    public decimal TotalVAT { get => _totalVAT; set => SetProperty(ref _totalVAT, value); }

    public PurchaseOrder? SelectedOrder
    {
        get => _selectedOrder;
        set => SetProperty(ref _selectedOrder, value);
    }

    // Commands
    public ICommand CreateOrderCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand PrintPdfCommand { get; }
    public ICommand ShowAddFormCommand { get; }
    public ICommand BackToHistoryCommand { get; }
    public ICommand InspectCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ReactivateCommand { get; }
    public ICommand ResetFiltersCommand { get; }

    public BonsCommandeViewModel(IUnitOfWork unitOfWork, IUserSession userSession, IPdfGeneratorService pdfService)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
        _pdfService = pdfService;
        Title = "Bons de Commande";

        CreateOrderCommand = new RelayCommand(async _ => await ExecuteCreateOrder(), _ => CanCreateOrder());
        RefreshCommand = new RelayCommand(async _ => await LoadInitialData());
        PrintPdfCommand = new RelayCommand(async p => await ExecutePrintPdf(p as PurchaseOrder));
        ShowAddFormCommand = new RelayCommand(_ => ExecuteShowAddForm());
        BackToHistoryCommand = new RelayCommand(_ => ExecuteBackToHistory());
        InspectCommand = new RelayCommand(async p => await ExecuteInspect(p as PurchaseOrder));
        CancelCommand = new RelayCommand(async p => await ExecuteCancel(p as PurchaseOrder));
        ReactivateCommand = new RelayCommand(async p => await ExecuteReactivate(p as PurchaseOrder));
        ResetFiltersCommand = new RelayCommand(_ => ExecuteResetFilters());

        _ = LoadInitialData();
    }

    private void ExecuteShowAddForm()
    {
        IsHistoryView = false;
        IsEditView = true;
        IsInspectView = false;
        
        // Reset form
        SelectedSupplier = null;
        SelectedQuotation = null;
        Items.Clear();
        Observations = string.Empty;
        VatRate = 20;
        CalculateTotals();
    }

    private void ExecuteBackToHistory()
    {
        IsHistoryView = true;
        IsEditView = false;
        IsInspectView = false;
        SelectedOrder = null;
    }

    // Track if the inspected order is editable
    private bool _isOrderEditable = true;
    public bool IsOrderEditable
    {
        get => _isOrderEditable;
        set => SetProperty(ref _isOrderEditable, value);
    }

    private async Task ExecuteInspect(PurchaseOrder? order)
    {
        if (order == null) return;

        IsBusy = true;
        try
        {
            var fullOrder = await _unitOfWork.PurchaseOrders.GetWithDetailsAsync(order.Id);
            if (fullOrder == null) return;

            SelectedOrder = fullOrder;
            IsHistoryView = false;
            IsEditView = false;
            IsInspectView = true;
            
            // Order is only editable if status is "En attente"
            IsOrderEditable = fullOrder.Status == PurchaseOrderStatus.Pending;

            // Fill "form" fields for display in inspection mode
            SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == fullOrder.SupplierId);
            Observations = fullOrder.Observations ?? string.Empty;
            TotalHT = fullOrder.TotalAmountHT;
            TotalVAT = fullOrder.TotalVAT;
            TotalTTC = fullOrder.TotalAmountTTC;
            
            // Calculate effective VAT rate if possible
            if (fullOrder.TotalAmountHT > 0)
            {
                _vatRate = (fullOrder.TotalVAT / fullOrder.TotalAmountHT) * 100;
                OnPropertyChanged(nameof(VatRate));
            }

            Items.Clear();
            foreach (var detail in fullOrder.Details)
            {
                var itemVM = new BCItemViewModel(detail, _vatRate, CalculateTotals)
                {
                    IsIncluded = true
                };
                Items.Add(itemVM);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecutePrintPdf(PurchaseOrder? order)
    {
        if (order == null) return;

        IsBusy = true;
        try
        {
            var fullOrder = await _unitOfWork.PurchaseOrders.GetWithDetailsAsync(order.Id);
            if (fullOrder == null) return;

            var filePath = await _pdfService.GeneratePurchaseOrderPdfAsync(fullOrder);
            
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
            var suppliers = await _unitOfWork.Suppliers.FindAsync(s => s.IsActive);
            Suppliers.Clear();
            // Add "Tous" option at the beginning (dummy supplier with Id -1)
            var tousSupplier = new Supplier { Id = -1, CompanyName = "Tous", IsActive = true };
            Suppliers.Add(tousSupplier);
            foreach (var s in suppliers) Suppliers.Add(s);
            SelectedSupplierFilter = tousSupplier;

            var orders = await _unitOfWork.PurchaseOrders.GetAllWithSuppliersAsync();
            _allOrders = orders.ToList();
            FilterOrders();

            var quotes = await _unitOfWork.Quotations.GetAllWithSuppliersAsync();
            AvailableQuotations.Clear();
            if (quotes != null)
            {
                foreach (var q in quotes)
                {
                    AvailableQuotations.Add(q);
                }
            }

            // Apply any pending navigation after initial load
            TryApplyNavigation();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterOrders()
    {
        var filtered = _allOrders.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(o => 
                o.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (o.Supplier?.CompanyName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (o.Quotation?.ReferenceNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false)
            );
        }

        if (SelectedSupplierFilter != null && SelectedSupplierFilter.Id != -1)
        {
            filtered = filtered.Where(o => o.SupplierId == SelectedSupplierFilter.Id);
        }

        if (SearchDate.HasValue)
        {
            filtered = filtered.Where(o => o.OrderDate.Date == SearchDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(SelectedStatusFilter) && SelectedStatusFilter != "Tous")
        {
            filtered = filtered.Where(o => o.Status == SelectedStatusFilter);
        }

        OrdersHistory.Clear();
        foreach (var o in filtered.OrderByDescending(x => x.OrderDate)) 
            OrdersHistory.Add(o);
    }

    private void ExecuteResetFilters()
    {
        SearchText = string.Empty;
        SearchDate = null;
        SelectedStatusFilter = "Tous";
        SelectedSupplierFilter = Suppliers.FirstOrDefault(s => s.Id == -1);
    }

    private async void LoadQuotationsForSupplier()
    {
        var supplier = SelectedSupplier;
        if (supplier == null)
        {
            // Si aucun fournisseur n'est sélectionné, on recharge tous les devis disponibles pour permettre la sélection
            var quotes = await _unitOfWork.Quotations.GetAllWithSuppliersAsync();
            AvailableQuotations.Clear();
            if (quotes != null)
            {
                foreach (var q in quotes)
                {
                    AvailableQuotations.Add(q);
                }
            }
            return;
        }

        try
        {
            // On récupère tous les devis du fournisseur et on filtre sur ceux qui peuvent devenir des BC
            var allQuotes = await _unitOfWork.Quotations.GetBySupplierWithDetailsAsync(supplier.Id);
            
            // On garde le devis actuellement sélectionné s'il appartient au fournisseur
            var currentQuotation = SelectedQuotation;
            AvailableQuotations.Clear();
            
            if (currentQuotation != null && currentQuotation.SupplierId == supplier.Id)
            {
                AvailableQuotations.Add(currentQuotation);
            }
            
            if (allQuotes != null)
            {
                foreach (var q in allQuotes.Where(x => x.Status == QuotationStatus.Validated))
                {
                    if (currentQuotation?.Id != q.Id)
                    {
                        AvailableQuotations.Add(q);
                    }
                }
            }
            
            // Restaurer la sélection si nécessaire
            if (currentQuotation != null && SelectedQuotation == null)
            {
                SelectedQuotation = currentQuotation;
            }
        }
        catch (Exception ex)
        {
            // Log error or show message
            System.Diagnostics.Debug.WriteLine($"Error loading quotations: {ex.Message}");
        }
    }

    private async Task LoadQuotationDetailsAndItems(Quotation quotation)
    {
        if (quotation == null) return;

        // Si des items sont déjà présents (par exemple via navigation), on ne recharge pas
        if (Items.Any() && SelectedQuotation != null && SelectedQuotation.Id == quotation.Id) return;

        try
        {
            ICollection<QuotationDetail>? details = quotation.Details;

            // Si les détails ne sont pas chargés, on les récupère
            if (details == null || !details.Any())
            {
                var fullQuotation = await _unitOfWork.Quotations.GetWithDetailsAsync(quotation.Id);
                if (fullQuotation != null)
                {
                    details = fullQuotation.Details;
                }
            }

            if (details == null) return;

            Items.Clear();
            foreach (var qDetail in details)
            {
                if (qDetail == null) continue;

                // Les prix sont maintenant pré-remplis à partir du devis (saisie fournisseur)
                Items.Add(new BCItemViewModel(new PurchaseOrderDetail
                {
                    ProductId = qDetail.ProductId,
                    Product = qDetail.Product,
                    Quantity = qDetail.Quantity,
                    UnitPriceHT = qDetail.UnitPriceHT,
                    UnitPriceTTC = qDetail.UnitPriceTTC
                }, VatRate, CalculateTotals));
            }
            CalculateTotals();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading quotation details: {ex.Message}");
        }
    }

    private void LoadItemsFromQuotation()
    {
        // Cette méthode est remplacée par LoadQuotationDetailsAndItems
    }

    private void CalculateTotals()
    {
        var includedItems = Items.Where(i => i.IsIncluded).ToList();
        TotalHT = includedItems.Sum(i => i.TotalHT);
        TotalVAT = TotalHT * (VatRate / 100);
        TotalTTC = TotalHT + TotalVAT;
    }

    private bool CanCreateOrder()
    {
        return !IsInspectView && 
               SelectedSupplier != null && 
               Items.Any(i => i.IsIncluded && i.Quantity > 0 && (i.UnitPriceHT > 0 || i.UnitPriceTTC > 0));
    }

    private async Task ExecuteCreateOrder()
    {
        if (!CanCreateOrder()) return;

        IsBusy = true;
        try
        {
            var order = new PurchaseOrder
            {
                OrderNumber = $"BC-{DateTime.Now:yyyy}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}",
                OrderDate = DateTime.UtcNow,
                SupplierId = SelectedSupplier!.Id,
                QuotationId = SelectedQuotation?.Id,
                NeedId = SelectedQuotation?.NeedId,
                TotalAmountHT = TotalHT,
                TotalVAT = TotalVAT,
                TotalAmountTTC = TotalTTC,
                Status = PurchaseOrderStatus.Pending,
                CreatedById = _userSession.CurrentUser?.Id ?? 1,
                Observations = Observations,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var item in Items.Where(i => i.IsIncluded))
            {
                order.Details.Add(new PurchaseOrderDetail
                {
                    ProductId = item.Detail.ProductId,
                    Quantity = item.Quantity,
                    UnitPriceHT = item.UnitPriceHT,
                    UnitPriceTTC = item.UnitPriceTTC
                });
            }

            await _unitOfWork.PurchaseOrders.AddAsync(order);

            await _unitOfWork.CompleteAsync();

            System.Windows.MessageBox.Show($"Le Bon de Commande {order.OrderNumber} a été généré avec succès.", "Succès", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

            ExecuteBackToHistory();
            await LoadInitialData();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de la création du BC : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteCancel(PurchaseOrder? order)
    {
        if (order == null || order.Status != PurchaseOrderStatus.Pending) return;

        // Show confirmation dialog
        var result = System.Windows.MessageBox.Show(
            $"Êtes-vous sûr de vouloir annuler le Bon de Commande {order.OrderNumber} ?", 
            "Confirmation d'annulation", 
            System.Windows.MessageBoxButton.YesNo, 
            System.Windows.MessageBoxImage.Question);
            
        if (result != System.Windows.MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            order.Status = PurchaseOrderStatus.Cancelled;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.PurchaseOrders.Update(order);
            await _unitOfWork.CompleteAsync();
            await LoadInitialData();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de l'annulation : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteReactivate(PurchaseOrder? order)
    {
        if (order == null || order.Status != PurchaseOrderStatus.Cancelled) return;

        IsBusy = true;
        try
        {
            order.Status = PurchaseOrderStatus.Pending;
            order.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.PurchaseOrders.Update(order);
            await _unitOfWork.CompleteAsync();
            await LoadInitialData();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de la réactivation : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }


}

