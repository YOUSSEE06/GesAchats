using System.Windows;
using GesAchats.WPF.ViewModels.Comptable;

namespace GesAchats.WPF.Views.Comptable.Factures;

public partial class InvoicePaymentDetailsPopup : Window
{
    public InvoicePaymentDetailsPopup(InvoicePaymentDetailsPopupViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        viewModel.Close += (sender, result) =>
        {
            DialogResult = result;
            Close();
        };
    }
}
