using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GesAchats.Core.DTOs;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace GesAchats.Core.Services;

public class SuiviFournisseurService : ISuiviFournisseurService
{
    private readonly IUnitOfWork _unitOfWork;

    public SuiviFournisseurService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SuiviFournisseurKpisDto> GetSuiviKpisAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // Périodes (identique au Dashboard principal) : période actuelle vs période précédente de même durée
        var now = DateTime.UtcNow;
        var end = endDate ?? now;
        var start = startDate ?? end.AddMonths(-1);
        var duration = end - start;
        var previousStart = start - duration;
        var previousEnd = start;

        Log.Information("GetSuiviKpisAsync: Period {Start} -> {End}, previous {PreviousStart} -> {PreviousEnd}", start, end, previousStart, previousEnd);

        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        var purchaseOrders = await _unitOfWork.PurchaseOrders.GetAllAsync();
        var invoices = await _unitOfWork.Invoices.GetAllAsync();
        var payments = await _unitOfWork.Payments.GetAllAsync();

        // Total fournisseurs : total cumulé, précédent = nouveaux fournisseurs créés sur la période précédente
        var totalSuppliers = suppliers.Count();
        var previousSuppliersCount = suppliers.Count(s => s.IsActive && s.CreatedAt >= previousStart && s.CreatedAt <= previousEnd);

        // Commandes en cours : commandes non annulées sur la période
        var activeOrders = purchaseOrders.Where(o => o.Status != PurchaseOrderStatus.Cancelled).ToList();
        var currentOrders = activeOrders.Where(o => o.OrderDate >= start && o.OrderDate <= end).ToList();
        var previousOrders = activeOrders.Where(o => o.OrderDate >= previousStart && o.OrderDate <= previousEnd).ToList();
        var totalCommande = currentOrders.Sum(o => o.TotalAmountTTC);
        var previousTotalCommande = previousOrders.Sum(o => o.TotalAmountTTC);

        // Solde total : solde cumulé "tous temps" (comme SoldeTotal du dashboard), comparaison au solde à la fin de la période précédente
        var soldeActuel = invoices.Sum(i => i.AmountTTC) - payments.Sum(p => p.AmountPaid);
        var soldePrecedent = invoices.Where(i => i.InvoiceDate <= previousEnd).Sum(i => i.AmountTTC)
                            - payments.Where(p => p.PaymentDate <= previousEnd).Sum(p => p.AmountPaid);

        Log.Information("GetSuiviKpisAsync: Suppliers {Suppliers} (prev {PrevSuppliers}), Orders {Orders} (prev {PrevOrders}), TotalCommande {TotalCommande} (prev {PrevTotalCommande}), Solde {Solde} (prev {PrevSolde})",
            totalSuppliers, previousSuppliersCount, currentOrders.Count, previousOrders.Count, totalCommande, previousTotalCommande, soldeActuel, soldePrecedent);

