using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.DTOs;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;
using Serilog;

namespace GesAchats.WPF.ViewModels.Admin;

public class SituationFournisseurViewModel : BaseViewModel, INavigatable
{
    private readonly ISuiviFournisseurService _service;
    private readonly INavigationService _navigationService;
    private SituationFournisseurDto? _situation;
    private bool _isLoading;
    private string _errorMessage = string.Empty;
    private int _fournisseurId;
    private ObservableCollection<SituationOperationGroupDto> _operations = new();

    public SituationFournisseurDto? Situation
    {
        get => _situation;
        set
        {
            if (SetProperty(ref _situation, value))
            {
                Log.Information("Situation property set to value with {OperationsCount} operations", value?.Operations.Count ?? 0);
                // Update the Operations collection
                Operations.Clear();
                if (value?.Operations != null)
                {
                    foreach (var op in value.Operations)
                    {
                        Operations.Add(op);
                    }
                }
                OnPropertyChanged(nameof(TotalCommandes));
                OnPropertyChanged(nameof(TotalBls));
                OnPropertyChanged(nameof(TotalFactures));
                OnPropertyChanged(nameof(TotalReglements));
                OnPropertyChanged(nameof(SoldeAPayer));
                if (value != null)
                {
                    Title = $"Situation du Fournisseur - {value.NomFournisseur}";
                }
            }
        }
    }

    public ObservableCollection<SituationOperationGroupDto> Operations
    {
        get => _operations;
        set => SetProperty(ref _operations, value);
    }

    public int TotalCommandes => Situation?.TotalCommandes ?? 0;
    public int TotalBls => Situation?.TotalBls ?? 0;
    public int TotalFactures => Situation?.TotalFactures ?? 0;
    public decimal TotalReglements => Situation?.TotalReglements ?? 0m;
    public decimal SoldeAPayer => Situation?.SoldeAPayer ?? 0m;

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    public ICommand RetourCommand { get; }
    public ICommand ExporterPdfCommand { get; }
    public ICommand ExporterExcelCommand { get; }

    public SituationFournisseurViewModel(ISuiviFournisseurService service, INavigationService navigationService)
    {
        _service = service;
        _navigationService = navigationService;
        Title = "Situation du Fournisseur";

        RetourCommand = new RelayCommand(_ => Retour());
        ExporterPdfCommand = new RelayCommand(_ => ExporterPdf());
        ExporterExcelCommand = new RelayCommand(_ => ExporterExcel());
    }

    public async void OnNavigatedTo(object parameter)
    {
        Log.Information("OnNavigatedTo called with parameter: {Parameter}, type: {Type}", parameter, parameter?.GetType().Name);
        if (parameter is int fournisseurId && fournisseurId > 0)
        {
            _fournisseurId = fournisseurId;
            await LoadSituationAsync(fournisseurId);
        }
    }

    private async System.Threading.Tasks.Task LoadSituationAsync(int fournisseurId)
    {
        Log.Information("LoadSituationAsync starting for fournisseurId: {FournisseurId}", fournisseurId);
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await _service.GetSituationFournisseurAsync(fournisseurId);
            Log.Information("LoadSituationAsync got result: NomFournisseur={Nom}, TotalCommandes={Commandes}, TotalBls={Bls}, TotalFactures={Factures}, TotalReglements={Reglements}, SoldeAPayer={Solde}",
                result.NomFournisseur, result.TotalCommandes, result.TotalBls, result.TotalFactures, result.TotalReglements, result.SoldeAPayer);
            Situation = result;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Error: {ex.Message}\nStack: {ex.StackTrace}", "Error");
            Log.Error(ex, "Erreur lors du chargement de la situation du fournisseur {FournisseurId}", fournisseurId);
            ErrorMessage = "Une erreur s'est produite. Veuillez réessayer.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Retour()
    {
        _navigationService.NavigateTo("SuiviFournisseurs");
    }

    private void ExporterPdf()
    {
        // TODO: Implement PDF export
        System.Windows.MessageBox.Show("Export PDF en cours de développement.", "Information",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private void ExporterExcel()
    {
        // TODO: Implement Excel export
        System.Windows.MessageBox.Show("Export Excel en cours de développement.", "Information",
            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }
}
