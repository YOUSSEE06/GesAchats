using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Core.DTOs;
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

    public async Task<PagedResult<StockExitHistoryDto>> GetStockExitsPagedAsync(int pageNumber, int pageSize, string? searchText, DateTime? filterDate, CancellationToken cancellationToken)
    {
        var query = GesAchatsDbContext.StockExits.AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(s => s.Product != null && s.Product.Designation.Contains(searchText));
        }

        if (filterDate.HasValue)
        {
            query = query.Where(s => s.ExitDate.Date == filterDate.Value.Date);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Get paginated items
        var items = await query
            .OrderByDescending(s => s.ExitDate)
            .ThenByDescending(s => s.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new StockExitHistoryDto
            {
                Id = s.Id,
                ExitDate = s.ExitDate,
                ProductDesignation = s.Product != null ? s.Product.Designation : "",
                Quantity = s.Quantity,
                Reason = s.Reason,
                StockAfterExit = s.StockAfterExit
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<StockExitHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
