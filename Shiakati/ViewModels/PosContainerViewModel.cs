using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Shiakati.ViewModels
{
    public partial class PosContainerViewModel : ObservableObject
    {
        public ObservableCollection<POSViewModel> PosTabs { get; set; } = new();

        [ObservableProperty]
        private POSViewModel _selectedTab;

        private int _tabCounter = 1;

        public PosContainerViewModel()
        {
            // Initialize with one tab
            AddNewTab();
        }
        [RelayCommand]
        private void AddNewTab()
        {
            var newTab = new POSViewModel($"Ticket #{_tabCounter++}");
            PosTabs.Add(newTab);
            SelectedTab = newTab;
        }

        [RelayCommand]
        private void CloseTab(POSViewModel tabToClose)
        {
            if (tabToClose != null && PosTabs.Contains(tabToClose))
            {
                PosTabs.Remove(tabToClose);
                if (SelectedTab == tabToClose)
                {
                    SelectedTab = PosTabs.Count > 0 ? PosTabs[0] : null;
                }
            }
        }
    }
}
