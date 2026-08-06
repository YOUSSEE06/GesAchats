using System;
using System.Linq;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Admin;
using Serilog;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class MagasinierOrdersViewModel : AdminOrdersViewModel
{
    private int _totalOrders;
    private int _validatedOrders;
    private int _pendingOrders;
    private string _totalOrdersTrendText = string.Empty;
    private string _validatedOrdersTrendText = string.Empty;
    private string _pendingOrdersTrendText = string.Empty;
    private bool _totalOrdersIsPositiveTrend = true;
    private bool _validatedOrdersIsPositiveTrend = true;
    private bool _pendingOrdersIsPositiveTrend = true;

    public int TotalOrders
    {
        get => _totalOrders;
        set => SetProperty(ref _totalOrders, value);
    }

    public int ValidatedOrders
    {
        get => _validatedOrders;
        set => SetProperty(ref _validatedOrders, value);
    }

    public int PendingOrders
    {
        get => _pendingOrders;
        set => SetProperty(ref _pendingOrders, value);
    }

    public string TotalOrdersTrendText
    {
        get => _totalOrdersTrendText;
        set => SetProperty(ref _totalOrdersTrendText, value);
    }

    public string ValidatedOrdersTrendText
    {
        get => _validatedOrdersTrendText;
        set => SetProperty(ref _validatedOrdersTrendText, value);
    }

    public string PendingOrdersTrendText
    {
        get => _pendingOrdersTrendText;
        set => SetProperty(ref _pendingOrdersTrendText, value);
    }

    public bool TotalOrdersIsPositiveTrend
    {
        get => _totalOrdersIsPositiveTrend;
        set => SetProperty(ref _totalOrdersIsPositiveTrend, value);
    }

    public bool ValidatedOrdersIsPositiveTrend
    {
        get => _validatedOrdersIsPositiveTrend;
        set => SetProperty(ref _validatedOrdersIsPositiveTrend, value);
    }

    public bool PendingOrdersIsPositiveTrend
    {
        get => _pendingOrdersIsPositiveTrend;
        set => SetProperty(ref _pendingOrdersIsPositiveTrend, value);
    }

    public MagasinierOrdersViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider, ILogger logger)
        : base(unitOfWork, serviceProvider, logger, excludeCancelled: true)
    {
        Title = "Gestion des Bons de Commande";
        
        // Retirer le statut "Annulé" pour le magasinier
        if (Statuses.Contains(PurchaseOrderStatus.Cancelled))
        {
            Statuses.Remove(PurchaseOrderStatus.Cancelled);
        }
    }

    protected override async Task CalculateStatsAsync()
    {
        await base.CalculateStatsAsync();
        
        // Mettre à jour les statistiques spécifiques au magasinier
        TotalOrders = TotalBc;
        ValidatedOrders = BcValides;
        PendingOrders = BcEnAttente;
        TotalOrdersTrendText = TotalBcTrendText;
        ValidatedOrdersTrendText = BcValidesTrendText;
        PendingOrdersTrendText = BcEnAttenteTrendText;
        TotalOrdersIsPositiveTrend = TotalBcIsPositiveTrend;
        ValidatedOrdersIsPositiveTrend = BcValidesIsPositiveTrend;
        PendingOrdersIsPositiveTrend = BcEnAttenteIsPositiveTrend;
    }
}
