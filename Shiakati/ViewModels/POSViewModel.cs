using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;

namespace Shiakati.ViewModels
{
    public partial class POSViewModel : ObservableObject
    {
        private readonly ILogger<POSViewModel> _logger;      
        private readonly IPrintService _printService;


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


        public POSViewModel(string name, ILogger<POSViewModel> logger, IPrintService printService)
        {
            TabName = name;
            _logger = logger;
            _printService = printService;   

            LoadFakeProducts();

            CartItems.CollectionChanged += CartItems_CollectionChanged;
        }

        private void CartItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // Si de nouveaux articles sont ajoutés, on s'abonne à leurs changements
            if (e.NewItems != null)
            {
                foreach (CartItem item in e.NewItems)
                {
                    item.PropertyChanged += CartItem_PropertyChanged;
                }
            }

            // Si des articles sont retirés, on SE DÉSABONNE (Crucial pour éviter les fuites de mémoire !)
            if (e.OldItems != null)
            {
                foreach (CartItem item in e.OldItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                }
            }

            // Dans tous les cas (ajout ou retrait), le total du panier change
            UpdateCartTotal();
        }

        // 3. Gestion des changements à l'INTÉRIEUR d'un article (ex: Quantité++, Remise manuelle)
        private void CartItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // On recalcule le panier total uniquement si une propriété financière de l'article a changé
            if (e.PropertyName == nameof(CartItem.TotalPrice) ||
                e.PropertyName == nameof(CartItem.RawTotal) ||
                e.PropertyName == nameof(CartItem.TotalLineDiscount))
            {
                UpdateCartTotal();
            }
        }

        // ... Vos propriétés CartSubTotal, TotalDiscountAmount, CartTotal restent identiques ...

        private void UpdateCartTotal()
        {
            OnPropertyChanged(nameof(CartSubTotal));
            OnPropertyChanged(nameof(TotalDiscountAmount));
            OnPropertyChanged(nameof(CartTotal));
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


        [RelayCommand]
        private void AddToCart(ProductVariantsModel selectedVariant)
        {
            if (selectedVariant == null) return;

            var existingItem = CartItems.FirstOrDefault(c => c.Variant.VariantID == selectedVariant.VariantID);

            if (existingItem != null)
            {
                // Cela va déclencher NotifyPropertyChangedFor, qui va déclencher CartItem_PropertyChanged, qui va déclencher UpdateCartTotal !
                existingItem.Quantity++;
            }
            else
            {
                // L'ajout dans la collection déclenche CartItems_CollectionChanged, qui va s'abonner et déclencher UpdateCartTotal !
                CartItems.Add(new CartItem(selectedVariant, selectedVariant.ProductInfo));
            }

            SearchText = string.Empty;
        }

        [RelayCommand]
        private void RemoveFromCart(CartItem itemToRemove)
        {
            if (itemToRemove != null)
            {
                CartItems.Remove(itemToRemove); // Déclenche CollectionChanged automatiquement
            }
        }

        [RelayCommand]
        private void IncrementQty(CartItem item)
        {
            if (item != null) item.Quantity++;
        }

        [RelayCommand]
        private void DecrementQty(CartItem item)
        {
            if (item != null)
            {
                if (item.Quantity > 1)
                    item.Quantity--;
                else
                    CartItems.Remove(item);
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
            string newTicketNumber = $"TK-{DateTime.Now:yyyyMMddHHmmss}";

            MessageBox.Show($"Vente validée pour un total de {CartTotal:N2} DA.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);


            var receip = new ReceipModel
            {
                TicketNumber = newTicketNumber,
                Date = DateTime.Now,
                TotalAmount = CartTotal,
                TotalDiscount = TotalDiscountAmount,
                Items = CartItems.Select(c => new ReceiptItem
                {
                    Designation = c.DisplayName,
                    Quantity = c.Quantity,
                    UnitPrice = c.Variant.SalePrice,
                }).ToList()

            };

            if (PrintTicket(receip))
            {
                // Avant de Clear(), on se désabonne manuellement pour être sûr à 100% que la mémoire est libérée
                foreach (var item in CartItems)
                {
                    item.PropertyChanged -= CartItem_PropertyChanged;
                }
                CartItems.Clear();
            }
        }

        private bool PrintTicket(ReceipModel receipt)
        {
            // Implémentation de l'impression du ticket
            try
            {
                _printService.PrintReceipt(receipt);
                MessageBox.Show("Vente validée et ticket imprimé !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression du ticket : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
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
