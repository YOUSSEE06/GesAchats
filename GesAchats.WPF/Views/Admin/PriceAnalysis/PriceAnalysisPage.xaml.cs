using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Admin;

namespace GesAchats.WPF.Views.Admin.PriceAnalysis;

public partial class PriceAnalysisPage : Page
{
    public PriceAnalysisPage(AdminPriceAnalysisViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
