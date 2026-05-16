
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using global::Shiakati.Models;
    using Shiakati.Services.Interfaces;
using Shiakati.Helpers;
    using Shiakati.Services.Implementations;
    using System;
    using System.Collections.ObjectModel;
using Shiakati.ViewModels;
    using System.Threading.Tasks;
    using System.Windows;
using Serilog;


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
            WidthsList = new ObservableCollection<string> { "Standard", "Large", "Slim" };
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
        [ObservableProperty] private int? _draftLength;
        [ObservableProperty] private int _labelsToPrint = 1;

        // ===========================================================
        // VI. COMMANDS
        // ===========================================================

        public async Task LoadInitialDataAsync()
        {
            if (IsLoading || _allStockItems.Any()) return;
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

                });
            }
        }

        [RelayCommand]
        private async Task ReceiveStockAsync()
        {
            // Logique d'enregistrement (POST ou PUT selon IsEditMode)
            
            _cacheService.Remove(CacheKeys.StockVariants);
            _allStockItems.Clear();

            await LoadInitialDataAsync();
            MessageBox.Show(IsEditMode ? "Stock modifié avec succès !" : "Stock enregistré avec succès !");
            IsReceptionVisible = false;
            ClearDraft();
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
            DraftWidth = null;
            DraftLength = null;
            DraftPurchasePrice = null;
            DraftFixedDiscount = null;
            DraftSalePrice = null;
            DraftQuantity = null;
            LabelsToPrint = 1;
        }

        //get sizes and save new and save edit and  PosView Product + ...
        //CartItem fix and useit
    }

}

