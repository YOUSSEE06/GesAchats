using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Services;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminHistoriqueSortiesViewModel : BaseViewModel, INavigatable
{
    private readonly IStockService _stockService;
    private string _searchText = string.Empty;
    private DateTime? _filterDate;
    private int _totalExits;
    private decimal _totalQuantityExited;
    private string _totalExitsTrendText = string.Empty;
    private string _totalQuantityExitedTrendText = string.Empty;

    public ObservableCollection<StockExit> StockExits { get; } = new();
    private List<StockExit> _allExits = new();

    public int TotalExits
    {
        get => _totalExits;
        set => SetProperty(ref _totalExits, value);
    }

    public decimal TotalQuantityExited
    {
        get => _totalQuantityExited;
        set => SetProperty(ref _totalQuantityExited, value);
    }

    public string TotalExitsTrendText
    {
        get => _totalExitsTrendText;
        set => SetProperty(ref _totalExitsTrendText, value);
    }

    public string TotalQuantityExitedTrendText
    {
        get => _totalQuantityExitedTrendText;
        set => SetProperty(ref _totalQuantityExitedTrendText, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                ApplyFilters();
        }
    }

    public DateTime? FilterDate
    {
        get => _filterDate;
        set
        {
            if (SetProperty(ref _filterDate, value))
                ApplyFilters();
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand ResetFiltersCommand { get; }

    public AdminHistoriqueSortiesViewModel(IStockService stockService)
    {
        _stockService = stockService;
        Title = "Historique des sorties";

        RefreshCommand = new RelayCommand(async _ => await LoadDataAsync());
        ResetFiltersCommand = new RelayCommand(_ => ExecuteResetFilters());
    }

    public async void OnNavigatedTo(object parameter)
    {
        await LoadDataAsync();
    }

    private void ExecuteResetFilters()
    {
        SearchText = string.Empty;
        FilterDate = null;
    }

    private async Task LoadDataAsync()
    {
        IsBusy = true;
        try
        {
            var exits = await _stockService.GetAllStockExitsAsync();
            _allExits = exits.ToList();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors du chargement des données : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ApplyFilters()
    {
        var filtered = _allExits.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(e => e.Product != null && e.Product.Designation.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (FilterDate.HasValue)
        {
            filtered = filtered.Where(e => e.ExitDate.Date == FilterDate.Value.Date);
        }

        var filteredList = filtered.ToList();
        StockExits.Clear();
        foreach (var e in filteredList)
        {
            StockExits.Add(e);
        }

        // Calculate statistics
        TotalExits = filteredList.Count;
        TotalQuantityExited = filteredList.Sum(e => e.Quantity);

        // Calculate yesterday's values
        DateTime today = DateTime.Today;
        DateTime yesterday = today.AddDays(-1);

        var yesterdayExits = filteredList.Where(e =>
            e.CreatedAt.Date >= yesterday && e.CreatedAt.Date < today).ToList();

        int yesterdayTotalCount = yesterdayExits.Count;
        decimal yesterdayTotalQuantity = yesterdayExits.Sum(e => e.Quantity);

        // Calculate trend texts
        TotalExitsTrendText = CalculateTrendText(TotalExits, yesterdayTotalCount);
        TotalQuantityExitedTrendText = CalculateTrendText(TotalQuantityExited, yesterdayTotalQuantity);
    }
}
