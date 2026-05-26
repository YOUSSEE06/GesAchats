using System.Windows;
using GesAchats.Core.Entities;

namespace GesAchats.WPF.Views.Acheteur.Besoins;

public partial class ArticleDetailWindow : Window
{
    public ArticleDetailWindow(Need need)
    {
        InitializeComponent();
        
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
}
