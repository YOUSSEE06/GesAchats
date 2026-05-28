using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using GesAchats.Core.Entities;

namespace GesAchats.WPF.ViewModels.Comptable;

public class InvoiceWithPaymentsViewModel : INotifyPropertyChanged
{
    public Invoice Invoice { get; set; }
    
    public ObservableCollection<Payment> Payments { get; set; } = new();

    public InvoiceWithPaymentsViewModel(Invoice invoice)
    {
        Invoice = invoice;
    }

    public decimal TotalPayments => Payments.Sum(p => p.AmountPaid);
    public decimal Balance => Invoice.AmountTTC - TotalPayments;
    
    public string StatusCalculated
    {
        get
        {
            if (TotalPayments <= 0)
                return "En attente";
            if (TotalPayments < Invoice.AmountTTC)
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
            if (TotalPayments < Invoice.AmountTTC)
                return "#FF9800"; // Orange
            return "#4CAF50"; // Vert
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void RefreshCalculations()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TotalPayments)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Balance)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusCalculated)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusColor)));
    }
}
