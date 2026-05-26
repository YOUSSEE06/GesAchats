using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Besoins;

public partial class BesoinsMagasinPage : Page
{
    public BesoinsMagasinPage(ReceivedNeedsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
