using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Analyses;

public partial class AnalyseComparativePage : Page
{
    public AnalyseComparativePage(ComparativeAnalysisViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
