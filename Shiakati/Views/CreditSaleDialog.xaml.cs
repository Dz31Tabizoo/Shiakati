using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Shiakati.Views
{
    /// <summary>
    /// Logique d'interaction pour CreditSaleDialog.xaml
    /// </summary>
    public partial class CreditSaleDialog : Window, INotifyPropertyChanged
    {
        private decimal _total;
        private decimal _paidAmount;
        private decimal _rest;
        private DateTime? _expiresAt;

        public decimal Total { get => _total; set { _total = value; OnPropertyChanged(); CalculateRest(); } }
        public decimal PaidAmount { get => _paidAmount; set { _paidAmount = value; OnPropertyChanged(); CalculateRest(); } }
        public decimal Rest { get => _rest; set { _rest = value; OnPropertyChanged(); } }
        public DateTime? ExpiresAt { get => _expiresAt; set { _expiresAt = value; OnPropertyChanged(); } }

        public CreditSaleDialog(decimal total)
        {
            InitializeComponent();
            DataContext = this;
            Total = total;
            PaidAmount = 0;
        }

        private void CalculateRest()
        {
            Rest = Total - PaidAmount;
            if (Rest < 0) { Rest = 0; PaidAmount = Total; }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnlyDigits_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, @"^\d+([.,]\d{0,2})?$");
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
