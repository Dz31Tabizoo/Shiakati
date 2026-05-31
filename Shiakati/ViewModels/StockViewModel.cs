//using CommunityToolkit.Mvvm.ComponentModel;
//using CommunityToolkit.Mvvm.Input;
//using CommunityToolkit.Mvvm.Messaging;
//using global::Shiakati.Models;
//using Serilog;
//using Shiakati.Helpers;
//using Shiakati.Messages;
//using Shiakati.Services.Implementations;
//using Shiakati.Services.Interfaces;
//using Shiakati.ViewModels;
//using Shiakati.Views;
//using System;
//using System.Collections.ObjectModel;
//using System.ComponentModel;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Data;
//using static MaterialDesignThemes.Wpf.Theme.ToolBar;


//namespace Shiakati.ViewModels
//{   
//    public partial class StockViewModel : ObservableObject
//    {
//        // ===========================================================
//        // I.                     SERVICES
//        // ===========================================================
//        private readonly IBarCodePrintService _printerService;
//        private readonly ICatalogService _catalogDb;
//        private readonly IProductsService _productsService;
//        private readonly IProductVariantsService _stockService;
//        private readonly ICacheService _cacheService;

//        public StockViewModel(IBarCodePrintService printerService, ICatalogService db,
//                              IProductVariantsService stockService, ICacheService cacheService, IProductsService productsService)
//        {
//            _printerService = printerService;
//            _catalogDb = db;
//            _productsService = productsService;
//            _stockService = stockService;
//            _cacheService = cacheService;

//            // Initialisation des collections
//            Categories = new ObservableCollection<CategoryModel>();
//            Brands = new ObservableCollection<BrandsModel>();
//            FilteredBrands = CollectionViewSource.GetDefaultView(Brands);
//            Products = new ObservableCollection<ProductModel>();
//            FilteredStock = new RangeObservableCollection<ProductVariantModel>();
//            FilterColors = new ObservableCollection<string>();
//            FilterSizes = new ObservableCollection<string>();
//            AllColors = new ObservableCollection<string> ();
//            WidthsList = new ObservableCollection<string> { "XS", "S", "M","L","XL","XXL","XXXL","1","2","3","4" };
//        }

//        // ===========================================================
//        // II.                   COLLECTIONS
//        // ===========================================================
//        public ObservableCollection<CategoryModel> Categories { get; }
//        public ObservableCollection<BrandsModel> Brands { get; }
//        public ICollectionView FilteredBrands { get; private set; }
//        public ObservableCollection<ProductModel> Products { get; }
//        public RangeObservableCollection<ProductVariantModel> FilteredStock { get; }
//        public ObservableCollection<string> FilterColors { get; }
//        public ObservableCollection<string> FilterSizes { get; }
//        public ObservableCollection<string> AllColors { get; }
//        public ObservableCollection<string> WidthsList { get; }

//        private List<ProductVariantModel> _allStockItems = new();

//        // ===========================================================
//        // III.                 UI STATES
//        // ===========================================================
//        [ObservableProperty] private bool _isLoading;
//        [ObservableProperty] private bool _isReceptionVisible;
//        [ObservableProperty] private bool _isEditMode;
//        [ObservableProperty] private bool _isNumericSizeVisible;
//        [ObservableProperty] private bool _isDimensionSizeVisible = true;
//        [ObservableProperty] private bool _printLabelsOnSave = true;
//        [ObservableProperty] private bool _isNonActiveItemsVisible = false;
//        [ObservableProperty] private bool _isManualSkuEnabled;
//        [ObservableProperty] private bool _isNotEditMode = true;

//        // ===========================================================
//        // IV. FILTERS & SELECTION
//        // ===========================================================
//        [ObservableProperty] private string _searchText;
//        [ObservableProperty] private CategoryModel _selectedCategory;
//        [ObservableProperty] private BrandsModel _selectedBrand;
//        [ObservableProperty] private string _filterColor;
//        [ObservableProperty] private string _filterFullSize;
//        [ObservableProperty] private ProductVariantModel _selectedStockItem;

