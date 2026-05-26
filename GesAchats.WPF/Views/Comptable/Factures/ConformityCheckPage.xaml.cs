using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Comptable;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.Views.Comptable.Factures;

public partial class ConformityCheckPage : Page, INavigatable
{
    private readonly ConformityCheckViewModel _viewModel;

    public ConformityCheckPage(ConformityCheckViewModel viewModel)
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
