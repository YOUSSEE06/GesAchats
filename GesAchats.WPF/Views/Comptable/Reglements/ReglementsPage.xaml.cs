using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Comptable;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.Views.Comptable.Reglements;

public partial class ReglementsPage : Page, INavigatable
{
    private readonly PaymentHistoryViewModel _viewModel;

    public ReglementsPage(PaymentHistoryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public void OnNavigatedTo(object parameter)
    {
        _viewModel.OnNavigatedTo(parameter);
    }
}