//        // ===========================================================
//        // V. FORM FIELDS (DRAFT)
//        // ===========================================================
//        [ObservableProperty] private CategoryModel _draftCategory;
//        [ObservableProperty] private BrandsModel _draftBrand;
//        [ObservableProperty] private string _draftProductName;
//        [ObservableProperty] private string _draftSKU;
//        [ObservableProperty] private decimal? _draftPurchasePrice;
//        [ObservableProperty] private decimal? _draftSalePrice;
//        [ObservableProperty] private decimal? _draftFixedDiscount;
//        [ObservableProperty] private int? _draftQuantity;
//        [ObservableProperty] private string _draftColor;
//        [ObservableProperty] private string _draftNumericSize;
//        [ObservableProperty] private string _draftWidth;
//        [ObservableProperty] private string _draftLength;
//        [ObservableProperty] private int _labelsToPrint = 1;
//        [ObservableProperty] private string _draftNewBrandName = string.Empty;
//        [ObservableProperty] private ProductModel? _draftSelectedProduct;


//        // ===========================================================
//        // VI. COMMANDS
//        // ===========================================================

//        public async Task LoadInitialDataAsync(bool forceRefresh = false)
//        {
//            if (!forceRefresh && (IsLoading || _allStockItems.Any())) 
//                return;
//            try
//            {
//                IsLoading = true;
//                var catalog = await _cacheService.GetOrLoadAsync<(List<BrandsModel> Brands, List<CategoryModel> Categories)>(
//                                    CacheKeys.Catalog,
//                                    () => _catalogDb.GetInitialGatalogDataAsync());

//                var prods = await _cacheService.GetOrLoadAsync(CacheKeys.Products, _productsService.GetProductsAsync);
//                var items = await _cacheService.GetOrLoadAsync(CacheKeys.StockVariants, _stockService.GetProductVariantsAsync);

//                _allStockItems = items.ToList();

//                var distinctColors = _allStockItems.Select(i => i.Color)
//                                        .Where(c => !string.IsNullOrWhiteSpace(c))
//                                        .Distinct()
//                                        .OrderBy(c => c)
//                                        .ToList();
//                var distinctSizes = _allStockItems.Select(i => i.FullSize)
//                                                    .Where(s => !string.IsNullOrWhiteSpace(s))
//                                                    .Distinct()
//                                                    .OrderBy(s => s)
//                                                    .ToList();

//                System.Windows.Application.Current.Dispatcher.Invoke(() =>
//                {

//                    ApplyFilters();

//                    Categories.Clear();
//                    foreach (var c in catalog.Categories) Categories.Add(c);

//                    Brands.Clear();
//                    foreach (var b in catalog.Brands) Brands.Add(b);

//                    Products.Clear();
//                    foreach (var p in prods) Products.Add(p);

//                    UpdateFilterOptions(distinctColors,distinctSizes);
//                });
//            }
//            catch
//            {
//                MessageBox.Show("Probleme de connexion", "Data Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally { IsLoading = false; }
//        }
//        [RelayCommand] private void ToggleReception() { IsReceptionVisible = !IsReceptionVisible; IsEditMode = false; ClearDraft(); }
//        [RelayCommand] private void ClearFilters()
//        {
//            SearchText = string.Empty;
//            SelectedCategory = null;
//            SelectedBrand = null;
//            FilterColor = null;
//            FilterFullSize = null;
//            IsManualSkuEnabled = false;
//            DraftSKU = string.Empty;
//        }
//        [RelayCommand] private void PrepareEdit(ProductVariantModel item)
//        {
//            if (item == null) return;
//            IsEditMode = true;
//            IsReceptionVisible = true;

//            // Hierarchy

//            DraftCategory = Categories.FirstOrDefault(c => c.CategoryName == item.CategoryName);
//            DraftBrand = Brands.FirstOrDefault(b => b.BrandName == item.BrandName);
//            DraftNewBrandName = DraftNewBrandName;

//            // Product
//            DraftProductName = item.ProductName;
//            DraftSelectedProduct = Products.FirstOrDefault(p => p.ProductName == item.ProductName && p.BrandName == item.BrandName);

//            // variant details
//            IsManualSkuEnabled = false;   // never editable in edit mode
//            DraftSKU = item.Sku;         // show the existing SKU (read‑only)
//            DraftPurchasePrice = item.PurchasePrice;
//            DraftSalePrice = item.SalePrice;
//            DraftFixedDiscount = item.DiscountFixed;
//            DraftQuantity = item.StockQuantity;
//            DraftColor = item.Color;

