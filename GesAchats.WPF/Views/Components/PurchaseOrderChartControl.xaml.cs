using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using GesAchats.WPF.ViewModels;

namespace GesAchats.WPF.Views.Components
{
    public partial class PurchaseOrderChartControl : System.Windows.Controls.UserControl
    {
        public PurchaseOrderChartControl()
        {
            InitializeComponent();
        }

        #region Dependency Properties

        public string ChartTitle
        {
            get { return (string)GetValue(ChartTitleProperty); }
            set { SetValue(ChartTitleProperty, value); }
        }

        public static readonly DependencyProperty ChartTitleProperty =
            DependencyProperty.Register("ChartTitle", typeof(string), 
                typeof(PurchaseOrderChartControl), 
                new PropertyMetadata("Bons de commande (7 derniers jours)"));

        public ObservableCollection<ChartBarData> ChartData
        {
            get { return (ObservableCollection<ChartBarData>)GetValue(ChartDataProperty); }
            set { SetValue(ChartDataProperty, value); }
        }

        public static readonly DependencyProperty ChartDataProperty =
            DependencyProperty.Register("ChartData", typeof(ObservableCollection<ChartBarData>),
                typeof(PurchaseOrderChartControl),
                new PropertyMetadata(new ObservableCollection<ChartBarData>()));

        #endregion
    }
}
