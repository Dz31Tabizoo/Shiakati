using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Shiakati.Views
{
    public partial class ReservationDialog : Window, INotifyPropertyChanged
    {
        private decimal _total;
        private decimal _depositAmount;
        private decimal _remaining;
        private DateTime _expirationDate = DateTime.Today.AddDays(7);

        public decimal Total { get => _total; set { _total = value; OnPropertyChanged(); CalculateRemaining(); } }
        public decimal DepositAmount { get => _depositAmount; set { _depositAmount = value; OnPropertyChanged(); CalculateRemaining(); } }
        public decimal Remaining { get => _remaining; set { _remaining = value; OnPropertyChanged(); } }
        public DateTime ExpirationDate { get => _expirationDate; set { _expirationDate = value; OnPropertyChanged(); } }

        public ReservationDialog(decimal total)
        {
            InitializeComponent();
            DataContext = this;
            Total = total;
            DepositAmount = 0;
            ExpirationDate = DateTime.Today.AddDays(7);
        }

        private void CalculateRemaining()
        {
            Remaining = Total - DepositAmount;
            if (Remaining < 0) { Remaining = 0; DepositAmount = Total; }
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
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