using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Models;
using Shiakati.Properties;
using Shiakati.Services.Interfaces;
using Shiakati.Views;
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
        private readonly IAuthenticationClientService _authService;

        [ObservableProperty] private string? _selectedTicketPrinterName;
        [ObservableProperty] private string? _selectedBarcodePrinterName;
        [ObservableProperty] private string _newCategoryName = string.Empty;

        public ObservableCollection<CategoryModel> GlobalCategories { get; } = new();
        public ObservableCollection<string> InstalledPrinters { get; } = new();

        //constractor
        public SettingsViewModel(ICacheService cacheService, ICatalogService catalogService, IAuthenticationClientService authenticationClientService)
        {
            _cache = cacheService;
            _catalogDb = catalogService;
            _authService = authenticationClientService;

            LoadPrinters();
            SelectedTicketPrinterName = Settings.Default.TicketPrinterName;
            SelectedBarcodePrinterName = Settings.Default.BarcodePrinterName;
            _ = LoadCategoriesAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                _cache.Clear();
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur chargement paramètres: {ex.Message}");
            }

        }
        [RelayCommand]
        private async Task AddCategory()
        {
            if (string.IsNullOrWhiteSpace(NewCategoryName)) return;

            var newCategory = new CategoryModel
            {
                CategoryName = NewCategoryName.Trim()
            };


            await _catalogDb.AddCategoryModelAsync(newCategory);

            await LoadCategoriesAsync();
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

        [RelayCommand]
        private async Task ChangePassword()
        {
            var dialog = new ChangePasswordDialog { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                dynamic data = dialog.Tag;
                bool success = await _authService.ChangePasswordAsync((string)data.OldPassword, (string)data.NewPassword);
                if (success)
                    MessageBox.Show("Mot de passe modifié avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Erreur lors du changement de mot de passe.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand] private async Task ChangeUsername()
        {
            var dialog = new ChangeUsernameDialog { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                dynamic data = dialog.Tag;
                bool success = await _authService.ChangeUsernameAsync((string)data.Password, (string)data.NewUsername);
                if (success)
                    MessageBox.Show("Nom d'utilisateur modifié avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                else
                    MessageBox.Show("Erreur lors du changement de nom d'utilisateur.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        [RelayCommand] private async Task Register()
        {

            if (_authService.CurrentSession?.Role != "admin" && _authService.CurrentSession?.Role != "owner")
            {
                MessageBox.Show("Vous n'avez pas les permissions nécessaires pour enregistrer un utilisateur.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var dialog = new RegisterDialog { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // ✅ Access properties directly – no need for anonymous object
                    bool success = await _authService.RegisterAsync(
                        dialog.Username,
                        dialog.Password,
                        dialog.Role
                    );

                    if (success)
                        MessageBox.Show("Utilisateur enregistré avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    else
                        MessageBox.Show("Erreur lors de l'enregistrement de l'utilisateur.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
