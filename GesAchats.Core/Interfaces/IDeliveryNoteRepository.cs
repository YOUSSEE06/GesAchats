using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IDeliveryNoteRepository : IRepository<DeliveryNote>
{
    Task<IEnumerable<DeliveryNote>> GetAllWithDetailsAsync();
    Task<PagedResult<DeliveryNoteHistoryDto>> GetBonsLivraisonPagedAsync(int pageNumber, int pageSize, string? searchText, string? selectedSupplier, string? selectedStatus, DateTime? filterReceptionDate, CancellationToken cancellationToken);
}
