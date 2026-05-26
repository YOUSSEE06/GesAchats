using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IComparativeAnalysisService
{
    Task<IEnumerable<Quotation>> CompareQuotationsForNeedAsync(int needId);
    decimal CalculateSupplierScore(Quotation quotation);
    Quotation? RecommendBestQuotation(IEnumerable<Quotation> quotations);
}
