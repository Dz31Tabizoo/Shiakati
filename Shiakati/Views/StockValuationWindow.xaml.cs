using Shiakati.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Shiakati.Services;
using Shiakati.Services.Interfaces;

namespace Shiakati.Views
{
    public partial class StockValuationWindow : Window, INotifyPropertyChanged
    {
        private readonly IProductVariantsService _stockService;

        private ObservableCollection<CategoryStockDto> _categories = new();
        public ObservableCollection<CategoryStockDto> Categories
        {
            get => _categories;
            set { _categories = value; OnPropertyChanged(); }
        }

        public StockValuationWindow(IProductVariantsService stockService)
        {
            InitializeComponent();
            _stockService = stockService;
            DataContext = this; // Bind to itself

            Loaded += async (s, e) => await LoadDataAsync();
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var data = await _stockService.GetStockValuationAsync();
                Categories.Clear();
                foreach (var cat in data.Categories)
                    Categories.Add(cat);

                // Update footer
                TotalQtyTextBlock.Text = data.TotalStockQuantity.ToString("N0");
                TotalPurchaseTextBlock.Text = data.TotalPurchaseValue.ToString("N2") + " DA";
                TotalSaleTextBlock.Text = data.TotalSaleValue.ToString("N2") + " DA";
                TotalMarginTextBlock.Text = data.TotalPotentialMargin.ToString("N2") + " DA";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur de chargement : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
