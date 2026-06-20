using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetWithDetailsAsync(int id);
    Task<IEnumerable<PurchaseOrder>> GetAllWithSuppliersAsync();
    Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersPagedAsync(int pageNumber, int pageSize, string? searchText, string? supplier, string? status, DateTime? date, bool excludeCancelled, CancellationToken cancellationToken);
}
