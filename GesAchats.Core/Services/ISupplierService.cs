namespace GesAchats.Core.Services;

public interface ISupplierService
{
    Task<int> GetActiveSupplierCountAsync();
    Task<List<SupplierExpenseData>> GetTopSuppliersByExpenseAsync(int top, DateTime startDate, DateTime endDate);
}
