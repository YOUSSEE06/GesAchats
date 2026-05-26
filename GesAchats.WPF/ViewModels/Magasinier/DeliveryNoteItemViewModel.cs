using System.ComponentModel;
using System.Runtime.CompilerServices;
using GesAchats.Core.Entities;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Magasinier;

public class DeliveryNoteItemViewModel : BaseViewModel
{
    private decimal _quantityReceived;
    private bool _isValidated;
    private readonly Action _onQuantityChanged;

    public PurchaseOrderDetail OrderDetail { get; }

    public string ProductName => OrderDetail.Product?.Designation ?? "Inconnu";
    public decimal UnitPriceHT => OrderDetail.UnitPriceHT;
    public decimal UnitPriceTTC => OrderDetail.UnitPriceTTC;
    public decimal QuantityOrdered => OrderDetail.Quantity;

    public decimal Total => QuantityReceived * UnitPriceTTC;

    public decimal QuantityReceived
    {
        get => _quantityReceived;
        set
        {
            if (SetProperty(ref _quantityReceived, value))
            {
                OnPropertyChanged(nameof(Total));
                UpdateValidation();
                _onQuantityChanged?.Invoke();
            }
        }
    }

    public bool IsValidated
    {
        get => _isValidated;
        set => SetProperty(ref _isValidated, value);
    }

    public string? Error { get; private set; }

    public DeliveryNoteItemViewModel(PurchaseOrderDetail detail, Action onQuantityChanged)
    {
        OrderDetail = detail;
        _onQuantityChanged = onQuantityChanged;
        _quantityReceived = detail.Quantity; // Par défaut, on propose la quantité commandée
        UpdateValidation();
    }

    private void UpdateValidation()
    {
        if (QuantityReceived > QuantityOrdered)
        {
            Error = "La quantité livrée ne peut pas dépasser la quantité commandée.";
            IsValidated = false;
        }
        else
        {
            Error = null;
            IsValidated = (QuantityReceived == QuantityOrdered);
        }
        OnPropertyChanged(nameof(Error));
    }
}