//            if (IsDimensionSizeVisible) // e.g., trousers, thobes
//            {
//                DraftWidth = item.Width;
//                DraftLength = item.Length?.ToString();
//                DraftNumericSize = string.Empty;
//            }
//            else if (IsNumericSizeVisible) // shoes, etc.
//            {
//                DraftNumericSize = item.Length?.ToString() ?? string.Empty;
//                DraftWidth = string.Empty;
//                DraftLength = string.Empty;
//            }
//            else
//            {
//                DraftNumericSize = string.Empty;
//                DraftWidth = string.Empty;
//                DraftLength = string.Empty;
//            }

//        }
//        [RelayCommand]
//        private async Task PrintFromGridAsync(ProductVariantModel item)
//        {
//            try
//            {
//                if (item == null || string.IsNullOrWhiteSpace(item.Sku))
//                {
//                    MessageBox.Show("Code‑barres manquant pour cet article.", "Impossible d'imprimer", MessageBoxButton.OK, MessageBoxImage.Warning);
//                    return;
//                }

//                string printerToUse = Properties.Settings.Default.BarcodePrinterName;
//                if (string.IsNullOrWhiteSpace(printerToUse))
//                {
//                    MessageBox.Show("Aucune imprimante code‑barres sélectionnée dans les paramètres.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
//                    return;
//                }

//                // Safely find a valid owner window
//                Window? owner = Application.Current.MainWindow;
//                if (owner == null || owner is PrintQuantityDialog) // guard against self-reference
//                {
//                    owner = Application.Current.Windows
//                               .OfType<Window>()
//                               .FirstOrDefault(w => w.IsActive && !(w is PrintQuantityDialog))
//                            ?? Application.Current.Windows
//                                   .OfType<Window>()
//                                   .FirstOrDefault(w => !(w is PrintQuantityDialog));
//                }

//                var dialog = new PrintQuantityDialog { Quantity = item.StockQuantity ?? 1 };
//                if (owner != null) dialog.Owner = owner;

//                if (dialog.ShowDialog() == true)
//                {
//                    _printerService.PrintBarCode(new BarecodeLabelData
//                    {
//                        VariantName = item.ProductName,
//                        Barcode = item.Sku,
//                        BrandName = item.BrandName,
//                        Price = item.SalePrice.GetValueOrDefault(),
//                        ProductSize = item.FullSize
//                    }, printerToUse, dialog.Quantity);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Erreur lors de l'impression du code-barres : {ex.Message}", "Erreur d'impression", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        [RelayCommand] private async Task ReceiveStockAsync()
//        {
//            if (!IsFormValid()) return;
//            int numericSize = 0;
//            bool hasNumericSize = int.TryParse(DraftNumericSize, out numericSize);

//            if (IsEditMode && SelectedStockItem != null)
//            {
//                // ─────── UPDATE ───────
//                var updateRequest = new UpdateVariantRequest
//                {
//                    VariantId = SelectedStockItem.VariantId,
//                    // Category (optional, only if changed)
//                    CategoryId = DraftCategory?.CategoryID,
//                    // Brand
//                    BrandId = (DraftBrand != null && DraftBrand.BrandID > 0) ? DraftBrand.BrandID : null,
//                    BrandName = (DraftBrand == null || DraftBrand.BrandID <= 0) && !string.IsNullOrWhiteSpace(DraftNewBrandName)
//                                  ? DraftNewBrandName : null,
//                    // Product
//                    ProductId = DraftSelectedProduct?.ProductID,
//                    ProductName = DraftSelectedProduct?.ProductName ?? DraftProductName,
//                    // Variant fields

//                    Color = DraftColor,
//                    PurchasePrice = DraftPurchasePrice,
//                    SalePrice = DraftSalePrice,
//                    DiscountFixed = DraftFixedDiscount,
//                    StockQuantity = DraftQuantity
//                };

//                if (IsNumericSizeVisible)
//                {
//                    updateRequest.Length = hasNumericSize ? numericSize : null;
//                    updateRequest.Width = null;
//                }
//                else if (IsDimensionSizeVisible)
//                {
//                    updateRequest.Length = int.TryParse(DraftLength?.ToString(), out int l) ? l : null;
//                    updateRequest.Width = string.IsNullOrWhiteSpace(DraftWidth) ? null : DraftWidth;
//                }

