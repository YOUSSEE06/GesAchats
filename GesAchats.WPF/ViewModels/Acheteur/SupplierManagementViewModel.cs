using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using GesAchats.WPF.Views.Acheteur.Fournisseurs;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class SupplierManagementViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;
    private Supplier? _selectedSupplier;

    public ObservableCollection<Supplier> Suppliers { get; } = new();

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetProperty(ref _selectedSupplier, value);
    }

    public ICommand AddSupplierCommand { get; }
    public ICommand EditSupplierCommand { get; }
    public ICommand RefreshCommand { get; }

    public SupplierManagementViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "Gestion des Fournisseurs";

        AddSupplierCommand = new RelayCommand(async _ => await OpenAddSupplierDialog());
        EditSupplierCommand = new RelayCommand(async _ => await OpenEditSupplierDialog(), _ => SelectedSupplier != null);
        RefreshCommand = new RelayCommand(async _ => await LoadSuppliers());

        _ = LoadSuppliers();
    }

    private async Task LoadSuppliers()
    {
        IsBusy = true;
        try
        {
            var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
            Suppliers.Clear();
            foreach (var s in suppliers.OrderBy(x => x.CompanyName))
            {
                Suppliers.Add(s);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task OpenAddSupplierDialog()
    {
        var viewModel = ActivatorUtilities.CreateInstance<SupplierDialogViewModel>(_serviceProvider);
        var dialog = new SupplierDialog(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadSuppliers();
        }
    }

    private async Task OpenEditSupplierDialog()
    {
        if (SelectedSupplier == null) return;

        var viewModel = ActivatorUtilities.CreateInstance<SupplierDialogViewModel>(_serviceProvider, SelectedSupplier);
        var dialog = new SupplierDialog(viewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadSuppliers();
            SelectedSupplier = null;
        }
    }
}
