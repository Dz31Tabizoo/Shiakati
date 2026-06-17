using CommunityToolkit.Mvvm.ComponentModel;

namespace Shiakati.Models
{
    public partial class SaleModel : ObservableObject
    {
        [ObservableProperty] private int? _saleID;
        [ObservableProperty] private string _ticketNumber = string.Empty;
        [ObservableProperty] private DateTime? _saleDate;
        [ObservableProperty] private decimal? _totalAmount;
        [ObservableProperty] private decimal? _globalDiscount;
        [ObservableProperty] private bool _isVoided;
        [ObservableProperty] private string? _userName = string.Empty;
    }
}
