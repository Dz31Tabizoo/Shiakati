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
    /// Logique d'interaction pour ReservationsView.xaml
    /// </summary>
    public partial class ReservationsView : UserControl
    {
        public ReservationsView()
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
