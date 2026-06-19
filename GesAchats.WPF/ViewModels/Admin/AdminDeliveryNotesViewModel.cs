using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;

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
    private CancellationTokenSource? _cts;
    private System.Threading.Timer? _debounceTimer;

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

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                DebouncedFilter();
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

    public AdminDeliveryNotesViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
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
            
            TotalBl = allNotesList.Count;
            BlValides = allNotesList.Count(n => n.Status == "Valide");
            BlEnAttente = allNotesList.Count(n => n.Status == "EnAttente");
            BlAnnules = allNotesList.Count(n => n.Status == "Annulé" || n.Status == "Rejeté" || n.Status == "Annule" || n.Status == "Rejete");
            FournisseursActifs = allNotesList.Where(n => n.SupplierId > 0).Select(n => n.SupplierId).Distinct().Count();

            // Calculate yesterday's data
            DateTime today = DateTime.Today;
            DateTime yesterday = today.AddDays(-1);
            var yesterdayNotes = allNotesList.Where(n =>
                n.CreatedAt.Date >= yesterday && n.CreatedAt.Date < today).ToList();

            int yesterdayTotal = yesterdayNotes.Count;
            int yesterdayValides = yesterdayNotes.Count(n => n.Status == "Valide");
            int yesterdayEnAttente = yesterdayNotes.Count(n => n.Status == "EnAttente");
            int yesterdayAnnules = yesterdayNotes.Count(n => n.Status == "Annulé" || n.Status == "Rejeté" || n.Status == "Annule" || n.Status == "Rejete");
            int yesterdayFournisseurs = yesterdayNotes.Where(n => n.SupplierId > 0).Select(n => n.SupplierId).Distinct().Count();

            // Calculate trend texts
            TotalBlTrendText = CalculateTrendText(TotalBl, yesterdayTotal);
            BlValidesTrendText = CalculateTrendText(BlValides, yesterdayValides);
            BlEnAttenteTrendText = CalculateTrendText(BlEnAttente, yesterdayEnAttente);
            BlAnnulesTrendText = CalculateTrendText(BlAnnules, yesterdayAnnules);
            FournisseursActifsTrendText = CalculateTrendText(FournisseursActifs, yesterdayFournisseurs);

            await LoadPageAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void DebouncedFilter()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Threading.Timer(async _ =>
        {
            await ResetAndLoadPageAsync();
        }, null, 300, System.Threading.Timeout.Infinite);
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
            var result = await _unitOfWork.DeliveryNotes.GetBonsLivraisonPagedAsync(
                CurrentPage,
                PageSize,
                SearchText,
                SelectedFilterSupplier,
                SelectedStatus,
                FilterReceptionDate,
                _cts.Token);

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
                     fullNote.FilePath = dialog.FileName;
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
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fullNote.FilePath,
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