using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Shiakati.Models;
using Shiakati.Services.Interfaces;

namespace Shiakati.Views
{
    public partial class ClientSelectorDialog : Window
    {
        private readonly IClientService _clientService;
        private List<ClientSummaryDto> _allClients = new();
        public ClientSummaryDto? SelectedClient { get; private set; }

        public ClientSelectorDialog(IClientService clientService)
        {
            InitializeComponent();
            _clientService = clientService;
            Loaded += async (s, e) => await LoadClientsAsync();
        }

        private async Task LoadClientsAsync(string? search = null)
        {
            _allClients = await _clientService.GetClientSummariesAsync(search);
            ClientsGrid.ItemsSource = _allClients;
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadClientsAsync(SearchTextBox.Text);
        }

        private void ClientsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (ClientsGrid.SelectedItem is ClientSummaryDto selected)
            {
                SelectedClient = selected;
                DialogResult = true;
                Close();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (ClientsGrid.SelectedItem is ClientSummaryDto selected)
            {
                SelectedClient = selected;
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client dans la liste.", "Aucun client sélectionné", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}