using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Views;

namespace Shiakati.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;

        // On garde les instances des ViewModels pour ne pas les recréer à chaque click

        public SalesHistoryViewModel SalesHistory { get; }
        public PosContainerViewModel PosContainer { get; }
        public StockViewModel Stock { get; }

        public MainViewModel(PosContainerViewModel posContainer, StockViewModel stockViewModel, SalesHistoryViewModel salesHistory)
        {
            PosContainer = posContainer;
            Stock = stockViewModel;
            SalesHistory = salesHistory;

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
        
    }
}
