using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class PurchaseOrderRepository : Repository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<PurchaseOrder?> GetWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(o => o.Supplier)
            .Include(o => o.Quotation)
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetAllWithSuppliersAsync()
    {
        return await _dbSet
            .Include(o => o.Supplier)
            .Include(o => o.Quotation)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }
}
