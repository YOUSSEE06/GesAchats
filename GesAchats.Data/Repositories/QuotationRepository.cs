using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class QuotationRepository : Repository<Quotation>, IQuotationRepository
{
    public QuotationRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Quotation>> GetByNeedWithSuppliersAsync(int needId)
    {
        return await _dbSet
            .Include(q => q.Supplier)
            .Include(q => q.Details)
                .ThenInclude(d => d.Product)
            .Where(q => q.NeedId == needId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Quotation>> GetBySupplierWithDetailsAsync(int supplierId)
    {
        return await _dbSet
            .Include(q => q.Supplier)
            .Include(q => q.Details)
                .ThenInclude(d => d.Product)
            .Where(q => q.SupplierId == supplierId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Quotation>> GetAllWithSuppliersAsync()
    {
        return await _dbSet
            .Include(q => q.Supplier)
            .OrderByDescending(q => q.Date)
            .ToListAsync();
    }

    public async Task<Quotation?> GetWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(q => q.Supplier)
            .Include(q => q.Need)
            .Include(q => q.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(q => q.Id == id);
    }
}
