using CommunityToolkit.Mvvm.ComponentModel;

namespace Shiakati.Models
{
    public partial class CartItem : ObservableObject
    {
        public Product ProductRef { get; set; }
        public string ProductName => ProductRef.Name;
        public decimal UnitPrice => ProductRef.Price;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(TotalPrice))] // Met à jour le Total quand la Qté change !
        private int _quantity;

        public decimal TotalPrice => Quantity * UnitPrice;

        public CartItem(Product product, int quantity = 1)
        {
            ProductRef = product;
            Quantity = quantity;
        }
    }
}
