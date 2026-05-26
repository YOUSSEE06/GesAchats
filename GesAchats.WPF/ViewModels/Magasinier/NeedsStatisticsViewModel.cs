using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using GesAchats.Core.Services;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class StatItem
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Color { get; set; } = "#1ABC9C";
}

public class NeedsStatisticsViewModel : BaseViewModel
{
    private readonly INeedsAnalyticsService _analyticsService;

    public int TotalNeeds { get; set; }
    public string CompletedPercentage { get; set; } = "0%";
    public string InProgressPercentage { get; set; } = "0%";
    public string PendingPercentage { get; set; } = "0%";
    public string CancelledPercentage { get; set; } = "0%";

    public ObservableCollection<StatItem> StatusDistribution { get; } = new();
    public ObservableCollection<StatItem> TopProducts { get; } = new();

    public NeedsStatisticsViewModel(INeedsAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
        _ = LoadStats();
    }

    private async Task LoadStats()
    {
        IsBusy = true;
        try
        {
            var summary = await _analyticsService.GetGlobalSummaryAsync();
            TotalNeeds = summary["Total"];
            
            if (TotalNeeds > 0)
            {
                CompletedPercentage = $"{(double)summary["Completed"] / TotalNeeds:P0}";
                InProgressPercentage = $"{(double)summary["InProgress"] / TotalNeeds:P0}";
                PendingPercentage = $"{(double)summary["Pending"] / TotalNeeds:P0}";
                CancelledPercentage = $"{(double)summary["Cancelled"] / TotalNeeds:P0}";
            }

            var distribution = await _analyticsService.GetStatusDistributionAsync();
            StatusDistribution.Clear();
            foreach (var item in distribution)
            {
                StatusDistribution.Add(new StatItem { Label = item.Key, Value = item.Value });
            }

            var products = await _analyticsService.GetTopRequestedProductsAsync();
            TopProducts.Clear();
            foreach (var item in products)
            {
                TopProducts.Add(new StatItem { Label = item.Key, Value = item.Value });
            }
        }
        finally
        {
            IsBusy = false;
        }
    }
}
