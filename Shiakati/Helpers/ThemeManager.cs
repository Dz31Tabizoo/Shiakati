using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace Shiakati.Helpers
{
    public class ThemeManager
    {

        private const string AssemblyName = "Shiakati"; // must match your project name
        private const string LightThemePath = "/Shiakati;component/Resources/Themes/LightTheme.xaml";
        private const string DarkThemePath = "/Shiakati;component/Resources/Themes/DarkTheme.xaml";

        public static void ApplyTheme(bool isDark)
        {
            var appDict = Application.Current.Resources.MergedDictionaries;
            string newThemeUri = isDark ? DarkThemePath : LightThemePath;
            string oldThemeUri = isDark ? LightThemePath : DarkThemePath;

            var oldTheme = appDict.FirstOrDefault(d =>
                d.Source?.OriginalString.EndsWith(oldThemeUri, StringComparison.OrdinalIgnoreCase) == true);
            if (oldTheme != null)
                appDict.Remove(oldTheme);

            if (appDict.Any(d => d.Source?.OriginalString.EndsWith(newThemeUri, StringComparison.OrdinalIgnoreCase) == true))
                return;

            var newTheme = new ResourceDictionary
            {
                Source = new Uri(newThemeUri, UriKind.Relative)
            };
            appDict.Add(newTheme);

            // Toggle MaterialDesign base theme to match
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
            paletteHelper.SetTheme(theme);
        }
    }
}
