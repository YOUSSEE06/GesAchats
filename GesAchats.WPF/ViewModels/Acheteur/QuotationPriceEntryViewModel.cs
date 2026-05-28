using System.Collections.ObjectModel;
using System.Windows.Input;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.Services;
using GesAchats.WPF.ViewModels.Base;
using Microsoft.EntityFrameworkCore;

namespace GesAchats.WPF.ViewModels.Acheteur;

public class QuotationPriceEntryViewModel : BaseViewModel, INavigatable
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

    public void OnNavigatedTo(object parameter)
    {
        if (parameter is int quotationId)
        {
            _ = LoadQuotationById(quotationId);
        }
    }

    private async Task LoadQuotationById(int quotationId)
    {
        IsBusy = true;
        try
        {
            var quote = await _unitOfWork.Quotations.GetWithDetailsAsync(quotationId);
            if (quote != null)
            {
                if (!SentQuotations.Any(q => q.Id == quote.Id))
                {
                    SentQuotations.Add(quote);
                }
                SelectedQuotation = SentQuotations.FirstOrDefault(q => q.Id == quote.Id);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadQuotations()
    {
        IsBusy = true;
        try
        {
            var allQuotes = await _unitOfWork.Quotations.GetAllWithAllRelatedAsync();
            var quotes = allQuotes.Where(q => q.Status == QuotationStatus.Pending);
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
            decimal totalHT = 0;
            foreach (var detail in QuotationDetails)
            {
                totalHT += detail.Quantity * detail.UnitPriceHT;
                _unitOfWork.QuotationDetails.Update(detail);
            }

            SelectedQuotation.TotalAmountHT = totalHT;
            SelectedQuotation.TotalAmountTTC = totalHT * 1.2m;
            SelectedQuotation.Status = QuotationStatus.Validated;
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
