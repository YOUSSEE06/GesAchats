using GesAchats.Core.Entities;
using GesAchats.Core.DTOs;

namespace GesAchats.Core.Interfaces;

public interface IStockExitRepository : IRepository<StockExit>
{
    Task<IEnumerable<StockExit>> GetAllWithDetailsAsync();
    Task<IEnumerable<StockExit>> GetByProductIdAsync(int productId);
    Task<PagedResult<StockExitHistoryDto>> GetStockExitsPagedAsync(int pageNumber, int pageSize, string? searchText, DateTime? filterDate, CancellationToken cancellationToken);
}
