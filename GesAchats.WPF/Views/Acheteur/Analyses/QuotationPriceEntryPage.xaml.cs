using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Analyses;

public partial class QuotationPriceEntryPage : Page
{
    public QuotationPriceEntryPage(QuotationPriceEntryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
