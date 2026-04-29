
    using CommunityToolkit.Mvvm.ComponentModel;
    using CommunityToolkit.Mvvm.Input;
    using global::Shiakati.Models;
    //using Shiakati.Services;
    using System;
    using System.Collections.ObjectModel;
    using System.Threading.Tasks;
    using System.Windows;


    namespace Shiakati.ViewModels
    {
    public partial class StockViewModel : ObservableObject
    {
        //private readonly IStockApiService _apiService;

        //public StockViewModel(IStockApiService apiService)
        //{
        //    _apiService = apiService;
        //    _ = LoadInitialDataAsync(); // Fire and forget au démarrage
        //}

        // ==========================================
        // UI VISIBILITY STATE
        // ==========================================

        [ObservableProperty]
        private bool _isReceptionVisible;

        [ObservableProperty]
        private bool _isNumericSizeVisible = false;

        [ObservableProperty]
        private bool _isDimensionSizeVisible = true;

        [RelayCommand]
        private void ToggleReception() => IsReceptionVisible = !IsReceptionVisible;

        // ==========================================
        // LEFT PANE: FILTER PROPERTIES & DATA
        // ==========================================
        [ObservableProperty] private string _searchText;
        [ObservableProperty] private CategoryModel _selectedCategory;
        [ObservableProperty] private BrandsModel _selectedBrand;
        [ObservableProperty] private string _filterColor;
        [ObservableProperty] private string _filterFullSize;

        [ObservableProperty] private ProductVariantsModel _selectedStockItem;

            public ObservableCollection<string> WidthsListe = new() { "XS", "S", "M", "L", "XL","XXL","XXXL","1","2","3","4","5" };
            public ObservableCollection<ProductVariantsModel> FilteredStock { get; } = new();
            public ObservableCollection<CategoryModel> Categories { get; } = new();
            public ObservableCollection<BrandsModel> Brands { get; } = new();

            // Triggers asynchrones lors du changement des filtres
            async partial void OnSearchTextChanged(string value) => await ApplyFiltersAsync();
            async partial void OnSelectedCategoryChanged(CategoryModel value) => await ApplyFiltersAsync();
            async partial void OnSelectedBrandChanged(BrandsModel value) => await ApplyFiltersAsync();
            async partial void OnFilterColorChanged(string value) => await ApplyFiltersAsync();
            async partial void OnFilterFullSizeChanged(string value) => await ApplyFiltersAsync();

            [RelayCommand]
            private async Task ClearFiltersAsync()
            {
                SearchText = string.Empty;
                SelectedCategory = null;
                SelectedBrand = null;
                FilterColor = string.Empty;
                FilterFullSize = string.Empty;
                await ApplyFiltersAsync();
            }

            private async Task ApplyFiltersAsync()
            {
                try
                {
                    //var results = await _apiService.GetFilteredStockAsync(
                    //    SearchText,
                    //    SelectedCategory?.CategoryID,
                    //    SelectedBrand?.BrandID,
                    //    FilterColor,
                    //    FilterFullSize);

                    FilteredStock.Clear();
                    //foreach (var item in results)
                    //{
                    //    FilteredStock.Add(item);
                    //}
                }
                catch (Exception ex)
                {
                    // TODO: Gérer l'erreur (ex: Snackbar ou MessageBox)
                    MessageBox.Show($"Erreur lors du filtrage: {ex.Message}");
                }
            }

            //private async Task LoadInitialDataAsync()
            //{
            //    var categories = await _apiService.GetCategoriesAsync();
            //    foreach (var cat in categories) Categories.Add(cat);

            //    var brands = await _apiService.GetBrandsAsync();
            //    foreach (var brand in brands) Brands.Add(brand);

            //    var names = await _apiService.GetAllProductNamesAsync();
            //    foreach (var name in names) AllProductNames.Add(name);

            //    var colors = await _apiService.GetAllColorsAsync();
            //    foreach (var color in colors) AllColors.Add(color);

            //    await ApplyFiltersAsync();
            //}

            // ==========================================
            // RIGHT PANE: RECEPTION FORM PROPERTIES
            // ==========================================
            [ObservableProperty] private CategoryModel _draftCategory;
            [ObservableProperty] private BrandsModel _draftBrand;
            [ObservableProperty] private string _draftProductName;
            [ObservableProperty] private string _draftSKU; // Généré auto par le backend
            [ObservableProperty] private string _draftColor;
            [ObservableProperty] private string _draftNumericSize;
            [ObservableProperty] private string _draftWidth;
            [ObservableProperty] private int? _draftLength;
            [ObservableProperty] private decimal? _draftPurchasePrice;
            [ObservableProperty] private decimal _draftSalePrice;
            [ObservableProperty] private decimal? _draftFixedDiscount;
            [ObservableProperty] private int _draftQuantity = 1;
            [ObservableProperty] private bool _printLabelsOnSave = true;
            [ObservableProperty] private int _labelsToPrint = 1;

            public ObservableCollection<string> AllProductNames { get; } = new();
            public ObservableCollection<string> AllColors { get; } = new();

            // Lie la quantité reçue à la quantité d'étiquettes par défaut
            partial void OnDraftQuantityChanged(int value)
            {
                LabelsToPrint = value;
            }

            // Logique de masquage des champs de taille selon la catégorie
            partial void OnDraftCategoryChanged(CategoryModel value)
            {
                if (value == null) return;

                string catName = value.CategoryName.ToLower();

                if (catName.Contains("cosmetic") || catName.Contains("shoe") ||
                    catName.Contains("chaussure") || catName.Contains("chaise"))
                {
                    IsNumericSizeVisible = true;
                    IsDimensionSizeVisible = false;
                    DraftWidth = null;
                    DraftLength = null;
                }
                else
                {
                    IsNumericSizeVisible = false;
                    IsDimensionSizeVisible = true;
                    DraftNumericSize = null;
                }
            }

            [RelayCommand]
            private async Task ReceiveStockAsync()
            {
                if (string.IsNullOrWhiteSpace(DraftProductName) || DraftSalePrice <= 0)
                {
                    MessageBox.Show("Veuillez remplir le nom du produit et le prix de vente minimum.");
                    return;
                }

                //var dto = new StockReceptionDto
                //{
                //    CategoryId = DraftCategory?.CategoryID,
                //    BrandId = DraftBrand?.BrandID,
                //    ProductName = DraftProductName,
                //    Color = DraftColor,
                //    NumericSize = DraftNumericSize,
                //    Length = DraftLength,
                //    Width = DraftWidth,
                //    PurchasePrice = DraftPurchasePrice,
                //    SalePrice = DraftSalePrice,
                //    FixedDiscount = DraftFixedDiscount,
                //    Quantity = DraftQuantity
                //};

                try
                {
                    //bool success = await _apiService.ReceiveStockAsync(dto);

                    //if (success)
                    //{
                    //    if (PrintLabelsOnSave && LabelsToPrint > 0)
                    //    {
                    //        // TODO: Appeler ici le service d'impression local
                    //        // _printerService.PrintBarcode(..., LabelsToPrint);
                    //    }

                    //    // Rafraîchir la grille
                    //    await ApplyFiltersAsync();

                    //    // Optionnel : Réinitialiser le formulaire
                    //    ResetDraftForm();
                    //    MessageBox.Show("Réception enregistrée avec succès.");
                    //}
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erreur lors de la réception: {ex.Message}");
                }
            }

            private void ResetDraftForm()
            {
                DraftQuantity = 1;
                DraftSKU = string.Empty;
                // Ne pas forcément réinitialiser la catégorie/marque si l'utilisateur saisit un lot similaire
            }
        }
    }

