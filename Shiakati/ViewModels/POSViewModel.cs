using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Shiakati.Messages;
using Shiakati.Models;
using Shiakati.Properties;
using Shiakati.Services.Interfaces.DataServices;
using Shiakati.Services.Implementations; // ✅ Add this
using Shiakati.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ZXing;
using Shiakati.Services.Interfaces.PrintServices;
using Shiakati.Services.Interfaces.CacheService;
using System.Linq.Expressions;

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

        // ---- Stable collection for the ICollectionView ----
        private readonly ObservableCollection<ProductVariantModel> _posProducts = new();

        public POSViewModel(string name, ILogger<POSViewModel> logger, IPrintService printService,
                            ICatalogDataService catalogDataService, IStockDataService stockService,
                            ICacheService cacheService, IReservationDataService reservation, ISaleDataService saleDataService,
                            IClientDataService clientDataService)
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

            // Create the view ONCE, bound to the stable collection
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
                // Reload on the UI thread – the source collection will be updated automatically
                Application.Current.Dispatcher.InvokeAsync(() => LoadProductsAsync());
            });
        }

        private Dictionary<string, ProductVariantModel> _skuLookup = new(StringComparer.OrdinalIgnoreCase);

        [ObservableProperty] private string _tabName;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private string _searchText = string.Empty;



        [ObservableProperty] private ObservableCollection<string> _categories = new();
        [ObservableProperty] private ObservableCollection<string> _brands = new();
        [ObservableProperty] private ObservableCollection<string> _filterColors = new();
        [ObservableProperty] private ObservableCollection<string> _filterSizes = new();

        [ObservableProperty] private string _selectedCategory = "TOUT";
        [ObservableProperty] private string _selectedBrand = "TOUT";
        [ObservableProperty] private string _selectedColor = "TOUT";
        [ObservableProperty] private string _selectedSize = "TOUT";

        [ObservableProperty] private ClientSummaryDto? _selectedClient;
        [ObservableProperty] private string _clientDisplay = "Aucun";
        [ObservableProperty] private decimal? _creditPaidAmount;
        [ObservableProperty] private DateTime? _creditExpiresAt;
        [ObservableProperty] private bool isCreditSale;

        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private string _editTicketNumber = string.Empty;
        [ObservableProperty] private int? _editSaleId;

        private List<ProductVariantModel> _allProducts = new();

        // The view stays the same instance, only its content changes
        public ICollectionView FilteredProductsView { get; }
        public ObservableCollection<CartItem> CartItems { get; } = new();

        public decimal? CartSubTotal => CartItems.Sum(x => x.RawTotal ?? 0);
        public decimal? TotalDiscountAmount => CartItems.Sum(x => x.TotalLineDiscount ?? 0);
        public decimal? CartTotal => CartSubTotal - TotalDiscountAmount;
        //---------- Loading -----------------
        private void SetFiltersToTout()
        {
            SelectedCategory = "TOUT";
            SelectedBrand = "TOUT";
            SelectedColor = "TOUT";
            SelectedSize = "TOUT";
        }
        public async Task LoadProductsAsync()
        {
            SetFiltersToTout();
            try
            {
                IsLoading = true;

                await _catalogDataService.LoadCatalogAsync();

                Categories.Clear();
                Categories.Add("TOUT");
                foreach (var cat in _catalogDataService.Categories)
                    Categories.Add(cat.CategoryName);

                

                await _stockService.LoadVariantsAsync();
                _allProducts = _stockService.Variants.Where(i => i.IsActive == true && i.StockQuantity > 0).ToList();

                _skuLookup = _allProducts
                    .Where(p => !string.IsNullOrWhiteSpace(p.Sku))
                    .ToDictionary(p => p.Sku!, StringComparer.OrdinalIgnoreCase);


                // ---- Replace the content of the stable collection ----
                _posProducts.Clear();

                foreach (var p in _allProducts)
                    _posProducts.Add(p);

                // The ICollectionView automatically updates, but we call Refresh() to reapply filters
                FilteredProductsView.Refresh();

                // Rebuild filter lists (these are separate)
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du chargement des produits pour le POS.");
                MessageBox.Show("Impossible de charger le catalogue. " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ────────────── Search & Scan Detection ──────────────
        private CancellationTokenSource? _searchDebounceToken;

        partial void OnSearchTextChanged(string value)
        {
            _searchDebounceToken?.Cancel();
            _searchDebounceToken = new CancellationTokenSource();
            var token = _searchDebounceToken.Token;

            if (string.IsNullOrWhiteSpace(value))
            {
                // After a scan we skip the refresh – otherwise restore full list
                if (_skipGridRefresh)
                {
                    _skipGridRefresh = false;
                    return;          // ← no grid update after a scanned product was added
                }
                FilteredProductsView.Refresh();
                return;
            }

            // Exact SKU match? (works for both scanner and manual typing)
            if (_skuLookup.TryGetValue(value, out var exactMatch))
            {
                _skipGridRefresh = true;               // prevent the next empty‑text refresh
                AddToCart(exactMatch);                 // this will also clear SearchText
                return;
            }

            // No match – normal text search with debounce
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

            if (!string.IsNullOrWhiteSpace(SelectedCategory) && SelectedCategory != "TOUT" &&
                !string.Equals(p.CategoryName, SelectedCategory, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(SelectedBrand) && SelectedBrand != "TOUT" &&
                !string.Equals(p.BrandName, SelectedBrand, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(SelectedColor) && SelectedColor != "TOUT" &&
                !string.Equals(p.Color, SelectedColor, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrWhiteSpace(SelectedSize) && SelectedSize != "TOUT" &&
                !string.Equals(p.FullSize, SelectedSize, StringComparison.OrdinalIgnoreCase)) return false;
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
        [RelayCommand] private void ToggleCategory(string category) { SelectedCategory = ToggleValue(SelectedCategory, category); FilteredProductsView.Refresh(); }
        [RelayCommand] private void ToggleBrand(string brand) { SelectedBrand = ToggleValue(SelectedBrand, brand); FilteredProductsView.Refresh(); }
        [RelayCommand] private void ToggleColor(string color) { SelectedColor = ToggleValue(SelectedColor, color); FilteredProductsView.Refresh(); }
        [RelayCommand] private void ToggleSize(string size) { SelectedSize = ToggleValue(SelectedSize, size); FilteredProductsView.Refresh(); }
        private string? ToggleValue(string? current, string value) => string.Equals(current, value, StringComparison.OrdinalIgnoreCase) ? null : value;

        partial void OnSelectedCategoryChanged(string value) => FilteredProductsView.Refresh();
        partial void OnSelectedBrandChanged(string value) => FilteredProductsView.Refresh();
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

        private async Task ReloadProductsInBackground()
        {
            try
            {
                await _stockService.LoadVariantsAsync();

                var filteredItems = _stockService.Variants.Where(i => i.IsActive == true && i.StockQuantity > 0).ToList();

                // Build filter lists from the loaded items
                var distinctBrands = filteredItems
                    .Where(p => !string.IsNullOrWhiteSpace(p.BrandName))
                    .Select(p => p.BrandName!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(b => b).ToList();
                var distinctColors = filteredItems
                    .Where(p => !string.IsNullOrWhiteSpace(p.Color))
                    .Select(p => p.Color!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(c => c).ToList();
                var distinctSizes = filteredItems
                    .Where(p => !string.IsNullOrWhiteSpace(p.FullSize))
                    .Select(p => p.FullSize!)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s).ToList();

                // ---- Update the UI on the dispatcher thread ----
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Replace the product collection safely
                    _allProducts = filteredItems;
                    _skuLookup = _allProducts
                        .Where(p => !string.IsNullOrWhiteSpace(p.Sku))
                        .ToDictionary(p => p.Sku!, StringComparer.OrdinalIgnoreCase);

                    _posProducts.Clear();
                    foreach (var p in _allProducts) _posProducts.Add(p);

                    // Update brand / color / size filters
                    Brands.Clear(); Brands.Add("TOUT");
                    foreach (var b in distinctBrands) Brands.Add(b);

                    FilterColors.Clear(); FilterColors.Add("TOUT");
                    foreach (var c in distinctColors) FilterColors.Add(c);

                    FilterSizes.Clear(); FilterSizes.Add("TOUT");
                    foreach (var s in distinctSizes) FilterSizes.Add(s);

                    // Refresh the product grid (filters are already "TOUT")
                    FilteredProductsView?.Refresh();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur rechargement produits en arrière‑plan");
                Application.Current.Dispatcher.Invoke(() =>
                    MessageBox.Show("Impossible de recharger le catalogue.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }
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
            IsCreditSale = false;
            CreditPaidAmount = 0;
            CreditExpiresAt = null;
            SelectedClient = null;
            ClientDisplay = "Aucun";
            ResetCartMemorySafe();
            FilteredProductsView.Refresh();
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
        private async void OnCatalogChanged()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    Categories.Add("TOUT");
                    foreach (var cat in _catalogDataService.Categories)
                        Categories.Add(cat.CategoryName);

                    Brands.Clear();
                    Brands.Add("TOUT");
                    foreach (var brand in _catalogDataService.Brands)
                        Brands.Add(brand.BrandName);

                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du catalogue.");
                MessageBox.Show("Impossible de mettre à jour le catalogue. " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void OnStockChanged()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() => LoadProductsAsync());

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du stock.");
                MessageBox.Show("Impossible de mettre à jour le stock. " + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            _catalogDataService.CatalogDataChanged -= OnCatalogChanged;
            _stockService.StockDataChanged -= OnStockChanged;
        }

    }
}