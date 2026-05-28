using System.Windows;
using GesAchats.WPF.ViewModels.Comptable;

namespace GesAchats.WPF.Views.Comptable.Factures;

public partial class FactureDetailsWindow : Window
{
    public FactureDetailsWindow(FactureDetailsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.RequestClose += (s, e) => this.Close();
    }
}
