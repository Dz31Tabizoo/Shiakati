using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Shiakati.Messages;
using Shiakati.Models;
using Shiakati.Properties;
using Shiakati.Services.Implementations; // ✅ Add this
using Shiakati.Services.Interfaces.CacheService;
using Shiakati.Services.Interfaces.DataServices;
using Shiakati.Services.Interfaces.PrintServices;
using Shiakati.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ZXing;

namespace Shiakati.ViewModels
{
    public partial class POSViewModel : ObservableObject, IDisposable
    {
        private readonly IReservationDataService _reservationDataService;
        private readonly ILogger<POSViewModel> _logger;
        private readonly IPrintService _printService;
        private readonly ICatalogDataService _catalogDataService;
        private readonly IStockDataService _stockService;
        private readonly ICacheService _cacheService;
        private readonly ISaleDataService _saleDataService;
        private readonly IClientDataService _clientDataService;
        private bool _skipGridRefresh;

        // Stable collection for the ICollectionView
        private readonly ObservableCollection<ProductVariantModel> _posProducts = new();

        public POSViewModel(string name, ILogger<POSViewModel> logger, IPrintService printService,
                            ICatalogDataService catalogDataService, IStockDataService stockService,
                            ICacheService cacheService, IReservationDataService reservation,
                            ISaleDataService saleDataService, IClientDataService clientDataService)
        {
            TabName = name;
            _logger = logger;
            _printService = printService;
            _catalogDataService = catalogDataService;
            _stockService = stockService;
            _cacheService = cacheService;
            _saleDataService = saleDataService;
            _clientDataService = clientDataService;
            _reservationDataService = reservation;

            CartItems.CollectionChanged += CartItems_CollectionChanged;

            FilteredProductsView = CollectionViewSource.GetDefaultView(_posProducts);
            FilteredProductsView.Filter = ProductFilter;

            WeakReferenceMessenger.Default.Register<EditSaleMessage>(this, (r, m) =>
            {
                LoadSaleForEditing(m.Sale, m.Items);
            });
            _catalogDataService.CatalogDataChanged += OnCatalogChanged;
            _stockService.StockDataChanged += OnStockChanged;

            _ = LoadProductsAsync();

            WeakReferenceMessenger.Default.Register<StockUpdatedMessage>(this, (r, m) =>
            {
                Application.Current.Dispatcher.InvokeAsync(() => LoadProductsAsync());
            });

            SelectedColor = "TOUT";
            SelectedSize = "TOUT";
        }

        // ---------- Data ----------
        private List<ProductVariantModel> _allProducts = new();
        private Dictionary<string, ProductVariantModel> _skuLookup = new(StringComparer.OrdinalIgnoreCase);
        private List<CategoryModel> _allCategories = new();
        private List<BrandsModel> _allBrands = new();

        // ---------- Observable properties ----------
        [ObservableProperty] private string _tabName;
        [ObservableProperty] private bool _isLoading;

        // Stage 1 – Categories
        [ObservableProperty] private ObservableCollection<CategoryModel> _categories = new();
        [ObservableProperty] private CategoryModel? _selectedCategory;

        // Stage 2 – Brands
        [ObservableProperty] private ObservableCollection<BrandsModel> _brandsForCategory = new();
        [ObservableProperty] private BrandsModel? _selectedBrand;

        // Stage 3 – Filters
        [ObservableProperty] private ObservableCollection<string> _availableColors = new();
        [ObservableProperty] private ObservableCollection<string> _availableSizes = new();
        [ObservableProperty] private string _selectedColor = "TOUT";
        [ObservableProperty] private string _selectedSize = "TOUT";

        // Visibility helpers
        public bool IsStage1Visible => SelectedCategory == null;
        public bool IsStage2Visible => SelectedCategory != null && SelectedBrand == null;
        public bool IsStage3Visible => SelectedBrand != null;

        // Search
        [ObservableProperty] private string _searchText = string.Empty;

        // Client / Cart / Checkout
        [ObservableProperty] private ClientSummaryDto? _selectedClient;
        [ObservableProperty] private string _clientDisplay = "Aucun";
        [ObservableProperty] private decimal? _creditPaidAmount;
        [ObservableProperty] private DateTime? _creditExpiresAt;
        [ObservableProperty] private bool isCreditSale;
        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private string _editTicketNumber = string.Empty;
        [ObservableProperty] private int? _editSaleId;
        public ObservableCollection<CartItem> CartItems { get; } = new();

