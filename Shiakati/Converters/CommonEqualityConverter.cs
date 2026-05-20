using System;
using System.Globalization;
using System.Windows.Data;

namespace Shiakati.Converters
{
    public class CommonEqualityConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2 || values[0] == null || values[1] == null)
                return false;

            // Compare le nom de la catégorie du bouton avec la catégorie sélectionnée dans le ViewModel
            return values[0].ToString().Equals(values[1].ToString(), StringComparison.OrdinalIgnoreCase);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}