using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IUnitOfWork _unitOfWork;

    public PurchaseOrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> GetPendingPurchaseOrdersCountAsync(DateTime startDate, DateTime endDate)
    {
        var orders = await _unitOfWork.PurchaseOrders.FindAsync(po =>
            po.Status == PurchaseOrderStatus.Pending &&
            po.CreatedAt >= startDate &&
            po.CreatedAt <= endDate);
        return orders.Count();
    }

    public async Task<int> GetPurchaseOrderCountByStatusAsync(string status)
    {
        // Convert to French status if needed
        var frenchStatus = status switch
        {
            "Pending" => PurchaseOrderStatus.Pending,
            "Validated" => PurchaseOrderStatus.Validated,
            "Transmitted" => "Transmis",
            _ => status
        };
        
        var orders = await _unitOfWork.PurchaseOrders.FindAsync(po => po.Status == frenchStatus);
        return orders.Count();
    }

    public async Task<List<MonthlyPurchaseData>> GetMonthlyPurchaseAmountAsync(int monthCount)
    {
        var result = new List<MonthlyPurchaseData>();
        var now = DateTime.Now;
        
        for (int i = monthCount - 1; i >= 0; i--)
        {
            var monthDate = now.AddMonths(-i);
            var startDate = new DateTime(monthDate.Year, monthDate.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            var orders = await _unitOfWork.PurchaseOrders.FindAsync(po =>
                po.CreatedAt >= startDate && po.CreatedAt <= endDate);
            
            var totalAmount = orders.Sum(po => po.TotalAmountTTC);
            var avgAmount = orders.Any() ? totalAmount / orders.Count() : 0;
            
            result.Add(new MonthlyPurchaseData
            {
                Month = monthDate.ToString("MMMM yyyy"),
                Amount = totalAmount,
                Total = totalAmount,
                Average = avgAmount
            });
        }
        
        return result;
    }

    public async Task<List<RecentPurchaseOrderData>> GetRecentPurchaseOrdersAsync(int count)
    {
        // Get recent orders including suppliers
        var orders = await _unitOfWork.PurchaseOrders.GetAllIncludingAsync(po => po.Supplier);
        var recentOrders = orders
            .OrderByDescending(po => po.CreatedAt)
            .Take(count)
            .Select(po => new RecentPurchaseOrderData
            {
                Reference = po.OrderNumber,
                Type = "BC",
                SupplierName = po.Supplier?.CompanyName ?? "N/A",
                CreatedDate = po.CreatedAt,
                TotalAmount = po.TotalAmountTTC,
                Status = po.Status
            })
            .ToList();
            
        return recentOrders;
    }
}
