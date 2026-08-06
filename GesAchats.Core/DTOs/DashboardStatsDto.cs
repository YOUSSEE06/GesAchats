using System;
using System.Collections.Generic;

namespace GesAchats.Core.DTOs;

public class DashboardStatsDto
{
    // KPI - Articles
    public int TotalArticles { get; set; }
    public int StockNormalCount { get; set; }
    public int StockSousMinimumCount { get; set; }
    public int StockEnRuptureCount { get; set; }

    // KPI - BL (Bons de Livraison)
    public int BlEnAttenteCount { get; set; }
    public int BlValidesCount { get; set; }

    // KPI - Besoins
    public int BesoinsEnCoursCount { get; set; }
    public int BesoinsTransmisCount { get; set; }

    // KPI - BC (Bons de Commande)
    public int BcEnAttenteCount { get; set; }
    public int BcValidesCount { get; set; }

    // Tendances (%) - période actuelle vs période précédente de même durée
    public double TotalArticlesVariation { get; set; }
    public double StockNormalVariation { get; set; }
    public double StockSousMinimumVariation { get; set; }
    public double StockEnRuptureVariation { get; set; }
    public double BlEnAttenteVariation { get; set; }
    public double BlValidesVariation { get; set; }
    public double BesoinsEnCoursVariation { get; set; }
    public double BesoinsTransmisVariation { get; set; }
    public double BcEnAttenteVariation { get; set; }
    public double BcValidesVariation { get; set; }

    // Listes pour les tableaux
    public List<CriticalProductDto> CriticalProducts { get; set; } = new();
    public List<RecentBlDto> RecentBls { get; set; } = new();
    public List<RecentNeedDto> RecentNeeds { get; set; } = new();
    public List<RecentBcDto> RecentBcs { get; set; } = new();

    // Données pour les graphiques
    public List<StockMovementDto> StockMovements { get; set; } = new();
}

public class CriticalProductDto
{
    public string Name { get; set; } = string.Empty;
    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Normal, Sous minimum, En rupture
}

public class RecentBlDto
{
    public DateTime Date { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string RelatedBc { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string FirstArticle { get; set; } = string.Empty;
}

public class RecentNeedDto
{
    public string Number { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Requester { get; set; } = string.Empty;
    public int ArticleCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string FirstArticle { get; set; } = string.Empty;
}

public class RecentBcDto
{
    public DateTime Date { get; set; }
    public string Number { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalTtc { get; set; }
    public string FirstArticle { get; set; } = string.Empty;
}

public class StockMovementDto
{
    public DateTime Date { get; set; }
    public decimal In { get; set; }
    public decimal Out { get; set; }
}
