using System.Windows;
using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Magasinier;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.Views.Magasinier.NeedsList;

public partial class NeedsListPage : Page
{
    private readonly IServiceProvider _serviceProvider;

    public NeedsListPage(NeedsListViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        DataContext = viewModel;

        // Gestion de l'ouverture de la popup d'ajout de produit
        viewModel.AddProductCommand = new RelayCommand(_ => ShowAddProductDialog());
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
            // Rafraîchir la liste si nécessaire ou le VM le fait via l'event
            ((NeedsListViewModel)DataContext).RefreshCommand.Execute(null);
        }
    }
}
