using System.Windows;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Fournisseurs;

public partial class SupplierDialog : Window
{
    public SupplierDialog(SupplierDialogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.OnResult += result => {
            DialogResult = result != null;
            Close();
        };

        SaveButton.Content = viewModel.IsEditMode ? "Modifier" : "Enregistrer";
    }
}
