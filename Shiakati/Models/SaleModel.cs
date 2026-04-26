using CommunityToolkit.Mvvm.ComponentModel;

namespace Shiakati.Models
{
    public partial class SaleModel : ObservableObject
    {
        [ObservableProperty]
        private int _saleID;
        [ObservableProperty]
        private int _ticketNumber;
        [ObservableProperty]
        private DateTime _saleDate;
        [ObservableProperty]
        private decimal _totalAmount;
        [ObservableProperty]
        private int _userID;
        [ObservableProperty]
        private decimal _globalDiscount;
    }
}
