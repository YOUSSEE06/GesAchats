using System.Windows;
using GesAchats.Core.Entities;
using GesAchats.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GesAchats.WPF.Views.Acheteur.Besoins;

public partial class ArticleDetailWindow : Window
{
    private readonly Need _need;

    public ArticleDetailWindow(Need need)
    {
        InitializeComponent();
        _need = need;

        TxtRequestNumber.Text = need.NumeroBesoin;
        TxtRequester.Text = need.RequestedBy?.FullName ?? "Inconnu";
        TxtDate.Text = need.DateTransmission?.ToString("dd/MM/yyyy") ?? need.RequestedAt.ToString("dd/MM/yyyy");
        TxtStatus.Text = need.Status.ToString();
        TxtDescription.Text = string.IsNullOrEmpty(need.Description) ? "Aucune description fournie." : need.Description;

        DgArticles.ItemsSource = need.Details;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private async void ExportPdf_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var pdfService = ((App)Application.Current).ServiceProvider.GetRequiredService<IPdfGeneratorService>();
            string filePath = await pdfService.GenerateNeedPdfAsync(_need);

            if (!string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"Erreur lors de la génération du PDF : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
