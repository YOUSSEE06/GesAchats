using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Comptable;
using GesAchats.WPF.Services;

namespace GesAchats.WPF.Views.Comptable.Factures;

public partial class InvoiceFormPage : Page, INavigatable
{
    public InvoiceFormPage(InvoiceFormViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    public void OnNavigatedTo(object parameter)
    {
        if (DataContext is INavigatable navigatable)
        {
            navigatable.OnNavigatedTo(parameter);
        }
    }
}
