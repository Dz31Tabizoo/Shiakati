using CommunityToolkit.Mvvm.ComponentModel;


namespace Shiakati.Models
{
    public partial class ProductModel : ObservableObject
    {
        [ObservableProperty]
        private int? _productID;

        [ObservableProperty]
        private int? _brandID;
        [ObservableProperty]
        private bool _isActive;
        [ObservableProperty]
        private string _productName;
        [ObservableProperty]        
        private string? _imagePath;
    }
}
