using System;
using System.Collections.Generic;

namespace GesAchats.Core.DTOs;

public class ComptableDashboardDto
{
    // 1. KPI Principaux
    public KpiItemDto TotalInvoices { get; set; } = new();
    public KpiItemDto PendingInvoices { get; set; } = new();
    public KpiItemDto ValidatedInvoices { get; set; } = new();
    public KpiItemDto PartiallyPaidInvoices { get; set; } = new();
    public KpiItemDto TotalBl { get; set; } = new();
    public KpiItemDto ValidatedBl { get; set; } = new();
    public KpiItemDto PendingBl { get; set; } = new();

    // 3. Soldes & Règlements
    public decimal MontantTotalTTC { get; set; } // Sum of all invoice TTC
    public decimal TotalPayments { get; set; } // Sum of all payments made
    public decimal TotalBalances { get; set; }
    public decimal RemainingTtcBalance { get; set; } // MontantTotalTTC - TotalPayments
    public double PaymentRatePercentage { get; set; } // (TotalPayments / MontantTotalTTC)*100

    // 4. Graphiques Centraux (Données)
    public List<DistributionDto> SupplierDistribution { get; set; } = new();
    public List<DistributionDto> InvoiceStatusDistribution { get; set; } = new();
    public List<DistributionDto> BlStatusDistribution { get; set; } = new();

    // 5. Évolution des paiements et soldes
    public List<TimeSeriesDataDto> PaymentEvolution { get; set; } = new();
    public List<TimeSeriesDataDto> BalanceEvolution { get; set; } = new();

    // 6. Modes de paiement
    public List<DistributionDto> PaymentModeDistribution { get; set; } = new();

    // 7. Dernières Opérations
    public List<ComptableOperationDto> RecentOperations { get; set; } = new();
}

public class KpiItemDto
{
    public double Value { get; set; }
    public double VariationPercentage { get; set; }
}

public class DistributionDto
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

public class TimeSeriesDataDto
{
    public DateTime Date { get; set; }
    public double Value { get; set; }
}

public class ComptableOperationDto
{
    public DateTime Date { get; set; }
    public string Module { get; set; } = string.Empty; // Facture, BL, Règlement
    public string Supplier { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public decimal AmountTtc { get; set; }
    public string Status { get; set; } = string.Empty;
}
