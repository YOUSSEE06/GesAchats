namespace GesAchats.Core.Security;

public enum AccessModule
{
    Dashboard,
    Besoins,
    Devis,
    BonsCommande,
    BonsLivraison,
    Factures,
    Paiements,
    Stock,
    StockAnalysis,
    OrderPlanning,
    MarketReport,
    Fournisseurs,
    Produits,
    Utilisateurs,
    Roles,
    AuditLogs,
    Settings
}

public enum AccessLevel
{
    None,
    Read,
    Write,
    Admin
}

public class ModulePermission
{
    public AccessModule Module { get; set; }
    public AccessLevel Level { get; set; }
}
