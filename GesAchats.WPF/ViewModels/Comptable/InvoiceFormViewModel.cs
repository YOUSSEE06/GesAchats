using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using Microsoft.Win32;

namespace GesAchats.WPF.ViewModels.Comptable;

public class InvoiceFormViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IUserSession _userSession;
    private readonly IFileStorageService _fileStorageService;
    private Task? _initializationTask;

    public async void OnNavigatedTo(object parameter)
    {
        IsBusy = true;
        try 
        {
            // Attendre l'initialisation si nécessaire pour avoir la liste des BL
            await EnsureInitializedAsync();

            if (parameter is DeliveryNote dn)
            {
                // Rechercher le BL dans la liste déjà chargée (qui inclut les détails)
                var fullDn = DeliveryNotes.FirstOrDefault(d => d.Id == dn.Id);
                
                // Si pas dans la liste (ex: déjà facturé mais on y accède par lien direct)
                if (fullDn == null)
                {
                    var allWithDetails = await _unitOfWork.DeliveryNotes.GetAllWithDetailsAsync();
                    fullDn = allWithDetails.FirstOrDefault(d => d.Id == dn.Id);
                    
                    if (fullDn != null)
                    {
                        DeliveryNotes.Add(fullDn);
                    }
                }

                if (fullDn != null)
                {
                    // Sélectionner le BL (ceci déclenchera LoadLineItemsFromDeliveryNote via le setter)
                    SelectedDeliveryNote = fullDn;
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private Invoice _invoice = new() { InvoiceDate = DateTime.Now, DueDate = DateTime.Now.AddMonths(1) };
    public Invoice Invoice
    {
        get => _invoice;
        set => SetProperty(ref _invoice, value);
    }

    private ObservableCollection<DeliveryNote> _deliveryNotes = new();
    public ObservableCollection<DeliveryNote> DeliveryNotes
    {
        get => _deliveryNotes;
        set => SetProperty(ref _deliveryNotes, value);
    }

    private ObservableCollection<InvoiceDetail> _invoiceDetails = new();
    public ObservableCollection<InvoiceDetail> InvoiceDetails
    {
        get => _invoiceDetails;
        set => SetProperty(ref _invoiceDetails, value);
    }

    private DeliveryNote? _selectedDeliveryNote;
    public DeliveryNote? SelectedDeliveryNote
    {
        get => _selectedDeliveryNote;
        set
        {
            if (SetProperty(ref _selectedDeliveryNote, value))
            {
                if (value != null)
                {
                    Invoice.DeliveryNoteId = value.Id;
                    Invoice.PurchaseOrderId = value.PurchaseOrderId;
                    Invoice.SupplierId = value.SupplierId;
                    
                    // TVA automatiquement récupérée du BC
                    _taxRate = value.PurchaseOrder?.TotalAmountHT > 0 
                        ? (value.PurchaseOrder.TotalVAT / value.PurchaseOrder.TotalAmountHT) * 100 
                        : 20.00m;
                    
                    // Auto-remplissage des lignes depuis le BL
                    LoadLineItemsFromDeliveryNote(value);
                    
                    OnPropertyChanged(nameof(SelectedPurchaseOrder));
                    OnPropertyChanged(nameof(SelectedSupplier));
                    OnPropertyChanged(nameof(TaxRate));
                }
                else
                {
                    Invoice.DeliveryNoteId = null;
                    Invoice.PurchaseOrderId = null;
                    Invoice.SupplierId = 0;
                    InvoiceDetails.Clear();
                    _taxRate = 0;
                }
                CalculateTotals();
            }
        }
    }

    public PurchaseOrder? SelectedPurchaseOrder => SelectedDeliveryNote?.PurchaseOrder;
    public Supplier? SelectedSupplier => SelectedDeliveryNote?.Supplier;

    private decimal _taxRate;
    public decimal TaxRate => _taxRate;

    public decimal SousTotalHT => Invoice.AmountHT;
    public decimal MontantTVA => Invoice.TaxAmount;
    public decimal TotalTTC => Invoice.AmountTTC;

    private string? _attachedFileName;
    public string? AttachedFileName
    {
        get => _attachedFileName;
        set => SetProperty(ref _attachedFileName, value);
    }

    private string? _attachedFilePath;
    public string? AttachedFilePath
    {
        get => _attachedFilePath;
        set => SetProperty(ref _attachedFilePath, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand UploadFileCommand { get; }

    public InvoiceFormViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IUserSession userSession, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _userSession = userSession;
        _fileStorageService = fileStorageService;
        Title = "Nouvelle Facture";

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => _navigationService.NavigateTo("Factures"));
        UploadFileCommand = new RelayCommand(async _ => await ExecuteUploadFileAsync());

        _initializationTask = InitializeAsync();
    }

    private Task EnsureInitializedAsync()
    {
        return _initializationTask ??= InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var deliveryNotes = await _unitOfWork.DeliveryNotes.GetAllWithDetailsAsync();
            var invoices = await _unitOfWork.Invoices.GetAllAsync();
            
            // On affiche seulement les BL qui ne sont pas déjà liés à une facture
            var usedDeliveryNoteIds = invoices.Where(i => i.DeliveryNoteId.HasValue).Select(i => i.DeliveryNoteId!.Value).ToList();
            var availableDeliveryNotes = deliveryNotes.Where(dn => !usedDeliveryNoteIds.Contains(dn.Id)).ToList();
            
            DeliveryNotes = new ObservableCollection<DeliveryNote>(availableDeliveryNotes);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void LoadLineItemsFromDeliveryNote(DeliveryNote bl)
    {
        InvoiceDetails.Clear();
        foreach (var blDetail in bl.Details)
        {
            var invoiceDetail = new InvoiceDetail
            {
                ProductId = blDetail.ProductId,
                Product = blDetail.Product,
                Quantity = blDetail.QuantityReceived,
                UnitPriceHT = blDetail.UnitPriceHT,
                TaxRate = TaxRate,
                TotalHT = blDetail.QuantityReceived * blDetail.UnitPriceHT
            };
            invoiceDetail.TotalTTC = invoiceDetail.TotalHT * (1 + invoiceDetail.TaxRate / 100);
            InvoiceDetails.Add(invoiceDetail);
        }
    }

    private void CalculateTotals()
    {
        decimal ht = 0;
        foreach (var detail in InvoiceDetails)
        {
            detail.TaxRate = TaxRate;
            detail.TotalHT = detail.Quantity * detail.UnitPriceHT;
            detail.TotalTTC = detail.TotalHT * (1 + detail.TaxRate / 100);
            ht += detail.TotalHT;
        }

        Invoice.AmountHT = ht;
        Invoice.TaxAmount = ht * (TaxRate / 100);
        Invoice.AmountTTC = ht + Invoice.TaxAmount;

        OnPropertyChanged(nameof(SousTotalHT));
        OnPropertyChanged(nameof(MontantTVA));
        OnPropertyChanged(nameof(TotalTTC));
        OnPropertyChanged(nameof(Invoice));
    }

    private async Task ExecuteUploadFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Fichiers PDF et Images (*.pdf;*.jpg;*.jpeg;*.png;*.webp;*.bmp)|*.pdf;*.jpg;*.jpeg;*.png;*.webp;*.bmp",
            Title = "Sélectionner la facture"
        };

        if (dialog.ShowDialog() == true)
        {
            AttachedFilePath = dialog.FileName;
            AttachedFileName = Path.GetFileName(dialog.FileName);
            try
            {
                // Fichier importé -> envoyé sur Supabase Storage (référence distante)
                string reference = await _fileStorageService.UploadImportedFileAsync(
                    "factures", "FACT", dialog.FileName);
                Invoice.FilePath = reference;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Impossible de joindre le fichier : {ex.Message}",
                    "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                AttachedFilePath = null;
                AttachedFileName = string.Empty;
                Invoice.FilePath = null;
            }
        }
    }

    private bool CanSave()
    {
        return SelectedDeliveryNote != null && 
               !string.IsNullOrWhiteSpace(Invoice.ExternalInvoiceNumber) && 
               Invoice.InvoiceDate != default &&
               !string.IsNullOrEmpty(AttachedFilePath);
    }

    private async Task SaveAsync()
    {
        if (!CanSave()) return;

        IsBusy = true;
        try
        {
            Invoice.InvoiceNumber = $"FAC-{DateTime.Now:yyyy}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            Invoice.CreatedAt = DateTime.UtcNow;
            Invoice.UpdatedAt = DateTime.UtcNow;
            Invoice.Status = "EnAttente";
            Invoice.ConformityStatus = "NonVerifiee";
            Invoice.TaxRate = TaxRate;

            // cree_par -> Users.Id : toujours depuis la session (jamais de fallback 1 :
            // aucun utilisateur avec Id=1 n'existe, la FK serait violée).
            if (_userSession.CurrentUser == null)
            {
                throw new InvalidOperationException("Session utilisateur introuvable : veuillez vous reconnecter.");
            }
            Invoice.CreatedById = _userSession.CurrentUser.Id;

            Invoice.Details.Clear();
            foreach (var detail in InvoiceDetails)
            {
                Invoice.Details.Add(detail);
            }

            await _unitOfWork.Invoices.AddAsync(Invoice);
            
            // Update BL status to Valide
            if (SelectedDeliveryNote != null)
            {
                var blToUpdate = await _unitOfWork.DeliveryNotes.GetByIdAsync(SelectedDeliveryNote.Id);
                if (blToUpdate != null)
                {
                    blToUpdate.Status = "Valide";
                    blToUpdate.UpdatedAt = DateTime.UtcNow;
                }
            }
            
            await _unitOfWork.CompleteAsync();
            
            // Show custom success modal
            var modal = new Views.Components.AlertModalWindow
            {
                Message = "Facture enregistrée avec succès !",
                AlertType = Views.Components.AlertType.Success
            };
            modal.ShowDialog();
            
            _navigationService.NavigateTo("Factures");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving invoice: {ex.Message}");
            System.Windows.MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
