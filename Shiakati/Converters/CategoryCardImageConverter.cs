using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Shiakati.Converters
{
    public class CategoryCardImageConverter : IValueConverter
    {
        private static readonly Dictionary<string, ImageSource> _iconCache =
            new(StringComparer.OrdinalIgnoreCase);

        // Point to alternative image files (e.g., bigger or more decorative)
        private static readonly Dictionary<string, string> CategoryCardImages = new()
        {
            { "Thob H", "/Resources/Photos/Cards/Thob-H-card.png" },
            { "Chaussures", "/Resources/Photos/Cards/Chaussures-card.png" },
            { "Accesoires", "/Resources/Photos/Cards/Accessoires-card.png" },
            { "Sous-Vetement", "/Resources/Photos/Cards/Sous-vetement-card.png" },
            
            { "Cosmétique", "/Resources/Photos/Cards/Cosmetic-card.png" },
        };

        private const string DefaultCardImage = "/Resources/Photos/Cards/Default-card.png";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string categoryName)
                return GetDefault();

            if (_iconCache.TryGetValue(categoryName, out var cached))
                return cached;

            string relativePath = CategoryCardImages.TryGetValue(categoryName, out var p)
                                  ? p
                                  : DefaultCardImage;

            // Build a proper pack URI
            string packUri = $"pack://application:,,,/Shiakati;component{relativePath}";
            var bitmap = new BitmapImage(new Uri(packUri, UriKind.Absolute));
            bitmap.Freeze();   // important for cross-thread usage

            _iconCache[categoryName] = bitmap;
            return bitmap;
        }

        private ImageSource GetDefault()
        {
            if (!_iconCache.TryGetValue("__default_card__", out var cached))
            {
                cached = new BitmapImage(new Uri(DefaultCardImage, UriKind.Relative));
                ((BitmapImage)cached).CacheOption = BitmapCacheOption.OnLoad;
                ((BitmapImage)cached).Freeze();
                _iconCache["__default_card__"] = cached;
            }
            return cached;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
