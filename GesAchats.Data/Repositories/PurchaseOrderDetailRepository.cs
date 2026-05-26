using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class PurchaseOrderDetailRepository : Repository<PurchaseOrderDetail>, IPurchaseOrderDetailRepository
{
    public PurchaseOrderDetailRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<PurchaseOrderDetail>> GetHistoryByProductAsync(int productId)
    {
        return await _dbSet
            .Include(d => d.PurchaseOrder)
                .ThenInclude(o => o.Supplier)
            .Where(d => d.ProductId == productId)
            .OrderByDescending(d => d.PurchaseOrder.OrderDate)
            .ToListAsync();
    }
}
