using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminNeedHistoryItemViewModel : BaseViewModel
{
    public int Id { get; }
    public string NumeroBesoin { get; }
    public string Date { get; }
    public string Demandeur { get; }
    public int ArticleCount { get; }
    public string StatusText { get; }
    public string StatusColor { get; }

    public AdminNeedHistoryItemViewModel(NeedHistoriqueDto dto)
    {
        Id = dto.Id;
        NumeroBesoin = dto.NumeroBesoin;
        Date = dto.RequestedAt.ToString("dd/MM/yyyy");
        Demandeur = dto.Demandeur;
        ArticleCount = dto.NombreArticles;
        StatusText = dto.Statut;
        StatusColor = dto.Statut == "Transmis" ? "#10B981" : "#3B82F6";
    }
}

public class AdminNeedsHistoryViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private CancellationTokenSource? _cts;
    
    // Pagination properties
    private int _currentPage = 1;
    private int _totalItems;
    private const int PageSize = 20;
    private string _searchNumero = string.Empty;
    private DateTime? _searchDate;
    private string _selectedStatus = "Tous";
    
    // Stats KPIs
    private int _totalBesoins;
    private int _besoinsEnCours;
    private int _besoinsTransmis;
    private int _totalArticlesDemandes;
    private int _demandeursActifs;
    private string _totalBesoinsTrendText = string.Empty;
    private string _besoinsEnCoursTrendText = string.Empty;
    private string _besoinsTransmisTrendText = string.Empty;
    private string _totalArticlesDemandesTrendText = string.Empty;
    private string _demandeursActifsTrendText = string.Empty;

    public ObservableCollection<string> StatusOptions { get; } = new() 
    { 
        "Tous", "En cours", "Transmis" 
    };

    public ObservableCollection<AdminNeedHistoryItemViewModel> Needs { get; } = new();

    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                OnPropertyChanged(nameof(FirstDisplayedItem));
                OnPropertyChanged(nameof(LastDisplayedItem));
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));
                OnPropertyChanged(nameof(PaginationInfo));
            }
        }
    }

    public int TotalItems
    {
        get => _totalItems;
        set
        {
            if (SetProperty(ref _totalItems, value))
            {
                OnPropertyChanged(nameof(TotalPages));
                OnPropertyChanged(nameof(FirstDisplayedItem));
                OnPropertyChanged(nameof(LastDisplayedItem));
                OnPropertyChanged(nameof(CanGoToFirstPage));
                OnPropertyChanged(nameof(CanGoToPreviousPage));
                OnPropertyChanged(nameof(CanGoToNextPage));
                OnPropertyChanged(nameof(CanGoToLastPage));
                OnPropertyChanged(nameof(PaginationInfo));
            }
        }
    }

    public int TotalPages => TotalItems == 0 ? 0 : (int)Math.Ceiling((double)TotalItems / PageSize);

    public int FirstDisplayedItem => TotalItems == 0 ? 0 : ((CurrentPage - 1) * PageSize) + 1;

    public int LastDisplayedItem => Math.Min(CurrentPage * PageSize, TotalItems);

    public bool CanGoToFirstPage => CurrentPage > 1 && TotalItems > 0;
    public bool CanGoToPreviousPage => CurrentPage > 1 && TotalItems > 0;
    public bool CanGoToNextPage => CurrentPage < TotalPages && TotalItems > 0;
    public bool CanGoToLastPage => CurrentPage < TotalPages && TotalItems > 0;

    public string PaginationInfo
    {
        get
        {
            if (TotalItems == 0)
                return "Affichage de 0 à 0 sur 0 besoins";
            return $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} besoins";
        }
    }

    public string SearchNumero
    {
        get => _searchNumero;
        set
        {
            if (SetProperty(ref _searchNumero, value))
            {
                DebouncedFilter();
            }
        }
    }

    public DateTime? SearchDate
    {
        get => _searchDate;
        set
        {
            if (SetProperty(ref _searchDate, value))
            {
                _ = ResetAndLoadPageAsync();
            }
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
            {
                _ = ResetAndLoadPageAsync();
            }
        }
    }

    // Stats KPIs
    public int TotalBesoins
    {
        get => _totalBesoins;
        set => SetProperty(ref _totalBesoins, value);
    }
    public int BesoinsEnCours
    {
        get => _besoinsEnCours;
        set => SetProperty(ref _besoinsEnCours, value);
    }
    public int BesoinsTransmis
    {
        get => _besoinsTransmis;
        set => SetProperty(ref _besoinsTransmis, value);
    }
    public int TotalArticlesDemandes
    {
        get => _totalArticlesDemandes;
        set => SetProperty(ref _totalArticlesDemandes, value);
    }
    public int DemandeursActifs
    {
        get => _demandeursActifs;
        set => SetProperty(ref _demandeursActifs, value);
    }

    public string TotalBesoinsTrendText
    {
        get => _totalBesoinsTrendText;
        set => SetProperty(ref _totalBesoinsTrendText, value);
    }
    public string BesoinsEnCoursTrendText
    {
        get => _besoinsEnCoursTrendText;
        set => SetProperty(ref _besoinsEnCoursTrendText, value);
    }
    public string BesoinsTransmisTrendText
    {
        get => _besoinsTransmisTrendText;
        set => SetProperty(ref _besoinsTransmisTrendText, value);
    }
    public string TotalArticlesDemandesTrendText
    {
        get => _totalArticlesDemandesTrendText;
        set => SetProperty(ref _totalArticlesDemandesTrendText, value);
    }
    public string DemandeursActifsTrendText
    {
        get => _demandeursActifsTrendText;
        set => SetProperty(ref _demandeursActifsTrendText, value);
    }

    public ICommand ViewDetailsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearFiltersCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    public AdminNeedsHistoryViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "HISTORIQUE DES BESOINS";

        ViewDetailsCommand = new RelayCommand(async p => await ExecuteViewDetails(p as AdminNeedHistoryItemViewModel));
        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        ClearFiltersCommand = new RelayCommand(async _ => await ExecuteClearFiltersAsync());
        FirstPageCommand = new RelayCommand(async _ => await GoToFirstPageAsync(), _ => CanGoToFirstPage);
        PreviousPageCommand = new RelayCommand(async _ => await GoToPreviousPageAsync(), _ => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(async _ => await GoToNextPageAsync(), _ => CanGoToNextPage);
        LastPageCommand = new RelayCommand(async _ => await GoToLastPageAsync(), _ => CanGoToLastPage);

        _ = LoadDataAsync();
    }

    private System.Threading.Timer? _debounceTimer;
    private void DebouncedFilter()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Threading.Timer(async _ =>
        {
            await ResetAndLoadPageAsync();
        }, null, 300, System.Threading.Timeout.Infinite);
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            await CalculateStatsAsync();
            await LoadPageAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task CalculateStatsAsync()
    {
        var allNeeds = await _unitOfWork.Needs.GetAllWithDetailsAsync();
        var needsList = allNeeds.ToList();

        TotalBesoins = needsList.Count;
        BesoinsEnCours = needsList.Count(n => n.Status != NeedStatus.TransmittedToPurchasing);
        BesoinsTransmis = needsList.Count(n => n.Status == NeedStatus.TransmittedToPurchasing);
        TotalArticlesDemandes = needsList.Sum(n => n.Details?.Count ?? 0);
        DemandeursActifs = needsList.Select(n => n.RequestedById).Distinct().Count();

        // Calculate yesterday's data
        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);
        var yesterdayNeeds = needsList.Where(n => 
            n.RequestedAt.Date >= yesterday && n.RequestedAt.Date < today).ToList();

        int yesterdayTotal = yesterdayNeeds.Count;
        int yesterdayEnCours = yesterdayNeeds.Count(n => n.Status != NeedStatus.TransmittedToPurchasing);
        int yesterdayTransmis = yesterdayNeeds.Count(n => n.Status == NeedStatus.TransmittedToPurchasing);
        int yesterdayArticles = yesterdayNeeds.Sum(n => n.Details?.Count ?? 0);
        int yesterdayDemandeurs = yesterdayNeeds.Select(n => n.RequestedById).Distinct().Count();

        // Calculate trend texts
        TotalBesoinsTrendText = CalculateTrendText(TotalBesoins, yesterdayTotal);
        BesoinsEnCoursTrendText = CalculateTrendText(BesoinsEnCours, yesterdayEnCours);
        BesoinsTransmisTrendText = CalculateTrendText(BesoinsTransmis, yesterdayTransmis);
        TotalArticlesDemandesTrendText = CalculateTrendText(TotalArticlesDemandes, yesterdayArticles);
        DemandeursActifsTrendText = CalculateTrendText(DemandeursActifs, yesterdayDemandeurs);
    }

    private async Task ResetAndLoadPageAsync()
    {
        CurrentPage = 1;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync()
    {
        // Cancel previous operation
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        try
        {
            var result = await _unitOfWork.Needs.GetHistoriqueBesoinsPagedAsync(
                CurrentPage,
                PageSize,
                SearchNumero,
                SearchDate,
                SelectedStatus,
                _cts.Token);

            Needs.Clear();
            foreach (var dto in result.Items)
            {
                Needs.Add(new AdminNeedHistoryItemViewModel(dto));
            }

            TotalItems = result.TotalCount;

            // If current page is beyond total pages, go to last page
            if (CurrentPage > TotalPages && TotalPages > 0)
            {
                CurrentPage = TotalPages;
                await LoadPageAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Operation cancelled, ignore
        }
    }

    private async Task GoToFirstPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage = 1;
            await LoadPageAsync();
        }
    }

    private async Task GoToPreviousPageAsync()
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
            await LoadPageAsync();
        }
    }

    private async Task GoToNextPageAsync()
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
            await LoadPageAsync();
        }
    }

    private async Task GoToLastPageAsync()
    {
        if (TotalPages > 0 && CurrentPage != TotalPages)
        {
            CurrentPage = TotalPages;
            await LoadPageAsync();
        }
    }

    private async Task ExecuteClearFiltersAsync()
    {
        SearchNumero = string.Empty;
        SearchDate = null;
        SelectedStatus = "Tous";
        await ResetAndLoadPageAsync();
    }

    private async Task ExecuteViewDetails(AdminNeedHistoryItemViewModel? item)
    {
        if (item == null) return;
        
        using (var scope = _serviceProvider.CreateScope())
        {
            var vm = ActivatorUtilities.CreateInstance<Magasinier.NeedsDetailsViewModel>(scope.ServiceProvider, item.Id);
            var win = ActivatorUtilities.CreateInstance<Views.Magasinier.NeedsHistory.NeedsDetailsWindow>(scope.ServiceProvider, vm);
            win.Owner = System.Windows.Application.Current.MainWindow;
            win.ShowDialog();
            
            // Refresh data after closing the details window in case something changed
            await LoadDataAsync();
        }
    }
}
