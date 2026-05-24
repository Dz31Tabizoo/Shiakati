using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;

namespace Shiakati.ViewModels
{
    public partial class StockMovementsViewModel : ObservableObject
    {
        private readonly IStockMovementService _movementService;
        private readonly ILogger<StockMovementsViewModel> _logger;

        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private DateTime? _startDate = DateTime.Today.AddMonths(-1);
        [ObservableProperty] private DateTime? _endDate = DateTime.Today;

        public ObservableCollection<StockMovementModel> Movements { get; } = new();

        public StockMovementsViewModel(IStockMovementService movementService, ILogger<StockMovementsViewModel> logger)
        {
            _movementService = movementService;
            _logger = logger;
            _ = LoadMovementsAsync();
        }

        [RelayCommand]
        private async Task LoadMovementsAsync()
        {
            if (IsLoading) return;
            try
            {
                IsLoading = true;
                Movements.Clear();
                var results = await _movementService.GetMovementsAsync(StartDate, EndDate);
                foreach (var m in results) Movements.Add(m);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur chargement mouvements de stock");
                MessageBox.Show("Impossible de charger les mouvements.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
            await LoadMovementsAsync();
        }

        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            StartDate = DateTime.Today.AddMonths(-1);
            EndDate = DateTime.Today;
            await LoadMovementsAsync();
        }
    }
}