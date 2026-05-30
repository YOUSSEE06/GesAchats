using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IStockExitRepository : IRepository<StockExit>
{
    Task<IEnumerable<StockExit>> GetAllWithDetailsAsync();
    Task<IEnumerable<StockExit>> GetByProductIdAsync(int productId);
}
