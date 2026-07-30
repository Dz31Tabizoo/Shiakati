using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Shiakati.Views
{
    public partial class ProductSelectionForInventoryDialog : Window
    {
        public List<ProductSelectionItem> SelectedProducts => Products.Where(p => p.IsSelected).ToList();
        private List<ProductSelectionItem> Products { get; }

        public ProductSelectionForInventoryDialog(List<ProductSelectionItem> items)
        {
            InitializeComponent();
            Products = items;
            ProductsGrid.ItemsSource = Products;
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (!Products.Any(p => p.IsSelected))
            {
                MessageBox.Show("Aucun produit sélectionné.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
                this.DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class ProductSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string BrandName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));


    }


}
