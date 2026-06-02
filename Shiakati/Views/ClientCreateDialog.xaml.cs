using System;
using System.Collections.Generic;
using System.Linq;
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
using static QRCoder.PayloadGenerator;

namespace Shiakati.Views
{
    /// <summary>
    /// Logique d'interaction pour ClientCreateDialog.xaml
    /// </summary>
    public partial class ClientCreateDialog : Window
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Email { get; set; }

        public ClientCreateDialog()
        {
            InitializeComponent();
            DataContext = this;
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(PhoneNumber))
            {
                MessageBox.Show("Le nom et le téléphone sont obligatoires.", "Validation",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
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
    }
}
