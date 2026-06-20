using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Shiakati.Models;
using Shiakati.Services;
using Shiakati.Services.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LiveCharts;
using LiveCharts.Wpf;

namespace Shiakati.ViewModels
{
    public partial class DashBordViewModel : ObservableObject
    {
        private readonly IDashBordService _dashboardService;
        private CancellationTokenSource? _loadCts;
        private readonly IAuthenticationClientService authservice;

        // Chart properties
        [ObservableProperty] private SeriesCollection _dailySalesSeries;
        [ObservableProperty] private Func<object, string> _dateLabelFormatter;
        [ObservableProperty] private Func<double, string> _currencyFormatter;

        // KPIs
        [ObservableProperty] private decimal _totalRevenue;
        [ObservableProperty] private int _totalOrders;
        [ObservableProperty] private decimal _averageBasket;
        [ObservableProperty] private decimal _totalMargin;
        [ObservableProperty] private decimal _averageMarginPercentage;
        [ObservableProperty] private List<string> _dailyLabels;

        // Collections
        public ObservableCollection<TopProductDto> TopSellingProducts { get; } = new();
        public ObservableCollection<TopProductDto> TopProfitableProducts { get; } = new();
        public ObservableCollection<StockAlertDto> StockAlerts { get; } = new();
        public ObservableCollection<UserPerformanceDto> UserPerformances { get; } = new();
        public ObservableCollection<DailySalesTrendDto> DailyTrend { get; } = new();

        // Date filters
        [ObservableProperty] private DateTime? _startDate;
        [ObservableProperty] private DateTime? _endDate;

        // UI state
        [ObservableProperty] private bool _isLoading;

        public DashBordViewModel(IDashBordService dashboardService, IAuthenticationClientService authservice)
        {
            _dashboardService = dashboardService;
            this.authservice = authservice; // ✅ Fix order

            EndDate = DateTime.Today;
            StartDate = EndDate.Value.AddDays(-30);

            DateLabelFormatter = date => ((DateTime)date).ToString("dd/MM");
            CurrencyFormatter = value => value.ToString("N2") + " DA";

            _ = LoadDashboardAsync();
        }

        [RelayCommand]
        private async Task RefreshAsync() => await LoadDashboardAsync();

        [RelayCommand]
        private async Task AcknowledgeAlertAsync(int variantId)
        {
            if (IsLoading) return;
            var alert = StockAlerts.FirstOrDefault(a => a.VariantId == variantId);
            if (alert == null) return;

            var result = MessageBox.Show($"Confirmer l'arrêt de l'alerte pour {alert.Sku} ?", "Arrêter l'alerte", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                IsLoading = true;
                var success = await _dashboardService.AcknowledgeAlertAsync(variantId);
                if (success)
                    StockAlerts.Remove(alert);
                else
                    MessageBox.Show("Erreur lors de l'arrêt de l'alerte.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        partial void OnStartDateChanged(DateTime? value)
        {
            if (value.HasValue && EndDate.HasValue && value <= EndDate)
                TriggerReload();
        }

        partial void OnEndDateChanged(DateTime? value)
        {
            if (value.HasValue && StartDate.HasValue && value >= StartDate)
                TriggerReload();
        }

        private void TriggerReload()
        {
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            _ = LoadDashboardAsync(_loadCts.Token);
        }

        // ✅ FIXED: calls the correct method name from your service
        private async Task LoadDashboardAsync(CancellationToken token = default)
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                token.ThrowIfCancellationRequested();

                // Build the filter request
                var filter = new DashboardFilterRequest
                {
                    StartDate = StartDate,
                    EndDate = EndDate,
                    //UserId = authservice.CurrentSession?.UserId 
                };

                var data = await _dashboardService.GetDashBordDataAsync(filter); // 👈 CORRECT method name

                if (token.IsCancellationRequested) return;

                TotalRevenue = data.TotalRevenue;
                TotalOrders = data.TotalOrders;
                AverageBasket = data.AverageBasket;
                TotalMargin = data.TotalMargin;
                AverageMarginPercentage = data.AverageMarginPercentage;

                UpdateCollection(TopSellingProducts, data.TopSellingProducts);
                UpdateCollection(TopProfitableProducts, data.TopProfitableProducts);
                UpdateCollection(StockAlerts, data.StockAlerts);
                UpdateCollection(UserPerformances, data.UserPerformances);
                UpdateCollection(DailyTrend, data.DailyTrend);

                if (data.DailyTrend?.Any() == true)
                {
                    DailyLabels = data.DailyTrend.Select(d => d.Date.ToString("dd/MM")).ToList();
                    var series = new SeriesCollection
                        {
                            new LineSeries
                            {
                                Title = "CA",
                                Values = new ChartValues<decimal>(data.DailyTrend.Select(d => d.Revenue)),
                                PointGeometry = DefaultGeometries.Circle,
                                PointGeometrySize = 8,
                                StrokeThickness = 3
                            }
                        };
                    DailySalesSeries = series;
                }
                else
                {
                    DailySalesSeries = new SeriesCollection(); // empty
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private static void UpdateCollection<T>(ObservableCollection<T> target, List<T>? source)
        {
            target.Clear();
            if (source == null) return;
            foreach (var item in source) target.Add(item);
        }
    }
}