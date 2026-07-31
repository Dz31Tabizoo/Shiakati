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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shiakati.Views
{
    /// <summary>
    /// Logique d'interaction pour ClientDetailView.xaml
    /// </summary>
    public partial class ClientDetailView : UserControl
    {
        public ClientDetailView()
        {
            InitializeComponent();
            this.Unloaded += (s, e) =>
            {
                // Dispose du ViewModel lorsque la vue est déchargée
                if (this.DataContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };

        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                Window parentWindow = Window.GetWindow(this);
                parentWindow?.DragMove();
            }
        }

        // Close the parent window
        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            parentWindow?.Close();
        }

        private void OnlyDigits_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !System.Text.RegularExpressions.Regex.IsMatch(e.Text, "^[0-9]+$");
        }

        private void OnDataGridRowPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.IsSelected)
            {
                // Sécurité : Si l'utilisateur clique sur un bouton (comme vos actions), on ne fait rien
                if (e.OriginalSource is DependencyObject visual)
                {
                    if (HasAncestor<Button>(visual))
                    {
                        return; // Laisse le bouton intercepter le clic normalement
                    }
                }

                // Si la ligne est déjà sélectionnée et qu'on re-clique dessus, on désélectionne
                row.IsSelected = false;
                e.Handled = true; // Indique à WPF que le clic a été géré
            }
        }

        // Fonction utilitaire pour détecter si le clic vient d'un bouton interne
        private bool HasAncestor<T>(DependencyObject element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T) return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

    }
}
