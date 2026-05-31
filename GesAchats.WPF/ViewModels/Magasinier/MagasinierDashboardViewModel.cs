using System.Windows.Input;
using System.Collections.ObjectModel;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class MagasinierDashboardViewModel : BaseViewModel
{
    private readonly IStockService _stockService;
    private readonly IUnitOfWork _unitOfWork;

    private decimal _totalStockUnits;
    private int _ruptureCount;
    private int _lowStockCount;
    private int _pendingNeedsCount;
    private int _deliveryCountThisWeek;
    private int _newProductsCount;

    public decimal TotalStockUnits { get => _totalStockUnits; set => SetProperty(ref _totalStockUnits, value); }
    public int RuptureCount { get => _ruptureCount; set => SetProperty(ref _ruptureCount, value); }
    public int LowStockCount { get => _lowStockCount; set => SetProperty(ref _lowStockCount, value); }
    public int PendingNeedsCount { get => _pendingNeedsCount; set => SetProperty(ref _pendingNeedsCount, value); }
    public int DeliveryCountThisWeek { get => _deliveryCountThisWeek; set => SetProperty(ref _deliveryCountThisWeek, value); }
    public int NewProductsCount { get => _newProductsCount; set => SetProperty(ref _newProductsCount, value); }

    public ObservableCollection<string> Alerts { get; } = new ObservableCollection<string>();

    public MagasinierDashboardViewModel(IStockService stockService, IUnitOfWork unitOfWork)
    {
        _stockService = stockService;
        _unitOfWork = unitOfWork;
        Title = "Dashboard - Espace Magasinier";
        LoadDataCommand = new RelayCommand(async _ => await LoadData());
        
        // Initial load
        _ = LoadData();
    }

    public ICommand LoadDataCommand { get; }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var allProducts = await _stockService.GetAllProductsAsync();
            TotalStockUnits = allProducts.Sum(p => p.CurrentStock);
            
            var ruptureProducts = await _stockService.GetRuptureProductsAsync();
            RuptureCount = ruptureProducts.Count();
            
            var lowStockProducts = await _stockService.GetLowStockProductsAsync();
            LowStockCount = lowStockProducts.Count();

            var pendingNeeds = await _unitOfWork.Needs.FindAsync(n => n.Status == NeedStatus.Draft || n.Status == NeedStatus.ToValidate);
            PendingNeedsCount = pendingNeeds.Count();

            var startOfWeek = DateTime.UtcNow.Date.AddDays(-(int)DateTime.UtcNow.DayOfWeek);
            var deliveries = await _unitOfWork.DeliveryNotes.FindAsync(d => d.ReceptionDate >= startOfWeek);
            DeliveryCountThisWeek = deliveries.Count();

            // Update Alerts
            Alerts.Clear();
            foreach (var p in ruptureProducts.Take(3))
                Alerts.Add($"🔴 {p.Designation} - RUPTURE CRITIQUE");
            
            foreach (var p in lowStockProducts.Take(3))
                Alerts.Add($"🟡 {p.Designation} - Sous le minimum");
        }
        catch (Exception)
        {
            // Log error
        }
        finally
        {
            IsBusy = false;
        }
    }
}
