using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Shiakati.Views
{
    public partial class HonorReservationDialog : Window, INotifyPropertyChanged
    {
        private decimal _deposit;
        private decimal _remaining;
        private decimal _amountPaid;
        private decimal _newDebt;

        public decimal Deposit { get => _deposit; set { _deposit = value; OnPropertyChanged(); } }
        public decimal Remaining { get => _remaining; set { _remaining = value; OnPropertyChanged(); CalculateNewDebt(); } }
        public decimal AmountPaid { get => _amountPaid; set { _amountPaid = value; OnPropertyChanged(); CalculateNewDebt(); } }
        public decimal NewDebt { get => _newDebt; set { _newDebt = value; OnPropertyChanged(); } }

        public HonorReservationDialog(decimal deposit, decimal remaining)
        {
            InitializeComponent();
            DataContext = this;
            Deposit = deposit;
            Remaining = remaining;
            AmountPaid = 0;   // default: no additional payment; the credit will be cancelled
        }

        private void CalculateNewDebt()
        {
            NewDebt = Remaining - AmountPaid;
            if (NewDebt < 0) { NewDebt = 0; AmountPaid = Remaining; }
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
}