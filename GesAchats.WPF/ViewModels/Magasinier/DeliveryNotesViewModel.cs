using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.Generic;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class DeliveryNoteListItemViewModel : BaseViewModel
{
    public DeliveryNote DeliveryNote { get; }
    public string? InvoiceNumber { get; set; }
    public bool HasInvoice => !string.IsNullOrEmpty(InvoiceNumber);
    public bool IsComptable { get; set; }

    public DeliveryNoteListItemViewModel(DeliveryNote deliveryNote, string? invoiceNumber = null, bool isComptable = false)
    {
        DeliveryNote = deliveryNote;
        InvoiceNumber = invoiceNumber;
        IsComptable = isComptable;
    }
}

public class DeliveryNotesViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;
    private readonly INavigationService _navigationService;
    private Task? _initializationTask;
    
    // View State
    private bool _isHistoryView = true;
    private bool _isEditView = false;
    private bool _isInspectView = false;

    public bool IsComptable => _userSession.HasRole("COMPTABLE");
    public bool IsMagasinier => _userSession.HasRole("MAGASINIER");

    // Filter properties
    private string _searchText = string.Empty;
    private string _selectedSupplier = "Tous";
    private string _selectedStatus = "Tous";
    private DateTime? _searchDate;
    public ObservableCollection<string> SupplierOptions { get; } = new ObservableCollection<string>();
    public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string> { "Tous", "En attente", "Validé" };
    
    // Statistics properties
    private int _totalDeliveries;
    private int _pendingDeliveries;
    private int _validatedDeliveries;

    public int TotalDeliveries
    {
        get => _totalDeliveries;
        set => SetProperty(ref _totalDeliveries, value);
    }

    public int PendingDeliveries
    {
        get => _pendingDeliveries;
        set => SetProperty(ref _pendingDeliveries, value);
    }

    public int ValidatedDeliveries
    {
        get => _validatedDeliveries;
        set => SetProperty(ref _validatedDeliveries, value);
    }

    // Form properties
    private DateTime _receptionDate = DateTime.Today;
    private string _deliveryNoteNumber = string.Empty;
    private PurchaseOrder? _selectedPurchaseOrder;
    private string _observations = string.Empty;
    private string _attachedFilePath = "Aucun fichier sélectionné";
    private string _supplierName = string.Empty;
    private DeliveryNote? _selectedDeliveryNote;

    private List<DeliveryNoteListItemViewModel> _allDeliveries = new List<DeliveryNoteListItemViewModel>();

    // State Accessors
    public bool IsHistoryView 
    { 
        get => _isHistoryView; 
        set 
        {
            if (SetProperty(ref _isHistoryView, value))
            {
                OnPropertyChanged(nameof(CanAddBL));
            }
        } 
    }
    public bool IsEditView { get => _isEditView; set => SetProperty(ref _isEditView, value); }
    public bool IsInspectView { get => _isInspectView; set => SetProperty(ref _isInspectView, value); }

    public bool CanAddBL => IsHistoryView && IsMagasinier;

    // Filter Accessors
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterDeliveries();
        }
    }

    public string SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
                FilterDeliveries();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
                FilterDeliveries();
        }
    }

    public DateTime? SearchDate { get => _searchDate; set { if (SetProperty(ref _searchDate, value)) FilterDeliveries(); } }

    // Form Accessors
    public DateTime ReceptionDate { get => _receptionDate; set => SetProperty(ref _receptionDate, value); }
    public string DeliveryNoteNumber { get => _deliveryNoteNumber; set => SetProperty(ref _deliveryNoteNumber, value); }
    public PurchaseOrder? SelectedPurchaseOrder 
    { 
        get => _selectedPurchaseOrder; 
        set 
        { 
            if (SetProperty(ref _selectedPurchaseOrder, value)) 
            {
                OnPurchaseOrderChanged();
            }
        } 
    }
    public string Observations { get => _observations; set => SetProperty(ref _observations, value); }
    public string AttachedFilePath { get => _attachedFilePath; set => SetProperty(ref _attachedFilePath, value); }
    public string SupplierName { get => _supplierName; set => SetProperty(ref _supplierName, value); }
    
    public DeliveryNote? SelectedDeliveryNote
    {
        get => _selectedDeliveryNote;
        set => SetProperty(ref _selectedDeliveryNote, value);
    }

    public ObservableCollection<PurchaseOrder> PurchaseOrders { get; } = new ObservableCollection<PurchaseOrder>();
    public ObservableCollection<DeliveryNoteItemViewModel> DeliveryItems { get; } = new ObservableCollection<DeliveryNoteItemViewModel>();
    public ObservableCollection<DeliveryNoteListItemViewModel> DeliveriesHistory { get; } = new ObservableCollection<DeliveryNoteListItemViewModel>();

    // Commands
    public ICommand SelectFileCommand { get; }
    public ICommand ValidateCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ShowAddFormCommand { get; }
    public ICommand BackToListCommand { get; }
    public ICommand InspectCommand { get; }
    public ICommand PrintPdfCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand AddInvoiceCommand { get; }

    public DeliveryNotesViewModel(IUnitOfWork unitOfWork, IUserSession userSession, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
        _navigationService = navigationService;
        Title = "Réception des Bons de Livraison";
        
        SelectFileCommand = new RelayCommand(_ => ExecuteSelectFile());
        ValidateCommand = new RelayCommand(async _ => await ExecuteValidate(), _ => CanValidate());
        RefreshCommand = new RelayCommand(async _ => await LoadInitialData());
        ShowAddFormCommand = new RelayCommand(_ => ExecuteShowAddForm());
        BackToListCommand = new RelayCommand(async _ => await ExecuteBackToList());
        InspectCommand = new RelayCommand(async p => await ExecuteInspect((p as DeliveryNoteListItemViewModel)?.DeliveryNote));
        PrintPdfCommand = new RelayCommand(p => ExecuteOpenOriginalFile((p as DeliveryNoteListItemViewModel)?.DeliveryNote));
        ResetFiltersCommand = new RelayCommand(_ => ExecuteResetFilters());
        AddInvoiceCommand = new RelayCommand(p => ExecuteAddInvoice(p as DeliveryNoteListItemViewModel));

        _initializationTask = LoadInitialData();
    }

    private Task EnsureInitializedAsync()
    {
        return _initializationTask ??= LoadInitialData();
    }

    private void ExecuteAddInvoice(DeliveryNoteListItemViewModel? item)
    {
        if (item != null)
        {
            _navigationService.NavigateTo("InvoiceForm", item.DeliveryNote);
        }
    }

    private void ExecuteResetFilters()
    {
        SearchText = string.Empty;
        SelectedSupplier = "Tous";
        SelectedStatus = "Tous";
        SearchDate = null;
    }

    private async Task LoadInitialData()
    {
        // On s'assure qu'une seule initialisation tourne à la fois
        if (IsBusy && _initializationTask != null && !_initializationTask.IsCompleted)
        {
            await _initializationTask;
            return;
        }

        IsBusy = true;
        try
        {
            await LoadPurchaseOrders();
            
            // Récupération optimisée avec les relations
            var deliveries = await _unitOfWork.DeliveryNotes.GetAllIncludingAsync(
                d => d.Supplier, 
                d => d.PurchaseOrder
            );
            
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            var invoiceMap = invoices.Where(i => i.DeliveryNoteId.HasValue)
                                    .GroupBy(i => i.DeliveryNoteId!.Value)
                                    .ToDictionary(g => g.Key, g => g.First().ExternalInvoiceNumber ?? g.First().InvoiceNumber);

            var newDeliveries = new List<DeliveryNoteListItemViewModel>();
            
            foreach (var d in deliveries.OrderByDescending(d => d.ReceptionDate))
            {
                string? invoiceNumber = invoiceMap.TryGetValue(d.Id, out var num) ? num : null;
                newDeliveries.Add(new DeliveryNoteListItemViewModel(d, invoiceNumber, IsComptable));
            }

            _allDeliveries = newDeliveries;

            // Load suppliers for filter options
            var allSuppliers = await _unitOfWork.Suppliers.GetAllAsync();
            var newSupplierOptions = new List<string> { "Tous" };
            var uniqueSupplierNames = allSuppliers
                .Select(s => s.CompanyName)
                .Distinct()
                .OrderBy(n => n);
            
            foreach (var name in uniqueSupplierNames)
            {
                newSupplierOptions.Add(name);
            }

            SupplierOptions.Clear();
            foreach(var opt in newSupplierOptions) SupplierOptions.Add(opt);

            FilterDeliveries();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Erreur chargement BL: {ex.Message}");
            System.Windows.MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterDeliveries()
    {
        var filtered = _allDeliveries.AsEnumerable();

        // Search filter: BL number OR Purchase Order number OR Invoice number
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(item => 
                item.DeliveryNote.DeliveryNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (item.DeliveryNote.PurchaseOrder != null && item.DeliveryNote.PurchaseOrder.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase)) ||
                (item.InvoiceNumber != null && item.InvoiceNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            );
        }

        // Supplier filter - Correction : Gérer le null et le vide
        if (!string.IsNullOrEmpty(SelectedSupplier) && SelectedSupplier != "Tous")
        {
            filtered = filtered.Where(item => item.DeliveryNote.Supplier != null && item.DeliveryNote.Supplier.CompanyName == SelectedSupplier);
        }

        // Status filter - Correction : Gérer le null et le vide
        if (!string.IsNullOrEmpty(SelectedStatus) && SelectedStatus != "Tous")
        {
            if (SelectedStatus == "En attente")
                filtered = filtered.Where(item => item.DeliveryNote.Status == "EnAttente");
            else if (SelectedStatus == "Validé")
                filtered = filtered.Where(item => item.DeliveryNote.Status == "Valide");
        }

        // Date filter
        if (SearchDate.HasValue)
        {
            filtered = filtered.Where(item => item.DeliveryNote.ReceptionDate.Date == SearchDate.Value.Date);
        }

        var filteredList = filtered.ToList();
        
        DeliveriesHistory.Clear();
        foreach (var d in filteredList)
        {
            DeliveriesHistory.Add(d);
        }

        // Calculate statistics
        TotalDeliveries = filteredList.Count;
        PendingDeliveries = filteredList.Count(item => item.DeliveryNote.Status == "EnAttente");
        ValidatedDeliveries = filteredList.Count(item => item.DeliveryNote.Status == "Valide");
    }

    private async Task LoadPurchaseOrders()
    {
        PurchaseOrders.Clear();
        var pos = await _unitOfWork.PurchaseOrders.FindAsync(p => 
            p.Status != PurchaseOrderStatus.Validated && 
            p.Status != "Delivered" && 
            p.Status != "Livré" && 
            p.Status != "Closed" && 
            p.Status != "Clôturé");

        foreach (var p in pos)
        {
            PurchaseOrders.Add(p);
        }
    }

    private void ExecuteShowAddForm()
    {
        ExecuteCancel(); // Reset form
        IsHistoryView = false;
        IsInspectView = false;
        IsEditView = true;
        Title = "Nouveau Bon de Livraison";
    }

    private async Task ExecuteBackToList()
    {
        IsEditView = false;
        IsInspectView = false;
        IsHistoryView = true;
        Title = "Réception des Bons de Livraison";

        // On charge les données après avoir affiché le conteneur pour assurer la stabilité
        await LoadInitialData();
    }

    private async Task ExecuteInspect(DeliveryNote? dn)
    {
        if (dn == null) return;

        IsBusy = true;
        try
        {
            SelectedDeliveryNote = dn;
            Observations = dn.Observations ?? string.Empty; // Correction : Synchroniser l'observation
            DeliveryNoteNumber = dn.DeliveryNumber; // Optionnel : Synchroniser aussi le numéro pour affichage
            
            // Charger les détails complets si nécessaire
            var details = await _unitOfWork.DeliveryNoteDetails.FindAsync(d => d.DeliveryNoteId == dn.Id);
            DeliveryItems.Clear();
            foreach (var detail in details)
            {
                if (detail.Product == null)
                    detail.Product = (await _unitOfWork.Products.GetByIdAsync(detail.ProductId))!;
                
                // On réutilise DeliveryNoteItemViewModel mais on pourrait en créer un dédié pour l'inspection
                // On passe une fausse commande de BC car on est en mode inspection
                var fakeBcDetail = new PurchaseOrderDetail { Product = detail.Product, Quantity = detail.QuantityOrdered, UnitPriceHT = detail.UnitPriceHT, UnitPriceTTC = detail.UnitPriceTTC };
                var itemVm = new DeliveryNoteItemViewModel(fakeBcDetail, () => { });
                itemVm.QuantityReceived = detail.QuantityReceived;
                itemVm.IsValidated = detail.IsValidated;
                
                DeliveryItems.Add(itemVm);
            }

            IsHistoryView = false;
            IsEditView = false;
            IsInspectView = true;
            Title = $"Détail BL {dn.DeliveryNumber}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExecuteOpenOriginalFile(DeliveryNote? dn)
    {
        if (dn == null || string.IsNullOrWhiteSpace(dn.FilePath))
        {
            System.Windows.MessageBox.Show("Aucun fichier n'est associé à ce Bon de Livraison.", "Fichier manquant", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        try
        {
            if (!System.IO.File.Exists(dn.FilePath))
            {
                System.Windows.MessageBox.Show($"Le fichier original est introuvable à l'emplacement suivant :\n{dn.FilePath}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var process = new Process();
            process.StartInfo = new ProcessStartInfo(dn.FilePath) { UseShellExecute = true };
            process.Start();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Impossible d'ouvrir le fichier : {ex.Message}", "Erreur système", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async void OnPurchaseOrderChanged()
    {
        DeliveryItems.Clear();
        SupplierName = string.Empty;

        if (SelectedPurchaseOrder == null) return;

        if (SelectedPurchaseOrder.Supplier == null)
            SelectedPurchaseOrder.Supplier = (await _unitOfWork.Suppliers.GetByIdAsync(SelectedPurchaseOrder.SupplierId))!;
        
        SupplierName = SelectedPurchaseOrder.Supplier?.CompanyName ?? "Inconnu";

        var details = await _unitOfWork.PurchaseOrderDetails.FindAsync(d => d.PurchaseOrderId == SelectedPurchaseOrder.Id);
        foreach (var detail in details)
        {
            if (detail.Product == null)
                detail.Product = (await _unitOfWork.Products.GetByIdAsync(detail.ProductId))!;
                
            DeliveryItems.Add(new DeliveryNoteItemViewModel(detail, () => CommandManager.InvalidateRequerySuggested()));
        }
    }

    private void ExecuteSelectFile()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Fichiers images et PDF (*.jpg, *.png, *.pdf)|*.jpg;*.png;*.pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            AttachedFilePath = dialog.FileName;
        }
    }

    private bool CanValidate()
    {
        return !string.IsNullOrWhiteSpace(DeliveryNoteNumber) && 
               SelectedPurchaseOrder != null && 
               DeliveryItems.Any() &&
               DeliveryItems.All(i => i.Error == null) &&
               AttachedFilePath != "Aucun fichier sélectionné";
    }

    private async Task ExecuteValidate()
    {
        IsBusy = true;
        try
        {
            if (IsInspectView && SelectedDeliveryNote != null)
            {
                // MODE MISE À JOUR (UPDATE)
                // On met à jour uniquement les observations dans cet exemple, 
                // car les autres champs sont en lecture seule en mode inspection.
                var dnToUpdate = await _unitOfWork.DeliveryNotes.GetByIdAsync(SelectedDeliveryNote.Id);
                if (dnToUpdate != null)
                {
                    dnToUpdate.Observations = Observations;
                    dnToUpdate.UpdatedAt = DateTime.UtcNow;
                    await _unitOfWork.CompleteAsync();
                    System.Windows.MessageBox.Show("Modifications enregistrées avec succès !", "Succès", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                    await ExecuteBackToList();
                }
                return;
            }

            // MODE CRÉATION (INSERT)
            
            // 1. Vérification préventive du doublon de numéro de BL
            var existingBL = await _unitOfWork.DeliveryNotes.FindAsync(d => d.DeliveryNumber == DeliveryNoteNumber);
            if (existingBL.Any())
            {
                System.Windows.MessageBox.Show($"Le numéro de Bon de Livraison '{DeliveryNoteNumber}' est déjà utilisé. Veuillez en saisir un autre.", 
                    "Doublon détecté", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }

            string finalFilePath = AttachedFilePath;

            if (!string.IsNullOrEmpty(AttachedFilePath) && System.IO.File.Exists(AttachedFilePath))
            {
                try
                {
                    string storageDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "BL");
                    if (!System.IO.Directory.Exists(storageDir))
                        System.IO.Directory.CreateDirectory(storageDir);

                    string fileName = $"BL_{DeliveryNoteNumber}_{DateTime.Now:yyyyMMddHHmmss}{System.IO.Path.GetExtension(AttachedFilePath)}";
                    string destinationPath = System.IO.Path.Combine(storageDir, fileName);
                    
                    System.IO.File.Copy(AttachedFilePath, destinationPath, true);
                    finalFilePath = destinationPath;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erreur lors de la copie du fichier : {ex.Message}");
                }
            }

            var dn = new DeliveryNote
            {
                DeliveryNumber = DeliveryNoteNumber,
                ReceptionDate = DateTime.SpecifyKind(ReceptionDate, DateTimeKind.Utc),
                SupplierId = SelectedPurchaseOrder!.SupplierId,
                PurchaseOrderId = SelectedPurchaseOrder!.Id,
                Observations = Observations,
                FilePath = finalFilePath,
                Status = "EnAttente",
                ReceivedById = _userSession.CurrentUser?.Id ?? 1,
                ReceivedQuantity = DeliveryItems.Sum(i => i.QuantityReceived),
                CompliantQuantity = DeliveryItems.Where(i => i.IsValidated).Sum(i => i.QuantityReceived),
                DefectiveQuantity = DeliveryItems.Where(i => !i.IsValidated).Sum(i => i.QuantityReceived),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            foreach (var item in DeliveryItems)
            {
                dn.Details.Add(new DeliveryNoteDetail
                {
                    ProductId = item.OrderDetail.ProductId,
                    QuantityOrdered = item.QuantityOrdered,
                    QuantityReceived = item.QuantityReceived,
                    UnitPriceHT = item.UnitPriceHT,
                    UnitPriceTTC = item.UnitPriceTTC,
                    Total = item.Total,
                    IsValidated = item.IsValidated
                });

                var product = await _unitOfWork.Products.GetByIdAsync(item.OrderDetail.ProductId);
                if (product != null)
                {
                    product.CurrentStock += item.QuantityReceived;
                    product.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (dn.Status == "FullyReceived")
                SelectedPurchaseOrder.Status = PurchaseOrderStatus.Validated;
            else
                SelectedPurchaseOrder.Status = PurchaseOrderStatus.Validated;

            await _unitOfWork.DeliveryNotes.AddAsync(dn);
            await _unitOfWork.CompleteAsync();

            await ExecuteBackToList();
        }
        catch (Exception ex)
        {
            var message = "Une erreur inattendue est survenue lors de l'enregistrement.";
            
            // Détection spécifique de l'erreur de doublon PostgreSQL au cas où la vérification préventive échouerait (concurrence)
            if (ex.InnerException?.Message.Contains("23505") == true || ex.Message.Contains("23505"))
            {
                message = $"Le numéro de BL '{DeliveryNoteNumber}' est déjà utilisé. Veuillez en choisir un autre.";
            }
            else if (ex.InnerException != null)
            {
                message = ex.InnerException.Message;
            }
            else
            {
                message = ex.Message;
            }
                
            System.Windows.MessageBox.Show(message, "Erreur de Sauvegarde", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExecuteCancel()
    {
        SelectedDeliveryNote = null; // Important : réinitialiser le BL sélectionné
        DeliveryNoteNumber = string.Empty;
        SelectedPurchaseOrder = null;
        DeliveryItems.Clear();
        Observations = string.Empty;
        AttachedFilePath = "Aucun fichier sélectionné";
        SupplierName = string.Empty;
    }
}