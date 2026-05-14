using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Shiakati.Models
{
    public partial class ProductVariantModel : ObservableObject
    {
        [ObservableProperty] private int _variantId;
        [ObservableProperty] private int? _productId;
        [ObservableProperty] private string? _sku;
        [ObservableProperty] private string? _color;
        [ObservableProperty] private int? _length;
        [ObservableProperty] private string? _width;
        [ObservableProperty] private decimal? _purchasePrice;
        [ObservableProperty] private decimal? _discountFixed;
        [ObservableProperty] private decimal? _salePrice;
        [ObservableProperty] private int? _stockQuantity;
        [ObservableProperty] private string? _fullSize;
        [ObservableProperty] private bool? _isActive;
        [ObservableProperty] private string? _productName;
        [ObservableProperty] private string? _brandName;
        [ObservableProperty] private string? _categoryName;
    }
}