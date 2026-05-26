using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface INeedRepository : IRepository<Need>
{
    Task<IEnumerable<Need>?> GetPendingNeedsWithProductsAsync();
    Task<IEnumerable<Need>> GetAllWithDetailsAsync();
    Task<Need?> GetByIdWithDetailsAsync(int id);
}
