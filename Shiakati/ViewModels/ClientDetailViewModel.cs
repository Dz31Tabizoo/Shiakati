using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    public partial class ClientDetailViewModel : ObservableObject
    {
        private readonly IClientService _clientService;
        private int _clientId;

        [ObservableProperty] private ClientDetailDto? _client;
        [ObservableProperty] private bool _isLoading;

        [ObservableProperty] private decimal _totalCreditsAvailable;
        [ObservableProperty] private decimal _totalPayments;


        

        // Credits tab
        public ObservableCollection<CreditDto> Credits { get; } = new();
        // Versements tab
        public ObservableCollection<VersementDto> Versements { get; } = new();

        // Form fields
        [ObservableProperty] private decimal _creditAmount;
        [ObservableProperty] private string? _creditNotes;
        [ObservableProperty] private decimal _versementAmount;
        [ObservableProperty] private string? _versementNotes;

        public ClientDetailViewModel(IClientService clientService)
        {
            _clientService = clientService;
        }

        public async Task LoadClientAsync(int clientId)
        {
            _clientId = clientId;
            await LoadAsync();
        }

        private async Task LoadAsync()
        {
            IsLoading = true;
            try
            {
                var detail = await _clientService.GetClientDetailAsync(_clientId);
                if (detail != null)
                {
                    Client = detail;
                    Credits.Clear();
                    foreach (var c in detail.Credits) Credits.Add(c);
                    Versements.Clear();
                    foreach (var v in detail.Versements) Versements.Add(v);
                }
                TotalCreditsAvailable = Credits.Where(c => !c.IsRedeemed).Sum(c => c.Amount);
                TotalPayments = Versements.Sum(v => v.Amount);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task EditClient()
        {
            if (Client == null) return;
            // Open edit dialog (similar to create)
            var dialog = new ClientEditDialog(Client.FullName, Client.PhoneNumber, Client.Address, Client.Email);
            if (dialog.ShowDialog() == true)
            {
                var request = new CreateClientRequest
                {
                    FullName = dialog.FullName,
                    PhoneNumber = dialog.PhoneNumber,
                    Address = dialog.Address,
                    Email = dialog.Email
                };
                var success = await _clientService.UpdateClientAsync(_clientId, request);
                if (success)
                {
                    await LoadAsync(); // refresh detail
                }
                else
                {
                    MessageBox.Show("Erreur lors de la mise à jour.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task GrantCreditAsync()
        {
            if (CreditAmount <= 0) return;
            var request = new CreateCreditRequest { ClientId = _clientId, Amount = CreditAmount, Notes = CreditNotes };
            var success = await _clientService.GrantCreditAsync(request);
            if (success)
            {
                CreditAmount = 0; CreditNotes = null;
                await LoadAsync(); // refresh
            }
            else MessageBox.Show("Erreur lors de l'ajout du crédit.");
        }

        [RelayCommand]
        private async Task AddVersementAsync()
        {
            if (VersementAmount <= 0) return;
            var request = new CreateVersementRequest { ClientId = _clientId, Amount = VersementAmount, Notes = VersementNotes };
            var success = await _clientService.AddVersementAsync(request);
            if (success)
            {
                VersementAmount = 0; VersementNotes = null;
                await LoadAsync();
            }
            else MessageBox.Show("Erreur lors de l'enregistrement du versement.");
        }
    }
}

