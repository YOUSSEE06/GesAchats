using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Comptable;

namespace GesAchats.WPF.Views.Comptable.Reglements;

public partial class ReglementsPage : Page
{
    public ReglementsPage(PaymentHistoryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
