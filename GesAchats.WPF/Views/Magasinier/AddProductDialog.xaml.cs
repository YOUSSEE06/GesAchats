using System.Windows;
using GesAchats.WPF.ViewModels.Magasinier;

namespace GesAchats.WPF.Views.Magasinier;

public partial class AddProductDialog : Window
{
    public AddProductDialog(AddProductViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.OnResult += result => {
            DialogResult = result != null;
            Close();
        };
    }
}
