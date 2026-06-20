using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<PagedResult<InvoiceDto>> GetInvoicesPagedAsync(int pageNumber, int pageSize, string? searchText, int? supplierId, string? status, DateTime? date, CancellationToken cancellationToken);
    Task<Invoice?> GetByIdWithDetailsAsync(int id);
}
