using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Models;
using Shiakati.Properties;
using Shiakati.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Shiakati.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly ICacheService _cache;
        private readonly ICatalogService _catalogDb;

        [ObservableProperty] private string? _selectedTicketPrinterName;
        [ObservableProperty] private string? _selectedBarcodePrinterName;
        [ObservableProperty] private string _newCategoryName = string.Empty;

        public ObservableCollection<CategoryModel> GlobalCategories { get; } = new();
        public ObservableCollection<string> InstalledPrinters { get; } = new ();

        //constractor
        public SettingsViewModel(ICacheService cacheService,ICatalogService catalogService)
        {
            _cache = cacheService;
            _catalogDb = catalogService;

            LoadPrinters();
            SelectedTicketPrinterName = Settings.Default.TicketPrinterName;
            SelectedBarcodePrinterName = Settings.Default.BarcodePrinterName;
            _ = LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                // On utilise la même logique infaillible que le StockViewModel
                var catalog = await _cache.GetOrLoadAsync<(List<BrandsModel> Brands, List<CategoryModel> Categories)>(
              CacheKeys.Catalog,
              () => _catalogDb.GetInitialGatalogDataAsync());

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    GlobalCategories.Clear();
                    if (catalog.Categories != null)
                    {
                        foreach (var cat in catalog.Categories ?? new())
                            GlobalCategories.Add(cat);
                    }
                });
            } catch(Exception ex) 
            {
                MessageBox.Show($"Erreur chargement paramètres: { ex.Message}");
            }
                
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
