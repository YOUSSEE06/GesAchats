using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Analyses;

public partial class ProductStatsPage : Page
{
    public ProductStatsPage(ProductStatsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
