using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Shiakati.ViewModels
{
    public partial class PosContainerViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        public ObservableCollection<POSViewModel> PosTabs { get; set; } = new();

        [ObservableProperty]
        private POSViewModel _selectedTab;

        private int _tabCounter = 1;

        public PosContainerViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            AddNewTab();
        }
        [RelayCommand]
        private void AddNewTab()
        {
            string tabName = $"Client #{_tabCounter}";
            var newTab = ActivatorUtilities.CreateInstance<POSViewModel>(_serviceProvider, tabName);
            PosTabs.Add(newTab);
            SelectedTab = newTab;
            _tabCounter++;
        }

        [RelayCommand]
        private void CloseTab(POSViewModel tabToClose)
        {
            if (tabToClose != null && PosTabs.Contains(tabToClose))
            {
                PosTabs.Remove(tabToClose);
                if (SelectedTab == tabToClose)
                {
                    SelectedTab = PosTabs.Count > 0 ? PosTabs[0] : default!;
                }
            }
        }
    }
}
