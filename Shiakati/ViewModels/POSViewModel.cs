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
        // ==========================================
        // 1. PROPRIÉTÉS (Connectées à l'UI)
        // ==========================================

        [ObservableProperty]
        private string _tabName;

        [ObservableProperty]
        private string _searchText = string.Empty; // Initialiser _searchText à une chaîne vide pour satisfaire la contrainte non-nullable

        // La liste de tous les produits (cachée en mémoire)
        private List<Product> _allProducts = new();

        // La liste affichée à l'écran (qui change quand on cherche)
        [ObservableProperty]
        private ObservableCollection<Product> _filteredProducts = new();

        // Le Panier
        public ObservableCollection<CartItem> CartItems { get; } = new();

        // Le Total du panier (calculé dynamiquement)
        public decimal CartTotal => CartItems.Sum(x => x.TotalPrice);


        // ==========================================
        // 2. CONSTRUCTEUR
        // ==========================================
        public POSViewModel(string name,ILogger<POSViewModel> logger)
        {
            TabName = name;
            LoadFakeProducts();
            _logger = logger;
            _logger.LogInformation("POSViewModel for {TabName} initialized.", TabName);

            // Astuce : On dit au ViewModel de recalculer le Total à chaque fois qu'on ajoute/supprime un article
            LoadFakeProducts();
            CartItems.CollectionChanged += (s, e) => OnPropertyChanged(nameof(CartTotal));
        }


        // ==========================================
        // 3. LOGIQUE MÉTIER & COMMANDES
        // ==========================================

        // Magie du Toolkit : Cette méthode est appelée AUTOMATIQUEMENT quand SearchText change !
        partial void OnSearchTextChanged(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredProducts = new ObservableCollection<Product>(_allProducts);
                return;
            }

            // Recherche par nom ou par code-barres
            var filtered = _allProducts.Where(p =>
                p.Name.Contains(value, System.StringComparison.OrdinalIgnoreCase) ||
                p.Barcode == value).ToList();

            FilteredProducts = new ObservableCollection<Product>(filtered);
        }

        [RelayCommand]
        private void AddToCart(Product selectedProduct)
        {
            if (selectedProduct == null) return;

            // On vérifie si le produit est déjà dans le panier
            var existingItem = CartItems.FirstOrDefault(c => c.ProductRef.Id == selectedProduct.Id);

            if (existingItem != null)
            {
                // Si oui, on ajoute juste +1 à la quantité
                existingItem.Quantity++;
                // On force la mise à jour du grand total en bas à droite
                OnPropertyChanged(nameof(CartTotal));
            }
            else
            {
                // Sinon, on crée une nouvelle ligne dans le ticket
                CartItems.Add(new CartItem(selectedProduct));
            }

            // Optionnel : On vide la barre de recherche après un scan
            SearchText = string.Empty;
        }

        [RelayCommand]
        private void RemoveFromCart(CartItem itemToRemove)
        {
            if (itemToRemove != null)
            {
                //loger if needed
                CartItems.Remove(itemToRemove);
            }
        }


        [RelayCommand]
        private void IncrementQty(CartItem item)
        {
            if (item != null)
            {
                item.Quantity++;

                // On force la mise à jour du grand total en bas à droite
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
                    // Si on est à 1 et qu'on clique sur "-", on retire l'article
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

            // Ici tu mettras la logique pour sauvegarder la vente dans SQL via Dapper
            MessageBox.Show($"Vente validée pour un total de {CartTotal:N2} DA.\nImpression du ticket en cours...",
                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

            // On vide le panier pour le prochain client de cet onglet
            CartItems.Clear();
        }

        // ==========================================
        // 4. DONNÉES DE TEST (Pour ce soir)
        // ==========================================
        private void LoadFakeProducts()
        {
            _allProducts = new List<Product>
            {
                new Product { Id = 1, Name = "Qamis Blanc Premium", Price = 4500, Barcode = "123456" },
                new Product { Id = 2, Name = "Parfum Oud Royal", Price = 8000, Barcode = "789101" },
                new Product { Id = 3, Name = "Chéchia Noire", Price = 500, Barcode = "112233" },
                new Product { Id = 4, Name = "Montre Classique", Price = 12000, Barcode = "445566" },
                new Product { Id = 5, Name = "Qamis Noir Simple", Price = 3500, Barcode = "778899" }
            };

            FilteredProducts = new ObservableCollection<Product>(_allProducts);
        }
    }

    
}
