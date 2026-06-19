using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class NeedRepository : Repository<Need>, INeedRepository
{
    public NeedRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Need>?> GetPendingNeedsWithProductsAsync()
    {
        return await _dbSet
            .Include(n => n.RequestedBy)
            .Include(n => n.Product)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .Where(n => n.Status == NeedStatus.TransmittedToPurchasing || n.Status == NeedStatus.InPurchase)
            .OrderByDescending(n => n.DateTransmission ?? n.RequestedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Need>> GetAllWithDetailsAsync()
    {
        return await _dbSet
            .Include(n => n.Product)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .Include(n => n.RequestedBy)
            .ToListAsync();
    }

    public async Task<Need?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(n => n.RequestedBy)
            .Include(n => n.Details)
                .ThenInclude(d => d.Product)
            .Include(n => n.PurchaseOrders) // Ajouté pour inclure les PurchaseOrders liés
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<PagedResult<NeedHistoriqueDto>> GetHistoriqueBesoinsPagedAsync(int pageNumber, int pageSize, string? numeroBesoin, DateTime? date, string? statut, CancellationToken cancellationToken)
    {
        var query = _dbSet.AsNoTracking();

        // Appliquer les filtres avant la pagination
        if (!string.IsNullOrWhiteSpace(numeroBesoin))
        {
            query = query.Where(n => n.NumeroBesoin.Contains(numeroBesoin));
        }

        if (date.HasValue)
        {
            query = query.Where(n => n.RequestedAt.Date == date.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(statut) && statut != "Tous")
        {
            if (statut == "Transmis")
            {
                query = query.Where(n => n.Status == NeedStatus.TransmittedToPurchasing);
            }
            else if (statut == "En cours")
            {
                query = query.Where(n => n.Status != NeedStatus.TransmittedToPurchasing);
            }
        }

        // Compter le total avant pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Pagination et projection vers DTO
        var items = await query
            .OrderByDescending(n => n.RequestedAt)
            .ThenByDescending(n => n.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NeedHistoriqueDto
            {
                Id = n.Id,
                NumeroBesoin = n.NumeroBesoin,
                RequestedAt = n.RequestedAt,
                Demandeur = n.RequestedBy != null ? n.RequestedBy.FullName : "Inconnu",
                NombreArticles = n.Details.Count,
                Statut = n.Status == NeedStatus.TransmittedToPurchasing ? "Transmis" : "En cours"
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<NeedHistoriqueDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
