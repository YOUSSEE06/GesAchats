using GesAchats.WPF.ViewModels.Admin;
using System.Windows.Controls;

namespace GesAchats.WPF.Views.Admin.Employees;

public partial class EmployeeManagementPage : Page
{
    public EmployeeManagementPage(EmployeeManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.LoadEmployeesCommand.Execute(null);
    }
}
