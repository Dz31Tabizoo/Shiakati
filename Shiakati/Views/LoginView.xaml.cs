using Microsoft.Extensions.DependencyInjection;
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
        public event Action? LoginSucceeded;

        public LoginView(LoginViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            viewModel.RequestClose += () =>
            {
                this.Dispatcher.Invoke(() =>
                {
                    this.Close();
                });
            };

            // Forward the login success from ViewModel to the event
            viewModel.LoginCompleted += () =>
            {
                LoginSucceeded?.Invoke();
            };
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }




    }
}


