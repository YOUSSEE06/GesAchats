using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Admin;

namespace GesAchats.WPF.Views.Admin.DeliveryNotes;

public partial class AdminDeliveryNotesPage : Page
{
    public AdminDeliveryNotesPage(AdminDeliveryNotesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
