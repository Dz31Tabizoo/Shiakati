using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Shiakati.Messages;
using Shiakati.Models;
using Shiakati.Properties;
using Shiakati.Services.Implementations;
using Shiakati.Services.Interfaces.DataServices;
using Shiakati.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Shiakati.Services.Interfaces.PrintServices;
using Shiakati.Services.Interfaces.CacheService;

namespace Shiakati.ViewModels
{
    public partial class StockViewModel : ObservableObject
    {
        //before the constructor, declare the services and collections7
        // ─────────────────────────────────────────────────────────
        //   Services
        // ─────────────────────────────────────────────────────────
        private readonly ILogger<StockViewModel> _logger;
        private readonly IBarCodePrintService _printerService;
        private readonly ICatalogDataService _catalogDataService;
        
        private readonly IStockDataService _stockService;
        private readonly ICacheService _cacheService;
        private readonly IPrintService _printService;

        // ─────────────────────────────────────────────────────────
        //   Private source collection (stable) and master list
        // ─────────────────────────────────────────────────────────
        private readonly ObservableCollection<ProductVariantModel> _stockSource = new();
        private List<ProductVariantModel> _allStockItems = new();

        public StockViewModel(IBarCodePrintService printerService,
                              ICatalogDataService catalogDataService,
                              IStockDataService stockService,
                              ICacheService cacheService,
                              IPrintService printServ,
                              ILogger<StockViewModel>logger)
        {
            _printerService = printerService;
            _printService = printServ;
            _catalogDataService = catalogDataService;
            _logger = logger;
            _stockService = stockService;
            _cacheService = cacheService;

            _stockService.StockDataChanged += OnStockChanged;

            // ── The view is created ONCE, bound to the stable source collection ──
            FilteredStockView = CollectionViewSource.GetDefaultView(_stockSource);
            FilteredStockView.Filter = StockFilter;

            // Initialise other collections
            
            FilteredBrands = CollectionViewSource.GetDefaultView(Brands);
            Products = new ObservableCollection<ProductModel>();
            FilterColors = new ObservableCollection<string>();
            FilterSizes = new ObservableCollection<string>();
            AllColors = new ObservableCollection<string>();
            WidthsList = new ObservableCollection<string> { "XS", "S", "M", "L", "XL", "XXL", "XXXL", "1", "2", "3", "4", "K" };
        }

        // ─────────────────────────────────────────────────────────
        //   Exposed collections
        // ─────────────────────────────────────────────────────────

        public ObservableCollection<CategoryModel> Categories { get; } = new();
        public ObservableCollection<BrandsModel> Brands { get; } = new();
        public ICollectionView FilteredBrands { get; private set; }
        public ObservableCollection<ProductModel> Products { get; }

        /// <summary>
        /// The DataGrid binds to this property. It never changes – only its content changes.
        /// </summary>
        public ICollectionView FilteredStockView { get; }

        [ObservableProperty] private int _totalFilteredQuantity;

        public ObservableCollection<string> FilterColors { get; }
        public ObservableCollection<string> FilterSizes { get; }
        public ObservableCollection<string> AllColors { get; }
        public ObservableCollection<string> WidthsList { get; }

        // ─────────────────────────────────────────────────────────
        //   UI States
        // ─────────────────────────────────────────────────────────
        [ObservableProperty] private bool _isSaving;
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isReceptionVisible;
        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private bool _isNumericSizeVisible;
        [ObservableProperty] private bool _isDimensionSizeVisible = true;
        [ObservableProperty] private bool _printLabelsOnSave = true;
        [ObservableProperty] private bool _isNonActiveItemsVisible = false;
        [ObservableProperty] private bool _isManualSkuEnabled;
        [ObservableProperty] private bool _isNotEditMode = true;

        // ─────────────────────────────────────────────────────────
        //   Filters & Selection
        // ─────────────────────────────────────────────────────────
        [ObservableProperty] private string _searchText;
        [ObservableProperty] private CategoryModel _selectedCategory;
        [ObservableProperty] private BrandsModel _selectedBrand;
        [ObservableProperty] private string _filterColor;
        [ObservableProperty] private string _filterFullSize;
        [ObservableProperty] private ProductVariantModel _selectedStockItem;

