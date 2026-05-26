using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Repositories;
using GesAchats.Data.Context;

namespace GesAchats.Data;

/// <summary>
/// Implémentation du Unit of Work
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly GesAchatsDbContext _context;

    public UnitOfWork(GesAchatsDbContext context)
    {
        _context = context;
        Users = new UserRepository(_context);
        Roles = new Repository<Role>(_context);
        Suppliers = new Repository<Supplier>(_context);
        Products = new Repository<Product>(_context);
        Quotations = new QuotationRepository(_context);
        QuotationDetails = new Repository<QuotationDetail>(_context);
        PurchaseOrders = new PurchaseOrderRepository(_context);
        PurchaseOrderDetails = new PurchaseOrderDetailRepository(_context);
        DeliveryNotes = new DeliveryNoteRepository(_context);
        DeliveryNoteDetails = new Repository<DeliveryNoteDetail>(_context);
        Invoices = new Repository<Invoice>(_context);
        InvoiceDetails = new Repository<InvoiceDetail>(_context);
        Payments = new Repository<Payment>(_context);
        Needs = new NeedRepository(_context);
        NeedDetails = new Repository<NeedDetail>(_context);
        AuditLogs = new Repository<AuditLog>(_context);
    }

    public IUserRepository Users { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<Role> Roles { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<Supplier> Suppliers { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<Product> Products { get; private set; }
    public IQuotationRepository Quotations { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<QuotationDetail> QuotationDetails { get; private set; }
    public IPurchaseOrderRepository PurchaseOrders { get; private set; }
    public IPurchaseOrderDetailRepository PurchaseOrderDetails { get; private set; }
    public IDeliveryNoteRepository DeliveryNotes { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<DeliveryNoteDetail> DeliveryNoteDetails { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<Invoice> Invoices { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<InvoiceDetail> InvoiceDetails { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<Payment> Payments { get; private set; }
    public INeedRepository Needs { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<NeedDetail> NeedDetails { get; private set; }
    public GesAchats.Core.Interfaces.IRepository<AuditLog> AuditLogs { get; private set; }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
