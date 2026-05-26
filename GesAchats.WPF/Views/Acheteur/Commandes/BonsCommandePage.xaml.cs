using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Commandes;

public partial class BonsCommandePage : Page
{
    public BonsCommandePage(BonsCommandeViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
