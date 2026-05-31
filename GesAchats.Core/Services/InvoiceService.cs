using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Services;

public class InvoiceService : IInvoiceService
{
    private readonly IUnitOfWork _unitOfWork;

    public InvoiceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<decimal> GetTotalInvoiceAmountAsync(DateTime startDate, DateTime endDate)
    {
        var invoices = await _unitOfWork.Invoices.FindAsync(i =>
            i.CreatedAt >= startDate && i.CreatedAt <= endDate);
        return invoices.Sum(i => i.AmountTTC);
    }

    public async Task<int> GetInvoiceCountAsync(DateTime startDate, DateTime endDate)
    {
        var invoices = await _unitOfWork.Invoices.FindAsync(i =>
            i.CreatedAt >= startDate && i.CreatedAt <= endDate);
        return invoices.Count();
    }
}
