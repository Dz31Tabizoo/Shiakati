using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shiakati.Views
{
    public partial class RegisterDialog : Window
    {
        // Public properties to retrieve values after dialog closes
        public string Username => UsernameBox.Text;
        public string Password => NewPasswordBox.Password;
        public string Role => Roles.SelectedItem?.ToString() ?? "user";

        public RegisterDialog()
        {
            InitializeComponent();

            // Populate the ComboBox with available roles
            Roles.ItemsSource = new List<string> { "admin", "manager", "vendeur" };
            Roles.SelectedIndex = 0; // Select "admin" by default
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(Username))
            {
                MessageBox.Show("Veuillez saisir un nom d'utilisateur.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show("Veuillez saisir un mot de passe.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Les mots de passe ne correspondent pas.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Password.Length < 4)
            {
                MessageBox.Show("Le mot de passe doit contenir au moins 4 caractères.", "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (Roles.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un rôle.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validation passed – close with success
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
}
