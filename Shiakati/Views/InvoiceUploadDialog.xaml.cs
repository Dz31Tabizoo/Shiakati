using System;
using System.Windows;
using Microsoft.Win32;

namespace Shiakati.Views
{
    public partial class InvoiceUploadDialog : Window
    {
        public string FilePath { get; private set; } = string.Empty;
        public DateTime? InvoiceDate { get; private set; }
        public int? ProductsTotal { get; private set; }
        public decimal? TotalAmount { get; private set; }
        public decimal? AmountPaid { get; private set; }

        public InvoiceUploadDialog()
        {
            InitializeComponent();
            InvoiceDatePicker.SelectedDate = DateTime.Today;
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.jpeg;*.png;*.bmp;*.pdf)|*.jpg;*.jpeg;*.png;*.bmp;*.pdf"
            };
            if (dialog.ShowDialog() == true)
            {
                FilePath = dialog.FileName;
                FileNameText.Text = System.IO.Path.GetFileName(FilePath);
                FileNameText.Foreground = FindResource("PrimaryBrush") as System.Windows.Media.Brush;
            }
        }

        private void ProductsTotalBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (int.TryParse(ProductsTotalBox.Text, out int value))
                ProductsTotal = value;
        }

        private void TotalAmountBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (decimal.TryParse(TotalAmountBox.Text, out decimal value))
                TotalAmount = value;
        }

        private void AmountPaidBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (decimal.TryParse(AmountPaidBox.Text, out decimal value))
                AmountPaid = value;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                MessageBox.Show("Veuillez sélectionner un fichier.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            InvoiceDate = InvoiceDatePicker.SelectedDate;
            DialogResult = true;
            Close();
        }
    }
}