        // ─────────────────────────────────────────────────────────
        //   Form Draft Fields
        // ─────────────────────────────────────────────────────────
        [ObservableProperty] private CategoryModel _draftCategory;
        [ObservableProperty] private BrandsModel _draftBrand;
        [ObservableProperty] private string _draftProductName;
        [ObservableProperty] private string _draftSKU;
        [ObservableProperty] private decimal? _draftPurchasePrice;
        [ObservableProperty] private decimal? _draftSalePrice;
        [ObservableProperty] private decimal? _draftFixedDiscount;
        [ObservableProperty] private int? _draftQuantity;
        [ObservableProperty] private string _draftColor;
        [ObservableProperty] private string _draftNumericSize;
        [ObservableProperty] private string _draftWidth;
        [ObservableProperty] private string _draftLength;
        [ObservableProperty] private int _labelsToPrint = 1;
        [ObservableProperty] private string _draftNewBrandName = string.Empty;
        [ObservableProperty] private ProductModel? _draftSelectedProduct;

        // ─────────────────────────────────────────────────────────
        //   Data Loading
        // ─────────────────────────────────────────────────────────
        public async Task LoadInitialDataAsync(bool forceRefresh = false)
        {
            if (!forceRefresh && (IsLoading || _allStockItems.Any()))
                return;
            try
            {
                IsLoading = true;

                await _catalogDataService.LoadCatalogAsync();


                // Products may not be available on all endpoints – we catch the exception
                List<ProductModel> prods = new();
                try
                {
                     await _stockService.LoadProductsAsync();
                    prods = _stockService.Products.ToList();

                }
                catch { /* ignore – the grid works without this list */ }

                await _stockService.LoadVariantsAsync();

                _allStockItems = _stockService.Variants.ToList();

                var distinctColors = _allStockItems
                    .Select(i => i.Color)
                    .Where(c => !string.IsNullOrWhiteSpace(c))
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();
                var distinctSizes = _allStockItems
                    .Select(i => i.FullSize)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .OrderBy(s => s)
                    .ToList();

                 Application.Current.Dispatcher.Invoke(() =>
                {
                    // Replace the entire content of the stable source collection
                    _stockSource.Clear();
                    foreach (var item in _allStockItems)
                        _stockSource.Add(item);

                    // Rebuild filter dropdowns
                    Categories.Clear();
                    
                    foreach (var c in _catalogDataService.Categories)
                        Categories.Add(c);

                    Brands.Clear();
                     
                    foreach (var b in _catalogDataService.Brands)
                        Brands.Add(b);

                    Products.Clear();
                    foreach (var p in prods) Products.Add(p);

                    UpdateFilterOptions(distinctColors, distinctSizes);

                    // Refresh the view – the filter is reapplied automatically
                    RefreshFilteredView();
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ─────────────────────────────────────────────────────────
        //   Filter predicate (used by the ICollectionView)
        // ─────────────────────────────────────────────────────────
        private bool StockFilter(object obj)
        {
            if (obj is not ProductVariantModel p) return false;

            // Soft‑delete filter
            if (!IsNonActiveItemsVisible && p.IsActive != true)
                return false;

            // Text search (product name or SKU)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                if (!(p.ProductName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                      p.Sku?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true))
                    return false;
            }

            // Dropdown filters
            if (SelectedCategory != null && 
                !string.Equals(p.CategoryName, SelectedCategory.CategoryName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (SelectedBrand != null &&
                !string.Equals(p.BrandName, SelectedBrand.BrandName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(FilterColor) &&
                !string.Equals(p.Color, FilterColor, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(FilterFullSize) &&
                !string.Equals(p.FullSize, FilterFullSize, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        // ─────────────────────────────────────────────────────────
        //   Filter refresh triggers
        // ─────────────────────────────────────────────────────────
        private CancellationTokenSource? _searchDebounceToken;

        partial void OnSearchTextChanged(string value) => DebounceRefresh();
        partial void OnSelectedCategoryChanged(CategoryModel value) => RefreshFilteredView();
        partial void OnSelectedBrandChanged(BrandsModel value) => RefreshFilteredView();
        partial void OnFilterColorChanged(string value) => RefreshFilteredView();
        partial void OnFilterFullSizeChanged(string value) => RefreshFilteredView();
        partial void OnIsNonActiveItemsVisibleChanged(bool value) => RefreshFilteredView();
        partial void OnIsEditModeChanged(bool value) => IsNotEditMode = !value;

        partial void OnDraftCategoryChanged(CategoryModel value)
        {
            if (value == null) return;
            IsDimensionSizeVisible = value.CategoryName.Contains("Thob") ||
                                     value.CategoryName.Contains("Pantalon") ||
                                     value.CategoryName.Contains("Sous");
            IsNumericSizeVisible = !IsDimensionSizeVisible;

            FilteredBrands.Filter = obj => obj is BrandsModel brand && brand.CategoryID == value.CategoryID;
            FilteredBrands.Refresh();
        }

        // ─────────────────────────────────────────────────────────
        //   Commands
        // ─────────────────────────────────────────────────────────
        [RelayCommand] private void ToggleReception() { IsReceptionVisible = !IsReceptionVisible; IsEditMode = false; ClearDraft(); }

        [RelayCommand] private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategory = null;
            SelectedBrand = null;
            FilterColor = null;
            FilterFullSize = null;
            IsManualSkuEnabled = false;
            DraftSKU = string.Empty;
            RefreshFilteredView();
        }

        [RelayCommand]
        private void PrepareEdit(ProductVariantModel item)
        {
            if (item == null) return;
            IsEditMode = true;
            IsReceptionVisible = true;

            DraftCategory = Categories.FirstOrDefault(c => c.CategoryName == item.CategoryName);
            DraftBrand = Brands.FirstOrDefault(b => b.BrandName == item.BrandName);

            DraftProductName = item.ProductName;
            DraftSelectedProduct = Products.FirstOrDefault(p => p.ProductName == item.ProductName && p.BrandName == item.BrandName);

            IsManualSkuEnabled = false;
            DraftSKU = item.Sku;
            DraftPurchasePrice = item.PurchasePrice;
            DraftSalePrice = item.SalePrice;
            DraftFixedDiscount = item.DiscountFixed;
            DraftQuantity = item.StockQuantity;
            DraftColor = item.Color;

            if (IsDimensionSizeVisible)
            {
                DraftWidth = item.Width;
                DraftLength = item.Length?.ToString();
                DraftNumericSize = string.Empty;
            }
            else if (IsNumericSizeVisible)
            {
                DraftNumericSize = item.Length?.ToString() ?? string.Empty;
                DraftWidth = string.Empty;
                DraftLength = string.Empty;
            }
            else
            {
                DraftNumericSize = string.Empty;
                DraftWidth = string.Empty;
                DraftLength = string.Empty;
            }
        }

        [RelayCommand]
        private async Task PrintFromGridAsync(ProductVariantModel item)
        {
            try
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Sku))
                {
                    MessageBox.Show("Code‑barres manquant pour cet article.", "Impossible d'imprimer", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string printerToUse = Settings.Default.BarcodePrinterName;
                if (string.IsNullOrWhiteSpace(printerToUse))
                {
                    MessageBox.Show("Aucune imprimante code‑barres sélectionnée dans les paramètres.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Window? owner = Application.Current.MainWindow;
                if (owner == null || owner is PrintQuantityDialog)
                {
                    owner = Application.Current.Windows
                               .OfType<Window>()
                               .FirstOrDefault(w => w.IsActive && !(w is PrintQuantityDialog))
                            ?? Application.Current.Windows
                                   .OfType<Window>()
                                   .FirstOrDefault(w => !(w is PrintQuantityDialog));
                }

                var dialog = new PrintQuantityDialog { Quantity = item.StockQuantity ?? 1 };
                if (owner != null) dialog.Owner = owner;

                if (dialog.ShowDialog() == true)
                {
                    _printerService.PrintBarCode(new BarecodeLabelData
                    {
                        VariantName = item.ProductName,
                        Barcode = item.Sku,
                        BrandName = item.BrandName,
                        Price = item.SalePrice.GetValueOrDefault(),
                        ProductSize = item.FullSize
                    }, printerToUse, dialog.Quantity);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression du code-barres : {ex.Message}", "Erreur d'impression", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task ReceiveStockAsync()
        {
            if (IsSaving) return;
            IsSaving = true;
            if (DraftCategory?.CategoryID <= 0)
            {
                MessageBox.Show("Category must be selected.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int numericSize = 0;
            bool hasNumericSize = int.TryParse(DraftNumericSize, out numericSize);

            try
            {
                if (IsEditMode && SelectedStockItem != null)
                {
                    var updateRequest = new UpdateVariantRequest
                    {
                        VariantId = SelectedStockItem.VariantId,
                        CategoryId = DraftCategory?.CategoryID,
                        BrandId = (DraftBrand != null && DraftBrand.BrandID > 0) ? DraftBrand.BrandID : null,
                        BrandName = (DraftBrand == null || DraftBrand.BrandID <= 0) && !string.IsNullOrWhiteSpace(DraftNewBrandName)
                                      ? DraftNewBrandName : null,
                        ProductId = DraftSelectedProduct?.ProductID,
                        ProductName = DraftSelectedProduct?.ProductName ?? DraftProductName,
                        Color = DraftColor,
                        PurchasePrice = DraftPurchasePrice,
                        SalePrice = DraftSalePrice,
                        DiscountFixed = DraftFixedDiscount,
                        StockQuantity = DraftQuantity
                    };

                    if (IsNumericSizeVisible)
                    {
                        updateRequest.Length = hasNumericSize ? numericSize : null;
                        updateRequest.Width = null;
                    }
                    else if (IsDimensionSizeVisible)
                    {
                        updateRequest.Length = int.TryParse(DraftLength?.ToString(), out int l) ? l : null;
                        updateRequest.Width = string.IsNullOrWhiteSpace(DraftWidth) ? null : DraftWidth;
                    }
                    if (!IsFormValid()) return;

                    var UpdatedVariant = await _stockService.UpdateProductVariantAsync(updateRequest);

                    if (UpdatedVariant != null)
                    {
                        
                        await ForceReloadAllDataAsync();
                        MessageBox.Show("Article modifié avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                        await PrintBarCodeOnReciveStock(UpdatedVariant, LabelsToPrint);
                        IsReceptionVisible = false;
                        IsEditMode = false;
                        ClearDraft();
                        WeakReferenceMessenger.Default.Send(new StockUpdatedMessage());
                    }
                    else
                    {
                        MessageBox.Show("Erreur lors de la modification.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    if (!IsBulkMode)
                    {
                        if (!IsFormValid()) return;

                        var request = new AddVariantRequest
                        {
                            
                            CategoryId = (DraftCategory?.CategoryID > 0) ? DraftCategory.CategoryID : throw new InvalidOperationException("Category must be selected."),

                            BrandId = DraftBrand?.BrandID > 0 ? DraftBrand.BrandID : null,
                            BrandName = (DraftBrand?.BrandID == 0 || DraftBrand == null) ? DraftNewBrandName : null,
                            ProductName = DraftSelectedProduct?.ProductName ?? DraftProductName,
                            Sku = string.IsNullOrWhiteSpace(DraftSKU) ? null : DraftSKU,
                            Color = string.IsNullOrWhiteSpace(DraftColor) ? null : DraftColor,
                            PurchasePrice = DraftPurchasePrice,
                            SalePrice = DraftSalePrice,
                            DiscountFixed = DraftFixedDiscount,
                            StockQuantity = DraftQuantity.GetValueOrDefault()
                        };

                        if (IsNumericSizeVisible)
                        {
                            request.Length = hasNumericSize ? numericSize : null;
                            request.Width = null;
                        }
                        else if (IsDimensionSizeVisible)
                        {
                            request.Length = int.TryParse(DraftLength?.ToString(), out int length) ? length : null;
                            request.Width = string.IsNullOrWhiteSpace(DraftWidth) ? null : DraftWidth;
                        }
                        var newVariant = await _stockService.AddProductVariantAsync(request);

                        if (newVariant != null)
                        {
                            
                            IsLoading = false;
                            await LoadInitialDataAsync(forceRefresh: true);
                            MessageBox.Show("Stock enregistré avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                            await PrintBarCodeOnReciveStock(newVariant, LabelsToPrint);
                            IsReceptionVisible = false;
                            IsEditMode = false;
                            ClearDraft();
                            WeakReferenceMessenger.Default.Send(new StockUpdatedMessage());
                        }
                        else
                        {
                            IsLoading = false;
                            MessageBox.Show("Erreur lors de l'enregistrement.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        // Bulk add
                        if (BulkVariants.Count == 0)
                        {
                            MessageBox.Show("Aucune variante à ajouter.", "Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var skus = BulkVariants
                                        .Select(v => v.Sku?.Trim())
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .ToList();


                        if (skus.Count != skus.Distinct(StringComparer.OrdinalIgnoreCase).Count())
                        {
                            MessageBox.Show("Certaines variantes ont le même Code à Barres. Veuillez vérifier.", "Code à Barres en double", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        var bulkRequest = new BulkAddVariantsRequest
                        {
                            CategoryId = DraftCategory.CategoryID,
                            BrandId = DraftBrand?.BrandID > 0 ? DraftBrand.BrandID : null,
                            BrandName = (DraftBrand?.BrandID == 0 || DraftBrand == null) ? DraftNewBrandName : null,
                            ProductName = DraftSelectedProduct?.ProductName ?? DraftProductName,
                            Variants = BulkVariants.ToList()
                        };

                        var result = await _stockService.BulkAddVariantsAsync(bulkRequest);

                        if (result != null && result.Any())
                        {
                            
                            IsLoading = false;
                            await LoadInitialDataAsync(forceRefresh: true);
                            MessageBox.Show($"{result.Count} variantes enregistrées avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                            BulkVariants.Clear();
                            IsBulkMode = false;
                            IsReceptionVisible = false;
                            ClearDraft();
                            WeakReferenceMessenger.Default.Send(new StockUpdatedMessage());
                        }
                        else
                        {
                            IsLoading = false;
                            MessageBox.Show("Erreur lors de l'enregistrement groupé.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }

                }
            }
            catch (InvalidOperationException ex) when (ex.Message == "Category must be selected.")
            {
                MessageBox.Show("Please select a category before receiving stock.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                // log and show generic error
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsSaving = false;
            }
        }
    


        [RelayCommand]
        private async Task DeleteStockItemAsync(ProductVariantModel item)
        {
            if (item == null) return;

            var result = MessageBox.Show(
                $"Êtes-vous sûr de vouloir supprimer définitivement l'article :\n\"{item.ProductName}\" (SKU: {item.Sku}) ?",
                "Confirmation de suppression",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No) return;

            try
            {
                IsLoading = true;

                var updateRequest = new UpdateVariantRequest
                {
                    VariantId = item.VariantId,
                    IsActive = false
                };

                var UpdatedVariant = await _stockService.UpdateProductVariantAsync(updateRequest);

                if (UpdatedVariant != null && !string.IsNullOrWhiteSpace(UpdatedVariant.BrandName))
                {
                   
                    await ForceReloadAllDataAsync();
                    WeakReferenceMessenger.Default.Send(new StockUpdatedMessage());
                    MessageBox.Show("Article supprimé du stock avec succès !", "Suppression réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Erreur lors de la suppression de l'article en base de données.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Une erreur critique est survenue: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task PrintBarCodeOnReciveStock(ProductVariantResponse newVariant, int Copies)
        {
            try
            {
                if (PrintLabelsOnSave)
                {
                    var label = new BarecodeLabelData
                    {
                        Barcode = newVariant.Sku,
                        BrandName = newVariant.BrandName,
                        VariantName = newVariant.ProductName,
                        ProductSize = newVariant.FullSize,
                        Price = newVariant.SalePrice ?? 0,
                    };

                    _printerService.PrintBarCode(label, Settings.Default.BarcodePrinterName, Copies);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'impression du code-barres: {ex.Message}", "Erreur d'impression", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand] private void StockAddingFiledsClear() => ClearDraft();

        // ─────────────────────────────────────────────────────────
        //   Helpers
        // ─────────────────────────────────────────────────────────
        private void DebounceRefresh()
        {
            _searchDebounceToken?.Cancel();
            _searchDebounceToken = new CancellationTokenSource();
            var token = _searchDebounceToken.Token;
            Task.Delay(500, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                    Application.Current.Dispatcher.Invoke(() => RefreshFilteredView());
            }, token);
        }

        private async Task ForceReloadAllDataAsync()
        {
            
            await LoadInitialDataAsync(forceRefresh: true);
        }

        private void UpdateFilterOptions(List<string> distinctColors, List<string> distinctSizes)
        {
            AllColors.Clear();
            foreach (var c in distinctColors) AllColors.Add(c);
            FilterColors.Clear();
            foreach (var c in distinctColors) FilterColors.Add(c);
            FilterSizes.Clear();
            foreach (var s in distinctSizes) FilterSizes.Add(s);
        }

        private void ClearDraft()
        {
            IsEditMode = false;
            DraftSKU = string.Empty;
            DraftCategory = null;
            DraftBrand = null;
            DraftProductName = string.Empty;
            DraftColor = string.Empty;
            DraftNumericSize = string.Empty;
            DraftWidth = null;
            DraftLength = string.Empty;
            DraftPurchasePrice = null;
            DraftFixedDiscount = null;
            DraftSalePrice = null;
            DraftQuantity = null;
            LabelsToPrint = 1;
            DraftNewBrandName = string.Empty;
            DraftSelectedProduct = null;
            BulkVariants.Clear();
            IsBulkMode = false;
            IsSimpleMode = true;
        }

        private bool IsFormValid()
        {
            if (DraftCategory == null)
            {
                MessageBox.Show("Veuillez sélectionner une catégorie.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            bool hasSelectedBrand = DraftBrand != null && DraftBrand.BrandID > 0;
            bool hasTypedBrand = !string.IsNullOrWhiteSpace(DraftNewBrandName);
            if (!hasSelectedBrand && !hasTypedBrand)
            {
                MessageBox.Show("Veuillez sélectionner une marque existante ou saisir un nouveau nom de marque.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            bool hasSelectedProduct = DraftSelectedProduct != null;
            bool hasTypedProduct = !string.IsNullOrWhiteSpace(DraftProductName);
            if (!hasSelectedProduct && !hasTypedProduct)
            {
                MessageBox.Show("Veuillez sélectionner un produit existant ou saisir un nouveau nom de produit.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (DraftPurchasePrice == null || DraftPurchasePrice <= 0)
            {
                MessageBox.Show("Veuillez saisir un prix d'achat valide supérieur à 0 DA.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (DraftSalePrice == null || DraftSalePrice <= 0)
            {
                MessageBox.Show("Veuillez saisir un prix de vente valide supérieur à 0 DA.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (DraftSalePrice < DraftPurchasePrice)
            {
                var result = MessageBox.Show("Le prix de vente est inférieur au prix d'achat. Voulez-vous continuer ?",
                                             "Attention", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return false;
            }
            if (IsNumericSizeVisible && string.IsNullOrWhiteSpace(DraftNumericSize))
            {
                MessageBox.Show("Veuillez renseigner la taille numérique de l'article.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            else if (IsDimensionSizeVisible && (string.IsNullOrWhiteSpace(DraftWidth) && string.IsNullOrWhiteSpace(DraftLength)))
            {
                MessageBox.Show("Veuillez renseigner la largeur et la longueur de l'article.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (DraftQuantity == null || DraftQuantity <= 0)
            {
                MessageBox.Show("Veuillez saisir une quantité reçue valide (minimum 1 unité).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (LabelsToPrint > 50)
            {
                var result = MessageBox.Show("La quantité reçue est très élevée. Êtes-vous sûr de vouloir Imprimer autant d'unités ?",
                                             "Attention", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No) return false;
            }
            return true;
        }

        private void RefreshFilteredView()
        {
            FilteredStockView?.Refresh();
            UpdateTotalFilteredQuantity();
        }

        // inventaire 

        [RelayCommand] private async Task PrintInventory()
        {
            // 1. Build list of unique (BrandName, ProductName) from the master list
            var productGroups = _allStockItems
                .Select(v => new { v.BrandName, v.ProductName })
                .Distinct()
                .OrderBy(g => g.BrandName)
                .ThenBy(g => g.ProductName)
                .Select(g => new ProductSelectionItem
                {
                    BrandName = g.BrandName ?? "",
                    ProductName = g.ProductName ?? ""
                })
                .ToList();

            if (!productGroups.Any())
            {
                MessageBox.Show("Aucun produit dans le stock.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 2. Show selection dialog
            var dialog = new ProductSelectionForInventoryDialog(productGroups) { Owner = Application.Current.MainWindow };
            if (dialog.ShowDialog() != true) return;

            var selectedProducts = dialog.SelectedProducts;

            string printerName = Properties.Settings.Default.TicketPrinterName;
            if (string.IsNullOrWhiteSpace(printerName))
            {
                MessageBox.Show("Aucune imprimante ticket configurée.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                foreach (var product in selectedProducts)
                {
                    // Get ALL variants for this product, sorted by color
                    var variants = _allStockItems
                        .Where(v => v.BrandName == product.BrandName && v.ProductName == product.ProductName)
                        .OrderBy(v => v.Color)
                        .ToList();

                    if (!variants.Any()) continue;

                    // Build a receipt with one ReceiptItem per variant
                    var receipt = new ReceipModel
                    {
                        TicketNumber = $"INV-{DateTime.Now:yyyyMMddHHmmss}",
                        Date = DateTime.Now,
                        DocumentType = "INVENTORY",
                        BrandName = product.BrandName,
                        ProductName = product.ProductName,
                        Items = variants.Select(v => new ReceiptItem
                        {
                            Designation = $"{v.Sku} | {v.Color} | {v.FullSize} | Qté: {v.StockQuantity}",
                            Quantity = v.StockQuantity ?? 0
                        }).ToList()
                    };

                    PrintTicket(receipt);
                }

                MessageBox.Show($"Inventaire imprimé : {selectedProducts.Count} produit(s).", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur d'impression : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PrintTicket(ReceipModel receipt)
        {
            string printerName = Properties.Settings.Default.TicketPrinterName;
            _printService.PrintReceipt(receipt, printerName);
        }
        private void UpdateTotalFilteredQuantity()
        {
            if (FilteredStockView == null)
            {
                TotalFilteredQuantity = 0;
                return;
            }

            int total = 0;
            foreach (ProductVariantModel item in FilteredStockView)
            {
                total += item.StockQuantity ?? 0;
            }
            TotalFilteredQuantity = total;
        }

        // Buld Reception from stock 

        // Bulk mode properties
        [ObservableProperty] private bool _isBulkMode;
        [ObservableProperty] private bool _isSimpleMode = true;   // default
        [ObservableProperty] private ObservableCollection<VariantDetail> _bulkVariants = new();

        partial void OnIsBulkModeChanged(bool value) => IsSimpleMode = !value;
        partial void OnIsSimpleModeChanged(bool value) => IsBulkMode = !value;

        // Add / remove bulk variant
        [RelayCommand]
        private void AddBulkVariant()
        {
            

            int numericSize = 0;
            bool hasNumericSize = int.TryParse(DraftNumericSize, out numericSize);

            

            var varDetail = new VariantDetail
            {
                Sku = string.IsNullOrWhiteSpace(DraftSKU) ? null : DraftSKU,
                Color = string.IsNullOrWhiteSpace(DraftColor) ? null : DraftColor,                
                PurchasePrice = DraftPurchasePrice,
                SalePrice = DraftSalePrice,
                DiscountFixed = DraftFixedDiscount,
                StockQuantity = DraftQuantity.GetValueOrDefault()
            };

            if (IsNumericSizeVisible)
            {
                varDetail.Length = hasNumericSize ? numericSize : null;
                varDetail.Width = null;
            }
            else if (IsDimensionSizeVisible)
            {
                varDetail.Length = int.TryParse(DraftLength?.ToString(), out int length) ? length : null;
                varDetail.Width = string.IsNullOrWhiteSpace(DraftWidth) ? null : DraftWidth;
            }

            if (IsFormValid())
            {
                BulkVariants.Add(varDetail);
                ClearVariantsDrafts();
            } else
                return;
                   
                    
        }

        [RelayCommand]
        private void RemoveBulkVariant(VariantDetail variant)
        {
            BulkVariants.Remove(variant);
        }

        private void ClearVariantsDrafts()
        {
            DraftSKU = string.Empty;
            DraftColor = string.Empty;
            DraftNumericSize = string.Empty;
            DraftWidth = null;
            DraftLength = string.Empty;
            DraftQuantity = null;
        }


        private async void OnStockChanged()
        {
            try
            {
                await Application.Current.Dispatcher.InvokeAsync(() => LoadInitialDataAsync(forceRefresh: true));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Erreur lors de la mise à jour du stock.");
                MessageBox.Show($"Erreur lors de la mise à jour du stock : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void Dispose()
        {
            _stockService.StockDataChanged -= OnStockChanged;
        }
    }
}