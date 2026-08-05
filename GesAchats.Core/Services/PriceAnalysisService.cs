using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public class PriceAnalysisService : IPriceAnalysisService
{
    private readonly IUnitOfWork _unitOfWork;

    public PriceAnalysisService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<DeliveryNoteDetail>> GetPriceHistoryForProductAsync(int productId)
    {
        return await _unitOfWork.DeliveryNotes.GetDeliveryHistoryByProductAsync(productId);
    }

    public async Task<Dictionary<int, decimal>> GetAveragePriceBySupplierAsync(int productId)
    {
        var details = await GetPriceHistoryForProductAsync(productId);
        return details
            .GroupBy(d => d.DeliveryNote.SupplierId)
            .ToDictionary(g => g.Key, g => g.Average(d => d.UnitPriceHT));
    }

    public string GetPriceTrend(int productId, int supplierId)
    {
        // Logique de tendance simplifiée
        return "Stable";
    }
}
