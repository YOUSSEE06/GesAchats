using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Comptable;

namespace GesAchats.WPF.Views.Comptable.Factures;

public partial class InvoiceFormPage : Page
{
    public InvoiceFormPage(InvoiceFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
