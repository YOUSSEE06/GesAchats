using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Magasinier;

namespace GesAchats.WPF.Views.Magasinier.Orders;

public partial class MagasinierOrdersPage : Page
{
    public MagasinierOrdersPage(MagasinierOrdersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
