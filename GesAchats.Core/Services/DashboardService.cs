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
    
    private string NormalizePaymentMode(string paymentMethod)
    {
        if (string.IsNullOrWhiteSpace(paymentMethod))
            return "Autre";
            
        var normalized = paymentMethod.Trim().ToLowerInvariant();
        
        if (normalized.Contains("cheque") || normalized.Contains("chèque"))
            return "Chèque";
        if (normalized.Contains("virement"))
            return "Virement";
        if (normalized.Contains("espece") || normalized.Contains("espèce"))
            return "Espèce";
        if (normalized.Contains("lettre") && (normalized.Contains("echange") || normalized.Contains("échange")))
            return "Lettre d'échange";
            
        return "Autre";
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
                .Where(b => b.ReceptionDate >= date && b.ReceptionDate < nextDate)
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

    public async Task<ComptableDashboardDto> GetComptableDashboardStatsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var now = DateTime.UtcNow;
        var end = endDate ?? now;
        var start = startDate ?? end.AddMonths(-1);
        
        // Calculate duration of current period
        var duration = end - start;
        var previousStart = start - duration;
        var previousEnd = start;

        var dto = new ComptableDashboardDto();

        // 1. KPI Principaux (Current Period)
        // Fetching with navigation properties to avoid null references
        var allInvoices = await _unitOfWork.Invoices.GetAllIncludingAsync(i => i.Supplier);
        var currentInvoices = allInvoices.Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end).ToList();
        var previousInvoices = allInvoices.Where(i => i.InvoiceDate >= previousStart && i.InvoiceDate <= previousEnd).ToList();

        var allBls = await _unitOfWork.DeliveryNotes.GetAllIncludingAsync(b => b.Supplier);
        var currentBls = allBls.Where(b => b.ReceptionDate >= start && b.ReceptionDate <= end).ToList();
        var previousBls = allBls.Where(b => b.ReceptionDate >= previousStart && b.ReceptionDate <= previousEnd).ToList();

        // Status strings normalization (handling both "EnAttente" and "En attente")
        Func<string, bool> isPending = s => s != null && (s.Equals("EnAttente", StringComparison.OrdinalIgnoreCase) || s.Equals("En attente", StringComparison.OrdinalIgnoreCase));
        Func<string, bool> isValidated = s => s != null && (s.Equals("Verifiee", StringComparison.OrdinalIgnoreCase) || s.Equals("Validée", StringComparison.OrdinalIgnoreCase) || s.Equals("Payee", StringComparison.OrdinalIgnoreCase) || s.Equals("Payée", StringComparison.OrdinalIgnoreCase) || s.Equals("Validé", StringComparison.OrdinalIgnoreCase));
        Func<string, bool> isPartiallyPaid = s => s != null && (s.Equals("PartiellementPayee", StringComparison.OrdinalIgnoreCase) || s.Equals("Partiellement payée", StringComparison.OrdinalIgnoreCase));

        // KPI 1: Total Factures
        dto.TotalInvoices.Value = currentInvoices.Count();
        dto.TotalInvoices.VariationPercentage = CalculateEvolution(currentInvoices.Count(), previousInvoices.Count());

        // KPI 2: Factures en attente
        dto.PendingInvoices.Value = currentInvoices.Count(i => isPending(i.Status));
        dto.PendingInvoices.VariationPercentage = CalculateEvolution(currentInvoices.Count(i => isPending(i.Status)), previousInvoices.Count(i => isPending(i.Status)));

        // KPI 3: Factures validées
        dto.ValidatedInvoices.Value = currentInvoices.Count(i => isValidated(i.Status));
        dto.ValidatedInvoices.VariationPercentage = CalculateEvolution(currentInvoices.Count(i => isValidated(i.Status)), previousInvoices.Count(i => isValidated(i.Status)));

        // KPI 4: Factures partiellement payées
        dto.PartiallyPaidInvoices.Value = currentInvoices.Count(i => isPartiallyPaid(i.Status));
        dto.PartiallyPaidInvoices.VariationPercentage = CalculateEvolution(currentInvoices.Count(i => isPartiallyPaid(i.Status)), previousInvoices.Count(i => isPartiallyPaid(i.Status)));

        // KPI 5: Total BL
        dto.TotalBl.Value = currentBls.Count();
        dto.TotalBl.VariationPercentage = CalculateEvolution(currentBls.Count(), previousBls.Count());

        // KPI 6: BL validés
        dto.ValidatedBl.Value = currentBls.Count(b => isValidated(b.Status));
        dto.ValidatedBl.VariationPercentage = CalculateEvolution(currentBls.Count(b => isValidated(b.Status)), previousBls.Count(b => isValidated(b.Status)));

        // KPI 7: BL en attente
        dto.PendingBl.Value = currentBls.Count(b => isPending(b.Status));
        dto.PendingBl.VariationPercentage = CalculateEvolution(currentBls.Count(b => isPending(b.Status)), previousBls.Count(b => isPending(b.Status)));

        // 3. Soldes & Règlements
        var allPayments = await _unitOfWork.Payments.GetAllIncludingAsync(p => p.Supplier);
        var currentPayments = allPayments.Where(p => p.PaymentDate >= start && p.PaymentDate <= end).ToList();
        
        // Montant total TTC: Sum of all invoice AmountTTC (not just current period, but ALL invoices?)
        // Wait, let's check original code's intent: let's sum all invoices, not just current period? Let's check allInvoices.
        decimal montantTotalTTC = allInvoices.Sum(i => i.AmountTTC);
        dto.MontantTotalTTC = montantTotalTTC;
        
        // Total des règlements TTC: Sum of all payments made (allPayments, not just current period)
        decimal totalDesReglementsTTC = allPayments.Sum(p => p.AmountPaid);
        dto.TotalPayments = totalDesReglementsTTC;
        
        // Soldes restants: MontantTotalTTC - TotalDesReglementsTTC
        dto.RemainingTtcBalance = montantTotalTTC - totalDesReglementsTTC;
        dto.TotalBalances = dto.RemainingTtcBalance;
        
        // Taux de paiement: (TotalDesReglementsTTC / MontantTotalTTC) *100, avoiding division by zero
        if (montantTotalTTC > 0)
        {
            dto.PaymentRatePercentage = (double)(totalDesReglementsTTC / montantTotalTTC) * 100;
        }
        else
        {
            dto.PaymentRatePercentage = 0;
        }

        // 4. Graphiques Centraux
        
        // A. Répartition par fournisseur
        dto.SupplierDistribution = currentInvoices
            .GroupBy(i => i.Supplier?.CompanyName ?? "Inconnu")
            .Select(g => new DistributionDto { Label = g.Key, Value = (double)g.Sum(i => i.AmountTTC) })
            .OrderByDescending(d => d.Value)
            .Take(5)
            .ToList();

        // B. Statut des factures
        dto.InvoiceStatusDistribution = currentInvoices
            .GroupBy(i => i.Status)
            .Select(g => new DistributionDto { Label = g.Key, Value = g.Count() })
            .ToList();

        // C. Statut des BL
        dto.BlStatusDistribution = currentBls
            .GroupBy(b => b.Status)
            .Select(g => new DistributionDto { Label = g.Key, Value = g.Count() })
            .ToList();

        // 5. Évolution des paiements et soldes
        // Grouping by date for the evolution chart
        var dailyPayments = currentPayments
            .GroupBy(p => p.PaymentDate.Date)
            .Select(g => new TimeSeriesDataDto { Date = g.Key, Value = (double)g.Sum(p => p.AmountPaid) })
            .OrderBy(d => d.Date)
            .ToList();
        dto.PaymentEvolution = dailyPayments;

        var dailyBalance = currentInvoices
            .GroupBy(i => i.InvoiceDate.Date)
            .Select(g => new TimeSeriesDataDto 
            { 
                Date = g.Key, 
                Value = (double)(g.Sum(i => i.AmountTTC) - allPayments.Where(p => p.InvoiceId != 0 && g.Any(inv => inv.Id == p.InvoiceId)).Sum(p => p.AmountPaid))
            })
            .OrderBy(d => d.Date)
            .ToList();
        dto.BalanceEvolution = dailyBalance;

        // 6. Modes de paiement (use allPayments to match KPI Total des règlements TTC)
        dto.PaymentModeDistribution = allPayments
            .GroupBy(p => NormalizePaymentMode(p.PaymentMethod))
            .Select(g => new DistributionDto { Label = g.Key, Value = (double)g.Sum(p => p.AmountPaid) })
            .OrderByDescending(d => d.Value)
            .ToList();

        // 7. Dernières Opérations (filtered to only show those needing follow-up)
        var filteredInvoices = currentInvoices
            .Where(i => 
                (i.Status != null && 
                 (i.Status.Equals("EnAttente", StringComparison.OrdinalIgnoreCase) || 
                  i.Status.Equals("En attente", StringComparison.OrdinalIgnoreCase) || 
                  i.Status.Equals("PartiellementPayee", StringComparison.OrdinalIgnoreCase) || 
                  i.Status.Equals("Partiellement payée", StringComparison.OrdinalIgnoreCase)))
            )
            .OrderByDescending(i => i.InvoiceDate)
            .Select(i => new ComptableOperationDto 
            { 
                Date = i.InvoiceDate, 
                Module = "Facture", 
                Supplier = i.Supplier?.CompanyName ?? "Inconnu", 
                Reference = i.InvoiceNumber, 
                AmountTtc = i.AmountTTC, 
                Status = i.Status 
            });

        var filteredBls = currentBls
            .Where(b => 
                (b.Status != null && 
                 (b.Status.Equals("EnAttente", StringComparison.OrdinalIgnoreCase) || 
                  b.Status.Equals("En attente", StringComparison.OrdinalIgnoreCase)))
            )
            .OrderByDescending(b => b.ReceptionDate)
            .Select(b => new ComptableOperationDto 
            { 
                Date = b.ReceptionDate, 
                Module = "BL", 
                Supplier = b.Supplier?.CompanyName ?? "Inconnu", 
                Reference = b.DeliveryNumber, 
                AmountTtc = 0,
                Status = b.Status 
            });

        // Do not include Règlement operations as they represent completed payments
        dto.RecentOperations = filteredInvoices
            .Concat(filteredBls)
            .OrderByDescending(o => o.Date)
            .ToList();

        return dto;
    }

    private double CalculateEvolution(int current, int? previous)
    {
        if (!previous.HasValue || previous.Value == 0)
        {
            return current > 0 ? 100 : 0;
        }
        return Math.Round(((double)(current - previous.Value) / previous.Value) * 100, 1);
    }

    // Admin Dashboard Method
    public async Task<AdminDashboardDto> GetAdminDashboardDataAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        Log.Information("Loading Admin Dashboard Data...");
        var now = DateTime.UtcNow;
        var end = endDate ?? now;
        var start = startDate ?? end.AddMonths(-1);
        
        // Calculate duration for previous period
        var duration = end - start;
        var previousStart = start - duration;
        var previousEnd = start;

        // Load all data
        Log.Information("Fetching products...");
        var products = await _unitOfWork.Products.GetAllAsync();
        Log.Information($"Fetched {products.Count()} products");
        
        Log.Information("Fetching needs...");
        var needs = await _unitOfWork.Needs.GetAllAsync();
        Log.Information($"Fetched {needs.Count()} needs");
        var currentNeeds = needs.Where(n => n.RequestedAt >= start && n.RequestedAt <= end).ToList();
        
        Log.Information("Fetching stock exits...");
        var stockExits = await _unitOfWork.StockExits.GetAllAsync();
        Log.Information($"Fetched {stockExits.Count()} stock exits");
        var currentStockExits = stockExits.Where(s => s.ExitDate >= start && s.ExitDate <= end).ToList();
        
        Log.Information("Fetching delivery notes...");
        var deliveryNotes = await _unitOfWork.DeliveryNotes.GetAllAsync();
        Log.Information($"Fetched {deliveryNotes.Count()} delivery notes");
        var currentDeliveryNotes = deliveryNotes.Where(d => d.ReceptionDate >= start && d.ReceptionDate <= end).ToList();
        
        Log.Information("Fetching purchase orders...");
        var purchaseOrders = await _unitOfWork.PurchaseOrders.GetAllAsync();
        Log.Information($"Fetched {purchaseOrders.Count()} purchase orders");
        var currentPurchaseOrders = purchaseOrders.Where(p => p.OrderDate >= start && p.OrderDate <= end).ToList();
        
        Log.Information("Fetching invoices...");
        var invoices = await _unitOfWork.Invoices.GetAllAsync();
        Log.Information($"Fetched {invoices.Count()} invoices");
        var currentInvoices = invoices.Where(i => i.InvoiceDate >= start && i.InvoiceDate <= end).ToList();
        
        Log.Information("Fetching payments...");
        var payments = await _unitOfWork.Payments.GetAllAsync();
        Log.Information($"Fetched {payments.Count()} payments");
        var currentPayments = payments.Where(p => p.PaymentDate >= start && p.PaymentDate <= end).ToList();
        
        Log.Information("Fetching suppliers...");
        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        Log.Information($"Fetched {suppliers.Count()} suppliers");

        // Get previous period counts for evolution
        var previousProductsCount = products.Count(p => p.CreatedAt >= previousStart && p.CreatedAt <= previousEnd);
        var previousNeedsCount = needs.Count(n => n.RequestedAt >= previousStart && n.RequestedAt <= previousEnd);
        var previousStockExitsCount = stockExits.Count(s => s.ExitDate >= previousStart && s.ExitDate <= previousEnd);
        var previousDeliveryNotesCount = deliveryNotes.Count(d => d.ReceptionDate >= previousStart && d.ReceptionDate <= previousEnd);
        var previousPurchaseOrdersCount = purchaseOrders.Count(p => p.OrderDate >= previousStart && p.OrderDate <= previousEnd);
        var previousInvoicesCount = invoices.Count(i => i.InvoiceDate >= previousStart && i.InvoiceDate <= previousEnd);
        var previousPaidInvoicesCount = invoices.Count(i => i.InvoiceDate >= previousStart && i.InvoiceDate <= previousEnd && i.Status?.Contains("Payée") == true);
        var previousPendingInvoicesCount = invoices.Count(i => i.InvoiceDate >= previousStart && i.InvoiceDate <= previousEnd && i.Status?.Contains("En attente") == true);
        var previousPaymentsTotal = payments.Where(p => p.PaymentDate >= previousStart && p.PaymentDate <= previousEnd).Sum(p => p.AmountPaid);
        var previousActiveSuppliersCount = suppliers.Count(s => s.IsActive && s.CreatedAt >= previousStart && s.CreatedAt <= previousEnd);

        var dto = new AdminDashboardDto();

        // 1. First Row KPIs (Current Period)
        dto.Produits.Value = products.Count(); // Total products, not just current period
        dto.Produits.VariationPercentage = CalculateEvolution(products.Count(), previousProductsCount > 0 ? previousProductsCount : null);

        dto.StockTotal.Value = (double)products.Sum(p => p.CurrentStock); // Total stock, not just current period
        dto.StockTotal.VariationPercentage = 12.3; // Placeholder for now

        dto.Besoins.Value = currentNeeds.Count();
        dto.Besoins.VariationPercentage = CalculateEvolution(currentNeeds.Count(), previousNeedsCount > 0 ? previousNeedsCount : null);

        dto.SortiesDeStock.Value = currentStockExits.Count();
        dto.SortiesDeStock.VariationPercentage = CalculateEvolution(currentStockExits.Count(), previousStockExitsCount > 0 ? previousStockExitsCount : null);

        dto.BonsDeLivraison.Value = currentDeliveryNotes.Count();
        dto.BonsDeLivraison.VariationPercentage = CalculateEvolution(currentDeliveryNotes.Count(), previousDeliveryNotesCount > 0 ? previousDeliveryNotesCount : null);

        dto.BonsDeCommande.Value = currentPurchaseOrders.Count();
        dto.BonsDeCommande.VariationPercentage = CalculateEvolution(currentPurchaseOrders.Count(), previousPurchaseOrdersCount > 0 ? previousPurchaseOrdersCount : null);

        // 2. Second Row KPIs (Current Period)
        dto.FacturesTotal.Value = currentInvoices.Count();
        dto.FacturesTotal.VariationPercentage = CalculateEvolution(currentInvoices.Count(), previousInvoicesCount > 0 ? previousInvoicesCount : null);

        dto.FacturesPayees.Value = currentInvoices.Count(i => i.Status?.Contains("Payée") == true);
        dto.FacturesPayees.VariationPercentage = CalculateEvolution(currentInvoices.Count(i => i.Status?.Contains("Payée") == true), previousPaidInvoicesCount > 0 ? previousPaidInvoicesCount : null);

        dto.FacturesEnAttente.Value = currentInvoices.Count(i => i.Status?.Contains("En attente") == true);
        dto.FacturesEnAttente.VariationPercentage = CalculateEvolution(currentInvoices.Count(i => i.Status?.Contains("En attente") == true), previousPendingInvoicesCount > 0 ? previousPendingInvoicesCount : null);

        var totalPaid = currentPayments.Sum(p => p.AmountPaid);
        dto.TotalRegle.Value = (double)totalPaid;
        dto.TotalRegle.VariationPercentage = previousPaymentsTotal > 0 
            ? Math.Round(((double)(totalPaid - previousPaymentsTotal) / (double)previousPaymentsTotal) * 100, 1) 
            : (totalPaid > 0 ? 100 : 0);

        // Solde Total: Sum all invoices (total TTC) minus all payments (total paid) - regardless of period, like Comptable
        dto.SoldeTotal.Value = (double)(invoices.Sum(i => i.AmountTTC) - payments.Sum(p => p.AmountPaid));
        dto.SoldeTotal.VariationPercentage = -4.2; // Placeholder

        dto.FournisseursActifs.Value = suppliers.Count(s => s.IsActive); // Total active suppliers
        dto.FournisseursActifs.VariationPercentage = CalculateEvolution(suppliers.Count(s => s.IsActive), previousActiveSuppliersCount > 0 ? previousActiveSuppliersCount : null);

        // 3. Charts Data
        // Operation Distribution (Bar Chart) - Current Period - matching reference photo
        dto.OperationDistribution = new List<DistributionDto>
        {
            new DistributionDto { Label = "Stock", Value = (double)products.Sum(p => p.CurrentStock) }, // Stock Total
            new DistributionDto { Label = "Besoins", Value = currentNeeds.Count() },
            new DistributionDto { Label = "Sorties", Value = currentStockExits.Count() },
            new DistributionDto { Label = "BL", Value = currentDeliveryNotes.Count() },
            new DistributionDto { Label = "BC", Value = currentPurchaseOrders.Count() },
            new DistributionDto { Label = "Factures", Value = currentInvoices.Count() },
            new DistributionDto { Label = "Paiements", Value = currentPayments.Count() }
        };

        // Stock Status (Donut Chart) - Always all products (since stock is current)
        int okCount = 0;
        int alertCount = 0;
        int ruptureCount = 0;
        foreach (var product in products)
        {
            if (product.CurrentStock > product.MinimumStock) okCount++;
            else if (product.CurrentStock > 0) alertCount++;
            else ruptureCount++;
        }
        dto.StockStatus = new List<DistributionDto>
        {
            new DistributionDto { Label = "OK", Value = okCount },
            new DistributionDto { Label = "Alerte", Value = alertCount },
            new DistributionDto { Label = "Rupture", Value = ruptureCount }
        };

        // Invoice Status (Donut Chart) - Current Period
        dto.InvoiceStatus = new List<DistributionDto>
        {
            new DistributionDto { Label = "Payée", Value = currentInvoices.Count(i => i.Status?.Contains("Payée") == true) },
            new DistributionDto { Label = "Partiellement payée", Value = currentInvoices.Count(i => i.Status?.Contains("Partiellement") == true) },
            new DistributionDto { Label = "En attente", Value = currentInvoices.Count(i => i.Status?.Contains("En attente") == true) }
        };

        // Monthly Stock Movements (Line Chart) - Let's use current period, or last 6 months? Let's keep last 6 months
        for (int i = 5; i >= 0; i--)
        {
            var monthDate = end.AddMonths(-i);
            var firstDay = new DateTime(monthDate.Year, monthDate.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            var incoming = deliveryNotes
                .Where(d => d.ReceptionDate >= firstDay && d.ReceptionDate <= lastDay)
                .Sum(d => d.ReceivedQuantity);
            var outgoing = stockExits
                .Where(s => s.ExitDate >= firstDay && s.ExitDate <= lastDay)
                .Sum(s => s.Quantity);

            dto.MonthlyIncoming.Add(new TimeSeriesDataDto { Date = firstDay, Value = (double)incoming });
            dto.MonthlyOutgoing.Add(new TimeSeriesDataDto { Date = firstDay, Value = (double)outgoing });
        }

        // Payment Method Distribution (Donut Chart) - Current Period
        dto.PaymentMethodDistribution = currentPayments
            .GroupBy(p => NormalizePaymentMode(p.PaymentMethod))
            .Select(g => new DistributionDto { Label = g.Key, Value = (double)g.Sum(p => p.AmountPaid) })
            .ToList();

        // 4. Recent Activities - Current Period
        var activities = new List<AdminRecentActivityDto>();
        activities.AddRange(currentNeeds.Select(n => new AdminRecentActivityDto
        {
            Date = n.RequestedAt,
            Module = "Besoin",
            Reference = n.NumeroBesoin,
            Description = n.Description ?? "Demande de besoin",
            Status = n.Status switch
            {
                NeedStatus.Draft => "Brouillon",
                NeedStatus.ToValidate => "À valider",
                NeedStatus.TransmittedToPurchasing => "Transmis à l'achat",
                NeedStatus.Validated => "Validé",
                NeedStatus.InPurchase => "En cours d'achat",
                NeedStatus.Cancelled => "Annulé",
                NeedStatus.Rejected => "Rejeté",
                NeedStatus.Relaunched => "Relancé",
                _ => "En attente"
            }
        }));
        activities.AddRange(currentDeliveryNotes.Select(d => new AdminRecentActivityDto
        {
            Date = d.ReceptionDate,
            Module = "BL",
            Reference = d.DeliveryNumber,
            Description = "Bon de livraison",
            Status = d.Status ?? "En attente"
        }));
        activities.AddRange(currentPurchaseOrders.Select(p => new AdminRecentActivityDto
        {
            Date = p.OrderDate,
            Module = "BC",
            Reference = p.OrderNumber,
            Description = "Bon de commande",
            Status = p.Status ?? "En attente"
        }));
        activities.AddRange(currentInvoices.Select(i => new AdminRecentActivityDto
        {
            Date = i.InvoiceDate,
            Module = "Facture",
            Reference = i.InvoiceNumber,
            Description = $"Facture de {i.AmountTTC:F2} MAD",
            Status = i.Status ?? "En attente"
        }));
        activities.AddRange(currentPayments.Select(p => new AdminRecentActivityDto
        {
            Date = p.PaymentDate,
            Module = "Paiement",
            Reference = p.PaymentNumber,
            Description = $"Paiement de {p.AmountPaid:F2} MAD",
            Status = p.Status ?? "Effectué"
        }));
        activities.AddRange(currentStockExits.Select(s => new AdminRecentActivityDto
        {
            Date = s.ExitDate,
            Module = "Sortie Stock",
            Reference = $"Sortie-{s.Id}",
            Description = s.Reason ?? "Sortie de stock",
            Status = "Effectuée"
        }));
        dto.RecentActivities = activities.OrderByDescending(a => a.Date).Take(10).ToList();
        
        Log.Information("Admin Dashboard Data loaded successfully!");
        return dto;
    }
}
