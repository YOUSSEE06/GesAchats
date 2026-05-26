using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Core.Services;

public class ConformityService : IConformityService
{
    private readonly IUnitOfWork _unitOfWork;

    public ConformityService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<ConformityResult>> CheckInvoiceConformityAsync(int invoiceId)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
        if (invoice == null) return Enumerable.Empty<ConformityResult>();

        // Chargement des détails de la facture
        var invoiceDetails = await _unitOfWork.InvoiceDetails.FindAsync(d => d.InvoiceId == invoiceId);
        
        // Chargement du BC et BL associés
        var purchaseOrder = invoice.PurchaseOrderId.HasValue 
            ? await _unitOfWork.PurchaseOrders.GetByIdAsync(invoice.PurchaseOrderId.Value) 
            : null;
            
        var deliveryNote = invoice.DeliveryNoteId.HasValue 
            ? await _unitOfWork.DeliveryNotes.GetByIdAsync(invoice.DeliveryNoteId.Value) 
            : null;

        var results = new List<ConformityResult>();

        foreach (var detail in invoiceDetails)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(detail.ProductId);
            var result = new ConformityResult
            {
                ArticleName = product?.Designation ?? "Article inconnu",
                QuantityInvoiced = detail.Quantity,
                PriceInvoiced = detail.UnitPriceHT
            };

            // Comparaison avec le BC
            if (purchaseOrder != null)
            {
                var poDetail = purchaseOrder.Details.FirstOrDefault(d => d.ProductId == detail.ProductId);
                if (poDetail != null)
                {
                    result.QuantityOrdered = poDetail.Quantity;
                    result.PriceOrdered = poDetail.UnitPriceHT;

                    if (detail.UnitPriceHT != poDetail.UnitPriceHT)
                    {
                        result.Status = "PriceMismatch";
                        result.Message = $"Écart de prix : Facturé {detail.UnitPriceHT}€ vs Commandé {poDetail.UnitPriceHT}€";
                    }
                }
                else
                {
                    result.Status = "NotInOrder";
                    result.Message = "Article non présent dans le Bon de Commande";
                }
            }

            // Comparaison avec le BL
            if (deliveryNote != null)
            {
                var dnDetail = deliveryNote.Details.FirstOrDefault(d => d.ProductId == detail.ProductId);
                if (dnDetail != null)
                {
                    result.QuantityReceived = dnDetail.QuantityReceived;
                    if (detail.Quantity != dnDetail.QuantityReceived)
                    {
                        if (result.Status == "OK") result.Status = "QuantityMismatch";
                        result.Message += (string.IsNullOrEmpty(result.Message) ? "" : " | ") + 
                                          $"Écart quantité : Facturé {detail.Quantity} vs Reçu {dnDetail.QuantityReceived}";
                    }
                }
                else
                {
                    if (result.Status == "OK") result.Status = "MissingInDelivery";
                    result.Message += (string.IsNullOrEmpty(result.Message) ? "" : " | ") + 
                                      "Article non présent dans le Bon de Livraison";
                }
            }

            results.Add(result);
        }

        return results;
    }

    public async Task<bool> ValidateInvoiceAsync(int invoiceId, string justification)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
        if (invoice == null) return false;

        invoice.Status = "Verifiee";
        invoice.ConformityStatus = "Conforme"; // Ou "EcartMineur" selon la logique
        invoice.ConformityJustification = justification;
        invoice.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Invoices.Update(invoice);
        await _unitOfWork.CompleteAsync();
        return true;
    }

    public async Task<bool> RejectInvoiceAsync(int invoiceId, string reason)
    {
        var invoice = await _unitOfWork.Invoices.GetByIdAsync(invoiceId);
        if (invoice == null) return false;

        invoice.Status = "Rejetee";
        invoice.ConformityStatus = "NonConforme";
        invoice.Observations = reason;
        invoice.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Invoices.Update(invoice);
        await _unitOfWork.CompleteAsync();
        return true;
    }
}
