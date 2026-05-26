using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.ViewModels.Comptable;

public class PaymentHistoryViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;

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

    public PaymentHistoryViewModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Title = "Suivi des Règlements";

        LoadPaymentsCommand = new RelayCommand(async _ => await LoadPaymentsAsync());
        ExportExcelCommand = new RelayCommand(_ => ExportToExcel());

        _ = LoadPaymentsAsync();
    }

    private async Task LoadPaymentsAsync()
    {
        IsBusy = true;
        try
        {
            var payments = await _unitOfWork.Payments.GetAllAsync();
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
