using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IPurchaseOrderDetailRepository : IRepository<PurchaseOrderDetail>
{
    Task<IEnumerable<PurchaseOrderDetail>> GetHistoryByProductAsync(int productId);
}
