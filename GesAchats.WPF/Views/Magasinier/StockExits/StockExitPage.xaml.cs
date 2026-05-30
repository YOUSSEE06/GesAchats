using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Magasinier;

namespace GesAchats.WPF.Views.Magasinier.StockExits;

/// <summary>
/// Logique d'interaction pour StockExitPage.xaml
/// </summary>
public partial class StockExitPage : Page
{
    public StockExitPage(StockExitViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
