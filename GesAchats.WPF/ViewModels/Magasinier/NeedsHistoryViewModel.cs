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
    public string StatusText { get; }
    public string StatusColor { get; }

    public NeedHistoryItemViewModel(Need need)
    {
        Need = need;
        ArticleCount = need.Details?.Count ?? 0;
        
        (StatusText, StatusColor) = need.Status switch
        {
            NeedStatus.Draft => ("En attente", "#9E9E9E"),
            NeedStatus.ToValidate => ("À Valider", "#607D8B"),
            NeedStatus.TransmittedToPurchasing => ("Transmis", "#2196F3"),
            NeedStatus.InPurchase => ("En cours", "#FFC107"),
            NeedStatus.Validated => ("Complété", "#4CAF50"),
            NeedStatus.Cancelled => ("Annulé", "#F44336"),
            NeedStatus.Rejected => ("Rejeté", "#F44336"),
            _ => (need.Status.ToString(), "#000000")
        };
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
    private string _selectedPeriod = "Ce mois";

    public ObservableCollection<NeedHistoryItemViewModel> Needs { get; } = new ObservableCollection<NeedHistoryItemViewModel>();
    public ObservableCollection<string> StatusFilterOptions { get; } = new ObservableCollection<string> { "Tous", "En attente", "Transmis", "En cours", "Complété", "Annulé" };
    public ObservableCollection<string> PeriodFilterOptions { get; } = new ObservableCollection<string> { "Ce mois", "3 derniers mois", "6 mois", "1 an", "Tout" };

    public string SearchText { get => _searchText; set { if (SetProperty(ref _searchText, value)) FilterNeeds(); } }
    public string SelectedStatus { get => _selectedStatus; set { if (SetProperty(ref _selectedStatus, value)) FilterNeeds(); } }
    public string SelectedPeriod { get => _selectedPeriod; set { if (SetProperty(ref _selectedPeriod, value)) FilterNeeds(); } }

    public ICommand RefreshCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand ShowStatsCommand { get; }
    public ICommand ExportCsvCommand { get; }

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
                "En attente" => new[] { NeedStatus.Draft, NeedStatus.ToValidate },
                "Transmis" => new[] { NeedStatus.TransmittedToPurchasing },
                "En cours" => new[] { NeedStatus.InPurchase },
                "Complété" => new[] { NeedStatus.Validated },
                "Annulé" => new[] { NeedStatus.Cancelled, NeedStatus.Rejected },
                _ => Array.Empty<NeedStatus>()
            };
            filtered = filtered.Where(n => targetStatus.Contains(n.Status));
        }

        // Période (simplifié)
        if (SelectedPeriod != "Tout")
        {
            var now = DateTime.UtcNow;
            var startDate = SelectedPeriod switch
            {
                "Ce mois" => new DateTime(now.Year, now.Month, 1),
                "3 derniers mois" => now.AddMonths(-3),
                "6 mois" => now.AddMonths(-6),
                "1 an" => now.AddYears(-1),
                _ => DateTime.MinValue
            };
            filtered = filtered.Where(n => n.RequestedAt >= startDate);
        }

        foreach (var n in filtered.OrderByDescending(x => x.RequestedAt))
            Needs.Add(new NeedHistoryItemViewModel(n));
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
