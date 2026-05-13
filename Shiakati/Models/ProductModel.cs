using CommunityToolkit.Mvvm.ComponentModel;


namespace Shiakati.Models
{
    public partial class ProductModel : ObservableObject
    {
        [ObservableProperty]
        private int? _productID;

        [ObservableProperty]
        private string _brandName = string.Empty;
        [ObservableProperty]
        private bool _isActive;
        [ObservableProperty]
        private string _productName= string.Empty;
        [ObservableProperty]        
        private string? _imagePath;
        [ObservableProperty]
        private string? _categoryName = string.Empty;
    }
}
