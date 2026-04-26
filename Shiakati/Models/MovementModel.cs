using CommunityToolkit.Mvvm.ComponentModel;



namespace Shiakati.Models
{
    public partial class MovementModel : ObservableObject
    {
        [ObservableProperty]
        private int _movementID;
        [ObservableProperty]
        private string _movementType = string.Empty;
        [ObservableProperty]
        private int _quantity;
        [ObservableProperty]
        private DateTime _movementDate;
        [ObservableProperty]
        private int _referenceID;
        [ObservableProperty]
        private string _note = string.Empty;
        [ObservableProperty]
        private int _userID;
    }
}
