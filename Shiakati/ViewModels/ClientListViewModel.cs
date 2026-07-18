using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using Shiakati.Services.Interfaces.DataServices;
using Shiakati.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Shiakati.ViewModels
{
    public partial class ClientListViewModel : ObservableObject
    {
        private CancellationTokenSource? _loadCts;

        private readonly IClientDataService _clientDataService;
        private readonly IServiceProvider _serviceProvider; // ← added

        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private ClientSummaryDto? _selectedClient;
        public ObservableCollection<ClientSummaryDto> Clients { get; } = new();
        private List<ClientSummaryDto> _allClients = new();

        // Constructor with IServiceProvider
        public ClientListViewModel(IClientDataService clientDataService, IServiceProvider serviceProvider)
        {
            _clientDataService = clientDataService;
            _serviceProvider = serviceProvider;
            _clientDataService.ClientsDataChanged += OnClientsDataChanged;
            _ = LoadClientsAsync();
        }

        [RelayCommand]
        private async Task LoadClientsAsync(CancellationToken token = default)
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                token.ThrowIfCancellationRequested();

                await _clientDataService.LoadClientsAsync();
                _allClients = _clientDataService.Clients.ToList();

                ApplyFilter();

            }
            catch (OperationCanceledException) { }
            finally { IsLoading = false; }
        }

        private void ApplyFilter()
        {
            var filteredClients = string.IsNullOrWhiteSpace(SearchText)
                ? _allClients
                : _allClients.Where(c =>
                    (c.FullName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (c.PhoneNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();

            Clients.Clear();
            foreach (var client in filteredClients)
                Clients.Add(client);
        }

        partial void OnSearchTextChanged(string value)
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();

            ApplyFilter();
        }

        [RelayCommand]
        private async Task AddNewClient()
        {
            
            var dialog = new ClientCreateDialog(); // not yet implemented – we can create it
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
                    var newClient = await _clientDataService.AddClientAsync(request);
                    if (newClient != null)
                    {
                        await LoadClientsAsync(); // refresh
                    }
                    else
                    {
                        MessageBox.Show("Ce numéro de téléphone existe déjà ou erreur serveur.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors de la création du client.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
            _ = LoadClientsAsync();
        }

        private async void OnClientsDataChanged()
        {
            await LoadClientsAsync();
        }
    }
}
