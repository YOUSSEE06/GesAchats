using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using GesAchats.Core.Entities;
using GesAchats.Core.DTOs;

namespace GesAchats.WPF.ViewModels.Comptable;

public class InvoiceWithPaymentsViewModel : INotifyPropertyChanged
{
    // Original constructor for Invoice entity
    public Invoice? Invoice { get; set; }
    
    public ObservableCollection<Payment> Payments { get; set; } = new();

    public InvoiceWithPaymentsViewModel(Invoice invoice)
    {
        Invoice = invoice;
    }

    // New constructor for InvoiceDto
    public InvoiceDto? InvoiceDto { get; set; }
    public ObservableCollection<PaymentListDto> PaymentDtos { get; set; } = new();

    public InvoiceWithPaymentsViewModel(InvoiceDto invoiceDto)
    {
        InvoiceDto = invoiceDto;
        if (invoiceDto.Payments != null)
        {
            foreach (var p in invoiceDto.Payments)
            {
                PaymentDtos.Add(p);
            }
        }
    }

    // Properties that work for both cases
    public string InvoiceNumber
    {
        get
        {
            if (Invoice != null)
                return Invoice.InvoiceNumber;
            if (InvoiceDto != null)
                return InvoiceDto.InvoiceNumber;
            return string.Empty;
        }
    }

    public string? ExternalInvoiceNumber
    {
        get
        {
            if (Invoice != null)
                return Invoice.ExternalInvoiceNumber;
            if (InvoiceDto != null)
                return InvoiceDto.ExternalInvoiceNumber;
            return null;
        }
    }

    public DateTime InvoiceDate
    {
        get
        {
            if (Invoice != null)
                return Invoice.InvoiceDate;
            if (InvoiceDto != null)
                return InvoiceDto.InvoiceDate;
            return DateTime.Now;
        }
    }

    public int SupplierId
    {
        get
        {
            if (Invoice != null)
                return Invoice.SupplierId;
            if (InvoiceDto != null)
                return InvoiceDto.SupplierId;
            return 0;
        }
    }

    public string SupplierName
    {
        get
        {
            if (Invoice != null)
                return Invoice.Supplier?.CompanyName ?? "Inconnu";
            if (InvoiceDto != null)
                return InvoiceDto.SupplierName ?? "Inconnu";
            return "Inconnu";
        }
    }

    public int? PurchaseOrderId
    {
        get
        {
            if (Invoice != null)
                return Invoice.PurchaseOrderId;
            if (InvoiceDto != null)
                return InvoiceDto.PurchaseOrderId;
            return null;
        }
    }

    public string? PurchaseOrderNumber
    {
        get
        {
            if (Invoice != null)
                return Invoice.PurchaseOrder?.OrderNumber;
            if (InvoiceDto != null)
                return InvoiceDto.PurchaseOrderNumber;
            return null;
        }
    }

    public int? DeliveryNoteId
    {
        get
        {
            if (Invoice != null)
                return Invoice.DeliveryNoteId;
            if (InvoiceDto != null)
                return InvoiceDto.DeliveryNoteId;
            return null;
        }
    }

    public string? DeliveryNoteNumber
    {
        get
        {
            if (Invoice != null)
                return Invoice.DeliveryNote?.DeliveryNumber;
            if (InvoiceDto != null)
                return InvoiceDto.DeliveryNoteNumber;
            return null;
        }
    }

    public decimal AmountHT
    {
        get
        {
            if (Invoice != null)
                return Invoice.AmountHT;
            if (InvoiceDto != null)
                return InvoiceDto.AmountHT;
            return 0;
        }
    }

    public decimal TaxAmount
    {
        get
        {
            if (Invoice != null)
                return Invoice.TaxAmount;
            if (InvoiceDto != null)
                return InvoiceDto.TaxAmount;
            return 0;
        }
    }

    public decimal AmountTTC
    {
        get
        {
            if (Invoice != null)
                return Invoice.AmountTTC;
            if (InvoiceDto != null)
                return InvoiceDto.AmountTTC;
            return 0;
        }
    }

    public string? FilePath
    {
        get
        {
            if (Invoice != null)
                return Invoice.FilePath;
            if (InvoiceDto != null)
                return InvoiceDto.FilePath;
            return null;
        }
    }

    public int Id
    {
        get
        {
            if (Invoice != null)
                return Invoice.Id;
            if (InvoiceDto != null)
                return InvoiceDto.Id;
            return 0;
        }
    }

    public decimal TotalPayments
    {
        get
        {
            if (Invoice != null)
                return Payments.Sum(p => p.AmountPaid);
            return PaymentDtos.Sum(p => p.AmountPaid);
        }
    }

    public decimal Balance => AmountTTC - TotalPayments;
    
    public string StatusCalculated
    {
        get
        {
            if (TotalPayments <= 0)
                return "En attente";
            if (TotalPayments < AmountTTC)
                return "Partiellement payée";
            return "Payée";
        }
    }

    public string StatusColor
    {
        get
        {
            if (TotalPayments <= 0)
                return "#F44336"; // Rouge
            if (TotalPayments < AmountTTC)
                return "#FF9800"; // Orange
            return "#4CAF50"; // Vert
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void RefreshCalculations()
    {
        OnPropertyChanged(nameof(TotalPayments));
        OnPropertyChanged(nameof(Balance));
        OnPropertyChanged(nameof(StatusCalculated));
        OnPropertyChanged(nameof(StatusColor));
    }
}
