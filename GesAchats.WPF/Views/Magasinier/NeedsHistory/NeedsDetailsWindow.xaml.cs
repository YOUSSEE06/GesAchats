using System.Windows;
using GesAchats.WPF.ViewModels.Magasinier;

namespace GesAchats.WPF.Views.Magasinier.NeedsHistory;

public partial class NeedsDetailsWindow : Window
{
    public NeedsDetailsWindow(NeedsDetailsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += (s, e) => this.Close();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
