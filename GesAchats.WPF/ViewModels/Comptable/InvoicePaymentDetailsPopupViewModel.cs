using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Comptable;

public class InvoicePaymentDetailsPopupViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly InvoiceWithPaymentsViewModel _invoiceVm;

    public Invoice Invoice => _invoiceVm.Invoice;
    public ObservableCollection<Payment> Payments => _invoiceVm.Payments;

    public decimal TotalPayments => _invoiceVm.TotalPayments;
    public decimal Balance => _invoiceVm.Balance;
    public string StatusCalculated => _invoiceVm.StatusCalculated;
    public string StatusColor => _invoiceVm.StatusColor;

    public string? NewPaymentMethod { get; set; } = "Virement";
    public DateTime NewPaymentDate { get; set; } = DateTime.Now;
    public decimal NewPaymentAmount { get; set; }
    public string? NewPaymentReference { get; set; }

    public ObservableCollection<string> PaymentMethods { get; set; } = new()
    {
        "Espèces", "Virement", "Chèque", "Carte bancaire"
    };

    public ICommand AddPaymentCommand { get; }
    public ICommand DeletePaymentCommand { get; }
    public ICommand ViewProofCommand { get; }
    public ICommand CloseCommand { get; }

    public InvoicePaymentDetailsPopupViewModel(InvoiceWithPaymentsViewModel invoiceVm, IUnitOfWork unitOfWork, IFileStorageService fileStorageService)
    {
        _invoiceVm = invoiceVm;
        _unitOfWork = unitOfWork;
        _fileStorageService = fileStorageService;

        Title = "Détails Facture et Règlements";

        AddPaymentCommand = new RelayCommand(async _ => await AddPaymentAsync(), _ => CanAddPayment());
        DeletePaymentCommand = new RelayCommand(async param => await DeletePaymentAsync(param as Payment));
        ViewProofCommand = new RelayCommand(param => ViewProof(param as string));
        CloseCommand = new RelayCommand(_ => Close?.Invoke(this, true));
    }
    
    private void ViewProof(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return;
            
        try
        {
            string fullPath = _fileStorageService.GetFullPath(filePath);

            // Verify file exists
            if (!File.Exists(fullPath))
            {
                System.Windows.MessageBox.Show($"Fichier introuvable : {fullPath}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private bool CanAddPayment()
    {
        return NewPaymentAmount > 0 && NewPaymentAmount <= Balance;
    }

    private async System.Threading.Tasks.Task AddPaymentAsync()
    {
        IsBusy = true;
        try
        {
            var payment = new Payment
            {
                InvoiceId = Invoice.Id,
                SupplierId = Invoice.SupplierId,
                PaymentNumber = $"REG-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 4)}",
                PaymentDate = NewPaymentDate,
                AmountPaid = NewPaymentAmount,
                PaymentMethod = NewPaymentMethod ?? "Virement",
                ReferenceNumber = NewPaymentReference,
                Status = "Validé",
                CreatedById = 1, // TODO: Get current user
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Payments.AddAsync(payment);
            await _unitOfWork.CompleteAsync();

            Payments.Add(payment);
            _invoiceVm.RefreshCalculations();

            // Reset form
            NewPaymentAmount = 0;
            NewPaymentReference = string.Empty;
            NewPaymentDate = DateTime.Now;

            OnPropertyChanged(nameof(NewPaymentAmount));
            OnPropertyChanged(nameof(NewPaymentReference));
            OnPropertyChanged(nameof(NewPaymentDate));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async System.Threading.Tasks.Task DeletePaymentAsync(Payment? payment)
    {
        if (payment == null) return;

        IsBusy = true;
        try
        {
            _unitOfWork.Payments.Remove(payment);
            await _unitOfWork.CompleteAsync();

            Payments.Remove(payment);
            _invoiceVm.RefreshCalculations();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event EventHandler<bool>? Close;
}
