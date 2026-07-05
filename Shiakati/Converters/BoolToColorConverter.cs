using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Shiakati.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b)
                ? new SolidColorBrush((Color)App.Current.Resources["AccentColor"])
                : new SolidColorBrush((Color)App.Current.Resources["DangerColor"]);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}