//                var UpdatedVariant = await _stockService.UpdateProductVariantAsync(updateRequest);

//                if (UpdatedVariant != null)
//                {
//                    _cacheService.Remove(CacheKeys.StockVariants);
//                    if (!string.IsNullOrWhiteSpace(UpdatedVariant.BrandName))
//                        _cacheService.Remove(CacheKeys.Catalog);

//                    await ForceReloadAllDataAsync();
//                    MessageBox.Show("Article modifié avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
//                    await PrintBarCodeOnReciveStock(UpdatedVariant, LabelsToPrint);
//                    IsReceptionVisible = false;
//                    IsEditMode = false;
//                    ClearDraft();
//                    WeakReferenceMessenger.Default.Send(new StockUpdatedMessage());

//                }
//                else
//                {
//                    MessageBox.Show("Erreur lors de la modification.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//            else
//            {
//                // ─────── ADD (existing code) ───────
//                var request = new AddVariantRequest
//                {
//                    CategoryId = DraftCategory.CategoryID,
//                    BrandId = DraftBrand?.BrandID > 0 ? DraftBrand.BrandID : null,
//                    BrandName = (DraftBrand?.BrandID == 0 || DraftBrand == null) ? DraftNewBrandName : null,
//                    ProductName = DraftSelectedProduct?.ProductName ?? DraftProductName,
//                    Sku = string.IsNullOrWhiteSpace(DraftSKU) ? null : DraftSKU,
//                    Color = string.IsNullOrWhiteSpace(DraftColor) ? null : DraftColor,
//                    PurchasePrice = DraftPurchasePrice,
//                    SalePrice = DraftSalePrice,
//                    DiscountFixed = DraftFixedDiscount,
//                    StockQuantity = DraftQuantity.GetValueOrDefault()
//                };

//                if (IsNumericSizeVisible)
//                {
//                    request.Length = hasNumericSize ? numericSize : null;
//                    request.Width = null;
//                }
//                else if (IsDimensionSizeVisible)
//                {
//                    request.Length = int.TryParse(DraftLength?.ToString(), out int length) ? length : null;
//                    request.Width = string.IsNullOrWhiteSpace(DraftWidth) ? null : DraftWidth;
//                }

//                var newVariant = await _stockService.AddProductVariantAsync(request);

//                if (newVariant != null)
//                {
//                    _cacheService.Remove(CacheKeys.StockVariants);
//                    if (request.BrandId == null && !string.IsNullOrWhiteSpace(request.BrandName))
//                        _cacheService.Remove(CacheKeys.Catalog);
//                    await Task.Delay(300);
//                    IsLoading = false;
//                    await LoadInitialDataAsync(forceRefresh: true);
//                    MessageBox.Show("Stock enregistré avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
//                    await PrintBarCodeOnReciveStock(newVariant, LabelsToPrint);
//                    IsReceptionVisible = false;
//                    IsEditMode = false;
//                    ClearDraft();
//                    WeakReferenceMessenger.Default.Send(new StockUpdatedMessage());
//                }
//                else
//                {
//                    IsLoading = false;
//                    MessageBox.Show("Erreur lors de l'enregistrement.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }        
//        [RelayCommand] private async Task DeleteStockItemAsync(ProductVariantModel item)
//        {
//            if (item == null) return;

//            var result = MessageBox.Show(
//                $"Êtes-vous sûr de vouloir supprimer définitivement l'article :\n\"{item.ProductName}\" (SKU: {item.Sku}) ?",
//                "Confirmation de suppression",
//                MessageBoxButton.YesNo,
//                MessageBoxImage.Warning);

//            if (result == MessageBoxResult.No) return;

//            try
//            {
//                IsLoading = true;

//                var updateRequest = new UpdateVariantRequest
//                {
//                    VariantId = item.VariantId,
//                    IsActive = false
//                };

//                var UpdatedVariant = await _stockService.UpdateProductVariantAsync(updateRequest);

