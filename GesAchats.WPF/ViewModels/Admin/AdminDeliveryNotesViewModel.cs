using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminDeliveryNoteItemViewModel : BaseViewModel
{
    public DeliveryNote DeliveryNote { get; }
    public string DateReception => DeliveryNote.ReceptionDate.ToString("dd/MM/yyyy");
    public string NumeroBL => DeliveryNote.DeliveryNumber;
    public string Fournisseur => DeliveryNote.Supplier?.CompanyName ?? "Inconnu";
    public string BCCorrespondant => DeliveryNote.PurchaseOrder?.OrderNumber ?? "Aucun";
    public string StatusText => DeliveryNote.Status switch
    {
        "EnAttente" => "En attente",
        "Valide" => "Validé",
        _ => DeliveryNote.Status
    };
    public string StatusColor => DeliveryNote.Status switch
    {
        "EnAttente" => "#FFC107",
        "Valide" => "#4CAF50",
        _ => "#9E9E9E"
    };

    public AdminDeliveryNoteItemViewModel(DeliveryNote deliveryNote)
    {
        DeliveryNote = deliveryNote;
    }
}

public class AdminDeliveryNotesViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;

    // Filter properties
    private string _searchText = string.Empty;
    private string _selectedFilterSupplier = "Tous";
    private string _selectedStatus = "Tous";
    private DateTime? _filterReceptionDate;

    private List<DeliveryNote> _allDeliveryNotes = new();

    // KPI Properties
    private int _totalBl;
    private int _blValides;
    private int _blEnAttente;
    private int _blAnnules;
    private int _fournisseursActifs;

    public int TotalBl { get => _totalBl; set => SetProperty(ref _totalBl, value); }
    public int BlValides { get => _blValides; set => SetProperty(ref _blValides, value); }
    public int BlEnAttente { get => _blEnAttente; set => SetProperty(ref _blEnAttente, value); }
    public int BlAnnules { get => _blAnnules; set => SetProperty(ref _blAnnules, value); }
    public int FournisseursActifs { get => _fournisseursActifs; set => SetProperty(ref _fournisseursActifs, value); }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterDeliveryNotes();
        }
    }

    public string SelectedFilterSupplier
    {
        get => _selectedFilterSupplier;
        set
        {
            if (SetProperty(ref _selectedFilterSupplier, value))
                FilterDeliveryNotes();
        }
    }

    public string SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
                FilterDeliveryNotes();
        }
    }

    public DateTime? FilterReceptionDate
    {
        get => _filterReceptionDate;
        set
        {
            if (SetProperty(ref _filterReceptionDate, value))
                FilterDeliveryNotes();
        }
    }

    public ObservableCollection<string> SupplierOptions { get; } = new();
    public ObservableCollection<string> StatusOptions { get; } = new() { "Tous", "En attente", "Validé" };
    public ObservableCollection<AdminDeliveryNoteItemViewModel> DeliveryNotes { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand ViewJustificatifCommand { get; }
    public ICommand ResetFiltersCommand { get; }

    public AdminDeliveryNotesViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "Réception des Bons de Livraison (BL)";

        RefreshCommand = new RelayCommand(async _ => await LoadData());
        ViewDetailsCommand = new RelayCommand(p => ExecuteViewDetails(p as AdminDeliveryNoteItemViewModel));
        ViewJustificatifCommand = new RelayCommand(p => ExecuteViewJustificatif(p as AdminDeliveryNoteItemViewModel));
        ResetFiltersCommand = new RelayCommand(_ => ExecuteResetFilters());

        _ = LoadInitialData();
    }

    private async Task LoadInitialData()
    {
        await LoadSuppliers();
        await LoadData();
    }

    private async Task LoadSuppliers()
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

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var notes = await _unitOfWork.DeliveryNotes.GetAllWithDetailsAsync();
            _allDeliveryNotes = notes.OrderByDescending(n => n.ReceptionDate).ToList();
            
            // Calculate KPIs
            TotalBl = _allDeliveryNotes.Count;
            BlValides = _allDeliveryNotes.Count(n => n.Status == "Valide");
            BlEnAttente = _allDeliveryNotes.Count(n => n.Status == "EnAttente");
            BlAnnules = _allDeliveryNotes.Count(n => n.Status == "Annulé" || n.Status == "Rejeté" || n.Status == "Annule" || n.Status == "Rejete");
            FournisseursActifs = _allDeliveryNotes.Where(n => n.SupplierId > 0).Select(n => n.SupplierId).Distinct().Count();

            FilterDeliveryNotes();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterDeliveryNotes()
    {
        var filtered = _allDeliveryNotes.AsEnumerable();

        // Search filter: BL number OR PO number
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(d =>
                d.DeliveryNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (d.PurchaseOrder != null && d.PurchaseOrder.OrderNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));
        }

        // Supplier filter
        if (SelectedFilterSupplier != "Tous")
        {
            filtered = filtered.Where(d => d.Supplier != null && d.Supplier.CompanyName == SelectedFilterSupplier);
        }

        // Status filter
        if (SelectedStatus != "Tous")
        {
            if (SelectedStatus == "En attente")
                filtered = filtered.Where(d => d.Status == "EnAttente");
            else if (SelectedStatus == "Validé")
                filtered = filtered.Where(d => d.Status == "Valide");
        }

        // Date filter
        if (FilterReceptionDate.HasValue)
        {
            filtered = filtered.Where(d => d.ReceptionDate.Date == FilterReceptionDate.Value.Date);
        }

        DeliveryNotes.Clear();
        foreach (var note in filtered)
        {
            DeliveryNotes.Add(new AdminDeliveryNoteItemViewModel(note));
        }
    }

    private void ExecuteResetFilters()
    {
        SearchText = string.Empty;
        SelectedFilterSupplier = "Tous";
        SelectedStatus = "Tous";
        FilterReceptionDate = null;
    }

    private void ExecuteViewDetails(AdminDeliveryNoteItemViewModel? item)
    {
        if (item == null) return;
        
        var window = new Views.Admin.DeliveryNotes.DeliveryNoteDetailsWindow();
        window.DataContext = new DeliveryNoteDetailsViewModel(item.DeliveryNote, _unitOfWork);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }

    private async void ExecuteViewJustificatif(AdminDeliveryNoteItemViewModel? item)
    {
        if (item == null) return;

        if (string.IsNullOrEmpty(item.DeliveryNote.FilePath))
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
                     item.DeliveryNote.FilePath = dialog.FileName;
                     _unitOfWork.DeliveryNotes.Update(item.DeliveryNote);
                     await _unitOfWork.CompleteAsync();
                     MessageBox.Show("Justificatif associé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
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
                    FileName = item.DeliveryNote.FilePath,
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
