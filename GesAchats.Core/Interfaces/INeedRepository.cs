using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface INeedRepository : IRepository<Need>
{
    Task<IEnumerable<Need>?> GetPendingNeedsWithProductsAsync();
    Task<IEnumerable<Need>> GetAllWithDetailsAsync();
    Task<Need?> GetByIdWithDetailsAsync(int id);
    Task<PagedResult<NeedHistoriqueDto>> GetHistoriqueBesoinsPagedAsync(int pageNumber, int pageSize, string? numeroBesoin, DateTime? date, string? statut, CancellationToken cancellationToken);
}
