using System.Collections.ObjectModel;
using System.Windows.Input;
using AsyncRelayCommand = CommunityToolkit.Mvvm.Input.AsyncRelayCommand;
using IAsyncRelayCommand = CommunityToolkit.Mvvm.Input.IAsyncRelayCommand;
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
                OnPropertyChanged(nameof(TotalCommandeTTC));
                OnPropertyChanged(nameof(TotalCommandeHT));
                OnPropertyChanged(nameof(TotalBlTTC));
                OnPropertyChanged(nameof(TotalBlHT));
                OnPropertyChanged(nameof(TotalFactureHT));
                OnPropertyChanged(nameof(TotalFactureTVA));
                OnPropertyChanged(nameof(TotalFactureTTC));
                OnPropertyChanged(nameof(TotalFacturesMontant));
                OnPropertyChanged(nameof(TotalReglementsMontant));
                OnPropertyChanged(nameof(TotalRegle));
                OnPropertyChanged(nameof(TotalResteAPayer));
                OnPropertyChanged(nameof(TotalCommandesPrevious));
                OnPropertyChanged(nameof(TotalCommandesPercentage));
                OnPropertyChanged(nameof(TotalBlsPrevious));
                OnPropertyChanged(nameof(TotalBlsPercentage));
                OnPropertyChanged(nameof(TotalFacturesPrevious));
                OnPropertyChanged(nameof(TotalFacturesPercentage));
                OnPropertyChanged(nameof(TotalReglementsPrevious));
                OnPropertyChanged(nameof(TotalReglementsPercentage));
                OnPropertyChanged(nameof(SoldeAPayerPrevious));
                OnPropertyChanged(nameof(SoldeAPayerPercentage));
                if (value != null)
                {
                    Title = $"Situation du Fournisseur - {value.NomFournisseur}";
                    FormatTrends();
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

    public decimal TotalCommandeTTC => Operations.Sum(o => o.TotalCommande);
    public decimal TotalCommandeHT => Operations.Sum(o => o.TotalCommandeHT);
    public decimal TotalBlTTC => Operations.Where(o => o.HasDeliveryNote).Sum(o => o.TotalBlTTC);
    public decimal TotalBlHT => Operations.Where(o => o.HasDeliveryNote).Sum(o => o.TotalBlHT);
    public decimal TotalFactureHT => Operations.Where(o => o.HasInvoice).SelectMany(o => o.FactureArticles).Sum(a => a.MontantHT);
    public decimal TotalFactureTVA => Operations.Where(o => o.HasInvoice).SelectMany(o => o.FactureArticles).Sum(a => a.TVA);
    public decimal TotalFactureTTC => Operations.Where(o => o.HasInvoice).SelectMany(o => o.FactureArticles).Sum(a => a.MontantTTC);
    public decimal TotalFacturesMontant => Operations.Where(o => o.HasInvoice && o.TotalFacture.HasValue).Sum(o => o.TotalFacture!.Value);
    public decimal TotalReglementsMontant => Operations.Where(o => o.HasPayments).SelectMany(o => o.Reglements).Sum(r => r.Montant);
    public decimal TotalRegle => Operations.Where(o => o.HasInvoice).Sum(o => o.TotalRegle ?? 0m);
    public decimal TotalResteAPayer => Operations.Where(o => o.HasInvoice).Sum(o => o.ResteAPayer ?? 0m);

    // Valeurs de la période précédente + pourcentage d'évolution (même logique que le Dashboard principal)
    public int TotalCommandesPrevious => Situation?.TotalCommandesPrevious ?? 0;
    public double TotalCommandesPercentage => Situation?.TotalCommandesVariation ?? 0;
    public int TotalBlsPrevious => Situation?.TotalBlsPrevious ?? 0;
    public double TotalBlsPercentage => Situation?.TotalBlsVariation ?? 0;
    public int TotalFacturesPrevious => Situation?.TotalFacturesPrevious ?? 0;
    public double TotalFacturesPercentage => Situation?.TotalFacturesVariation ?? 0;
    public decimal TotalReglementsPrevious => Situation?.TotalReglementsPrevious ?? 0m;
    public double TotalReglementsPercentage => Situation?.TotalReglementsVariation ?? 0;
    public decimal SoldeAPayerPrevious => Situation?.SoldeAPayerPrevious ?? 0m;
    public double SoldeAPayerPercentage => Situation?.SoldeAPayerVariation ?? 0;

    // Filtre de période (identique au Dashboard principal)
    private DateTime _startDate = DateTime.Today.AddMonths(-1);
    private DateTime _endDate = DateTime.Today;

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    // Tendances KPI (couple texte + couleur, comme les KpiCard du dashboard)
    private string _totalCommandesTrend = "";
    private bool _totalCommandesIsPositive = true;
    public string TotalCommandesTrend { get => _totalCommandesTrend; set => SetProperty(ref _totalCommandesTrend, value); }
    public bool TotalCommandesIsPositive { get => _totalCommandesIsPositive; set => SetProperty(ref _totalCommandesIsPositive, value); }

    private string _totalBlsTrend = "";
    private bool _totalBlsIsPositive = true;
    public string TotalBlsTrend { get => _totalBlsTrend; set => SetProperty(ref _totalBlsTrend, value); }
    public bool TotalBlsIsPositive { get => _totalBlsIsPositive; set => SetProperty(ref _totalBlsIsPositive, value); }

    private string _totalFacturesTrend = "";
    private bool _totalFacturesIsPositive = true;
    public string TotalFacturesTrend { get => _totalFacturesTrend; set => SetProperty(ref _totalFacturesTrend, value); }
    public bool TotalFacturesIsPositive { get => _totalFacturesIsPositive; set => SetProperty(ref _totalFacturesIsPositive, value); }

    private string _totalReglementsTrend = "";
    private bool _totalReglementsIsPositive = true;
    public string TotalReglementsTrend { get => _totalReglementsTrend; set => SetProperty(ref _totalReglementsTrend, value); }
    public bool TotalReglementsIsPositive { get => _totalReglementsIsPositive; set => SetProperty(ref _totalReglementsIsPositive, value); }

    private string _soldeAPayerTrend = "";
    private bool _soldeAPayerIsPositive = true;
    public string SoldeAPayerTrend { get => _soldeAPayerTrend; set => SetProperty(ref _soldeAPayerTrend, value); }
    public bool SoldeAPayerIsPositive { get => _soldeAPayerIsPositive; set => SetProperty(ref _soldeAPayerIsPositive, value); }

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
    public ICommand ExporterExcelCommand { get; }
    public IAsyncRelayCommand RefreshCommand { get; }

    public SituationFournisseurViewModel(ISuiviFournisseurService service, INavigationService navigationService)
    {
        _service = service;
        _navigationService = navigationService;
        Title = "Situation du Fournisseur";

        RetourCommand = new RelayCommand(_ => Retour());
        ExporterExcelCommand = new RelayCommand(_ => ExporterExcel());
        RefreshCommand = new AsyncRelayCommand(() => LoadSituationAsync(_fournisseurId));
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
            var result = await _service.GetSituationFournisseurAsync(fournisseurId, StartDate, EndDate);
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

    private void FormatTrends()
    {
        FormatSingleTrend(TotalCommandesPercentage, out var cmdTrend, out var cmdPos);
        TotalCommandesTrend = cmdTrend; TotalCommandesIsPositive = cmdPos;

        FormatSingleTrend(TotalBlsPercentage, out var blsTrend, out var blsPos);
        TotalBlsTrend = blsTrend; TotalBlsIsPositive = blsPos;

        FormatSingleTrend(TotalFacturesPercentage, out var facTrend, out var facPos);
        TotalFacturesTrend = facTrend; TotalFacturesIsPositive = facPos;

        FormatSingleTrend(TotalReglementsPercentage, out var regTrend, out var regPos);
        TotalReglementsTrend = regTrend; TotalReglementsIsPositive = regPos;

        FormatSingleTrend(SoldeAPayerPercentage, out var solTrend, out var solPos);
        SoldeAPayerTrend = solTrend; SoldeAPayerIsPositive = solPos;
    }

    private void FormatSingleTrend(double percentage, out string trendText, out bool isPositive)
    {
        if (percentage > 0)
        {
            trendText = $"+{percentage:F1}%";
            isPositive = true;
        }
        else if (percentage < 0)
        {
            trendText = $"{percentage:F1}%";
            isPositive = false;
        }
        else
        {
            trendText = "0%";
            isPositive = true;
        }
    }

    private void Retour()
    {
        _navigationService.NavigateTo("SuiviFournisseurs");
    }

    private string BuildExportFileName()
    {
        var name = (Situation?.NomFournisseur ?? "fournisseur").Trim();
        var invalid = System.IO.Path.GetInvalidFileNameChars();
        name = new string(name.Where(c => !invalid.Contains(c)).ToArray()).Replace(' ', '_');
        if (string.IsNullOrWhiteSpace(name)) name = "fournisseur";
        return $"Suivi_fournisseur_{name}.xlsx";
    }

    private void ExporterExcel()
    {
        if (Situation == null)
        {
            System.Windows.MessageBox.Show("Aucune situation à exporter.", "Information",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            return;
        }

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Exporter la situation en Excel",
            Filter = "Fichier Excel (*.xlsx)|*.xlsx",
            FileName = BuildExportFileName()
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                Services.SuiviFournisseurExcelExporter.Export(dialog.FileName, Situation);
                System.Windows.MessageBox.Show($"Export Excel terminé :\n{dialog.FileName}", "Export réussi",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors de l'export Excel de la situation");
                System.Windows.MessageBox.Show($"Erreur lors de l'export Excel : {ex.Message}", "Erreur",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
