using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Services;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Views.Magasinier.NeedsHistory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class NeedHistoryItemViewModel : BaseViewModel
{
    public Need Need { get; }
    public int ArticleCount { get; }
    
    private string _statusText = string.Empty;
    private string _statusColor = string.Empty;
    
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }
    
    public string StatusColor
    {
        get => _statusColor;
        set => SetProperty(ref _statusColor, value);
    }
    
    public bool IsStatusEnCours => Need.Status == NeedStatus.InPurchase || Need.Status == NeedStatus.Draft || Need.Status == NeedStatus.ToValidate;
    public bool IsStatusAnnule => Need.Status == NeedStatus.Cancelled || Need.Status == NeedStatus.Rejected;
    public bool IsStatusTransmis => Need.Status == NeedStatus.TransmittedToPurchasing;
    public bool IsNotTransmis => !IsStatusTransmis;

    public NeedHistoryItemViewModel(Need need)
    {
        Need = need;
        ArticleCount = need.Details?.Count ?? 0;
        UpdateStatusDisplay();
    }
    
    private void UpdateStatusDisplay()
    {
        (_statusText, _statusColor) = Need.Status switch
        {
            NeedStatus.Draft => ("En attente", "#9E9E9E"),
            NeedStatus.ToValidate => ("À Valider", "#607D8B"),
            NeedStatus.TransmittedToPurchasing => ("transmit", "#2196F3"),
            NeedStatus.InPurchase => ("encours", "#FFC107"),
            NeedStatus.Validated => ("Complété", "#4CAF50"),
            NeedStatus.Cancelled => ("Annulé", "#F44336"),
            NeedStatus.Rejected => ("Annulé", "#F44336"),
            _ => (Need.Status.ToString(), "#000000")
        };
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColor));
    }
    
    public void RefreshStatusFlags()
    {
        UpdateStatusDisplay();
        OnPropertyChanged(nameof(IsStatusEnCours));
        OnPropertyChanged(nameof(IsStatusAnnule));
        OnPropertyChanged(nameof(IsStatusTransmis));
        OnPropertyChanged(nameof(IsNotTransmis));
    }
}

