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
        
        // Bind PasswordBox to ViewModel
        AdminPasswordBox.PasswordChanged += (sender, args) => 
        {
            if (DataContext is EmployeeManagementViewModel vm)
            {
                vm.CurrentAdminPassword = AdminPasswordBox.Password;
            }
        };

        // Bind new password PasswordBoxes to ViewModel
        AdminNewPasswordBox.PasswordChanged += (sender, args) =>
        {
            if (DataContext is EmployeeManagementViewModel vm)
            {
                vm.NewAdminPassword = AdminNewPasswordBox.Password;
            }
        };

        AdminConfirmPasswordBox.PasswordChanged += (sender, args) =>
        {
            if (DataContext is EmployeeManagementViewModel vm)
            {
                vm.ConfirmAdminPassword = AdminConfirmPasswordBox.Password;
            }
        };
    }
}
