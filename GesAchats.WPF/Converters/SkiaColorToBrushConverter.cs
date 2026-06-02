using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GesAchats.WPF.Converters
{
    public class SkiaColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorPaint solidPaint)
            {
                var skColor = solidPaint.Color;
                return new SolidColorBrush(Color.FromArgb(
                    skColor.Alpha,
                    skColor.Red,
                    skColor.Green,
                    skColor.Blue));
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
