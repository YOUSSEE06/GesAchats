using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Views.Acheteur.Fournisseurs;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class SupplierManagementViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private Supplier? _selectedSupplier;
    private List<Supplier> _allSuppliers = new();
    private List<Supplier> _allFilteredSuppliers = new();
    private string _searchText = string.Empty;

    private int _currentPage = 1;
    private int _itemsPerPage = 20;
    private int _totalPages = 1;
    private int _totalItems;

    public ObservableCollection<Supplier> Suppliers { get; } = new();

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                FilterSuppliers();
            }
        }
    }

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetProperty(ref _selectedSupplier, value);
    }

    public ICommand AddSupplierCommand { get; }
    public ICommand EditSupplierCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand FirstPageCommand { get; }
    public ICommand PreviousPageCommand { get; }
    public ICommand NextPageCommand { get; }
    public ICommand LastPageCommand { get; }

    public string PaginationText
    {
        get
        {
            if (TotalItems == 0) return "Aucun fournisseur";
            var unit = TotalItems > 1 ? "fournisseurs" : "fournisseur";
            return $"Affichage de {FirstDisplayedItem} à {LastDisplayedItem} sur {TotalItems} {unit}";
        }
    }

    // ===================== PAGINATION =====================
    public int CurrentPage
    {
        get => _currentPage;
        set
        {
            if (SetProperty(ref _currentPage, value))
            {
                UpdatePagedSuppliers();
            }
        }
    }

    public int ItemsPerPage
    {
        get => _itemsPerPage;
        set
        {
            if (SetProperty(ref _itemsPerPage, value) && value > 0)
            {
                CalculateTotalPages();
                if (CurrentPage > TotalPages) CurrentPage = TotalPages;
                UpdatePagedSuppliers();
            }
        }
    }

    public int TotalPages
    {
        get => _totalPages;
        set => SetProperty(ref _totalPages, value);
    }

    public int TotalItems
    {
        get => _totalItems;
        set
        {
            if (SetProperty(ref _totalItems, value))
            {
                CalculateTotalPages();
                OnPropertyChanged(nameof(FirstDisplayedItem));
                OnPropertyChanged(nameof(LastDisplayedItem));
                OnPropertyChanged(nameof(PaginationText));
            }
        }
    }

    public int FirstDisplayedItem => TotalItems == 0 ? 0 : ((CurrentPage - 1) * ItemsPerPage) + 1;

    public int LastDisplayedItem => Math.Min(CurrentPage * ItemsPerPage, TotalItems);

    private void CalculateTotalPages()
    {
        TotalPages = (int)Math.Ceiling((double)_allFilteredSuppliers.Count / ItemsPerPage);
        if (TotalPages < 1) TotalPages = 1;
    }

    private void UpdatePagedSuppliers()
    {
        Suppliers.Clear();
        var start = (CurrentPage - 1) * ItemsPerPage;
        foreach (var s in _allFilteredSuppliers.Skip(start).Take(ItemsPerPage))
        {
            Suppliers.Add(s);
        }

        OnPropertyChanged(nameof(FirstDisplayedItem));
        OnPropertyChanged(nameof(LastDisplayedItem));
        OnPropertyChanged(nameof(PaginationText));

        // Notifie les commandes de pagination (états désactivés des boutons)
        CommandManager.InvalidateRequerySuggested();
    }

    public SupplierManagementViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "Gestion des Fournisseurs";

        AddSupplierCommand = new RelayCommand(async _ => await OpenAddSupplierDialog());
        EditSupplierCommand = new RelayCommand(async _ => await OpenEditSupplierDialog(), _ => SelectedSupplier != null);
        RefreshCommand = new RelayCommand(async _ => await LoadSuppliers());
        FirstPageCommand = new RelayCommand(_ => CurrentPage = 1, _ => CurrentPage > 1);
        PreviousPageCommand = new RelayCommand(_ => { if (CurrentPage > 1) CurrentPage--; }, _ => CurrentPage > 1);
        NextPageCommand = new RelayCommand(_ => { if (CurrentPage < TotalPages) CurrentPage++; }, _ => CurrentPage < TotalPages);
        LastPageCommand = new RelayCommand(_ => CurrentPage = TotalPages, _ => CurrentPage < TotalPages);

        _ = LoadSuppliers();
    }

    private async Task LoadSuppliers()
    {
        IsBusy = true;
        try
        {
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            _allSuppliers = suppliers.OrderBy(x => x.CompanyName).ToList();
            FilterSuppliers();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterSuppliers()
    {
        var filtered = _allSuppliers.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.ToLower();
            filtered = filtered.Where(s => 
                s.CompanyName.ToLower().Contains(searchLower) || 
                (s.ContactName != null && s.ContactName.ToLower().Contains(searchLower)) ||
                (s.Email != null && s.Email.ToLower().Contains(searchLower)) ||
                (s.Phone != null && s.Phone.ToLower().Contains(searchLower)) ||
                (s.City != null && s.City.ToLower().Contains(searchLower)));
        }
        
        var filteredList = filtered.ToList();
        _allFilteredSuppliers = filteredList;

        // Réinitialise la pagination (recalcul du total + retour page 1)
        TotalItems = _allFilteredSuppliers.Count;
        CurrentPage = 1;
        UpdatePagedSuppliers();
    }

    private async Task OpenAddSupplierDialog()
    {
        var viewModel = ActivatorUtilities.CreateInstance<SupplierDialogViewModel>(_serviceProvider);
        var dialog = new SupplierDialog(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadSuppliers();
        }
    }

    private async Task OpenEditSupplierDialog()
    {
        if (SelectedSupplier == null) return;

        var viewModel = ActivatorUtilities.CreateInstance<SupplierDialogViewModel>(_serviceProvider, SelectedSupplier);
        var dialog = new SupplierDialog(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadSuppliers();
            SelectedSupplier = null;
        }
    }
}
