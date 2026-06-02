namespace GesAchats.Core.DTOs;

// Admin Dashboard main DTO
public class AdminDashboardDto
{
    // 1. Top Row of KPIs (6 items)
    public KpiItemDto Produits { get; set; } = new();
    public KpiItemDto StockTotal { get; set; } = new();
    public KpiItemDto Besoins { get; set; } = new();
    public KpiItemDto SortiesDeStock { get; set; } = new();
    public KpiItemDto BonsDeLivraison { get; set; } = new();
    public KpiItemDto BonsDeCommande { get; set; } = new();
    
    // 2. Second Row of KPIs (6 items)
    public KpiItemDto FacturesTotal { get; set; } = new();
    public KpiItemDto FacturesPayees { get; set; } = new();
    public KpiItemDto FacturesEnAttente { get; set; } = new();
    public KpiItemDto TotalRegle { get; set; } = new();
    public KpiItemDto SoldeTotal { get; set; } = new();
    public KpiItemDto FournisseursActifs { get; set; } = new();
    
    // 3. Charts Data
    public List<DistributionDto> OperationDistribution { get; set; } = new();
    public List<DistributionDto> StockStatus { get; set; } = new();
    public List<DistributionDto> InvoiceStatus { get; set; } = new();
    public List<TimeSeriesDataDto> MonthlyIncoming { get; set; } = new();
    public List<TimeSeriesDataDto> MonthlyOutgoing { get; set; } = new();
    public List<DistributionDto> PaymentMethodDistribution { get; set; } = new();
    
    // 4. Recent Activities
    public List<AdminRecentActivityDto> RecentActivities { get; set; } = new();
}

public class AdminRecentActivityDto
{
    public DateTime Date { get; set; }
    public string Module { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
