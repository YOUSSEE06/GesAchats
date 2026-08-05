using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IPriceAnalysisService
{
    Task<IEnumerable<DeliveryNoteDetail>> GetPriceHistoryForProductAsync(int productId);
    Task<Dictionary<int, decimal>> GetAveragePriceBySupplierAsync(int productId);
    string GetPriceTrend(int productId, int supplierId);
}
