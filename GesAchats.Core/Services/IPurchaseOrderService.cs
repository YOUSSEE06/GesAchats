namespace GesAchats.Core.Services;

public interface IPurchaseOrderService
{
    Task<int> GetPendingPurchaseOrdersCountAsync(DateTime startDate, DateTime endDate);
    Task<int> GetPurchaseOrderCountByStatusAsync(string status);
    Task<List<MonthlyPurchaseData>> GetMonthlyPurchaseAmountAsync(int monthCount);
    Task<List<RecentPurchaseOrderData>> GetRecentPurchaseOrdersAsync(int count);
    Task<(int Pending, int Validated, int Cancelled)> GetPurchaseOrderStatusCountsAsync();
}
