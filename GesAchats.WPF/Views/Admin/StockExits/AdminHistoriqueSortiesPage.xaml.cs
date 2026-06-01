using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Admin;

namespace GesAchats.WPF.Views.Admin.StockExits;

public partial class AdminHistoriqueSortiesPage : Page
{
    public AdminHistoriqueSortiesPage(AdminHistoriqueSortiesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
