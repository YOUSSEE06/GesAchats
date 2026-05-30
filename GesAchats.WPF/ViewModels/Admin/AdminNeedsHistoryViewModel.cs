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
            NeedStatus.Draft => ("En attente", "#9E9E9E"),
            NeedStatus.ToValidate => ("À Valider", "#FF9800"),
            NeedStatus.TransmittedToPurchasing => ("Transmis", "#2196F3"),
            NeedStatus.InPurchase => ("En cours", "#3B82F6"),
            NeedStatus.Validated => ("Complété", "#4CAF50"),
            NeedStatus.Cancelled => ("Annulé", "#9E9E9E"),
            NeedStatus.Rejected => ("Rejeté", "#F44336"),
            NeedStatus.Relaunched => ("Relancé", "#673AB7"),
            _ => (need.Status.ToString(), "#000000")
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
        "Tous", "En attente", "À Valider", "Transmis", "En cours", "Complété", "Annulé", "Rejeté", "Relancé" 
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

    public ICommand ViewDetailsCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    public AdminNeedsHistoryViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "HISTORIQUE DES BESOINS";

        ViewDetailsCommand = new RelayCommand(p => ExecuteViewDetails(p as AdminNeedHistoryItemViewModel));
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
                    NeedStatus.Draft => "En attente",
                    NeedStatus.ToValidate => "À Valider",
                    NeedStatus.TransmittedToPurchasing => "Transmis",
                    NeedStatus.InPurchase => "En cours",
                    NeedStatus.Validated => "Complété",
                    NeedStatus.Cancelled => "Annulé",
                    NeedStatus.Rejected => "Rejeté",
                    NeedStatus.Relaunched => "Relancé",
                    _ => n.Status.ToString()
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

    private void ExecuteViewDetails(AdminNeedHistoryItemViewModel? item)
    {
        if (item == null) return;
        
        using (var scope = _serviceProvider.CreateScope())
        {
            // Re-use existing details view if possible
            var vm = ActivatorUtilities.CreateInstance<Magasinier.NeedsDetailsViewModel>(scope.ServiceProvider, item.Need.Id);
            var win = ActivatorUtilities.CreateInstance<Views.Magasinier.NeedsHistory.NeedsDetailsWindow>(scope.ServiceProvider, vm);
            win.Owner = System.Windows.Application.Current.MainWindow;
            win.ShowDialog();
        }
    }
}
