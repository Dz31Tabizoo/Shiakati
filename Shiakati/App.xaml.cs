using System.Configuration;
using System.Data;
using System.Windows;

namespace Shiakati
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // إضافة الموارد يدوياً لضمان التحميل في حال فشل XAML Designer
            var materialDesignDefaults = new ResourceDictionary
            {
                Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml")
            };
            this.Resources.MergedDictionaries.Add(materialDesignDefaults);
        }
    }

}
