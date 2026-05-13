
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using global::Shiakati.Models;
    using Shiakati.Services.Interfaces;
    using Shiakati.Services.Implementations;
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using System.Windows;
using Serilog;


namespace Shiakati.ViewModels
    {
    public partial class StockViewModel : ObservableObject
    {
        private readonly IBarCodePrintService _printerService;
        private readonly ICatalogService _db;
        private readonly IProductsService _productsService;

        public StockViewModel(IBarCodePrintService printerService, ICatalogService db,IProductsService productsService)
        {
            _printerService = printerService;
            _db = db;
            _productsService = productsService;

            // Toujours instancier les listes pour éviter les erreurs de Binding
            Categories = new ObservableCollection<CategoryModel>();
            Brands = new ObservableCollection<BrandsModel>();
            Products = new ObservableCollection<ProductModel>();
            FilteredStock = new ObservableCollection<ProductVariantsModel>();
        }

        // ==========================================
        // UI STATE
        // ==========================================
        [ObservableProperty] private bool _isReceptionVisible;
        [ObservableProperty] private bool _isNumericSizeVisible = false;
        [ObservableProperty] private bool _isDimensionSizeVisible = true;
        [ObservableProperty] private bool _isLoading; // Pour afficher un Spinner si besoin

        [RelayCommand]
        private void ToggleReception() => 
            
            
            
            
            
            
            
            
            
            
            
            
            
            
            IsReceptionVisible = !IsReceptionVisible;

        // ==========================================
        // FILTER PROPERTIES
        // ==========================================
        [ObservableProperty] private string _searchText;
        [ObservableProperty] private CategoryModel _selectedCategory;
        [ObservableProperty] private BrandsModel _selectedBrand;
        [ObservableProperty] private string _filterColor;
        [ObservableProperty] private string _filterFullSize;
        [ObservableProperty] private ProductVariantsModel _selectedStockItem;

        public ObservableCollection<string> WidthsList { get; } = new() { "XS", "S", "M", "L", "XL", "XXL", "XXXL", "1", "2", "3", "4", "5" };
        public ObservableCollection<ProductVariantsModel> FilteredStock { get; }
        public ObservableCollection<CategoryModel> Categories { get; }
        public ObservableCollection<BrandsModel> Brands { get; }
        public ObservableCollection<ProductModel> Products { get; }

        // Triggers asynchrones (Générés par CommunityToolkit)
        // On wrap dans un try-catch car async void ne remonte pas les exceptions proprement
        async partial void OnSearchTextChanged(string value) => await SafeApplyFiltersAsync();
        async partial void OnSelectedCategoryChanged(CategoryModel value) => await SafeApplyFiltersAsync();
        async partial void OnSelectedBrandChanged(BrandsModel value) => await SafeApplyFiltersAsync();
        async partial void OnFilterColorChanged(string value) => await SafeApplyFiltersAsync();
        async partial void OnFilterFullSizeChanged(string value) => await SafeApplyFiltersAsync();

        private async Task SafeApplyFiltersAsync()
        {
            try { await ApplyFiltersAsync(); }
            catch (Exception ex) { Log.Error(ex, "Erreur filtres"); }
        }

        [RelayCommand]
        private async Task ClearFiltersAsync()
        {
            _searchText = string.Empty; // Utiliser le champ privé pour éviter de déclencher 5 appels API
            _selectedCategory = null;
            _selectedBrand = null;
            _filterColor = string.Empty;
            _filterFullSize = string.Empty;

            // On notifie les changements manuellement
            OnPropertyChanged(nameof(SearchText));
            OnPropertyChanged(nameof(SelectedCategory));
            OnPropertyChanged(nameof(SelectedBrand));
            OnPropertyChanged(nameof(FilterColor));
            OnPropertyChanged(nameof(FilterFullSize));

            await ApplyFiltersAsync();
        }

        private async Task ApplyFiltersAsync()
        {
            // TODO: Appeler ton IStockApiService ici
            await Task.CompletedTask;
        }

        public async Task LoadInitialDataAsync()
        {
            if (IsLoading) return;

            try
            {
                IsLoading = true;
                var catalog = await _db.GetInitialGatalogDataAsync();
                var products = await _productsService.GetProductsAsync();

                // On vide et on remplit pour garder la même instance de collection (Best Practice WPF)
                Categories.Clear();
                foreach (var cat in catalog.Categories) Categories.Add(cat);  
                Brands.Clear();
                foreach (var b in catalog.Brands) Brands.Add(b);                    
                Products.Clear();
                foreach (var p in products) Products.Add(p);
                

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
            string catName = value.CategoryName.ToLower();

            // Logique de visibilité simplifiée
            bool isSpecial = catName.Contains("cosmetic") || catName.Contains("shoe") ||
                             catName.Contains("chaussure") || catName.Contains("chaise");

            IsNumericSizeVisible = isSpecial;
            IsDimensionSizeVisible = !isSpecial;

            if (isSpecial) { DraftWidth = null; DraftLength = null; }
            else { DraftNumericSize = null; }
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
    }
}

