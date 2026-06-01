using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GesAchats.WPF.ViewModels
{
    public partial class PurchaseOrderChartViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableCollection<ChartBarData> _chartData;

        public PurchaseOrderChartViewModel()
        {
            ChartData = new ObservableCollection<ChartBarData>();
        }

        public double CalculateBarHeight(int value, int maxValue, double chartHeight)
        {
            return (value / (double)maxValue) * chartHeight;
        }
    }

    public partial class ChartBarData : ObservableObject
    {
        [ObservableProperty]
        private string _label;

        [ObservableProperty]
        private int _value;

        [ObservableProperty]
        private double _barHeight;

        [ObservableProperty]
        private Brush _barColor;
    }
}
