using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
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

        private readonly IClientService _clientService;
        private readonly IServiceProvider _serviceProvider; // ← added

        [ObservableProperty] private string _searchText = string.Empty;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private ClientSummaryDto? _selectedClient;
        public ObservableCollection<ClientSummaryDto> Clients { get; } = new();

        // Constructor with IServiceProvider
        public ClientListViewModel(IClientService clientService, IServiceProvider serviceProvider)
        {
            _clientService = clientService;
            _serviceProvider = serviceProvider;
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
                var list = await _clientService.GetClientSummariesAsync(SearchText);
                if (!token.IsCancellationRequested)
                {
                    Clients.Clear();
                    foreach (var c in list) Clients.Add(c);
                }
            }
            catch (OperationCanceledException) { }
            finally { IsLoading = false; }
        }

        partial void OnSearchTextChanged(string value)
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;
            _ = LoadClientsAsync(token);
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
                    var newClient = await _clientService.CreateClientAsync(request);
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
        }
    }
}
