using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _unitOfWork;

    public SupplierService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<int> GetActiveSupplierCountAsync()
    {
        var suppliers = await _unitOfWork.Suppliers.FindAsync(s => s.IsActive);
        return suppliers.Count();
    }

    public async Task<List<SupplierExpenseData>> GetTopSuppliersByExpenseAsync(int top, DateTime startDate, DateTime endDate)
    {
        var purchaseOrders = await _unitOfWork.PurchaseOrders.GetAllIncludingAsync(po => po.Supplier);
        var filteredOrders = purchaseOrders.Where(po =>
            po.CreatedAt >= startDate && po.CreatedAt <= endDate);
            
        var supplierExpenses = filteredOrders
            .GroupBy(po => po.Supplier)
            .Select(g => new SupplierExpenseData
            {
                Name = g.Key?.CompanyName ?? "N/A",
                TotalExpense = g.Sum(po => po.TotalAmountTTC)
            })
            .OrderByDescending(x => x.TotalExpense)
            .Take(top)
            .ToList();
            
        var totalExpense = supplierExpenses.Sum(x => x.TotalExpense);
        foreach (var item in supplierExpenses)
        {
            item.PercentageOfTotal = totalExpense > 0 
                ? (double)(item.TotalExpense / totalExpense) * 100 
                : 0;
        }
            
        return supplierExpenses;
    }
}
