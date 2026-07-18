using Microsoft.Extensions.DependencyInjection;
using Shiakati.ViewModels;
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
using System.Text.RegularExpressions;

namespace Shiakati.Views
{
    /// <summary>
    /// Logique d'interaction pour StockView.xaml
    /// </summary>
    public partial class StockView : UserControl
    {
        public StockView()
        {
            InitializeComponent();

            this.Unloaded +=(s , e) =>
            {
                // Dispose du ViewModel lorsque la vue est déchargée
                if (this.DataContext is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };
        }

        private void OnlyDigits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Regex pour autoriser UNIQUEMENT les chiffres [0-9]
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void OnlyDigits_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Bloque la touche Espace car PreviewTextInput ne la détecte pas
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            // Sécurité : Si l'utilisateur tente de copier-coller du texte (ex: "abc")
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                Regex regex = new Regex("[^0-9]+");
                if (regex.IsMatch(text))
                {
                    e.CancelCommand(); // Annule le copier-coller
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
