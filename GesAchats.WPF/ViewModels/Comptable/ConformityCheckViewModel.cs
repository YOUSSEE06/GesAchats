using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.ViewModels.Comptable;

public class ConformityCheckViewModel : BaseViewModel, INavigatable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConformityService _conformityService;
    private readonly INavigationService _navigationService;

    private int _invoiceId;
    private Invoice? _invoice;
    public Invoice? Invoice
    {
        get => _invoice;
        set => SetProperty(ref _invoice, value);
    }

    private ObservableCollection<ConformityResult> _results = new();
    public ObservableCollection<ConformityResult> Results
    {
        get => _results;
        set => SetProperty(ref _results, value);
    }

    private string _justification = string.Empty;
    public string Justification
    {
        get => _justification;
        set => SetProperty(ref _justification, value);
    }

    public ICommand ValidateCommand { get; }
    public ICommand RejectCommand { get; }
    public ICommand BackCommand { get; }

    public ConformityCheckViewModel(IUnitOfWork unitOfWork, IConformityService conformityService, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _conformityService = conformityService;
        _navigationService = navigationService;
        Title = "Vérification de Conformité";

        ValidateCommand = new RelayCommand(async _ => await ValidateAsync());
        RejectCommand = new RelayCommand(async _ => await RejectAsync());
        BackCommand = new RelayCommand(_ => _navigationService.NavigateTo("Factures"));
    }

    public async void OnNavigatedTo(object parameter)
    {
        if (parameter is int id)
        {
            _invoiceId = id;
            await LoadDataAsync();
        }
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            Invoice = await _unitOfWork.Invoices.GetByIdAsync(_invoiceId);
            var results = await _conformityService.CheckInvoiceConformityAsync(_invoiceId);
            Results = new ObservableCollection<ConformityResult>(results);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ValidateAsync()
    {
        if (await _conformityService.ValidateInvoiceAsync(_invoiceId, Justification))
            _navigationService.NavigateTo("Factures");
    }

    private async Task RejectAsync()
    {
        if (await _conformityService.RejectInvoiceAsync(_invoiceId, Justification))
            _navigationService.NavigateTo("Factures");
    }
}
