using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;

namespace GesAchats.Core.Interfaces;

/// <summary>
/// Interface Unit of Work pour coordonner les repositories et les transactions
/// </summary>
public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IRepository<Role> Roles { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<Product> Products { get; }
    IRepository<Magasin> Magasins { get; }
    IQuotationRepository Quotations { get; }
    IRepository<QuotationDetail> QuotationDetails { get; }
    IPurchaseOrderRepository PurchaseOrders { get; }
    IPurchaseOrderDetailRepository PurchaseOrderDetails { get; }
    IDeliveryNoteRepository DeliveryNotes { get; }
    IRepository<DeliveryNoteDetail> DeliveryNoteDetails { get; }
    IRepository<Invoice> Invoices { get; }
    IRepository<InvoiceDetail> InvoiceDetails { get; }
    IRepository<Payment> Payments { get; }
    INeedRepository Needs { get; }
    IRepository<NeedDetail> NeedDetails { get; }
    IRepository<AuditLog> AuditLogs { get; }
    IStockExitRepository StockExits { get; }

    Task<int> CompleteAsync();
}
