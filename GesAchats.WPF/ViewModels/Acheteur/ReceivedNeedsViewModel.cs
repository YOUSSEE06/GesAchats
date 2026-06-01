using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;

using GesAchats.WPF.Views.Acheteur.Besoins;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class ReceivedNeedItemViewModel : BaseViewModel
{
    public Need Need { get; }
    
    public ReceivedNeedItemViewModel(Need need)
    {
        Need = need;
    }

    public int ArticleCount => Need.Details?.Count ?? 0;
    public string Summary => Need.Details != null && Need.Details.Any() 
        ? string.Join(", ", Need.Details.Take(3).Select(d => d.Product?.Designation ?? "Inconnu")) + (Need.Details.Count > 3 ? "..." : "")
        : "Aucun article";

    public string StatusColor => Need.Status switch
    {
        NeedStatus.Draft => "#9E9E9E",
        NeedStatus.ToValidate => "#FF9800",
        NeedStatus.TransmittedToPurchasing => "#2196F3",
        NeedStatus.Validated => "#4CAF50",
        NeedStatus.InPurchase => "#FFC107",
        NeedStatus.Cancelled => "#F44336",
        NeedStatus.Rejected => "#F44336",
        NeedStatus.Relaunched => "#E91E63",
        _ => "#9E9E9E"
    };

    public string StatusText => Need.Status switch
    {
        NeedStatus.Draft => "Brouillon",
        NeedStatus.ToValidate => "À valider",
        NeedStatus.TransmittedToPurchasing => "transmit",
        NeedStatus.Validated => "Validé",
        NeedStatus.InPurchase => "encours",
        NeedStatus.Cancelled => "Annulé",
        NeedStatus.Rejected => "Rejeté",
        NeedStatus.Relaunched => "Relancé",
        _ => "Inconnu"
    };
}

public class ReceivedNeedsViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private readonly INavigationService _navigationService;

    private List<ReceivedNeedItemViewModel> _allNeeds = new List<ReceivedNeedItemViewModel>();

    public ObservableCollection<ReceivedNeedItemViewModel> Needs { get; } = new ObservableCollection<ReceivedNeedItemViewModel>();
    public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string> { "Tous", "encours", "transmit" };

    private string _searchNumeroDemande = string.Empty;
    public string SearchNumeroDemande
    {
        get => _searchNumeroDemande;
        set
        {
            if (SetProperty(ref _searchNumeroDemande, value))
            {
                FilterNeeds();
            }
        }
    }

    private DateTime? _searchDate;
    public DateTime? SearchDate
    {
        get => _searchDate;
        set
        {
            if (SetProperty(ref _searchDate, value))
            {
                FilterNeeds();
            }
        }
    }

    private string _selectedStatus = "Tous";
    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                FilterNeeds();
            }
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand CreateQuoteCommand { get; }
    public ICommand ResetFiltersCommand { get; }

    private int _totalNeeds;
    public int TotalNeeds
    {
        get => _totalNeeds;
        set => SetProperty(ref _totalNeeds, value);
    }

    private int _transmittedNeeds;
    public int TransmittedNeeds
    {
        get => _transmittedNeeds;
        set => SetProperty(ref _transmittedNeeds, value);
    }

    private int _inProgressNeeds;
    public int InProgressNeeds
    {
        get => _inProgressNeeds;
        set => SetProperty(ref _inProgressNeeds, value);
    }

    public ReceivedNeedsViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, INavigationService navigationService)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        _navigationService = navigationService;
        Title = "Besoins Reçus";

        RefreshCommand = new RelayCommand(async _ => await LoadData());
        ViewDetailsCommand = new RelayCommand(p => ExecuteViewDetails(p as ReceivedNeedItemViewModel));
        CreateQuoteCommand = new RelayCommand(p => ExecuteCreateQuote(p as ReceivedNeedItemViewModel));
        ResetFiltersCommand = new RelayCommand(_ => ExecuteResetFilters());

        _ = LoadData();
    }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var needs = await _unitOfWork.Needs.GetPendingNeedsWithProductsAsync();
            _allNeeds.Clear();
            if (needs != null)
            {
                foreach (var n in needs)
                {
                    _allNeeds.Add(new ReceivedNeedItemViewModel(n));
                }
            }
            FilterNeeds();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterNeeds()
    {
        var filtered = _allNeeds.AsEnumerable();
        
        if (!string.IsNullOrWhiteSpace(SearchNumeroDemande))
        {
            filtered = filtered.Where(n => 
                n.Need.NumeroBesoin != null && 
                n.Need.NumeroBesoin.Contains(SearchNumeroDemande, StringComparison.OrdinalIgnoreCase));
        }
        
        if (SearchDate.HasValue)
        {
            filtered = filtered.Where(n => 
                n.Need.DateTransmission.HasValue && 
                n.Need.DateTransmission.Value.Date == SearchDate.Value.Date);
        }

        if (SelectedStatus != "Tous")
        {
            filtered = filtered.Where(n => 
                (SelectedStatus == "encours" && n.Need.Status == NeedStatus.InPurchase) || 
                (SelectedStatus == "transmit" && n.Need.Status == NeedStatus.TransmittedToPurchasing));
        }
        
        Needs.Clear();
        foreach (var n in filtered)
        {
            Needs.Add(n);
        }

        UpdateStatistics(filtered.ToList());
    }

    private void UpdateStatistics(List<ReceivedNeedItemViewModel> currentNeeds)
    {
        TotalNeeds = currentNeeds.Count;
        TransmittedNeeds = currentNeeds.Count(n => n.Need.Status == NeedStatus.TransmittedToPurchasing);
        InProgressNeeds = currentNeeds.Count(n => n.Need.Status == NeedStatus.InPurchase);
    }
    
    private void ExecuteResetFilters()
    {
        SearchNumeroDemande = string.Empty;
        SearchDate = null;
        SelectedStatus = "Tous";
    }

    private void ExecuteViewDetails(ReceivedNeedItemViewModel? item)
    {
        if (item == null) return;
        
        var detailWindow = new ArticleDetailWindow(item.Need);
        detailWindow.Owner = System.Windows.Application.Current.MainWindow;
        detailWindow.ShowDialog();
    }

    private void ExecuteCreateQuote(ReceivedNeedItemViewModel? item)
    {
        if (item == null) return;
        
        // Navigation vers la page de gestion des devis avec le besoin sélectionné
        _navigationService.NavigateTo("Devis", item.Need);
    }
}
