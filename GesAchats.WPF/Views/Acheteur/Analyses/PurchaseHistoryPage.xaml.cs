using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Analyses;

public partial class PurchaseHistoryPage : Page
{
    public PurchaseHistoryPage(PurchaseHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
