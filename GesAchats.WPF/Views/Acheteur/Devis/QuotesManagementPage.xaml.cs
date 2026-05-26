using System.Windows.Controls;
using GesAchats.WPF.ViewModels.Acheteur;

namespace GesAchats.WPF.Views.Acheteur.Devis;

public partial class QuotesManagementPage : Page
{
    public QuotesManagementPage(QuotesManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
