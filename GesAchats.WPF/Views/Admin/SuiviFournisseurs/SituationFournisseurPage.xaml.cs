using GesAchats.WPF.ViewModels.Admin;
using System.Windows.Controls;

namespace GesAchats.WPF.Views.Admin.SuiviFournisseurs;

public partial class SituationFournisseurPage : Page
{
    public SituationFournisseurPage(SituationFournisseurViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
