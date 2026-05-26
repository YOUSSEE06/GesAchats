using System.Windows;
using GesAchats.WPF.ViewModels.Magasinier;

namespace GesAchats.WPF.Views.Magasinier.NeedsHistory;

public partial class NeedsStatisticsWindow : Window
{
    public NeedsStatisticsWindow(NeedsStatisticsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
}
