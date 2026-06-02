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
        [ObservableProperty] private ObservableCollection<DailyBonusModel> _dailyBonuses = new();
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
        private async Task QuickSetPeriod(string period)
        {
            DateTime today = DateTime.Today;
            switch (period)
            {
                case "today":
                    StartDate = today;
                    EndDate = today;
                    break;
                case "yesterday":
                    StartDate = today.AddDays(-1);
                    EndDate = today.AddDays(-1);
                    break;
                case "lastweek":
                    StartDate = today.AddDays(-(int)today.DayOfWeek + 1 - 7);
                    EndDate = StartDate.Value.AddDays(6);
                    break;
                case "lastmonth":
                    var firstDayLastMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-1);
                    StartDate = firstDayLastMonth;
                    EndDate = firstDayLastMonth.AddMonths(1).AddDays(-1);
                    break;
                case "year":
                    StartDate = new DateTime(today.Year, 1, 1);
                    EndDate = new DateTime(today.Year, 12, 31);
                    break;
                case "all":
                    StartDate = null;
                    EndDate = null;
                    break;
            }
            await LoadSalesAsync();
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
                SaleItemID = i.SaleItemId,   // important for updating existing items
                VariantID = i.VariantId,
                Quantity = i.Quantity,
                DiscountAmount = i.DiscountAmount
            }).ToList();

            // 1. Navigate to the POS container (makes it visible)
            WeakReferenceMessenger.Default.Send(new NavigateToPosMessage());

            // 2. Send the edit data – it will be received by the active POS tab
            WeakReferenceMessenger.Default.Send(new EditSaleMessage(
                new SaleModel { SaleID = sale.SaleId, TicketNumber = sale.TicketNumber },
                items));
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

        [RelayCommand]
        private void CalculateDailyBonuses()
        {
            // 1. Only consider non‑voided sales
            var nonVoided = Sales.Where(s => !s.IsVoided).ToList();

            if (nonVoided.Count == 0)
            {
                MessageBox.Show("Aucune vente valide à analyser.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2. Group by date (ignoring time)
            var dailyGroups = nonVoided
                .Where(s => s.SaleDate.HasValue)
                .GroupBy(s => s.SaleDate!.Value.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    TotalSales = g.Sum(s => s.TotalAmount ?? 0)
                })
                .OrderBy(g => g.Date);

            // 3. Apply bonus tiers
            var bonusList = new List<DailyBonusModel>();
            foreach (var day in dailyGroups)
            {
                decimal percentage = 0;
                if (day.TotalSales >= 20000 && day.TotalSales < 50000)
                    percentage = 2m;
                else if (day.TotalSales >= 50000 && day.TotalSales <= 100000)
                    percentage = 2.5m;
                else if (day.TotalSales > 100000)
                    percentage = 3m;

                bonusList.Add(new DailyBonusModel
                {
                    Date = day.Date,
                    TotalSales = day.TotalSales,
                    BonusPercentage = percentage,
                    BonusAmount = day.TotalSales * percentage / 100
                });
            }

            DailyBonuses = new ObservableCollection<DailyBonusModel>(bonusList);
        }
    }
}

