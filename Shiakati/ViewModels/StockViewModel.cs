using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using global::Shiakati.Models;
using Serilog;
using Shiakati.Helpers;
using Shiakati.Services.Implementations;
using Shiakati.Services.Interfaces;
using Shiakati.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using static MaterialDesignThemes.Wpf.Theme.ToolBar;


namespace Shiakati.ViewModels
{   
    public partial class StockViewModel : ObservableObject
    {
        // ===========================================================
        // I. SERVICES
        // ===========================================================
        private readonly IBarCodePrintService _printerService;
        private readonly ICatalogService _catalogDb;
        private readonly IProductsService _productsService;
        private readonly IProductVariantsService _stockService;
        private readonly ICacheService _cacheService;

        public StockViewModel(IBarCodePrintService printerService, ICatalogService db,
                              IProductVariantsService stockService, ICacheService cacheService, IProductsService productsService)
        {
            _printerService = printerService;
            _catalogDb = db;
            _productsService = productsService;
            _stockService = stockService;
            _cacheService = cacheService;

            // Initialisation des collections
            Categories = new ObservableCollection<CategoryModel>();
            Brands = new ObservableCollection<BrandsModel>();
            Products = new ObservableCollection<ProductModel>();
            FilteredStock = new RangeObservableCollection<ProductVariantModel>();
            FilterColors = new ObservableCollection<string>();
            FilterSizes = new ObservableCollection<string>();
            AllColors = new ObservableCollection<string> ();
            WidthsList = new ObservableCollection<string> { "XS", "S", "M","L","XL","XXL","XXXL","1","2","3","4" };
        }

        // ===========================================================
        // II. COLLECTIONS
        // ===========================================================
        public ObservableCollection<CategoryModel> Categories { get; }
        public ObservableCollection<BrandsModel> Brands { get; }
        public ObservableCollection<ProductModel> Products { get; }
        public RangeObservableCollection<ProductVariantModel> FilteredStock { get; }
        public ObservableCollection<string> FilterColors { get; }
        public ObservableCollection<string> FilterSizes { get; }
        public ObservableCollection<string> AllColors { get; }
        public ObservableCollection<string> WidthsList { get; }

        private List<ProductVariantModel> _allStockItems = new();

        // ===========================================================
        // III. UI STATES
        // ===========================================================
        [ObservableProperty] private bool _isLoading;
        [ObservableProperty] private bool _isReceptionVisible;
        [ObservableProperty] private bool _isEditMode;
        [ObservableProperty] private bool _isNumericSizeVisible;
        [ObservableProperty] private bool _isDimensionSizeVisible = true;
        [ObservableProperty] private bool _printLabelsOnSave = true;

        // ===========================================================
        // IV. FILTERS & SELECTION
        // ===========================================================
        [ObservableProperty] private string _searchText;
        [ObservableProperty] private CategoryModel _selectedCategory;
        [ObservableProperty] private BrandsModel _selectedBrand;
        [ObservableProperty] private string _filterColor;
        [ObservableProperty] private string _filterFullSize;
        [ObservableProperty] private ProductVariantModel _selectedStockItem;

        // ===========================================================
        // V. FORM FIELDS (DRAFT)
        // ===========================================================
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


        // ===========================================================
        // VI. COMMANDS
        // ===========================================================
         
