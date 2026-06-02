using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Comptable;

namespace GesAchats.WPF.Views.Comptable.Dashboard;

public partial class ComptableDashboardPage : Page
{
    public ComptableDashboardPage(ComptableDashboardViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
