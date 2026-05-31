using GesAchats.Core.Entities;

namespace GesAchats.Core.Interfaces;

public interface IPdfGeneratorService
{
    Task<string> GeneratePurchaseOrderPdfAsync(PurchaseOrder order);
    Task<string> GenerateQuotationRequestPdfAsync(Quotation quotation);
    Task<string> GenerateNeedPdfAsync(Need need);
    Task<string> GenerateDeliveryNotePdfAsync(DeliveryNote deliveryNote);
    Task<string> GeneratePaymentReceiptPdfAsync(Payment reglement);
    Task<string> GenerateNeedsListPdfAsync(IEnumerable<Need> needs);
}
