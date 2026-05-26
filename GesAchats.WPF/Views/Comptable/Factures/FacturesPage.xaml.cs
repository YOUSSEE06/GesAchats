using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Comptable;

namespace GesAchats.WPF.Views.Comptable.Factures;

public partial class FacturesPage : Page
{
    public FacturesPage(FacturesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
