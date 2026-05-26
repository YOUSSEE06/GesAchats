using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Magasinier;

namespace GesAchats.WPF.Views.Magasinier.NeedsHistory;

public partial class NeedsHistoryPage : Page
{
    public NeedsHistoryPage(NeedsHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
