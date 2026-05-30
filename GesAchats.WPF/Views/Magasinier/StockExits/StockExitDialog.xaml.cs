using System.Windows;

namespace GesAchats.WPF.Views.Magasinier.StockExits;

public partial class StockExitDialog : Window
{
    public StockExitDialog()
    {
        InitializeComponent();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // On pourrait ajouter un événement ou une action pour fermer la fenêtre après succès, 
    // mais pour faire simple, le ViewModel peut déclencher un événement ou on peut écouter le succès.
}