//                if (UpdatedVariant != null && !string.IsNullOrWhiteSpace(UpdatedVariant.BrandName))
//                {
//                    if (updateRequest.BrandId == null && !string.IsNullOrWhiteSpace(updateRequest.BrandName))
//                        _cacheService.Remove(CacheKeys.Catalog);
//                    // On utilise la méthode centralisée qui s'occupe de vider le cache et recharger
//                    await ForceReloadAllDataAsync();

//                    WeakReferenceMessenger.Default.Send(new StockUpdatedMessage());

//                    MessageBox.Show("Article supprimé du stock avec succès !", "Suppression réussie", MessageBoxButton.OK, MessageBoxImage.Information);
//                }
//                else
//                {
//                    MessageBox.Show("Erreur lors de la suppression de l'article en base de données.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Une erreur critique est survenue: {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//            finally
//            {
//                IsLoading = false;
//            }
//        }
//        private async Task PrintBarCodeOnReciveStock(ProductVariantResponse newVariant, int Copies)
//        {
//            try
//            {
//                if (PrintLabelsOnSave)
//                {
//                    var label = new BarecodeLabelData
//                    {
//                        Barcode = newVariant.Sku,
//                        BrandName = newVariant.BrandName,
//                        VariantName = newVariant.ProductName,
//                        ProductSize = newVariant.FullSize,
//                        Price = newVariant.SalePrice ?? 0,
//                    };

//                    _printerService.PrintBarCode(label, Properties.Settings.Default.BarcodePrinterName, Copies);
//                }
//                else
//                {
//                    return;
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Erreur lors de l'impression du code-barres: {ex.Message}", "Erreur d'impression", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }
//        [RelayCommand] private void StockAddingFiledsClear()
//        {
//            ClearDraft();
//        }

//        // ===========================================================
//        // VII. HELPERS & TRIGGERS
//        // ===========================================================

//        private CancellationTokenSource? _searchDebounceToken;

//        partial void OnSearchTextChanged(string value)
//        {
//            _searchDebounceToken?.Cancel();
//            _searchDebounceToken = new CancellationTokenSource();

//            var token = _searchDebounceToken.Token;
//            Task.Delay(500, token).ContinueWith(_ =>
//            {
//                if (!token.IsCancellationRequested)
//                {
//                    Application.Current.Dispatcher.Invoke(() => ApplyFilters());
//                }
//            }, token);
//        }
//        partial void OnSelectedCategoryChanged(CategoryModel value) => ApplyFilters();
//        partial void OnSelectedBrandChanged(BrandsModel value) => ApplyFilters();
//        partial void OnFilterColorChanged(string value) => ApplyFilters();
//        partial void OnFilterFullSizeChanged(string value) => ApplyFilters();
//        partial void OnDraftCategoryChanged(CategoryModel value)
//        {
//            if (value == null) return;

//            // 1. Logique de visibilité des tailles
//            IsDimensionSizeVisible = value.CategoryName.Contains("Thob") ||
//                                     value.CategoryName.Contains("Pantalon") ||
//                                     value.CategoryName.Contains("Sous");
//            IsNumericSizeVisible = !IsDimensionSizeVisible;

//            // 2. Filtrage des marques
//            FilteredBrands.Filter = (obj) =>
//            {
//                if (obj is BrandsModel brand)
//                {
//                    // Affiche la marque si elle appartient à la catégorie, 
//                    // ou si elle n'est liée à aucune catégorie (si c'est ton besoin)
//                    return brand.CategoryID == value.CategoryID;
//                }
//                return false;
//            };

//            // On notifie la vue que le filtre a changé
//            FilteredBrands.Refresh();

//        }
//        partial void OnIsNonActiveItemsVisibleChanged(bool value)
//        {
//            ApplyFilters();
//        }
//        partial void OnIsEditModeChanged(bool value) => IsNotEditMode = !value;
//        private async Task ForceReloadAllDataAsync()
//        {
//            // 1. On vide le cache pour forcer l'application à demander des données fraîches
//            _cacheService.Remove(CacheKeys.StockVariants);
//            _cacheService.Remove(CacheKeys.Catalog);
//            _cacheService.Remove(CacheKeys.Products);

//            // 2. Un petit délai de sécurité (300ms) pour laisser le temps à l'API/Base de données
//            // de bien terminer l'écriture de la modification/suppression
//            await Task.Delay(300);

