using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Admin;

namespace GesAchats.WPF.Views.Admin.NeedsHistory;

public partial class AdminNeedsHistoryPage : Page
{
    public AdminNeedsHistoryPage(AdminNeedsHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