public class NeedsHistoryViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserSession _userSession;
    private readonly INeedsAnalyticsService _analyticsService;
    private readonly IServiceProvider _serviceProvider;

    private string _searchText = string.Empty;
    private string _selectedStatus = "Tous";
    private DateTime? _exactDate = null;

    public ObservableCollection<NeedHistoryItemViewModel> Needs { get; } = new ObservableCollection<NeedHistoryItemViewModel>();
    public ObservableCollection<string> StatusFilterOptions { get; } = new ObservableCollection<string> { "Tous", "transmit", "encours", "Annulé" };
    
    // Statistics properties
    private int _totalNeeds;
    private int _transmittedNeeds;
    private int _inProgressNeeds;
    private int _cancelledNeeds;

    public int TotalNeeds
    {
        get => _totalNeeds;
        set => SetProperty(ref _totalNeeds, value);
    }

    public int TransmittedNeeds
    {
        get => _transmittedNeeds;
        set => SetProperty(ref _transmittedNeeds, value);
    }

    public int InProgressNeeds
    {
        get => _inProgressNeeds;
        set => SetProperty(ref _inProgressNeeds, value);
    }

    public int CancelledNeeds
    {
        get => _cancelledNeeds;
        set => SetProperty(ref _cancelledNeeds, value);
    }

    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) FilterNeeds(); } }
    public string SelectedStatus { get => _selectedStatus; set { if (SetProperty(ref _selectedStatus, value)) FilterNeeds(); } }
    public DateTime? ExactDate { get => _exactDate; set { if (SetProperty(ref _exactDate, value)) FilterNeeds(); } }

    public ICommand RefreshCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand ShowStatsCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand AnnulerCommand { get; }
    public ICommand ReactiverCommand { get; }
    public ICommand SupprimerCommand { get; }

    private List<Need> _allNeeds = new List<Need>();

    public NeedsHistoryViewModel(IUnitOfWork unitOfWork, IUserSession userSession, INeedsAnalyticsService analyticsService, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _userSession = userSession;
        _analyticsService = analyticsService;
        _serviceProvider = serviceProvider;
        Title = "Historique des Listes de Besoins";

        RefreshCommand = new RelayCommand(async _ => await LoadData());
        ViewDetailsCommand = new RelayCommand(p => ExecuteViewDetails(p as NeedHistoryItemViewModel));
        ShowStatsCommand = new RelayCommand(_ => ExecuteShowStats());
        ExportCsvCommand = new RelayCommand(async _ => await ExecuteExportCsv());
        AnnulerCommand = new RelayCommand(async p => await ExecuteAnnuler(p as NeedHistoryItemViewModel));
        ReactiverCommand = new RelayCommand(async p => await ExecuteReactiver(p as NeedHistoryItemViewModel));
        SupprimerCommand = new RelayCommand(async p => await ExecuteSupprimer(p as NeedHistoryItemViewModel));

        _ = LoadData();
    }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var needs = await _unitOfWork.Needs.GetAllWithDetailsAsync();
            // On filtre par l'utilisateur connecté (ID 1 par défaut si session vide pour tests)
            var currentUserId = _userSession.CurrentUser?.Id ?? 1;
            _allNeeds = needs.Where(n => n.RequestedById == currentUserId).ToList();
            FilterNeeds();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterNeeds()
    {
        Needs.Clear();
        var filtered = _allNeeds.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
            filtered = filtered.Where(n => n.NumeroBesoin.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        if (SelectedStatus != "Tous")
        {
            var targetStatus = SelectedStatus switch
            {
                "transmit" => new[] { NeedStatus.TransmittedToPurchasing },
                "encours" => new[] { NeedStatus.InPurchase },
                "Annulé" => new[] { NeedStatus.Cancelled, NeedStatus.Rejected },
                _ => Array.Empty<NeedStatus>()
            };
            filtered = filtered.Where(n => targetStatus.Contains(n.Status));
        }

        // Filtre par date exacte
        if (ExactDate.HasValue)
        {
            var dateUtc = ExactDate.Value.Date;
            filtered = filtered.Where(n => n.RequestedAt.Date == dateUtc);
        }

        var filteredList = filtered.ToList();
        foreach (var n in filteredList.OrderByDescending(x => x.RequestedAt))
            Needs.Add(new NeedHistoryItemViewModel(n));

        // Calculate statistics
        TotalNeeds = filteredList.Count;
        TransmittedNeeds = filteredList.Count(n => n.Status == NeedStatus.TransmittedToPurchasing);
        InProgressNeeds = filteredList.Count(n => n.Status == NeedStatus.InPurchase);
        CancelledNeeds = filteredList.Count(n => n.Status == NeedStatus.Cancelled || n.Status == NeedStatus.Rejected);
    }

    private void ExecuteViewDetails(NeedHistoryItemViewModel? item)
    {
        if (item == null) return;
        
        using (var scope = _serviceProvider.CreateScope())
        {
            var vm = ActivatorUtilities.CreateInstance<NeedsDetailsViewModel>(scope.ServiceProvider, item.Need.Id);
            var win = ActivatorUtilities.CreateInstance<NeedsDetailsWindow>(scope.ServiceProvider, vm);
            win.Owner = System.Windows.Application.Current.MainWindow;
            win.ShowDialog();
        }
    }
    
    private async Task ExecuteAnnuler(NeedHistoryItemViewModel? item)
    {
        if (item == null) return;
        
        item.Need.Status = NeedStatus.Cancelled;
        item.Need.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Needs.Update(item.Need);
        await _unitOfWork.CompleteAsync();
        item.RefreshStatusFlags();
    }
    
    private async Task ExecuteReactiver(NeedHistoryItemViewModel? item)
    {
        if (item == null) return;
        
        item.Need.Status = NeedStatus.InPurchase;
        item.Need.UpdatedAt = DateTime.UtcNow;
        _unitOfWork.Needs.Update(item.Need);
        await _unitOfWork.CompleteAsync();
        item.RefreshStatusFlags();
    }
    
    private async Task ExecuteSupprimer(NeedHistoryItemViewModel? item)
    {
        if (item == null) return;
        
        var result = MessageBox.Show(
            $"Êtes-vous sûr de vouloir supprimer le besoin {item.Need.NumeroBesoin} ? Cette action est irréversible.",
            "Confirmer la suppression",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
            
        if (result == MessageBoxResult.Yes)
        {
            try
            {
                // Récupérer le besoin avec tous les détails depuis la base de données
                var need = await _unitOfWork.Needs.GetByIdWithDetailsAsync(item.Need.Id);
                if (need != null)
                {
                    // Désolidariser les Quotations liés à ce Need (Need n'a pas de nav prop Quotations, donc on les recherche via QuotationRepository)
                    var linkedQuotations = await _unitOfWork.Quotations.FindAsync(q => q.NeedId == need.Id);
                    foreach (var q in linkedQuotations)
                    {
                        q.NeedId = null;
                        _unitOfWork.Quotations.Update(q);
                    }

                    // Désolidariser les PurchaseOrders liés à ce Need
                    foreach (var po in need.PurchaseOrders.ToList())
                    {
                        po.NeedId = null;
                        _unitOfWork.PurchaseOrders.Update(po);
                    }

                    // Supprimer d'abord les NeedDetails
                    foreach (var detail in need.Details.ToList())
                    {
                        _unitOfWork.NeedDetails.Remove(detail);
                    }
                    
                    // Puis supprimer le Need
                    _unitOfWork.Needs.Remove(need);
                    await _unitOfWork.CompleteAsync();
                    
                    Needs.Remove(item);
                    _allNeeds.Remove(need);
                }
            }
            catch (Exception ex)
            {
                // Récupérer toutes les inner exceptions
                var fullErrorMessage = $"Erreur lors de la suppression : {ex.Message}";
                var innerEx = ex.InnerException;
                int counter = 1;
                while (innerEx != null)
                {
                    fullErrorMessage += $"\n\nInner Exception {counter} : {innerEx.Message}";
                    innerEx = innerEx.InnerException;
                    counter++;
                }
                MessageBox.Show(fullErrorMessage, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ExecuteShowStats()
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var vm = scope.ServiceProvider.GetRequiredService<NeedsStatisticsViewModel>();
            var win = ActivatorUtilities.CreateInstance<NeedsStatisticsWindow>(scope.ServiceProvider, vm);
            win.Owner = System.Windows.Application.Current.MainWindow;
            win.ShowDialog();
        }
    }

    private async Task ExecuteExportCsv()
    {
        if (!Needs.Any()) return;

        var sfd = new SaveFileDialog
        {
            Filter = "Fichiers CSV (*.csv)|*.csv",
            FileName = $"Historique_Besoins_{DateTime.Now:yyyyMMdd}.csv"
        };

        if (sfd.ShowDialog() == true)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Date;N°Besoin;Articles;Statut;Date Transmission");

                foreach (var item in Needs)
                {
                    sb.AppendLine($"{item.Need.RequestedAt:dd/MM/yyyy HH:mm};{item.Need.NumeroBesoin};{item.ArticleCount};{item.StatusText};{item.Need.DateTransmission:dd/MM/yyyy HH:mm}");
                }

                await File.WriteAllTextAsync(sfd.FileName, sb.ToString(), Encoding.UTF8);
                MessageBox.Show("Exportation CSV réussie.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'exportation CSV : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
