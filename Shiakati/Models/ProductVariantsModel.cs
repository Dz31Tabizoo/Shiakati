using CommunityToolkit.Mvvm.ComponentModel;

namespace Shiakati.Models
{
    public partial class ProductVariantsModel : ObservableObject
    {
        [ObservableProperty]
        private int _variantID;

        [ObservableProperty]
        private int _productID;
        [ObservableProperty]
        private string _sKU;
        
        [ObservableProperty]
        private string _color;

        [ObservableProperty]
        private int _lenth;
        [ObservableProperty]
        private string _width;

        [ObservableProperty]
        private decimal _purchasePrice;
        [ObservableProperty]
        private decimal _discountFixed;
        [ObservableProperty]
        private decimal _salePrice;

        [ObservableProperty]
        private int _stockQuantity;
        [ObservableProperty]
        private string _fullSize;

        [ObservableProperty]
        private bool _isActive;
    }
}