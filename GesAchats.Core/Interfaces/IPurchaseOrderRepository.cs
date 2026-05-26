using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetWithDetailsAsync(int id);
    Task<IEnumerable<PurchaseOrder>> GetAllWithSuppliersAsync();
}