//            // 3. On appelle ta méthode existante de chargement en activant le rafraîchissement forcé
//            await LoadInitialDataAsync(forceRefresh: true);
//        }

//        // ===========================================================

//        private void ApplyFilters()
//        {
//            if (_allStockItems == null) return;

//            var filtered = _allStockItems.Where(i =>
//                // 1. Filtre d'activité (Soft Delete)
//                (IsNonActiveItemsVisible || i.IsActive == true ) &&

//                // 2. Filtres textuels (Recherche par Nom ou SKU)
//                (string.IsNullOrEmpty(SearchText) || i.ProductName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || i.Sku.Contains(SearchText)) &&

//                // 3. Filtres par ComboBox
//                (SelectedCategory == null || i.CategoryName == SelectedCategory.CategoryName) &&
//                (SelectedBrand == null || i.BrandName == SelectedBrand.BrandName) &&
//                (string.IsNullOrEmpty(FilterColor) || i.Color == FilterColor) &&
//                (string.IsNullOrEmpty(FilterFullSize) || i.FullSize == FilterFullSize)
//            ).ToList();

//            // Mise à jour synchrone de la collection liée au DataGrid
//            FilteredStock.Clear();
//            FilteredStock.AddRange(filtered);
//        }
//        private void UpdateFilterOptions(List<string> distinctColors,List<string> distinctSizes)
//        {
//            AllColors.Clear();
//            foreach (var c in distinctColors) AllColors.Add(c);


//            FilterColors.Clear();
//            foreach (var c in distinctColors) FilterColors.Add(c);


//            FilterSizes.Clear();
//            foreach (var s in distinctSizes) FilterSizes.Add(s);
//        }                               
//        private void ClearDraft()
//        {
//            IsEditMode = false; // 💡 CRITIQUE : Repasse l'UI en mode normal (Fond blanc, bouton bleu)

//            DraftSKU = string.Empty;
//            DraftCategory = null;
//            DraftBrand = null;
//            DraftProductName = string.Empty;
//            DraftColor = string.Empty;
//            DraftNumericSize = string.Empty;
//            DraftWidth = null;
//            DraftLength = string.Empty;
//            DraftPurchasePrice = null;
//            DraftFixedDiscount = null;
//            DraftSalePrice = null;
//            DraftQuantity = null;
//            LabelsToPrint = 1;
//            DraftNewBrandName = string.Empty;
//            DraftSelectedProduct = null;
//        }
//        private bool IsFormValid()
//        {
//            // 1. Validation de la Catégorie (Selection obligatoire)
//            if (DraftCategory == null)
//            {
//                MessageBox.Show("Veuillez sélectionner une catégorie.",
//                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return false;
//            }

//            // 2. Validation de la Marque (Soit sélectionnée, soit saisie textuelle)
//            bool hasSelectedBrand = DraftBrand != null && DraftBrand.BrandID > 0;
//            bool hasTypedBrand = !string.IsNullOrWhiteSpace(DraftNewBrandName);

//            if (!hasSelectedBrand && !hasTypedBrand)
//            {
//                MessageBox.Show("Veuillez sélectionner une marque existante ou saisir un nouveau nom de marque.",
//                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return false;
//            }

//            // 3. Validation du Produit (Soit sélectionné, soit saisie textuelle)
//            bool hasSelectedProduct = DraftSelectedProduct != null;
//            bool hasTypedProduct = !string.IsNullOrWhiteSpace(DraftProductName);

//            if (!hasSelectedProduct && !hasTypedProduct)
//            {
//                MessageBox.Show("Veuillez sélectionner un produit existant ou saisir un nouveau nom de produit.",
//                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return false;
//            }

//            // 4. Validation des Prix (Doivent être renseignés et supérieurs à 0)
//            if (DraftPurchasePrice == null || DraftPurchasePrice <= 0)
//            {
//                MessageBox.Show("Veuillez saisir un prix d'achat valide supérieur à 0 DA.",
//                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return false;
//            }

//            if (DraftSalePrice == null || DraftSalePrice <= 0)
//            {
//                MessageBox.Show("Veuillez saisir un prix de vente valide supérieur à 0 DA.",
//                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return false;
//            }

