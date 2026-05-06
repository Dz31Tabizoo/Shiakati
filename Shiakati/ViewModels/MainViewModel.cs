using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Shiakati.Services.Interfaces;
using Shiakati.Views;

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

        public MainViewModel(IAuthenticationClientService authService, PosContainerViewModel posContainer, StockViewModel stockViewModel, SalesHistoryViewModel salesHistory,SettingsViewModel settingsViewModel )
        {
            _authService = authService;
            PosContainer = posContainer;
            Stock = stockViewModel;
            SalesHistory = salesHistory;
            Settings = settingsViewModel;
            // Vue par défaut au démarrage
            CurrentView = PosContainer;
            
        }
        // ✅ On assigne le ViewModel, pas la View !
        [RelayCommand]
        private void NavigateToStock() => CurrentView = Stock;        

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
