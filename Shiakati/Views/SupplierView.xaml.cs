using Shiakati.Models;
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
    /// Logique d'interaction pour SupplierView.xaml
    /// </summary>
    public partial class SupplierView : UserControl
    {
        public SupplierView()
        {
            InitializeComponent();
        }

        private void PreviewInvoice_Click(object sender, RoutedEventArgs e)
        {
            var invoice = (sender as FrameworkElement)?.Tag as InvoiceImageDto;
            if (invoice == null) return;

            // Open a simple dialog to show the image
            var previewWindow = new Window
            {
                Title = invoice.FileName,
                Width = 600,
                Height = 500,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Application.Current.MainWindow,
                Content = new Grid
                {
                    Margin = new Thickness(20),
                    Children =
            {
                new Image
                {
                    Source = new BitmapImage(new Uri(invoice.FilePath, UriKind.RelativeOrAbsolute)),
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
                }
            };
            previewWindow.ShowDialog();
        }
    }
}
