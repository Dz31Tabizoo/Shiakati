using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LiveCharts;
using LiveCharts.Wpf;
using Shiakati.Messages;
using Shiakati.Models;
using Shiakati.Services;
using Shiakati.Services.Interfaces.APIServices;
using Shiakati.Services.Interfaces.DataServices;
using Shiakati.Views;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Shiakati.ViewModels
{
    public partial class DashBordViewModel : ObservableObject, IDisposable
    {
        private readonly ILogger<DashBordViewModel> _logger;
        private readonly IDashBordDataService _dashboardDataService;
        private CancellationTokenSource? _loadCts;
        private readonly IAuthenticationClientService authservice;
        private readonly IStockDataService _stockService;

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

        public DashBordViewModel(IDashBordDataService dashboardDataService,ILogger<DashBordViewModel> logger , IStockDataService stockService, IAuthenticationClientService authservice)
        {
            _dashboardDataService = dashboardDataService;
            this.authservice = authservice; // ✅ Fix order
            _stockService = stockService;
            _logger = logger;

            EndDate = DateTime.Today;
            StartDate = EndDate.Value.AddDays(-30);

            DateLabelFormatter = date => ((DateTime)date).ToString("dd/MM");
            CurrencyFormatter = value => value.ToString("N2") + " DA";

            _dashboardDataService.DashBordDataChanged += OnDashBordDataChanged;

            _ = LoadDashboardAsync();
        }


        [RelayCommand]
        private void OpenStockValuation()
        {
            try
            {
                var window = new StockValuationWindow(_stockService)
                {
                    Owner = Application.Current.MainWindow
                };
                window.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de l'état du stock : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                var success = await _dashboardDataService.AcknowledgeAlertAsync(variantId);
                if (success)
                {
                    StockAlerts.Remove(alert);
                    UpdateAlertCount();
                }
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

                var data = await _dashboardDataService.GetDashboardDataAsync(filter); // 👈 CORRECT method name

                if (token.IsCancellationRequested) return;

                TotalRevenue = data.TotalRevenue;
                TotalOrders = data.TotalOrders;
                AverageBasket = data.AverageBasket;
                TotalMargin = data.TotalMargin;
                AverageMarginPercentage = data.AverageMarginPercentage;

                UpdateCollection(TopSellingProducts, data.TopSellingProducts);
                UpdateCollection(TopProfitableProducts, data.TopProfitableProducts);
                UpdateCollection(StockAlerts, data.StockAlerts);
                UpdateAlertCount();
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

        //message 
        private void UpdateAlertCount()
        {
            int count = StockAlerts.Count;
            WeakReferenceMessenger.Default.Send(new StockAlertCountMessage(count));
        }

        private async void OnDashBordDataChanged()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() => LoadDashboardAsync());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du tableau de bord suite à un changement de données.");
                MessageBox.Show($"Erreur lors de la mise à jour du tableau de bord : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            _dashboardDataService.DashBordDataChanged -= OnDashBordDataChanged;
        }
    }
}