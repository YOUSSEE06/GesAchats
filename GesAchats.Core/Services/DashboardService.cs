using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GesAchats.Core.DTOs;
using GesAchats.Core.Interfaces;
using GesAchats.Core.Entities;
using Serilog;

namespace GesAchats.Core.Services;

public class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    private string NormalizeQuotationStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return QuotationStatus.Pending;
        
        status = status.Trim();
        
        // Convert old values "Accepted" to "Validated"
        if (status.Equals("Accepted", StringComparison.OrdinalIgnoreCase) || 
            status.Equals("accepted", StringComparison.OrdinalIgnoreCase) || 
            status.Equals("ACCEPTED", StringComparison.OrdinalIgnoreCase))
            return QuotationStatus.Validated;
        
        // Normalize existing values
        if (status.Equals("En attente", StringComparison.OrdinalIgnoreCase) || 
            status.Equals("en_attente", StringComparison.OrdinalIgnoreCase))
            return QuotationStatus.Pending;
        
        if (status.Equals("Validé", StringComparison.OrdinalIgnoreCase) || 
            status.Equals("valide", StringComparison.OrdinalIgnoreCase))
            return QuotationStatus.Validated;
        
        return status;
    }

    public async Task<List<DashboardAlertDto>> GetDashboardAlertsAsync()
    {
        var alerts = new List<DashboardAlertDto>();

        // Load Besoins Magasin (Need) with pending status
        var needs = await _unitOfWork.Needs.FindAsync(n => 
            n.Status == NeedStatus.ToValidate || 
            n.Status == NeedStatus.TransmittedToPurchasing ||
            n.Status == NeedStatus.InPurchase);
        
        alerts.AddRange(needs.Select(n => new DashboardAlertDto
        {
            Type = "Besoin Magasin",
            Reference = n.NumeroBesoin,
            Statut = "En attente",
            Date = n.RequestedAt,
            Title = "Besoin magasin en attente",
            Subtitle = $"{n.NumeroBesoin} • En attente • {n.RequestedAt:dd/MM/yyyy}"
        }));

        // Load Devis (Quotation) with pending status
        var quotations = await _unitOfWork.Quotations.GetAllWithSuppliersAsync();
        alerts.AddRange(quotations
            .Where(q => q.Status == QuotationStatus.Pending)
            .Select(q => new DashboardAlertDto
            {
                Type = "Devis",
                Reference = q.ReferenceNumber,
                Statut = q.Status,
                Date = q.Date,
                Title = "Devis en attente",
                Subtitle = $"{q.ReferenceNumber} • {q.Status} • {q.Date:dd/MM/yyyy}"
            }));

        // Load Bons de Commande (PurchaseOrder) with pending status
        var purchaseOrders = await _unitOfWork.PurchaseOrders.GetAllWithSuppliersAsync();
        alerts.AddRange(purchaseOrders
            .Where(po => po.Status == PurchaseOrderStatus.Pending)
            .Select(po => new DashboardAlertDto
            {
                Type = "Bon de Commande",
                Reference = po.OrderNumber,
                Statut = po.Status,
                Date = po.OrderDate,
                Title = "Bon de commande en attente",
                Subtitle = $"{po.OrderNumber} • {po.Status} • {po.OrderDate:dd/MM/yyyy}"
            }));

        // Sort by date descending
        return alerts.OrderByDescending(a => a.Date).ToList();
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

        // 5. Articles Critiques
        stats.CriticalProducts = allProducts
            .Where(p => p.CurrentStock < p.MinimumStock)
            .OrderBy(p => p.CurrentStock / (p.MinimumStock > 0 ? p.MinimumStock : 1))
            .Select(p => new CriticalProductDto
            {
                Name = p.Designation,
                CurrentStock = p.CurrentStock,
                MinimumStock = p.MinimumStock,
                Unit = p.Unit,
                Status = p.CurrentStock <= 0 ? "En rupture" : "Sous minimum"
            })
            .ToList();

        // 6. Derniers BL (Top 10)
        var recentBlData = await _unitOfWork.DeliveryNotes.GetAllIncludingAsync(n => n.Supplier, n => n.PurchaseOrder, n => n.Details);
        stats.RecentBls = recentBlData
            .OrderByDescending(b => b.ReceptionDate)
            .Take(10)
            .Select(b => new RecentBlDto
            {
                Date = b.ReceptionDate,
                Number = b.DeliveryNumber,
                Supplier = b.Supplier?.CompanyName ?? "Inconnu",
                RelatedBc = b.PurchaseOrder?.OrderNumber ?? "-",
                Status = b.Status == "Valide" || b.Status == "Validé" ? "Validé" : "En attente",
                FirstArticle = b.Details?.FirstOrDefault()?.Product?.Designation ?? "-"
            })
            .ToList();

        // 7. Derniers Besoins (Top 10)
        var recentNeedsData = await _unitOfWork.Needs.GetAllIncludingAsync(n => n.RequestedBy, n => n.Details);
        stats.RecentNeeds = recentNeedsData
            .OrderByDescending(n => n.RequestedAt)
            .Take(10)
            .Select(n => new RecentNeedDto
            {
                Number = n.NumeroBesoin,
                Date = n.RequestedAt,
                Requester = n.RequestedBy?.FullName ?? "Inconnu",
                ArticleCount = n.Details?.Count ?? 0,
                Status = n.Status switch
                {
                    NeedStatus.TransmittedToPurchasing => "Transmis",
                    NeedStatus.InPurchase => "En cours",
                    NeedStatus.Cancelled => "Annulé",
                    _ => "En cours"
                },
                FirstArticle = n.Details?.FirstOrDefault()?.Product?.Designation ?? "-"
            })
            .ToList();

        // 8. Derniers BC (Top 10)
        var recentBcData = await _unitOfWork.PurchaseOrders.GetAllIncludingAsync(b => b.Supplier, b => b.Details);
        stats.RecentBcs = recentBcData
            .OrderByDescending(b => b.OrderDate)
            .Take(10)
            .Select(b => new RecentBcDto
            {
                Date = b.OrderDate,
                Number = b.OrderNumber,
                Supplier = b.Supplier?.CompanyName ?? "Inconnu",
                Status = b.Status,
                TotalTtc = b.TotalAmountTTC,
                FirstArticle = b.Details?.FirstOrDefault()?.Product?.Designation ?? "-"
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

    public async Task<AcheteurKpiDto> GetAcheteurKpisAsync()
    {
        // 1. Calculate current KPI counts from database
        var allNeeds = await _unitOfWork.Needs.GetAllAsync();
        var allQuotations = await _unitOfWork.Quotations.GetAllAsync();
        var allSuppliers = await _unitOfWork.Suppliers.GetAllAsync();
        var allPurchaseOrders = await _unitOfWork.PurchaseOrders.GetAllAsync();

        var currentKpis = new DashboardKpiSnapshot
        {
            SnapshotDate = DateTime.Today,
            BesEnCoursCount = allNeeds.Count(n => n.Status == NeedStatus.Draft || n.Status == NeedStatus.ToValidate || n.Status == NeedStatus.InPurchase || n.Status == NeedStatus.Relaunched),
            BesTransmisCount = allNeeds.Count(n => n.Status == NeedStatus.TransmittedToPurchasing),
            DevEnAttenteCount = allQuotations.Count(q => NormalizeQuotationStatus(q.Status) == QuotationStatus.Pending),
            DevValideCount = allQuotations.Count(q => NormalizeQuotationStatus(q.Status) == QuotationStatus.Validated),
            FournisseursActifsCount = allSuppliers.Count(s => s.IsActive),
            TotalBcCount = allPurchaseOrders.Count()
        };

        // Log current KPI values
        Log.Information("Current KPI values:");
        Log.Information("  BesEnCours: {Count}", currentKpis.BesEnCoursCount);
        Log.Information("  BesTransmis: {Count}", currentKpis.BesTransmisCount);
        Log.Information("  DevEnAttente: {Count}", currentKpis.DevEnAttenteCount);
        Log.Information("  DevValide: {Count}", currentKpis.DevValideCount);
        Log.Information("  FournisseursActifs: {Count}", currentKpis.FournisseursActifsCount);
        Log.Information("  TotalBc: {Count}", currentKpis.TotalBcCount);

        // 2. Save today's snapshot if it doesn't exist, or update it
        var today = DateTime.Today;
        Log.Information("Today: {Today}", today);
        
        var allSnapshots = await _unitOfWork.DashboardKpiSnapshots.GetAllAsync();
        
        // Log all SnapshotDate values
        Log.Information("All snapshots:");
        foreach (var snapshot in allSnapshots)
        {
            Log.Information("  {Date}", snapshot.SnapshotDate);
        }
        
        // Check for existing today's snapshot by Date part only
        var existingTodaySnapshot = allSnapshots.FirstOrDefault(s => s.SnapshotDate.Date == today.Date);
        
        if (existingTodaySnapshot != null)
        {
            // Update existing snapshot
            Log.Information("Updating existing snapshot for today: {Date}", existingTodaySnapshot.SnapshotDate);
            existingTodaySnapshot.BesEnCoursCount = currentKpis.BesEnCoursCount;
            existingTodaySnapshot.BesTransmisCount = currentKpis.BesTransmisCount;
            existingTodaySnapshot.DevEnAttenteCount = currentKpis.DevEnAttenteCount;
            existingTodaySnapshot.DevValideCount = currentKpis.DevValideCount;
            existingTodaySnapshot.FournisseursActifsCount = currentKpis.FournisseursActifsCount;
            existingTodaySnapshot.TotalBcCount = currentKpis.TotalBcCount;
            _unitOfWork.DashboardKpiSnapshots.Update(existingTodaySnapshot);
        }
        else
        {
            // Add new snapshot
            Log.Information("Adding new snapshot for today");
            await _unitOfWork.DashboardKpiSnapshots.AddAsync(currentKpis);
        }

        await _unitOfWork.CompleteAsync();

        // 3. Find snapshot from 7 days ago (or closest before that)
        var targetDate = today.AddDays(-7).Date;
        Log.Information("Target date (7 days ago): {TargetDate}", targetDate);
        
        var snapshot7DaysAgo = allSnapshots
            .Where(s => s.SnapshotDate.Date <= targetDate.Date)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefault();

        Log.Information("Selected previous snapshot date: {SelectedDate}", 
            snapshot7DaysAgo != null ? snapshot7DaysAgo.SnapshotDate.ToString() : "No previous snapshot found");
        
        if (snapshot7DaysAgo != null)
        {
            Log.Information("Previous KPI values:");
            Log.Information("  BesEnCours: {Count}", snapshot7DaysAgo.BesEnCoursCount);
            Log.Information("  BesTransmis: {Count}", snapshot7DaysAgo.BesTransmisCount);
            Log.Information("  DevEnAttente: {Count}", snapshot7DaysAgo.DevEnAttenteCount);
            Log.Information("  DevValide: {Count}", snapshot7DaysAgo.DevValideCount);
            Log.Information("  FournisseursActifs: {Count}", snapshot7DaysAgo.FournisseursActifsCount);
            Log.Information("  TotalBc: {Count}", snapshot7DaysAgo.TotalBcCount);
        }

        // 4. Calculate evolution percentages
        var result = new AcheteurKpiDto
        {
            BesEnCoursCount = currentKpis.BesEnCoursCount,
            BesTransmisCount = currentKpis.BesTransmisCount,
            DevEnAttenteCount = currentKpis.DevEnAttenteCount,
            DevValideCount = currentKpis.DevValideCount,
            FournisseursActifsCount = currentKpis.FournisseursActifsCount,
            TotalBcCount = currentKpis.TotalBcCount,

            BesEnCoursEvolution = CalculateEvolution(currentKpis.BesEnCoursCount, snapshot7DaysAgo?.BesEnCoursCount),
            BesTransmisEvolution = CalculateEvolution(currentKpis.BesTransmisCount, snapshot7DaysAgo?.BesTransmisCount),
            DevEnAttenteEvolution = CalculateEvolution(currentKpis.DevEnAttenteCount, snapshot7DaysAgo?.DevEnAttenteCount),
            DevValideEvolution = CalculateEvolution(currentKpis.DevValideCount, snapshot7DaysAgo?.DevValideCount),
            FournisseursActifsEvolution = CalculateEvolution(currentKpis.FournisseursActifsCount, snapshot7DaysAgo?.FournisseursActifsCount),
            TotalBcEvolution = CalculateEvolution(currentKpis.TotalBcCount, snapshot7DaysAgo?.TotalBcCount)
        };

        Log.Information("Calculated percentages:");
        Log.Information("  BesEnCoursEvolution: {Percent}", result.BesEnCoursEvolution);
        Log.Information("  BesTransmisEvolution: {Percent}", result.BesTransmisEvolution);
        Log.Information("  DevEnAttenteEvolution: {Percent}", result.DevEnAttenteEvolution);
        Log.Information("  DevValideEvolution: {Percent}", result.DevValideEvolution);
        Log.Information("  FournisseursActifsEvolution: {Percent}", result.FournisseursActifsEvolution);
        Log.Information("  TotalBcEvolution: {Percent}", result.TotalBcEvolution);

        return result;
    }

    private double CalculateEvolution(int current, int? previous)
    {
        if (!previous.HasValue || previous.Value == 0)
        {
            return current > 0 ? 100 : 0;
        }
        return Math.Round(((double)(current - previous.Value) / previous.Value) * 100, 1);
    }
}