        public async Task LoadInitialDataAsync(bool forceRefresh = false)
        {
            if (IsLoading || (!forceRefresh && _allStockItems.Any())) 
                return;
            try
            {
                IsLoading = true;
                var catalog = await _cacheService.GetOrLoadAsync<(List<BrandsModel> Brands, List<CategoryModel> Categories)>(
                                    CacheKeys.Catalog,
                                    () => _catalogDb.GetInitialGatalogDataAsync());

                var prods = await _cacheService.GetOrLoadAsync(CacheKeys.Products, _productsService.GetProductsAsync);
                var items = await _cacheService.GetOrLoadAsync(CacheKeys.StockVariants, _stockService.GetProductVariantsAsync);

                _allStockItems = items.ToList();

                var distinctColors = _allStockItems.Select(i => i.Color)
                                        .Where(c => !string.IsNullOrWhiteSpace(c))
                                        .Distinct()
                                        .OrderBy(c => c)
                                        .ToList();
                var distinctSizes = _allStockItems.Select(i => i.FullSize)
                                                    .Where(s => !string.IsNullOrWhiteSpace(s))
                                                    .Distinct()
                                                    .OrderBy(s => s)
                                                    .ToList();

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                   
                    FilteredStock.Clear();
                    FilteredStock.AddRange(_allStockItems);

                    Categories.Clear();
                    foreach (var c in catalog.Categories) Categories.Add(c);

                    Brands.Clear();
                    foreach (var b in catalog.Brands) Brands.Add(b);

                    Products.Clear();
                    foreach (var p in prods) Products.Add(p);

                    UpdateFilterOptions(distinctColors,distinctSizes);
                });
            }
            catch
            {
                MessageBox.Show("Probleme de connexion", "Data Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand]
        private void ToggleReception() { IsReceptionVisible = !IsReceptionVisible; IsEditMode = false; ClearDraft(); }

        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategory = null;
            SelectedBrand = null;
            FilterColor = null;
            FilterFullSize = null;
        }

        [RelayCommand]
        private void PrepareEdit(ProductVariantModel item)
        {
            if (item == null) return;
            IsEditMode = true;
            IsReceptionVisible = true;

            DraftSKU = item.Sku;
            DraftProductName = item.ProductName;
            DraftPurchasePrice = item.PurchasePrice;
            DraftSalePrice = item.SalePrice;
            DraftQuantity = item.StockQuantity;
            DraftColor = item.Color;
            DraftCategory = Categories.FirstOrDefault(c => c.CategoryName == item.CategoryName);
            DraftBrand = Brands.FirstOrDefault(b => b.BrandName == item.BrandName);
            // Logique pour les tailles à ajouter selon ton modèle
        }

        [RelayCommand]
        private async Task PrintFromGridAsync(ProductVariantModel item)
        {
            string printerToUse = Properties.Settings.Default.BarcodePrinterName;
            if (item == null) return;
            var dialog = new Shiakati.Views.PrintQuantityDialog { Owner = App.Current.MainWindow };
            dialog.Quantity = item.StockQuantity ?? 1;
            if (dialog.ShowDialog() == true)
            {
                 _printerService.PrintBarCode(new BarecodeLabelData
                {
                    VariantName = item.ProductName,
                    Barcode = item.Sku,
                    BrandName = item.BrandName,
                    Price = item.SalePrice.GetValueOrDefault(),
                    ProductSize = item.FullSize

                } , printerToUse,dialog.Quantity );
            }
        }

        [RelayCommand]
        private async Task ReceiveStockAsync()
        {
            // Sécurité de base : validation des champs obligatoires
            if (DraftCategory == null || string.IsNullOrWhiteSpace(DraftProductName))
            {
                MessageBox.Show("Veuillez sélectionner au moins une catégorie et saisir un nom de produit.",
                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int numericSize = 0;
            bool hasNumericSize = int.TryParse(DraftNumericSize, out numericSize);

            // Préparation de la requête pour l'API
            var request = new AddVariantRequest
            {
                CategoryId = DraftCategory.CategoryID,
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

            // Gestion dynamique des tailles
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

            try
            {
                IsLoading = true; // On affiche le spinner pendant l'écriture en DB

                // 🔥 APPEL SERVICE HTTP
                bool isSuccess = await _stockService.AddProductVariantAsync(request);

                if (isSuccess)
                {
                    // Invalidation du Cache
                    _cacheService.Remove(CacheKeys.StockVariants);

                    if (request.BrandId == null && !string.IsNullOrWhiteSpace(request.BrandName))
                    {
                        _cacheService.Remove(CacheKeys.Catalog);
                    }

                    _allStockItems.Clear();

                    // On attend que la DB locale applique les modifications
                    await Task.Delay(400);

                    // On libère le flag IsLoading de la sauvegarde pour que LoadInitialDataAsync puisse s'exécuter !
                    IsLoading = false;

                    // Rechargement immédiat (qui va remettre IsLoading à true le temps du téléchargement)
                    await LoadInitialDataAsync(forceRefresh: true);

                    MessageBox.Show(IsEditMode ? "Stock modifié avec succès !" : "Stock enregistré avec succès !",
                                    "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                                       

                    await PrintBarCodeOnReciveStock(request.ProductName,LabelsToPrint);

                    IsReceptionVisible = false;
                    IsEditMode = false;
                    ClearDraft();
                }
                else
                {
                    IsLoading = false; // Ne pas oublier de le couper ici aussi en cas d'échec serveur
                    MessageBox.Show("Le serveur a refusé l'enregistrement de l'article. Vérifiez les données.",
                                    "Erreur Serveur", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                IsLoading = false; // Ne pas oublier de le couper ici aussi en cas de crash réseau
                MessageBox.Show($"Une erreur est survenue lors de l'envoi : {ex.Message}",
                                "Erreur réseau", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PrintBarCodeOnReciveStock(string newProduct, int stockQuantity)
        {
            var newItem = _allStockItems.FirstOrDefault(x => x.ProductName == newProduct);
            var label = new BarecodeLabelData { 
                Barcode = newItem.Sku,
                BrandName = newItem.BrandName,
                VariantName = newItem.ProductName,
                ProductSize = newItem.FullSize,
                Price = newItem.SalePrice?? 0,
            };

            _printerService.PrintBarCode(label, Properties.Settings.Default.BarcodePrinterName, stockQuantity);
        }

        // ===========================================================
        // VII. HELPERS & TRIGGERS
        // ===========================================================

        partial void OnDraftCategoryChanged(CategoryModel value)
        {
            if (value == null) return;
            // Exemple : Si catégorie Chaussures -> Numérique, sinon Dimensions
            IsNumericSizeVisible = value.CategoryName.Contains("Chaussures");
            IsDimensionSizeVisible = !IsNumericSizeVisible;
        }
        partial void OnSearchTextChanged(string value) => ApplyFilters();
        partial void OnSelectedCategoryChanged(CategoryModel value) => ApplyFilters();
        partial void OnSelectedBrandChanged(BrandsModel value) => ApplyFilters();
        partial void OnFilterColorChanged(string value) => ApplyFilters();
        partial void OnFilterFullSizeChanged(string value) => ApplyFilters();

        // ===========================================================

        private void ApplyFilters()
        {
            if (_allStockItems == null) return;

            var filtered = _allStockItems.Where(i =>
                (string.IsNullOrEmpty(SearchText) || i.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || i.Sku.Contains(SearchText)) &&
                (SelectedCategory == null || i.CategoryName == SelectedCategory.CategoryName) &&
                (SelectedBrand == null || i.BrandName == SelectedBrand.BrandName) &&
                (string.IsNullOrEmpty(FilterColor) || i.Color == FilterColor) &&
                (string.IsNullOrEmpty(FilterFullSize) || i.FullSize == FilterFullSize)
            ).ToList();

            FilteredStock.Clear();
            FilteredStock.AddRange(filtered);
        }
        private void UpdateFilterOptions(List<string> distinctColors,List<string> distinctSizes)
        {
            AllColors.Clear();
            foreach (var c in distinctColors) AllColors.Add(c);

            
            FilterColors.Clear();
            foreach (var c in distinctColors) FilterColors.Add(c);

            
            FilterSizes.Clear();
            foreach (var s in distinctSizes) FilterSizes.Add(s);
        }
                
        [RelayCommand]   private void StockAddingFiledsClear()
        {
            ClearDraft();
        }
        private void ClearDraft()
        {
            IsEditMode = false; // 💡 CRITIQUE : Repasse l'UI en mode normal (Fond blanc, bouton bleu)

            DraftSKU = string.Empty;
            DraftCategory = null;
            DraftBrand = null;
            DraftProductName = string.Empty;
            DraftColor = string.Empty;
            DraftNumericSize = string.Empty;
            DraftWidth = string.Empty;
            DraftLength = string.Empty;
            DraftPurchasePrice = null;
            DraftFixedDiscount = null;
            DraftSalePrice = null;
            DraftQuantity = null;
            LabelsToPrint = 1;
            DraftNewBrandName = string.Empty;
            DraftSelectedProduct = null;
        }

        
    }

}

