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
    private List<ReceivedNeedItemViewModel> _allFilteredNeeds = new List<ReceivedNeedItemViewModel>();

    private int _currentPage = 1;
    private int _itemsPerPage = 20;
    private int _totalPages = 1;
    private int _totalItems;

    public ObservableCollection<ReceivedNeedItemViewModel> Needs { get; } = new ObservableCollection<ReceivedNeedItemViewModel>();
    public ObservableCollection<string> StatusOptions { get; } = new ObservableCollection<string> { "Tous", "encours", "transmit", "Annulé" };

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
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

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

    private int _cancelledNeeds;
    public int CancelledNeeds
    {
        get => _cancelledNeeds;
        set => SetProperty(ref _cancelledNeeds, value);
    }

    private string _totalNeedsTrendText = string.Empty;
    public string TotalNeedsTrendText
    {
        get => _totalNeedsTrendText;
        set => SetProperty(ref _totalNeedsTrendText, value);
    }

    private string _transmittedNeedsTrendText = string.Empty;
    public string TransmittedNeedsTrendText
    {
        get => _transmittedNeedsTrendText;
        set => SetProperty(ref _transmittedNeedsTrendText, value);
    }

    private string _inProgressNeedsTrendText = string.Empty;
    public string InProgressNeedsTrendText
    {
        get => _inProgressNeedsTrendText;
        set => SetProperty(ref _inProgressNeedsTrendText, value);
    }

    private string _cancelledNeedsTrendText = string.Empty;
    public string CancelledNeedsTrendText
    {
        get => _cancelledNeedsTrendText;
        set => SetProperty(ref _cancelledNeedsTrendText, value);
    }

    // ===================== FILTRE PÉRIODE =====================
    private DateTime _startDate = DateTime.Today.AddMonths(-1);
    public DateTime StartDate
    {
        get => _startDate;
        set
        {
            if (SetProperty(ref _startDate, value)) { RecomputePeriodStats(); }
        }
    }

    private DateTime _endDate = DateTime.Today;
    public DateTime EndDate
    {
        get => _endDate;
        set
        {
            if (SetProperty(ref _endDate, value)) { RecomputePeriodStats(); }
        }
    }

    // ===================== TENDANCES KPI =====================
    private bool _totalNeedsIsPositive = true;
    public bool TotalNeedsIsPositive
    {
        get => _totalNeedsIsPositive;
        set => SetProperty(ref _totalNeedsIsPositive, value);
    }

    private bool _transmittedNeedsIsPositive = true;
    public bool TransmittedNeedsIsPositive
    {
        get => _transmittedNeedsIsPositive;
        set => SetProperty(ref _transmittedNeedsIsPositive, value);
    }

    private bool _inProgressNeedsIsPositive = true;
    public bool InProgressNeedsIsPositive
    {
        get => _inProgressNeedsIsPositive;
        set => SetProperty(ref _inProgressNeedsIsPositive, value);
    }

    private bool _cancelledNeedsIsPositive = true;
    public bool CancelledNeedsIsPositive
    {
        get => _cancelledNeedsIsPositive;
        set => SetProperty(ref _cancelledNeedsIsPositive, value);
    }

    // ===================== PAGINATION =====================
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                UpdatePagedNeeds();
            }
        }
    }

    public int ItemsPerPage
    {
        get => _itemsPerPage;
        set
        {
            if (SetProperty(ref _itemsPerPage, value) && value > 0)
            {
                CalculateTotalPages();
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                UpdatePagedNeeds();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        set
        {
            if (SetProperty(ref _totalItems, value))
            {
                CalculateTotalPages();
                OnPropertyChanged(nameof(FirstDisplayedItem));
                OnPropertyChanged(nameof(LastDisplayedItem));
                OnPropertyChanged(nameof(PaginationText));
            }
        }
    }

    public int FirstDisplayedItem => TotalItems == 0 ? 0 : ((CurrentPage - 1) * ItemsPerPage) + 1;

    public int LastDisplayedItem => Math.Min(CurrentPage * ItemsPerPage, TotalItems);

    public string PaginationText
    {
        get
        {
            if (TotalItems == 0) return "Aucune demande";
            var unit = TotalItems > 1 ? "demandes" : "demande";
            return $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} {unit}";
        }
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
        FirstPageCommand = new RelayCommand(_ => CurrentPage = 1, _ => CurrentPage > 1);
        PreviousPageCommand = new RelayCommand(_ => { if (CurrentPage > 1) CurrentPage--; }, _ => CurrentPage > 1);
        NextPageCommand = new RelayCommand(_ => { if (CurrentPage < TotalPages) CurrentPage++; }, _ => CurrentPage < TotalPages);
        LastPageCommand = new RelayCommand(_ => CurrentPage = TotalPages, _ => CurrentPage < TotalPages);

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
            RecomputePeriodStats();
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
                (SelectedStatus == "transmit" && n.Need.Status == NeedStatus.TransmittedToPurchasing) ||
                (SelectedStatus == "Annulé" && (n.Need.Status == NeedStatus.Cancelled || n.Need.Status == NeedStatus.Rejected)));
        }
        
        var filteredList = filtered.ToList();
        _allFilteredNeeds = filteredList;

        // Réinitialise la pagination (recalcul du total + retour page 1)
        TotalItems = _allFilteredNeeds.Count;
        CurrentPage = 1;
        UpdatePagedNeeds();
    }

    private void CalculateTotalPages()
    {
        TotalPages = (int)Math.Ceiling((double)_allFilteredNeeds.Count / ItemsPerPage);
        if (TotalPages < 1) TotalPages = 1;
    }

    private void UpdatePagedNeeds()
    {
        Needs.Clear();
        var start = (CurrentPage - 1) * ItemsPerPage;
        foreach (var n in _allFilteredNeeds.Skip(start).Take(ItemsPerPage))
        {
            Needs.Add(n);
        }

        OnPropertyChanged(nameof(FirstDisplayedItem));
        OnPropertyChanged(nameof(LastDisplayedItem));
        OnPropertyChanged(nameof(PaginationText));

        // Notifie les commandes de pagination (états désactivés des boutons)
        CommandManager.InvalidateRequerySuggested();
    }

    private void RecomputePeriodStats()
    {
        // Période actuelle vs période précédente de même durée (formule Dashboard standardisée)
        var start = StartDate.Date;
        var end = EndDate.Date.AddDays(1);
        var duration = EndDate - StartDate;
        var previousStart = StartDate - duration;
        var previousEnd = StartDate;

        var currentNeeds = _allNeeds.Where(n => n.Need.RequestedAt >= start && n.Need.RequestedAt < end).ToList();
        var previousNeeds = _allNeeds.Where(n => n.Need.RequestedAt >= previousStart && n.Need.RequestedAt < previousEnd).ToList();

        TotalNeeds = currentNeeds.Count;
        TransmittedNeeds = currentNeeds.Count(n => n.Need.Status == NeedStatus.TransmittedToPurchasing);
        InProgressNeeds = currentNeeds.Count(n => n.Need.Status == NeedStatus.InPurchase);
        CancelledNeeds = currentNeeds.Count(n => n.Need.Status == NeedStatus.Cancelled || n.Need.Status == NeedStatus.Rejected);

        TotalNeedsIsPositive = ApplyTrend(TotalNeeds, previousNeeds.Count, out var totalTrend);
        TotalNeedsTrendText = totalTrend;
        TransmittedNeedsIsPositive = ApplyTrend(TransmittedNeeds,
            previousNeeds.Count(n => n.Need.Status == NeedStatus.TransmittedToPurchasing), out var transmittedTrend);
        TransmittedNeedsTrendText = transmittedTrend;
        InProgressNeedsIsPositive = ApplyTrend(InProgressNeeds,
            previousNeeds.Count(n => n.Need.Status == NeedStatus.InPurchase), out var inProgressTrend);
        InProgressNeedsTrendText = inProgressTrend;
        CancelledNeedsIsPositive = ApplyTrend(CancelledNeeds,
            previousNeeds.Count(n => n.Need.Status == NeedStatus.Cancelled || n.Need.Status == NeedStatus.Rejected), out var cancelledTrend);
        CancelledNeedsTrendText = cancelledTrend;
    }

    private static bool ApplyTrend(int current, int previous, out string trendText)
    {
        var variation = CalculateVariation(current, previous);
        trendText = FormatTrend(variation, out var isPositive);
        return isPositive;
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
