using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Shiakati.Models;
using System.Collections.ObjectModel;
using System.Windows;

namespace Shiakati.ViewModels
{
    public partial class POSViewModel : ObservableObject
    {
        private readonly ILogger<POSViewModel> _logger;

       

       

        [ObservableProperty]
        private string _tabName;

        [ObservableProperty]
        private string _searchText = string.Empty;

        // ON CHANGE ICI : On utilise ProductVariantsModel
        private List<ProductVariantsModel> _allProducts = new();

        [ObservableProperty]
        private ObservableCollection<ProductVariantsModel> _filteredProducts = new();

        // Le Panier utilise notre nouveau CartItem
        public ObservableCollection<CartItem> CartItems { get; } = new();


        public POSViewModel(string name, ILogger<POSViewModel> logger)
        {
            TabName = name;
            _logger = logger;

            LoadFakeProducts();

            CartItems.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CartTotal));
        }

        //need async + await 500 ms delay to not query the API on every keystroke
        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredProducts = new ObservableCollection<ProductVariantsModel>(_allProducts);
                return;
            }

            var filtered = _allProducts.Where(p =>
                (p.ProductInfo?.ProductName?.Contains(value, StringComparison.OrdinalIgnoreCase) == true) ||
                (p.SKU != null && p.SKU.Equals(value, StringComparison.OrdinalIgnoreCase))).ToList();

            FilteredProducts = new ObservableCollection<ProductVariantsModel>(filtered);
        }

        public decimal CartSubTotal => CartItems.Sum(x => x.RawTotal);

        // Somme de toutes les remises (fixes et manuelles)
        public decimal TotalDiscountAmount => CartItems.Sum(x => x.TotalLineDiscount);

        // Le montant final à encaisser
        public decimal CartTotal => CartSubTotal - TotalDiscountAmount;
        private void UpdateCartTotal()
        {
            OnPropertyChanged(nameof(CartSubTotal));
            OnPropertyChanged(nameof(TotalDiscountAmount));
            OnPropertyChanged(nameof(CartTotal));
        }

        [RelayCommand]
        private void AddToCart(ProductVariantsModel selectedVariant)
        {
            if (selectedVariant == null) return;

            var existingItem = CartItems.FirstOrDefault(c => c.Variant.VariantID == selectedVariant.VariantID);

            if (existingItem != null)
            {
                existingItem.Quantity++;
                UpdateCartTotal(); // On met à jour le total global
            }
            else
            {
                // On passe notre méthode UpdateCartTotal en tant que Callback !
                CartItems.Add(new CartItem(selectedVariant, selectedVariant.ProductInfo, UpdateCartTotal));
            }

            SearchText = string.Empty;
        }

        [RelayCommand]
        private void RemoveFromCart(CartItem itemToRemove)
        {
            if (itemToRemove != null)
            {
                CartItems.Remove(itemToRemove);
                UpdateCartTotal();
            }
        }

        [RelayCommand]
        private void IncrementQty(CartItem item)
        {
            if (item != null)
            {
                item.Quantity++;
                UpdateCartTotal();
            }
        }

        [RelayCommand]
        private void DecrementQty(CartItem item)
        {
            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                    UpdateCartTotal();
                }
                else
                {
                    RemoveFromCart(item);
                }
            }
        }


        [RelayCommand]
        private void Checkout()
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Le panier est vide !", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"Vente validée pour un total de {CartTotal:N2} DA.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            CartItems.Clear();
        }

        public void LoadFakeProducts()
        {
            // On simule ce que l'API/Dapper va nous renvoyer (La jointure)
            var parentProd1 = new ProductModel { ProductID = 1, ProductName = "ثوب أعمال برايم – أبيض – أكمام سادة, قلاب" };
            var parentProd2 = new ProductModel { ProductID = 2, ProductName = "Parfum Oud Royal" };

            _allProducts = new List<ProductVariantsModel>
        {
            new ProductVariantsModel { VariantID = 1, ProductID = 1, SKU = "123456", SalePrice = 4500, FullSize = "L", Color = "Blanc", ProductInfo = parentProd1 },
            new ProductVariantsModel { VariantID = 2, ProductID = 1, SKU = "123457", SalePrice = 4500, FullSize = "XL", Color = "Blanc", ProductInfo = parentProd1 },
            new ProductVariantsModel { VariantID = 3, ProductID = 2, SKU = "789101", SalePrice = 8000, DiscountFixed=600 , FullSize = "50ml", Color = "Standard", ProductInfo = parentProd2 }
        };

            FilteredProducts = new ObservableCollection<ProductVariantsModel>(_allProducts);
        }
    }


}
