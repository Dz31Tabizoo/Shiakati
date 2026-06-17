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

namespace Shiakati.Views
{
    /// <summary>
    /// Logique d'interaction pour ChangePasswordDialog.xaml
    /// </summary>
    public partial class ChangePasswordDialog : Window
    {
        public ChangePasswordDialog()
        {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OldPasswordBox.Password) ||
                string.IsNullOrWhiteSpace(NewPasswordBox.Password) ||
                string.IsNullOrWhiteSpace(ConfirmPasswordBox.Password))
            {
                MessageBox.Show("Veuillez remplir tous les champs.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Les mots de passe ne correspondent pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewPasswordBox.Password.Length < 4)
            {
                MessageBox.Show("Le mot de passe doit contenir au moins 4 caractères.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Tag = new { OldPassword = OldPasswordBox.Password, NewPassword = NewPasswordBox.Password };
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}
