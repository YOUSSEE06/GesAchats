using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class SupplierManagementViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private Supplier? _selectedSupplier;
    private Supplier _editingSupplier = new();
    private bool _isEditMode;

    public ObservableCollection<Supplier> Suppliers { get; } = new();

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set
        {
            if (SetProperty(ref _selectedSupplier, value))
            {
                if (value != null)
                {
                    EditingSupplier = new Supplier
                    {
                        Id = value.Id,
                        CompanyName = value.CompanyName,
                        ContactName = value.ContactName,
                        Email = value.Email,
                        Phone = value.Phone,
                        Address = value.Address,
                        City = value.City,
                        Rating = value.Rating,
                        IsActive = value.IsActive
                    };
                    IsEditMode = true;
                }
                else
                {
                    ResetForm();
                }
            }
        }
    }

    public Supplier EditingSupplier
    {
        get => _editingSupplier;
        set => SetProperty(ref _editingSupplier, value);
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        set => SetProperty(ref _isEditMode, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand NewCommand { get; }
    public ICommand RefreshCommand { get; }

    public SupplierManagementViewModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Title = "Gestion des Fournisseurs";

        SaveCommand = new RelayCommand(async _ => await ExecuteSave(), _ => !string.IsNullOrEmpty(EditingSupplier.CompanyName));
        NewCommand = new RelayCommand(_ => ResetForm());
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

    private async Task ExecuteSave()
    {
        IsBusy = true;
        try
        {
            if (IsEditMode)
            {
                _unitOfWork.Suppliers.Update(EditingSupplier);
            }
            else
            {
                await _unitOfWork.Suppliers.AddAsync(EditingSupplier);
            }

            await _unitOfWork.CompleteAsync();
            await LoadSuppliers();
            ResetForm();
            System.Windows.MessageBox.Show("Fournisseur enregistré avec succès.", "Succès", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetForm()
    {
        EditingSupplier = new Supplier { IsActive = true };
        SelectedSupplier = null;
        IsEditMode = false;
    }
}
