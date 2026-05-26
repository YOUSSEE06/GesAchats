using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Fournisseurs;

public partial class GestionFournisseursPage : Page
{
    public GestionFournisseursPage(SupplierManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
