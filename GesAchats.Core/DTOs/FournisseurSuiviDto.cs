namespace GesAchats.Core.DTOs;

public class NeedHistoriqueDto
{
    public int Id { get; set; }
    public string NumeroBesoin { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public string Demandeur { get; set; } = string.Empty;
    public int NombreArticles { get; set; }
    public string Statut { get; set; } = string.Empty;
}

public class FournisseurSuiviDto
{
    public int Id { get; set; }
    public string NomFournisseur { get; set; } = string.Empty;
    public string? NomContact { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Ville { get; set; }
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class SituationFournisseurDto
{
    public int FournisseurId { get; set; }
    public string NomFournisseur { get; set; } = string.Empty;
    
    // KPIs
    public int TotalCommandes { get; set; }
    public int TotalBls { get; set; }
    public int TotalFactures { get; set; }
    public decimal TotalReglements { get; set; }
    public decimal SoldeAPayer { get; set; }
    
    // Details (grouped by operation for the new single table)
    public List<SituationOperationGroupDto> Operations { get; set; } = new();

    // Legacy details for compatibility
    public List<CommandeDetailDto> Commandes { get; set; } = new();
    public List<BlDetailDto> BonsLivraison { get; set; } = new();
    public List<FactureDetailDto> Factures { get; set; } = new();
    public List<ReglementDetailDto> Reglements { get; set; } = new();
}

// New DTO for grouped operation
public class SituationOperationGroupDto
{
    // BC
    public string NumeroBC { get; set; } = string.Empty;
    public List<CommandeArticleDto> CommandeArticles { get; set; } = new();
    public decimal TotalCommande { get; set; }
    
    // BL
    public string NumeroBL { get; set; } = string.Empty;
    public List<BlArticleDto> BlArticles { get; set; } = new();
    public string BlEtat { get; set; } = string.Empty;
    public bool HasDeliveryNote { get; set; }
    
    // Facture
    public string NumeroFacture { get; set; } = string.Empty;
    public List<FactureArticleDto> FactureArticles { get; set; } = new();
    public decimal? TotalFacture { get; set; }
    public bool HasInvoice { get; set; }
    
    // Règlements
    public List<ReglementRowDto> Reglements { get; set; } = new();
    public decimal? TotalRegle { get; set; }
    public decimal? ResteAPayer { get; set; }
    public string ReglementStatut { get; set; } = string.Empty;
    public bool HasPayments { get; set; }
    
    // Number of sublines for this operation
    public int NombreSousLignes { get; set; }
    public List<SituationSousLigneDto> SousLignes { get; set; } = new();
}

public class CommandeArticleDto
{
    public string Article { get; set; } = string.Empty;
    public decimal PrixUnitaire { get; set; }
    public decimal Quantite { get; set; }
    public decimal Total { get; set; }
}

public class BlArticleDto
{
    public string Article { get; set; } = string.Empty;
    public decimal QtBC { get; set; }
    public decimal QtLivree { get; set; }
    public decimal Ecart { get; set; }
}

public class FactureArticleDto
{
    public string Article { get; set; } = string.Empty;
    public decimal MontantHT { get; set; }
    public decimal TVA { get; set; }
    public decimal MontantTTC { get; set; }
}

public class ReglementRowDto
{
    public DateTime Date { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Montant { get; set; }
}

// DTO for a single sub-line
public class SituationSousLigneDto
{
    // BC
    public string? BcArticle { get; set; }
    public decimal? BcPrixUnitaire { get; set; }
    public decimal? BcQuantite { get; set; }
    public decimal? BcTotal { get; set; }
    
    // BL
    public string? BlArticle { get; set; }
    public decimal? BlQtBC { get; set; }
    public decimal? BlQtLivree { get; set; }
    public decimal? BlEcart { get; set; }
    
    // Facture
    public string? FactureArticle { get; set; }
    public decimal? FactureMontantHT { get; set; }
    public decimal? FactureTVA { get; set; }
    public decimal? FactureMontantTTC { get; set; }
    
    // Règlement
    public DateTime? ReglementDate { get; set; }
    public string? ReglementMode { get; set; }
    public string? ReglementReference { get; set; }
    public decimal? ReglementMontant { get; set; }
}

public class CommandeDetailDto
{
    public string NumeroBC { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public decimal PrixUnitaire { get; set; }
    public decimal Quantite { get; set; }
    public decimal TotalCommande { get; set; }
}

public class BlDetailDto
{
    public string NumeroBL { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public decimal QtBC { get; set; }
    public decimal QtLivree { get; set; }
    public decimal Ecart { get; set; }
    public string Etat { get; set; } = string.Empty;
}

public class FactureDetailDto
{
    public string NumeroFacture { get; set; } = string.Empty;
    public string Article { get; set; } = string.Empty;
    public decimal MontantHT { get; set; }
    public decimal TVA { get; set; }
    public decimal MontantTTC { get; set; }
    public decimal TotalFacture { get; set; }
}

public class ReglementDetailDto
{
    public string Mode { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal Montant { get; set; }
    public decimal TotalRegle { get; set; }
    public decimal RatioAPayer { get; set; }
    public string Statut { get; set; } = string.Empty;
}

/// <summary>
/// DTO for displaying payment list with minimal required data
/// </summary>
public class PaymentListDto
{
    public int Id { get; set; }
    public DateTime PaymentDate { get; set; }
    public int SupplierId { get; set; }
    public string SupplierCompanyName { get; set; } = string.Empty;
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? ReferenceNumber { get; set; }
    public string? ProofFilePath { get; set; }
}
