using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class NeedRepository : Repository<Need>, INeedRepository
{
    public NeedRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Need>?> GetPendingNeedsWithProductsAsync()
    {
        return await _dbSet
            .Include(n => n.RequestedBy)
            .Include(n => n.Product)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .Where(n => n.Status == NeedStatus.TransmittedToPurchasing || n.Status == NeedStatus.InPurchase)
            .OrderByDescending(n => n.DateTransmission ?? n.RequestedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Need>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(n => n.Product)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .Include(n => n.RequestedBy)
            .ToListAsync();
    }

    public async Task<Need?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(n => n.RequestedBy)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .Include(n => n.PurchaseOrders) // Ajouté pour inclure les PurchaseOrders liés
            .FirstOrDefaultAsync(n => n.Id == id);
    }
}
