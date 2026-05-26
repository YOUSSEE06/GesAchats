using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public interface INeedsAnalyticsService
{
    Task<Dictionary<string, int>> GetGlobalSummaryAsync();
    Task<Dictionary<string, int>> GetStatusDistributionAsync();
    Task<Dictionary<string, int>> GetTopRequestedProductsAsync(int count = 5);
}

public class NeedsAnalyticsService : INeedsAnalyticsService
{
    private readonly IUnitOfWork _unitOfWork;

    public NeedsAnalyticsService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Dictionary<string, int>> GetGlobalSummaryAsync()
    {
        var allNeeds = await _unitOfWork.Needs.GetAllAsync();
        return new Dictionary<string, int>
        {
            { "Total", allNeeds.Count() },
            { "Completed", allNeeds.Count(n => n.Status == NeedStatus.Validated) },
            { "InProgress", allNeeds.Count(n => n.Status == NeedStatus.InPurchase || n.Status == NeedStatus.TransmittedToPurchasing) },
            { "Pending", allNeeds.Count(n => n.Status == NeedStatus.Draft || n.Status == NeedStatus.ToValidate) },
            { "Cancelled", allNeeds.Count(n => n.Status == NeedStatus.Cancelled || n.Status == NeedStatus.Rejected) }
        };
    }

    public async Task<Dictionary<string, int>> GetStatusDistributionAsync()
    {
        var allNeeds = await _unitOfWork.Needs.GetAllAsync();
        return allNeeds.GroupBy(n => n.Status.ToString())
                      .ToDictionary(g => g.Key, g => g.Count());
    }

    public async Task<Dictionary<string, int>> GetTopRequestedProductsAsync(int count = 5)
    {
        var details = await _unitOfWork.NeedDetails.GetAllAsync();
        return details.GroupBy(d => d.Product?.Designation ?? "Inconnu")
                      .OrderByDescending(g => g.Count())
                      .Take(count)
                      .ToDictionary(g => g.Key, g => g.Count());
    }
}
