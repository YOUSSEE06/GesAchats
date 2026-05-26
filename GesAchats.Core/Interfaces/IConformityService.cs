using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public class ConformityResult
{
    public string ArticleName { get; set; } = string.Empty;
    public decimal QuantityOrdered { get; set; }
    public decimal PriceOrdered { get; set; }
    public decimal QuantityReceived { get; set; }
    public decimal QuantityInvoiced { get; set; }
    public decimal PriceInvoiced { get; set; }
    public string Status { get; set; } = "OK"; // OK, PriceMismatch, QuantityMismatch, MissingInDelivery, NotInOrder
    public string Message { get; set; } = string.Empty;
}

public interface IConformityService
{
    Task<IEnumerable<ConformityResult>> CheckInvoiceConformityAsync(int invoiceId);
    Task<bool> ValidateInvoiceAsync(int invoiceId, string justification);
    Task<bool> RejectInvoiceAsync(int invoiceId, string reason);
}
