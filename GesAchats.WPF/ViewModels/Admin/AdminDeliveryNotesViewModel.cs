using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.ViewModels.Admin;

public class AdminDeliveryNoteItemViewModel : BaseViewModel
{
    public DeliveryNote DeliveryNote { get; }
    public string DateReception => DeliveryNote.ReceptionDate.ToString("dd/MM/yyyy");
    public string NumeroBL => DeliveryNote.DeliveryNumber;
    public string Fournisseur => DeliveryNote.Supplier?.CompanyName ?? "Inconnu";
    public string BCCorrespondant => DeliveryNote.PurchaseOrder?.OrderNumber ?? "Aucun";

    public AdminDeliveryNoteItemViewModel(DeliveryNote deliveryNote)
    {
        DeliveryNote = deliveryNote;
    }
}

public class AdminDeliveryNotesViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IServiceProvider _serviceProvider;

    private string _deliveryNumber = string.Empty;
    private Supplier? _selectedSupplier;
    private string _purchaseOrderNumber = string.Empty;
    private DateTime? _receptionDate = DateTime.Today;

    public string DeliveryNumber
    {
        get => _deliveryNumber;
        set => SetProperty(ref _deliveryNumber, value);
    }

    public Supplier? SelectedSupplier
    {
        get => _selectedSupplier;
        set => SetProperty(ref _selectedSupplier, value);
    }

    public string PurchaseOrderNumber
    {
        get => _purchaseOrderNumber;
        set => SetProperty(ref _purchaseOrderNumber, value);
    }

    public DateTime? ReceptionDate
    {
        get => _receptionDate;
        set => SetProperty(ref _receptionDate, value);
    }

    public ObservableCollection<Supplier> Suppliers { get; } = new();
    public ObservableCollection<AdminDeliveryNoteItemViewModel> DeliveryNotes { get; } = new();

    public ICommand RefreshCommand { get; }
    public ICommand AddDeliveryNoteCommand { get; }
    public ICommand ViewDetailsCommand { get; }
    public ICommand ViewJustificatifCommand { get; }

    public AdminDeliveryNotesViewModel(IUnitOfWork unitOfWork, IServiceProvider serviceProvider)
    {
        _unitOfWork = unitOfWork;
        _serviceProvider = serviceProvider;
        Title = "Réception des Bons de Livraison (BL)";

        RefreshCommand = new RelayCommand(async _ => await LoadData());
        AddDeliveryNoteCommand = new RelayCommand(async _ => await ExecuteAddDeliveryNote(), _ => CanAdd());
        ViewDetailsCommand = new RelayCommand(p => ExecuteViewDetails(p as AdminDeliveryNoteItemViewModel));
        ViewJustificatifCommand = new RelayCommand(p => ExecuteViewJustificatif(p as AdminDeliveryNoteItemViewModel));

        _ = LoadInitialData();
    }

    private async Task LoadInitialData()
    {
        await LoadSuppliers();
        await LoadData();
    }

    private async Task LoadSuppliers()
    {
        var suppliers = await _unitOfWork.Suppliers.GetAllAsync();
        Suppliers.Clear();
        foreach (var s in suppliers.OrderBy(x => x.CompanyName))
        {
            Suppliers.Add(s);
        }
    }

    private async Task LoadData()
    {
        IsBusy = true;
        try
        {
            var notes = await _unitOfWork.DeliveryNotes.GetAllWithDetailsAsync();
            DeliveryNotes.Clear();
            foreach (var note in notes.OrderByDescending(n => n.ReceptionDate))
            {
                DeliveryNotes.Add(new AdminDeliveryNoteItemViewModel(note));
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAdd()
    {
        return !string.IsNullOrWhiteSpace(DeliveryNumber) && 
               SelectedSupplier != null && 
               ReceptionDate.HasValue;
    }

    private async Task ExecuteAddDeliveryNote()
    {
        try
        {
            // Simple validation for PO if provided
            PurchaseOrder? po = null;
            if (!string.IsNullOrWhiteSpace(PurchaseOrderNumber))
            {
                var allPos = await _unitOfWork.PurchaseOrders.GetAllAsync();
                po = allPos.FirstOrDefault(p => p.OrderNumber == PurchaseOrderNumber);
                if (po == null)
                {
                    MessageBox.Show("Le numéro de bon de commande est introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var newNote = new DeliveryNote
            {
                DeliveryNumber = DeliveryNumber,
                SupplierId = SelectedSupplier!.Id,
                ReceptionDate = ReceptionDate!.Value,
                PurchaseOrderId = po?.Id ?? 0, // In real scenario, we might need a default PO or allow 0
                Status = "Recu",
                CreatedAt = DateTime.UtcNow,
                ReceivedById = 1 // Admin
            };

            await _unitOfWork.DeliveryNotes.AddAsync(newNote);
            await _unitOfWork.CompleteAsync();

            // Reset form
            DeliveryNumber = string.Empty;
            PurchaseOrderNumber = string.Empty;
            SelectedSupplier = null;
            ReceptionDate = DateTime.Today;

            await LoadData();
            MessageBox.Show("Bon de livraison ajouté avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erreur lors de l'ajout : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExecuteViewDetails(AdminDeliveryNoteItemViewModel? item)
    {
        if (item == null) return;
        
        var window = new Views.Admin.DeliveryNotes.DeliveryNoteDetailsWindow();
        window.DataContext = new DeliveryNoteDetailsViewModel(item.DeliveryNote, _unitOfWork);
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }

    private async void ExecuteViewJustificatif(AdminDeliveryNoteItemViewModel? item)
    {
        if (item == null) return;

        if (string.IsNullOrEmpty(item.DeliveryNote.FilePath))
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Sélectionner le justificatif (PDF, Image)",
                Filter = "Fichiers documents (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                 {
                     item.DeliveryNote.FilePath = dialog.FileName;
                     _unitOfWork.DeliveryNotes.Update(item.DeliveryNote);
                     await _unitOfWork.CompleteAsync();
                     MessageBox.Show("Justificatif associé avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                 }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = item.DeliveryNote.FilePath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Impossible d'ouvrir le fichier : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

public class DeliveryNoteDetailsViewModel : BaseViewModel
{
    private readonly DeliveryNote _deliveryNote;
    private readonly IUnitOfWork _unitOfWork;

    public DeliveryNote DeliveryNote => _deliveryNote;
    public ObservableCollection<DeliveryNoteDetail> Details { get; } = new();

    public string TitleText => $"Détails du Bon de Livraison : {_deliveryNote.DeliveryNumber}";
    public string SupplierName => _deliveryNote.Supplier?.CompanyName ?? "Inconnu";
    public string DateReception => _deliveryNote.ReceptionDate.ToString("dd/MM/yyyy");
    public string BCNumber => _deliveryNote.PurchaseOrder?.OrderNumber ?? "N/A";

    public DeliveryNoteDetailsViewModel(DeliveryNote deliveryNote, IUnitOfWork unitOfWork)
    {
        _deliveryNote = deliveryNote;
        _unitOfWork = unitOfWork;
        _ = LoadDetails();
    }

    private async Task LoadDetails()
    {
        // On recharge avec les détails si nécessaire
        var fullNote = await _unitOfWork.DeliveryNotes.GetByIdAsync(_deliveryNote.Id);
        if (fullNote?.Details != null)
        {
            foreach (var detail in fullNote.Details)
            {
                Details.Add(detail);
            }
        }
    }
}
