namespace GesAchats.Core.Services;

// DTOs for Dashboard
public class DashboardOperationDto
{
    public string Reference { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Fournisseur { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Statut { get; set; } = string.Empty;
}

public class MonthlyPurchaseData
{
    public string Month { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Total { get; set; }
    public decimal Average { get; set; }
}

public class RecentPurchaseOrderData
{
    public string Reference { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class SupplierExpenseData
{
    public string Name { get; set; } = string.Empty;
    public decimal TotalExpense { get; set; }
    public double PercentageOfTotal { get; set; }
}

public class ProductPriceAnalysisData
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public double PriceChangePercentage { get; set; }
    public string EvolutionText { get; set; } = string.Empty;
}
