using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shiakati.Messages;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using Shiakati.Views;
using System;
using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace Shiakati.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IAuthenticationClientService _authService;
        private readonly ICacheService _cacheService;
        [ObservableProperty] private object? _currentView;
        [ObservableProperty] private string? _currentViewTitle;

        // On garde les instances des ViewModels pour ne pas les recréer à chaque click

        public SalesHistoryViewModel SalesHistory { get; }
        public PosContainerViewModel PosContainer { get; }

        public StockMovementsViewModel StockMovements { get; }
        public StockViewModel Stock { get; }

        public ClientListViewModel ClientList { get; }
        public SettingsViewModel Settings { get; }

        public MainViewModel(ICacheService cacheService,
                            IAuthenticationClientService authService,
                            PosContainerViewModel posContainer,
                            StockViewModel stockViewModel,
                            SalesHistoryViewModel salesHistory,
                            SettingsViewModel settingsViewModel,
                            StockMovementsViewModel stockMovementsViewModel,
                            ClientListViewModel clientList)
        {
            StockMovements = stockMovementsViewModel;
            _authService = authService;
            PosContainer = posContainer;
            Stock = stockViewModel;
            SalesHistory = salesHistory;
            Settings = settingsViewModel;
            // Vue par défaut au démarrage
            CurrentView = PosContainer;
            CurrentViewTitle = "Point de Vente";
            ClientList = clientList;

            WeakReferenceMessenger.Default.Register<NavigateToPosMessage>(this, (r, m) =>
            {
                CurrentView = PosContainer;
            });

        }

        [RelayCommand] private async Task NavigateToStock()
        {
            CurrentView = Stock;
            CurrentViewTitle = "Stock";
            await Stock.LoadInitialDataAsync(true);
        }
        [RelayCommand] private void NavigateToPOS()
        {
            if (CurrentView == PosContainer) return;
            CurrentView = PosContainer;
            CurrentViewTitle = "Point de Vente";
            PosContainer.SelectedTab?.LoadProductsAsync();
        }
        [RelayCommand] private void NavigateToSalesHistory()
        {
            CurrentView = SalesHistory;
            CurrentViewTitle = "Historique des Ventes";

        }
        [RelayCommand] private void ExitApplication()
        {
            // Logique pour fermer l'application
            System.Windows.Application.Current.Shutdown();
        }
        [RelayCommand] private void NavToSettings()
        {
            CurrentView = Settings;
            CurrentViewTitle = "Paramètres";

        }
        [RelayCommand] private void Logout()
        {
            _authService.Logout();

            var loginView = App.ServiceProvider?.GetRequiredService<LoginView>();

            loginView.LoginSucceeded += () =>
            {
                App.Current.MainWindow?.Show();
            };

            App.Current.MainWindow?.Hide();   // now hides the actual main menu
            loginView.Show();
        }
        [RelayCommand] private void NavigateToStockMovements()
        {
            CurrentView = StockMovements;
            CurrentViewTitle = "Mouvements de Stock";
        }
        [RelayCommand] private void NavigateToClients()
        {
            CurrentView = ClientList;
            CurrentViewTitle = "Clients C/V";
        }
        [RelayCommand] private void CloseCurrentView()
        {
            CurrentView = null;
            CurrentViewTitle = null;  // also hides the header bar
        }
    }
}
