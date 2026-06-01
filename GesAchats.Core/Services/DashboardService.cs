using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GesAchats.Core.DTOs;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Entities;

namespace GesAchats.Core.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<DashboardOperationDto>> GetRecentOperationsAsync(int count = 6)
    {
        var operations = new List<DashboardOperationDto>();

        // Load Quotations (Devis)
        var quotations = await _unitOfWork.Quotations.GetAllWithSuppliersAsync();
        operations.AddRange(quotations.Select(q => new DashboardOperationDto
        {
            Reference = q.ReferenceNumber,
            Type = "Devis",
            Fournisseur = q.Supplier?.CompanyName ?? "Inconnu",
            Date = q.Date,
            Statut = q.Status
        }));

        // Load Purchase Orders (Bons de Commande)
        var purchaseOrders = await _unitOfWork.PurchaseOrders.GetAllWithSuppliersAsync();
        operations.AddRange(purchaseOrders.Select(po => new DashboardOperationDto
        {
            Reference = po.OrderNumber,
            Type = "Bon de Commande",
            Fournisseur = po.Supplier?.CompanyName ?? "Inconnu",
            Date = po.OrderDate,
            Statut = po.Status
        }));

        // Sort by date descending and take top count
        return operations
            .OrderByDescending(op => op.Date)
            .Take(count)
            .ToList();
    }

    public async Task<DashboardStatsDto> GetMagasinierDashboardStatsAsync(int days = 30)
    {
        var stats = new DashboardStatsDto();
        var startDate = DateTime.UtcNow.AddDays(-days);

        // 1. Statistiques Articles
        var allProducts = await _unitOfWork.Products.GetAllAsync();
        stats.TotalArticles = allProducts.Count();
        stats.StockEnRuptureCount = allProducts.Count(p => p.CurrentStock <= 0);
        stats.StockSousMinimumCount = allProducts.Count(p => p.CurrentStock > 0 && p.CurrentStock < p.MinimumStock);
        stats.StockNormalCount = stats.TotalArticles - stats.StockEnRuptureCount - stats.StockSousMinimumCount;

        // 2. Statistiques BL
        var allBls = await _unitOfWork.DeliveryNotes.GetAllAsync();
        stats.BlEnAttenteCount = allBls.Count(b => b.Status == "En attente" || b.Status == "EnAttente");
        stats.BlValidesCount = allBls.Count(b => b.Status == "Validé" || b.Status == "Valide");

        // 3. Statistiques Besoins
        var allNeeds = await _unitOfWork.Needs.GetAllAsync();
        stats.BesoinsEnCoursCount = allNeeds.Count(n => n.Status == NeedStatus.Draft || n.Status == NeedStatus.ToValidate);
        stats.BesoinsTransmisCount = allNeeds.Count(n => n.Status == NeedStatus.TransmittedToPurchasing);

        // 4. Statistiques BC
        var allBcs = await _unitOfWork.PurchaseOrders.GetAllAsync();
        stats.BcEnAttenteCount = allBcs.Count(b => b.Status == PurchaseOrderStatus.Pending);
        stats.BcValidesCount = allBcs.Count(b => b.Status == PurchaseOrderStatus.Validated);

        // 5. Articles Critiques (Top 5)
        stats.CriticalProducts = allProducts
            .Where(p => p.CurrentStock < p.MinimumStock)
            .OrderBy(p => p.CurrentStock / (p.MinimumStock > 0 ? p.MinimumStock : 1))
            .Take(5)
            .Select(p => new CriticalProductDto
            {
                Name = p.Designation,
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                Unit = p.Unit,
                Status = p.CurrentStock <= 0 ? "En rupture" : "Sous minimum"
            })
            .ToList();

        // 6. Derniers BL (Top 5)
        var recentBlData = await _unitOfWork.DeliveryNotes.GetAllIncludingAsync(n => n.Supplier, n => n.PurchaseOrder);
        stats.RecentBls = recentBlData
            .OrderByDescending(b => b.ReceptionDate)
            .Take(5)
            .Select(b => new RecentBlDto
            {
                Date = b.ReceptionDate,
                Number = b.DeliveryNumber,
                Supplier = b.Supplier?.CompanyName ?? "Inconnu",
                RelatedBc = b.PurchaseOrder?.OrderNumber ?? "-",
                Status = b.Status == "Valide" || b.Status == "Validé" ? "Validé" : "En attente"
            })
            .ToList();

        // 7. Derniers Besoins (Top 5)
        var recentNeedsData = await _unitOfWork.Needs.GetAllIncludingAsync(n => n.RequestedBy, n => n.Details);
        stats.RecentNeeds = recentNeedsData
            .OrderByDescending(n => n.RequestedAt)
            .Take(5)
            .Select(n => new RecentNeedDto
            {
                Number = n.NumeroBesoin,
                Date = n.RequestedAt,
                Requester = n.RequestedBy?.FullName ?? "Inconnu",
                ArticleCount = n.Details?.Count ?? 0,
                Status = n.Status.ToString()
            })
            .ToList();

        // 8. Derniers BC (Top 5 - Filtrer "Annulé")
        var recentBcData = await _unitOfWork.PurchaseOrders.GetAllIncludingAsync(b => b.Supplier);
        stats.RecentBcs = recentBcData
            .Where(b => b.Status != PurchaseOrderStatus.Cancelled)
            .OrderByDescending(b => b.OrderDate)
            .Take(5)
            .Select(b => new RecentBcDto
            {
                Date = b.OrderDate,
                Number = b.OrderNumber,
                Supplier = b.Supplier?.CompanyName ?? "Inconnu",
                Status = b.Status,
                TotalTtc = b.TotalAmountTTC
            })
            .ToList();

        // 9. Mouvements de stock (7 derniers jours)
        var stockExits = await _unitOfWork.StockExits.GetAllAsync();
        
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Today.AddDays(-i);
            var nextDate = date.AddDays(1);

            var dailyOut = stockExits
                .Where(s => s.ExitDate >= date && s.ExitDate < nextDate)
                .Sum(s => s.Quantity);

            var dailyIn = allBls
                .Where(b => (b.Status == "Valide" || b.Status == "Validé") && b.ReceptionDate >= date && b.ReceptionDate < nextDate)
                .Sum(b => b.ReceivedQuantity);

            stats.StockMovements.Add(new StockMovementDto
            {
                Date = date,
                In = dailyIn,
                Out = dailyOut
            });
        }

        return stats;
    }
}
