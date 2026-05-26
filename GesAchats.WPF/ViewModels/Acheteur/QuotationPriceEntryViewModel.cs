using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class QuotationPriceEntryViewModel : BaseViewModel
{
    private readonly IUnitOfWork _unitOfWork;
    private Quotation? _selectedQuotation;

    public ObservableCollection<Quotation> SentQuotations { get; } = new();
    public ObservableCollection<QuotationDetail> QuotationDetails { get; } = new();

    public Quotation? SelectedQuotation
    {
        get => _selectedQuotation;
        set
        {
            if (SetProperty(ref _selectedQuotation, value))
            {
                _ = LoadQuotationDetails();
            }
        }
    }

    public ICommand RefreshCommand { get; }
    public ICommand SavePricesCommand { get; }

    public QuotationPriceEntryViewModel(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        Title = "Saisie des Prix Fournisseurs";

        RefreshCommand = new RelayCommand(async _ => await LoadQuotations());
        SavePricesCommand = new RelayCommand(async _ => await ExecuteSavePrices(), _ => SelectedQuotation != null);

        _ = LoadQuotations();
    }

    private async Task LoadQuotations()
    {
        IsBusy = true;
        try
        {
            // Charger les devis envoyés (Sent) qui n'ont pas encore été totalement complétés ou validés
            var quotes = await _unitOfWork.Quotations.FindAsync(q => q.Status == "Sent" || q.Status == "Received");
            SentQuotations.Clear();
            foreach (var q in quotes.OrderByDescending(x => x.Date))
            {
                SentQuotations.Add(q);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadQuotationDetails()
    {
        if (SelectedQuotation == null)
        {
            QuotationDetails.Clear();
            return;
        }

        IsBusy = true;
        try
        {
            var fullQuote = await _unitOfWork.Quotations.GetWithDetailsAsync(SelectedQuotation.Id);
            QuotationDetails.Clear();
            if (fullQuote != null)
            {
                foreach (var detail in fullQuote.Details)
                {
                    QuotationDetails.Add(detail);
                }
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ExecuteSavePrices()
    {
        if (SelectedQuotation == null) return;

        IsBusy = true;
        try
        {
            // Calculer le total
            decimal totalHT = 0;
            foreach (var detail in QuotationDetails)
            {
                totalHT += detail.Quantity * detail.UnitPriceHT;
                _unitOfWork.QuotationDetails.Update(detail);
            }

            SelectedQuotation.TotalAmountHT = totalHT;
            SelectedQuotation.TotalAmountTTC = totalHT * 1.2m; // Exemple TVA 20%
            SelectedQuotation.Status = "Received";
            SelectedQuotation.UpdatedAt = DateTime.UtcNow;
            SelectedQuotation.ResponseDate = DateTime.UtcNow;

            _unitOfWork.Quotations.Update(SelectedQuotation);
            await _unitOfWork.CompleteAsync();

            System.Windows.MessageBox.Show("Les prix ont été enregistrés avec succès.", "Succès", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
