using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IQuotationRepository : IRepository<Quotation>
{
    Task<IEnumerable<Quotation>> GetByNeedWithSuppliersAsync(int needId);
    Task<IEnumerable<Quotation>> GetBySupplierWithDetailsAsync(int supplierId);
    Task<IEnumerable<Quotation>> GetAllWithSuppliersAsync();
    Task<IEnumerable<Quotation>> GetAllWithAllRelatedAsync();
    Task<Quotation?> GetWithDetailsAsync(int id);
}
