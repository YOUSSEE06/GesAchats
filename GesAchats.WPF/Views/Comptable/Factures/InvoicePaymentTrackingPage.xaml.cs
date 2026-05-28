using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Comptable;

namespace GesAchats.WPF.Views.Comptable.Factures;

public partial class InvoicePaymentTrackingPage : Page
{
    public InvoicePaymentTrackingPage(InvoicePaymentTrackingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
