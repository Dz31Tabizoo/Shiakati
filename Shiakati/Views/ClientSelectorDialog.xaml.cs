using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using Shiakati.Services.Interfaces.DataServices;

namespace Shiakati.Views
{
    public partial class ClientSelectorDialog : Window
    {
        private readonly IClientDataService _clientDataService;
        private List<ClientSummaryDto> _allClients = new();
        public ClientSummaryDto? SelectedClient { get; private set; }

        public ClientSelectorDialog(IClientDataService clientDataService)
        {
            InitializeComponent();
            _clientDataService = clientDataService;
            Loaded += async (s, e) => await LoadClientsAsync();
        }

        private async Task LoadClientsAsync(string? search = null)
        {
            await _clientDataService.LoadClientsAsync();
            _allClients = _clientDataService.Clients.ToList();

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            string search = SearchTextBox?.Text ?? string.Empty;

            var filtered = string.IsNullOrWhiteSpace(search)
                ? _allClients
                : _allClients
                    .Where(c =>
                        (c.FullName?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                        (c.PhoneNumber?.Contains(search, StringComparison.OrdinalIgnoreCase) == true))
                    .ToList();

            ClientsGrid.ItemsSource = filtered;
        }

        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
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
                MessageBox.Show("Veuillez sélectionner un client dans la liste.",
                "Aucun client sélectionné", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}