using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class DeliveryNoteRepository : Repository<DeliveryNote>, IDeliveryNoteRepository
{
    public DeliveryNoteRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<DeliveryNote>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(n => n.PurchaseOrder)
            .Include(n => n.Supplier)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .OrderByDescending(n => n.ReceptionDate)
            .ToListAsync();
    }

    public async Task<PagedResult<DeliveryNoteHistoryDto>> GetBonsLivraisonPagedAsync(int pageNumber, int pageSize, string? searchText, string? selectedSupplier, string? selectedStatus, DateTime? filterReceptionDate, CancellationToken cancellationToken)
    {
        var query = _dbSet.AsNoTracking();

        // Apply search filter first
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(n => 
                n.DeliveryNumber.Contains(searchText) || 
                (n.PurchaseOrder != null && n.PurchaseOrder.OrderNumber.Contains(searchText)));
        }

        // Apply supplier filter
        if (!string.IsNullOrWhiteSpace(selectedSupplier) && selectedSupplier != "Tous")
        {
            query = query.Where(n => n.Supplier != null && n.Supplier.CompanyName == selectedSupplier);
        }

        // Apply status filter
        if (!string.IsNullOrWhiteSpace(selectedStatus) && selectedStatus != "Tous")
        {
            if (selectedStatus == "En attente")
                query = query.Where(n => n.Status == "EnAttente");
            else if (selectedStatus == "Validé")
                query = query.Where(n => n.Status == "Valide");
        }

        // Apply date filter
        if (filterReceptionDate.HasValue)
        {
            var date = filterReceptionDate.Value.Date;
            query = query.Where(n => n.ReceptionDate.Date == date);
        }

        // Count total items
        var totalCount = await query.CountAsync(cancellationToken);

        // Paginate and project to DTO
        var items = await query
            .OrderByDescending(n => n.ReceptionDate)
            .ThenByDescending(n => n.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new DeliveryNoteHistoryDto
            {
                Id = n.Id,
                ReceptionDate = n.ReceptionDate,
                DeliveryNumber = n.DeliveryNumber,
                SupplierCompanyName = n.Supplier != null ? n.Supplier.CompanyName : "Inconnu",
                PurchaseOrderNumber = n.PurchaseOrder != null ? n.PurchaseOrder.OrderNumber : "Aucun",
                Status = n.Status
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<DeliveryNoteHistoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
