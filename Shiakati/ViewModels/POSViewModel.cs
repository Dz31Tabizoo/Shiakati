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
        private readonly ICatalogService _catalogDb;
        private readonly IProductsService _productsService;
        private readonly IProductVariantsService _stockService;
        private readonly ICacheService _cacheService;
        private readonly ISaleService _saleService;

        public POSViewModel(string name, ILogger<POSViewModel> logger, IPrintService printService,
                            ICatalogService catalogDb, IProductsService productsService,
                            IProductVariantsService stockService, ICacheService cacheService, ISaleService saleService)
        {
            TabName = name;
            _logger = logger;
            _printService = printService;
            _catalogDb = catalogDb;
            _productsService = productsService;
            _stockService = stockService;
            _cacheService = cacheService;
            _saleService = saleService;

            CartItems.CollectionChanged += CartItems_CollectionChanged;

            WeakReferenceMessenger.Default.Register<EditSaleMessage>(this, (r, m) =>
            {
                LoadSaleForEditing(m.Sale, m.Items);
            });

            _ = LoadProductsAsync();
        }

        /*---------------------------------------------
         * State & Data Properties 
         *---------------------------------------------*/

        [ObservableProperty] private string _tabName;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _searchText = string.Empty;

        // Filter collections (NO "TOUT" anymore)
        [ObservableProperty] private ObservableCollection<string> _categories = new();
        [ObservableProperty] private ObservableCollection<string> _brands = new();
        [ObservableProperty] private ObservableCollection<string> _filterColors = new();
        [ObservableProperty] private ObservableCollection<string> _filterSizes = new();

        // Selected filter values (default empty or "TOUT"? We'll keep as empty string or null, treat empty as "no filter")
        [ObservableProperty] private string _selectedCategory = "TOUT";
        [ObservableProperty] private string _selectedBrand = "TOUT";
        [ObservableProperty] private string _selectedColor = "TOUT";
        [ObservableProperty] private string _selectedSize = "TOUT";

        // Edit Mode Properties
        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private string _editTicketNumber = string.Empty;
        [ObservableProperty] private int? _editSaleId;

        // Collections
        private List<ProductVariantModel> _allProducts = new();
        [ObservableProperty] private ObservableCollection<ProductVariantModel> _filteredProducts = new();
        public ObservableCollection<CartItem> CartItems { get; } = new();

        // Financial Totals
        public decimal? CartSubTotal => CartItems.Sum(x => x.RawTotal ?? 0);
        public decimal? TotalDiscountAmount => CartItems.Sum(x => x.TotalLineDiscount ?? 0);
        public decimal? CartTotal => CartSubTotal - TotalDiscountAmount;

        /*---------------------------------------------
         * Data Initialization
         *---------------------------------------------*/

        private async Task LoadProductsAsync()
        {
            try
            {
                IsLoading = true;

                // 1. Load all categories from the catalog (not from products)
                var catalog = await _cacheService.GetOrLoadAsync<(List<BrandsModel> Brands, List<CategoryModel> Categories)>(
                    CacheKeys.Catalog,
                    () => _catalogDb.GetInitialGatalogDataAsync());

                Categories.Clear();
                Categories.Add("TOUT");
                foreach (var cat in catalog.Categories)
                    Categories.Add(cat.CategoryName);   // use the CategoryName property

                // 2. Load products and filter to active + stock > 0
                var items = await _cacheService.GetOrLoadAsync("StockVariants", _stockService.GetProductVariantsAsync);
                _allProducts = items.Where(i => i.IsActive == true && i.StockQuantity > 0).ToList();

                // 3. Build dynamic filter lists (brands, colors, sizes) from the filtered products
                var distinctBrands = _allProducts
                    .Where(p => !string.IsNullOrWhiteSpace(p.BrandName))
                    .Select(p => p.BrandName!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(b => b)
                    .ToList();
                Brands.Clear();
                Brands.Add("TOUT");
                foreach (var b in distinctBrands) Brands.Add(b);

                var distinctColors = _allProducts
                    .Where(p => !string.IsNullOrWhiteSpace(p.Color))
                    .Select(p => p.Color!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c)
                    .ToList();
                FilterColors.Clear();
                FilterColors.Add("TOUT");
                foreach (var c in distinctColors) FilterColors.Add(c);

                var distinctSizes = _allProducts
                    .Where(p => !string.IsNullOrWhiteSpace(p.FullSize))
                    .Select(p => p.FullSize!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s)
                    .ToList();
                FilterSizes.Clear();
                FilterSizes.Add("TOUT");
                foreach (var s in distinctSizes) FilterSizes.Add(s);

                ApplyFilters();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des produits pour le POS.");
                MessageBox.Show("Impossible de charger le catalogue.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /*---------------------------------------------
         * Search & Filter Logic
         *---------------------------------------------*/

        [RelayCommand]
        private async Task ProcessScanOrSearchAsync()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
                return;

            // 1. Check if the text is an exact SKU match
            var exactMatch = _allProducts.FirstOrDefault(p =>
                p.Sku != null && p.Sku.Equals(SearchText, StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
            {
                // It's a barcode – add directly to cart
                AddToCart(exactMatch);
                SearchText = string.Empty;  // clear for the next scan
                return;
            }
        }

        partial void OnSearchTextChanged(string value) => ApplyFilters();

        [RelayCommand]
        private void ApplyFilters()
        {
            var query = _allProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SelectedCategory) && !SelectedCategory.Equals("TOUT", StringComparison.OrdinalIgnoreCase))
                query = query.Where(p => string.Equals(p.CategoryName, SelectedCategory, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SelectedBrand) && !SelectedBrand.Equals("TOUT", StringComparison.OrdinalIgnoreCase))
                query = query.Where(p => string.Equals(p.BrandName, SelectedBrand, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SelectedColor) && !SelectedColor.Equals("TOUT", StringComparison.OrdinalIgnoreCase))
                query = query.Where(p => string.Equals(p.Color, SelectedColor, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SelectedSize) && !SelectedSize.Equals("TOUT", StringComparison.OrdinalIgnoreCase))
                query = query.Where(p => string.Equals(p.FullSize, SelectedSize, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(SearchText))
                query = query.Where(p =>
                    (p.ProductName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                    (p.Sku != null && p.Sku.Equals(SearchText, StringComparison.OrdinalIgnoreCase)));

            FilteredProducts = new ObservableCollection<ProductVariantModel>(query);
        }

        /*---------------------------------------------
         * Toggle filter commands
         *---------------------------------------------*/

        [RelayCommand]
        private void ToggleCategory(string category)
        {
            SelectedCategory = ToggleValue(SelectedCategory, category);
            ApplyFilters();
        }
        [RelayCommand]
        private void ToggleBrand(string brand)
        {
            SelectedBrand = ToggleValue(SelectedBrand, brand);
            ApplyFilters();
        }
        [RelayCommand]
        private void ToggleColor(string color)
        {
            SelectedColor = ToggleValue(SelectedColor, color);
            ApplyFilters();
        }
        [RelayCommand]
        private void ToggleSize(string size)
        {
            SelectedSize = ToggleValue(SelectedSize, size);
            ApplyFilters();
        }
        private string? ToggleValue(string? current, string value)
        {
            return string.Equals(current, value, StringComparison.OrdinalIgnoreCase) ? null : value;
        }

        /*---------------------------------------------
         * Cart Event Handlers
         *---------------------------------------------*/

        partial void OnSelectedCategoryChanged(string value) => ApplyFilters();
        partial void OnSelectedBrandChanged(string value) => ApplyFilters();
        partial void OnSelectedColorChanged(string value) => ApplyFilters();
        partial void OnSelectedSizeChanged(string value) => ApplyFilters();
        private void CartItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CartItem item in e.NewItems) item.PropertyChanged += CartItem_PropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (CartItem item in e.OldItems) item.PropertyChanged -= CartItem_PropertyChanged;
            }
            UpdateCartTotal();
        }
        private void CartItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(CartItem.TotalPrice) or nameof(CartItem.RawTotal) or nameof(CartItem.TotalLineDiscount) or nameof(CartItem.Quantity))
            {
                UpdateCartTotal();
            }
        }
        private void UpdateCartTotal()
        {
            OnPropertyChanged(nameof(CartSubTotal));
            OnPropertyChanged(nameof(TotalDiscountAmount));
            OnPropertyChanged(nameof(CartTotal));
        }

        /*---------------------------------------------
         * Commands (POS Actions)
         *---------------------------------------------*/

        [RelayCommand]
        private void AddToCart(ProductVariantModel selectedVariant)
        {
            if (selectedVariant == null) return;

            var existingItem = CartItems.FirstOrDefault(c => c.Variant?.VariantId == selectedVariant.VariantId);

            if (existingItem != null)
            {
                existingItem.Quantity = (existingItem.Quantity ?? 0) + 1;
            }
            else
            {
                CartItems.Add(new CartItem(selectedVariant));
            }

            SearchText = string.Empty;
        }
        [RelayCommand] private void RemoveFromCart(CartItem itemToRemove) => CartItems.Remove(itemToRemove);
        [RelayCommand] private void IncrementQty(CartItem item)
        {
            if (item != null) item.Quantity = (item.Quantity ?? 0) + 1;
        }
        [RelayCommand] private void DecrementQty(CartItem item)
        {
            if (item == null) return;
            if ((item.Quantity ?? 0) > 1) item.Quantity--;
            else CartItems.Remove(item);
        }
        [RelayCommand] private void CancelEdit() => ResetPOS();

        [RelayCommand]
        private async Task CheckoutAsync()
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Le panier est vide !", "Action requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            IsLoading = true;
            try
            {
                if (IsEditMode && EditSaleId.HasValue)
                {
                    // ─── UPDATE existing sale ───
                    var updateRequest = new UpdateSaleRequest
                    {
                        SaleId = EditSaleId.Value,
                        GlobalDiscount = 0,   // or from UI if you add it later
                        Items = CartItems.Select(c => new UpdateSaleItemDto
                        {
                            SaleItemId = c.SaleItemId,   // null for new items added during edit
                            VariantId = c.Variant!.VariantId,
                            Quantity = c.Quantity ?? 1,
                            FixedDiscountApplied = c.IsDiscountPinned,
                            ManualDiscountAmount = c.ManualDiscount
                        }).ToList()
                    };

                    var result = await _saleService.UpdateSaleAsync(EditSaleId.Value, updateRequest);
                    if (result)
                    {
                        // Optionally fetch the updated sale to get total, or trust the UI
                        var receipt = new ReceipModel
                        {
                            TicketNumber = EditTicketNumber,
                            IsEdited = true,
                            Date = DateTime.Now,
                            TotalAmount = CartTotal ?? 0,
                            TotalDiscount = TotalDiscountAmount ?? 0,
                            Items = CartItems.Select(c => new ReceiptItem
                            {
                                Designation = c.DisplayName,
                                Quantity = c.Quantity ?? 0,
                                UnitPrice = c.Variant?.SalePrice ?? 0
                            }).ToList()
                        };

                        if (PrintTicket(receipt))
                        {
                            MessageBox.Show($"Vente modifiée – Ticket {EditTicketNumber}",
                                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        _cacheService.Remove(CacheKeys.StockVariants);
                        _ = LoadProductsAsync();
                        ResetPOS();
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de la modification de la vente.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    // ─── CREATE new sale ───
                    var saleRequest = new SaleRequest
                    {
                        GlobalDiscount = 0,   // or from UI
                        Items = CartItems.Select(c => new SaleItemDto
                        {
                            VariantId = c.Variant!.VariantId,
                            Quantity = c.Quantity ?? 1,
                            FixedDiscountApplied = c.IsDiscountPinned,
                            ManualDiscountAmount = c.ManualDiscount
                        }).ToList()
                    };

                    var result = await _saleService.CreateSaleAsync(saleRequest);
                    if (result != null)
                    {
                        var receipt = new ReceipModel
                        {
                            TicketNumber = result.TicketNumber,
                            Date = DateTime.Now,
                            TotalAmount = CartTotal ?? 0,
                            TotalDiscount = TotalDiscountAmount ?? 0,
                            Items = CartItems.Select(c => new ReceiptItem
                            {
                                Designation = c.DisplayName,
                                Quantity = c.Quantity ?? 0,
                                UnitPrice = c.Variant?.SalePrice ?? 0
                            }).ToList()
                        };

                        if (PrintTicket(receipt))
                        {
                            MessageBox.Show($"Vente validée – Ticket {result.TicketNumber}",
                                            "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        _cacheService.Remove(CacheKeys.StockVariants);
                        _ = LoadProductsAsync();
                        ResetPOS();
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de l'enregistrement de la vente.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            finally
            {
                IsLoading = false;
            }
        }
        /*---------------------------------------------
         * Helper Methods 
         *---------------------------------------------*/


        public void LoadSaleForEditing(SaleModel sale, IEnumerable<SaleItemModel> items)
        {
            if (sale == null || items == null) return;

            IsEditMode = true;
            EditSaleId = sale.SaleID;
            EditTicketNumber = sale.TicketNumber;

            ResetCartMemorySafe();

            foreach (var item in items)
            {
                var variant = _allProducts.FirstOrDefault(p => p.VariantId == item.VariantID);
                if (variant != null)
                {
                    var cartItem = new CartItem(variant)
                    {
                        Quantity = item.Quantity ?? 1,
                        ManualDiscount = item.DiscountAmount,
                        SaleItemId = item.SaleItemID   // store the original sale item ID
                    };
                    CartItems.Add(cartItem);
                }
            }
        }

        private void ResetPOS()
        {
            IsEditMode = false;
            EditTicketNumber = string.Empty;
            EditSaleId = null;
            SelectedCategory = "TOUT";
            SelectedBrand = "TOUT";
            SelectedColor = "TOUT";
            SelectedSize = "TOUT";
            SearchText = string.Empty;

            ResetCartMemorySafe();
            ApplyFilters();
        }

        private void ResetCartMemorySafe()
        {
            foreach (var item in CartItems)
            {
                item.PropertyChanged -= CartItem_PropertyChanged;
            }
            CartItems.Clear();
        }

        private bool PrintTicket(ReceipModel receipt)
        {
            try
            {
                string printerToUse = Properties.Settings.Default.TicketPrinterName;
                _printService.PrintReceipt(receipt, printerToUse);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur d'impression");
                MessageBox.Show($"Erreur lors de l'impression du ticket : {ex.Message}", "Erreur Imprimante", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}