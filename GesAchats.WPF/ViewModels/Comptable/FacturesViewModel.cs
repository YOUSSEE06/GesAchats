using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.ViewModels.Comptable;

public class FacturesViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INavigationService _navigationService;

    private ObservableCollection<Invoice> _factures = new();
    public ObservableCollection<Invoice> Factures
    {
        get => _factures;
        set => SetProperty(ref _factures, value);
    }

    private Invoice? _selectedFacture;
    public Invoice? SelectedFacture
    {
        get => _selectedFacture;
        set => SetProperty(ref _selectedFacture, value);
    }

    public ICommand LoadFacturesCommand { get; }
    public ICommand AddFactureCommand { get; }
    public ICommand VerifyConformityCommand { get; }
    public ICommand RegisterPaymentCommand { get; }

    public FacturesViewModel(IUnitOfWork unitOfWork, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _navigationService = navigationService;
        Title = "Factures Fournisseurs";

        LoadFacturesCommand = new RelayCommand(async _ => await LoadFacturesAsync());
        AddFactureCommand = new RelayCommand(_ => _navigationService.NavigateTo("InvoiceForm"));
        VerifyConformityCommand = new RelayCommand(_ => 
        {
            if (SelectedFacture != null)
                _navigationService.NavigateTo("ConformityCheck", SelectedFacture.Id);
        }, _ => SelectedFacture != null);
        
        RegisterPaymentCommand = new RelayCommand(_ => 
        {
            if (SelectedFacture != null)
                _navigationService.NavigateTo("PaymentForm", SelectedFacture.Id);
        }, _ => SelectedFacture != null);

        _ = LoadFacturesAsync();
    }

    private async Task LoadFacturesAsync()
    {
        IsBusy = true;
        try
        {
            var factures = await _unitOfWork.Invoices.GetAllAsync();
            Factures = new ObservableCollection<Invoice>(factures.OrderByDescending(f => f.CreatedAt));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
