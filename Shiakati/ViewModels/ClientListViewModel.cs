using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using Shiakati.Services.Interfaces.DataServices;
using Shiakati.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Shiakati.ViewModels
{
    public partial class ClientListViewModel : ObservableObject, IDisposable
    {
        private readonly IClientDataService _clientDataService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ClientListViewModel> _logger;

        // ✅ Bind to Data Service's collection
        public ICollectionView FilteredClientsView { get; }

        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private ClientSummaryDto? _selectedClient;

        public ClientListViewModel(IClientDataService clientDataService, IServiceProvider serviceProvider, ILogger<ClientListViewModel> logger)
        {
            _clientDataService = clientDataService;
            _serviceProvider = serviceProvider;
            _logger = logger;

            // Create the filtered view over the Data Service's collection
            FilteredClientsView = CollectionViewSource.GetDefaultView(_clientDataService.Clients);
            FilteredClientsView.Filter = ClientFilter;

            _clientDataService.ClientsDataChanged += OnClientsDataChanged;
            _ = LoadClientsAsync();
        }

        private bool ClientFilter(object obj)
        {
            if (obj is not ClientSummaryDto c) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            return c.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                   c.PhoneNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true;
        }

        partial void OnSearchTextChanged(string value)
        {
            FilteredClientsView.Refresh();
        }

        [RelayCommand]
        private async Task LoadClientsAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                await _clientDataService.LoadClientsAsync();
                FilteredClientsView.Refresh(); // apply current filter
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur chargement clients");
                MessageBox.Show("Impossible de charger les clients.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task AddNewClient()
        {
            var dialog = new ClientCreateDialog();
            if (dialog.ShowDialog() == true)
            {
                var request = new CreateClientRequest
                {
                    FullName = dialog.FullName,
                    PhoneNumber = dialog.PhoneNumber,
                    Address = dialog.Address,
                    Email = dialog.Email
                };
                try
                {
                    await _clientDataService.AddClientAsync(request);
                    // UI updates automatically because FilteredClientsView is bound to the Data Service's collection
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private void OpenClientDetail(ClientSummaryDto selectedClient)
        {
            if (selectedClient == null) return;
            var detailVm = _serviceProvider.GetRequiredService<ClientDetailViewModel>();
            var detailView = _serviceProvider.GetRequiredService<ClientDetailView>();
            detailView.DataContext = detailVm;
            _ = detailVm.LoadClientAsync(selectedClient.ClientId);

            var window = new Window
            {
                Content = detailView,
                Title = $"Client : {selectedClient.FullName}",
                Width = 900,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };
            window.ShowDialog();
            // After closing, refresh if needed
            _ = LoadClientsAsync();
        }

        private async void OnClientsDataChanged()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() => LoadClientsAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour des clients");
                MessageBox.Show("Impossible de mettre à jour la liste.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            _clientDataService.ClientsDataChanged -= OnClientsDataChanged;
        }
    }
}
