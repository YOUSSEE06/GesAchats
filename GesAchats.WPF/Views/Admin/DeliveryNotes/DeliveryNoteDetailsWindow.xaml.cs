using System.Windows;

namespace GesAchats.WPF.Views.Admin.DeliveryNotes
{
    public partial class DeliveryNoteDetailsWindow : Window
    {
        public DeliveryNoteDetailsWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
