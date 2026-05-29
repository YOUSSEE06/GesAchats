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
            _ => (need.Status.ToString(), "#000000")
        };
    }
}

public class AdminNeedsHistoryViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;

    public ObservableCollection<AdminNeedHistoryItemViewModel> Needs { get; } = new();

    public ICommand ViewDetailsCommand { get; }
    public ICommand RefreshCommand { get; }

    public AdminNeedsHistoryViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "HISTORIQUE DES BESOINS";

        ViewDetailsCommand = new RelayCommand(p => ExecuteViewDetails(p as AdminNeedHistoryItemViewModel));
        RefreshCommand = new RelayCommand(async _ => await LoadData());

        _ = LoadData();
    }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var needs = await _unitOfWork.Needs.GetAllWithDetailsAsync();
            
            // Admin sees all needs
            var sortedNeeds = needs.OrderByDescending(n => n.RequestedAt).ToList();
            
            Needs.Clear();
            foreach (var n in sortedNeeds)
            {
                Needs.Add(new AdminNeedHistoryItemViewModel(n));
            }
        }
        finally
        {
            IsBusy = false;
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
