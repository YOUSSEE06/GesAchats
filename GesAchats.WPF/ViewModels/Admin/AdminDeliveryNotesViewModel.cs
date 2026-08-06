using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using Serilog;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminDeliveryNoteItemViewModel : BaseViewModel
{
    public int Id { get; }
    public string DateReception { get; }
    public string NumeroBL { get; }
    public string Fournisseur { get; }
    public string BCCorrespondant { get; }
    public string StatusText { get; }
    public string StatusColor { get; }

    public AdminDeliveryNoteItemViewModel(DeliveryNoteHistoryDto dto)
    {
        Id = dto.Id;
        DateReception = dto.ReceptionDate.ToString("dd/MM/yyyy");
        NumeroBL = dto.DeliveryNumber;
        Fournisseur = dto.SupplierCompanyName;
        BCCorrespondant = dto.PurchaseOrderNumber ?? "Aucun";
        StatusText = dto.Status switch
        {
            "EnAttente" => "En attente",
            "Valide" => "Validé",
            _ => dto.Status
        };
        StatusColor = dto.Status switch
        {
            "EnAttente" => "#FFC107",
            "Valide" => "#4CAF50",
            _ => "#9E9E9E"
        };
    }
}

public class AdminDeliveryNotesViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private readonly IFileStorageService _fileStorageService;
    private CancellationTokenSource? _searchCts;
    private int _loadVersion;

    // Filter properties
    private string _searchText = string.Empty;
    private string _selectedFilterSupplier = "Tous";
    private string _selectedStatus = "Tous";
    private DateTime? _filterReceptionDate;

    // Pagination properties
    private int _currentPage = 1;
    private int _totalItems;
    private const int PageSize = 20;

    // KPI Properties
    private int _totalBl;
    private int _blValides;
    private int _blEnAttente;
    private int _blAnnules;
    private int _fournisseursActifs;
    private string _totalBlTrendText = string.Empty;
    private string _blValidesTrendText = string.Empty;
    private string _blEnAttenteTrendText = string.Empty;
    private string _blAnnulesTrendText = string.Empty;
    private string _fournisseursActifsTrendText = string.Empty;

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
                return "Affichage de 0 à 0 sur 0 bons de livraison";
            return $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} bons de livraison";
        }
    }

    public int TotalBl { get => _totalBl; set => SetProperty(ref _totalBl, value); }
    public int BlValides { get => _blValides; set => SetProperty(ref _blValides, value); }
    public int BlEnAttente { get => _blEnAttente; set => SetProperty(ref _blEnAttente, value); }
    public int BlAnnules { get => _blAnnules; set => SetProperty(ref _blAnnules, value); }
    public int FournisseursActifs { get => _fournisseursActifs; set => SetProperty(ref _fournisseursActifs, value); }

    public string TotalBlTrendText
    {
        get => _totalBlTrendText;
        set => SetProperty(ref _totalBlTrendText, value);
    }
    public string BlValidesTrendText
    {
        get => _blValidesTrendText;
        set => SetProperty(ref _blValidesTrendText, value);
    }
    public string BlEnAttenteTrendText
    {
        get => _blEnAttenteTrendText;
        set => SetProperty(ref _blEnAttenteTrendText, value);
    }
    public string BlAnnulesTrendText
    {
        get => _blAnnulesTrendText;
        set => SetProperty(ref _blAnnulesTrendText, value);
    }
    public string FournisseursActifsTrendText
    {
        get => _fournisseursActifsTrendText;
        set => SetProperty(ref _fournisseursActifsTrendText, value);
    }

    // Filtre de période (identique au Dashboard principal)
    private DateTime _startDate = DateTime.Today.AddMonths(-1);
    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    private DateTime _endDate = DateTime.Today;
    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    // Couleurs de tendance KPI
    private bool _totalBlIsPositiveTrend = true;
    public bool TotalBlIsPositiveTrend { get => _totalBlIsPositiveTrend; set => SetProperty(ref _totalBlIsPositiveTrend, value); }
    private bool _blValidesIsPositiveTrend = true;
    public bool BlValidesIsPositiveTrend { get => _blValidesIsPositiveTrend; set => SetProperty(ref _blValidesIsPositiveTrend, value); }
    private bool _blEnAttenteIsPositiveTrend = true;
    public bool BlEnAttenteIsPositiveTrend { get => _blEnAttenteIsPositiveTrend; set => SetProperty(ref _blEnAttenteIsPositiveTrend, value); }
    private bool _blAnnulesIsPositiveTrend = true;
    public bool BlAnnulesIsPositiveTrend { get => _blAnnulesIsPositiveTrend; set => SetProperty(ref _blAnnulesIsPositiveTrend, value); }
    private bool _fournisseursActifsIsPositiveTrend = true;
    public bool FournisseursActifsIsPositiveTrend { get => _fournisseursActifsIsPositiveTrend; set => SetProperty(ref _fournisseursActifsIsPositiveTrend, value); }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                _ = DebouncedFilterAsync();
        }
    }

    public string SelectedFilterSupplier
    {
        get => _selectedFilterSupplier;
        set
        {
            if (SetProperty(ref _selectedFilterSupplier, value))
                _ = ResetAndLoadPageAsync();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
                _ = ResetAndLoadPageAsync();
        }
    }

    public DateTime? FilterReceptionDate
    {
        get => _filterReceptionDate;
        set
        {
            if (SetProperty(ref _filterReceptionDate, value))
                _ = ResetAndLoadPageAsync();
        }
    }

    public ObservableCollection<string> SupplierOptions { get; } = new();
    public ObservableCollection<string> StatusOptions { get; } = new() { "Tous", "En attente", "Validé" };
    public ObservableCollection<AdminDeliveryNoteItemViewModel> DeliveryNotes { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand ViewJustificatifCommand { get; }
    public ICommand ResetFiltersCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    public AdminDeliveryNotesViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger logger, IFileStorageService fileStorageService)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _fileStorageService = fileStorageService;
        Title = "Réception des Bons de Livraison (BL)";

        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        ViewDetailsCommand = new RelayCommand(p => ExecuteViewDetails(p as AdminDeliveryNoteItemViewModel));
        ViewJustificatifCommand = new RelayCommand(p => ExecuteViewJustificatif(p as AdminDeliveryNoteItemViewModel));
        ResetFiltersCommand = new RelayCommand(async _ => await ExecuteResetFiltersAsync());
        FirstPageCommand = new RelayCommand(async _ => await GoToFirstPageAsync(), _ => CanGoToFirstPage);
        PreviousPageCommand = new RelayCommand(async _ => await GoToPreviousPageAsync(), _ => CanGoToPreviousPage);
        NextPageCommand = new RelayCommand(async _ => await GoToNextPageAsync(), _ => CanGoToNextPage);
        LastPageCommand = new RelayCommand(async _ => await GoToLastPageAsync(), _ => CanGoToLastPage);

        _ = LoadInitialDataAsync();
    }

    private async Task LoadInitialDataAsync()
    {
        await LoadSuppliersAsync();
        await LoadDataAsync();
    }

    private async Task LoadSuppliersAsync()
    {
        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        SupplierOptions.Clear();
        SupplierOptions.Add("Tous");
        foreach (var s in suppliers.OrderBy(x => x.CompanyName))
        {
            if (!SupplierOptions.Contains(s.CompanyName))
                SupplierOptions.Add(s.CompanyName);
        }
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            // Calculate KPIs using all delivery notes
            var allNotes = await _unitOfWork.DeliveryNotes.GetAllWithDetailsAsync();
            var allNotesList = allNotes.ToList();

            // Période actuelle vs période précédente de même durée (comme le Dashboard principal)
            var start = StartDate.Date;
            var end = EndDate.Date.AddDays(1);
            var duration = EndDate - StartDate;
            var previousStart = StartDate - duration;
            var previousEnd = StartDate;

            var currentNotes = allNotesList.Where(n => n.ReceptionDate >= start && n.ReceptionDate < end).ToList();
            var previousNotes = allNotesList.Where(n => n.ReceptionDate >= previousStart && n.ReceptionDate < previousEnd).ToList();

            TotalBl = currentNotes.Count;
            BlValides = currentNotes.Count(n => n.Status == "Valide");
            BlEnAttente = currentNotes.Count(n => n.Status == "EnAttente");
            BlAnnules = currentNotes.Count(n => n.Status == "Annulé" || n.Status == "Rejeté" || n.Status == "Annule" || n.Status == "Rejete");
            FournisseursActifs = currentNotes.Where(n => n.SupplierId > 0).Select(n => n.SupplierId).Distinct().Count();

            int previousTotal = previousNotes.Count;
            int previousValides = previousNotes.Count(n => n.Status == "Valide");
            int previousEnAttente = previousNotes.Count(n => n.Status == "EnAttente");
            int previousAnnules = previousNotes.Count(n => n.Status == "Annulé" || n.Status == "Rejeté" || n.Status == "Annule" || n.Status == "Rejete");
            int previousFournisseurs = previousNotes.Where(n => n.SupplierId > 0).Select(n => n.SupplierId).Distinct().Count();

            // Tendances (texte + couleur), formule ((current - previous) / previous) * 100
            TotalBlTrendText = FormatTrend(CalculateVariation(TotalBl, previousTotal), out bool totalPos);
            TotalBlIsPositiveTrend = totalPos;
            BlValidesTrendText = FormatTrend(CalculateVariation(BlValides, previousValides), out bool validesPos);
            BlValidesIsPositiveTrend = validesPos;
            BlEnAttenteTrendText = FormatTrend(CalculateVariation(BlEnAttente, previousEnAttente), out bool attentePos);
            BlEnAttenteIsPositiveTrend = attentePos;
            BlAnnulesTrendText = FormatTrend(CalculateVariation(BlAnnules, previousAnnules), out bool annulesPos);
            BlAnnulesIsPositiveTrend = annulesPos;
            FournisseursActifsTrendText = FormatTrend(CalculateVariation(FournisseursActifs, previousFournisseurs), out bool fournisseursPos);
            FournisseursActifsIsPositiveTrend = fournisseursPos;

            await LoadPageAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task DebouncedFilterAsync()
    {
        var newCts = new CancellationTokenSource();
        var oldCts = Interlocked.Exchange(ref _searchCts, newCts);

        if (oldCts != null)
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        try
        {
            await Task.Delay(400, newCts.Token);
            CurrentPage = 1;
            await LoadPageAsync(newCts.Token);
        }
        catch (OperationCanceledException) when (newCts.IsCancellationRequested)
        {
            // Normal cancellation, do nothing
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error during debounced filter");
        }
        finally
        {
            if (ReferenceEquals(_searchCts, newCts))
            {
                Interlocked.CompareExchange(ref _searchCts, null, newCts);
            }
            newCts.Dispose();
        }
    }

    private async Task ResetAndLoadPageAsync()
    {
        CurrentPage = 1;
        await LoadPageAsync();
    }

    private async Task LoadPageAsync(CancellationToken cancellationToken = default)
    {
        var version = Interlocked.Increment(ref _loadVersion);

        try
        {
            var result = await _unitOfWork.DeliveryNotes.GetBonsLivraisonPagedAsync(
                CurrentPage,
                PageSize,
                SearchText,
                SelectedFilterSupplier,
                SelectedStatus,
                FilterReceptionDate,
                cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            if (version != _loadVersion)
                return;

            // Update UI on dispatcher thread
            if (System.Windows.Application.Current.Dispatcher.CheckAccess())
            {
                UpdateItems(result);
            }
            else
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() => UpdateItems(result), DispatcherPriority.Normal, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Operation cancelled, ignore
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error loading page");
        }
    }

    private void UpdateItems(PagedResult<DeliveryNoteHistoryDto> result)
    {
        DeliveryNotes.Clear();
        foreach (var dto in result.Items)
        {
            DeliveryNotes.Add(new AdminDeliveryNoteItemViewModel(dto));
        }

        TotalItems = result.TotalCount;

        // If current page is beyond total pages, go to last page
        if (CurrentPage > TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages;
            _ = LoadPageAsync();
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

    private async Task ExecuteResetFiltersAsync()
    {
        SearchText = string.Empty;
        SelectedFilterSupplier = "Tous";
        SelectedStatus = "Tous";
        FilterReceptionDate = null;
        await ResetAndLoadPageAsync();
    }

    private async void ExecuteViewDetails(AdminDeliveryNoteItemViewModel? item)
    {
        if (item == null) return;
        
        var fullNote = await _unitOfWork.DeliveryNotes.GetByIdAsync(item.Id);
        if (fullNote == null) return;

        var window = new Views.Admin.DeliveryNotes.DeliveryNoteDetailsWindow();
        window.DataContext = new DeliveryNoteDetailsViewModel(fullNote, _unitOfWork);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
        
        await LoadDataAsync();
    }

    private async void ExecuteViewJustificatif(AdminDeliveryNoteItemViewModel? item)
    {
        if (item == null) return;
        
        var fullNote = await _unitOfWork.DeliveryNotes.GetByIdAsync(item.Id);
        if (fullNote == null) return;

        if (string.IsNullOrEmpty(fullNote.FilePath))
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Sélectionner le justificatif (PDF, Image)",
                Filter = "Fichiers documents (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                 {
                     // Fichier importé -> envoyé sur Supabase Storage (référence distante)
                     fullNote.FilePath = await _fileStorageService.UploadImportedFileAsync("bonlivraison", "BL", dialog.FileName);
                     _unitOfWork.DeliveryNotes.Update(fullNote);
                     await _unitOfWork.CompleteAsync();
                     MessageBox.Show("Justificatif associé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                     await LoadPageAsync();
                 }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            try
            {
                string fullPath = await _fileStorageService.GetFullPathAsync(fullNote.FilePath);
                if (!System.IO.File.Exists(fullPath))
                {
                    MessageBox.Show($"Fichier introuvable : {fullPath}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fullPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le fichier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

public class DeliveryNoteDetailsViewModel : BaseViewModel
{
    private readonly DeliveryNote _deliveryNote;
    private readonly IUnitOfWork _unitOfWork;

    public DeliveryNote DeliveryNote => _deliveryNote;
    public ObservableCollection<DeliveryNoteDetail> Details { get; } = new();

    public string TitleText => $"Détails du Bon de Livraison : {_deliveryNote.DeliveryNumber}";
    public string SupplierName => _deliveryNote.Supplier?.CompanyName ?? "Inconnu";
    public string DateReception => _deliveryNote.ReceptionDate.ToString("dd/MM/yyyy");
    public string BCNumber => _deliveryNote.PurchaseOrder?.OrderNumber ?? "N/A";

    public DeliveryNoteDetailsViewModel(DeliveryNote deliveryNote, IUnitOfWork unitOfWork)
    {
        _deliveryNote = deliveryNote;
        _unitOfWork = unitOfWork;
        _ = LoadDetails();
    }

    private async Task LoadDetails()
    {
        // On recharge avec les détails si nécessaire
        var fullNote = await _unitOfWork.DeliveryNotes.GetByIdAsync(_deliveryNote.Id);
        if (fullNote?.Details != null)
        {
            foreach (var detail in fullNote.Details)
            {
                Details.Add(detail);
            }
        }
    }
}