//            if (DraftSalePrice < DraftPurchasePrice)
//            {
//                var result = MessageBox.Show("Le prix de vente est inférieur au prix d'achat. Voulez-vous continuer ?",
//                                             "Attention", MessageBoxButton.YesNo, MessageBoxImage.Question);
//                if (result == MessageBoxResult.No) return false;
//            }

//            // 5. Validation des Tailles Dynamiques (Optionnel mais sécurise ton XAML)
//            if (IsNumericSizeVisible && string.IsNullOrWhiteSpace(DraftNumericSize))
//            {
//                MessageBox.Show("Veuillez renseigner la taille numérique de l'article.",
//                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return false;
//            }
//            else if (IsDimensionSizeVisible && ((string.IsNullOrWhiteSpace(DraftWidth) && string.IsNullOrWhiteSpace(DraftLength?.ToString()))))
//            {
//                MessageBox.Show("Veuillez renseigner la largeur et la longueur de l'article.",
//                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return false;
//            }

//            // 6. Validation de la Quantité reçue (Doit être supérieure à 0)
//            if (DraftQuantity == null || DraftQuantity <= 0)
//            {
//                MessageBox.Show("Veuillez saisir une quantité reçue valide (minimum 1 unité).",
//                                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
//                return false;
//            }
//            if (LabelsToPrint > 50)
//            {
//                var result = MessageBox.Show("La quantité reçue est très élevée. Êtes-vous sûr de vouloir Imprimer autant d'unités ?",
//                                             "Attention", MessageBoxButton.YesNo, MessageBoxImage.Question);
//                if (result == MessageBoxResult.No) return false;
//            }


//            // Tout est correct
//            return true;
//        }
//    }

//}



