using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Shiakati.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;

        public MainViewModel()
        {
            // set a default 
            //CurrentView = new DashboardViewModel();
        }
        [RelayCommand]
        private void NavigateToDashboard()
        {
            CurrentView = new DashboardViewModel();
        }
         [RelayCommand]
        private void NavigateToSettings()
        {
            CurrentView = new SettingsViewModel();
        } 
        [RelayCommand]
        private void NavigateToPOS()
        {
            CurrentView = new POSViewModel();
        }
    }
}
