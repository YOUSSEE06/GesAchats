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
        var now = DateTime.Now;
        var startDate = now.AddMonths(-(monthCount - 1));
        var firstDayOfStartMonth = new DateTime(startDate.Year, startDate.Month, 1);
        
        // Get all validated orders
        var allValidatedOrders = await _unitOfWork.PurchaseOrders.FindAsync(po => 
            po.Status == PurchaseOrderStatus.Validated);
        
        // Create a list of months in order (oldest to newest)
        var monthlyData = new List<MonthlyPurchaseData>();
        for (int i = 0; i < monthCount; i++)
        {
            var currentMonth = firstDayOfStartMonth.AddMonths(i);
            var monthStart = new DateTime(currentMonth.Year, currentMonth.Month, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);
            
            // Get orders in this month
            var ordersInMonth = allValidatedOrders.Where(po => 
                po.OrderDate >= monthStart && po.OrderDate <= monthEnd).ToList();
            
            var totalAmount = ordersInMonth.Sum(po => po.TotalAmountTTC);
            var avgAmount = ordersInMonth.Any() ? totalAmount / ordersInMonth.Count : 0;
            
            monthlyData.Add(new MonthlyPurchaseData
            {
                Month = currentMonth.ToString("MMMM yyyy"),
                Amount = totalAmount,
                Total = totalAmount,
                Average = avgAmount
            });
        }
        
        return monthlyData;
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

    public async Task<(int Pending, int Validated, int Cancelled)> GetPurchaseOrderStatusCountsAsync()
    {
        var allOrders = await _unitOfWork.PurchaseOrders.GetAllAsync();

        int pending = allOrders.Count(po => po.Status == PurchaseOrderStatus.Pending);
        int validated = allOrders.Count(po => po.Status == PurchaseOrderStatus.Validated);
        int cancelled = allOrders.Count(po => po.Status == PurchaseOrderStatus.Cancelled);

        return (pending, validated, cancelled);
    }
}
