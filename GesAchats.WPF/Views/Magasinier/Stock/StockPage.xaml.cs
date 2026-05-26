using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using GesAchats.WPF.ViewModels.Magasinier;
using GesAchats.WPF.Views.Magasinier.NeedsList;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.Views.Magasinier.Stock;

public partial class StockPage : Page
{
    private readonly IServiceProvider _serviceProvider;

    public StockPage(StockAnalysisViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        DataContext = viewModel;

        // Gestion de l'ouverture de la popup d'ajout de produit
        viewModel.AddProductCommand = new RelayCommand(_ => ShowAddProductDialog());
        viewModel.OnCreateNeedRequested = () => this.NavigationService.Navigate(_serviceProvider.GetRequiredService<NeedsListPage>());
    }

    private void ShowAddProductDialog()
    {
        var dialogViewModel = _serviceProvider.GetRequiredService<AddProductViewModel>();
        var dialog = new AddProductDialog(dialogViewModel)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            ((StockAnalysisViewModel)DataContext).RefreshCommand.Execute(null);
        }
    }
}
