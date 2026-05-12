using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using Shiakati.Views;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Shiakati.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IAuthenticationClientService _authService;
        [ObservableProperty]
        private object _currentView;

        // On garde les instances des ViewModels pour ne pas les recréer à chaque click

        public SalesHistoryViewModel SalesHistory { get; }
        public PosContainerViewModel PosContainer { get; }
        public StockViewModel Stock { get; }

        public SettingsViewModel Settings { get; }

        public MainViewModel(ICacheService cacheService ,IAuthenticationClientService authService, PosContainerViewModel posContainer, StockViewModel stockViewModel, SalesHistoryViewModel salesHistory,SettingsViewModel settingsViewModel )
        {
            _authService = authService;
            PosContainer = posContainer;
            Stock = stockViewModel;
            SalesHistory = salesHistory;
            Settings = settingsViewModel;
            // Vue par défaut au démarrage
            CurrentView = PosContainer;

            // later on if empty we get data from DB andon the login and not here

            if (!cacheService.Contains(CacheKeys.CategoriesList))
            {
                var initialCategories = new ObservableCollection<CategoryModel>
                {
                    new CategoryModel{CategoryID = 1,CategoryName="Thob" },
                    new CategoryModel{CategoryID = 2,CategoryName="Pantalon" },
                    new CategoryModel{CategoryID = 1,CategoryName="Chaussure" },
                    new CategoryModel{CategoryID = 1,CategoryName="Cosmetic" },
                    new CategoryModel{CategoryID = 1,CategoryName="Accessoire" }
                };
                cacheService.Set(CacheKeys.CategoriesList, initialCategories);
            }
        }
        // ✅ On assigne le ViewModel, pas la View !
        [RelayCommand]
        private async Task NavigateToStock()
        {
            CurrentView = Stock;

            if (Stock.Categories.Count()==0 ||Stock.Brands.Count == 0)
            {
                await Stock.LoadInitialDataAsync();
            }
        }

        [RelayCommand]
        private void NavigateToPOS()=> CurrentView = PosContainer;

        [RelayCommand]
        private void NavigateToSalesHistory() => CurrentView = SalesHistory;
        [RelayCommand]
        private void ExitApplication()
        {
            // Logique pour fermer l'application
            System.Windows.Application.Current.Shutdown();
        }

        [RelayCommand]
        private void NavToSettings() => CurrentView = Settings;

        [RelayCommand]
        private void Logout()
        {
            // Logique pour se déconnecter et revenir à la page de connexion
            _authService.Logout(); // Appeler la méthode de déconnexion de votre service d'authentification
            var loginView = App.ServiceProvider.GetRequiredService<LoginView>();
            loginView.Show();
            // Fermer la fenêtre principale
            System.Windows.Application.Current.MainWindow.Close();
        }

    }
}
