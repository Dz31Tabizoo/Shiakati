using CommunityToolkit.Mvvm.ComponentModel;

namespace Shiakati.Models
{
    public partial class SaleItemModel : ObservableObject
    {
        [ObservableProperty]
        private int? _saleItemID;
        [ObservableProperty]
        private int? _saleID;
        [ObservableProperty]
        private int? _variantID;
        [ObservableProperty]
        private int? _quantity;
        [ObservableProperty]
        private decimal? _discountAmount;
        [ObservableProperty]
        private decimal? _lineTotal;
    }
}
