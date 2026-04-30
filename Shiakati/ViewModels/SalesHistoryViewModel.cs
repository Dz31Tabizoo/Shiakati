using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shiakati.ViewModels
{
    public partial class SalesHistoryViewModel : ObservableObject
    {
        private readonly IHistoryService _historyService;

        private readonly ILogger<SalesHistoryViewModel> _logger;

        [ObservableProperty]
        private string _searchTicketNumber = string.Empty;

        [ObservableProperty]
        private DateTime? _startDate;

        [ObservableProperty]
        private DateTime? _endDate;

        [ObservableProperty]
        private bool _isLoading;

        public ObservableCollection<SaleModel> Sales { get; } = new();

        public SalesHistoryViewModel()
        {
            //_historyService = historyService;
            //_logger = logger;

            // Set default date to today to avoid loading 10 years of history at startup
            StartDate = DateTime.Today;
            EndDate = DateTime.Today;

            _ = LoadSalesAsync();
        }

        // A barcode/QR scanner acts like a keyboard. 
        // We can trigger search automatically when text changes (with debounce) or via a button.
        async partial void OnSearchTicketNumberChanged(string value)
        {
            // If scanner is fast, you might want a 500ms debounce here like we did for POS
            //if (value.Length > 7) // Assuming ticket numbers are at least 4 chars
            //{
            //    await LoadSalesAsync();
            //}

            Sales.Select(X => X.TicketNumber == value);
        }

        //fake load for testing the UI without backend
        public void LoadFakeSales()
        {
            Sales.Clear();
            var random = new Random();

            for (int i = 1; i <= 20; i++)
            {
                // On génère des dates étalées sur le dernier mois
                DateTime fakeDate = DateTime.Now.AddDays(-random.Next(0, 30))
                                               .AddHours(-random.Next(1, 12));

                Sales.Add(new SaleModel
                {
                    SaleID = i,
                    // Format de ticket réaliste pour tester le scanner QR/Code-barres
                    TicketNumber = $"TK-{fakeDate:yyyyMMdd}-{random.Next(100, 999)}",
                    SaleDate = fakeDate,
                    TotalAmount = (decimal)(random.NextDouble() * (15000 - 1000) + 1000), // Entre 1000 et 15000 DA
                    UserID = 1,
                    GlobalDiscount = random.Next(0, 5) == 0 ? 500 : 0 // Une remise de 500 DA une fois sur cinq
                });
            }

            // Optionnel : Trier par date la plus récente
            var sorted = Sales.OrderByDescending(s => s.SaleDate).ToList();
            Sales.Clear();
            foreach (var sale in sorted) Sales.Add(sale);
        }



        [RelayCommand]
        private async Task LoadSalesAsync()
        {
            //if (IsLoading) return;

            //try
            //{
            //    IsLoading = true;
            //    Sales.Clear();

            //    var results = await _historyService.GetSalesAsync(SearchTicketNumber, StartDate, EndDate);

            //    foreach (var sale in results)
            //    {
            //        Sales.Add(sale);
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "Error loading sales history");
            //    // TODO: Show Error Snackbar/MessageBox
            //}
            //finally
            //{
            //    IsLoading = false;
            //}

                LoadFakeSales();
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
        private void ViewSaleDetails(SaleModel selectedSale)
        {
            if (selectedSale == null) return;
            // TODO: Open a dialog or navigate to a details view where they can exchange items
        }

        [RelayCommand]
        private async Task VoidSaleAsync(SaleModel selectedSale)
        {
            if (selectedSale == null || selectedSale.SaleID == null) return;

            // TODO: Add a MessageBox confirmation here! "Are you sure you want to refund this ticket?"

           // bool success = await _historyService.VoidSaleAsync(selectedSale.SaleID.Value);
            //if (success)
            //{
            //    Sales.Remove(selectedSale);
            //}
        }
    }
}

