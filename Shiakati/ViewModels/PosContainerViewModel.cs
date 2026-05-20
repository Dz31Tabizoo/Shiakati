using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Shiakati.ViewModels;
using System.Collections.ObjectModel;

namespace Shiakati.ViewModels
{

    public partial class PosContainerViewModel : ObservableObject
    {
        private readonly IServiceProvider _serviceProvider;
        public ObservableCollection<POSViewModel> PosTabs { get; } = new();

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
            // Professional tab name – sequential, simple, and clear
            string tabName = $"Vente #{_tabCounter}";
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
                SelectedTab = PosTabs.Count > 0 ? PosTabs[0] : null!;
            }
        }
    }
}