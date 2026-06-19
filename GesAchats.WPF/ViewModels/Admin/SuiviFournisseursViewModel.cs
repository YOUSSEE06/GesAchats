using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.DTOs;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Admin;

public class SuiviFournisseursViewModel : BaseViewModel
{
    private readonly ISuiviFournisseurService _service;
    private readonly INavigationService _navigationService;
    private string _searchText = string.Empty;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _totalFournisseurs;
    private int _totalFilteredCount;
    private int _commandesEnCours;
    private decimal _totalCommande;
    private decimal _soldeTotal;
    private int _pageActuelle = 1;
    private int _totalPages;
    private const int ItemsPerPage = 10;

    public ObservableCollection<FournisseurSuiviDto> Fournisseurs { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                PerformSearch();
            }
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public int TotalFournisseurs
    {
        get => _totalFournisseurs;
        set => SetProperty(ref _totalFournisseurs, value);
    }

    public int CommandesEnCours
    {
        get => _commandesEnCours;
        set => SetProperty(ref _commandesEnCours, value);
    }

    public decimal TotalCommande
    {
        get => _totalCommande;
        set => SetProperty(ref _totalCommande, value);
    }

    public decimal SoldeTotal
    {
        get => _soldeTotal;
        set => SetProperty(ref _soldeTotal, value);
    }

    public int PageActuelle
    {
        get => _pageActuelle;
        set
        {
            if (SetProperty(ref _pageActuelle, value))
            {
                LoadFournisseursAsync().ConfigureAwait(false);
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        set
        {
            if (SetProperty(ref _totalPages, value))
            {
                OnPropertyChanged(nameof(PaginationInfo));
                UpdatePaginationButtons();
            }
        }
    }

    public string PaginationInfo
    {
        get
        {
            var start = (PageActuelle - 1) * ItemsPerPage + 1;
            var end = Math.Min(PageActuelle * ItemsPerPage, _totalFilteredCount);
            return $"Affichage de {start} à {end} sur {_totalFilteredCount} fournisseurs";
        }
    }

    public bool CanGoFirst => PageActuelle > 1;
    public bool CanGoPrevious => PageActuelle > 1;
    public bool CanGoNext => PageActuelle < TotalPages;
    public bool CanGoLast => PageActuelle < TotalPages;

    public ICommand ResetSearchCommand { get; }
    public ICommand PageFirstCommand { get; }
    public ICommand PagePreviousCommand { get; }
    public ICommand PageNextCommand { get; }
    public ICommand PageLastCommand { get; }
    public ICommand PageNumberCommand { get; }
    public ICommand VoirSituationCommand { get; }

    public SuiviFournisseursViewModel(ISuiviFournisseurService service, INavigationService navigationService)
    {
        _service = service;
        _navigationService = navigationService;
        Title = "Suivi Fournisseurs";

        ResetSearchCommand = new RelayCommand(_ => ResetSearch());
        PageFirstCommand = new RelayCommand(_ => GoToFirstPage(), _ => CanGoFirst);
        PagePreviousCommand = new RelayCommand(_ => GoToPreviousPage(), _ => CanGoPrevious);
        PageNextCommand = new RelayCommand(_ => GoToNextPage(), _ => CanGoNext);
        PageLastCommand = new RelayCommand(_ => GoToLastPage(), _ => CanGoLast);
        PageNumberCommand = new RelayCommand(param => GoToPage((int)(param ?? 1)));
        VoirSituationCommand = new RelayCommand(param => VoirSituation((int)(param ?? 0)));

        InitializeAsync().ConfigureAwait(false);
    }

    private async System.Threading.Tasks.Task InitializeAsync()
    {
        await LoadKPIsAsync();
        await LoadFournisseursAsync();
    }

    private async System.Threading.Tasks.Task LoadKPIsAsync()
    {
        try
        {
            TotalFournisseurs = await _service.GetTotalFournisseursAsync();
            CommandesEnCours = await _service.GetCommandesEnCoursAsync();
            TotalCommande = await _service.GetTotalCommandeAsync();
            SoldeTotal = await _service.GetSoldeTotalAsync();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors du chargement des KPIs");
            ErrorMessage = "Une erreur s'est produite lors du chargement des statistiques.";
        }
    }

    private async System.Threading.Tasks.Task LoadFournisseursAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        Fournisseurs.Clear();

        try
        {
            var result = await _service.SearchFournisseursAsync(SearchText, PageActuelle, ItemsPerPage);
            foreach (var fournisseur in result.Items)
            {
                Fournisseurs.Add(fournisseur);
            }
            _totalFilteredCount = result.TotalCount;
            TotalPages = result.TotalPages;
            OnPropertyChanged(nameof(PaginationInfo));
            UpdatePaginationButtons();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Erreur lors du chargement des fournisseurs");
            ErrorMessage = "Une erreur s'est produite. Veuillez réessayer.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private System.Threading.Timer? _searchTimer;
    private void PerformSearch()
    {
        _searchTimer?.Dispose();
        _searchTimer = new System.Threading.Timer(async _ =>
        {
            PageActuelle = 1;
            await LoadFournisseursAsync();
        }, null, 300, System.Threading.Timeout.Infinite);
    }

    private void ResetSearch()
    {
        SearchText = string.Empty;
        PageActuelle = 1;
        LoadFournisseursAsync().ConfigureAwait(false);
    }

    private void GoToFirstPage()
    {
        PageActuelle = 1;
    }

    private void GoToPreviousPage()
    {
        if (PageActuelle > 1)
            PageActuelle--;
    }

    private void GoToNextPage()
    {
        if (PageActuelle < TotalPages)
            PageActuelle++;
    }

    private void GoToLastPage()
    {
        PageActuelle = TotalPages;
    }

    private void GoToPage(int page)
    {
        if (page >= 1 && page <= TotalPages)
            PageActuelle = page;
    }

    private void VoirSituation(int fournisseurId)
    {
        if (fournisseurId > 0)
        {
            _navigationService.NavigateTo("SituationFournisseur", fournisseurId);
        }
    }

    private void UpdatePaginationButtons()
    {
        OnPropertyChanged(nameof(CanGoFirst));
        OnPropertyChanged(nameof(CanGoPrevious));
        OnPropertyChanged(nameof(CanGoNext));
        OnPropertyChanged(nameof(CanGoLast));
        CommandManager.InvalidateRequerySuggested();
    }
}
