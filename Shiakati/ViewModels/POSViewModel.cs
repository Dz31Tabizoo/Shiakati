using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Shiakati.Models;
using Shiakati.Services.Interfaces;
using System.Collections.ObjectModel;
using System.Windows;
using Shiakati.Messages;

namespace Shiakati.ViewModels
{
    public partial class POSViewModel : ObservableObject
    {
        private readonly ILogger<POSViewModel> _logger;      
        private readonly IPrintService _printService;

        /*---------------------------------------------
         *             Edit Properties 
         *---------------------------------------------*/
        [ObservableProperty]
        private bool _isEditMode;
        [ObservableProperty]
        private string _editTicketNumber = string.Empty;
        [ObservableProperty]
        private int? _editSaleId;

        /*---------------------------------------------
         *             POS Properties 
         *---------------------------------------------*/
        [ObservableProperty]
        private string _tabName;

        [ObservableProperty]
        private string _searchText = string.Empty;

        // ON CHANGE ICI : On utilise ProductVariantsModel
        private List<ProductVariantModel> _allProducts = new();

        [ObservableProperty]
        private ObservableCollection<ProductVariantModel> _filteredProducts = new();

        // Le Panier utilise notre nouveau CartItem
        public ObservableCollection<CartItem> CartItems { get; } = new();

        // Constructeur avec injection de dépendances pour le logger et le service d'impression
        public POSViewModel(string name, ILogger<POSViewModel> logger, IPrintService printService)
        {
            TabName = name;
            _logger = logger;
            _printService = printService;   

            

            CartItems.CollectionChanged += CartItems_CollectionChanged;

            WeakReferenceMessenger.Default.Register<EditSaleMessage>(this, (r, m) =>
            {
                // Ce message est envoyé par les CartItems lorsqu'ils changent (ex: remise manuelle)
                loadSaleForEditing(m.Sale,m.Items);
            });
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
            //if (string.IsNullOrWhiteSpace(value))
            //{
            //    FilteredProducts = new ObservableCollection<ProductVariantModel>(_allProducts);
            //    return;
            //}

            //var filtered = _allProducts.Where(p =>
            //    (p.ProductInfo?.ProductName?.Contains(value, StringComparison.OrdinalIgnoreCase) == true) ||
            //    (p.Sku != null && p.Sku.Equals(value, StringComparison.OrdinalIgnoreCase))).ToList();

            //FilteredProducts = new ObservableCollection<ProductVariantModel>(filtered);
        }

        public decimal? CartSubTotal => CartItems.Sum(x => x.RawTotal);

        // Somme de toutes les remises (fixes et manuelles)
        public decimal? TotalDiscountAmount => (decimal?)CartItems.Sum(x => x.TotalLineDiscount);

        // Le montant final à encaisser
        public decimal? CartTotal => CartSubTotal - TotalDiscountAmount;

        [RelayCommand]
        private void AddToCart(ProductVariantModel selectedVariant)
        {
            if (selectedVariant == null) return;
            var existingItem = CartItems.FirstOrDefault(c => c.Variant.VariantId == selectedVariant.VariantId);

            if (existingItem != null) existingItem.Quantity++;            
            //else CartItems.Add(new CartItem(selectedVariant));
            
            SearchText = string.Empty;
        }

        [RelayCommand]
        private void RemoveFromCart(CartItem itemToRemove)
        {
            if (itemToRemove != null) CartItems.Remove(itemToRemove); 
            // Déclenche CollectionChanged automatiquement            
        }


        public void loadSaleForEditing(SaleModel sale,IEnumerable<SaleItemModel>items)
        {
                if (sale == null) return;
    
                IsEditMode = true;
                EditSaleId = sale.SaleID;
                EditTicketNumber = sale.TicketNumber;
    
                CartItems.Clear();
            foreach (var item in items)
            {
                var variant = _allProducts.FirstOrDefault(p => p.VariantId == item.VariantID);
                if (variant != null)
                {
                    //var cartItem = new CartItem(variant, variant.ProductInfo)
                    //{
                    //    Quantity = item.Quantity                        
                        
                    //};
                    //CartItems.Add(cartItem);
                }
            
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            ResetPOS();
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

            if (IsEditMode)
            {
                MessageBox.Show($"Modification de la vente {EditTicketNumber} validée pour un total de {CartTotal:N2} DA.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                string editedTicketNumber = EditTicketNumber + " (modifiée)";
                // TODO : Ici, je met ajour salesHistory and stockMovment
                var receip = new ReceipModel
                {
                    TicketNumber = editedTicketNumber,
                    Date = DateTime.Now,
                    TotalAmount = CartTotal ?? 0,
                    TotalDiscount = TotalDiscountAmount ?? 0,
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
                    ResetPOS();
                }
            }
            else
            {
                string newTicketNumber = $"TK-{DateTime.Now:yyyyMMddHHmmss}";

                MessageBox.Show($"Vente validée pour un total de {CartTotal:N2} DA.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);


                var receip = new ReceipModel
                {
                    TicketNumber = newTicketNumber,
                    Date = DateTime.Now,
                    TotalAmount = CartTotal ?? 0,
                    TotalDiscount = TotalDiscountAmount ?? 0,
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
                    ResetPOS();
                }
            }
        }

        private void ResetPOS()
        {
            IsEditMode = false;
            EditTicketNumber = string.Empty;
            EditSaleId = null;
            foreach (var item in CartItems)
            {
                item.PropertyChanged -= CartItem_PropertyChanged;
            }
            CartItems.Clear();
        }
        private bool PrintTicket(ReceipModel receipt)
        {
            // Implémentation de l'impression du ticket
            try
            {
                // Récupérer le nom de l'imprimante depuis les paramètres
                string printerToUse = Properties.Settings.Default.TicketPrinterName; 
                _printService.PrintReceipt(receipt,printerToUse);
                MessageBox.Show("Vente validée et ticket imprimé !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression du ticket : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }        
    }
}
