using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using Shiakati.Properties;
using Shiakati.Services.Interfaces;
using Shiakati.Models;

namespace Shiakati.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ICacheService _cache;

        [ObservableProperty]
        private string? _selectedTicketPrinterName;
        [ObservableProperty]
        private string? _selectedBarcodePrinterName;
        [ObservableProperty]
        private string _newCategoryName = string.Empty;

        public ObservableCollection<CategoryModel> GlobalCategories
            => _cache.Get<ObservableCollection<CategoryModel>>(CacheKeys.Catalog);

        public ObservableCollection<string> InstalledPrinters { get; } = new ();

        //constractor
        public SettingsViewModel(ICacheService cacheService)
        {
            _cache = cacheService;

            LoadPrinters();
            SelectedTicketPrinterName = Settings.Default.TicketPrinterName;
            SelectedBarcodePrinterName = Settings.Default.BarcodePrinterName;
        }

        [RelayCommand]
        private void AddCategory()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName)) return;
            
                int newID = GlobalCategories.Count >0 ? GlobalCategories.Max(c => c.CategoryID) + 1 : 1;

                GlobalCategories.Add(new CategoryModel
                {
                    CategoryID = newID,
                    CategoryName = NewCategoryName.Trim()
                });

                NewCategoryName = string.Empty;
            
        }
        private void LoadPrinters()
        {
            InstalledPrinters.Clear();
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                InstalledPrinters.Add(printer);
            }
        }
        [RelayCommand]
        private void SaveTicketPrinterSettings()
        {
            // Save the selected printer name to application settings
             Properties.Settings.Default.TicketPrinterName = SelectedTicketPrinterName;
             Properties.Settings.Default.Save();

            System.Windows.MessageBox.Show("Settings saved successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        [RelayCommand]
        private void SaveBarcodePrinterSettings()
        {
            // Save the selected printer name to application settings
            Properties.Settings.Default.BarcodePrinterName = SelectedBarcodePrinterName;
            Properties.Settings.Default.Save();

            System.Windows.MessageBox.Show("Settings saved successfully!", "Success", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

    }
}
