using CommunityToolkit.Mvvm.ComponentModel;

namespace Shiakati.ViewModels
{
    public partial class POSViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _tabName;
        //add panier ICI 
        // public ObservableCollection<SaleItem> Panier { get; set; } = new();

        public POSViewModel(string name)
        {
            TabName = name;
        }
        
    }
}
