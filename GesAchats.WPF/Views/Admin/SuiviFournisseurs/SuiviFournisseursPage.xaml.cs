using GesAchats.WPF.ViewModels.Admin;
using System.Windows.Controls;

namespace GesAchats.WPF.Views.Admin.SuiviFournisseurs;

public partial class SuiviFournisseursPage : Page
{
    public SuiviFournisseursPage(SuiviFournisseursViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
