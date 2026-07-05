using Microsoft.Win32;
using Shiakati.Models;
using System;
using System.IO;
using System.Windows;

namespace Shiakati.Views
{
    public partial class InvoiceUploadDialog : Window
    {
        public string FilePath { get; private set; } = string.Empty;
        public DateTime? InvoiceDate { get; private set; }
        public int? ProductsTotal { get; private set; }
        public decimal? TotalAmount { get; private set; }
        public decimal? AmountPaid { get; private set; }
        public bool IsEditMode { get; private set; }
        public bool FileReplaced { get; private set; }

        // ─── Constructor for ADD ──────────────────────────────────────────
        public InvoiceUploadDialog()
        {
            InitializeComponent();
            InvoiceDatePicker.SelectedDate = DateTime.Today;
            IsEditMode = false;
            FileReplaced = false;
        }

        // ─── Constructor for EDIT ─────────────────────────────────────────
        public InvoiceUploadDialog(InvoiceImageDto invoice) : this()
        {
            IsEditMode = true;
            InvoiceDatePicker.SelectedDate = invoice.InvoiceDate ?? DateTime.Today;
            ProductsTotalBox.Text = invoice.ProductsTotal?.ToString() ?? "";
            TotalAmountBox.Text = invoice.TotalAmount?.ToString("N2") ?? "";
            AmountPaidBox.Text = invoice.AmountPaid?.ToString("N2") ?? "";

            // Show existing file info
            FileNameText.Text = invoice.FileName ?? "Aucun fichier";
            FilePath = invoice.FilePath ?? "";

            // Hide the main "Choose file" button, show the replace panel
            SelectFileButton.Visibility = Visibility.Collapsed;
            FileInfoPanel.Visibility = Visibility.Visible;
            Title = "Modifier la facture";
        }

        // ─── File selection ──────────────────────────────────────────────
        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            PickFile();
        }

        private void ReplaceFileButton_Click(object sender, RoutedEventArgs e)
        {
            PickFile();
            ReplaceFileButton.Content = "Remplacer ✓"; // optional feedback
        }

        private void PickFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|" +
                         "PDF (*.pdf)|*.pdf|" +
                         "Word (*.docx;*.doc)|*.docx;*.doc|" +
                         "Excel (*.xlsx;*.xls;*.csv)|*.xlsx;*.xls;*.csv|" +
                         "Tous les fichiers (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                FilePath = dialog.FileName;
                FileNameText.Text = Path.GetFileName(FilePath);
                FileNameText.Foreground = FindResource("PrimaryBrush") as System.Windows.Media.Brush;
                FileReplaced = true;
            }
        }

        // ─── Text changed handlers ───────────────────────────────────────
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

        // ─── Buttons ──────────────────────────────────────────────────────
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(FilePath))
            {
                var msgResult = MessageBox.Show("Vous n'avais pas sélectionner un fichier, Voullez vous continuer ? ", "Attention", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation);
                if (msgResult == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            InvoiceDate = InvoiceDatePicker.SelectedDate;
            DialogResult = true;
            Close();
        }
    }
}