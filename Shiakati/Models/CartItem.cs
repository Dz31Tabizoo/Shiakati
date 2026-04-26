using CommunityToolkit.Mvvm.ComponentModel;
using Shiakati.Models;


namespace Shiakati.Models
{
    public partial class CartItem : ObservableObject
    {
        public ProductVariantsModel Variant { get; }
        public ProductModel Product { get; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPrice))]
        private int _quantity;

        public decimal TotalPrice => Variant.SalePrice * Quantity;

        // Utilisation de ?. au cas où Product ou Variant seraient nuls
        public string DisplayName => $"{Product?.ProductName} {Variant?.FullSize} {Variant?.Color}".Trim();

        public CartItem(ProductVariantsModel variant, ProductModel product)
        {
            Variant = variant;
            Product = product;
            _quantity = 1; // Initialisation directe du champ privé
        }
    }
}

