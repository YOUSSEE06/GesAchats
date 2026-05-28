using System.Windows.Controls;
using System.Windows.Input;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Fournisseurs;

public partial class GestionFournisseursPage : Page
{
    public GestionFournisseursPage(SupplierManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void SuppliersDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is SupplierManagementViewModel viewModel && viewModel.EditSupplierCommand.CanExecute(null))
        {
            viewModel.EditSupplierCommand.Execute(null);
        }
    }
}
