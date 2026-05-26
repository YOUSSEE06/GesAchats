using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Commandes;

public partial class OrderTrackingPage : Page
{
    public OrderTrackingPage(OrderTrackingViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
