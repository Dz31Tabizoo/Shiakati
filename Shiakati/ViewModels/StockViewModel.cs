
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
    /*public partial class StockViewModel : ObservableObject
    {
        private readonly IBarCodePrintService _printerService;
        private readonly ICatalogService _db;
        private readonly IProductsService _productsService;
        private readonly IProductVariantsService _productVariantsService;

        public StockViewModel(IBarCodePrintService printerService, ICatalogService db,IProductVariantsService productVariantsService,IProductsService productsService)
        {
            _printerService = printerService;
            _db = db;
            _productsService = productsService;
            _productVariantsService = productVariantsService;

            Categories = new ObservableCollection<CategoryModel>();
            Brands = new ObservableCollection<BrandsModel>();
            Products = new ObservableCollection<ProductModel>();
            FilteredStock = new RangeObservableCollection<ProductVariantModel>();
        }

        // ==========================================
        // UI STATE
        // ==========================================
        [ObservableProperty] private bool _isReceptionVisible;
        [ObservableProperty] private bool _isNumericSizeVisible = false;
        [ObservableProperty] private bool _isDimensionSizeVisible = true;
        [ObservableProperty] private bool _isLoading; // Pour afficher un Spinner si besoin

        [RelayCommand]
        private void ToggleReception() => IsReceptionVisible = !IsReceptionVisible;

        // ==========================================
        // FILTER PROPERTIES
        // ==========================================
        [ObservableProperty] private string _searchText;
        [ObservableProperty] private CategoryModel _selectedCategory;
        [ObservableProperty] private BrandsModel _selectedBrand;
        [ObservableProperty] private string _filterColor;
        [ObservableProperty] private string _filterFullSize;
        [ObservableProperty] private ProductVariantModel _selectedStockItem;

        private List<BrandsModel> _allBrands = new();
        private List<ProductModel> _allProducts = new();
        private List<ProductVariantModel> _allStockItems = new();

        public ObservableCollection<string> FilterColors { get; } = new();
        public ObservableCollection<string> FilterSizes { get; } = new();
        public ObservableCollection<string> WidthsList { get; } = new() { "XS", "S", "M", "L", "XL", "XXL", "XXXL", "1", "2", "3", "4", "5" };
        public RangeObservableCollection<ProductVariantModel> FilteredStock { get; }
        public ObservableCollection<CategoryModel> Categories { get; } = new();
        public ObservableCollection<BrandsModel> Brands { get; } = new();
        public ObservableCollection<ProductModel> Products { get; } = new();


        // Triggers asynchrones (Générés par CommunityToolkit)
        async partial void OnSearchTextChanged(string value) => await SafeApplyFiltersAsync();
        async partial void OnSelectedCategoryChanged(CategoryModel value) => await SafeApplyFiltersAsync();
        async partial void OnSelectedBrandChanged(BrandsModel value) => await SafeApplyFiltersAsync();
        async partial void OnFilterColorChanged(string value) => await SafeApplyFiltersAsync();
        async partial void OnFilterFullSizeChanged(string value) => await SafeApplyFiltersAsync();

        private async Task SafeApplyFiltersAsync()
        {
            try 
            {
                // On travaille sur la liste complète en mémoire
                var query = _allStockItems.AsEnumerable();

                // 1. Recherche Textuelle
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    string search = SearchText.ToLower();
                    query = query.Where(x => x.Sku.ToLower().Contains(search) ||
                                             x.ProductName.ToLower().Contains(search));
                }

                // 2. Filtre Catégorie
                if (SelectedCategory != null)
                    query = query.Where(x => x.CategoryName == SelectedCategory.CategoryName);

                // 3. Filtre Marque
                if (SelectedBrand != null)
                    query = query.Where(x => x.BrandName== SelectedBrand.BrandName);

                // 4. Filtre Couleur
                if (!string.IsNullOrWhiteSpace(FilterColor))
                    query = query.Where(x => x.Color == FilterColor);

                // 5. Filtre Taille
                if (!string.IsNullOrWhiteSpace(FilterFullSize))
                    query = query.Where(x => x.FullSize == FilterFullSize);

                // Mise à jour de l'UI en un seul coup (Bulk)
                FilteredStock.Clear();
                FilteredStock.AddRange(query.ToList());

                await Task.CompletedTask;
            }
            catch (Exception ex) { Log.Error(ex, "Erreur filtres"); }
        }

        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            _searchText = string.Empty; // Utiliser le champ privé pour éviter de déclencher 5 appels API
            _selectedCategory = null;
            _selectedBrand = null;
            _filterColor = null;
            _filterFullSize = null;

            // On notifie les changements manuellement
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(SelectedCategory));
            OnPropertyChanged(nameof(SelectedBrand));
            OnPropertyChanged(nameof(FilterColor));
            OnPropertyChanged(nameof(FilterFullSize));

            await SafeApplyFiltersAsync();
        }

       

        public async Task LoadInitialDataAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                var catalog = await _db.GetInitialGatalogDataAsync();
                var products = await _productsService.GetProductsAsync();
                var items = await _productVariantsService.GetProductVariantsAsync();
                _allStockItems = items.ToList();

                FilteredStock.Clear();
                FilteredStock.AddRange(_allStockItems);

                UpdateFilterOptions();

                Categories.Clear();
                foreach (var cat in catalog.Categories) Categories.Add(cat);

                _allBrands = catalog.Brands.ToList();
                _allProducts=products.ToList();

                RefreshFilterdBrands();
                RefreshFilterdProducts();


            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erreur lors du chargement initial du catalogue");
                MessageBox.Show("Impossible de charger les données du catalogue. Vérifiez votre connexion au serveur.");
            }
            finally
            {
                IsLoading = false;
            }
        }

        // ==========================================
        // RECEPTION FORM
        // ==========================================
        [ObservableProperty] private CategoryModel _draftCategory;
        [ObservableProperty] private BrandsModel _draftBrand = new BrandsModel();
        [ObservableProperty] private string _draftProductName;
        [ObservableProperty] private string _draftSKU;
        [ObservableProperty] private string _draftColor;
        [ObservableProperty] private string _draftNumericSize;
        [ObservableProperty] private string _draftWidth;
        [ObservableProperty] private int? _draftLength;
        [ObservableProperty] private decimal? _draftPurchasePrice;
        [ObservableProperty] private decimal? _draftSalePrice = null;
        [ObservableProperty] private decimal? _draftFixedDiscount;
        [ObservableProperty] private int? _draftQuantity = null;
        [ObservableProperty] private bool _printLabelsOnSave = true;
        [ObservableProperty] private int? _labelsToPrint = null;

        partial void OnDraftQuantityChanged(int? value) => LabelsToPrint = value;
        partial void OnDraftCategoryChanged(CategoryModel value)
        {
            if (value == null) return;

            UpdateVisibilityLogic(value);

            RefreshFilterdBrands();
            DraftBrand = null;

            //logic to get the right size textBox 

            //if (value == null) return;
            //string catName = value.CategoryName.ToLower();

            //// Logique de visibilité simplifiée
            //bool isSpecial = catName.Contains("cosmetic") || catName.Contains("shoe") ||
            //                 catName.Contains("chaussure") || catName.Contains("chaise");

            //IsNumericSizeVisible = isSpecial;
            //IsDimensionSizeVisible = !isSpecial;

            //if (isSpecial) { DraftWidth = null; DraftLength = null; }
            //else { DraftNumericSize = null; }
        }
        partial void OnDraftBrandChanged(BrandsModel value)
        {
            RefreshFilterdProducts();
            DraftProductName = null;
        }

        [RelayCommand]
        private async Task ReceiveStockAsync()
        {
            if (string.IsNullOrWhiteSpace(DraftProductName) || (DraftSalePrice ?? 0) <= 0)
            {
                MessageBox.Show("Veuillez remplir le nom du produit et le prix de vente.");
                return;
            }

            try
            {
                // Simulation API
                await Task.Delay(500);
                bool success = true;

                if (success)
                {
                    if (PrintLabelsOnSave && LabelsToPrint > 0)
                    {
                        PrintLabels();
                    }

                    MessageBox.Show("Réception enregistrée avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                    ResetDraftForm();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de la réception : {ex.Message}");
            }
        }

        private void UpdateVisibilityLogic(CategoryModel value)
        {
            string catName = value.CategoryName;

            bool isSpecial = catName.Contains("cosmetic") || catName.Contains("shoe") ||
                     catName.Contains("chaussure") || catName.Contains("chaise");

            IsNumericSizeVisible = isSpecial;
            IsDimensionSizeVisible = !isSpecial;

            if (isSpecial)
            {
                DraftWidth = null;
                DraftLength = null;
            }
            else
            {
                DraftNumericSize = null;
            }
        }

        private void PrintLabels()
        {
            string printerName = Properties.Settings.Default.BarcodePrinterName;
            if (string.IsNullOrWhiteSpace(printerName)) return;

            var label = new BarecodeLabelData
            {
                BrandName = ToAscii(DraftBrand?.BrandName ?? "N/A"),
                VariantName = ToAscii($"{DraftProductName} {DraftColor}").Trim(),
                Barcode = DraftSKU ?? "1234567890123",
                ProductSize = IsNumericSizeVisible ? DraftNumericSize : $"{DraftWidth} / {DraftLength}",
                Price = DraftSalePrice ?? 0
            };

            _printerService.PrintBarCode(label, printerName, LabelsToPrint ?? 1);
        }

        private static string ToAscii(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input.Replace('é', 'e').Replace('è', 'e').Replace('ê', 'e')
                        .Replace('à', 'a').Replace('â', 'a').Replace('ô', 'o')
                        .Replace('ù', 'u').Replace('û', 'u').Replace('î', 'i')
                        .Replace('ç', 'c');
        }

        private void ResetDraftForm()
        {
            DraftProductName = string.Empty;
            DraftQuantity = 1;
            DraftSKU = string.Empty;
            DraftPurchasePrice = null;
            DraftSalePrice = null;
        }

        private void RefreshFilterdBrands()
        {
            Brands.Clear();

            var filterd = (DraftCategory == null)
                ? _allBrands 
                : _allBrands.Where(b => b.CategoryID == DraftCategory.CategoryID );
            foreach (var b in filterd)
            {
                Brands.Add(b);
            }
        }

        private void RefreshFilterdProducts()
        {
            Products.Clear();
            var query = _allProducts.AsEnumerable();
            if (DraftCategory != null) query = query.Where(p => p.CategoryName == DraftCategory.CategoryName);

            if (DraftBrand != null && !string.IsNullOrEmpty(DraftBrand.BrandName))
                query = query.Where(p => p.BrandName == DraftBrand.BrandName);

            foreach (var p in query)
            {
                Products.Add(p);
            }
        }

        private void UpdateFilterOptions()
        {
            var colors = _allStockItems
                .Select(x => x.Color)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .OrderBy(s => s);

            FilterColors.Clear();
            foreach(var item in colors) FilterColors.Add(item);

            var sizes = _allStockItems
            .Select(x => x.FullSize)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .OrderBy(s => s);

            FilterSizes.Clear();
            foreach (var s in sizes) FilterSizes.Add(s);

        }


        [RelayCommand]
        private async Task PrintFromGridAsync(ProductVariantModel item)
        {
            if (item == null) return;

            // 1. Demander la quantité à l'utilisateur
            // Note : Tu peux créer une petite Window de dialogue ou utiliser un InputDialog
            string input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Combien d'étiquettes pour {item.ProductName} ?\n(Stock actuel : {item.StockQuantity})",
                "Impression Code-barres",
                item.StockQuantity.ToString());

            if (int.TryParse(input, out int quantity) && quantity > 0)
            {
                // 2. Vérifier que la quantité ne dépasse pas le stock (optionnel selon ton besoin)
                if (quantity > item.StockQuantity)
                {
                    MessageBox.Show("Attention : La quantité demandée dépasse le stock disponible.");
                }

                // 3. Préparer les données pour le service d'impression
                var label = new BarecodeLabelData
                {
                    BrandName = ToAscii(item.BrandName ?? "N/A"),
                    VariantName = ToAscii($"{item.ProductName} {item.Color}").Trim(),
                    Barcode = item.Sku ?? "000000000000",
                    ProductSize = item.FullSize,
                    Price = item.SalePrice ?? 0,
                };

                // 4. Lancer l'impression
                string printerName = Properties.Settings.Default.BarcodePrinterName;
                if (!string.IsNullOrEmpty(printerName))
                {
                    _printerService.PrintBarCode(label, printerName, quantity);
                }
                else
                {
                    MessageBox.Show("Veuillez configurer l'imprimante dans les paramètres.");
                }
            }
        }
    }*/

  

    
        public partial class StockViewModel : ObservableObject
        {
            // ===========================================================
            // I. SERVICES
            // ===========================================================
            private readonly IBarCodePrintService _printerService;
            private readonly ICatalogService _catalogDb;
            private readonly IProductsService _productsService;
            private readonly IProductVariantsService _stockService;

            public StockViewModel(IBarCodePrintService printerService, ICatalogService db,
                                  IProductVariantsService stockService, IProductsService productsService)
            {
                _printerService = printerService;
                _catalogDb = db;
                _productsService = productsService;
                _stockService = stockService;

                // Initialisation des collections
                Categories = new ObservableCollection<CategoryModel>();
                Brands = new ObservableCollection<BrandsModel>();
                Products = new ObservableCollection<ProductModel>();
                FilteredStock = new RangeObservableCollection<ProductVariantModel>();
                FilterColors = new ObservableCollection<string>();
                FilterSizes = new ObservableCollection<string>();
                AllColors = new ObservableCollection<string> { "Noir", "Blanc", "Rouge", "Bleu", "Gris", "Vert" };
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
                if (IsLoading) return;
                try
                {
                    IsLoading = true;
                    var catalog = await _catalogDb.GetInitialGatalogDataAsync();
                    var items = await _stockService.GetProductVariantsAsync();
                    var prods = await _productsService.GetProductsAsync();

                    _allStockItems = items.ToList();

                FilteredStock.Clear();
                    FilteredStock.AddRange(_allStockItems);

                    Categories.Clear();
                    foreach (var c in catalog.Categories) Categories.Add(c);

                    Brands.Clear();
                    foreach (var b in catalog.Brands) Brands.Add(b);

                    Products.Clear();
                    foreach (var p in prods) Products.Add(p);

                    UpdateFilterOptions();
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
                MessageBox.Show("Stock enregistré avec succès !");
                await LoadInitialDataAsync();
                IsReceptionVisible = false;
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

            private void UpdateFilterOptions()
            {
                FilterColors.Clear();
                foreach (var col in _allStockItems.Select(i => i.Color).Distinct()) FilterColors.Add(col);

                FilterSizes.Clear();
                foreach (var sz in _allStockItems.Select(i => i.FullSize).Distinct()) FilterSizes.Add(sz);
            }

            private void ClearDraft()
            {
                DraftSKU = string.Empty;
                DraftProductName = string.Empty;
                DraftPurchasePrice = null;
                DraftSalePrice = null;
                DraftQuantity = null;
            }
        }
    
}

