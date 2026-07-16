using Shiakati.Models;
using Shiakati.Services.Interfaces;
using Shiakati.Services.Interfaces.DataServices;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Shiakati.Views
{
    public partial class AddInvoiceItemDialog : Window
    {
        private readonly IStockDataService _productVariantsService;
        private List<ProductVariantModel> _allVariants = new();

        public int SelectedVariantId => (int)VariantComboBox.SelectedValue;
        public int Quantity => int.TryParse(QuantityBox.Text, out int q) ? q : 0;
        public decimal? UnitCost => decimal.TryParse(UnitCostBox.Text, out decimal c) ? c : (decimal?)null;

        public string? Notes => NotesBox.Text; 

        public AddInvoiceItemDialog(IStockDataService productVariantsService)
        {
            InitializeComponent();
            _productVariantsService = productVariantsService;
            LoadVariants();
        }

        private async void LoadVariants()
        {
            await _productVariantsService.LoadVariantsAsync();
            var allVariants = _productVariantsService.Variants.ToList();

            // Get distinct variants by VariantId
            _allVariants = allVariants
                .GroupBy(v => v.VariantId)
                .Select(g => g.First())
                .OrderBy(v => v.ProductName)
                .ThenBy(v => v.Color)
                .ThenBy(v => v.FullSize)
                .ToList();

            // Initial population
            ApplyFilter("");
        }

        private void ApplyFilter(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                VariantComboBox.ItemsSource = _allVariants;
            }
            else
            {
                var filtered = _allVariants
                    .Where(v => v.ProductName?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                v.Color?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                v.FullSize?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                v.Sku?.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
                VariantComboBox.ItemsSource = filtered;
            }

            if (VariantComboBox.Items.Count > 0)
                VariantComboBox.SelectedIndex = 0;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(SearchBox.Text);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedVariantId == 0)
            {
                MessageBox.Show("Veuillez sélectionner un variant.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (Quantity <= 0)
            {
                MessageBox.Show("La quantité doit être supérieure à 0.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}
