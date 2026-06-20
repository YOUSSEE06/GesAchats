using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class PurchaseOrderRepository : Repository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<PurchaseOrder?> GetWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(o => o.Supplier)
            .Include(o => o.Quotation)
            .Include(o => o.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<PurchaseOrder>> GetAllWithSuppliersAsync()
    {
        return await _dbSet
            .Include(o => o.Supplier)
            .Include(o => o.Quotation)
            .OrderByDescending(o => o.OrderDate)
            .ToListAsync();
    }

    public async Task<PagedResult<PurchaseOrderDto>> GetPurchaseOrdersPagedAsync(int pageNumber, int pageSize, string? searchText, string? supplier, string? status, DateTime? date, bool excludeCancelled, CancellationToken cancellationToken)
    {
        var query = _dbSet.AsNoTracking();

        // Appliquer les filtres avant la pagination
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(p => p.OrderNumber.Contains(searchText) || (p.Quotation != null && p.Quotation.ReferenceNumber.Contains(searchText)));
        }

        if (!string.IsNullOrWhiteSpace(supplier))
        {
            query = query.Where(p => p.Supplier != null && p.Supplier.CompanyName == supplier);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(p => p.Status == status);
        }

        if (date.HasValue)
        {
            query = query.Where(p => p.OrderDate.Date == date.Value.Date);
        }

        if (excludeCancelled)
        {
            query = query.Where(p => p.Status != PurchaseOrderStatus.Cancelled);
        }

        // Compter le total avant pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Pagination et projection vers DTO
        var items = await query
            .OrderByDescending(p => p.OrderDate)
            .ThenByDescending(p => p.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PurchaseOrderDto
            {
                Id = p.Id,
                OrderDate = p.OrderDate,
                OrderNumber = p.OrderNumber,
                SupplierName = p.Supplier != null ? p.Supplier.CompanyName : "Inconnu",
                QuotationRef = p.Quotation != null ? p.Quotation.ReferenceNumber : "N/A",
                TotalTTC = p.TotalAmountTTC,
                Status = p.Status
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<PurchaseOrderDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
