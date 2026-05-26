using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Interfaces;

/// <summary>
/// Interface Unit of Work pour coordonner les repositories et les transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRepository<Entities.Role> Roles { get; }
    IRepository<Entities.Supplier> Suppliers { get; }
    IRepository<Entities.Product> Products { get; }
    IQuotationRepository Quotations { get; }
    IRepository<Entities.QuotationDetail> QuotationDetails { get; }
    IPurchaseOrderRepository PurchaseOrders { get; }
    IPurchaseOrderDetailRepository PurchaseOrderDetails { get; }
    IDeliveryNoteRepository DeliveryNotes { get; }
    IRepository<Entities.DeliveryNoteDetail> DeliveryNoteDetails { get; }
    IRepository<Entities.Invoice> Invoices { get; }
    IRepository<Entities.InvoiceDetail> InvoiceDetails { get; }
    IRepository<Entities.Payment> Payments { get; }
    INeedRepository Needs { get; }
    IRepository<Entities.NeedDetail> NeedDetails { get; }
    IRepository<Entities.AuditLog> AuditLogs { get; }

    Task<int> CompleteAsync();
}
