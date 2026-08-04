using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Shiakati.Converters
{
    public class CategoryToIconConverter : IValueConverter
    {
        // Static cache – key = category name, value = loaded ImageSource
        private static readonly Dictionary<string, ImageSource> _iconCache =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> CategoryIcons = new()
    {
        { "Thob H", "/Resources/Photos/Thob-H.png" },
        { "Chaussures", "/Resources/Photos/Chaussures.png" },
        { "Accesoires", "/Resources/Photos/Accessoires.png" },
        { "Sous-Vetement", "/Resources/Photos/Underwear.png" },
        { "Pantalon", "/Resources/Photos/Pantalon.png" },
        { "Cosmétique", "/Resources/Photos/Parfum.png" },
    };

        private const string DefaultIcon = "/Resources/Photos/Box.png";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string categoryName)
                return GetDefault();

            // Try cache first
            if (_iconCache.TryGetValue(categoryName, out var cached))
                return cached;

            // Determine path
            string iconPath = CategoryIcons.TryGetValue(categoryName, out var path)
                              ? path
                              : DefaultIcon;

            // Load and cache
            var imageSource = new BitmapImage(new Uri(iconPath, UriKind.Relative));
            _iconCache[categoryName] = imageSource;
            return imageSource;
        }

        private ImageSource GetDefault()
        {
            if (!_iconCache.TryGetValue("__default__", out var cached))
            {
                cached = new BitmapImage(new Uri(DefaultIcon, UriKind.Relative));
                _iconCache["__default__"] = cached;
            }
            return cached;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
