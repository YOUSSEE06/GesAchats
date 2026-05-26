using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Comptable;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.Views.Comptable.Factures;

public partial class PaymentFormPage : Page, INavigatable
{
    private readonly PaymentFormViewModel _viewModel;

    public PaymentFormPage(PaymentFormViewModel viewModel)
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
