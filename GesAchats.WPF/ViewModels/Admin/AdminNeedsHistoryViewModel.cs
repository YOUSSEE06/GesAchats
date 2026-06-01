using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.ViewModels.Magasinier; // Re-use NeedHistoryItemViewModel if appropriate or create new
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminNeedHistoryItemViewModel : BaseViewModel
{
    public Need Need { get; }
    public string NumeroBesoin => Need.NumeroBesoin;
    public string Date => Need.RequestedAt.ToString("dd/MM/yyyy");
    public string Demandeur => Need.RequestedBy?.FullName ?? "Inconnu";
    public int ArticleCount => Need.Details?.Count ?? 0;
    public string StatusText { get; }
    public string StatusColor { get; }

    public AdminNeedHistoryItemViewModel(Need need)
    {
        Need = need;
        
        (StatusText, StatusColor) = need.Status switch
        {
            NeedStatus.TransmittedToPurchasing => ("Transmis", "#2196F3"),
            _ => ("En cours", "#3B82F6")
        };
    }
}

public class AdminNeedsHistoryViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private List<Need> _allNeeds = new();
    private string _searchNumero = string.Empty;
    private DateTime? _searchDate;
    private string _selectedStatus = "Tous";

    public ObservableCollection<string> StatusOptions { get; } = new() 
    { 
        "Tous", "En cours", "Transmis" 
    };

    public string SearchNumero
    {
        get => _searchNumero;
        set { if (SetProperty(ref _searchNumero, value)) FilterNeeds(); }
    }

    public DateTime? SearchDate
    {
        get => _searchDate;
        set { if (SetProperty(ref _searchDate, value)) FilterNeeds(); }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set { if (SetProperty(ref _selectedStatus, value)) FilterNeeds(); }
    }

    public ObservableCollection<AdminNeedHistoryItemViewModel> Needs { get; } = new();

    // Stats KPIs
    public int TotalBesoins => _allNeeds.Count;
    public int BesoinsEnCours => _allNeeds.Count(n => n.Status != NeedStatus.TransmittedToPurchasing);
    public int BesoinsTransmis => _allNeeds.Count(n => n.Status == NeedStatus.TransmittedToPurchasing);
    public int TotalArticlesDemandes => _allNeeds.Sum(n => n.Details?.Count ?? 0);
    public int DemandeursActifs => _allNeeds.Select(n => n.RequestedById).Distinct().Count();

    public ICommand ViewDetailsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    public AdminNeedsHistoryViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "HISTORIQUE DES BESOINS";

        ViewDetailsCommand = new RelayCommand(async p => await ExecuteViewDetails(p as AdminNeedHistoryItemViewModel));
        RefreshCommand = new RelayCommand(async _ => await LoadData());
        ClearFiltersCommand = new RelayCommand(_ => { SearchNumero = string.Empty; SearchDate = null; SelectedStatus = "Tous"; });

        _ = LoadData();
    }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var needs = await _unitOfWork.Needs.GetAllWithDetailsAsync();
            _allNeeds = needs.ToList();
            FilterNeeds();

            // Update stats
            OnPropertyChanged(nameof(TotalBesoins));
            OnPropertyChanged(nameof(BesoinsEnCours));
            OnPropertyChanged(nameof(BesoinsTransmis));
            OnPropertyChanged(nameof(TotalArticlesDemandes));
            OnPropertyChanged(nameof(DemandeursActifs));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterNeeds()
    {
        var filtered = _allNeeds.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchNumero))
            filtered = filtered.Where(n => n.NumeroBesoin.Contains(SearchNumero, StringComparison.OrdinalIgnoreCase));

        if (SearchDate.HasValue)
            filtered = filtered.Where(n => n.RequestedAt.Date == SearchDate.Value.Date);

        if (SelectedStatus != "Tous")
        {
            filtered = filtered.Where(n => 
            {
                var statusText = n.Status switch
                {
                    NeedStatus.InPurchase => "En cours",
                    NeedStatus.TransmittedToPurchasing => "Transmis",
                    _ => "En cours" // Map other statuses to "En cours" for filter? Or keep them as is?
                };
                return statusText == SelectedStatus;
            });
        }

        var sortedNeeds = filtered.OrderByDescending(n => n.RequestedAt).ToList();
        
        Needs.Clear();
        foreach (var n in sortedNeeds)
        {
            Needs.Add(new AdminNeedHistoryItemViewModel(n));
        }
    }

    private async Task ExecuteViewDetails(AdminNeedHistoryItemViewModel? item)
    {
        if (item == null) return;
        
        using (var scope = _serviceProvider.CreateScope())
        {
            // Re-use existing details view if possible
            var vm = ActivatorUtilities.CreateInstance<Magasinier.NeedsDetailsViewModel>(scope.ServiceProvider, item.Need.Id);
            var win = ActivatorUtilities.CreateInstance<Views.Magasinier.NeedsHistory.NeedsDetailsWindow>(scope.ServiceProvider, vm);
            win.Owner = System.Windows.Application.Current.MainWindow;
            win.ShowDialog();
            
            // Refresh data after closing the details window in case something changed
            await LoadData();
        }
    }
}
