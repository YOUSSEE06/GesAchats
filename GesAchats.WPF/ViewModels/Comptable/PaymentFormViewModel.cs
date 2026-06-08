using System.Collections.ObjectModel;
using System.Windows.Input;
using System.IO;
using System.Linq;
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

    private ObservableCollection<InvoiceWithPaymentsViewModel> _invoices = new();
    public ObservableCollection<InvoiceWithPaymentsViewModel> Invoices
    {
        get => _invoices;
        set => SetProperty(ref _invoices, value);
    }

    private InvoiceWithPaymentsViewModel? _selectedInvoice;
    public InvoiceWithPaymentsViewModel? SelectedInvoice
    {
        get => _selectedInvoice;
        set
        {
            if (SetProperty(ref _selectedInvoice, value))
            {
                if (value != null)
                {
                    Payment.InvoiceId = value.Invoice.Id;
                    Payment.SupplierId = value.Invoice.SupplierId;
                    Payment.AmountPaid = value.Balance; // Default to remaining balance
                    OnPropertyChanged(nameof(Payment));
                }
            }
        }
    }

    private Payment _payment = new() { PaymentDate = DateTime.Now, PaymentMethod = "Virement" };
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
        CancelCommand = new RelayCommand(_ => _navigationService.NavigateTo("Reglements"));
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is int id)
        {
            await LoadInvoicesAsync();
            SelectedInvoice = Invoices.FirstOrDefault(i => i.Invoice.Id == id);
        }
        else
        {
            await LoadInvoicesAsync();
        }
    }

    private async Task LoadInvoicesAsync()
    {
        IsBusy = true;
        try
        {
            var invoices = await _unitOfWork.Invoices.GetAllIncludingAsync(i => i.Supplier);
            var payments = await _unitOfWork.Payments.GetAllAsync();
            
            var invoiceViewModels = new List<InvoiceWithPaymentsViewModel>();
            foreach (var invoice in invoices)
            {
                var vm = new InvoiceWithPaymentsViewModel(invoice);
                var invoicePayments = payments.Where(p => p.InvoiceId == invoice.Id);
                foreach (var payment in invoicePayments)
                {
                    vm.Payments.Add(payment);
                }
                invoiceViewModels.Add(vm);
            }
            
            // Keep only invoices that are not fully paid
            Invoices = new ObservableCollection<InvoiceWithPaymentsViewModel>(
                invoiceViewModels.Where(i => i.StatusCalculated != "Payée").OrderByDescending(i => i.Invoice.InvoiceDate));
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
        return SelectedInvoice != null &&
               Payment.AmountPaid > 0 && 
               Payment.AmountPaid <= SelectedInvoice.Balance &&
               !string.IsNullOrEmpty(Payment.PaymentMethod);
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
            Payment.Status = "Validé"; // Add default status
            
            // Clear navigation properties to avoid EF tracking issues
            Payment.Invoice = null!;
            Payment.Supplier = null!;
            Payment.CreatedBy = null!;

            await _unitOfWork.Payments.AddAsync(Payment);

            // Mettre à jour le statut de la facture si nécessaire
            if (SelectedInvoice != null)
            {
                // Calculate total payments including this new one
                var totalPayments = SelectedInvoice.TotalPayments + Payment.AmountPaid;
                
                // Get the tracked entity from DB to update
                var invoiceToUpdate = await _unitOfWork.Invoices.GetByIdAsync(SelectedInvoice.Invoice.Id);
                if (invoiceToUpdate != null)
                {
                    if (totalPayments >= SelectedInvoice.Invoice.AmountTTC)
                    {
                        invoiceToUpdate.Status = "Payée";
                    }
                    else
                    {
                        invoiceToUpdate.Status = "Partiellement payée";
                    }
                    _unitOfWork.Invoices.Update(invoiceToUpdate);
                }
            }

            await _unitOfWork.CompleteAsync();
            
            // Show custom success modal
            var modal = new Views.Components.AlertModalWindow
            {
                Message = "Règlement enregistré avec succès !",
                AlertType = Views.Components.AlertType.Success
            };
            modal.ShowDialog();

            // Générer le reçu PDF
            var receiptPath = await _pdfGeneratorService.GeneratePaymentReceiptPdfAsync(Payment);
            Payment.ReceiptFilePath = await _fileStorageService.SavePaymentReceiptAsync(Payment.SupplierId, Payment.InvoiceId, receiptPath);
            await _unitOfWork.CompleteAsync();

            _navigationService.NavigateTo("Reglements");
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de la sauvegarde:\n{ex.Message}\n\nInner Exception:\n{ex.InnerException?.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
