using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Admin;

namespace GesAchats.WPF.Views.Admin.Stock;

public partial class AdminStockPage : Page
{
    public AdminStockPage(AdminStockViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
