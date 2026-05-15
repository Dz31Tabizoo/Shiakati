using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Logique d'interaction pour PrintQuantityDialog.xaml
    /// </summary>
    public partial class PrintQuantityDialog : Window
    {
        public int Quantity { get; set; }
        public PrintQuantityDialog()
        {
            InitializeComponent();
            TxtQuantity.Focus();
            TxtQuantity.Text = "1";
        }

        private void Print_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtQuantity.Text, out int result) && result > 0)
            {
                Quantity = result;
                this.DialogResult = true; // Indique que l'utilisateur a validé
                this.Close();
            }
            else
            {
                MessageBox.Show("Veuillez saisir une quantité valide.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Sécurité : N'autorise que les chiffres (0-9)
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
