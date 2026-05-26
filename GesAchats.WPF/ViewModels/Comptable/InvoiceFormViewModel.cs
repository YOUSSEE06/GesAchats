using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using Microsoft.Win32;

namespace GesAchats.WPF.ViewModels.Comptable;

public class InvoiceFormViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;

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
                    
                    // TVA automatically retrieved from PurchaseOrder
                    _taxRate = value.PurchaseOrder.TotalAmountHT > 0 
                        ? (value.PurchaseOrder.TotalVAT / value.PurchaseOrder.TotalAmountHT) * 100 
                        : 20.00m;
                    
                    // Auto-fill line items from DeliveryNote
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

    public InvoiceFormViewModel(IUnitOfWork unitOfWork, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        Title = "Nouvelle Facture";

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => _navigationService.NavigateTo("Factures"));
        UploadFileCommand = new RelayCommand(_ => ExecuteUploadFile());

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            var deliveryNotes = await _unitOfWork.DeliveryNotes.GetAllWithDetailsAsync();
            DeliveryNotes = new ObservableCollection<DeliveryNote>(deliveryNotes);
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

    private void ExecuteUploadFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Fichiers PDF et Images (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png",
            Title = "Sélectionner la facture"
        };

        if (dialog.ShowDialog() == true)
        {
            AttachedFilePath = dialog.FileName;
            AttachedFileName = Path.GetFileName(dialog.FileName);
            Invoice.FilePath = AttachedFilePath;
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
        IsBusy = true;
        try
        {
            Invoice.InvoiceNumber = $"FAC-{DateTime.Now:yyyy}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
            Invoice.CreatedAt = DateTime.UtcNow;
            Invoice.UpdatedAt = DateTime.UtcNow;
            Invoice.Status = "EnAttente";
            Invoice.ConformityStatus = "NonVerifiee";
            Invoice.TaxRate = TaxRate;

            Invoice.Details.Clear();
            foreach (var detail in InvoiceDetails)
            {
                Invoice.Details.Add(detail);
            }

            await _unitOfWork.Invoices.AddAsync(Invoice);
            await _unitOfWork.CompleteAsync();
            
            _navigationService.NavigateTo("Factures");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving invoice: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
