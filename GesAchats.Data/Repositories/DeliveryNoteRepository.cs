using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class DeliveryNoteRepository : Repository<DeliveryNote>, IDeliveryNoteRepository
{
    public DeliveryNoteRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DeliveryNote>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(n => n.PurchaseOrder)
            .Include(n => n.Supplier)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .OrderByDescending(n => n.ReceptionDate)
            .ToListAsync();
    }
}
