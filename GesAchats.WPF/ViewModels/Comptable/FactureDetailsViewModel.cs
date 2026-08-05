using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Windows;

namespace GesAchats.WPF.ViewModels.Comptable;

public class FactureDetailsViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileStorageService _fileStorageService;
    private readonly int _invoiceId;
    private Invoice? _invoice;

    public Invoice? Invoice
    {
        get => _invoice;
        set => SetProperty(ref _invoice, value);
    }

    public ObservableCollection<InvoiceDetail> Details { get; } = new();

    public ICommand CloseCommand { get; }
    public ICommand ViewJustificatifCommand { get; }
    public event EventHandler? RequestClose;

    public FactureDetailsViewModel(IUnitOfWork unitOfWork, int invoiceId, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _invoiceId = invoiceId;
        _fileStorageService = fileStorageService;

        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke(this, EventArgs.Empty));
        ViewJustificatifCommand = new RelayCommand(async _ => await ExecuteViewJustificatifAsync());

        _ = LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var invoices = await _unitOfWork.Invoices.GetAllIncludingAsync(
                i => i.Supplier,
                i => i.PurchaseOrder,
                i => i.DeliveryNote,
                i => i.Details
            );
            Invoice = invoices.FirstOrDefault(i => i.Id == _invoiceId);

            if (Invoice != null)
            {
                Details.Clear();
                foreach (var detail in Invoice.Details)
                    Details.Add(detail);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteViewJustificatifAsync()
    {
        if (Invoice == null || string.IsNullOrWhiteSpace(Invoice.FilePath))
            return;

        try
        {
            string fullPath = await _fileStorageService.GetFullPathAsync(Invoice.FilePath);
            if (!System.IO.File.Exists(fullPath))
            {
                MessageBox.Show($"Fichier introuvable : {fullPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'ouverture du fichier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
