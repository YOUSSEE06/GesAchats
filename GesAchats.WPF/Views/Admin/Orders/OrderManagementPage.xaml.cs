using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Admin;

namespace GesAchats.WPF.Views.Admin.Orders;

public partial class OrderManagementPage : Page
{
    public OrderManagementPage(AdminOrdersViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
