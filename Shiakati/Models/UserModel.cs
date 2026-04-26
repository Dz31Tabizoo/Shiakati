using CommunityToolkit.Mvvm.ComponentModel;


namespace Shiakati.Models
{
    public partial class UserModel : ObservableObject
    {
        [ObservableProperty]
        private int? _userID;
        [ObservableProperty]
        private string? _username = string.Empty;
        [ObservableProperty]
        private string _passwordHash = string.Empty;
        [ObservableProperty]
        private string _role = string.Empty;
    }
}
