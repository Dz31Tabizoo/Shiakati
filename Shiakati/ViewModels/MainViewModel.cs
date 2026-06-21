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
using System.Windows.Threading;

namespace Shiakati.ViewModels
{
    public partial class MainViewModel : ObservableObject, IRecipient<StockAlertCountMessage>
    {
        private readonly IAuthenticationClientService _authService;
        private readonly ICacheService _cacheService;

        [ObservableProperty] private object? _currentView;
        [ObservableProperty] private string? _currentViewTitle;
        [ObservableProperty] private string? _toDay;
        [ObservableProperty] private int _alertBadgeCount;

        private DispatcherTimer? _pulseTimer;

        [ObservableProperty] private bool _isAlertActive;
        [ObservableProperty] private bool _isAlertPulsing;
        [ObservableProperty] private bool _isAlertStatic;

        // ViewModels (injected)
        public SalesHistoryViewModel SalesHistory { get; }
        public PosContainerViewModel PosContainer { get; }
        public ReservationsViewModel ReservationsVM { get; }
        public StockMovementsViewModel StockMovements { get; }
        public StockViewModel Stock { get; }
        public DashBordViewModel DashBord { get; }
        public ClientListViewModel ClientList { get; }
        public SettingsViewModel Settings { get; }

        [ObservableProperty] private string _userName;

        public MainViewModel(
            ICacheService cacheService,
            IAuthenticationClientService authService,
            PosContainerViewModel posContainer,
            StockViewModel stockViewModel,
            SalesHistoryViewModel salesHistory,
            SettingsViewModel settingsViewModel,
            StockMovementsViewModel stockMovementsViewModel,
            ClientListViewModel clientList,
            ReservationsViewModel reservationsViewModel,
            DashBordViewModel dashBordViewModel)
        {
            _authService = authService;
            UserName = _authService.CurrentSession?.UserName ?? "Utilisateur";
            _authService.OnAuthenticationStateChanged += OnAuthStateChanged;

            _cacheService = cacheService;

            ReservationsVM = reservationsViewModel;
            StockMovements = stockMovementsViewModel;
            PosContainer = posContainer;
            Stock = stockViewModel;
            DashBord = dashBordViewModel;
            SalesHistory = salesHistory;
            Settings = settingsViewModel;
            ClientList = clientList;

            CurrentView = PosContainer;
            CurrentViewTitle = "Point de Vente";

            // Timer: 60 seconds, then stop pulsing
            _pulseTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
            _pulseTimer.Tick += (s, e) =>
            {
                _pulseTimer.Stop();
                IsAlertPulsing = false;
                UpdateAlertStates();
            };

            LoadUserData();

            // Register for messages
            WeakReferenceMessenger.Default.Register<StockAlertCountMessage>(this);
            WeakReferenceMessenger.Default.Register<NavigateToPosMessage>(this, (r, m) =>
            {
                CurrentView = PosContainer;
            });

            // Initial state
            AlertBadgeCount = 0;
            IsAlertActive = false;
            IsAlertPulsing = false;
            IsAlertStatic = false;

            
        }

        private void LoadUserData()
        {
            var session = _authService.CurrentSession;
            if (session != null)
            {
                UserName = session.UserName;
            }
            ToDay = DateTime.Now.ToString("D");
        }

        // Called whenever a new alert count arrives
        public void Receive(StockAlertCountMessage message)
        {
            AlertBadgeCount = message.Count;

            if (AlertBadgeCount > 0)
            {
                IsAlertActive = true;

                // ✅ Reset timer on each new alert – pulse for 60 seconds from now
                if (_pulseTimer != null)
                {
                    _pulseTimer.Stop();
                    _pulseTimer.Start();
                }
                IsAlertPulsing = true;
            }
            else
            {
                IsAlertActive = false;
                IsAlertPulsing = false;
                _pulseTimer?.Stop();
            }

            UpdateAlertStates();
        }

        private void UpdateAlertStates()
        {
            // Static = active but not pulsing (after 60 seconds)
            IsAlertStatic = IsAlertActive && !IsAlertPulsing;
        }

        // ===== Navigation Commands =====
        [RelayCommand]
        private async Task NavigateToStock()
        {
            if(_authService.CurrentSession?.Role != "admin" && _authService.CurrentSession?.Role != "owner")
                return;

            CurrentView = Stock;
            CurrentViewTitle = "Stock";
            await Stock.LoadInitialDataAsync(true);
        }

        [RelayCommand]
        private void NavigateToPOS()
        {
            if (CurrentView == PosContainer) return;
            CurrentView = PosContainer;
            CurrentViewTitle = "Point de Vente";
            PosContainer.SelectedTab?.LoadProductsAsync();
        }

        [RelayCommand]
        private void NavigateToSalesHistory()
        {
            CurrentView = SalesHistory;
            CurrentViewTitle = "Historique des Ventes";
        }

        [RelayCommand]
        private void NavigateToDashBord()
        {
            if (_authService.CurrentSession?.Role != "admin" && _authService.CurrentSession?.Role != "owner")
                return;

            CurrentView = DashBord;
            CurrentViewTitle = "Tableau de Bord";
        }

        [RelayCommand]
        private void ExitApplication()
        {
            App.Current.Shutdown();
        }

        [RelayCommand]
        private void NavToSettings()
        {
            CurrentView = Settings;
            CurrentViewTitle = "Paramètres";
        }

        [RelayCommand]
        private void Logout()
        {
            _authService.Logout();
            UserName = null;
            var loginView = App.ServiceProvider?.GetRequiredService<LoginView>();
            if (loginView != null)
            {
                loginView.LoginSucceeded += () =>
                {
                    App.Current.MainWindow?.Show();
                    
                };
                App.Current.MainWindow?.Hide();
                loginView.Show();
            }
        }

        [RelayCommand]
        private void NavigateToStockMovements()
        {
            CurrentView = StockMovements;
            CurrentViewTitle = "Mouvements de Stock";
        }

        [RelayCommand]
        private void NavigateToClients()
        {
            CurrentView = ClientList;
            CurrentViewTitle = "Clients details";
        }

        [RelayCommand]
        private void CloseCurrentView()
        {
            CurrentView = null;
            CurrentViewTitle = null;
        }

        [RelayCommand] 
        private void NavigateToReservations() => CurrentView = ReservationsVM;

        private void OnAuthStateChanged()
        {
            UserName = _authService.CurrentSession?.UserName ?? "Utilisateur";
        }
    }
}
