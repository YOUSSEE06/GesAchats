using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Magasinier;

namespace GesAchats.WPF.Views.Magasinier.Livraisons;

public partial class LivraisonsPage : Page
{
    public LivraisonsPage(DeliveryNotesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
