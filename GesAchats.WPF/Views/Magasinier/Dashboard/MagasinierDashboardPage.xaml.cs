using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Magasinier;

namespace GesAchats.WPF.Views.Magasinier.Dashboard;

public partial class MagasinierDashboardPage : Page
{
    public MagasinierDashboardPage(MagasinierDashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
