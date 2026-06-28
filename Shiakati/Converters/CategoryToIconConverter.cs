using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace Shiakati.Converters
{
    public class CategoryToIconConverter : IValueConverter
    {
        // Dictionary for category → relative image path (inside your project)
        private static readonly Dictionary<string, string> CategoryIcons = new Dictionary<string, string>
        {
            { "Thob H", "/Resources/Photos/Thob-H.png" },
            { "Chaussures", "/Resources/Photos/Chaussures.png" },
            { "Accesoires", "/Resources/Photos/Accessoires.png" },
            { "Sous-Vetement", "/Resources/Photos/Underwear.png" },
            { "Pantalon", "/Resources/Photos/Pantalon.png" },
            { "Cosmétique", "/Resources/Photos/Parfum.png" },
            // add as many as you need
        };

        // Fallback icon if category not found
        private const string DefaultIcon = "/Resources/Photos/Box.png";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string categoryName && CategoryIcons.TryGetValue(categoryName, out string iconPath))
                return new BitmapImage(new Uri(iconPath, UriKind.Relative));

            // Return default
            return new BitmapImage(new Uri(DefaultIcon, UriKind.Relative));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