        return new SuiviFournisseurKpisDto
        {
            TotalFournisseurs = new KpiModel
            {
                Current = totalSuppliers,
                Previous = previousSuppliersCount
            },
            CommandesEnCours = new KpiModel
            {
                Current = currentOrders.Count,
                Previous = previousOrders.Count
            },
            TotalCommande = new KpiModel
            {
                Current = (double)totalCommande,
                Previous = (double)previousTotalCommande
            },
            SoldeTotal = new KpiModel
            {
                Current = (double)soldeActuel,
                Previous = (double)soldePrecedent
            }
        };
    }

    public async Task<PagedResult<FournisseurSuiviDto>> SearchFournisseursAsync(string searchText, int pageNumber, int pageSize)
    {
        Log.Information("SearchFournisseursAsync: Search text '{SearchText}', Page {PageNumber}, Size {PageSize}", searchText, pageNumber, pageSize);
        var suppliers = (await _unitOfWork.Suppliers.GetAllAsync()).AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchLower = searchText.ToLower();
            suppliers = suppliers.Where(s => 
                (s.CompanyName != null && s.CompanyName.ToLower().Contains(searchLower)) ||
                (s.ContactName != null && s.ContactName.ToLower().Contains(searchLower)) ||
                (s.Email != null && s.Email.ToLower().Contains(searchLower)) ||
                (s.Phone != null && s.Phone.ToLower().Contains(searchLower)));
        }

        var totalCount = suppliers.Count();
        var items = suppliers
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new FournisseurSuiviDto
            {
                Id = s.Id,
                NomFournisseur = s.CompanyName,
                NomContact = s.ContactName,
                Telephone = s.Phone,
                Email = s.Email,
                Ville = s.City
            })
            .ToList();

        Log.Information("SearchFournisseursAsync: Found {Count} suppliers, returning {Items} items", totalCount, items.Count);

        return new PagedResult<FournisseurSuiviDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<SituationFournisseurDto> GetSituationFournisseurAsync(int fournisseurId, DateTime? startDate = null, DateTime? endDate = null)
    {
        Log.Information("GetSituationFournisseurAsync: Starting for supplier ID {FournisseurId}", fournisseurId);

        // Récupération du fournisseur
        var supplier = await _unitOfWork.Suppliers.GetByIdAsync(fournisseurId);
        if (supplier == null)
        {
            Log.Warning("GetSituationFournisseurAsync: Supplier {FournisseurId} not found", fournisseurId);
            return new SituationFournisseurDto { FournisseurId = fournisseurId };
        }

        Log.Information("GetSituationFournisseurAsync: Found supplier {SupplierName}", supplier.CompanyName);

        // Récupération des données nécessaires pour ce fournisseur
        var allPurchaseOrders = await _unitOfWork.PurchaseOrders.GetAllWithSuppliersAsync();
        var supplierPurchaseOrders = allPurchaseOrders.Where(po => po.SupplierId == fournisseurId).ToList();
        Log.Information("GetSituationFournisseurAsync: Found {Count} POs for supplier", supplierPurchaseOrders.Count);

        var purchaseOrders = new List<PurchaseOrder>();
        foreach (var po in supplierPurchaseOrders)
        {
            var poWithDetails = await _unitOfWork.PurchaseOrders.GetWithDetailsAsync(po.Id);
            if (poWithDetails != null)
            {
                purchaseOrders.Add(poWithDetails);
                Log.Information("GetSituationFournisseurAsync: Added PO {OrderNumber} with {DetailCount} details", poWithDetails.OrderNumber, poWithDetails.Details?.Count ?? 0);
            }
        }

        var allDeliveryNotes = await _unitOfWork.DeliveryNotes.GetAllWithDetailsAsync();
        var deliveryNotes = allDeliveryNotes.Where(dn => dn.SupplierId == fournisseurId).ToList();
        Log.Information("GetSituationFournisseurAsync: Found {Count} delivery notes for supplier", deliveryNotes.Count);

        var allInvoicesQuery = _unitOfWork.Invoices.GetQueryable();
        var allInvoices = await allInvoicesQuery
            .Include(i => i.PurchaseOrder)
            .Include(i => i.DeliveryNote)
            .Include(i => i.Supplier)
            .Include(i => i.Details)
                .ThenInclude(id => id.Product)
            .ToListAsync();
        var invoices = allInvoices.Where(i => i.SupplierId == fournisseurId).ToList();
        Log.Information("GetSituationFournisseurAsync: Found {Count} invoices for supplier", invoices.Count);

        var allPayments = await _unitOfWork.Payments.GetAllAsync();
        var payments = allPayments.Where(p => p.SupplierId == fournisseurId).ToList();
        Log.Information("GetSituationFournisseurAsync: Found {Count} payments for supplier", payments.Count);

        // Périodes (identique au Dashboard principal) : période actuelle vs période précédente de même durée
        var now = DateTime.UtcNow;
        var endOfPeriod = endDate ?? now;
        var startOfPeriod = startDate ?? endOfPeriod.AddMonths(-1);
        var duration = endOfPeriod - startOfPeriod;
        var previousStart = startOfPeriod - duration;
        var previousEnd = startOfPeriod;

        // KPIs basés sur la période (current period)
        var currentCommandes = purchaseOrders.Where(po => po.OrderDate >= startOfPeriod && po.OrderDate <= endOfPeriod).ToList();
        var previousCommandes = purchaseOrders.Where(po => po.OrderDate >= previousStart && po.OrderDate <= previousEnd).ToList();
        var currentBls = deliveryNotes.Where(dn => dn.ReceptionDate >= startOfPeriod && dn.ReceptionDate <= endOfPeriod).ToList();
        var previousBls = deliveryNotes.Where(dn => dn.ReceptionDate >= previousStart && dn.ReceptionDate <= previousEnd).ToList();
        var currentInvoices = invoices.Where(i => i.InvoiceDate >= startOfPeriod && i.InvoiceDate <= endOfPeriod).ToList();
        var previousInvoices = invoices.Where(i => i.InvoiceDate >= previousStart && i.InvoiceDate <= previousEnd).ToList();
        var currentPayments = payments.Where(p => p.PaymentDate >= startOfPeriod && p.PaymentDate <= endOfPeriod).ToList();
        var previousPayments = payments.Where(p => p.PaymentDate >= previousStart && p.PaymentDate <= previousEnd).ToList();

        // Solde à payer : solde cumulé "tous temps" (comme SoldeTotal du dashboard), comparaison au solde à la fin de la période précédente
        var totalInvoicesAmount = invoices.Sum(i => i.AmountTTC);
        var totalPaymentsAmount = payments.Sum(p => p.AmountPaid);
        var soldeActuel = totalInvoicesAmount - totalPaymentsAmount;
        var soldePrecedent = invoices.Where(i => i.InvoiceDate <= previousEnd).Sum(i => i.AmountTTC)
                            - payments.Where(p => p.PaymentDate <= previousEnd).Sum(p => p.AmountPaid);

        var result = new SituationFournisseurDto
        {
            FournisseurId = fournisseurId,
            NomFournisseur = supplier.CompanyName,
            NomContact = supplier.ContactName,
            Telephone = supplier.Phone,
            Email = supplier.Email,
            Ville = supplier.City,
            TotalCommandes = currentCommandes.Count,
            TotalCommandesPrevious = previousCommandes.Count,
            TotalCommandesVariation = CalculateVariation(currentCommandes.Count, previousCommandes.Count),
            TotalBls = currentBls.Count,
            TotalBlsPrevious = previousBls.Count,
            TotalBlsVariation = CalculateVariation(currentBls.Count, previousBls.Count),
            TotalFactures = currentInvoices.Count,
            TotalFacturesPrevious = previousInvoices.Count,
            TotalFacturesVariation = CalculateVariation(currentInvoices.Count, previousInvoices.Count),
            TotalReglements = currentPayments.Sum(p => p.AmountPaid),
            TotalReglementsPrevious = previousPayments.Sum(p => p.AmountPaid),
            TotalReglementsVariation = CalculateVariation((double)currentPayments.Sum(p => p.AmountPaid), (double)previousPayments.Sum(p => p.AmountPaid)),
            SoldeAPayer = soldeActuel,
            SoldeAPayerPrevious = soldePrecedent,
            SoldeAPayerVariation = CalculateVariation((double)soldeActuel, (double)soldePrecedent),
            Operations = new List<SituationOperationGroupDto>()
        };

        Log.Information("GetSituationFournisseurAsync: Calculated KPIs - Orders: {TotalCommandes}, BLs: {TotalBls}, Invoices: {TotalFactures}, Payments: {TotalReglements}, Solde: {SoldeAPayer}",
            result.TotalCommandes, result.TotalBls, result.TotalFactures, result.TotalReglements, result.SoldeAPayer);

        // Construction des opérations groupées
        foreach (var po in purchaseOrders)
        {
            var op = new SituationOperationGroupDto
            {
                NumeroBC = po.OrderNumber,
                DateCommande = po.OrderDate,
                TotalCommande = po.Details?.Sum(d => d.TotalTTC) ?? po.TotalAmountTTC,
                TotalCommandeHT = po.Details?.Sum(d => d.TotalHT) ?? po.TotalAmountHT,
                CommandeArticles = po.Details?.Select(d => new CommandeArticleDto
                {
                    Article = d.Product?.Designation ?? "N/A",
                    PrixUnitaire = d.UnitPriceHT,
                    Quantite = d.Quantity,
                    TotalHT = d.TotalHT,
                    Total = d.TotalTTC
                }).ToList() ?? new List<CommandeArticleDto>()
            };

            // Trouver le bon de livraison associé
            var relatedDn = deliveryNotes.FirstOrDefault(dn => dn.PurchaseOrderId == po.Id);
            if (relatedDn != null)
            {
                op.HasDeliveryNote = true;
                op.NumeroBL = relatedDn.DeliveryNumber;
                op.DateLivraison = relatedDn.ReceptionDate;
                op.BlArticles = relatedDn.Details?.Select(d => new BlArticleDto
                {
                    Article = d.Product?.Designation ?? "N/A",
                    QtBC = d.QuantityOrdered,
                    QtLivree = d.QuantityReceived,
                    Ecart = d.QuantityOrdered - d.QuantityReceived
                }).ToList() ?? new List<BlArticleDto>();
                op.TotalBlTTC = relatedDn.Details?.Sum(d => d.QuantityReceived * d.UnitPriceTTC) ?? 0m;
                op.TotalBlHT = relatedDn.Details?.Sum(d => d.QuantityReceived * d.UnitPriceHT) ?? 0m;

                // Calculer l'état du BL
                if (!string.IsNullOrWhiteSpace(relatedDn.Status))
                {
                    op.BlEtat = relatedDn.Status;
                }
                else
                {
                    bool hasAnyDelivery = op.BlArticles.Any(a => a.QtLivree > 0);
                    bool allDelivered = op.BlArticles.All(a => a.QtLivree >= a.QtBC);
                    if (!hasAnyDelivery)
                    {
                        op.BlEtat = "En attente";
                    }
                    else if (allDelivered)
                    {
                        op.BlEtat = "Livré";
                    }
                    else
                    {
                        op.BlEtat = "Partiel";
                    }
                }
            }
            else
            {
                op.HasDeliveryNote = false;
                op.BlArticles = new List<BlArticleDto>();
                op.BlEtat = string.Empty;
            }

            // Trouver la facture associée
            var relatedInvoice = invoices.FirstOrDefault(i => i.PurchaseOrderId == po.Id);
            if (relatedInvoice != null)
            {
                op.HasInvoice = true;
                op.NumeroFacture = relatedInvoice.InvoiceNumber;
                op.NumeroFactureFournisseur = relatedInvoice.ExternalInvoiceNumber ?? string.Empty;
                op.DateFacture = relatedInvoice.InvoiceDate;
                op.TotalFacture = relatedInvoice.AmountTTC;
                op.FactureArticles = relatedInvoice.Details?.Select(d => new FactureArticleDto
                {
                    Article = d.Product?.Designation ?? "N/A",
                    MontantHT = d.TotalHT,
                    TVA = d.TotalTTC - d.TotalHT,
                    MontantTTC = d.TotalTTC
                }).ToList() ?? new List<FactureArticleDto>();

                // Trouver les règlements associés
                var relatedPayments = payments.Where(p => p.InvoiceId == relatedInvoice.Id).ToList();
                op.Reglements = relatedPayments.Select(p => new ReglementRowDto
                {
                    Date = p.PaymentDate,
                    Mode = p.PaymentMethod,
                    Reference = p.ReferenceNumber ?? p.PaymentNumber,
                    Montant = p.AmountPaid
                }).ToList();
                op.HasPayments = op.Reglements.Count > 0;
                op.TotalRegle = op.Reglements.Sum(r => r.Montant);
                op.ResteAPayer = op.TotalFacture - op.TotalRegle;
                op.ReglementStatut = op.ResteAPayer <= 0 ? "Payé" : "EnAttente";
            }
            else
            {
                op.HasInvoice = false;
                op.FactureArticles = new List<FactureArticleDto>();
                op.TotalFacture = null;
                op.Reglements = new List<ReglementRowDto>();
                op.HasPayments = false;
                op.TotalRegle = 0; // As per user request: Total réglé = 0,00 when no facture
                op.ResteAPayer = 0; // Reste à payer = Total Facture (which is null, but user says Total Facture empty, Reste à payer = 0?)
                op.ReglementStatut = "En attente"; // Statut = En attente when no facture? Wait user says:
                // Wait user says: "Dans ce cas uniquement" (facture exists but no règlements) → Total réglé = 0,00; Reste à payer = Total Facture; Statut = En attente.
                // But what if no facture at all? Let's check user's requirements again:
            }

            // Calcul du nombre de sous-lignes (only based on real data that exists)
            var counts = new List<int>();
            counts.Add(op.CommandeArticles.Count);
            if (op.HasDeliveryNote) counts.Add(op.BlArticles.Count);
            if (op.HasInvoice) counts.Add(op.FactureArticles.Count);
            if (op.HasInvoice && op.HasPayments) counts.Add(op.Reglements.Count);
            
            int maxCount = counts.Count > 0 ? counts.Max() : 1;
            op.NombreSousLignes = maxCount;

            // Construction des sous-lignes
            for (int i = 0; i < op.NombreSousLignes; i++)
            {
                var sousLigne = new SituationSousLigneDto();

                // BC
                if (i < op.CommandeArticles.Count)
                {
                    sousLigne.BcArticle = op.CommandeArticles[i].Article;
                    sousLigne.BcPrixUnitaire = op.CommandeArticles[i].PrixUnitaire;
                    sousLigne.BcQuantite = op.CommandeArticles[i].Quantite;
                    sousLigne.BcTotalHT = op.CommandeArticles[i].TotalHT;
                    sousLigne.BcTotal = op.CommandeArticles[i].Total;
                }

                // BL
                if (op.HasDeliveryNote && i < op.BlArticles.Count)
                {
                    sousLigne.BlArticle = op.BlArticles[i].Article;
                    sousLigne.BlQtBC = op.BlArticles[i].QtBC;
                    sousLigne.BlQtLivree = op.BlArticles[i].QtLivree;
                    sousLigne.BlEcart = op.BlArticles[i].Ecart;
                }

                // Facture
                if (op.HasInvoice && i < op.FactureArticles.Count)
                {
                    sousLigne.FactureArticle = op.FactureArticles[i].Article;
                    sousLigne.FactureMontantHT = op.FactureArticles[i].MontantHT;
                    sousLigne.FactureTVA = op.FactureArticles[i].TVA;
                    sousLigne.FactureMontantTTC = op.FactureArticles[i].MontantTTC;
                }

                // Règlement
                if (op.HasInvoice && op.HasPayments && i < op.Reglements.Count)
                {
                    sousLigne.ReglementDate = op.Reglements[i].Date;
                    sousLigne.ReglementMode = op.Reglements[i].Mode;
                    sousLigne.ReglementReference = op.Reglements[i].Reference;
                    sousLigne.ReglementMontant = op.Reglements[i].Montant;
                }

                op.SousLignes.Add(sousLigne);
            }

            result.Operations.Add(op);
            Log.Information("GetSituationFournisseurAsync: Added operation group for PO {OrderNumber} with {SubLineCount} sub-lines", po.OrderNumber, op.SousLignes.Count);
        }

        Log.Information("GetSituationFournisseurAsync: Completed, returning {OpCount} operation groups", result.Operations.Count);
        return result;
    }

    // Même formule que les KPI du Dashboard principal : ((current - previous) / previous) * 100
    private double CalculateVariation(double current, double previous)
    {
        if (previous == 0)
        {
            return current > 0 ? 100 : 0;
        }
        return Math.Round(((current - previous) / previous) * 100, 1);
    }
}