        public ICollectionView FilteredProductsView { get; }
        public decimal? CartSubTotal => CartItems.Sum(x => x.RawTotal ?? 0);
        public decimal? TotalDiscountAmount => CartItems.Sum(x => x.TotalLineDiscount ?? 0);
        public decimal? CartTotal => CartSubTotal - TotalDiscountAmount;

        // ---------- Loading ----------
        public async Task LoadProductsAsync()
        {
            try
            {
                IsLoading = true;

                await _catalogDataService.LoadCatalogAsync();
                await _stockService.LoadVariantsAsync();

                _allProducts = _stockService.Variants
                    .Where(i => i.IsActive == true && i.StockQuantity > 0)
                    .ToList();
                _allCategories = _catalogDataService.Categories.ToList();
                _allBrands = _catalogDataService.Brands.ToList();

                // Build SKU lookup
                _skuLookup = _allProducts
                    .Where(p => !string.IsNullOrWhiteSpace(p.Sku))
                    .ToDictionary(p => p.Sku!, StringComparer.OrdinalIgnoreCase);

                // Populate active categories (those with at least one active variant)
                var activeCategoryNames = _allProducts.Select(p => p.CategoryName).Distinct().ToHashSet();
                Categories.Clear();
                foreach (var cat in _allCategories.Where(c => activeCategoryNames.Contains(c.CategoryName)))
                    Categories.Add(cat);

                // Reset selections
                SelectedCategory = null;
                SelectedBrand = null;
                BrandsForCategory.Clear();

                // Fill the stable collection
                _posProducts.Clear();
                foreach (var p in _allProducts) _posProducts.Add(p);
                FilteredProductsView.Refresh();

                RefreshVisibility();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement initial du POS.");
                MessageBox.Show("Impossible de charger le catalogue. " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }




        // ---------- Stage navigation ----------
        [RelayCommand]
        private void SelectCategory(CategoryModel category)
        {
            if (category == null) return;
            SelectedCategory = category;
            SelectedBrand = null;

            // Get distinct, trimmed brand names for this category (case‑insensitive)
            var brandNames = _allProducts
                .Where(p => string.Equals(p.CategoryName, category.CategoryName, StringComparison.OrdinalIgnoreCase))
                .Select(p => p.BrandName?.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Get the corresponding BrandsModel objects (one per distinct name)
            var brands = _allBrands
                .Where(b => brandNames.Contains(b.BrandName?.Trim(), StringComparer.OrdinalIgnoreCase))
                .ToList();

            // Remove duplicates that have the same trimmed name (different IDs)
            var distinctBrands = brands
                .GroupBy(b => b.BrandName?.Trim() ?? "", StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            BrandsForCategory.Clear();
            foreach (var b in distinctBrands)
                BrandsForCategory.Add(b);

            SelectedColor = "TOUT";
            SelectedSize = "TOUT";

            RefreshVisibility();
            FilteredProductsView.Refresh();
        }

        [RelayCommand]
        private void SelectBrand(BrandsModel brand)
        {
            if (brand == null) return;
            SelectedBrand = brand;

            var selectedCategoryName = SelectedCategory?.CategoryName ?? "(null)";
            var selectedBrandName = brand.BrandName;


            var productsForBrand = _allProducts
                .Where(p =>
                {
                    bool brandMatch = string.Equals(p.BrandName?.Trim(), selectedBrandName?.Trim(), StringComparison.OrdinalIgnoreCase);
                    bool categoryMatch = string.Equals(p.CategoryName?.Trim(), selectedCategoryName?.Trim(), StringComparison.OrdinalIgnoreCase);

                    if (!brandMatch || !categoryMatch)
                        return false;

                    return true;
                })
                .ToList();

            var colors = productsForBrand
                .Where(p => !string.IsNullOrWhiteSpace(p.Color))
                .Select(p => p.Color!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c)
                .ToList();
            AvailableColors.Clear();
            AvailableColors.Add("TOUT");
            foreach (var c in colors) AvailableColors.Add(c);

            var sizes = productsForBrand
                .Where(p => !string.IsNullOrWhiteSpace(p.FullSize))
                .Select(p => p.FullSize!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(s => s)
                .ToList();
            AvailableSizes.Clear();
            AvailableSizes.Add("TOUT");
            foreach (var s in sizes) AvailableSizes.Add(s);

            SelectedColor = "TOUT";
            SelectedSize = "TOUT";

            RefreshVisibility();
            FilteredProductsView.Refresh();
        }

        [RelayCommand]
        private void Reset()
        {
            SelectedCategory = null;
            SelectedBrand = null;
            SelectedColor = "TOUT";
            SelectedSize = "TOUT";
            BrandsForCategory.Clear();
            RefreshVisibility();
            FilteredProductsView.Refresh();
        }

        private void RefreshVisibility()
        {
            OnPropertyChanged(nameof(IsStage1Visible));
            OnPropertyChanged(nameof(IsStage2Visible));
            OnPropertyChanged(nameof(IsStage3Visible));
        }

        // ────────────── Search & Scan ──────────────
        private CancellationTokenSource? _searchDebounceToken;

        partial void OnSearchTextChanged(string value)
        {
            _searchDebounceToken?.Cancel();
            _searchDebounceToken = new CancellationTokenSource();
            var token = _searchDebounceToken.Token;

            if (string.IsNullOrWhiteSpace(value))
            {
                if (_skipGridRefresh)
                {
                    _skipGridRefresh = false;
                    return;
                }
                FilteredProductsView.Refresh();
                return;
            }

            if (_skuLookup.TryGetValue(value, out var exactMatch))
            {
                _skipGridRefresh = true;
                AddToCart(exactMatch);
                return;
            }

            Task.Delay(500, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                    Application.Current.Dispatcher.Invoke(() => FilteredProductsView.Refresh());
            }, token);
        }

        private bool ProductFilter(object obj)
        {
            if (obj is not ProductVariantModel p) return false;
            if (p.IsActive != true || p.StockQuantity <= 0) return false;

            // Must have a brand selected
            if (SelectedBrand == null) return false;
            if (!string.Equals(p.BrandName, SelectedBrand.BrandName, StringComparison.OrdinalIgnoreCase))
                return false;

            // ➕ Filter by the selected category as well
            if (SelectedCategory != null &&
                !string.Equals(p.CategoryName, SelectedCategory.CategoryName, StringComparison.OrdinalIgnoreCase))
                return false;

            // Color filter
            if (!string.IsNullOrWhiteSpace(SelectedColor) && SelectedColor != "TOUT" &&
                !string.Equals(p.Color, SelectedColor, StringComparison.OrdinalIgnoreCase)) return false;

            // Size filter
            if (!string.IsNullOrWhiteSpace(SelectedSize) && SelectedSize != "TOUT" &&
                !string.Equals(p.FullSize, SelectedSize, StringComparison.OrdinalIgnoreCase)) return false;

            // Search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                return (p.ProductName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true) ||
                       (p.Sku != null && p.Sku.Equals(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            return true;
        }

        //----------- Client Sales Management --------------
        [RelayCommand]
        private void OpenClientSelectionDialog()
        {
            var dialog = new ClientSelectorDialog(_clientDataService) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
            {
                SelectedClient = dialog.SelectedClient;
                ClientDisplay = SelectedClient != null
                    ? $"{SelectedClient.FullName} ({SelectedClient.PhoneNumber})"
                    : "Aucun";
            }
        }

        [RelayCommand]
        private async Task OpenCreditSaleDialogAsync()
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Le panier est vide !", "Action requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedClient == null)
            {
                MessageBox.Show("Veuillez d'abord sélectionner un client.", "Client requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var total = CartTotal ?? 0;
            var creditDialog = new Views.CreditSaleDialog(total);
            if (creditDialog.ShowDialog() == true)
            {
                IsCreditSale = true;
                CreditPaidAmount = creditDialog.PaidAmount;
                CreditExpiresAt = creditDialog.ExpiresAt;
                // Proceed to checkout with credit sale
                await CheckoutAsync();
            }
        }
        // ────────────── Filter toggles ──────────────
        partial void OnSelectedColorChanged(string value) => FilteredProductsView.Refresh();
        partial void OnSelectedSizeChanged(string value) => FilteredProductsView.Refresh();

        // ────────────── Cart operations ──────────────
        private void CartItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null) foreach (CartItem item in e.NewItems) item.PropertyChanged += CartItem_PropertyChanged;
            if (e.OldItems != null) foreach (CartItem item in e.OldItems) item.PropertyChanged -= CartItem_PropertyChanged;
            UpdateCartTotal();
        }
        private void CartItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(CartItem.TotalPrice) or nameof(CartItem.RawTotal) or nameof(CartItem.TotalLineDiscount) or nameof(CartItem.Quantity))
                UpdateCartTotal();
        }
        private void UpdateCartTotal()
        {
            OnPropertyChanged(nameof(CartSubTotal));
            OnPropertyChanged(nameof(TotalDiscountAmount));
            OnPropertyChanged(nameof(CartTotal));
        }

        [RelayCommand]
        private void AddToCart(ProductVariantModel selectedVariant)
        {
            if (selectedVariant == null) return;
            var existingItem = CartItems.FirstOrDefault(c => c.Variant?.VariantId == selectedVariant.VariantId);
            if (existingItem != null)
                IncrementQty(existingItem);
            else
                CartItems.Add(new CartItem(selectedVariant));
            SearchText = string.Empty;
        }
        [RelayCommand] private void RemoveFromCart(CartItem itemToRemove) => CartItems.Remove(itemToRemove);
        
        [RelayCommand]
        private void IncrementQty(CartItem item)
        {
            if (item == null || item.Variant == null) return;
            int maxStock = item.Variant.StockQuantity ?? 0;
            if ((item.Quantity ?? 0) >= maxStock)
            {
                MessageBox.Show($"Stock maximum atteint ({maxStock} unité(s)).", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            item.Quantity = (item.Quantity ?? 0) + 1;
        }
        [RelayCommand]
        private void DecrementQty(CartItem item)
        {
            if (item == null) return;
            if ((item.Quantity ?? 0) > 1) item.Quantity--;
            else CartItems.Remove(item);
        }
        [RelayCommand] private void CancelEdit() => ResetPOS();
        //----------------------------------------------
        //   Check Out Command                
        [RelayCommand]
        private async Task CheckoutAsync()
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Le panier est vide !", "Action requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool remiseExcessive = CartItems.Any(item =>
                item.ManualDiscount.HasValue && item.ManualDiscount.Value > 0 &&
                item.Variant.DiscountFixed.HasValue && item.IsDiscountPinned);

            if (remiseExcessive)
            {
                var result = MessageBox.Show(
                    "Un ou plusieurs articles ont une remise manuelle supérieure à la remise fixe.\n\n" +
                    "Voulez-vous continuer ?", "Remise élevée",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result != MessageBoxResult.Yes) return;
            }

            IsLoading = true;
            try
            {
                if (IsEditMode && EditSaleId.HasValue)
                {
                    var updateRequest = new UpdateSaleRequest
                    {
                        SaleId = EditSaleId.Value,
                        GlobalDiscount = 0,
                        Items = CartItems.Select(c => new UpdateSaleItemDto
                        {
                            SaleItemId = c.SaleItemId,
                            VariantId = c.Variant!.VariantId,
                            Quantity = c.Quantity ?? 1,
                            FixedDiscountApplied = c.IsDiscountPinned,
                            ManualDiscountAmount = c.ManualDiscount
                        }).ToList()
                    };

                    if (SelectedClient != null)
                    {
                        updateRequest.ClientId = SelectedClient.ClientId;
                        updateRequest.PaidAmount = CartTotal;
                    }

                    if (IsCreditSale && SelectedClient != null)
                    {
                        updateRequest.PaidAmount = CreditPaidAmount ?? 0;
                        updateRequest.CreditExpiresAt = CreditExpiresAt;
                    }


                    var success = await _saleDataService.UpdateSaleAsync(EditSaleId.Value, updateRequest);

                    if (success)
                    {
                        var receipt = new ReceipModel
                        {
                            TicketNumber = EditTicketNumber,
                            IsEdited = true,
                            Date = DateTime.Now,
                            TotalAmount = CartTotal ?? 0,
                            TotalDiscount = TotalDiscountAmount ?? 0,
                            ClientName = SelectedClient?.FullName,
                            Items = CartItems.Select(c => new ReceiptItem
                            {
                                Designation = c.DisplayName,
                                Quantity = c.Quantity ?? 0,
                                UnitPrice = c.Variant?.SalePrice ?? 0
                            })
                            .ToList()
                        };

                        if (IsCreditSale)
                        {

                            receipt.DocumentType = "CREDIT SALE";
                            receipt.PaidAmount = CreditPaidAmount ?? 0;
                            receipt.RemainingDebt = (CartTotal ?? 0) - (CreditPaidAmount ?? 0);
                        }
                        else
                        {
                            receipt.DocumentType = "SALE";
                            receipt.PaidAmount = CartTotal ?? 0;
                            receipt.RemainingDebt = 0;
                        }


                        if (PrintTicket(receipt))
                            MessageBox.Show($"Vente modifiée – Ticket {EditTicketNumber}", "Succès",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                        _ = LoadProductsAsync();
                        ResetPOS();
                    }
                    else
                        MessageBox.Show("Erreur lors de la modification de la vente.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var saleRequest = new SaleRequest
                    {
                        GlobalDiscount = 0,
                        Items = CartItems.Select(c => new SaleItemDto
                        {
                            VariantId = c.Variant!.VariantId,
                            Quantity = c.Quantity ?? 1,
                            FixedDiscountApplied = c.IsDiscountPinned,
                            ManualDiscountAmount = c.ManualDiscount
                        }).ToList(),

                    };

                    if (SelectedClient != null)
                    {
                        saleRequest.ClientId = SelectedClient.ClientId;
                        saleRequest.PaidAmount = CartTotal;
                    }

                    if (IsCreditSale && SelectedClient != null)
                    {
                        saleRequest.PaidAmount = CreditPaidAmount ?? 0;
                        saleRequest.CreditExpiresAt = CreditExpiresAt;
                    }

                    var result = await _saleDataService.CreateSaleAsync(saleRequest);

                    if (result != null)
                    {
                        var receipt = new ReceipModel
                        {
                            TicketNumber = result.TicketNumber,
                            Date = DateTime.Now,
                            TotalAmount = CartTotal ?? 0,
                            TotalDiscount = TotalDiscountAmount ?? 0,
                            ClientName = SelectedClient?.FullName,
                            Items = CartItems.Select(c => new ReceiptItem
                            {
                                Designation = c.DisplayName,
                                Quantity = c.Quantity ?? 0,
                                UnitPrice = c.Variant?.SalePrice ?? 0
                            })
                            .ToList()
                        };

                        if (IsCreditSale)
                        {
                            receipt.DocumentType = "CREDIT SALE";
                            receipt.PaidAmount = CreditPaidAmount ?? 0;
                            receipt.RemainingDebt = (CartTotal ?? 0) - (CreditPaidAmount ?? 0);
                        }
                        else
                        {
                            receipt.DocumentType = "SALE";
                            receipt.PaidAmount = CartTotal ?? 0;
                            receipt.RemainingDebt = 0;
                        }


                        PrintTicket(receipt);

                        MessageBox.Show($"Vente validée – Ticket {result.TicketNumber}", "Succès",
                            MessageBoxButton.OK, MessageBoxImage.Information);


                        ResetPOS();



                        // await LoadProductsAsync();
                        // Reload products in background → UI stays completely responsive
                        Task.Run(() => ReloadProductsInBackground());



                    }
                    else
                        MessageBox.Show("Erreur lors de l'enregistrement de la vente.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement de la vente : " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);

            }
            finally { IsLoading = false; }
        }
        //----------------------------------------------

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
                        SaleItemId = item.SaleItemID
                    };
                    CartItems.Add(cartItem);
                }
            }
        }
        private async Task ReloadProductsInBackground()
        {
            try
            {
                await _stockService.LoadVariantsAsync();
                var filteredItems = _stockService.Variants
                    .Where(i => i.IsActive == true && i.StockQuantity > 0)
                    .ToList();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _allProducts = filteredItems;
                    _skuLookup = _allProducts
                        .Where(p => !string.IsNullOrWhiteSpace(p.Sku))
                        .ToDictionary(p => p.Sku!, StringComparer.OrdinalIgnoreCase);

                    _posProducts.Clear();
                    foreach (var p in _allProducts) _posProducts.Add(p);

                    // Rebuild categories if needed (optional, but safe)
                    var activeCategoryNames = _allProducts.Select(p => p.CategoryName).Distinct().ToHashSet();
                    Categories.Clear();
                    foreach (var cat in _allCategories.Where(c => activeCategoryNames.Contains(c.CategoryName)))
                        Categories.Add(cat);

                    FilteredProductsView?.Refresh();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur rechargement produits en arrière‑plan");
            }
        }
        private void ResetPOS()
        {
            IsEditMode = false;
            EditTicketNumber = string.Empty;
            EditSaleId = null;
            SelectedCategory = null;   // <-- corrected
            SelectedBrand = null;      // <-- corrected
            SelectedColor = "TOUT";
            SelectedSize = "TOUT";
            SearchText = string.Empty;
            IsCreditSale = false;
            CreditPaidAmount = 0;
            CreditExpiresAt = null;
            SelectedClient = null;
            ClientDisplay = "Aucun";
            ResetCartMemorySafe();
            FilteredProductsView.Refresh();
            RefreshVisibility();       // ensure we go back to stage 1
        }
        private void ResetCartMemorySafe()
        {
            foreach (var item in CartItems) item.PropertyChanged -= CartItem_PropertyChanged;
            CartItems.Clear();
        }
        private bool PrintTicket(ReceipModel receipt)
        {
            try
            {
                string printerToUse = Settings.Default.TicketPrinterName;
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

        // ------------------- reservation ------------------

        [RelayCommand]
        private async Task OpenReservationDialogAsync()
        {
            if (CartItems.Count == 0)
            {
                MessageBox.Show("Le panier est vide !", "Action requise", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedClient == null)
            {
                MessageBox.Show("Veuillez sélectionner un client avant de faire une réservation.",
                                "Client requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var total = CartTotal ?? 0;
            var reservationDialog = new ReservationDialog(total);
            if (reservationDialog.ShowDialog() == true)
            {
                await ExecuteReservationAsync(reservationDialog);
            }
        }

        private async Task ExecuteReservationAsync(ReservationDialog dialog)
        {
            var request = new CreateReservationRequest
            {
                ClientId = SelectedClient!.ClientId,
                DepositAmount = dialog.DepositAmount,
                ExpirationDate = dialog.ExpirationDate,
                Items = CartItems.Select(c => new ReservationItemDto
                {
                    DisplayName = c.DisplayName,
                    VariantId = c.Variant!.VariantId,
                    Quantity = c.Quantity ?? 1,
                    UnitPrice = c.Variant?.SalePrice ?? 0,
                    TotalDiscount = c.TotalLineDiscount
                }).ToList()
            };

            IsLoading = true;
            try
            {
                var reservationID = await _reservationDataService.CreateReservationAsync(request);

                if (reservationID.HasValue && reservationID.Value > 0)
                {
                    var receipt = new ReceipModel
                    {
                        TicketNumber = "RES-" + reservationID.ToString(),
                        Date = DateTime.Now,
                        TotalAmount = CartTotal ?? 0,
                        DepositAmount = dialog.DepositAmount,
                        RemainingDebt = dialog.Remaining,   // the debt after deposit
                        ClientName = SelectedClient?.FullName,
                        ExpirationDate = dialog.ExpirationDate,
                        DocumentType = "RESERVATION",
                        Items = CartItems.Select(c => new ReceiptItem
                        {
                            Designation = c.DisplayName,
                            Quantity = c.Quantity ?? 0,
                            UnitPrice = c.Variant?.SalePrice ?? 0
                        }).ToList()
                    };
                    PrintTicket(receipt);

                    MessageBox.Show("Réservation enregistrée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);


                    ResetPOS();
                    _ = LoadProductsAsync();
                }
                else
                    MessageBox.Show("Erreur lors de l'enregistrement de la réservation.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }


        // ------------------- End of reservation ------------------
        private async void OnStockChanged()
        {
            await Application.Current.Dispatcher.InvokeAsync(() => LoadProductsAsync());
        }

        private async void OnCatalogChanged()
        {
            await Application.Current.Dispatcher.InvokeAsync(() => LoadProductsAsync());
        }

        public void Dispose()
        {
            _catalogDataService.CatalogDataChanged -= OnCatalogChanged;
            _stockService.StockDataChanged -= OnStockChanged;
        }

    }
}