using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Shiakati.Helpers;
using Shiakati.Messages;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Shiakati.ViewModels
{
    public partial class SalesHistoryViewModel : ObservableObject
    {
        private readonly ISaleService _saleService;
        private readonly ILogger<SalesHistoryViewModel> _logger;

        [ObservableProperty] private string _searchTicketNumber = string.Empty;
        [ObservableProperty] private DateTime? _startDate = DateTime.Today;
        [ObservableProperty] private DateTime? _endDate = DateTime.Today;
        [ObservableProperty] private bool _isLoading;
        public ObservableCollection<SaleModel> Sales { get; } = new();

        public SalesHistoryViewModel(ISaleService saleService, ILogger<SalesHistoryViewModel> logger)
        {
            _saleService = saleService;
            _logger = logger;
            _ = LoadSalesAsync();
        }

        [RelayCommand]
        private async Task LoadSalesAsync()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                Sales.Clear();
                var results = await _saleService.GetSalesAsync(SearchTicketNumber, StartDate, EndDate);
                foreach (var s in results) Sales.Add(s);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur chargement historique ventes");
                MessageBox.Show("Impossible de charger l'historique.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            SearchTicketNumber = string.Empty;
            StartDate = DateTime.Today;
            EndDate = DateTime.Today;
            await LoadSalesAsync();
        }

        [RelayCommand]
        private async Task EditSale(SaleModel selectedSale)
        {
            if (selectedSale?.SaleID == null) return;
            var sale = await _saleService.GetSaleAsync(selectedSale.SaleID.Value);
            if (sale == null)
            {
                MessageBox.Show("Vente introuvable.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var items = sale.Items.Select(i => new SaleItemModel
            {
                VariantID = i.VariantId,
                Quantity = i.Quantity,
                DiscountAmount = i.DiscountAmount
            }).ToList();

            WeakReferenceMessenger.Default.Send(new EditSaleMessage(
                new SaleModel { SaleID = sale.SaleId, TicketNumber = sale.TicketNumber },
                items));
            WeakReferenceMessenger.Default.Send(new SwitchTabMessage("POS"));
        }

        [RelayCommand]
        private async Task VoidSaleAsync(SaleModel selectedSale)
        {
            if (selectedSale?.SaleID == null) return;
            var result = MessageBox.Show($"Annuler définitivement le ticket {selectedSale.TicketNumber} ?\nLes articles seront remis en stock.",
                                          "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            try
            {
                bool success = await _saleService.VoidSaleAsync(selectedSale.SaleID.Value);
                if (success)
                {
                    Sales.Remove(selectedSale);
                    MessageBox.Show("Vente annulée. Stock mis à jour.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur annulation vente");
                MessageBox.Show("Erreur lors de l'annulation.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

