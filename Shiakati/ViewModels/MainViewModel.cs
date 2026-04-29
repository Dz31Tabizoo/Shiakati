using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Views;

namespace Shiakati.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;

        // On garde les instances des ViewModels pour ne pas les recréer à chaque clic
        public PosContainerViewModel PosContainer { get; }
        public StockViewModel Stock { get; }

        public MainViewModel(PosContainerViewModel posContainer, StockViewModel stockViewModel)
        {
            PosContainer = posContainer;
            Stock = stockViewModel;

            // Vue par défaut au démarrage
            CurrentView = PosContainer;
        }

        [RelayCommand]
        private void NavigateToStock()
        {
            // ✅ On assigne le ViewModel, pas la View !
            CurrentView = Stock;
        }

        [RelayCommand]
        private void NavigateToPOS()
        {
            CurrentView = PosContainer;
        }

        [RelayCommand]
        private void NavigateToSettings()
        {
            // CurrentView = Settings;
        }
    }
}
