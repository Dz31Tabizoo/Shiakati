using CommunityToolkit.Mvvm.ComponentModel;

namespace Shiakati.Models
{
    public partial class BrandsModel : ObservableObject
    {
        [ObservableProperty]
        private int _brandID;

        [ObservableProperty]
        private string _brandName = string.Empty;

        [ObservableProperty]
        private int _categoryID;
    }   

}
