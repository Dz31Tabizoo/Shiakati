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
        private bool _isDiscountPinned = false;

        // On peut ajouter une propriété pour le montant de la remise, même si elle n'est pas encore utilisée
        public decimal DiscountAmount { get; private set; } = 0;

        [RelayCommand]
        private void ToggleDiscount()
        {
            //toogle the discount pinning
            IsDiscountPinned = !IsDiscountPinned;

            
        }

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

        public decimal CartTotal => CartItems.Sum(x => x.TotalPrice);

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

        [RelayCommand]
        private void AddToCart(ProductVariantsModel selectedVariant)
        {
            if (selectedVariant == null) return;

            // 1. Correction de la recherche (On cherche par VariantID et on utilise .Variant)
            var existingItem = CartItems.FirstOrDefault(c => c.Variant.VariantID == selectedVariant.VariantID);

            if (existingItem != null)
            {
                // 2. Utilise la propriété générée 'Quantity'
                existingItem.Quantity++;
                OnPropertyChanged(nameof(CartTotal));
            }
            else
            {
                // 3. Correction CS7036 : On doit passer la variante ET son info produit
                // On suppose que selectedVariant a une propriété 'ProductInfo' (voir étape 3)
                CartItems.Add(new CartItem(selectedVariant, selectedVariant.ProductInfo));
            }

            SearchText = string.Empty;
        }

        [RelayCommand]
        private void RemoveFromCart(CartItem itemToRemove)
        {
            if (itemToRemove != null)
            {
                CartItems.Remove(itemToRemove);
                OnPropertyChanged(nameof(CartTotal)); // Forcer la maj du total
            }
        }

        [RelayCommand]
        private void IncrementQty(CartItem item)
        {
            if (item != null)
            {
                item.Quantity++;
                OnPropertyChanged(nameof(CartTotal));
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
                    OnPropertyChanged(nameof(CartTotal));
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
            var parentProd1 = new ProductModel { ProductID = 1, ProductName = "Qamis Blanc Premium" };
            var parentProd2 = new ProductModel { ProductID = 2, ProductName = "Parfum Oud Royal" };

            _allProducts = new List<ProductVariantsModel>
        {
            new ProductVariantsModel { VariantID = 1, ProductID = 1, SKU = "123456", SalePrice = 4500, FullSize = "L", Color = "Blanc", ProductInfo = parentProd1 },
            new ProductVariantsModel { VariantID = 2, ProductID = 1, SKU = "123457", SalePrice = 4500, FullSize = "XL", Color = "Blanc", ProductInfo = parentProd1 },
            new ProductVariantsModel { VariantID = 3, ProductID = 2, SKU = "789101", SalePrice = 8000, FullSize = "50ml", Color = "Standard", ProductInfo = parentProd2 }
        };

            FilteredProducts = new ObservableCollection<ProductVariantsModel>(_allProducts);
        }
    }


}
