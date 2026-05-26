using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public class ComparativeAnalysisService : IComparativeAnalysisService
{
    private readonly IUnitOfWork _unitOfWork;

    public ComparativeAnalysisService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<Quotation>> CompareQuotationsForNeedAsync(int needId)
    {
        return await _unitOfWork.Quotations.GetByNeedWithSuppliersAsync(needId);
    }

    public decimal CalculateSupplierScore(Quotation quotation)
    {
        // Algorithme simplifié de scoring (0 à 10)
        // 50% Prix, 30% Délai, 20% Historique/Note fournisseur
        
        decimal score = 0;

        // 1. Prix (plus c'est bas, mieux c'est)
        // Pour simplifier, on compare au montant TTC
        if (quotation.TotalAmountTTC < 1000) score += 5;
        else if (quotation.TotalAmountTTC < 5000) score += 3;
        else score += 1;

        // 2. Délai
        int avgDelay = quotation.Details.Any() ? (int)quotation.Details.Average(d => d.DeliveryDelayDays ?? 7) : 7;
        if (avgDelay <= 3) score += 3;
        else if (avgDelay <= 7) score += 2;
        else score += 1;

        // 3. Note fournisseur
        decimal rating = quotation.Supplier?.Rating ?? 3;
        score += (rating / 5) * 2;

        return Math.Min(score, 10);
    }

    public Quotation? RecommendBestQuotation(IEnumerable<Quotation> quotations)
    {
        return quotations.OrderByDescending(q => CalculateSupplierScore(q)).FirstOrDefault();
    }
}
