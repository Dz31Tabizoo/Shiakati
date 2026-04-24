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

    /// <summary>
    /// Logique d'interaction pour LoginView.xaml
    /// </summary>
namespace Shiakati.Views
    {
        public partial class LoginView : Window
        {
            public LoginView()
            {
                InitializeComponent();
            }

            // Permet de déplacer la fenêtre avec la souris
            private void Window_MouseDown(object sender, MouseButtonEventArgs e)
            {
                if (e.ChangedButton == MouseButton.Left)
                    this.DragMove();
            }

            // CORRECTION : Le nom doit correspondre à Click="BtnClose_Click" dans le XAML
            private void BtnClose_Click(object sender, RoutedEventArgs e)
            {
                Application.Current.Shutdown();
            }

            // CORRECTION : Ajout de la méthode manquante pour Click="BtnLogin_Click"
            private void BtnLogin_Click(object sender, RoutedEventArgs e)
            {
                // Simulation de connexion pour le moment
                string username = TxtUsername.Text;
                string password = TxtPassword.Password;

                if (username == "admin" && password == "admin")
                {
                    // Si c'est bon, on ouvre la MainWindow
                    MainWindow main = new MainWindow();
                    main.Show();
                    this.Close();
                }
                else
                {
                    // Affichage du message d'erreur défini dans ton XAML
                    TxtErrorMessage.Visibility = Visibility.Visible;
                }
            }
        }
    }


