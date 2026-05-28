using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;
using System.Diagnostics;
using System.Windows;

namespace GesAchats.WPF.ViewModels.Comptable;

public class PaymentHistoryViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;
    private readonly IFileStorageService _fileStorageService;

    private ObservableCollection<Payment> _payments = new();
    public ObservableCollection<Payment> Payments
    {
        get => _payments;
        set => SetProperty(ref _payments, value);
    }

    private decimal _totalPaidMonth;
    public decimal TotalPaidMonth
    {
        get => _totalPaidMonth;
        set => SetProperty(ref _totalPaidMonth, value);
    }

    public ICommand LoadPaymentsCommand { get; }
    public ICommand ExportExcelCommand { get; }
    public ICommand AddPaymentCommand { get; }
    public ICommand ViewProofCommand { get; }

    public PaymentHistoryViewModel(IUnitOfWork unitOfWork, INavigationService navigationService, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        _fileStorageService = fileStorageService;
        Title = "Suivi des Règlements";

        LoadPaymentsCommand = new RelayCommand(async _ => await LoadPaymentsAsync());
        ExportExcelCommand = new RelayCommand(_ => ExportToExcel());
        AddPaymentCommand = new RelayCommand(_ => _navigationService.NavigateTo("PaymentForm"));
        ViewProofCommand = new RelayCommand(p => ViewProof(p as Payment));

        _ = LoadPaymentsAsync();
    }

    public void OnNavigatedTo(object parameter)
    {
    }

    private void ViewProof(Payment? payment)
    {
        if (payment == null || string.IsNullOrWhiteSpace(payment.ProofFilePath))
            return;

        try
        {
            string fullPath = _fileStorageService.GetFullPath(payment.ProofFilePath);
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

    private async Task LoadPaymentsAsync()
    {
        IsBusy = true;
        try
        {
            var payments = await _unitOfWork.Payments.GetAllIncludingAsync(
                p => p.Supplier,
                p => p.Invoice,
                p => p.CreatedBy
            );
            Payments = new ObservableCollection<Payment>(payments.OrderByDescending(p => p.PaymentDate));
            
            TotalPaidMonth = Payments
                .Where(p => p.PaymentDate.Month == DateTime.Now.Month && p.PaymentDate.Year == DateTime.Now.Year)
                .Sum(p => p.AmountPaid);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ExportToExcel()
    {
        // TODO: Implémenter ExportService
    }
}
