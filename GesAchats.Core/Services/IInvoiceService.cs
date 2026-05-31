namespace GesAchats.Core.Services;

public interface IInvoiceService
{
    Task<decimal> GetTotalInvoiceAmountAsync(DateTime startDate, DateTime endDate);
    Task<int> GetInvoiceCountAsync(DateTime startDate, DateTime endDate);
}
