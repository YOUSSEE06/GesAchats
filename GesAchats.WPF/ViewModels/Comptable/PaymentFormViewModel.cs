using System.Windows.Input;
using System.IO;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using Microsoft.Win32;

namespace GesAchats.WPF.ViewModels.Comptable;

public class PaymentFormViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IFileStorageService _fileStorageService;
    private readonly IPdfGeneratorService _pdfGeneratorService;

    private int _invoiceId;
    private Invoice? _invoice;
    public Invoice? Invoice
    {
        get => _invoice;
        set => SetProperty(ref _invoice, value);
    }

    private Payment _payment = new() { PaymentDate = DateTime.Now };
    public Payment Payment
    {
        get => _payment;
        set => SetProperty(ref _payment, value);
    }

    private string? _selectedFilePath;
    public string? SelectedFilePath
    {
        get => _selectedFilePath;
        set => SetProperty(ref _selectedFilePath, value);
    }

    public ICommand BrowseCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public PaymentFormViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IFileStorageService fileStorageService, IPdfGeneratorService pdfGeneratorService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _fileStorageService = fileStorageService;
        _pdfGeneratorService = pdfGeneratorService;
        Title = "Enregistrer un Règlement";

        BrowseCommand = new RelayCommand(_ => BrowseFile());
        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave());
        CancelCommand = new RelayCommand(_ => _navigationService.NavigateTo("Factures"));
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is int id)
        {
            _invoiceId = id;
            await LoadInvoiceAsync();
        }
    }

    private async Task LoadInvoiceAsync()
    {
        IsBusy = true;
        try
        {
            Invoice = await _unitOfWork.Invoices.GetByIdAsync(_invoiceId);
            if (Invoice != null)
            {
                Payment.InvoiceId = Invoice.Id;
                Payment.SupplierId = Invoice.SupplierId;
                Payment.AmountPaid = Invoice.AmountTTC;
                OnPropertyChanged(nameof(Payment));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void BrowseFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Fichiers images et PDF (*.pdf, *.jpg, *.png)|*.pdf;*.jpg;*.png"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            SelectedFilePath = openFileDialog.FileName;
        }
    }

    private bool CanSave()
    {
        return Payment.AmountPaid > 0 && !string.IsNullOrEmpty(Payment.PaymentMethod);
    }

    private async Task SaveAsync()
    {
        IsBusy = true;
        try
        {
            // Sauvegarder la preuve si sélectionnée
            if (!string.IsNullOrEmpty(SelectedFilePath))
            {
                Payment.ProofFilePath = await _fileStorageService.SavePaymentProofAsync(Payment.SupplierId, Payment.InvoiceId, SelectedFilePath);
                Payment.FileType = Path.GetExtension(SelectedFilePath).TrimStart('.').ToLower();
            }

            Payment.PaymentNumber = $"REG-{DateTime.Now:yyyy}-{Guid.NewGuid().ToString().Substring(0, 4).ToUpper()}";
            Payment.CreatedAt = DateTime.UtcNow;
            Payment.UpdatedAt = DateTime.UtcNow;
            Payment.CreatedById = 1; // TODO: Get from current user session

            await _unitOfWork.Payments.AddAsync(Payment);

            // Mettre à jour le statut de la facture
            if (Invoice != null)
            {
                Invoice.Status = "Payée";
                _unitOfWork.Invoices.Update(Invoice);
            }

            await _unitOfWork.CompleteAsync();

            // Générer le reçu PDF
            var receiptPath = await _pdfGeneratorService.GeneratePaymentReceiptPdfAsync(Payment);
            Payment.ReceiptFilePath = await _fileStorageService.SavePaymentReceiptAsync(Payment.SupplierId, Payment.InvoiceId, receiptPath);
            await _unitOfWork.CompleteAsync();

            _navigationService.NavigateTo("Factures");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