using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Shiakati.Messages;
using Shiakati.Models;
using Shiakati.Properties;
using Shiakati.Services.Interfaces;
using Shiakati.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

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
            FilteredBrands = CollectionViewSource.GetDefaultView(Brands);
            Products = new ObservableCollection<ProductModel>();
            // FilteredStock n'est plus une ObservableCollection mais un ICollectionView
            FilterColors = new ObservableCollection<string>();
            FilterSizes = new ObservableCollection<string>();
            AllColors = new ObservableCollection<string>();
            WidthsList = new ObservableCollection<string> { "XS", "S", "M", "L", "XL", "XXL", "XXXL", "1", "2", "3", "4" };
        }

        // ===========================================================
        // II. COLLECTIONS
        // ===========================================================
        public ObservableCollection<CategoryModel> Categories { get; }
        public ObservableCollection<BrandsModel> Brands { get; }
        public ICollectionView FilteredBrands { get; private set; }
        public ObservableCollection<ProductModel> Products { get; }
        public ICollectionView FilteredStockView { get; private set; }  // ← nouveau
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
        [ObservableProperty] private bool _isNonActiveItemsVisible = false;
        [ObservableProperty] private bool _isManualSkuEnabled;
        [ObservableProperty] private bool _isNotEditMode = true;

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
            if (!forceRefresh && (IsLoading || _allStockItems.Any()))
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

                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Créer ou réaffecter la vue filtrée
                    FilteredStockView = CollectionViewSource.GetDefaultView(_allStockItems);
                    FilteredStockView.Filter = StockFilter;
                    OnPropertyChanged(nameof(FilteredStockView)); // Notification pour le binding

                    Categories.Clear();
                    foreach (var c in catalog.Categories) Categories.Add(c);

                    Brands.Clear();
                    foreach (var b in catalog.Brands) Brands.Add(b);

                    Products.Clear();
                    foreach (var p in prods) Products.Add(p);

                    UpdateFilterOptions(distinctColors, distinctSizes);
                });
            }
            catch
            {
                MessageBox.Show("Probleme de connexion", "Data Loading Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { IsLoading = false; }
        }

        [RelayCommand] private void ToggleReception() { IsReceptionVisible = !IsReceptionVisible; IsEditMode = false; ClearDraft(); }
        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategory = null;
            SelectedBrand = null;
            FilterColor = null;
            FilterFullSize = null;
            IsManualSkuEnabled = false;
            DraftSKU = string.Empty;
            FilteredStockView?.Refresh(); // rafraîchir la vue filtrée
        }
        [RelayCommand]
        private void PrepareEdit(ProductVariantModel item)
        {
            if (item == null) return;
            IsEditMode = true;
            IsReceptionVisible = true;

            DraftCategory = Categories.FirstOrDefault(c => c.CategoryName == item.CategoryName);
            DraftBrand = Brands.FirstOrDefault(b => b.BrandName == item.BrandName);
            DraftNewBrandName = string.Empty; // corriger l'auto-assignation

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
            if (!IsFormValid()) return;
            int numericSize = 0;
            bool hasNumericSize = int.TryParse(DraftNumericSize, out numericSize);

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

                var UpdatedVariant = await _stockService.UpdateProductVariantAsync(updateRequest);

                if (UpdatedVariant != null)
                {
                    _cacheService.Remove(CacheKeys.StockVariants);
                    if (!string.IsNullOrWhiteSpace(UpdatedVariant.BrandName))
                        _cacheService.Remove(CacheKeys.Catalog);

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
                    _cacheService.Remove(CacheKeys.StockVariants);
                    if (request.BrandId == null && !string.IsNullOrWhiteSpace(request.BrandName))
                        _cacheService.Remove(CacheKeys.Catalog);
                    await Task.Delay(300);
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
                    if (updateRequest.BrandId == null && !string.IsNullOrWhiteSpace(updateRequest.BrandName))
                        _cacheService.Remove(CacheKeys.Catalog);
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

        // ===========================================================
        // VII. HELPERS & TRIGGERS
        // ===========================================================
        private CancellationTokenSource? _searchDebounceToken;

        partial void OnSearchTextChanged(string value)
        {
            _searchDebounceToken?.Cancel();
            _searchDebounceToken = new CancellationTokenSource();
            var token = _searchDebounceToken.Token;

            Task.Delay(500, token).ContinueWith(_ =>
            {
                if (!token.IsCancellationRequested)
                {
                    Application.Current.Dispatcher.Invoke(() => FilteredStockView?.Refresh());
                }
            }, token);
        }
        partial void OnSelectedCategoryChanged(CategoryModel value) => FilteredStockView?.Refresh();
        partial void OnSelectedBrandChanged(BrandsModel value) => FilteredStockView?.Refresh();
        partial void OnFilterColorChanged(string value) => FilteredStockView?.Refresh();
        partial void OnFilterFullSizeChanged(string value) => FilteredStockView?.Refresh();
        partial void OnDraftCategoryChanged(CategoryModel value)
        {
            if (value == null) return;
            IsDimensionSizeVisible = value.CategoryName.Contains("Thob") ||
                                     value.CategoryName.Contains("Pantalon") ||
                                     value.CategoryName.Contains("Sous");
            IsNumericSizeVisible = !IsDimensionSizeVisible;

            FilteredBrands.Filter = (obj) =>
            {
                if (obj is BrandsModel brand)
                    return brand.CategoryID == value.CategoryID;
                return false;
            };
            FilteredBrands.Refresh();
        }
        partial void OnIsNonActiveItemsVisibleChanged(bool value) => FilteredStockView?.Refresh();
        partial void OnIsEditModeChanged(bool value) => IsNotEditMode = !value;
        private async Task ForceReloadAllDataAsync()
        {
            _cacheService.Remove(CacheKeys.StockVariants);
            _cacheService.Remove(CacheKeys.Catalog);
            _cacheService.Remove(CacheKeys.Products);
            await Task.Delay(300);
            await LoadInitialDataAsync(forceRefresh: true);
        }

        // ===========================================================
        // Filtre ICollectionView pour le stock
        // ===========================================================
        private bool StockFilter(object obj)
        {
            if (obj is not ProductVariantModel p) return false;

            // Filtre d'activité
            if (!IsNonActiveItemsVisible && p.IsActive != true)
                return false;

            // Recherche textuelle
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                if (!(p.ProductName?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                      p.Sku?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true))
                    return false;
            }

            // Filtres par propriétés
            if (SelectedCategory != null && !string.Equals(p.CategoryName, SelectedCategory.CategoryName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (SelectedBrand != null && !string.Equals(p.BrandName, SelectedBrand.BrandName, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(FilterColor) && !string.Equals(p.Color, FilterColor, StringComparison.OrdinalIgnoreCase))
                return false;
            if (!string.IsNullOrWhiteSpace(FilterFullSize) && !string.Equals(p.FullSize, FilterFullSize, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
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
    }
}