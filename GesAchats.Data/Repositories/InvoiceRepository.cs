using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.Data.Repositories;

public class InvoiceRepository : Repository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(GesAchatsDbContext context) : base(context)
    {
    }

    public async Task<Invoice?> GetByIdWithDetailsAsync(int id)
    {
        return await _dbSet
            .Include(i => i.Supplier)
            .Include(i => i.Details)
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<PagedResult<InvoiceDto>> GetInvoicesPagedAsync(int pageNumber, int pageSize, string? searchText, int? supplierId, string? status, DateTime? date, CancellationToken cancellationToken)
    {
        var query = _dbSet.AsNoTracking();

        // Appliquer les filtres avant la pagination
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            query = query.Where(i =>
                (i.InvoiceNumber != null && i.InvoiceNumber.Contains(searchText)) ||
                (i.ExternalInvoiceNumber != null && i.ExternalInvoiceNumber.Contains(searchText)) ||
                (i.Supplier != null && i.Supplier.CompanyName.Contains(searchText)) ||
                (i.PurchaseOrder != null && i.PurchaseOrder.OrderNumber.Contains(searchText)) ||
                (i.DeliveryNote != null && i.DeliveryNote.DeliveryNumber.Contains(searchText)));
        }

        if (supplierId.HasValue && supplierId.Value != 0)
        {
            query = query.Where(i => i.SupplierId == supplierId.Value);
        }

        if (date.HasValue)
        {
            query = query.Where(i => i.InvoiceDate.Date == date.Value.Date);
        }

        // Statut filter: load matching invoice IDs first
        if (!string.IsNullOrWhiteSpace(status) && status != "Tous")
        {
            List<int> matchingInvoiceIds = new List<int>();

            // First apply other filters to invoice query
            var filteredInvoicesQuery = _dbSet.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filteredInvoicesQuery = filteredInvoicesQuery.Where(i =>
                    (i.InvoiceNumber != null && i.InvoiceNumber.Contains(searchText)) ||
                    (i.ExternalInvoiceNumber != null && i.ExternalInvoiceNumber.Contains(searchText)) ||
                    (i.Supplier != null && i.Supplier.CompanyName.Contains(searchText)) ||
                    (i.PurchaseOrder != null && i.PurchaseOrder.OrderNumber.Contains(searchText)) ||
                    (i.DeliveryNote != null && i.DeliveryNote.DeliveryNumber.Contains(searchText)));
            }

            if (supplierId.HasValue && supplierId.Value != 0)
            {
                filteredInvoicesQuery = filteredInvoicesQuery.Where(i => i.SupplierId == supplierId.Value);
            }

            if (date.HasValue)
            {
                filteredInvoicesQuery = filteredInvoicesQuery.Where(i => i.InvoiceDate.Date == date.Value.Date);
            }

            // Get filtered invoices with their total payments
            var invoicePaymentTotals = await (from i in filteredInvoicesQuery
                                              join p in _context.Payments.AsNoTracking() on i.Id equals p.InvoiceId into pg
                                              from p in pg.DefaultIfEmpty()
                                              select new { InvoiceId = i.Id, AmountTTC = i.AmountTTC, PaymentAmount = (decimal?)p.AmountPaid })
                                              .ToListAsync(cancellationToken);

            // Calculate total paid per invoice
            var invoiceTotals = invoicePaymentTotals
                .GroupBy(x => x.InvoiceId)
                .Select(g => new
                {
                    InvoiceId = g.Key,
                    AmountTTC = g.First().AmountTTC,
                    TotalPaid = g.Sum(x => x.PaymentAmount ?? 0m)
                });

            // Filter based on status
            if (status == "Payée")
            {
                matchingInvoiceIds = invoiceTotals.Where(x => x.TotalPaid >= x.AmountTTC).Select(x => x.InvoiceId).ToList();
            }
            else if (status == "Partiellement payée")
            {
                matchingInvoiceIds = invoiceTotals.Where(x => x.TotalPaid > 0m && x.TotalPaid < x.AmountTTC).Select(x => x.InvoiceId).ToList();
            }
            else if (status == "En attente")
            {
                matchingInvoiceIds = invoiceTotals.Where(x => x.TotalPaid <= 0m).Select(x => x.InvoiceId).ToList();
            }

            // Only include matching invoices
            query = query.Where(i => matchingInvoiceIds.Contains(i.Id));
        }

        // Compter le total avant pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Get payments for all matching invoices first
        var invoiceIds = await query.Select(i => i.Id).ToListAsync(cancellationToken);
        var payments = await _context.Payments
            .AsNoTracking()
            .Where(p => invoiceIds.Contains(p.InvoiceId))
            .Select(p => new PaymentListDto
            {
                Id = p.Id,
                PaymentDate = p.PaymentDate,
                SupplierId = p.SupplierId,
                SupplierCompanyName = p.Supplier != null ? p.Supplier.CompanyName : "Inconnu",
                InvoiceId = p.InvoiceId,
                InvoiceNumber = p.Invoice != null ? p.Invoice.InvoiceNumber : null,
                AmountPaid = p.AmountPaid,
                PaymentMethod = p.PaymentMethod,
                ReferenceNumber = p.ReferenceNumber,
                ProofFilePath = p.ProofFilePath
            })
            .ToListAsync(cancellationToken);

        // Pagination and projection towards DTO
        var items = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(i => new InvoiceDto
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                ExternalInvoiceNumber = i.ExternalInvoiceNumber,
                InvoiceDate = i.InvoiceDate,
                SupplierId = i.SupplierId,
                SupplierName = i.Supplier != null ? i.Supplier.CompanyName : "Inconnu",
                PurchaseOrderId = i.PurchaseOrderId,
                PurchaseOrderNumber = i.PurchaseOrder != null ? i.PurchaseOrder.OrderNumber : null,
                DeliveryNoteId = i.DeliveryNoteId,
                DeliveryNoteNumber = i.DeliveryNote != null ? i.DeliveryNote.DeliveryNumber : null,
                AmountHT = i.AmountHT,
                TaxAmount = i.TaxAmount,
                AmountTTC = i.AmountTTC,
                Status = i.Status,
                FilePath = i.FilePath,
                Payments = new List<PaymentListDto>()
            })
            .ToListAsync(cancellationToken);

        // Attach payments to each invoice
        foreach (var invoice in items)
        {
            invoice.Payments = payments.Where(p => p.InvoiceId == invoice.Id).ToList();
        }

        return new PagedResult<InvoiceDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
