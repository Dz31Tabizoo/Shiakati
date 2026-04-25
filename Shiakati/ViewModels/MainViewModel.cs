using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Views;

namespace Shiakati.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private object _currentView;

        public PosContainerViewModel PosContainer { get; } 
        public MainViewModel( PosContainerViewModel posContainer)
        {
            PosContainer = posContainer;
            // set a default 
            //CurrentView = new DashboardViewModel();
        }
        [RelayCommand]
        private void NavigateToDashboard()
        {
            //CurrentView = new DashboardViewModel();
        }
         [RelayCommand]
        private void NavigateToSettings()
        {
            //CurrentView = new SettingsViewModel();
        } 
        [RelayCommand]
        private void NavigateToPOS()
        {
            CurrentView = this.PosContainer;
        }
    }
}
