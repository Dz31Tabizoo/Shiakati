using Shiakati.Models;
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

namespace Shiakati.Views
{
    /// <summary>
    /// Logique d'interaction pour ClientListView.xaml
    /// </summary>
    public partial class ClientListView : UserControl
    {
        public ClientListView()
        {
            InitializeComponent();
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && row.DataContext is ClientSummaryDto selectedClient)
            {
                
                var vm = DataContext as ClientListViewModel;
                vm?.OpenClientDetailCommand?.Execute(selectedClient);
            }
        }
    }
}
