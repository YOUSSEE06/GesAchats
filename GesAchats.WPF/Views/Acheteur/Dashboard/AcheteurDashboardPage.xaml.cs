using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Dashboard;

public partial class AcheteurDashboardPage : Page
{
    public AcheteurDashboardPage(AcheteurDashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
