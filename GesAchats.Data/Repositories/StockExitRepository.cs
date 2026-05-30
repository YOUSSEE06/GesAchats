using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class StockExitRepository : Repository<StockExit>, IStockExitRepository
{
    public StockExitRepository(GesAchatsDbContext context) : base(context)
    {
    }

    private GesAchatsDbContext GesAchatsDbContext => (GesAchatsDbContext)_context;

    public async Task<IEnumerable<StockExit>> GetAllWithDetailsAsync()
    {
        return await GesAchatsDbContext.StockExits
            .Include(s => s.Product)
            .Include(s => s.CreatedBy)
            .OrderByDescending(s => s.ExitDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockExit>> GetByProductIdAsync(int productId)
    {
        return await GesAchatsDbContext.StockExits
            .Include(s => s.Product)
            .Include(s => s.CreatedBy)
            .Where(s => s.ProductId == productId)
            .OrderByDescending(s => s.ExitDate)
            .ToListAsync();
    }
}
